using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;



public class FollowController : MonoBehaviour
{
    enum Hand { Left, Right };

    [SerializeField]
    Hand hand;
    [SerializeField]
    ActionBasedController Controller;
    [SerializeField]
    Transform VisualMarker;
    private void Start()
    {
        if (VisualMarker == null)
        {
            VisualMarker = transform.Find("VisualWithOffset");
            if (VisualMarker == null)
            {
                VisualMarker = transform;
            }
        }
        if (Controller == null)
        {
            Debug.LogError("[FollowController] does not have a controller assinged"); //TODO should be able to find the controller in script, do later to make the code cleaner
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Controller != null)
        {
            if (VisualMarker != null && !VisualMarker.gameObject.activeInHierarchy)
            {
                VisualMarker.gameObject.SetActive(true);
            }
            transform.position = Controller.transform.position;
            transform.rotation = Controller.transform.rotation;
        }
        else if (VisualMarker != null && VisualMarker.gameObject.activeInHierarchy)
        {
            VisualMarker.gameObject.SetActive(false);
        }
    }
}
