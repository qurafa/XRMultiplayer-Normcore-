using System;
using System.Collections.Generic;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Hands;

[RequireComponent(typeof(Rigidbody))]
public class CallibrateRoom : MonoBehaviour
{
    /// <summary>
    /// Room we're calibrating
    /// </summary>
    [SerializeField]
    private GameObject _room;
    [SerializeField]
    private XROrigin _player;
    [SerializeField]
    private Transform _playerSpawn;
    /// <summary>
    /// What the room shouuld rotate about
    /// </summary>
    [SerializeField]
    private GameObject _rotationReference;
    /// <summary>
    /// Reference to the center of the player in the room
    /// </summary>
    [SerializeField]
    private GameObject _playerCenterReference;
    /// <summary>
    /// ARSession to manage and toggle ar in the scene
    /// </summary>
    [SerializeField]
    private ARSession _ARSession;
    /*    private List<Transform> _ignores;
        private List<Rigidbody> _ignoresRigidbody;*/
    /*    /// <summary>
        /// The RealtimeView of the Room
        /// </summary>
        [SerializeField]
        private RealtimeView _rtView;
        /// <summary>
        /// The RealtimeTransform of the Room
        /// </summary>
        [SerializeField]
        private RealtimeTransform _rtTransform;*/
    /// <summary>
    /// realtimeHelper to help us join the room
    /// </summary>
   /* [SerializeField]
    private realtimeHelper _rtHelper;*/
    /// <summary>
    /// Event to be triggered after we're done calibrating the room
    /// </summary>
    [SerializeField]
    private MyLoadSceneEvent _doneEvent;
    [SerializeField]
    private bool _doneDebug;
    /// <summary>
    /// Passthrough layer to toggle passthrough for the users at runtime
    /// </summary>
    /*[SerializeField]
    private OVRPassthroughLayer _passthroughLayer;*/

    private Rigidbody _roomRB;
    private List<Transform> _listOfChildren = new List<Transform>();
    /// <summary>
    /// We turn this off when we're done calibrating
    /// </summary>
    [SerializeField]
    private bool _canCalibrate = true;//we turn this off after we're done calibrating

    /// <summary>
    /// Texts to provide ui feedback to the user
    /// 0 - Standby, 1 - Position, 2 - Rotation
    /// </summary>
    [Header("MODES")]
    [SerializeField]
    private TMP_Text[] _modes = new TMP_Text[3];
    /// <summary>
    /// Passthrough text to provide visual feedback
    /// </summary>
    [SerializeField]
    private TMP_Text passThrough;
    [SerializeField]
    private Material[] _seeThroughInPassThroughMaterials = new Material[0];
    [SerializeField]
    private Material[] _partialSeeThroughInPassThroughMaterials = new Material[0];
    [SerializeField]
    private Color UI_NOT_SELECTED = Color.white;
    [SerializeField]
    private Color UI_SELECTED = Color.blue;

    [Header("INPUT ACTIONS")]
    [SerializeField]
    private InputAction menuButton;
    [SerializeField]
    private InputAction leftHandPB;
    [SerializeField]
    private InputAction leftHandSB;
    [SerializeField]
    private InputAction leftJS;
    [SerializeField]
    private InputAction rightHandPB;
    [SerializeField]
    private InputAction rightHandSB;
    [SerializeField]
    private InputAction rightJS;
    [SerializeField]
    private InputAction trigger;

    private Vector2 _rightJsValTemp = new Vector2(0,0);
    private bool _triggerPressed = false;

    /// <summary>
    /// What we send to the SceneLoader as we move to the next scene
    /// </summary>
    private MyTransform _send;
    GameObject _rotRef;
    
    enum Vision
    {
        Normal,
        Passthrough
    }
    private Vision _vision;

    public enum Mode
    {
        Standby,
        CalibratingPos,
        CalibratingRot,
        Done
    }
    private Mode _mode;

    [SerializeField]
    private float direction = 0.0f;
    public readonly float rotFactor = 0.05f;
    public readonly float posFactor = 0.00625f;

