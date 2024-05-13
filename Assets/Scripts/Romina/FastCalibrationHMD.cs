using com.perceptlab.armultiplayer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;



public class FastCalibrationHMD : MonoBehaviour
{
    enum Mode { Setup, Calibration };
    enum MovingObjectForSetup { Player, HMDMarker };

    string SETUP_FILE_PATH;

    [Header("Required GameObjects")]
    [SerializeField, Tooltip("Must be a under the MainCamera in the hierarchy.")]
    GameObject HMDMarker;
    [SerializeField]
    GameObject FixedHMDMarkerPosition;
    [SerializeField]
    GameObject FixedHMDPosition;
    [SerializeField]
    GameObject MainCamera;
    [SerializeField, Tooltip("Set to the heighest GameObject in the hierarchy that contains the MainCamera")]
    GameObject Player;

    [Header("Settings")]
    [SerializeField]
    bool UsePreviousSetup;
    [SerializeField, Tooltip("In the setup mode, you will be able to adjust the position of the HMD visual related to the HMD.")]
    Mode mode = Mode.Calibration;
    [SerializeField]
    UnityEvent DoneAlign;
    [SerializeField]
    TMPro.TextMeshPro WarningText;



    [Header("Movement Speed for Setup")]
    [SerializeField]
    private float move_speed = 0.03f;
    [SerializeField]
    private float rotatoin_speed = 1f;


    [Header("Input")]
    // assign the actions asset to this field in the inspector:
    [SerializeField, Tooltip("The action asset with the following Action Maps: MovingActions, HMDSetup, HMDCalibrate")]
    private InputActionAsset Actions;
    [SerializeField, Tooltip("The name of the MovingActions ActionMap in the Actions asset. It must have \"MoveX\", \"MoveY\", \"MoveZ\", and \"RotateY\" 1D axis Input Actions. Default is \"MovingActions\".")]
    private string MovingActionsName = "MovingActions";
    [SerializeField, Tooltip("The name of the HMDCalibrate ActionMap in the Actions asset. It must have \"ToggleTheMovingObject\" and \"SaveSettings\" Button Input Actions. Default is \"HMDSetup\".")]
    private string SetupActionsName = "HMDSetup";
    [SerializeField, Tooltip("The name of the HMDSetup ActionMap in the Actions asset. It must have \"Calibrate\" and \"Done\" Button Input Actions. Default is \"HMDCalibrate\".")]
    private string CalibrateActionsName = "HMDCalibrate";
    [SerializeField, Tooltip("Button InputAction to toggle mode between Calibrate and Setup. Default is the 'Y' button")]
    private InputAction ToggleMode;

    private InputAction moveX;
    private InputAction moveY;
    private InputAction moveZ;
    private InputAction rotateY;

    MovingObjectForSetup movingObject = MovingObjectForSetup.Player;

    int counter = 0;
    bool timerRunning = false;

    private void Awake()
    {
        SETUP_FILE_PATH = $"{Application.persistentDataPath}/CalibrationSetup.csv";
    }

    private void OnEnable()
    {
        InitializeAndMapInputActions();
        setInputActionsForMode();
        if (UsePreviousSetup && File.Exists(SETUP_FILE_PATH))
        {
            positionVisualsBasedOnFileData();
        }
        setWarning();
    }

    private void OnDisable()
    {
        DisableAllActionMaps();
        ToggleMode.Disable();
    }

    private void DisableAllActionMaps()
    {
        Actions.FindActionMap(MovingActionsName).Disable();
        Actions.FindActionMap(SetupActionsName).Disable();
        Actions.FindActionMap(CalibrateActionsName).Disable();
    }


    void InitializeAndMapInputActions()
    {
        RLogger.Log("Initializing Input Actions");
        if (ToggleMode.bindings.Count == 0)
        {
            ToggleMode = new InputAction("B", binding: "<XRController>{RightHand}/secondaryButton"); //https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@2.0/manual/xr-controller-action-based.html
        }
        ToggleMode.performed += toggleMode;
        ToggleMode.Enable();
        moveX = Actions.FindActionMap(MovingActionsName).FindAction("MoveX");
        moveY = Actions.FindActionMap(MovingActionsName).FindAction("MoveY");
        moveZ = Actions.FindActionMap(MovingActionsName).FindAction("MoveZ");
        rotateY = Actions.FindActionMap(MovingActionsName).FindAction("RotateY");

        Actions.FindActionMap(SetupActionsName).FindAction("ToggleTheMovingObject").performed += toggleTheMovingObject;
        Actions.FindActionMap(SetupActionsName).FindAction("SaveSettings").performed += onSaveSettings;
        Actions.FindActionMap(CalibrateActionsName).FindAction("Calibrate").performed += onAlign;
        Actions.FindActionMap(CalibrateActionsName).FindAction("Done").performed += onDoneAlign;

    }

