using MixedReality.Toolkit;
using MixedReality.Toolkit.Subsystems;
using Normal.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;
using UnityEditor.XR.LegacyInputHelpers;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.Receiver.Primitives;
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
    private XRHandSubsystem _subsystem;
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
    private XROrigin _xrOrigin;
    private XRHand _xrHand;
    private XRNode _xrNode;
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

        InitLocalReferences();
        InitController();
        InitHandSubsystem();
        InitXR();

        ControllerTracking();
        HandTracking();
    }

    private void InitLocalReferences()
    {
        if (_localRootInit) return;

       GameObject lr  = GameObject.FindWithTag(m_LocalRootTag);
       GameObject cO = GameObject.FindWithTag(m_CameraOffsetTag);

        if(lr != null && cO != null)
        {
            _localRoot = lr.transform;
            _cameraOffset = cO.transform;

            _localRootInit = true;
        }
    }

    private void InitController()
    {
        if (m_HandMode != HandMode.Controller && m_HandMode != HandMode.Both)
        {
            Debug.Log("HandMode does not need controllers");
            _controllerInit =  true;
            return;
        }

        if (_controllerInit) return;

        switch (m_Type)
        {
            case Type.LeftHand:
                _controller = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                break;
            case Type.RightHand:
                _controller = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                break;
            default:
                Debug.Log("Avatar Type set does not need controllers");
                return;
        }

        _controllerInit = _controller != null;
    }

    private void InitHandSubsystem()
    {
        if (_handSubsysInit) return;
            
        if(m_Device == Device.MetaQuest || m_Device == Device.HoloLens)
        {
            SubsystemManager.GetSubsystems(_subsystems);
            if (_subsystems.Count == 0)
                return;
                
            foreach (XRHandSubsystem s in _subsystems)
            {
                if(s.running) _subsystem = s;
            }

            _subsystem.trackingAcquired += OnHandTrackingAcquired;
            _subsystem.trackingLost += OnHandTrackingLost;
            _subsystem.updatedHands += OnUpdatedHands;
        }
        else
        {
            Debug.Log("Init HandSubsystem, Device not supported");
        }
        _handSubsysInit = true;
    }

    private void InitHandJoints(Transform root)
    {
        if(_jointsInit) return;
            
        if((m_Type == Type.LeftHand || m_Type == Type.RightHand) && (m_Device == Device.MetaQuest || m_Device == Device.HoloLens))
        {
            for (int j = 0; j < _joints.Length; j++)
            {
                if (root.name.ToLower().Contains(XRHandJointIDUtility.FromIndex(j).ToString().ToLower()))
                {
                    _joints[j] = root;
                    Debug.Log($"{j} Assign {root.name} to {XRHandJointIDUtility.FromIndex(j)}");
                    break;
                }
            }
            for (int c = 0; c < root.childCount; c++)
                InitHandJoints(root.GetChild(c));
        }
        /*else if(m_Device == Device.HoloLens)
        {
            for (int j = 0; j < _joints.Length; j++)
            {
                if (root.name.ToLower().Contains(((TrackedHandJoint)j).ToString().ToLower()))
                {
                    _joints[j] = root;
                    break;
                }
            }
            for (int c = 0; c < root.childCount; c++)
                InitHandJoints(root.GetChild(c));
        }*/
        else
        {
            Debug.Log("Init joints, device not supported");
        }
    }

    private void InitXR()
    {
        if (_xrInit) return;

        switch (m_Type)
        {
            case Type.LeftHand:
                _xrNode = XRNode.LeftHand;
                _handedness = Handedness.Left;
                if(_subsystem != null) _xrHand = _subsystem.leftHand;
                break;
            case Type.RightHand:
                _xrNode = XRNode.RightHand;
                _handedness = Handedness.Right;
                if (_subsystem != null) _xrHand = _subsystem.rightHand;
                break;
            case Type.Head:
                _xrNode = XRNode.Head;
                break;
            default:
                Debug.Log("Init XR, Avatar Type not properly set");
                return;
        }
        _xrInit = true;
    }

    private bool ControllerTracking()
    {
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
                Debug.Log("handedness not set");
                break;
        }

        _controllerTracking = (controllerDevice != null);

        return _controllerTracking;
    }

    private bool HandTracking()
    {
        /*if(m_Device == Device.HoloLens)
        {
            _handTracking = _hololensSubsystem.TryGetEntireHand(_xrNode, out IReadOnlyList<HandJointPose> jp);
        }*/

        if (_subsystem != null)
        {
            switch (_xrHand.handedness)
            {
                case Handedness.Left:
                    if (_subsystem.leftHand.isTracked) _handTracking = true;
                    else _handTracking = false;
                    break;
                case Handedness.Right:
                    if (_subsystem.rightHand.isTracked) _handTracking = true;
                    else _handTracking = false;
                    break;
                default:
                    Debug.Log("error updating hand tracking");
                    break;
            }
        }
        else
        {
            _handTracking = false;
        }

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
            return;

        //initializing data to send
        string dataToSend = "";

        if(m_Type == Type.Head || m_Type == Type.Other) 
        {
            if (!_localRootInit) return;

            dataToSend += "1|";
            dataToSend += $"{_localRoot.position.x}|{_localRoot.position.y}|{_localRoot.position.z}|{_localRoot.eulerAngles.x}|{_localRoot.eulerAngles.y}|{_localRoot.eulerAngles.z}|";
            _dataManager.UpdatePlayerFile(m_RealtimeView.ownerIDSelf, _localRoot);
        }
        else
        {
            if (_handTracking)
            {
                if (!_jointsInit || !_xrInit) return;

                dataToSend += "2|";
                if (m_Device == Device.MetaQuest || m_Device == Device.HoloLens)
                {
                    for (int j = 0; j < _joints.Length; j++)
                    {
                        if (!_xrHand.GetJoint((XRHandJointID)(j + 1)).TryGetPose(out Pose jp))
                            return;

                        var cameraOffsetPose = new Pose(_cameraOffset.position, _cameraOffset.rotation);
                        Pose jointPose = jp.GetTransformedBy(cameraOffsetPose);

                        dataToSend += $"{jointPose.position.x}|{jointPose.position.y}|{jointPose.position.z}|{jointPose.rotation.eulerAngles.x}|{jointPose.rotation.eulerAngles.y}|{jointPose.rotation.eulerAngles.z}|";

                        _dataManager.UpdatePlayerFile(m_RealtimeView.ownerIDSelf, jointPose, string.Concat((XRHandJointID)(j + 1)));
                    }
                }
                else
                {
                    Debug.Log("Hand Joint data not supported fro this device");
                }
            }
            else if (_controllerTracking)
            {
                if(!_localRootInit || !_controllerInit) return;

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
            }
            else
            {
                dataToSend = "0";
            }
        }
        Debug.Log($"Sending {dataToSend}");
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

        Debug.Log($"Receiving {netData}");

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
                Debug.Log($"joint {j} null: {_joints[j] == null}");
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
            _subsystem.trackingAcquired -= OnHandTrackingAcquired;
            _subsystem.trackingLost -= OnHandTrackingLost;
            _subsystem.updatedHands -= OnUpdatedHands;
        }
    }
}
