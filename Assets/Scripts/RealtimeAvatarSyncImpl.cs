using MixedReality.Toolkit;
using MixedReality.Toolkit.Subsystems;
using Normal.Realtime;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using Handedness = UnityEngine.XR.Hands.Handedness;

public class RealtimeAvatarSyncImpl : MonoBehaviour
{
    [Header("REMOTE")]
    [SerializeField] private RealtimeView m_RealtimeView;
    [SerializeField] private AvatarSync m_AvatarSync;
    [SerializeField] private Transform m_RemoteRoot;
    [Header("OPTIONAL")]
    [SerializeField] private Transform m_RemoteController;
    [SerializeField] private SkinnedMeshRenderer m_RemoteHandMesh;
    [Header("ENUMS")]
    [SerializeField] private Device m_Device;
    [SerializeField] private Type m_Type;

    //Devices and Subsystems
    
    //Bool checks
    private bool _jointsInit = false;

    //Transforms
    private Transform[] _joints = new Transform[26];

    //Other
    private DataManager _dataManager;

    //Enums
    public enum Device { MetaQuest, HoloLens, Other }
    public enum Type { Head, LeftHand, RightHand, CenterEye, LeftEye, RightEye, Other }
    public enum HandMode { None, Controller, HandTracking, Both }

    private AvatarInfoPub _avatarInfoPublisher;

    private void OnEnable()
    {
        SubscribeAvatarInfo();
    }

    // Start is called before the first frame update
    void Start()
    {
        _dataManager = FindFirstObjectByType<DataManager>();
        if (m_RealtimeView == null) m_RealtimeView = GetComponent<RealtimeView>();
        if (m_AvatarSync == null) m_AvatarSync = GetComponent<AvatarSync>();

        InitHandJoints(m_RemoteRoot);
        _jointsInit = true;
    }

    private void SubscribeAvatarInfo()
    {
        Debug.Log($"{gameObject} getting publish info");
        _avatarInfoPublisher = FindFirstObjectByType<AvatarInfoPub>();
        if (!_avatarInfoPublisher)
        {
            Debug.Log("Cant find avatar info publisher");
            return;
        }

        switch (m_Type)
        {
            case Type.Head:
                _avatarInfoPublisher.OnPublishHeadData += UpdateToNormcore;
                break;
            case Type.LeftHand:
                _avatarInfoPublisher.OnPublishLeftHandControllerData += UpdateToNormcore;
                break;
            case Type.RightHand:
                _avatarInfoPublisher.OnPublishRightHandControllerData += UpdateToNormcore;
                break;
            case Type.CenterEye:
                _avatarInfoPublisher.OnPublishCenterGazeData += UpdateToNormcore;
                break;
            case Type.RightEye:
                _avatarInfoPublisher.OnPublishRightGazeData += UpdateToNormcore;
                break;
            case Type.LeftEye:
                _avatarInfoPublisher.OnPublishLeftGazeData += UpdateToNormcore;
                break;
            default: break;
        }
    }

    private void UnSubscribeAvatarInfo()
    {
        if (!_avatarInfoPublisher)
        {
            Debug.Log("Cant find avatar info publisher");
            return;
        }

        switch (m_Type)
        {
            case Type.Head:
                _avatarInfoPublisher.OnPublishHeadData -= UpdateToNormcore;
                break;
            case Type.LeftHand:
                _avatarInfoPublisher.OnPublishLeftHandControllerData -= UpdateToNormcore;
                break;
            case Type.RightHand:
                _avatarInfoPublisher.OnPublishRightHandControllerData -= UpdateToNormcore;
                break;
            default:
                break;
        }
    }

    private void InitHandJoints(Transform root)
    {
        if(_jointsInit) return;

        if (m_Device == Device.MetaQuest && (m_Type == Type.LeftHand || m_Type == Type.RightHand))
        {
            for (int j = 0; j < _joints.Length; j++)
            {
                if (root.name.ToLower().Contains(XRHandJointIDUtility.FromIndex(j).ToString().ToLower()))
                {
                    _joints[j] = root;
                    break;
                }
            }
        }
        else if(m_Device == Device.HoloLens && (m_Type == Type.LeftHand || m_Type == Type.RightHand))
        {
            /*if (!_holoHandSubsystem.TryGetEntireHand(_xrNode, out IReadOnlyList<HandJointPose> jp))
            {
                _jointsInit = false;
                Debug.Log($"{this.gameObject.name} _jointsInit false couldnt get hololens hand");
                return false;
            }*/

            for (int j = 0; j < _joints.Length; j++)
            {
                if (root.name.ToLower().Contains(((TrackedHandJoint)j).ToString().ToLower()))
                {
                    _joints[j] = root;
                    //Debug.Log($"{this.gameObject.name}...assigned {root.name}");
                    break;
                }
            }
        }
        else
        {
            //Debug.Log($"{this.gameObject.name}...Init joints, device or type not supported");
            _jointsInit = true;//so we don't try again
            return;
        }
        for (int c = 0; c < root.childCount; c++)
            InitHandJoints(root.GetChild(c));

        /*if (_joints.Contains(null))
        {
            Debug.Log($"{this.gameObject.name} _jointsInit false joints contains null");
        }*/
    }

