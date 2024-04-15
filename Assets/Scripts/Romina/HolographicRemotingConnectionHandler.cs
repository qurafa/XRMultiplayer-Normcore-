/**
 * Romina:
 * Resource: https://learn.microsoft.com/en-us/dotnet/api/microsoft.mixedreality.openxr.remoting.appremoting?view=mixedreality-openxr-plugin-1.10
 * Resources for investigating the app quit bug: 
 *      https://github.com/microsoft/OpenXR-Unity-MixedReality-Samples/tree/main/RemotingSample
 * I might have over-complicated things with using the AppRemoting.Connected and AppRemoting.Disconnecting delegates, 
 * but in long run it might avoid having connection errors and not knowing
 * **/

using Microsoft.MixedReality.OpenXR.Remoting;
using System;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Management;
namespace com.perceptlab.armultiplayer
{
    public class HolographicRemotingConnectionHandler : MonoBehaviour
    {

        public enum HandlerConnectionState { Connected, Disconnected, Disconnecting };

        /// <summary>
        /// The connection state of handler. It is not safe to exit the app while state is Connected or Disconnecting
        /// </summary>
        public HandlerConnectionState _connectionState { get; private set; } = HandlerConnectionState.Disconnected;

        /// <summary>
        /// set EnableAudio = true to make the audio play from HL2 (rather than the PC)
        /// port 8265 is the port Holographic Remoting player app on.
        /// </summary>
        private RemotingConnectConfiguration remotingConfiguration = new()
        {
            RemoteHostName = "192.168.0.103",
            RemotePort = 8265,
            EnableAudio = true,
            MaxBitrateKbps = 20000,
        };

        [SerializeField, Tooltip("Is invoked when connected to Hololens")]
        public UnityEvent onConnectedToDevice;

        [SerializeField, Tooltip("Is invoked when disconnected from Hololens")]
        public UnityEvent<DisconnectReason> onDisconnectedFromDevice;

        [SerializeField]
        bool drawGUI = false;

        int preventedCount = 0;

        // connects to port 8265 because HL2 player app listens to this port.
        public void Awake()
        {
            AppRemoting.Connected += onConnected;
            AppRemoting.Disconnecting += onDisconnecting;

            Application.wantsToQuit += SafeQuit;
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += delegate (PlayModeStateChange pmstate)
            {
                if (pmstate == PlayModeStateChange.ExitingPlayMode)
                {
                    RLogger.Log("[HolographicRemotingConnectionHandler] EditorApplication trying to exit playmode");
                    if (!SafeQuit())
                    {
                        RLogger.Log("[HolographicRemotingConnectionHandler] EditorApplication preventing playmode exit");
                        EditorApplication.isPlaying = true;
                    }
                }
            };
#endif
        }

        private ConnectionState? GetAppRemotingConnectionState()
        {
            ConnectionState cs = new ConnectionState();
            DisconnectReason dr = new DisconnectReason();
            if (AppRemoting.TryGetConnectionState(out cs, out dr))
            {
                return cs;
            }
            else
            {
                return null;
            }
        }

        private void Quit()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit(); 
#endif
        }

        private bool SafeQuit()
        {
            if (preventedCount >= 10)
            {
                RLogger.Log("[HolographicRemotingConnectionHandler] QuitCheck allowing quit because it has prevented quit for more than ten times");
                return true;
            }
            RLogger.Log("[HolographicRemotingConnectionHandler] Checking Quit condition");
            if (_connectionState != HandlerConnectionState.Disconnected)
            {
                RLogger.Log("[HolographicRemotingConnectionHandler] wants to quit but is not disconnected, not allowing quit, and trying to disconnect instead.");
                onDisconnectedFromDevice.AddListener(delegate (DisconnectReason dr)
                {
                    RLogger.Log("[HolographicRemotingConnectionHandler] Quitting following a previously rejected quit request.");
                    Quit();
                });
                Disconnect();
                preventedCount += 1;
                return false;
            }
            if (XRGeneralSettings.Instance.Manager.activeLoader != null)
            {
                RLogger.Log("[HolographicRemotingConnectionHandler] wants to quit and is disconnected but XR active loader is not null, not allowing quit.");
                preventedCount += 1;
                return false;
            }
            RLogger.Log("[HolographicRemotingConnectionHandler] allwoing quit.");
            return true;
        }

