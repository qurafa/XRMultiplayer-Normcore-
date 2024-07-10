using com.perceptlab.armultiplayer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ML2InputActionTest : MonoBehaviour
{
    [SerializeField]
    private InputAction pointerPositionInputAction =
        new InputAction(binding: "<HandInteraction>{LeftHand}/pointerPositon",expectedControlType:"Vector3");
    [SerializeField]
    private InputAction graspValueInputAction =
        new InputAction(binding: "<HandInteraction>{LeftHand}/graspValue", expectedControlType: "Axis");

    // Update is called once per frame
    void Update()
    {
        RLogger.Log($"Left Hand Pointer Position: {pointerPositionInputAction.ReadValue<Vector3>()}");

        RLogger.Log($"Left Hand Grasp Value: {graspValueInputAction.ReadValue<float>()}");
    }

    private void OnDestroy()
    {
        graspValueInputAction.Dispose();
        pointerPositionInputAction.Dispose();
    }
}