    void toggleTheMovingObject(InputAction.CallbackContext callbackContext)
    {
        RLogger.Log("toggleTheMovingObject pressed");
        // Implemented this way, so that if we add more movingObjects, this fuction remains usable
        movingObject = (MovingObjectForSetup)((1 + (int)movingObject) % Enum.GetNames(typeof(MovingObjectForSetup)).Length);
    }

    void toggleMode(InputAction.CallbackContext callbackContext)
    {
        RLogger.Log("toggleMode pressed");
        // Implemented this way, so that if we add more Modes, this fuction remains usable
        mode = (Mode)((1 + (int)mode) % Enum.GetNames(typeof(Mode)).Length);
        setInputActionsForMode();
        setWarning();
    }

    void setInputActionsForMode()
    {
        DisableAllActionMaps();
        switch (mode)
        {
            case Mode.Calibration:
                Actions.FindActionMap(CalibrateActionsName).Enable();
                break;
            case Mode.Setup:
                Actions.FindActionMap(SetupActionsName).Enable();
                Actions.FindActionMap(MovingActionsName).Enable();
                break;
        }
    }

    private void Update()
    {
        if (mode == Mode.Setup)
        {
            if (movingObject == MovingObjectForSetup.Player)
            {
                movePlayer();
            }
            else
            {
                moveObject(HMDMarker);
            }
        }
    }

    void movePlayer()
    {
        float movex = moveX.ReadValue<float>();
        float movez = -moveZ.ReadValue<float>();
        float movey = -moveY.ReadValue<float>();
        float rotatey = rotateY.ReadValue<float>();

        //Vector3 player_forward = player_camera.transform.forward; player_forward.y = 0f; player_forward = Vector3.Normalize(player_forward);
        //Vector3 player_right = player_camera.transform.right; player_right.y = 0f; player_right = Vector3.Normalize(player_right);
        //Vector3 translate = - player_forward*movez + player_right*movex - Vector3.up*movey;

        Vector3 translate = -Vector3.right * movez - Vector3.forward * movex - Vector3.up * movey;

        Player.transform.Translate(-1f * translate * move_speed, Space.World);

        // if we assume objects are rendered straight up, which we did!, we only need to rotate the room about the world y axis to adjust (we have assuemd x and z axes rotations are 0)
        Player.transform.RotateAround(Player.transform.position, Vector3.up, -1f * rotatey * rotatoin_speed);
    }

    void moveObject(GameObject obj)
    {
        float movex = moveX.ReadValue<float>();
        float movez = -moveZ.ReadValue<float>();
        float movey = -moveY.ReadValue<float>();

        //Vector3 player_forward = player_camera.transform.forward; player_forward.y = 0f; player_forward = Vector3.Normalize(player_forward);
        //Vector3 player_right = player_camera.transform.right; player_right.y = 0f; player_right = Vector3.Normalize(player_right);
        //Vector3 translate = - player_forward*movez + player_right*movex - Vector3.up*movey;

        Vector3 translate = Vector3.right * movez + Vector3.forward * movex + Vector3.up * movey;

        obj.transform.Translate(-1f * translate * move_speed, Space.World);
    }

    void setWarning()
    {
        if (mode == Mode.Setup)
        {
            WarningText.enabled = true;
        }
        else
        {
            WarningText.enabled = false;
        }
    }

    void onAlign(InputAction.CallbackContext context)
    {
        AlignHelpers.moveDadToMakeChildMatchDestination(MainCamera, Player, FixedHMDPosition.transform.position);
        Vector3 vv = HMDMarker.transform.position - MainCamera.transform.position;
        Vector3 av = FixedHMDMarkerPosition.transform.position - FixedHMDPosition.transform.position;
        AlignHelpers.rotateDadtoAlignVirtualActualVectorsOnGroundPlane(MainCamera, Player, vv, av);
    }

    void onDoneAlign(InputAction.CallbackContext context)
    {
        DoneAlign?.Invoke();
    }

    private void positionVisualsBasedOnFileData()
    {
        Dictionary<string, Dictionary<string, Vector3>> data;
        data = parseInputFile();

        Dictionary<string, GameObject> map = new Dictionary<string, GameObject>();
        map["FixedHMDMarkerPosition"] = FixedHMDMarkerPosition;
        map["FixedHMDPosition"] = FixedHMDPosition;
        map["HMDMarker"] = HMDMarker;

        foreach (string objectName in new string[] { "FixedHMDMarkerPosition", "FixedHMDPosition", "HMDMarker" })
        {
            if (data.TryGetValue(objectName, out Dictionary<string, Vector3> transformInfo))
            {
                map[objectName].transform.position = transformInfo["pos"];
                map[objectName].transform.rotation = Quaternion.Euler(transformInfo["euler"]);
            }
        }
    }