    [SerializeField]
    Mode mode
    {
        get { return _mode; }
        set
        {
            _mode = value;

            //ToggleModeUI();
            ModeChanged();
        }
    }

    Vision vision
    {
        get { return _vision; }
        set
        {
            _vision = value;

            //VisionChanged();
        }
    }

    void OnEnable()
    {
        menuButton.Enable(); menuButton.performed += DoneAction;
        //leftHandPB.Enable(); leftHandPB.performed += VisionToggleAction;
        //leftHandSB.Enable(); leftHandSB.performed += LeftRotateAction;
        //leftJS.Enable(); leftJS.performed += VPositioningAction;
        rightHandPB.Enable(); rightHandPB.performed += TeleportAction;
        //rightHandSB.Enable(); rightHandSB.performed += RightRotateAction;
        rightJS.Enable(); rightJS.performed += HPositioningAction; rightJS.performed += VPositioningAction;
        trigger.Enable(); trigger.performed += TriggerPressed; trigger.canceled += TriggerReleased;
    }

    void Start()
    {
        if (!_rotationReference)
            _rotationReference = GameObject.FindWithTag("MainCamera");

        _roomRB = _room.GetComponent<Rigidbody>();
        mode = Mode.CalibratingPos;
        vision = Vision.Passthrough;

        //saving the starting transform
        _send = new MyTransform(transform.position, transform.eulerAngles);
        _rotRef = Instantiate(new GameObject("_rotRef"), _rotationReference.transform);

        Debug.Log("Strt Done");
    }

    // Update is called once per frame
    void Update()
    {
        //only if we can can calibrate and are holding the controllers
        if (_canCalibrate)
        {
            //if  "done" is toggled in inspector
            if (_doneDebug)
            {
                mode = Mode.Done;
            }

            if (mode == Mode.CalibratingRot)
                _roomRB.transform.RotateAround(_rotationReference.transform.position, Vector3.up, rotFactor * direction);
        }
    }

    public void SetMode(Mode m)
    {
        mode = m;
    }

    public void SetMode(int m)
    {
        mode = (Mode)m;
    }

    private void TeleportAction(InputAction.CallbackContext obj)
    {
        float yPos = _player.transform.position.y + _player.CameraInOriginSpaceHeight;
        _player.MoveCameraToWorldLocation(new Vector3(_playerSpawn.position.x, yPos, _playerSpawn.position.z));
        _player.MatchOriginUpCameraForward(_playerSpawn.up, _playerSpawn.forward);
    }

    private void PositionToggleAction(InputAction.CallbackContext obj)
    {
        Debug.Log("A was pressed");

        switch (mode)
        {
            case Mode.Standby:
                mode = Mode.CalibratingPos;
                break;
            default:
                Debug.Log($"Switch from {mode.ToString()} to standby");
                mode = Mode.Standby;
                return;
        }
    }
    
    private void VPositioningAction(InputAction.CallbackContext obj)
    {
        if(mode == Mode.CalibratingPos && _triggerPressed) {
            Vector2 val = rightJS.ReadValue<Vector2>();
            val -= _rightJsValTemp;
            _rotRef.transform.eulerAngles = new Vector3(0, _rotationReference.transform.eulerAngles.y, 0);
            _room.transform.Translate(new Vector3(0, val.y, 0) * posFactor, _rotRef.transform);
        }
    }

    private void HPositioningAction(InputAction.CallbackContext obj)
    {
        if (mode == Mode.CalibratingPos && !_triggerPressed) {
            Vector2 val = rightJS.ReadValue<Vector2>();
            val -= _rightJsValTemp;
            _rotRef.transform.eulerAngles = new Vector3(0, _rotationReference.transform.eulerAngles.y, 0);
            _room.transform.Translate(new Vector3(val.x, 0, val.y) * posFactor, _rotRef.transform);
        }
    }

    private void TriggerPressed(InputAction.CallbackContext obj)
    {
        _triggerPressed = true;
    }

