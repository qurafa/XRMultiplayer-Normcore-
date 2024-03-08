/**
 * Romina:
 * Resource: https://learn.microsoft.com/en-us/dotnet/api/microsoft.mixedreality.openxr.remoting.appremoting?view=mixedreality-openxr-plugin-1.10
 * I might have over-complicated things with using the AppRemoting.Connected and AppRemoting.Disconnecting delegates, 
 * but in long run it might avoid having connection errors and not knowing
 * **/

using Microsoft.MixedReality.OpenXR.Remoting;
using System.Net.Sockets;
using System;
using UnityEngine;
using UnityEngine.Events;
namespace com.perceptlab.armultiplayer
{
    public class HolographicRemotingConnectionHandler : MonoBehaviour
    {
        private bool connected { get; set; } = false;

        private RemotingConnectConfiguration remotingConfiguration = new() { RemoteHostName = "192.168.0.103", RemotePort = 8265, MaxBitrateKbps = 20000 };

        [SerializeField, Tooltip("Is invoked when connected to Hololens")]
        public UnityEvent onConnectedToDevice;

        [SerializeField, Tooltip("Is invoked when connected to Hololens")]
        public UnityEvent<DisconnectReason> onDisconnectedFromDevice;

        [SerializeField]
        bool drawGUI = false;

        // connects to port 8265 because HL2 player app listens to this port.
        public void Connect(string IP)
        {
            remotingConfiguration.RemoteHostName = IP;

            AppRemoting.Connected += onConnected;
            AppRemoting.Disconnecting += onDisconnected;

            if (!isReachable(remotingConfiguration.RemoteHostName, remotingConfiguration.RemotePort))
            {
                RLogger.Log("The ip is not reachable, make sure it's entered correclty");
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
            } catch 
            {
                return false;
            }

        }

        public void Disconnect()
        {
            RLogger.Log("HolographicRemoting: Disconnect request");
            AppRemoting.Disconnect();
        }

        private void onConnected()
        {
            RLogger.Log("HolographicRemoting: Connected");
            connected = true;
            onConnectedToDevice?.Invoke();
        }

        private void onDisconnected(DisconnectReason reason)
        {
            RLogger.Log("HolographicRemoting: Disconnected");
            if (reason != DisconnectReason.DisconnectRequest)
            {
                RLogger.Log("HolographicRemoting: unexpected disconnect, reason: " + reason.ToString());
            }
            connected = false;
            onDisconnectedFromDevice?.Invoke(reason);
        }

        public void OnDisable() 
        {
            RLogger.Log("OnDisable called, holographic remoting disconnecting must see Disconnecting in the next line:");
            if (connected)
            {
                RLogger.Log("Disconnecting");
                AppRemoting.Disconnect();
            }
        }

        public void OnApplicationQuit()
        {
            RLogger.Log("Application quit called, holographic remoting disconnecting must see Disconnecting in the next line:");
            if (connected)
            {
                RLogger.Log("Disconnecting");
                AppRemoting.Disconnect();
            }
        }

        public void OnApplicationPause()
        {
            RLogger.Log("Application pause called, holographic remoting disconnecting must see Disconnecting in the next line:");
            if (connected)
            {
                RLogger.Log("Disconnecting"); 
                AppRemoting.Disconnect();
            }
        }

        public void OnGUI()
        {
            if (drawGUI) {
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