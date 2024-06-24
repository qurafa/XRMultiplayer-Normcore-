using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine;
using com.perceptlab.armultiplayer;
using System.Collections.Generic;

public class EnvObject : MonoBehaviour
{
    /// <summary>
    /// Whether to track this object or not
    /// </summary>
    [SerializeField]
    protected bool tracking = false;
    [SerializeField]
    protected AudioSource _audioSource;
    [SerializeField]
    protected int _ownerID = 0;

    private Vector3 _initPosition;
    private Quaternion _initRotation;

    private bool _stopTracking;
    private float _stopTrackCount = 0;
    private float _stopTrackLimit = 1.5f;

    /// <summary>
    /// Whether the EnvObject is colliding with a the posting box or not
    /// </summary>
    private bool _boxColliding = false;
    /// <summary>
    /// Time when the EnvObject collides with the box in total
    /// </summary>
    private float _boxColTime = 0;
    /// <summary>
    /// Time when the EnvObject collides with the box while entering or being posted into the box
    /// </summary>
    private float _boxColTimeWhilePosting = 0;
    /// <summary>
    /// Colliders interacting with the EnvObject
    /// </summary>
    private HashSet<GameObject> _boxTriggers;

    public enum StatusWRTBox
    {
        OutsideBox,
        EnteringBoxRight,
        EnteringBoxWrong,
        InsideBox
    }

    /// <summary>
    /// Status of the object with reference to the box.
    /// Either "Outside Box" or "Inside Box" or "Entering {hole name}"
    /// </summary>
    private StatusWRTBox _statusWRTBox = StatusWRTBox.OutsideBox;

    private Rigidbody _rigidbody;
    private XRGrabInteractable _grabInteractable;

