using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rule34Gallery.Core.Remote;

public static class RemoteProtocol
{
    public const int Version = 1;
    public const int DefaultPort = 8765;
    public const string TokenHeader = "X-Remote-Token";

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}

public static class RemoteCommands
{
    public const string Search = "search";
    public const string SetIncludeTags = "setIncludeTags";
    public const string AddIncludeTag = "addIncludeTag";
    public const string RemoveIncludeTag = "removeIncludeTag";
    public const string ClearIncludeTags = "clearIncludeTags";
    public const string GoToPage = "goToPage";
    public const string FocusBrowse = "focusBrowse";
    public const string NavigatePage = "navigatePage";
    public const string PageNext = "pageNext";
    public const string PagePrev = "pagePrev";
    public const string OpenPost = "openPost";
    public const string ViewerNext = "viewerNext";
    public const string ViewerPrev = "viewerPrev";
    public const string ViewerClose = "viewerClose";
    public const string ViewerPlayPause = "viewerPlayPause";
    public const string ViewerPlay = "viewerPlay";
    public const string ViewerPause = "viewerPause";
    public const string ViewerSeekRelative = "viewerSeekRelative";
    public const string ViewerSeekTo = "viewerSeekTo";
    public const string ViewerToggleMute = "viewerToggleMute";
    public const string ViewerSetMuted = "viewerSetMuted";
    public const string ViewerSetVolume = "viewerSetVolume";
    public const string ViewerSetSpeed = "viewerSetSpeed";
    public const string SetBrowseLayout = "setBrowseLayout";
    public const string FeedNext = "feedNext";
    public const string FeedPrev = "feedPrev";
    public const string LibrarySelectTab = "librarySelectTab";
    public const string LibrarySelectList = "librarySelectList";
    public const string LibraryRefreshMetadata = "libraryRefreshMetadata";
    public const string LocalSelectLibrary = "localSelectLibrary";
    public const string LocalSelectTopFilter = "localSelectTopFilter";
    public const string LocalSelectLeafFilter = "localSelectLeafFilter";
    public const string DownloadsClearFinished = "downloadsClearFinished";
}

public sealed class RemotePingResponse
{
    public int Version { get; init; } = RemoteProtocol.Version;

    public string App { get; init; } = "Rule34Gallery";

    public string MachineName { get; init; } = Environment.MachineName;

    public bool RemoteEnabled { get; init; }

    public bool RequiresToken { get; init; }
}

public sealed class RemotePairRequest
{
    public string Pin { get; init; } = string.Empty;
}

public sealed class RemotePairResponse
{
    public bool Ok { get; init; }

    public string? Error { get; init; }

    public string? Host { get; init; }

    public int Port { get; init; }

    public string? Token { get; init; }

    public string? ConnectionString { get; init; }

    public static RemotePairResponse Success(string host, int port, string token) =>
        new()
        {
            Ok = true,
            Host = host,
            Port = port,
            Token = token,
            ConnectionString = new RemotePairingPayload
            {
                Host = host,
                Port = port,
                Token = token,
            }.ToConnectionString(),
        };

    public static RemotePairResponse Fail(string error) => new() { Ok = false, Error = error };
}

public sealed class RemoteCommandRequest
{
    public string Type { get; init; } = string.Empty;

    public List<string>? Tags { get; init; }

    public string? Tag { get; init; }

    public int? PostId { get; init; }

    /// <summary>1-based page number for <see cref="RemoteCommands.GoToPage"/>.</summary>
    public int? Page { get; init; }

    /// <summary>Seconds value used by seek and playback commands.</summary>
    public double? Seconds { get; init; }

    /// <summary>Volume percentage (0..100) for <see cref="RemoteCommands.ViewerSetVolume"/>.</summary>
    public int? Volume { get; init; }

    /// <summary>Muted flag for <see cref="RemoteCommands.ViewerSetMuted"/>.</summary>
    public bool? Muted { get; init; }

    /// <summary>Playback speed multiplier (for example 0.5, 1.0, 1.5, 2.0).</summary>
    public double? Speed { get; init; }

    /// <summary><c>grid</c> or <c>feed</c> for <see cref="RemoteCommands.SetBrowseLayout"/>.</summary>
    public string? Layout { get; init; }

    /// <summary>App section for <see cref="RemoteCommands.NavigatePage"/> (browse, library, local, downloads, settings, account).</summary>
    public string? Section { get; init; }

    /// <summary>Library tab (favorites, watchLater, lists) for <see cref="RemoteCommands.LibrarySelectTab"/>.</summary>
    public string? Tab { get; init; }

    /// <summary>Firebase list id for <see cref="RemoteCommands.LibrarySelectList"/>.</summary>
    public string? ListId { get; init; }

    /// <summary>Local library id for <see cref="RemoteCommands.LocalSelectLibrary"/>.</summary>
    public string? LibraryId { get; init; }

    /// <summary>Category filter label for local filter commands.</summary>
    public string? Filter { get; init; }
}

public sealed class RemoteCommandResponse
{
    public bool Ok { get; init; }

    public string? Error { get; init; }

    public RemoteStateSnapshot? State { get; init; }

    public static RemoteCommandResponse Success(RemoteStateSnapshot? state = null) =>
        new() { Ok = true, State = state };

