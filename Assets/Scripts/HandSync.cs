using Normal.Realtime;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(HandSyncImpl))]
public class HandSync : RealtimeComponent<HandSyncModel>
{
    [SerializeField]
    private HandSyncImpl handImpl;

    private void Start()
    {
        if(handImpl == null)
            handImpl = GetComponent<HandSyncImpl>();
    }

/*    private HandSyncModel model
    {
        set
        {
            if (_handSync != null)
            {
                _handSync.handDataDidChange -= HandDataChanged;
            }

            _handSync = value;

            if (_handSync != null)
            {
                if (_handSync.handData != null)
                {
                    UpdateHandData();

                    _handSync.handDataDidChange += HandDataChanged;
                }
            }
        }
    }*/

    protected override void OnRealtimeModelReplaced(HandSyncModel previousModel, HandSyncModel currentModel)
    {
        if(previousModel != null) {
            previousModel.handDataDidChange -= HandDataChanged;
        }

        if(currentModel != null)
        {
            if (currentModel.isFreshModel)
                SetHandData("0|");

            UpdateHandData();

            currentModel.handDataDidChange += HandDataChanged;
        }
    }

    private void HandDataChanged(HandSyncModel model, string value)
    {
        UpdateHandData();
    }

/*    private void UpdateHandData()
    {
        Debug.Log("Updating from Normcore " + _handSync.handData);
        if (_handSync == null)
            return;

        if (_handSync.handData == "")
            return;
        
        handImpl.UpdateFromNormcore(_handSync.handData);
    }*/

    private void UpdateHandData()
    {
        if(model == null)
            return;

        if (model.handData == "")
            return;

        //Debug.Log("Updating from Normcore " + model.handData);
        handImpl.UpdateFromNormcore(model.handData);
    }

    public void SetHandData(string handData)
    {
        model.handData = handData;
    }
}