    private DataManager _dataManager;
    //add a listener to the selectEntered so it requests ownership when the object is grabbed
    public virtual void OnEnable()
    {
        _dataManager = FindAnyObjectByType<DataManager>();
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.AddListener(OnGrab);
            _grabInteractable.selectExited.AddListener(OnRelease);
        }
        SetUp();
    }

    private void FixedUpdate()
    {
        //don't update stuff for the box for now
        if (CompareTag("Box") || !tracking) return;

        if (_stopTrackCount >= _stopTrackLimit)
        {
            _dataManager.RemoveObjectTrack(gameObject);
            tracking = false;
            gameObject.SetActive(false);
        }

        if (_stopTracking)
            _stopTrackCount += Time.deltaTime;

        if (_boxColliding)
        {
            _boxColTime += Time.deltaTime;
            if (_statusWRTBox == StatusWRTBox.EnteringBoxRight)
                _boxColTimeWhilePosting += Time.deltaTime;
        }
        //RLogger.Log($"{name} Box Col Time {_boxColTime}");
    }

    public virtual void SetUp()
    {
        SetUpColliders(transform);
        _dataManager.AddObjectTrack(gameObject);
        _initPosition = transform.position;
        _initRotation = transform.rotation;

        _boxTriggers = new HashSet<GameObject>();
    }

    private void SetUpColliders(Transform o)
    {
        if(o.TryGetComponent<Collider>(out Collider col) && !_grabInteractable.colliders.Contains(col))
            _grabInteractable.colliders.Add(col);

        foreach(Transform child in o)
            SetUpColliders(child);

        return;
    }

    /// <summary>
    /// Get the status of the object with respect to the box
    /// </summary>
    /// <returns>The status of the object with respect to the box</returns>
    public StatusWRTBox GetStatus()
    {
        return _statusWRTBox;
    }

    public int GetOwnerID()
    {
        return _ownerID;
    }

    public virtual void OnGrab(SelectEnterEventArgs args)
    {
        //RLogger.Log("On Grab!!!!");
        _rigidbody.constraints = RigidbodyConstraints.None;
        _audioSource.Play();
        _rigidbody.drag = 0;
        _rigidbody.angularDrag = 0.05f;
        _rigidbody.isKinematic = true;
    }

    public virtual void OnRelease(SelectExitEventArgs args)
    {
        _rigidbody.constraints = RigidbodyConstraints.None;
        _rigidbody.drag = 0;
        _rigidbody.angularDrag = 0.05f;
        _rigidbody.isKinematic = false;
    }

    private void ResetToStartPos()
    {
        transform.SetPositionAndRotation(_initPosition, _initRotation);
        //so it doesn't keep moving with it's current velocity
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        //so we release it even after reseting position
        if(_grabInteractable.isSelected)
            _grabInteractable.interactionManager.CancelInteractorSelection(_grabInteractable.firstInteractorSelecting);
    }

    private void OnCollisionEnter(Collision collision)
    {
        //_audioSource.Play();
        if (collision.collider.CompareTag("Floor"))
        {
            ResetToStartPos();//transform.SetPositionAndRotation(m_InitPosition, m_InitRotation);
        }
/*        if(collision.collider.CompareTag("BoxCollider"))
        {
            //Debug.Log($"BoxCollider OnCollisionEnter {collision.collider}");
            _boxColliding = true;
            _boxTriggers.Add(collision.gameObject);
        }*/
    }

    private void OnCollisionStay(Collision collision)
    {
        //_audioSource.Play();
        if (collision.collider.CompareTag("Floor"))
        {
            ResetToStartPos();//transform.SetPositionAndRotation(m_InitPosition, m_InitRotation);
        }
/*        if (collision.collider.CompareTag("BoxCollider"))
        {
            _boxColliding = true;
            _boxTriggers.Add(collision.gameObject);
        }*/
    }

    private void OnCollisionExit(Collision collision)
    {
/*        if (collision.collider.CompareTag("BoxCollider"))
        {
            _boxTriggers.Remove(collision.gameObject);
            if(_boxTriggers.Count <= 0)
                _boxColliding = false;
        }*/
    }

    private void OnTriggerEnter(Collider other)
    {
        //RLogger.Log($"on trigger enter {other.name}, Tag {other.tag}");
        if (other.CompareTag("Trigger"))
        {
            if (other.name.ToLower().Contains("reset")) ResetToStartPos();
            if (other.name.ToLower().Contains("box")) _statusWRTBox = StatusWRTBox.InsideBox;
            else if (other.name.ToLower().Contains("hole")) _statusWRTBox = StatusWRTBox.EnteringBoxRight;

            if (other.name.ToLower().Contains("bottom"))
            {
                Debug.Log("Stopping Tracking True");
                _stopTracking = true;
            }
        }
        else if (other.CompareTag("BoxTrigger"))
        {
            _boxColliding = true;
            _boxTriggers.Add(other.gameObject);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        //RLogger.Log("on trigger stay");
        if (other.CompareTag("Trigger"))
        {
            if (other.name.ToLower().Contains("box")) _statusWRTBox = StatusWRTBox.InsideBox;
            else if (other.name.ToLower().Contains("hole")) _statusWRTBox = StatusWRTBox.EnteringBoxRight;

            if (other.name.ToLower().Contains("bottom"))
            {
                Debug.Log("Stopping Tracking True");
                _stopTracking = true;
            }
                
        }
        else if (other.CompareTag("BoxTrigger"))
        {
            _boxColliding = true;
            _boxTriggers.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //RLogger.Log("on trigger exit");
        if (other.CompareTag("Trigger"))
        {
            _statusWRTBox = StatusWRTBox.OutsideBox;

            if (other.name.ToLower().Contains("bottom"))
            {
                Debug.Log("Stopping Tracking False");
                _stopTracking = false;
            }
        }
        if (other.CompareTag("BoxTrigger"))
        {
            _boxTriggers.Remove(other.gameObject);
            if (_boxTriggers.Count <= 0)
                _boxColliding = false;
        }
    }

    private void OnDestroy()
    {
        tracking = false;
    }
}
