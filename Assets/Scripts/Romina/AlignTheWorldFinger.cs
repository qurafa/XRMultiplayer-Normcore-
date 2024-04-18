using com.perceptlab.armultiplayer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;

public class AlignTheWorldFinger : AlignTheWorld
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

    [Header("Finger and TableCorner")]
    [SerializeField, Tooltip("The Finger GameObject, must be a child of player")]
    GameObject Finger;
    [SerializeField, Tooltip("The TableCorner GameObject")]
    GameObject TableCorner;

    [Header("Input")]
    // assign the actions asset to this field in the inspector:
    [SerializeField, Tooltip("The action asset with 1D axes: MoveX, MoveY, MoveZ, and RotateY; and buttons: Reset, PauseUnpause, and DoneAlign.")]
    private InputActionAsset actions;
    // assign the actions asset to this field in the inspector:
    [SerializeField, Tooltip("The name of the actionMap in the Actions asset that should be used")]
    private string myActionMaps = "AlignWithFinger";

    private Vector3 defaultCameraPosition;

    private void Awake()
    {
        RLogger.Log("FingerAlign the world is awake");
        actions.FindActionMap(myActionMaps).FindAction("DoneAlign").performed += OnDoneAlign;
        actions.FindActionMap(myActionMaps).FindAction("Reset").performed += OnReset;
        actions.FindActionMap(myActionMaps).FindAction("Set").performed += OnSet;
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

    private void OnReset(InputAction.CallbackContext context)
    {
        RLogger.Log("Reset was pressed");
        moveCameraToDestination(defaultCameraPosition);
        makeCameraFaceDirection(Vector3.right);
    }

    private void OnSet(InputAction.CallbackContext context)
    {
        RLogger.Log($"Set was pressed, Finger forward: {Finger.transform.forward}, tablecormer forward: {TableCorner.transform.forward}.");
        float angle = Vector3.SignedAngle(Finger.transform.forward, TableCorner.transform.forward, Vector3.up);
        player.transform.RotateAround(player_camera.transform.position, Vector3.up, angle);
        RLogger.Log($"-------Rotated, Finger forward: {Finger.transform.forward}, tablecormer forward: {TableCorner.transform.forward}.");

        RLogger.Log($"Set was pressed, Finger pose: {Finger.transform.position}, tablecormer pose: {TableCorner.transform.position}.");
        AlignHelpers.moveDadToMakeChildMatchDestination(Finger, player, TableCorner.transform.position);
        RLogger.Log($"-------Moved, Finger pose: {Finger.transform.position}, tablecormer pose: {TableCorner.transform.position}.");

    }

    private void moveCameraToDestination(Vector3 destination)
    {
        //Vector3 diff = player_camera.transform.position - player.transform.position;
        //player.transform.position = destination + diff;
        AlignHelpers.moveDadToMakeChildMatchDestination(player_camera, player, destination);
    }

    private void makeCameraFaceDirection(Vector3 direction)
    {
        //direction.y = 0; direction.Normalize();
        //Vector3 cameraFacing = player_camera.transform.forward; cameraFacing.y = 0; cameraFacing.Normalize();
        //float angle = Vector3.SignedAngle(cameraFacing, direction, Vector3.up);
        //player.transform.RotateAround(player_camera.transform.position, Vector3.up, angle);
        AlignHelpers.rotateDadtoMakeChildFaceDirection(player_camera, player, direction);
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
                "Align the left finger's object with the left corner of the table.\n" +
                "Press 's' on the keyboard or X on joystick to move the blue grid, repeat untill the grid looks good.\n" +
                "When grid matches the table press Enter to finish.");
        }
    }

    /*********************************************
    * list of all required defined buttons and axes:
    * Set : button      [moves the table to your finger]
    * DoneAlign: button
    * Reset: button 
    *********************************************/
}
