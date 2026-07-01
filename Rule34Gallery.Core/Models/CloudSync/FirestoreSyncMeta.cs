namespace Rule34Gallery.Core.CloudSync;

public sealed class FirestoreSyncMeta
{
    public long? LastFullSyncAtUnix { get; set; }

    public string LastSyncDeviceId { get; set; } = string.Empty;

    public string LastSyncDirection { get; set; } = string.Empty;

    public string LastSyncStatus { get; set; } = string.Empty;
}
