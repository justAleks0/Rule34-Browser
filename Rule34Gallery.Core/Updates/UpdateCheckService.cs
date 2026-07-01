using System.Net.Http.Headers;
using System.Text.Json;

namespace Rule34Gallery.Core.Updates;

public sealed class UpdateCheckService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;

    public UpdateCheckService(HttpClient http)
    {
        _http = http;
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync(
        string currentVersion,
        string platformAssetName,
        string? dismissedVersion = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedCurrent = SemVer.NormalizeTag(currentVersion);

        using var request = new HttpRequestMessage(HttpMethod.Get, UpdateCatalog.LatestReleaseApiUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Rule34Gallery", normalizedCurrent));

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        if (release is null || string.IsNullOrWhiteSpace(release.TagName))
        {
            return null;
        }

        var latestVersion = SemVer.NormalizeTag(release.TagName);
        if (!SemVer.IsNewer(latestVersion, normalizedCurrent))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(dismissedVersion) &&
            string.Equals(latestVersion, SemVer.NormalizeTag(dismissedVersion), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var downloadUrl = ResolveAssetUrl(release, platformAssetName)
                          ?? $"https://github.com/{UpdateCatalog.GitHubOwner}/{UpdateCatalog.GitHubRepo}/releases/latest/download/{platformAssetName}";

        return new UpdateInfo
        {
            Version = latestVersion,
            DownloadUrl = downloadUrl,
            ReleaseNotes = release.Body?.Trim() ?? string.Empty,
            HtmlUrl = release.HtmlUrl ?? string.Empty,
        };
    }

    private static string? ResolveAssetUrl(GitHubRelease release, string assetName)
    {
        foreach (var asset in release.Assets ?? [])
        {
            if (string.Equals(asset.Name, assetName, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl))
            {
                return asset.BrowserDownloadUrl;
            }
        }

        return null;
    }

    private sealed class GitHubRelease
    {
        public string? TagName { get; set; }

        public string? Body { get; set; }

        public string? HtmlUrl { get; set; }

        public List<GitHubAsset>? Assets { get; set; }
    }

    private sealed class GitHubAsset
    {
        public string? Name { get; set; }

        public string? BrowserDownloadUrl { get; set; }
    }
}
