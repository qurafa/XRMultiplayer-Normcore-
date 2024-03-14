using MixedReality.Toolkit;
using MixedReality.Toolkit.Subsystems;
using Normal.Realtime;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using Handedness = UnityEngine.XR.Hands.Handedness;

public class AvatarSyncImpl : MonoBehaviour
{
    [Header("REMOTE")]
    [SerializeField] private RealtimeView m_RealtimeView;
    [SerializeField] private AvatarSync m_AvatarSync;
    [SerializeField] private Transform m_RemoteRoot;
    [SerializeField] private Transform m_RemoteController;
    [SerializeField] private SkinnedMeshRenderer m_RemoteHandMesh;
    [Header("TAGS")]
    [SerializeField] private string m_LocalRootTag;
    [SerializeField] private string m_CameraOffsetTag;
    [Header("ENUMS")]
    [SerializeField] private Device m_Device;
    [SerializeField] private Type m_Type;
    [SerializeField] private HandMode m_HandMode;

    //Devices and Subsystems
    private InputDevice _controller;
    private XRHandSubsystem _questHandSubsystem;
    private HandsAggregatorSubsystem _holoHandSubsystem;
    private List<XRHandSubsystem> _subsystems = new();
    
    //Bool checks
    private bool _localRootInit = false;
    private bool _controllerInit = false;
    private bool _handSubsysInit = false;
    private bool _jointsInit = false;
    private bool _xrInit = false;

    private bool _controllerTracking = false;
    private bool _handTracking = false;

    //Transforms
    private Transform _localRoot;
    private Transform _cameraOffset;
    private Transform[] _joints = new Transform[26];

    //XR
    private XRHand _xrHand;
    private Handedness _handedness;

    //Other
    private DataManager _dataManager;

    //Enums
    public enum Device { MetaQuest, HoloLens, Other }
    public enum Type { Head, LeftHand, RightHand, Other }
    public enum HandMode { None, Controller, HandTracking, Both }

    // Start is called before the first frame update
    void Start()
    {
        _dataManager = FindFirstObjectByType<DataManager>();
        if (m_RealtimeView == null) m_RealtimeView = GetComponent<RealtimeView>();
        if (m_AvatarSync == null) m_AvatarSync = GetComponent<AvatarSync>();

        InitHandJoints(m_RemoteRoot);
        _jointsInit = true;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateToNormcore();

        if (m_RealtimeView != null && m_RealtimeView.isOwnedLocallySelf)
        {
            InitLocalReferences();
            InitController();
            InitHandSubsystem();
        }

        ControllerTracking();
        HandTracking();
    }

    private void InitLocalReferences()
    {
        if (_localRoot != null && _cameraOffset != null) return;

       GameObject lr  = GameObject.FindWithTag(m_LocalRootTag);
       GameObject cO = GameObject.FindWithTag(m_CameraOffsetTag);

        if(lr)
            _localRoot = lr.transform;
        //else
            //Debug.Log($"{this.gameObject.name}...Cant find {m_LocalRootTag}");

        if (cO)
            _cameraOffset = cO.transform;
        //else
            //Debug.Log($"{this.gameObject.name}...Cant find {m_CameraOffsetTag}");

        _localRootInit = _localRoot != null && _cameraOffset != null;
    }

    private void InitController()
    {
        if (m_HandMode != HandMode.Controller && m_HandMode != HandMode.Both)
        {
            //Debug.Log($"{this.gameObject.name}...Controllers not supported");
            return;
        }

        if (_controller.isValid) return;

        switch (m_Type)
        {
            case Type.LeftHand:
                _controller = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                _handedness = Handedness.Left;
                break;
            case Type.RightHand:
                _controller = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                _handedness = Handedness.Right;
                break;
            default:
                //Debug.Log($"{this.gameObject.name}...Avatar Type set does not need controllers");
                return;
        }

        _controllerInit = _controller.isValid;
    }

