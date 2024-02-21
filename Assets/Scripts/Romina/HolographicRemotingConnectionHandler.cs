/**
 * Romina:
 * Resource: https://learn.microsoft.com/en-us/dotnet/api/microsoft.mixedreality.openxr.remoting.appremoting?view=mixedreality-openxr-plugin-1.10
 * I might have over-complicated things with using the AppRemoting.Connected and AppRemoting.Disconnecting delegates, 
 * but in long run it might avoid having connection errors and not knowing
 * **/

using Microsoft.MixedReality.OpenXR.Remoting;
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

        // connects to port 8265 because HL2 player app listens to this port.
        public void BlockingConnect(string IP)
        {
            remotingConfiguration.RemoteHostName = IP;

            AppRemoting.Connected += onConnected;
            AppRemoting.Disconnecting += onDisconnected;

            RLogger.Log("HolographicRemoting: Blocking connect started");
            while (AppRemoting.IsReadyToStart == false) { RLogger.Log("Waiting for HolographicRemoting to be ready to start"); }
            RLogger.Log("HolographicRemoting: Ready to start, trying to connect");
            AppRemoting.StartConnectingToPlayer(remotingConfiguration);
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
            RLogger.Log("OnDisable called, holographic remoting disconnecting");
            if (connected) 
                AppRemoting.Disconnect();
        }

        public void OnApplicationQuit()
        {
            RLogger.Log("Application quit called, holographic remoting disconnecting");
            if (connected)
                AppRemoting.Disconnect();
        }

        
    }
}