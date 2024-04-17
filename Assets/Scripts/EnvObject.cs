using Normal.Realtime;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine;
using System;
using System.Collections.Generic;
using com.perceptlab.armultiplayer;

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

    private Vector3 m_InitPosition;
    private Quaternion m_InitRotation;

    private bool stopTracking;
    private float stopTrackCount = 0;
    private float stopTrackLimit = 1.5f;

    /// <summary>
    /// Status of the object with reference to the box.
    /// Either "Outside Box" or "Inside Box" or "Entering {hole name}"
    /// </summary>
    private string _statusWRTBox = "Outside Box";

    private Rigidbody m_Rigidbody;
    private XRGrabInteractable m_GrabInteractable;

    private DataManager m_DataManager;
    //add a listener to the selectEntered so it requests ownership when the object is grabbed
    public virtual void OnEnable()
    {
        m_DataManager = FindAnyObjectByType<DataManager>();
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

    public virtual void SetUp()
    {
        SetUpColliders(transform);
        m_DataManager.AddObjectTrack(gameObject);
        m_InitPosition = transform.position;
        m_InitRotation = transform.rotation;
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

    public virtual void OnGrab(SelectEnterEventArgs args)
    {
        RLogger.Log("On Grab!!!!");
        m_Rigidbody.constraints = RigidbodyConstraints.None;
        _audioSource.Play();
        m_Rigidbody.drag = 0;
        m_Rigidbody.angularDrag = 0.05f;
    }

    public virtual void OnRelease(SelectExitEventArgs args)
    {
        m_Rigidbody.constraints = RigidbodyConstraints.None;
        m_Rigidbody.drag = 0;
        m_Rigidbody.angularDrag = 0.05f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        //_audioSource.Play();
        if (collision.gameObject.tag == "Floor")
        {
            transform.SetPositionAndRotation(m_InitPosition, m_InitRotation);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        RLogger.Log("on trigger enter");
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
        RLogger.Log("on trigger stay");
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
        RLogger.Log("on trigger exit");
        if (other.CompareTag("Trigger"))
        {
            _statusWRTBox = "Outside Box";
        }
        if (this.CompareTag("Shape") && other.CompareTag("Bottom"))
        {
            //stopTracking = false;
            //stopTrackCount = 0;
        }
    }

    private void OnDestroy()
    {
        tracking = false;
    }
}
