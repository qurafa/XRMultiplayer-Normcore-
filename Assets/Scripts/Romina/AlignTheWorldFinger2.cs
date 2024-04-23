using com.perceptlab.armultiplayer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;

public class AlignTheWorldFinger2 : AlignTheWorld
{
    private ARSession _ARSession;

    [SerializeField, Tooltip("The top-right corner of the drawn GUI on the PC app")]
    private Vector2 GUITopRight = Vector2.zero;

    [Header("XR Rig and Player Set up")]
    [SerializeField, Tooltip("The game object in hierarchy that moves with locomotion; \"Camera Offset\" in MRTK XR Rig and \"XR Origin\" in VR")]
    GameObject player;
    [SerializeField, Tooltip("The camera")]
    GameObject player_camera;
    [SerializeField, Tooltip("When reset is pressed the camera's position will be set to this transform's position, if not provided, the starting position will be used")]
    private Transform defaultCameraTransform;

    [Header("Finger and TableCorners")]
    [SerializeField, Tooltip("The Left Finger GameObject, must be a child of player")]
    GameObject LFinger;
    [SerializeField, Tooltip("The Right Finger GameObject, must be a child of player")]
    GameObject RFinger;
    [SerializeField, Tooltip("The Left TableCorner GameObject")]
    GameObject LTableCorner;
    [SerializeField, Tooltip("The Right TableCorner GameObject")]
    GameObject RTableCorner;

    [Header("Input")]
    // assign the actions asset to this field in the inspector:
    [SerializeField, Tooltip("The action asset buttons: Left, Right, Reset, and DoneAlign")]
    private InputActionAsset actions;
    // assign the actions asset to this field in the inspector:
    [SerializeField, Tooltip("The name of the actionMap in the Actions asset that should be used")]
    private string myActionMaps = "AlignWithTwoFingers";

    private Vector3 defaultCameraPosition;

    private void Awake()
    {
        RLogger.Log("FingerAlign the world is awake");
        actions.FindActionMap(myActionMaps).FindAction("DoneAlign").performed += OnDoneAlign;
        actions.FindActionMap(myActionMaps).FindAction("Left").performed += OnLeft;
        actions.FindActionMap(myActionMaps).FindAction("Right").performed += OnRight;
        defaultCameraPosition = (defaultCameraTransform == null) ? player_camera.transform.position : defaultCameraTransform.position;
    }

    private void Start()
    {
        RLogger.Log("FingerAlign the world start called");
        _ARSession = GetComponent<ARSession>();
        if (_ARSession != null)
        {
            _ARSession.enabled = true;
        }
    }

    private void DisableARandRevertChanges()
    {
        if (_ARSession != null)
        {
            _ARSession.enabled = false;
            // revert the changes caused by _ARSession.MatchFrameRate = True
            Application.targetFrameRate = 0;
            QualitySettings.vSyncCount = 1;
        }
    }

    private void DisableAR()
    {
        if (_ARSession != null)
        {
            _ARSession.enabled = false;
        }
    }

    private void EnableAR()
    {
        if (_ARSession != null)
        {
            _ARSession.enabled = true;
        }

    }

    private void OnDoneAlign(InputAction.CallbackContext context)
    {
        //if (done)
        //{
        //    RLogger.Log("DoneAlign was pressed, but already done");
        //    return;
        //}
        RLogger.Log("DoneAlign was pressed, player position is: " + player.transform.position);
        //done = true;
        onDoneAlign?.Invoke();
        //actions.FindActionMap("Align").Disable();
    }

    private void OnLeft(InputAction.CallbackContext context)
    {
        RLogger.Log($"Set was pressed, Finger pose: {LFinger.transform.position}, tablecormer pose: {LTableCorner.transform.position}.");
        AlignHelpers.moveDadToMakeChildMatchDestination(LFinger, player, LTableCorner.transform.position);
        RLogger.Log($"-------Moved, Finger pose: {LFinger.transform.position}, tablecormer pose: {LTableCorner.transform.position}.");

    }

    private void OnRight(InputAction.CallbackContext context)
    {
        RLogger.Log($"Set was pressed, Right Finger pose: {RFinger.transform.position}, Rtablecormer pose: {RTableCorner.transform.position}.");
        Vector3 actualTableEdge = RFinger.transform.position - LTableCorner.transform.position; actualTableEdge.y = 0;
        Vector3 virtualTableEdge = RTableCorner.transform.position - LTableCorner.transform.position; virtualTableEdge.y = 0;
        float angle = Vector3.SignedAngle(actualTableEdge, virtualTableEdge, Vector3.up);
        player.transform.RotateAround(LTableCorner.transform.position, Vector3.up, angle);
        RLogger.Log($"-------Rotated, Right Finger pose: {RFinger.transform.position}, Rtablecormer pose: {RTableCorner.transform.position}.");
    }

    void OnEnable()
    {
        RLogger.Log("Align the world is enabled");
        actions.FindActionMap(myActionMaps).Enable();
        EnableAR();
    }
    void OnDisable()
    {
        RLogger.Log("Align the world is disabled");
        actions.FindActionMap(myActionMaps).Disable();
        DisableAR();
    }

    private void OnDestroy()
    {
        RLogger.Log("Align the world is distroyed");
        actions.FindActionMap(myActionMaps).Disable();
        DisableARandRevertChanges();
    }

    private void OnGUI()
    {
        if (drawGUI)
        {
            GUI.Label(new Rect(GUITopRight.x + 10, GUITopRight.y + 100, 400, 90),
                "Put your left finger on the left corner of the table and press L.\n" +
                "Then, put your right finger on the right corner of the table and press R.\n" +
                "When grid matches the table press Enter to finish.");
        }
    }

    /*********************************************
    * list of all required defined buttons and axes:
    * L : button      [matches the left corner of the table to your left finger]
    * R : button      [matches the right corner of the table to your right finger]
    * DoneAlign: button
    *********************************************/
}