    private void TriggerReleased(InputAction.CallbackContext obj)
    {
        _triggerPressed = false;
    }

    private void LeftRotateAction(InputAction.CallbackContext obj)
    {
        Rotate(-1);
    }

    private void RightRotateAction(InputAction.CallbackContext obj)
    {
        Rotate(1);
    }

    private void Rotate(int dir)
    {
        switch (mode)
        {
            case Mode.Standby:
                mode = Mode.CalibratingRot;
                direction = dir;
                break;
            default:
                Debug.Log($"Switch from {mode.ToString()} to standby");
                mode = Mode.Standby;
                return;
        }
    }
    
    private void VisionToggleAction(InputAction.CallbackContext obj)
    {
        switch (vision)
        {
            case Vision.Normal:
                vision = Vision.Passthrough;
                break;
            case Vision.Passthrough:
                vision = Vision.Normal;
                break;
            default:
                vision = Vision.Normal;
                return;
        }
    }

    private void DoneAction(InputAction.CallbackContext obj)
    {
        mode = Mode.Done;
    }

    private void ModeChanged()
    {
        if (mode == Mode.Standby)
        {
            //freeze everything, inlcuding position and rotation
            //hide the passthrough layer
            //stop all room rotations
            //set the material colors right
            _roomRB.constraints = RigidbodyConstraints.FreezeAll;
            direction = 0;
        }
        else if (mode == Mode.CalibratingPos)
        {
            //only freeze the room rotating
            _roomRB.constraints = RigidbodyConstraints.FreezeRotation;// | RigidbodyConstraints.FreezeRotationZ;
        }
        else if (mode == Mode.CalibratingRot)
        {
            //only freeze the x, y and z rotations
            _roomRB.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePosition;
        }
        else if (mode == Mode.Done)
        {
            //done, so send the player's information wrt to the room to the next scene
            // R: where the player is relative to the room's centre (which is not 0 anymore),
            // how much the player is rotated relatvie to Quaternion.Identity
            // how much the room is rotated relative to Quaternion.Identity
            _send = new MyTransform(_playerCenterReference.transform.position - _room.transform.position,
            _playerCenterReference.transform.eulerAngles, _room.transform.eulerAngles);
            vision = Vision.Normal;

            if (_doneEvent != null)
                _doneEvent.Invoke(_send);

            //stop ability to calibrate
            _canCalibrate = false;
        }
        else
        {
            Debug.Log("Error with ModeChanged(), mode not set properly");
        }
    }

    private void VisionChanged()
    {
        if (vision == Vision.Normal)
        {
            foreach (Material m in _seeThroughInPassThroughMaterials)
            {
                Color c = m.color;
                c.a = 1;
                m.color = c;
            }
            foreach (Material m in _partialSeeThroughInPassThroughMaterials)
            {
                Color c = m.color;
                c.a = 1;
                m.color = c;
            }
            passThrough.color = UI_NOT_SELECTED;
            //_ARSession.enabled = false;
        }
        else if (vision == Vision.Passthrough)
        {
            //passthrough layer is always active

            foreach (Material m in _seeThroughInPassThroughMaterials)
            {
                Color c = m.color;
                c.a = 0;
                m.color = c;
            }
            foreach (Material m in _partialSeeThroughInPassThroughMaterials)
            {
                Color c = m.color;
                c.a = 0.5f;
                m.color = c;
            }
            passThrough.color = UI_SELECTED;
            //_ARSession.enabled = true;
        }
        else
        {
            Debug.Log("Problem with setting up vision change");
        }
    }

