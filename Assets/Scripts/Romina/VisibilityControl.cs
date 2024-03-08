using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class VisibilityControl : MonoBehaviour
{
    [SerializeField] private bool _visible = true;

    private void Awake()
    {
        setVisible(_visible);
    }
    public void setVisible(bool visible)
    {
        foreach (MeshRenderer mr in GetComponentsInChildren<MeshRenderer>())
        {
            mr.enabled = visible;
        }
        _visible = visible;
    }
}
