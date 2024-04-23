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
    private int haveRemoting = -1; //-1: not set, 0: no remoting, 1: remoting
    [SerializeField]
    private HolographicRemotingConnectionHandler remotingHandler;
    [SerializeField]
    private GameObject alignGameObject;
    AlignTheWorld _align;


    //string IP = "";
    //string disconnectReason = "";

    private void Awake()
    {
        _align.onDoneAlign.AddListener(() => { aligned = true; alignGameObject.SetActive(false); });
        _align.drawGUI = true;
        alignGameObject.SetActive(false);
    }

    // This method will be called when the component is validated or applied
    private void OnValidate()
    {
        // Check if the targetGameObject is not null and doesn't have the required component
        if (alignGameObject == null || !alignGameObject.TryGetComponent<AlignTheWorld>(out _align))
        {
            Debug.LogError("[GUIHandler]: The align GameObject must have the AlignTheWorld component.");
        }
    }

    private void Start()
    {
        if (remotingHandler != null)
        {
            remotingHandler.onConnectedToDevice.AddListener(delegate () { remotingConnected = true; });
            remotingHandler.onDisconnectedFromDevice.AddListener(delegate (DisconnectReason reason) { remotingConnected = false; });
        }
        else
        {
            haveRemoting = 0;
        }
    }

    // Start is called before the first frame update
    private void OnGUI()
    {
        if (haveRemoting == -1)
        {
            GetRemotingInput();
            return;
        }
        //if (haveRemoting == 1)
        //{
        //    RemotingConnectionInput();
        //}
        if (!aligned && !_align.isActiveAndEnabled && (haveRemoting == 0 || (haveRemoting == 1 && remotingConnected)))
        {
            RLogger.Log("setting align the world active");
            alignGameObject.SetActive(true);
        }
        if (_align == null && !aligned)
        {
            RLogger.Log("[GUIHandler] Error: haven't aligned but align is null");
        }
    }

    private void GetRemotingInput()
    {

        if (GUI.Button(new Rect(365, 10, 100, 30), "Remoting"))
        {
            haveRemoting = 1;
            remotingHandler.drawGUI = true;
            return;
        }
        if (GUI.Button(new Rect(265, 10, 100, 30), "No Remoting"))
        {
            haveRemoting = 0;
            remotingHandler.enabled = false;
            return;
        }

    }

    //private void RemotingConnectionInput()
    //{
    //    GUI.Label(new Rect(10, 10, 150, 30), "Player's IP:");
    //    if (!remotingConnected)
    //    {
    //        IP = GUI.TextField(new Rect(155, 10, 200, 30), IP, 25);
    //        if (GUI.Button(new Rect(365, 10, 100, 30), "Connect"))
    //        {
    //            GUI.Label(new Rect(365, 10, 100, 30), "Connecting...");
    //            remotingHandler.Connect(IP);
    //        }
    //    }
    //    else
    //    {
    //        GUI.Label(new Rect(215, 10, 200, 30), IP);
    //        if (GUI.Button(new Rect(430, 10, 100, 30), "Disconnect"))
    //        {
    //            remotingHandler.Disconnect();
    //        }
    //    }
    //}

}
