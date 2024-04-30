using com.perceptlab.armultiplayer;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class ControllerFollow : MonoBehaviour
{
    [SerializeField]
    ActionBasedController RightController;
    [SerializeField]
    GameObject ControllerVisual;
    [SerializeField]
    GameObject VirtualVisual;
    [SerializeField]
    GameObject Room;

    enum Mode { SaveInfo, Calibrate };

    [SerializeField, Tooltip("If true, when you press X three times (left controller main), the transform of the visualizer (the same as the controller) will be saved, so you can put the virtual one there for calibration.")]
    Mode mode = Mode.Calibrate;

    InputAction x;
    int counter = 0;
    bool timerRunning = false;
    void Start()
    {
        x = new InputAction("X", binding: "<XRController>{LeftHand}/primaryButton"); //https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@2.0/manual/xr-controller-action-based.html
        x.Enable();
        x.performed += onX;
    }

    void onX(InputAction.CallbackContext context)
    {
        RLogger.Log("X pressed");
        counter += 1;
        if (!timerRunning)
        {
            StartCoroutine(Timer());
        }
        if (counter == 3)
        {
            OnPressedThreeTimes();
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

    void OnPressedThreeTimes()
    {
        RLogger.Log("X pressed 3 times");
        if (mode == Mode.SaveInfo)
        {
            string OBJECT_FILE_PATH = $"{Application.persistentDataPath}/objectsData_{System.DateTime.Now.ToString("yyyy-MM-dd-HH_mm_ss")}.csv";//something to identify the participant
            string txt =
                 $"====================================================\n" +
                 $"= Controller pos:{RightController.transform.position}--euler:{RightController.transform.rotation.eulerAngles}\n" +
                 $"= ControllerVisual pos:{RightController.transform.position}--euler:{RightController.transform.rotation.eulerAngles}\n" +
                 $"= ControllerVisual local pos:{RightController.transform.localPosition}--euler:{RightController.transform.localEulerAngles}\n" +
                 $"= VirtualVisual pos:{RightController.transform.position}--euler:{RightController.transform.rotation.eulerAngles}\n" +
                 $"= VirtualVisual local pos:{RightController.transform.localPosition}--euler:{RightController.transform.localEulerAngles}\n" +
                 $"====================================================\n";
            File.AppendAllText(OBJECT_FILE_PATH, txt);
            RLogger.Log("[ControllerFollow]:\n" + txt);
        }
        if (mode == Mode.Calibrate)
        {
            AlignHelpers.moveDadToMakeChildMatchDestination(VirtualVisual, Room, ControllerVisual.transform.position);
            RLogger.Log($"rotating, virtualvisual is {VirtualVisual.transform.eulerAngles}");
            AlignHelpers.rotateDadtoMakeChildFaceDirection(VirtualVisual, Room, ControllerVisual.transform.eulerAngles);
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (RightController != null)
        {
            if (!ControllerVisual.activeInHierarchy)
            {
                ControllerVisual.SetActive(true);
            }
            ControllerVisual.transform.position = RightController.transform.position;
            ControllerVisual.transform.rotation = RightController.transform.rotation;
        }
        else if (ControllerVisual.activeInHierarchy)
        {
            ControllerVisual.SetActive(false);
        }
    }
}
