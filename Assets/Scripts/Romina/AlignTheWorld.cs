using UnityEngine;
using UnityEngine.Events;

public abstract class AlignTheWorld : MonoBehaviour
{
    [SerializeField]
    public UnityEvent onDoneAlign;

    [SerializeField]
    public bool drawGUI = false;
}
