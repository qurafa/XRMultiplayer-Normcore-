using Normal.Realtime;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class RealtimeEnvObject : EnvObject
{
    [Header("REALTIME")]
    [SerializeField]
    private RealtimeView m_RealtimeView;

    public override void OnEnable()
    {
        if(m_RealtimeView == null) m_RealtimeView = GetComponent<RealtimeView>();
        base.OnEnable();
    }

    public override void SetUp()
    {
        m_OwnerID = m_RealtimeView.ownerIDSelf;
        base.SetUp();
    }

    public override void OnGrab(SelectEnterEventArgs args)
    {
        m_RealtimeView.RequestOwnershipOfSelfAndChildren();
        m_OwnerID = m_RealtimeView.ownerIDSelf;
        base.OnGrab(args);
    }
}
