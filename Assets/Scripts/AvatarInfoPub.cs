using MixedReality.Toolkit;
using MixedReality.Toolkit.Subsystems;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using Handedness = UnityEngine.XR.Hands.Handedness;

public class AvatarInfoPub : MonoBehaviour
{
    [SerializeField] private int m_PlayerID = -1;
    [SerializeField] private DataManager m_DataManager;
    [Header("ROOTS")]
    [SerializeField] private Transform m_HeadRoot;
    [SerializeField] private Transform m_LeftControllerRoot;
    [SerializeField] private Transform m_RightControllerRoot;
    [SerializeField] private Transform m_CameraOffset;
    [Header("ENUMS")]
    [SerializeField] private Device m_Device;

    /*    [SerializeField] private Type m_Type;
        [SerializeField] private HandMode m_HandMode;*/

    //Devices and Subsystems
    private InputDevice _leftControllerInput;
    private InputDevice _rightControllerInput;
    private XRHandSubsystem _questHandSubsystem;
    private HandsAggregatorSubsystem _holoHandSubsystem;
    private List<XRHandSubsystem> _subsystems = new();

    //Bool checks
    private bool _localRootInit = false;
    private bool _controllerInit = false;
    private bool _handSubsysInit = false;

    //private bool _controllerTracking = false;
    private bool _leftHandTracking = false;
    private bool _rightHandTracking = false;

    //Transforms
    //private Transform _localHeadRoot;
    //private Transform _localLeftControllerRoot;
    //private Transform _localRightControllerRoot;
    //private Transform _cameraOffset;
    //private Transform[] _joints = new Transform[26];

    //XR
    private XRHand _leftXRHand;
    private XRHand _rightXRHand;
    private XRNode _leftXRNode;
    private XRNode _rightXRNode;
    private Handedness _leftHandedness;
    private Handedness _rightHandedness;

    private static readonly string DEFAULTSEND = "0|";

    //Enums
    public enum Device { MetaQuest, HoloLens, Other }
    public enum Type { Head, LeftHand, RightHand, Other }
    public enum HandMode { None, Controller, HandTracking, Both }

    public event EventHandler<AvatarInfoEventArgs>
        OnPublishHeadData,
        OnPublishLeftHandControllerData,
        OnPublishRightHandControllerData = delegate {};

    // Update is called once per frame
    void Update()
    {
        UpdateCall();
    }

    //removed cuz it might be making it a bit laggy calling the same thing multiple times in one frame
    //will test and find out...same for LateUpdate()
    void FixedUpdate()
    {
        //UpdateCall();
    }

    void LateUpdate()
    {
        //UpdateCall();
    }

    private void UpdateCall()
    {
        Raise();

        LocalRootInit();
        InitController();
        InitHandSubsystem();
        //ControllerTracking();
        //HandTracking();
    }

    public void SetPlayerID(int playerID)
    {
        m_PlayerID = playerID;
    }

    private void LocalRootInit()
    {
/*        if (_localHeadRoot != null && _cameraOffset != null) return;

        GameObject lhr = GameObject.FindWithTag(m_LocalHeadRootTag);
        GameObject cO = GameObject.FindWithTag(m_CameraOffsetTag);

        if (lhr)
            _localHeadRoot = lhr.transform;
        //else
        //Debug.Log($"{this.gameObject.name}...Cant find {m_LocalRootTag}");

        if (cO)
            _cameraOffset = cO.transform;
        //else
        //Debug.Log($"{this.gameObject.name}...Cant find {m_CameraOffsetTag}");*/

        _localRootInit = m_HeadRoot != null && m_LeftControllerRoot != null 
            && m_RightControllerRoot != null && m_CameraOffset != null;
    }

    private void InitController()
    {
/*        if (m_HandMode != HandMode.Controller && m_HandMode != HandMode.Both)
        {
            //Debug.Log($"{this.gameObject.name}...Controllers not supported");
            return;
        }*/

        if (_leftControllerInput == null || !_leftControllerInput.isValid)
        {
            _leftControllerInput = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            _leftHandedness = Handedness.Left;
        }

        if (_rightControllerInput == null || !_rightControllerInput.isValid)
        {
            _rightControllerInput = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            _rightHandedness = Handedness.Right;
        }

/*        switch (m_Type)
        {
            case Type.LeftHand:

                break;
            case Type.RightHand:

                break;
            default:
                //Debug.Log($"{this.gameObject.name}...Avatar Type set does not need controllers");
                return;
        }*/

        _controllerInit = _leftControllerInput.isValid && _rightControllerInput.isValid;
    }