    public static RemoteCommandResponse Fail(string error) =>
        new() { Ok = false, Error = error };
}

public sealed class RemoteStateSnapshot
{
    public int Page { get; init; }

    public string Status { get; init; } = string.Empty;

    public IReadOnlyList<string> IncludeTags { get; init; } = [];

    public bool ViewerOpen { get; init; }

    public int? ViewerPostId { get; init; }

    public int ViewerIndex { get; init; }

    public RemoteViewerPlaybackState ViewerPlayback { get; init; } = new();

    public IReadOnlyList<RemotePostSummary> Posts { get; init; } = [];

    public bool RemoteClientActive { get; init; }

    public bool FeedModeAvailable { get; init; }

    public string BrowseLayout { get; init; } = "grid";

    public int FeedIndex { get; init; }

    public int FeedPostCount { get; init; }

    public string ActivePage { get; init; } = "browse";

    public RemoteLibraryState Library { get; init; } = new();

    public RemoteLocalState Local { get; init; } = new();

    public RemoteDownloadsState Downloads { get; init; } = new();
}

public sealed class RemoteLibraryState
{
    public bool SignedIn { get; init; }

    /// <summary>favorites, watchLater, or lists.</summary>
    public string Tab { get; init; } = "favorites";

    public string? SelectedListId { get; init; }

    public string? SelectedListName { get; init; }

    public IReadOnlyList<RemoteListSummary> Lists { get; init; } = [];
}

public sealed class RemoteListSummary
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}

public sealed class RemoteLocalState
{
    public IReadOnlyList<RemoteLocalLibrarySummary> Libraries { get; init; } = [];

    public string? SelectedLibraryId { get; init; }

    public string? SelectedLibraryName { get; init; }

    public IReadOnlyList<string> TopFilters { get; init; } = [];

    public IReadOnlyList<string> LeafFilters { get; init; } = [];

    public string TopFilter { get; init; } = "All";

    public string LeafFilter { get; init; } = "All";

    public int PostCount { get; init; }
}

public sealed class RemoteLocalLibrarySummary
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}

public sealed class RemoteDownloadsState
{
    public int ActiveCount { get; init; }

    public int FinishedCount { get; init; }

    public int TotalCount { get; init; }

    public string Summary { get; init; } = string.Empty;
}

public sealed class RemoteViewerPlaybackState
{
    public bool Active { get; init; }

    public bool IsPlaying { get; init; }

    public bool IsMuted { get; init; }

    public int Volume { get; init; }

    public double Speed { get; init; } = 1.0;

    public double PositionSeconds { get; init; }

    public double DurationSeconds { get; init; }
}

public sealed class RemotePostSummary
{
    public int Id { get; init; }

    public string PreviewUrl { get; init; } = string.Empty;

    public string? Rating { get; init; }
}

public sealed class RemotePairingPayload
{
    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = RemoteProtocol.DefaultPort;

    public string Token { get; init; } = string.Empty;

    public string ToConnectionString() =>
        $"r34remote://{Host}:{Port}?token={Uri.EscapeDataString(Token)}";

    public static bool TryParse(string text, out RemotePairingPayload payload)
    {
        payload = new RemotePairingPayload();
        var trimmed = text.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return false;
        }

        if (trimmed.StartsWith("r34remote://", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                return false;
            }

            var token = ParseQueryToken(uri.Query);
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            payload = new RemotePairingPayload
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : RemoteProtocol.DefaultPort,
                Token = token,
            };
            return !string.IsNullOrWhiteSpace(payload.Host);
        }

        try
        {
            var json = JsonSerializer.Deserialize<RemotePairingPayload>(trimmed, RemoteProtocol.JsonOptions);
            if (json is not null && !string.IsNullOrWhiteSpace(json.Host) && !string.IsNullOrWhiteSpace(json.Token))
            {
                payload = new RemotePairingPayload
                {
                    Host = json.Host,
                    Port = json.Port > 0 ? json.Port : RemoteProtocol.DefaultPort,
                    Token = json.Token,
                };
                return true;
            }
        }
        catch
        {
            // not json
        }

        return false;
    }

    private static string? ParseQueryToken(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return null;
        }

        var q = query.TrimStart('?');
        foreach (var part in q.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0].Equals("token", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(kv[1]);
            }
        }

        return null;
    }
}

public static class RemoteNetworkHelper
{
    public static IReadOnlyList<string> GetLocalIPv4Addresses()
    {
        var results = new List<string>();
        try
        {
            foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                {
                    continue;
                }

                if (nic.NetworkInterfaceType is System.Net.NetworkInformation.NetworkInterfaceType.Loopback
                    or System.Net.NetworkInformation.NetworkInterfaceType.Tunnel)
                {
                    continue;
                }

                foreach (var uni in nic.GetIPProperties().UnicastAddresses)
                {
                    if (uni.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        var ip = uni.Address.ToString();
                        if (!ip.StartsWith("169.254.", StringComparison.Ordinal))
                        {
                            results.Add(ip);
                        }
                    }
                }
            }
        }
        catch
        {
            // ignore
        }

        return results.Distinct().ToList();
    }
}

public static class RemoteTokenGenerator
{
    public static string CreateToken() => Guid.NewGuid().ToString("N");
}
