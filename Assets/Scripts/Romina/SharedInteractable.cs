using com.perceptlab.armultiplayer;
using Normal.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable), typeof(RealtimeView))]
public class SharedInteractable : MonoBehaviour
{
    void Awake()
    {
        RealtimeView _rv = GetComponent<RealtimeView>();
        gameObject.GetComponent<XRGrabInteractable>().selectEntered.AddListener((args) => _rv.RequestOwnershipOfSelfAndChildren());
        gameObject.GetComponent<XRGrabInteractable>().selectEntered.AddListener((args) => RLogger.Log(gameObject.name+" :selectEntered"));
    }

}
