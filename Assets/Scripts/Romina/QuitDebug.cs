using com.perceptlab.armultiplayer;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SubsystemsImplementation;
using UnityEngine.XR;

public class QuitDebug : MonoBehaviour
{
    private void Awake()
    {
        LogSubsystems();
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void LogSubsystems()
    {
        List<XRInputSubsystem> allXRInputSubsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(allXRInputSubsystems);
        RLogger.Log(">>>>>>>>>>>>>All XRInputSubsystems are:");
        foreach (XRInputSubsystem subsystem in allXRInputSubsystems)
        {
            RLogger.Log("XRInputSubsystem: " + subsystem.GetType());
        }
        RLogger.Log("<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");
        List<SubsystemWithProvider> all = new List<SubsystemWithProvider>();
        SubsystemManager.GetSubsystems(all);
        RLogger.Log(">>>>>>>>>>>>>All SubsystemWithProviders are:");
        foreach (SubsystemWithProvider subsystem in all)
        {
            RLogger.Log("Subsystem: " + subsystem.GetType());
        }
        RLogger.Log("<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");
    }
}
