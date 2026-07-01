namespace Rule34Gallery.Core;

/// <summary>Persisted download record (links + metadata) for history and retry.</summary>
public sealed class DownloadHistoryEntry
{
    public string Id { get; set; } = string.Empty;

    public int PostId { get; set; }

    public string SourceUrl { get; set; } = string.Empty;

    public string FileUrl { get; set; } = string.Empty;

    public string SampleUrl { get; set; } = string.Empty;

    public string PreviewUrl { get; set; } = string.Empty;

    public string Tags { get; set; } = string.Empty;

    public string Rating { get; set; } = string.Empty;

    public int Score { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public string LibraryName { get; set; } = string.Empty;

    public string RelativeCategory { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string DestinationDirectory { get; set; } = string.Empty;

    public string DestinationPath { get; set; } = string.Empty;

    public string? SidecarPath { get; set; }

    public string? ErrorMessage { get; set; }

    public DownloadJobStatus Status { get; set; }

    public double Progress { get; set; }

    public string StatusText { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }
}