    private void InitHandSubsystem()
    {
        if (m_Device == Device.MetaQuest && _questHandSubsystem != null && _questHandSubsystem.running) return;

        if (m_Device == Device.HoloLens && _holoHandSubsystem != null && _holoHandSubsystem.running) return;

        if (m_Device == Device.MetaQuest)
        {
            SubsystemManager.GetSubsystems(_subsystems);
            if (_subsystems.Count == 0)
            {
                _handSubsysInit = false;
                return;
            }

            foreach (XRHandSubsystem s in _subsystems)
            {
                if (s.running)
                {
                    _questHandSubsystem = s;
                    break;
                }
            }

            SubSubsystem();
            InitQuestXRHand();
            _handSubsysInit = true;
        }
        else if (m_Device == Device.HoloLens)
        {
            _leftXRNode = XRNode.LeftHand;
            _rightXRNode = XRNode.RightHand;

/*            switch (m_Type)
            {
                case Type.LeftHand:
                    _xrNode = XRNode.LeftHand;
                    break;
                case Type.RightHand:
                    _xrNode = XRNode.RightHand;
                    break;
                default:
                    //Debug.Log($"{this.gameObject.name}...Avatar Type set does not need _xrNode");
                    return;
            }*/

            _holoHandSubsystem = XRSubsystemHelpers.GetFirstRunningSubsystem<HandsAggregatorSubsystem>();
            _handSubsysInit = _holoHandSubsystem != null && _holoHandSubsystem.running;
            //if(!_handSubsysInit) Debug.Log($"{this.gameObject.name}...HandSubsystem......failed to init hololens hand subsystem");
        }
        else
        {
            //Debug.Log($"{this.gameObject.name}...HandSubsystem......device not supported");
            _handSubsysInit = false;
        }

    }

    private void InitQuestXRHand()
    {
        _leftXRHand = _questHandSubsystem.leftHand;
        _rightXRHand = _questHandSubsystem.rightHand;
/*        switch (m_Type)
        {
            case Type.LeftHand:
                _leftXRHand = _questHandSubsystem.leftHand;
                break;
            case Type.RightHand:
                _rightXRHand = _questHandSubsystem.rightHand;
                break;
            default:
                //Debug.Log($"{this.gameObject.name}...Avatar type does not need XRHand");
                return;
        }*/
    }

    private bool ControllerTracking(Handedness hand)
    {
        if (!_controllerInit)
        {
            //Debug.Log($"{this.gameObject.name}...controller init {_controllerInit}");
            //_controllerTracking = false;
            return false;
        }

        UnityEngine.InputSystem.XR.XRController controllerDevice = null;

        switch (hand)
        {
            case Handedness.Left:
                controllerDevice = UnityEngine.InputSystem.InputSystem.GetDevice<UnityEngine.InputSystem.XR.XRController>(UnityEngine.InputSystem.CommonUsages.LeftHand);
                break;
            case Handedness.Right:
                controllerDevice = UnityEngine.InputSystem.InputSystem.GetDevice<UnityEngine.InputSystem.XR.XRController>(UnityEngine.InputSystem.CommonUsages.RightHand);
                break;
            default:
                //Debug.Log($"{this.gameObject.name}...handedness not set");
                break;
        }

        //_controllerTracking = 

        return (controllerDevice != null);
    }

    private bool HandTracking(Handedness hand)
    {
        if (!_handSubsysInit)
        {
            //Debug.Log($"{this.gameObject.name}...hand susbsysinit {_handSubsysInit}");
            //_handTracking = false;
            return false;
        }

        if (m_Device == Device.MetaQuest)
        {
            InitQuestXRHand();
            switch(hand)
            {
                case Handedness.Left:
                    return _leftXRHand.isTracked;
                case Handedness.Right:
                    return _rightXRHand.isTracked;
                default:
                    return false;
            }
        }
        else if (m_Device == Device.HoloLens)
        {
            return (_holoHandSubsystem != null && _holoHandSubsystem.running);
        }
        else 
            return false;
    }

    private void OnHandTrackingAcquired(XRHand hand)
    {
        if (hand.handedness == _leftXRHand.handedness)
            _leftHandTracking = true;

        if (hand.handedness == _rightXRHand.handedness)
            _rightHandTracking = true;
    }

    private void OnHandTrackingLost(XRHand hand)
    {
        if (hand.handedness == _leftXRHand.handedness)
            _leftHandTracking = false;

        if (hand.handedness == _rightXRHand.handedness)
            _rightHandTracking = false;
    }

    private void OnUpdatedHands(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags, XRHandSubsystem.UpdateType updateType)
    {
        if (updateType == XRHandSubsystem.UpdateType.Dynamic)
            return;

        Raise();
    }

