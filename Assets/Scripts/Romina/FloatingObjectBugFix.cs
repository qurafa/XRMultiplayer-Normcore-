using com.perceptlab.armultiplayer;
using MixedReality.Toolkit.SpatialManipulation;
using Normal.Realtime;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using static Normal.Realtime.Realtime;
using static UnityEngine.Rendering.DebugUI;

[RequireComponent(typeof(ObjectManipulator), typeof(RealtimeView))]
public class FlaotingObjectBugFix : MonoBehaviour
{
    private Rigidbody _rigidbody;
    private ObjectManipulator _objectManipulator;
    private RealtimeView _realtimeView;
    [SerializeField] private float waitSeconds = 0.3f;


    // Start is called before the first frame update
    void Start()
    {
        _objectManipulator = GetComponent<ObjectManipulator>();
        _rigidbody = GetComponent<Rigidbody>();
        _realtimeView = GetComponent<RealtimeView>();
    }

    void Update()
    {
        // to solve the floating bug faced with MRTK manipulator when object is shared over the network
        if (_realtimeView.isOwnedLocallyInHierarchy && _rigidbody != null && !_objectManipulator.IsGrabSelected.Active) 
        {
            if (!_rigidbody.isKinematic && _rigidbody.useGravity == false && Time.time-_objectManipulator.IsGrabSelected.EndTime>waitSeconds)
            {
                RLogger.Log("Shared object with Rigidbody is not grabbed and has and no gravity. Setting Gravity by hand");
                    _rigidbody.useGravity = true;
            }
        }
    }
}