    private Dictionary<string, Dictionary<string, Vector3>> parseInputFile()
    {
        Dictionary<string, Dictionary<string, Vector3>> data = new Dictionary<string, Dictionary<string, Vector3>>();
        RLogger.Log("reading file for calibration from " + SETUP_FILE_PATH);
        foreach (string s in File.ReadLines(SETUP_FILE_PATH))
        {
            if (s[0] == '#')
            {
                continue;
            }
            RLogger.Log("line is " + s);
            string[] ls = s.Split("\",\"");
            char[] cs = { ' ', '\"' };
            string key = ls[0].Trim(cs);
            Vector3 pos = ParseVector3(ls[1].Trim(cs));
            Vector3 euler = ParseVector3(ls[2].Trim(cs));
            Dictionary<string, Vector3> t = new Dictionary<string, Vector3>();
            t.Add("pos", pos);
            t.Add("euler", euler);
            data.Add(key, t);
        }
        RLogger.Log($"data is {data}");
        return data;
    }
    private Vector3 ParseVector3(string vectorString)
    {
        vectorString = vectorString.Trim('"'); // Remove surrounding quotes
        vectorString = vectorString.Replace("Vector3(", "").Replace(")", "");
        RLogger.Log("vector string is " + vectorString);
        string[] values = vectorString.Split(',');
        float x = float.Parse(values[0]);
        float y = float.Parse(values[1]);
        float z = float.Parse(values[2]);
        return new Vector3(x, y, z);
    }

    void onSaveSettings(InputAction.CallbackContext context)
    {
        RLogger.Log("SaveSettings pressed once");
        counter += 1;
        if (!timerRunning)
        {
            StartCoroutine(Timer());
        }
        if (counter == 3)
        {
            onSaveSettingsThreeTimes();
            counter = 0;
        }
    }
    IEnumerator Timer()
    {
        timerRunning = true;
        yield return new WaitForSeconds(1);
        counter = 0;
        timerRunning = false;
    }

    void saveTransformInfo()
    {
        RLogger.Log(
            $"[FastCalibrationHMD] -- " +
            $"In the previous settings saved, Fixed HMD Marker Position was {FixedHMDMarkerPosition.transform.position}; " +
            $"it is now {HMDMarker.transform.position}.\n" +
            $"The difference is {(FixedHMDMarkerPosition.transform.position - HMDMarker.transform.position).magnitude}" +
            $"In the previous settings saved, Fixed HMD Position was {FixedHMDPosition.transform.position} " +
            $"it is now {MainCamera.transform.position}.\n" +
            $"The difference is {(FixedHMDPosition.transform.position - MainCamera.transform.position).magnitude}"
            );
        FixedHMDMarkerPosition.transform.position = HMDMarker.transform.position;
        FixedHMDMarkerPosition.transform.eulerAngles = HMDMarker.transform.eulerAngles;
        FixedHMDPosition.transform.position = MainCamera.transform.position;
        FixedHMDPosition.transform.eulerAngles = MainCamera.transform.eulerAngles;
        File.WriteAllText(SETUP_FILE_PATH, "");
        string txt =
            $"#Start\n" +
            $"#Object, pos, euler\n" +
            $"\"HMDMarker\",\"Vector3{HMDMarker.transform.localPosition}\",\"Vector3{HMDMarker.transform.localEulerAngles}\"\n" +
            $"#Object, pos, euler\n" +
            $"\"FixedHMDMarkerPosition\",\"Vector3{FixedHMDMarkerPosition.transform.position}\",\"Vector3{FixedHMDMarkerPosition.transform.eulerAngles}\"\n" +
            $"\"FixedHMDPosition\",\"Vector3{FixedHMDPosition.transform.position}\",\"Vector3{FixedHMDPosition.transform.eulerAngles}\"\n";
        File.AppendAllText(SETUP_FILE_PATH, txt);
        File.AppendAllText(SETUP_FILE_PATH, "#End\n");
    }

    void onSaveSettingsThreeTimes()
    {
        RLogger.Log("saveSettings pressed 3 times");
        saveTransformInfo();
    }

    public void distroyVisuals()
    {
        GameObject.Destroy(HMDMarker);
        GameObject.Destroy(FixedHMDMarkerPosition);
        GameObject.Destroy(FixedHMDPosition);
    }
}
