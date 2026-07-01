namespace Rule34Gallery.Core.CloudSync;

public sealed class SyncSessionMeta
{
    public SyncSessionStatus Status { get; set; } = SyncSessionStatus.Idle;

    public long? LastSuccessAtUnix { get; set; }

    public long? LastAttemptAtUnix { get; set; }

    public string LastDeviceId { get; set; } = string.Empty;

    public string LastDeviceLabel { get; set; } = string.Empty;

    public string LastDirection { get; set; } = string.Empty;

    public string LastError { get; set; } = string.Empty;

    public string LastSummary { get; set; } = string.Empty;
}
