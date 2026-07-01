namespace Rule34Gallery.Core.CloudSync;

public sealed class SyncDeviceInfo
{
    public string DeviceId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Platform { get; set; } = string.Empty;

    public string AppVersion { get; set; } = string.Empty;

    public long LastSeenAtUnix { get; set; }

    public long LastSyncAtUnix { get; set; }
}
