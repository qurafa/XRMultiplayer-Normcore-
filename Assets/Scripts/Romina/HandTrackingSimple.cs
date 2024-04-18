/****************************************************
 * 
 * MRTK3 Handtracking documentation:
 * https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk3-input/packages/input/hand-tracking
 * 
 * **************************************************/

using MixedReality.Toolkit;
using MixedReality.Toolkit.Subsystems;
using UnityEngine;
using UnityEngine.XR;

public class HandTrackingSimple : MonoBehaviour
{

    HandsAggregatorSubsystem aggregator;
    [Tooltip("The GameObject to put where the index finger is.")]
    [SerializeField] GameObject indexFinger;

    void Start()
    {
        indexFinger.SetActive(false);
    }

    private void Update()
    {
        if (aggregator == null)
        {
            aggregator = XRSubsystemHelpers.GetFirstRunningSubsystem<HandsAggregatorSubsystem>();
        }
        //we check aggregator!= null because it might take time for the Subsystem to become available,
        //I believe once it's available it remains available, so we won't try to get it anymore
        if (aggregator != null)
        {
            HandJointPose jointPose;
            //TrackedHandJoin enum has 26 different joint/palm/writs positions with their names
            if (aggregator.TryGetJoint(TrackedHandJoint.IndexTip, XRNode.LeftHand, out jointPose))
            {
                indexFinger.SetActive(true);
                indexFinger.transform.position = jointPose.Position;
                indexFinger.transform.rotation = jointPose.Rotation;
            }
            else
            {
                indexFinger.SetActive(false);
            }
        }
    }
}
