using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using Rule34Gallery.Core.Abstractions;
using Rule34Gallery.Core.Firebase;
using Rule34Gallery.Core.Updates;

namespace Rule34Gallery.Core.Services;

public sealed class AppServices
{
    private static AppServices? _current;

    public static AppServices Current => _current ?? throw new InvalidOperationException("AppServices not initialized. Call Initialize first.");

    public static void Initialize(IPlatformServices platform)
    {
        _current = new AppServices(platform);
    }

    private AppServices(IPlatformServices platform)
    {
        Platform = platform;
        Http = CreateHttpClient();
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("Rule34GalleryApp/2.0");
        Http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        Library = new UserLibraryService(
            FirebaseConfig.Load(),
            platform.GoogleSignIn,
            platform.SecureStore,
            platform.AppDataFolder);
        Messenger = new AppMessenger();
        Gallery = new GalleryCoordinator(this);
        Navigation = new NavigationHistoryService();
        Settings = platform.SettingsStore.Load() ?? new UserSettings();
        Downloads = new DownloadService(this);
        ForYou = new ForYouService(this);
        CloudSync = new CloudSyncService(this);
        Updates = new UpdateCheckService(Http);
        Settings.MigrateLegacyTagsIfNeeded();
        Library.Http = Http;
        Library.GetSettings = () => Settings;
        Library.SaveSettings = settings =>
        {
            Settings = settings;
            Platform.SettingsStore.Save(Settings);
        };
        Library.RecordFavoriteSignal = post => ForYou.RecordFavorite(post);
        Library.RecordWatchLaterSignal = post => ForYou.RecordWatchLater(post);
        Library.CloudCredentialsSynced += (_, _) =>
        {
            ForYou.ApplySettings(Settings);
            CredentialsSynced?.Invoke(this, EventArgs.Empty);
        };
        Library.AuthStateChanged += (_, _) =>
        {
            if (Library.IsSignedIn)
            {
                _ = ForYou.SyncFromCloudAsync();
            }
        };
    }

    public IPlatformServices Platform { get; }

    public HttpClient Http { get; }

    public UserLibraryService Library { get; }

    public AppMessenger Messenger { get; }

    public GalleryCoordinator Gallery { get; }

    /// <summary>Where the viewer was opened from; controls passive For You learning.</summary>
    public string ViewerLearningSource { get; set; } = ForYouLearningSources.Browse;

    public NavigationHistoryService Navigation { get; }

    public DownloadService Downloads { get; }

    public CloudSyncService CloudSync { get; }

    public UpdateCheckService Updates { get; }

    public ForYouService ForYou { get; }

    public UserSettings Settings { get; private set; }

    public event EventHandler? CredentialsSynced;

    public ObservableCollection<PostItem> Posts { get; } = [];

    public ObservableCollection<PostItem> LocalPosts { get; } = [];

    public ObservableCollection<TagSuggestion> Autocomplete { get; } = [];

    public ObservableCollection<TagSuggestion> BlacklistAutocomplete { get; } = [];

    public IImageCache ImageCache => Platform.ImageCache;

    public IMediaPlaybackCache MediaPlaybackCache => Platform.MediaPlaybackCache;

    public void ReloadSettings() => Settings = Platform.SettingsStore.Load() ?? new UserSettings();

    public void SaveSettings()
    {
        Platform.SettingsStore.Save(Settings);
        _ = Library.SaveCredentialsToCloudAsync();
        _ = Library.SaveSavedTagPresetsToCloudAsync();
        _ = ForYou.SaveToCloudAsync();
    }

    public Task<CloudSyncResult> SyncCloudNowAsync() => CloudSync.RunSyncAsync();

    public void SetCredentials(string userId, string apiKey, bool persist = true)
    {
        Settings.UserId = userId.Trim();
        Settings.ApiKey = apiKey;
        if (persist)
        {
            SaveSettings();
        }
        else if (Library.IsSignedIn)
        {
            _ = Library.SaveCredentialsToCloudAsync();
        }
    }

    public string[] LimitOptions { get; } = ["25", "50", "100", "200"];

    public string GetSelectedLimit()
    {
        var index = Settings.LimitIndex;
        if (index < 0 || index >= LimitOptions.Length)
        {
            index = 1;
        }

        return LimitOptions[index];
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        };

        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(60),
        };
    }
}
