using Normal.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;

public class AvatarSyncImpl : MonoBehaviour
{
    [SerializeField] private RealtimeView m_RealtimeView;
    [SerializeField] private AvatarSync m_AvatarSync;
    [SerializeField] private Device m_Device;
    [SerializeField] private Type m_Type;
    [SerializeField] private HandMode m_HandMode;

    private Subsystem m_Subsystem;
    private bool m_Initialized;
    private bool m_ControllerTracking = false;
    private XRNode m_XRNode;
    private XROrigin m_XROrigin;
    private UnityEngine.InputSystem.XR.XRController controller;

    public enum Device { MetaQuest, HoloLens, Other }
    public enum Type { None, Head, LeftHand, RightHand, Other }
    public enum HandMode { Nome, Controller, HandTracking, Both }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void Init()
    {
        if(m_RealtimeView == null) m_RealtimeView = GetComponent<RealtimeView>();
        if(m_AvatarSync == null) m_AvatarSync = GetComponent<AvatarSync>();

        m_XROrigin = FindObjectOfType<XROrigin>();

        m_Initialized = true;
    }

    private void InitController()
    {
        ActionBasedController[] cList = FindObjectsOfType<ActionBasedController>();

        foreach (ActionBasedController c in cList)
        {

        }
    }

    private bool UpdateControllerTracking()
    {
        if (m_HandMode != (HandMode.Controller | HandMode.Both))
            return false;

        controller = null;

        switch (m_Type)
        {
            case Type.LeftHand:
                controller = UnityEngine.InputSystem.InputSystem.GetDevice<UnityEngine.InputSystem.XR.XRController>(UnityEngine.InputSystem.CommonUsages.LeftHand);
                break;
            case Type.RightHand:
                controller = UnityEngine.InputSystem.InputSystem.GetDevice<UnityEngine.InputSystem.XR.XRController>(UnityEngine.InputSystem.CommonUsages.RightHand);
                break;
            default:
                break;
        }

        m_ControllerTracking = controller != null;
        return m_ControllerTracking;
    }

    private void InitHandSubsystem()
    {

    }

    private void OnHandTrackingAcquired()
    {

    }

    private void OnHandTrackingLost()
    {

    }

    private void UpdateHands()
    {

    }

    private void UpdateToNormCore()
    {

    }

    public void UpdateFromNormcore(string netData)
    {

    }
}