    /*    private void ToggleOwnership(bool val)
        {
            _listOfChildren.Clear();
            GetChildRecursive(_room.transform);
            if (val)
            {
                if (_rtView) _rtView.RequestOwnership();
                if (_rtTransform) _rtTransform.RequestOwnership();
                foreach (Transform t in _listOfChildren)
                {
                    if (t.TryGetComponent<RealtimeView>(out RealtimeView rTV))
                        rTV.RequestOwnership();
                    if (t.TryGetComponent<RealtimeTransform>(out RealtimeTransform rTT))
                        rTT.RequestOwnership();
                }
            }
            else
            {
                _rtView.ClearOwnership();
                _rtTransform.ClearOwnership();
                foreach (Transform t in _listOfChildren)
                {
                    if (t.TryGetComponent<RealtimeView>(out RealtimeView rTV))
                        rTV.ClearOwnership();
                    if (t.TryGetComponent<RealtimeTransform>(out RealtimeTransform rTT))
                        rTT.ClearOwnership();
                }
            }  
        }*/

    //private void GetChildRecursive(Transform obj)
    //{
    //    if (null == obj)
    //        return;

    //    foreach (Transform child in obj)
    //    {
    //        if (null == child)
    //            continue;

    //        if (child != obj)
    //        {
    //            _listOfChildren.Add(child);
    //        }
    //        GetChildRecursive(child);
    //    }
    //}

    private void ToggleModeUI()
    {
        foreach (TMP_Text mode in _modes)
        {
            mode.color = UI_NOT_SELECTED;
        }
            
        if ((int)mode < _modes.Length)
        {
            _modes[(int)mode].color = UI_SELECTED;
        }
            
    }

    private void OnDisable()
    {
        menuButton.performed -= DoneAction; menuButton.Disable();
        leftHandPB.performed -= VisionToggleAction; leftHandPB.Disable();
        leftHandSB.performed -= LeftRotateAction; leftHandSB.Disable();
        leftJS.performed -= VPositioningAction; leftJS.Disable();
        rightHandPB.performed -= PositionToggleAction; rightHandPB.Disable();
        rightHandSB.performed -= RightRotateAction; rightHandSB.Disable();
        rightJS.performed -= HPositioningAction; rightJS.performed -= VPositioningAction; rightJS.Disable();
        trigger.performed -= TriggerPressed; trigger.canceled -= TriggerReleased; trigger.Disable();
        Debug.Log("OnDisable");
    }

    /*    private void SaveTransform()
        {
            if (_ignoresTransform == null || _ignoresTransform.Count <= 0 || _ignoresTransform.Count < _ignores.Count)
            {
                _ignoresTransform = new List<Transform>();
                for (int i = 0; i < _ignores.Count; i++)
                    _ignoresTransform.Add(_ignores[i]);
            }
            else
            {
                for (int i = 0; i < _ignores.Count; i++)
                    _ignoresTransform[i] = _ignores[i];
            }
        }

        private void SetTransform()
        {
            if (_ignoresTransform == null || _ignoresTransform.Count < _ignores.Count)
                SaveTransform();

            for (int i = 0; i < _ignores.Count; i++)
                _ignores[i].SetPositionAndRotation(_ignoresTransform[i].position, _ignoresTransform[i].rotation);
        }

        private void SetLocalTransform()
        {
            if (_ignoresTransform == null || _ignoresTransform.Count < _ignores.Count)
                SaveTransform();

            for (int i = 0; i < _ignores.Count; i++)
                _ignores[i].SetLocalPositionAndRotation(_ignoresTransform[i].localPosition, _ignoresTransform[i].localRotation);
        }*/
}

[System.Serializable]
public class MyLoadSceneEvent : UnityEvent<MyTransform>
{
}

public struct MyTransform
{
    public MyTransform(Vector3 pos, Vector3 eul)
    {
        position = pos;
        eulerAngles = eul;
        rotAbt = Vector3.zero;
    }

    public MyTransform(Vector3 pos, Vector3 eul, Vector3 rotA)
    {
        position = pos;
        eulerAngles = eul;
        rotAbt = rotA;
    }

    public Vector3 position { get; }
    public Vector3 eulerAngles { get; }
    public Vector3 rotAbt { get; }
    public override String ToString()
    {
        return $"Position: {position} EulerAngles: {eulerAngles} RotateAbout: {rotAbt}";
    }
}
