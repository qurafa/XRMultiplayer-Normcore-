using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class AvatarSyncImpl : MonoBehaviour
{
    [SerializeField] Part _avatar;

    public enum Part
    {
        None,
        Head,
        LeftHand,
        RightHand,
        Other
    }

    public enum HandMode
    {
        Nome,
        Controller,
        HandTracking
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
