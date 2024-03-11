using Normal.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AvatarSyncImpl))]
public class AvatarSync : RealtimeComponent<AvatarSyncModel>
{
    [SerializeField]
    private AvatarSyncImpl m_AvatarSyncImpl;

    // Start is called before the first frame update
    void Start()
    {
        if (m_AvatarSyncImpl == null)
            m_AvatarSyncImpl = GetComponent<AvatarSyncImpl>();
    }

    protected override void OnRealtimeModelReplaced(AvatarSyncModel previousModel, AvatarSyncModel currentModel)
    {
        if (previousModel != null)
        {
            previousModel.avatarDataDidChange -= AvatarDataChanged;
        }

        if(currentModel != null)
        {
            if(model.isFreshModel)
                SetAvatarData("0|");//show nothing

            //then get what to show from Normcore
            UpdateAvatarData();

            currentModel.avatarDataDidChange += AvatarDataChanged;
        }
    }

    private void AvatarDataChanged(AvatarSyncModel model, string value)
    {
        UpdateAvatarData();
    }

    private void UpdateAvatarData()
    {
        if (model == null)
            return;
        if (model.avatarData == null || model.avatarData.Length == 0)
            return;

        Debug.Log($"getting avatar data....{model.avatarData}");
        m_AvatarSyncImpl.UpdateFromNormcore(model.avatarData);
    }

    public void SetAvatarData(string value)
    {
        //Debug.Log($"set avatar data....{value}");
        model.avatarData = value;
    }
}
