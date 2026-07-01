namespace Rule34Gallery.Core.Updates;

/// <summary>GitHub Releases source for in-app updates. Edit owner/repo to match your published release repo.</summary>
public static class UpdateCatalog
{
    public const string GitHubOwner = "justAleks0";

    public const string GitHubRepo = "Rule34-Browser";

    public const string WindowsZipAsset = "Rule34Gallery-win-x64.zip";

    public const string AndroidApkAsset = "R34Browser.apk";

    public static string LatestReleaseApiUrl =>
        $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";

    public static string WindowsDownloadUrl =>
        $"https://github.com/{GitHubOwner}/{GitHubRepo}/releases/latest/download/{WindowsZipAsset}";

    public static string AndroidDownloadUrl =>
        $"https://github.com/{GitHubOwner}/{GitHubRepo}/releases/latest/download/{AndroidApkAsset}";
}
