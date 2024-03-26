using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RealtimeModel]
public partial class AvatarSyncModel
{
    [RealtimeProperty(1, false, true)]
    private string _avatarData;
}
