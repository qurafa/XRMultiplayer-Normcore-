using Normal.Realtime;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine;
using System;
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
    protected int m_OwnerID = 0;

    private bool stopTracking;
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

    private DataManager m_DataManager;
    //add a listener to the selectEntered so it requests ownership when the object is grabbed
    private void OnEnable()
    {
        m_DataManager = FindAnyObjectByType<DataManager>();
        m_RealtimeView = GetComponent<RealtimeView>();
        m_GrabInteractable = GetComponent<XRGrabInteractable>();
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (m_GrabInteractable != null)
        {
            m_GrabInteractable.selectEntered.AddListener(OnGrab);
            m_GrabInteractable.selectExited.AddListener(OnRelease);
        }
        SetUp();
    }

    private void FixedUpdate()
    {
        if (!tracking) return;

        if (stopTrackCount >= stopTrackLimit)
        {
            m_DataManager.RemoveObjectTrack(gameObject);
            tracking = false;
            gameObject.SetActive(false);
        }

        if (stopTracking)
            stopTrackCount += Time.deltaTime;
    }

    private void SetUp()
    {
        SetUpColliders(transform);
        m_DataManager.AddObjectTrack(gameObject);
    }

    private void SetUpColliders(Transform o)
    {
        if(o.TryGetComponent<Collider>(out Collider col) && !m_GrabInteractable.colliders.Contains(col))
            m_GrabInteractable.colliders.Add(col);

        foreach(Transform child in o)
            SetUpColliders(child);

        return;
    }

    /// <summary>
    /// Get the status of the object with respect to the box
    /// </summary>
    /// <returns>The status of the object with respect to the box</returns>
    public string GetStatus()
    {
        return _statusWRTBox;
    }

    public int GetOwnerID()
    {
        return m_OwnerID;
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        m_Rigidbody.constraints = RigidbodyConstraints.None;
        m_Rigidbody.drag = 0;
        m_Rigidbody.angularDrag = 0.05f;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
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

    private void OnDestroy()
    {
        tracking = false;
    }
}
