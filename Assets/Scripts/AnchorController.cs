using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class AnchorController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
        ARAnchor a = gameObject.AddComponent<ARAnchor>();
    }
}
