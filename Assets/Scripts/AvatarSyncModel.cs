[RealtimeModel]
public partial class AvatarSyncModel
{
    // It's just the data you want to send
    [RealtimeProperty(1, false, true)]
    private string _avatarData;
}
