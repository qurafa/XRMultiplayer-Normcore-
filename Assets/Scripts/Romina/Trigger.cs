using com.perceptlab.armultiplayer;
using UnityEngine;

public class Trigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        RLogger.Log("Trigger Entered with" + other.name);
    }
}
