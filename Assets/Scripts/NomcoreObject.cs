using Normal.Realtime;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(XRGrabInteractable), typeof(RealtimeTransform), typeof(ObjectModelImpl))]
public class NomcoreObject : MonoBehaviour
{
    [SerializeField]
    private ObjectModelImpl objImpl;
    [SerializeField]
    AudioSource _audioSource;
    /// <summary>
    /// Whether to track this object or not
    /// </summary>
    [SerializeField]
    private bool tracking = false;

    //private HashSet<GameObject> colliders;

    private bool stopTracking = false;
    private bool grabbing = false;
    private readonly float touchingLimit = 0.5f;
    private float touchingCount = 0;
    private float releaseLimit = 1.5f;
    private float releaseCount = 0;
    private float stopTrackCount = 0;
    private float stopTrackLimit = 1.5f;

    /// <summary>
    /// Status of the object with reference to the box.
    /// Either "Outside Box" or "Inside Box" or "Entering {hole name}"
    /// </summary>
    private string _statusWRTBox = "Outside Box";

    private Rigidbody m_Rigidbody;

    private RealtimeView m_RealtimeView;
    private XRGrabInteractable m_GrabInteractable;

    //add a listener to the selectEntered so it requests ownership when the object is grabbed
    private void OnEnable()
    {
        m_RealtimeView = GetComponent<RealtimeView>();
        m_GrabInteractable = GetComponent<XRGrabInteractable>();
        m_Rigidbody = GetComponent<Rigidbody>();

        if (objImpl == null) objImpl = GetComponent<ObjectModelImpl>();

        if (m_GrabInteractable != null)
        {
            m_GrabInteractable.selectEntered.AddListener(OnGrab);
            m_GrabInteractable.selectExited.AddListener(OnRelease);
        }
    }

    private void Start()
    {
        //colliders = new HashSet<GameObject>();
        objImpl.UpdateTS(0);//set to idle tracking state
        SetUp();
    }

    private void FixedUpdate()
    {
        if (!tracking) return;

        if (touchingCount >= touchingLimit)
        {
            objImpl.UpdateTS(1);//set to tracking when touching/holding
        }
        else if (releaseCount >= releaseLimit)
        {
            objImpl.UpdateTS(0);//set to idle when released
        }

        if (stopTrackCount >= stopTrackLimit)
        {
            objImpl.UpdateTS(2);//set to stop tracking when touches the bottom
            tracking = false;
        }

        if (grabbing)
            touchingCount += Time.deltaTime;
        else
            releaseCount += Time.deltaTime;

        if (stopTracking)
            stopTrackCount += Time.deltaTime;
    }

    private void SetUp()
    {
        SetUpColliders(transform);
    }

    private void SetUpColliders(Transform o)
    {
        if(o.TryGetComponent<Collider>(out Collider col) && !m_GrabInteractable.colliders.Contains(col))
            m_GrabInteractable.colliders.Add(col);

        foreach(Transform child in o)
            SetUpColliders(child);

        return;
    }

    private void ClearOwnership(SelectExitEventArgs arg0)
    {
        
    }

    private void RequestOwnership(SelectEnterEventArgs args)
    {
        m_RealtimeView.RequestOwnershipOfSelfAndChildren();
    }

    /// <summary>
    /// Get the status of the object with respect to the box
    /// </summary>
    /// <returns>The status of the object with respect to the box</returns>
    public string GetStatus()
    {
        if (this.CompareTag("Box")) return grabbing ? "Grabbing" : "Released";
        else return _statusWRTBox;
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        m_RealtimeView.RequestOwnershipOfSelfAndChildren();
        grabbing = true;
        releaseCount = 0;

        m_Rigidbody.constraints = RigidbodyConstraints.None;
        m_Rigidbody.drag = 0;
        m_Rigidbody.angularDrag = 0.05f;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        grabbing = false;
        touchingCount = 0;

        m_Rigidbody.constraints = RigidbodyConstraints.None;
        m_Rigidbody.drag = 0;
        m_Rigidbody.angularDrag = 0.05f;
    }

/*    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($".....................Tag entered: {collision.collider.tag}, Object Entered: {collision.collider.name}");

        if (collision.collider.CompareTag("GrabCol"))
        {
            colliders?.Add(collision.collider.gameObject);
            releaseCount = 0;
            m_rigidbody.useGravity = false;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.collider.CompareTag("GrabCol"))
        {
            if (!colliders.Contains(collision.collider.gameObject))
            {
                colliders?.Add(collision.collider.gameObject);
                m_rigidbody.useGravity = false;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.collider.CompareTag("GrabCol"))
        {
            colliders?.Remove(collision.collider.gameObject);
            if (colliders.Count <= 0)
            {
                touchingCount = 0;
                m_rigidbody.useGravity = true;
            }
        }
    }*/

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Trigger"))
        {
            if (other.name.Equals("Box")) _statusWRTBox = "Inside Box";
            else _statusWRTBox = $"Entering {other.name}";
        }
        if (this.CompareTag("Shape") && other.CompareTag("Bottom"))
        {
            stopTracking = true;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Trigger"))
        {
            if (other.name.Equals("Box")) _statusWRTBox = "Inside Box";
            else _statusWRTBox = $"Entering {other.name}";
        }
        if (this.CompareTag("Shape") && other.CompareTag("Bottom"))
        {
            stopTracking = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Trigger"))
        {
            _statusWRTBox = "Outside Box";
        }
        if (this.CompareTag("Shape") && other.CompareTag("Bottom"))
        {
            stopTracking = false;
            stopTrackCount = 0;
        }
    }

    private void OnDisable() 
    {
        m_GrabInteractable.selectEntered.RemoveListener(RequestOwnership);
        m_GrabInteractable.selectExited.RemoveListener(ClearOwnership);
    }

    private void OnDestroy()
    {
        objImpl.UpdateTS(2);
        tracking = false;
    }
}
