using com.perceptlab.armultiplayer;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;



public class FastCalibration : MonoBehaviour
{
    string SETUP_FILE_PATH;

    [SerializeField]
    GameObject RightControllerVisual;
    [SerializeField]
    GameObject LeftControllerVisual;
    [SerializeField]
    GameObject LVirtualVisual;
    [SerializeField]
    GameObject RVirtualVisual;
    [SerializeField]
    GameObject Room;
    [SerializeField]
    bool UsePreviousSetup;

    enum Mode { SingleControllerCalibrate, DoubleControllerCalibrate };

    [SerializeField]
    Mode mode = Mode.DoubleControllerCalibrate;

    InputAction x;
    InputAction y;
    int counter = 0;
    bool timerRunning = false;
    void Start()
    {
        SETUP_FILE_PATH = $"{Application.persistentDataPath}/CalibrationSetup.csv";
        x = new InputAction("X", binding: "<XRController>{LeftHand}/primaryButton"); //https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@2.0/manual/xr-controller-action-based.html
        x.Enable();
        y = new InputAction("Y", binding: "<XRController>{LeftHand}/secondaryButton"); //https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@2.0/manual/xr-controller-action-based.html
        y.Enable();
        x.performed += onX;
        y.performed += onY;
        if (UsePreviousSetup && File.Exists(SETUP_FILE_PATH))
        {
            AlignControllerVisualsBasedOnFileData();
        }
    }

    void onX(InputAction.CallbackContext context)
    {
        RLogger.Log("X pressed");
        if (mode == Mode.SingleControllerCalibrate)
        {
            alignSingle();
        }
        if (mode == Mode.DoubleControllerCalibrate)
        {
            alignDouble();
        }
    }
    void alignSingle()
    {
        AlignHelpers.moveDadToMakeChildMatchDestination(RVirtualVisual, Room, RightControllerVisual.transform.position);
        AlignHelpers.rotateDadtoMakeChildFaceDirection(RVirtualVisual, Room, RightControllerVisual.transform.forward);
    }
    void alignDouble()
    {
        AlignHelpers.moveDadToMakeChildMatchDestination(RVirtualVisual, Room, RightControllerVisual.transform.position);
        Vector3 vv = LVirtualVisual.transform.position - RVirtualVisual.transform.position;
        Vector3 av = LeftControllerVisual.transform.position - RightControllerVisual.transform.position;
        AlignHelpers.rotateDadtoAlignVirtualActualVectorsOnGroundPlane(RVirtualVisual, Room, vv, av);
    }

    private void AlignControllerVisualsBasedOnFileData()
    {
        Dictionary<string, Dictionary<string, Vector3>> data;
        data = ParseInputFile();
        LeftControllerVisual.transform.localPosition = data["LCVisualWithOffset"]["localpos"];
        LeftControllerVisual.transform.localRotation = Quaternion.Euler(data["LCVisualWithOffset"]["localeuler"]);
        RightControllerVisual.transform.localPosition = data["RCVisualWithOffset"]["localpos"];
        RightControllerVisual.transform.localRotation = Quaternion.Euler(data["RCVisualWithOffset"]["localeuler"]);
    }
    private Dictionary<string, Dictionary<string, Vector3>> ParseInputFile()
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
            t.Add("localpos", pos);
            t.Add("localeuler", euler);
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

    void onY(InputAction.CallbackContext context)
    {
        RLogger.Log("Y pressed");
        counter += 1;
        if (!timerRunning)
        {
            StartCoroutine(Timer());
        }
        if (counter == 3)
        {
            OnYPressedThreeTimes();
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
        RightControllerVisual.transform.position = RVirtualVisual.transform.position;
        RightControllerVisual.transform.eulerAngles = RVirtualVisual.transform.eulerAngles;
        LeftControllerVisual.transform.position = LVirtualVisual.transform.position;
        LeftControllerVisual.transform.eulerAngles = LVirtualVisual.transform.eulerAngles;
        File.WriteAllText(SETUP_FILE_PATH, "");
        string txt =
            $"#Start\n" +
            $"#Object, localpos, localeuler\n" +
            $"LCVisualWithOffset\",\"Vector3{LeftControllerVisual.transform.localPosition}\",\"Vector3{LeftControllerVisual.transform.localEulerAngles}\"\n" +
            $"RCVisualWithOffset\",\"Vector3{RightControllerVisual.transform.localPosition}\",\"Vector3{RightControllerVisual.transform.localEulerAngles}\"\n";
        File.AppendAllText(SETUP_FILE_PATH, txt);
        File.AppendAllText(SETUP_FILE_PATH, "#end\n");
    }

    void OnYPressedThreeTimes()
    {
        RLogger.Log("Y pressed 3 times");
        saveTransformInfo();
    }
}
