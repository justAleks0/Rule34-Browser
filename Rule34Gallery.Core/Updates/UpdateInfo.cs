namespace Rule34Gallery.Core.Updates;

public sealed class UpdateInfo
{
    public string Version { get; init; } = string.Empty;

    public string DownloadUrl { get; init; } = string.Empty;

    public string ReleaseNotes { get; init; } = string.Empty;

    public string HtmlUrl { get; init; } = string.Empty;
}
