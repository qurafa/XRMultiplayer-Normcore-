using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class SetAnchorAtStart : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        gameObject.AddComponent<ARAnchor>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