    private void UpdateToNormcore(object sender, AvatarInfoEventArgs args)
    {
        //DONT SEND ANYTHING TO NORMCORE IF THE REALTIME IS NOT LOCALLY OWNED
        if (m_RealtimeView != null && !m_RealtimeView.isOwnedLocallySelf)
        {
            //Debug.Log($"{this.gameObject.name}...Not sending to Normcore....returning");
            return;
        }

        //Debug.Log($"{this.gameObject.name} Receiving from publisher....{args.Value}....userID...{m_RealtimeView.ownerIDSelf}");
        m_AvatarSync.SetAvatarData(args.Value);
    }

    public void UpdateFromNormcore(string netData)
    {
        if (m_RealtimeView != null && m_RealtimeView.isOwnedLocallySelf)
        {
            m_RemoteRoot.gameObject.SetActive(false);
            return;
        }
/*
        if (netData == null || netData == "")
        {
            //Debug.Log("Empty netData");
            return;
        }*/

        //Debug.Log($"{this.gameObject.name}...receiving....{netData}....userID...{m_RealtimeView.ownerIDSelf}");

        string[] netDataArr = netData.Split('|');

        if (netDataArr[0] == "0")
        {
            //Debug.Log($"{this.gameObject.name}...received 0, show nothing");
            if (m_Type == Type.LeftHand || m_Type == Type.RightHand)
            {
                m_RemoteHandMesh.enabled = false;
                m_RemoteController.gameObject.SetActive(false);
                m_RemoteRoot.gameObject.SetActive(false);
            }
        }
        else if (netDataArr[0] == "1")
        {
            if (m_Type != Type.Head && m_Type != Type.Other) return;

            m_RemoteRoot.gameObject.SetActive(true);

            m_RemoteRoot.position = new Vector3(float.Parse(netDataArr[1]),
                float.Parse(netDataArr[2]),
                float.Parse(netDataArr[3]));
            m_RemoteRoot.eulerAngles = new Vector3(float.Parse(netDataArr[4]),
                float.Parse(netDataArr[5]),
                float.Parse(netDataArr[6]));

/*            if (_dataManager)
                _dataManager.UpdatePlayerFile(m_RealtimeView.ownerIDSelf, m_RemoteRoot.transform);*/
        }
        else if (netDataArr[0] == "2")
        {
            
            if (m_Type == Type.LeftHand || m_Type == Type.RightHand)
            {
                m_RemoteRoot.gameObject.SetActive(true);
                m_RemoteHandMesh.enabled = true;
                m_RemoteController.gameObject.SetActive(false);
            }
            else return;
            
            for (int j = 0; j < _joints.Length; j++)
            {
                if(_joints[j] == null)
                {
                    //Debug.Log($"joint {j} null? {_joints[j] == null}");
                    continue;
                }
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
/*                if(_dataManager)
                    _dataManager.UpdatePlayerFile(m_RealtimeView.ownerIDSelf, _joints[j].GetWorldPose(), string.Concat((XRHandJointID)(j + 1)));*/
            }
        }
        else if (netDataArr[0] == "3")
        {
            if (m_Type == Type.LeftHand || m_Type == Type.RightHand)
            {
                m_RemoteRoot.gameObject.SetActive(true);
                m_RemoteController.gameObject.SetActive(true);
                m_RemoteHandMesh.enabled = false;
            }
            else return;

            m_RemoteController.position = new Vector3(float.Parse(netDataArr[1]),
                float.Parse(netDataArr[2]),
                float.Parse(netDataArr[3]));
            m_RemoteController.eulerAngles = new Vector3(float.Parse(netDataArr[4]),
                float.Parse(netDataArr[5]),
                float.Parse(netDataArr[6]));

            float trigger = float.Parse(netDataArr[7]);
            float grip = float.Parse(netDataArr[8]);
            //you can manipulate things like animator values using grip and trigger above

/*            if (_dataManager)
                _dataManager.UpdatePlayerFile(m_RealtimeView.ownerIDSelf, m_RemoteController.transform);*/
        }
        else if (netDataArr[0] == "4")
        {
            if (m_Type == Type.CenterEye)
            {
                m_RemoteRoot.gameObject.SetActive(true);
            }
            else return;

            m_RemoteRoot.position = new Vector3(float.Parse(netDataArr[1]),
                float.Parse(netDataArr[2]),
                float.Parse(netDataArr[3]));
            m_RemoteRoot.eulerAngles = new Vector3(float.Parse(netDataArr[4]),
                float.Parse(netDataArr[5]),
                float.Parse(netDataArr[6]));
        }
        else
        {
            //Debug.Log("Error reading netdata");
        }
    }

    private void OnDisable()
    {
        UnSubscribeAvatarInfo();
    }
}
