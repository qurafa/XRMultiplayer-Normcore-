using Normal.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkPlayerManager : MonoBehaviour
{
    [SerializeField]
    private RealtimeView rtView;

    private DataManager m_dataManager;
    private bool _Init = false;

    // Update is called once per frame
    void Update()
    {
        if(!_Init)
            Init();
    }

    private void Init()
    {
        if (rtView == null)
            return;

        transform.name = rtView.ownerIDSelf.ToString();

        if (rtView.isOwnedLocallySelf)
        {
            //REQUEST OWNERSHIP OF EACH CHILD REALTIMEVIEW
            foreach (RealtimeView childRTView in GetComponentsInChildren<RealtimeView>())
            {
                childRTView.RequestOwnershipOfSelfAndChildren();
            }
        }

        m_dataManager = FindObjectOfType<DataManager>();
        m_dataManager.CreatePlayerFile(rtView.ownerIDInHierarchy);

        _Init = true;
    }
}