        public void Connect(string IP)
        {
            remotingConfiguration.RemoteHostName = IP;

            if (!isReachable(remotingConfiguration.RemoteHostName, remotingConfiguration.RemotePort))
            {
                RLogger.Log("[HolographicRemotingConnectionHandler]: The IP address is not reachable, make sure it's entered correclty");
                return;
            }
            if (AppRemoting.IsReadyToStart == false)
            {
                RLogger.Log("[HolographicRemotingConnectionHandler] Error: HolographicRemoting is not ready to start. Check App's XR Settings and try again later.");
                return;
            }
            RLogger.Log("[HolographicRemotingConnectionHandler]: Ready to start, trying to connect");
            AppRemoting.StartConnectingToPlayer(remotingConfiguration);
        }

        private bool isReachable(string ip, int port)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    IAsyncResult connectResult = client.BeginConnect(ip, port, null, null);
                    bool success = connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));

                    if (!success)
                    {
                        return false;
                    }
                    client.EndConnect(connectResult);
                }
                return true;
            }
            catch
            {
                return false;
            }

        }

        public void Disconnect()
        {
            RLogger.Log("[HolographicRemotingConnectionHandler] Disconnect called, XRGeneralSettings.Instance.Manager.activeLoader is null? " + (XRGeneralSettings.Instance.Manager.activeLoader == null).ToString());
            if (GetAppRemotingConnectionState() != ConnectionState.Disconnected)
            {
                RLogger.Log("[HolographicRemotingConnectionHandler] Calling Disconnect on AppRemoting");
                AppRemoting.Disconnect();
            }
            else
            {
                RLogger.Log("[HolographicRemotingConnectionHandler] Disconnect called but we're already disconnected");
            }
        }

        private void onConnected()
        {
            ConnectionState? cs = GetAppRemotingConnectionState();
            RLogger.Log("[HolographicRemotingConnectionHandler] onConnected called" + " XRGeneralSettings.Instance.Manager.activeLoader is null ? " + (XRGeneralSettings.Instance.Manager.activeLoader == null).ToString() + ". Connection State: " + cs.ToString());
            _connectionState = (cs == ConnectionState.Connected) ? HandlerConnectionState.Connected : HandlerConnectionState.Disconnected;
            if (_connectionState == HandlerConnectionState.Connected)
            {
                RLogger.Log("[HolographicRemotingConnectionHandler] invoking onConnectedToDevice");
                onConnectedToDevice?.Invoke();
            }
        }

        private void onDisconnecting(DisconnectReason reason)
        {
            ConnectionState? cs = GetAppRemotingConnectionState();
            RLogger.Log("[HolographicRemotingConnectionHandler] onDisconnected called. Reason:" + reason.ToString() + " XRGeneralSettings.Instance.Manager.activeLoader is null ? " + (XRGeneralSettings.Instance.Manager.activeLoader == null).ToString() + ". Connection State: " + cs.ToString());
            _connectionState = (cs == ConnectionState.Connected) ? HandlerConnectionState.Disconnecting : HandlerConnectionState.Disconnected;
            RLogger.Log("[HolographicRemotingConnectionHandler] OnDisconnected called, _connectionState is now: " + _connectionState.ToString());
            if (_connectionState == HandlerConnectionState.Disconnected)
            {
                RLogger.Log("[HolographicRemotingConnectionHandler] invoking onDisconnectedFromDevice");
                onDisconnectedFromDevice?.Invoke(reason);
            }
        }

        public void OnApplicationQuit()
        {
            RLogger.Log("[HolographicRemotingConnectionHandler] OnApplicationQuit called. " +
                "XRGeneralSettings.Instance.Manager.activeLoader is null ? " + (XRGeneralSettings.Instance.Manager.activeLoader == null).ToString() +
                ", _connectionState = " + _connectionState.ToString());
        }

        public void OnGUI()
        {
            if (drawGUI)
            {
                if (_connectionState == HandlerConnectionState.Disconnected)
                {
                    remotingConfiguration.RemoteHostName = GUI.TextField(new Rect(155, 10, 200, 30), remotingConfiguration.RemoteHostName, 25);
                    if (GUI.Button(new Rect(365, 10, 100, 30), "Connect"))
                    {
                        GUI.Label(new Rect(365, 10, 100, 30), "Connecting...");
                        Connect(remotingConfiguration.RemoteHostName);
                    }
                }
                else
                {
                    GUI.Label(new Rect(215, 10, 200, 30), remotingConfiguration.RemoteHostName);
                    if (GUI.Button(new Rect(430, 10, 100, 30), "Disconnect"))
                    {
                        Disconnect();
                    }

                }
            }
        }

    }
}