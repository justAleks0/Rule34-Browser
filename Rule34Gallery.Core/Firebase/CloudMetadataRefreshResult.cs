namespace Rule34Gallery.Core.Firebase;

public sealed class CloudMetadataRefreshResult
{
    public int Updated { get; init; }

    public int Failed { get; init; }

    public int Total { get; init; }

    public string Summary =>
        Total == 0
            ? "No posts to refresh."
            : $"Updated {Updated} of {Total}" + (Failed > 0 ? $"; {Failed} failed" : string.Empty) + ".";
}