    private void InitHandSubsystem()
    {
        if (m_Device == Device.MetaQuest && _questHandSubsystem != null && _questHandSubsystem.running) return;

        if (m_Device == Device.HoloLens && _holoHandSubsystem != null && _holoHandSubsystem.running) return;

        if(m_Device == Device.MetaQuest)
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
            InitXRHand();
            _handSubsysInit = true;
        }
        else if (m_Device == Device.HoloLens)
        {
            _holoHandSubsystem = XRSubsystemHelpers.GetFirstRunningSubsystem<HandsAggregatorSubsystem>();
            _handSubsysInit = _holoHandSubsystem != null && _holoHandSubsystem.running;
            //todo
        }
        else
        {
            //Debug.Log($"{this.gameObject.name}...HandSubsystem......device not supported");
            _handSubsysInit = false;
        }
        
    }

    private void InitHandJoints(Transform root)
    {
        if(_jointsInit) return;
            
        if((m_Type == Type.LeftHand || m_Type == Type.RightHand) && m_Device == Device.MetaQuest)
        {
            for (int j = 0; j < _joints.Length; j++)
            {
                if (root.name.ToLower().Contains(XRHandJointIDUtility.FromIndex(j).ToString().ToLower()))
                {
                    _joints[j] = root;
                    //Debug.Log($"{this.gameObject.name}...{j} Assign {root.name} to {XRHandJointIDUtility.FromIndex(j)}");
                    break;
                }
            }
            
        }
        else if((m_Type == Type.LeftHand || m_Type == Type.RightHand) && m_Device == Device.HoloLens)
        {
            for (int j = 0; j < _joints.Length; j++)
            {
                if (root.name.ToLower().Contains(((TrackedHandJoint)j).ToString().ToLower()))
                {
                    _joints[j] = root;
                    break;
                }
            }
        }
        else
        {
            Debug.Log($"{this.gameObject.name}...Init joints, device or type not supported");
            return;
        }
        for (int c = 0; c < root.childCount; c++)
            InitHandJoints(root.GetChild(c));
    }

    private void InitXRHand()
    {
        switch (m_Type)
        {
            case Type.LeftHand:
                _xrHand = _questHandSubsystem.leftHand;
                break;
            case Type.RightHand:
                _xrHand = _questHandSubsystem.rightHand;
                break;
            default:
                //Debug.Log($"{this.gameObject.name}...Avatar type does not need XRHand");
                return;
        }
    }

    private bool ControllerTracking()
    {
        if (!_controllerInit)
        {
            //Debug.Log($"{this.gameObject.name}...controller init {_controllerInit}");
            _controllerTracking = false;
            return false;
        }

        UnityEngine.InputSystem.XR.XRController controllerDevice = null;

        switch (_handedness)
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

        _controllerTracking = (controllerDevice != null);

        return _controllerTracking;
    }

    private bool HandTracking()
    {
        if(!_handSubsysInit) {
            //Debug.Log($"{this.gameObject.name}...hand susbsysinit {_handSubsysInit}");
            _handTracking = false;
            return false;
        }

        InitXRHand();
        _handTracking = _xrHand.isTracked;

        return _handTracking;
    }

    private void OnHandTrackingAcquired(XRHand hand)
    {
        if(hand.handedness == _xrHand.handedness)
            _handTracking = true;
    }

    private void OnHandTrackingLost(XRHand hand)
    {
        if (hand.handedness == _xrHand.handedness)
            _handTracking = false;
    }

    private void OnUpdatedHands(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags, XRHandSubsystem.UpdateType updateType)
    {
        if (updateType == XRHandSubsystem.UpdateType.Dynamic)
            return;

        UpdateToNormcore();
    }

    private void UpdateToNormcore()
    {
        //DONT SEND ANYTHING TO NORMCORE IF THE REAALTIME VIEW IS NOT LOCALLY OWNED
        if (m_RealtimeView != null && !m_RealtimeView.isOwnedLocallySelf)
        {
            //Debug.Log($"{this.gameObject.name}...Not sending to Normcore....returning");
            return;
        }

        //initializing data to send
        string dataToSend = "";

        if (!_localRootInit)
        {
            //Debug.Log($"{this.gameObject.name}...Local root not initialized....returning");
            return;
        }

        if (m_Type == Type.Head || m_Type == Type.Other) 
        {
            dataToSend += "1|";
            dataToSend += $"{_localRoot.position.x}|{_localRoot.position.y}|{_localRoot.position.z}|{_localRoot.eulerAngles.x}|{_localRoot.eulerAngles.y}|{_localRoot.eulerAngles.z}|";
            _dataManager.UpdatePlayerFile(m_RealtimeView.ownerIDSelf, _localRoot);
            //Debug.Log($"{this.gameObject.name}...data to send....{dataToSend}");
        }
        else
        {
            //Debug.Log($"{this.gameObject.name}..._handTracking: {_handTracking}, _controllerTracking: {_controllerTracking}");
            if (_handTracking)
            {
                if (!_jointsInit || !_handSubsysInit)
                {
                    //Debug.Log($"{this.gameObject.name}..._jointsInit: {_jointsInit}, _handSubsysInit: {_handSubsysInit}");
                    return;
                }
                dataToSend += "2|";
                for (int j = 0; j < _joints.Length; j++)
                {
                    if (!_xrHand.GetJoint((XRHandJointID)(j + 1)).TryGetPose(out Pose jp))
                        return;

                    var cameraOffsetPose = new Pose(_cameraOffset.position, _cameraOffset.rotation);
                    Pose jointPose = jp.GetTransformedBy(cameraOffsetPose);

                    dataToSend += $"{jointPose.position.x}|{jointPose.position.y}|{jointPose.position.z}|{jointPose.rotation.eulerAngles.x}|{jointPose.rotation.eulerAngles.y}|{jointPose.rotation.eulerAngles.z}|";

                    _dataManager.UpdatePlayerFile(m_RealtimeView.ownerIDSelf, jointPose, string.Concat((XRHandJointID)(j + 1)));
                }
                //Debug.Log($"{this.gameObject.name}...sending hand track data....{dataToSend}");
            }
            else if (_controllerTracking)
            {
                if (!_controllerInit)
                {
                    //Debug.Log($"{this.gameObject.name}..._controllerInit:{_controllerInit}");
                    return;
                }

                dataToSend += "3|";
                dataToSend += $"{_localRoot.position.x}|{_localRoot.position.y}|{_localRoot.position.z}|{_localRoot.eulerAngles.x}|{_localRoot.eulerAngles.y}|{_localRoot.eulerAngles.z}|";

                if (_controller.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float triggerValue))
                    dataToSend += $"{triggerValue}|";
                else
                    dataToSend += $"{0}|";

                if (_controller.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out float gripValue))
                    dataToSend += $"{gripValue}|";
                else
                    dataToSend += $"{0}|";

                _dataManager.UpdatePlayerFile(m_RealtimeView.ownerIDSelf, _localRoot);
                //Debug.Log($"{this.gameObject.name}...sending controller data....{dataToSend}");
            }
            else
            {
                dataToSend = "0|";
            }
        }
        //Debug.Log($"{this.gameObject.name}...Sending....{dataToSend}");
        m_AvatarSync.SetAvatarData(dataToSend);
    }

    public void UpdateFromNormcore(string netData)
    {
        if (m_RealtimeView != null && m_RealtimeView.isOwnedLocallySelf)
            return;

        if (netData == null || netData == "")
        {
            Debug.Log("Empty netData");
            return;
        }  

        //Debug.Log($"Receiving {netData}");

        string[] netDataArr = netData.Split('|');

        if (netDataArr[0] == "0")
        {
            if (m_HandMode == HandMode.HandTracking || m_HandMode == HandMode.Both) m_RemoteHandMesh.enabled = false;
            if (m_HandMode == HandMode.Controller || m_HandMode == HandMode.Both) m_RemoteController.gameObject.SetActive(false);
            m_RemoteRoot.gameObject.SetActive(false);
        }
        else if (netDataArr[0] == "1")
        {
            if(m_Type == Type.Head) m_RemoteRoot.gameObject.SetActive(true);

            m_RemoteRoot.position = new Vector3(float.Parse(netDataArr[1]),
                float.Parse(netDataArr[2]),
                float.Parse(netDataArr[3]));
            m_RemoteRoot.eulerAngles = new Vector3(float.Parse(netDataArr[4]),
                float.Parse(netDataArr[5]),
                float.Parse(netDataArr[6]));

            _dataManager.UpdatePlayerFile(m_RealtimeView.ownerIDSelf, m_RemoteRoot.transform);
        }
        else if (netDataArr[0] == "2")
        {
            m_RemoteRoot.gameObject.SetActive(true);
            if (m_HandMode == HandMode.HandTracking || m_HandMode == HandMode.Both) m_RemoteHandMesh.enabled = true;
            if (m_HandMode == HandMode.Controller || m_HandMode == HandMode.Both) m_RemoteController.gameObject.SetActive(false);
            
            for (int j = 0; j < _joints.Length; j++)
            {
                //Debug.Log($"joint {j} null? {_joints[j] == null}");
                int jTmp = j * 6;
                _joints[j].position =
                    new Vector3(
                    float.Parse(netDataArr[jTmp + 1]),
                    float.Parse(netDataArr[jTmp + 2]),
                    float.Parse(netDataArr[jTmp + 3]));
                _joints[j].eulerAngles =
                    new Vector3(
                        float.Parse(netDataArr[jTmp + 4]),
                        float.Parse(netDataArr[jTmp + 5]),
                        float.Parse(netDataArr[jTmp + 6]));

                _dataManager.UpdatePlayerFile(m_RealtimeView.ownerIDSelf, _joints[j].GetWorldPose(), string.Concat((XRHandJointID)(j + 1)));
            }
        }
        else if (netDataArr[0] == "3")
        {
            m_RemoteRoot.gameObject.SetActive(true);
            if (m_HandMode == HandMode.HandTracking || m_HandMode == HandMode.Both) m_RemoteHandMesh.enabled = false;
            if (m_HandMode == HandMode.Controller || m_HandMode == HandMode.Both) m_RemoteController.gameObject.SetActive(true);

            m_RemoteController.position = new Vector3(float.Parse(netDataArr[1]),
                float.Parse(netDataArr[2]),
                float.Parse(netDataArr[3]));
            m_RemoteController.eulerAngles = new Vector3(float.Parse(netDataArr[4]),
                float.Parse(netDataArr[5]),
                float.Parse(netDataArr[6]));

            float trigger = float.Parse(netDataArr[7]);
            float grip = float.Parse(netDataArr[8]);
            //you can manipulate things like animator values using grip and trigger above

            _dataManager.UpdatePlayerFile(m_RealtimeView.ownerIDSelf, m_RemoteController.transform);
        }
        else
        {
            Debug.Log("Error reading netdata");
        }
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
        if(_questHandSubsystem == null) return;

        _questHandSubsystem.trackingAcquired -= OnHandTrackingAcquired;
        _questHandSubsystem.trackingLost -= OnHandTrackingLost;
        _questHandSubsystem.updatedHands -= OnUpdatedHands;
    }
}