    private void Raise()
    {
        //initializing data to send
        string _dataToSend = DEFAULTSEND;

        if (!_localRootInit)
        {
            //Debug.Log($"{this.gameObject.name}...Local root not initialized....returning");
            //return;
            LocalRootInit();
            return;
        }

        //event...
        if(OnPublishHeadData != null)
        {
            _dataToSend = PublishData(m_HeadRoot);
            OnPublishHeadData(this, new AvatarInfoEventArgs(_dataToSend));
        }
            
        //event...
        if(OnPublishLeftHandControllerData != null)
        {
            _dataToSend = PublishHandControllerData(_leftXRHand, _leftXRNode, _leftControllerInput, m_LeftControllerRoot, _leftHandedness);
            OnPublishLeftHandControllerData(this, new AvatarInfoEventArgs(_dataToSend));
        }

        //event...
        if(OnPublishRightHandControllerData != null)
        {
            _dataToSend = PublishHandControllerData(_rightXRHand, _rightXRNode, _rightControllerInput, m_RightControllerRoot, _rightHandedness);
            OnPublishRightHandControllerData(this, new AvatarInfoEventArgs(_dataToSend));
        }
    }

    private string PublishData(Transform transform)
    {
        string send = $"1|{transform.position.x}|{transform.position.y}|{transform.position.z}|" +
            $"{transform.eulerAngles.x}|{transform.eulerAngles.y}|{transform.eulerAngles.z}|";
        if (m_DataManager)
            m_DataManager.UpdatePlayerFile(m_PlayerID, transform);

        return send;
    }

    private string PublishHandControllerData(XRHand xrHand, XRNode xrNode, InputDevice controllerInput, Transform controllerRoot, Handedness handedness)
    {
        string send = DEFAULTSEND;
        if (HandTracking(handedness))
        {
            if (!_handSubsysInit)
            {
                InitHandSubsystem();
                return send;
            }
            send = "2|";
            var cameraOffsetPose = new Pose(m_CameraOffset.position, m_CameraOffset.rotation);
            if (m_Device == Device.MetaQuest)
            {
                //because there are 26 joints
                for (int j = 0; j < 26; j++)
                {
                    if (!xrHand.GetJoint((XRHandJointID)(j + 1)).TryGetPose(out Pose jp))
                        continue;

                    Pose jointPose = jp.GetTransformedBy(cameraOffsetPose);

                    send += $"{jointPose.position.x}|{jointPose.position.y}|{jointPose.position.z}|" +
                        $"{jointPose.rotation.eulerAngles.x}|{jointPose.rotation.eulerAngles.y}|{jointPose.rotation.eulerAngles.z}|";

                    if(m_DataManager)
                        m_DataManager.UpdatePlayerFile(m_PlayerID, jointPose, string.Concat((XRHandJointID)(j + 1)));
                }
            }
            else if (m_Device == Device.HoloLens)
            {
                //float error = 0.0f;
                for (int j = 0; j < 26; j++)
                {
                    if (!_holoHandSubsystem.TryGetJoint((TrackedHandJoint)j, xrNode, out HandJointPose hjp))
                        continue;

                    Pose jointPose = hjp.Pose;//.GetTransformedBy(cameraOffsetPose);

                    send += $"{jointPose.position.x}|{jointPose.position.y}|{jointPose.position.z}|" +
                        $"{jointPose.rotation.eulerAngles.x}|{jointPose.rotation.eulerAngles.y}|{jointPose.rotation.eulerAngles.z}|";

                    if (m_DataManager)
                        m_DataManager.UpdatePlayerFile(m_PlayerID, jointPose, string.Concat((TrackedHandJoint)j));
                }
            }
            else
            {
                send = DEFAULTSEND;
            }
        }else if (ControllerTracking(handedness))
        {
            if (!_controllerInit)
            {
                InitController();
                return send;
            }

            send = $"3|{controllerRoot.position.x}|{controllerRoot.position.y}|{controllerRoot.position.z}|" +
                $"{controllerRoot.eulerAngles.x}|{controllerRoot.eulerAngles.y}|{controllerRoot.eulerAngles.z}|";

            if (controllerInput.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float triggerValue))
                send += $"{triggerValue}|";
            else
                send += $"{0}|";

            if (controllerInput.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out float gripValue))
                send += $"{gripValue}|";
            else
                send += $"{0}|";

            if (m_DataManager)
                
                m_DataManager.UpdatePlayerFile(m_PlayerID, controllerRoot);
        }

        return send;
    }

    private void OnDisable()
    {
        if (m_Device == Device.MetaQuest)
        {
            UnSubSubsystem();
        }
    }

    private void SubSubsystem()
    {
        if (_questHandSubsystem == null) return;

        _questHandSubsystem.trackingAcquired += OnHandTrackingAcquired;
        _questHandSubsystem.trackingLost += OnHandTrackingLost;
        _questHandSubsystem.updatedHands += OnUpdatedHands;
    }

    private void UnSubSubsystem()
    {
        if (_questHandSubsystem == null) return;

        _questHandSubsystem.trackingAcquired -= OnHandTrackingAcquired;
        _questHandSubsystem.trackingLost -= OnHandTrackingLost;
        _questHandSubsystem.updatedHands -= OnUpdatedHands;
    }
    
}

public class AvatarInfoEventArgs: EventArgs
{
    public string Value { get;}

    public AvatarInfoEventArgs(string value)
    {
        Value = value;
    }
}