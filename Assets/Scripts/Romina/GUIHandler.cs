using com.perceptlab.armultiplayer;
using Microsoft.MixedReality.OpenXR.Remoting;
using UnityEngine;

///< summary >
/// <c>GUIHandler</c>: GUI for the Windows app
/// </summary>
public class GUIHandler : MonoBehaviour
{

    private bool remotingConnected = false;
    private bool aligned = false;
    private int haveRemoting = -1;
    [SerializeField]
    private HolographicRemotingConnectionHandler remotingHandler;
    [SerializeField]
    private AlignTheWorld align;
    
    [SerializeField, Tooltip("Default IP to use to connect with Holographic Remoting. Must have a HolographicRemoteConnect Component to Connect")]
    string IP = "192.168.0.103";
    string disconnectReason = "";


    private void Start()
    {
        if (remotingHandler != null)
        {
            align.enabled = false;
            remotingHandler.onConnectedToDevice.AddListener(delegate () { remotingConnected = true;});
            remotingHandler.onDisconnectedFromDevice.AddListener(delegate (DisconnectReason reason) { disconnectReason = reason.ToString();  remotingConnected = false;});
        }
        else
        {
            haveRemoting = 0;
        }
        align.onDoneAlign.AddListener(()=> { aligned = true; });

    }

    // Start is called before the first frame update
    private void OnGUI()
    {
        if (haveRemoting == -1)
        {
            GetRemotingInput();
            return;
        }
        if (haveRemoting == 1)
        {
            RemotingConnectionInput();
        }
        if (aligned == false)
        {
            Align();
        }
    }

    private void GetRemotingInput()
    {
       
        if (GUI.Button(new Rect(365, 10, 100, 30), "Remoting"))
        {
            haveRemoting = 1; 
            align.enabled = true;
            return;
        }
        if (GUI.Button(new Rect(265, 10, 100, 30), "No Remoting"))
        {
            haveRemoting = 0; 
            align.enabled = true;
            return;
        }

    }

    private void RemotingConnectionInput()
    {
        GUI.Label(new Rect(10, 10, 150, 30), "Player's IP:");
        if (disconnectReason != "")
            GUI.Label(new Rect(10, 40, 400, 30), "Disconnect reason: "+ disconnectReason);
        if (!remotingConnected)
        {
            IP = GUI.TextField(new Rect(155, 10, 200, 30), IP, 25);
            if (GUI.Button(new Rect(365, 10, 100, 30), "Connect"))
            {
                GUI.Label(new Rect(365, 10, 100, 30), "Connecting...");
                remotingHandler.Connect(IP);
            }
        }
        else
        {
            GUI.Label(new Rect(215, 10, 200, 30), IP);
            if (GUI.Button(new Rect(430, 10, 100, 30), "Disconnect"))
            {
                remotingHandler.Disconnect();
            }

        }
    }

    private void Align()
    {
        GUI.Label(new Rect(10, 80, 400, 60), "Align the world. When finished, press 'start' on joystick or 'enter' on keyboard.\nUse a,w,s,d, and arrows on keyboard or the joystick to align." );
    }

}
