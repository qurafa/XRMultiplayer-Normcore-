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
        private bool connected { get; set; } = false;

        // port 8265 is the port Holographic Remoting player app on 
        private RemotingConnectConfiguration remotingConfiguration = new() { RemoteHostName = "192.168.0.103", RemotePort = 8265, MaxBitrateKbps = 20000 };

        [SerializeField, Tooltip("Is invoked when connected to Hololens")]
        public UnityEvent onConnectedToDevice;

        [SerializeField, Tooltip("Is invoked when connected to Hololens")]
        public UnityEvent<DisconnectReason> onDisconnectedFromDevice;

        [SerializeField]
        bool drawGUI = false;

        int preventedCount = 0;

        // connects to port 8265 because HL2 player app listens to this port.

        public void Awake()
        {
            AppRemoting.Connected += onConnected;
            AppRemoting.Disconnecting += onDisconnected;
            Application.wantsToQuit += wantsToQuitCheck;
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += delegate
            {
                if (!wantsToQuitCheck())
                {
                    RLogger.Log("playMode state changed and we don't want to allow quitting, so we prevent it");
                    EditorApplication.isPlaying = true;
                }
            };
#endif
        }

        private bool isDisconnected()
        {
            RLogger.Log("Checking disconnected");
            ConnectionState cs = new ConnectionState();
            DisconnectReason dr = new DisconnectReason();
            if (AppRemoting.TryGetConnectionState(out cs, out dr))
            {
                RLogger.Log("Connection state is: " + cs.ToString());
                if (cs != ConnectionState.Disconnected)
                {
                    RLogger.Log("Returning true");
                    return true;
                }
            }
            else
            {
                RLogger.Log("couldn't get connection state, assuming we're not connected (probably remoting Subsystem is not initialized yet)....");
                return true;
            }
            RLogger.Log("returning false");
            return false;
        }

        private bool wantsToQuitCheck()
        {
            if (preventedCount >= 10)
            {
                RLogger.Log("wantsToQuitCheck allowing quit because has prevented quit for more than ten times");
                return true;
            }
            RLogger.Log("application wants to quit");
            if (!isDisconnected())
            {
                RLogger.Log("wants to quit but is not disconnected, not allowing quit, and trying to disconnect instead.");
                Disconnect();
                preventedCount += 1;
                return false;
            }
            if (XRGeneralSettings.Instance.Manager.activeLoader != null)
            {
                RLogger.Log("wants to quit and is disconnected but XR active loader is not null, not allowing quit.");
                preventedCount += 1;
                return false;
            }
            RLogger.Log("allwoing quit.");
            return true;
        }

        public void Connect(string IP)
        {
            remotingConfiguration.RemoteHostName = IP;

            if (!isReachable(remotingConfiguration.RemoteHostName, remotingConfiguration.RemotePort))
            {
                RLogger.Log("The IP address is not reachable, make sure it's entered correclty");
                return;
            }
            if (AppRemoting.IsReadyToStart == false)
            {
                RLogger.Log("Error: HolographicRemoting is not ready to start. Check App's XR Settings and try again later.");
                return;
            }
            RLogger.Log("HolographicRemoting: Ready to start, trying to connect");
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
            RLogger.Log("HolographicRemoting: Disconnect request, XRGeneralSettings.Instance.Manager.activeLoader is null? " + (XRGeneralSettings.Instance.Manager.activeLoader == null).ToString());
            if (!isDisconnected())
            {
                RLogger.Log("Calling Disconnect on AppRemoting");
                AppRemoting.Disconnect();
            }
            else
            {
                RLogger.Log("Disconnect called but we're already disconnected");
            }
        }

        private void onConnected()
        {
            RLogger.Log("HolographicRemoting: Connected" + " XRGeneralSettings.Instance.Manager.activeLoader is null ? " + (XRGeneralSettings.Instance.Manager.activeLoader == null).ToString());
            connected = true;
            onConnectedToDevice?.Invoke();
        }

        private void onDisconnected(DisconnectReason reason)
        {
            RLogger.Log("HolographicRemoting: Disconnected. Reason:" + reason.ToString() + " XRGeneralSettings.Instance.Manager.activeLoader is null ? " + (XRGeneralSettings.Instance.Manager.activeLoader == null).ToString());
            connected = false;
            ConnectionState cs = new ConnectionState();
            DisconnectReason dr = new DisconnectReason();
            AppRemoting.TryGetConnectionState(out cs, out dr);
            RLogger.Log("I set connected to false, but are we really disconnected? this is the AppRemoting status: " + cs.ToString());
            onDisconnectedFromDevice?.Invoke(reason);
        }

        public void OnDisable()
        {
            RLogger.Log("OnDisable called" + "XRGeneralSettings.Instance.Manager.activeLoader is null ? " + (XRGeneralSettings.Instance.Manager.activeLoader == null).ToString() + ", holographic remoting disconnecting must see Disconnecting in the next line:");
            Disconnect();
        }

        public void OnApplicationQuit()
        {
            RLogger.Log("OnApplicationQuit called" + "XRGeneralSettings.Instance.Manager.activeLoader is null ? " + (XRGeneralSettings.Instance.Manager.activeLoader == null).ToString() + ", holographic remoting disconnecting must see Disconnecting in the next line:");
            Disconnect();
        }

        public void OnApplicationPause()
        {
            RLogger.Log("OnApplicationPause called" + "XRGeneralSettings.Instance.Manager.activeLoader is null ? " + (XRGeneralSettings.Instance.Manager.activeLoader == null).ToString() + ", holographic remoting disconnecting must see Disconnecting in the next line:");
            Disconnect();
        }

        public void OnGUI()
        {
            if (drawGUI)
            {
                if (!connected)
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