namespace Rule34Gallery.Core.CloudSync;

public sealed class CloudSyncDiff
{
    public List<SyncDiffItem> Items { get; set; } = [];

    public int LocalOnlyCount => Items.Count(i => i.Status == SyncNodeStatus.LocalOnly);

    public int CloudOnlyCount => Items.Count(i => i.Status == SyncNodeStatus.CloudOnly);

    public int ChangedCount => Items.Count(i => i.Status is SyncNodeStatus.Changed or SyncNodeStatus.Conflict);
}

public sealed class SyncDiffItem
{
    public string Id { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public SyncDataCategory Category { get; set; }

    public string? ParentId { get; set; }

    public SyncNodeStatus Status { get; set; }

    public bool IsSelected { get; set; } = true;

    public string Detail { get; set; } = string.Empty;
}
