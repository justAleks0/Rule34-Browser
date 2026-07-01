using System.Text.Json;
using Rule34Gallery.Core.Abstractions;
namespace Rule34Gallery.Core.Settings;

public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ISecureCredentialStore _secureStore;
    private readonly string _settingsPath;

    public JsonSettingsStore(ISecureCredentialStore secureStore, string appDataFolder)
    {
        _secureStore = secureStore;
        _settingsPath = Path.Combine(appDataFolder, "settings.json");
    }

    public UserSettings? Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return null;
            }

            var json = File.ReadAllText(_settingsPath);
            var stored = JsonSerializer.Deserialize<StoredSettings>(json, JsonOptions);
            if (stored is null)
            {
                return null;
            }

            var settings = new UserSettings
            {
                ActiveSource = ParseEnum(stored.ActiveSource, GallerySource.Rule34),
                UserId = stored.UserId ?? string.Empty,
                ApiKey = UnprotectApiKey(stored),
                DanbooruLogin = stored.DanbooruLogin ?? string.Empty,
                DanbooruApiKey = UnprotectDanbooruApiKey(stored),
                E621Username = stored.E621Username ?? string.Empty,
                E621ApiKey = UnprotectE621ApiKey(stored),
                FilterAi = stored.FilterAi,
                LimitIndex = stored.LimitIndex,
                Tags = stored.Tags ?? string.Empty,
                IncludeTags = stored.IncludeTags ?? [],
                BlacklistTags = stored.BlacklistTags ?? [],
                GlobalBlockedTags = stored.GlobalBlockedTags ?? [],
                ActiveBlacklistPresetIds = stored.ActiveBlacklistPresetIds ?? [],
                ActiveSearchPresetIds = stored.ActiveSearchPresetIds ?? [],
                RatingSafe = stored.RatingSafe,
                RatingQuestionable = stored.RatingQuestionable,
                RatingExplicit = stored.RatingExplicit,
                MediaFilter = ParseEnum(stored.MediaFilter, MediaFilterMode.All),
                SortMode = ParseEnum(stored.SortMode, SearchSortMode.ScoreDesc),
                MinScore = stored.MinScore,
                MinWidth = stored.MinWidth,
                MinHeight = stored.MinHeight,
                ArtistFilter = stored.ArtistFilter ?? string.Empty,
                CharacterFilter = stored.CharacterFilter ?? string.Empty,
                CopyrightFilter = stored.CopyrightFilter ?? string.Empty,
                SearchOptionsExpanded = stored.SearchOptionsExpanded,
                PlaybackMuted = stored.PlaybackMuted,
                PlaybackVolume = ClampVolume(stored.PlaybackVolume),
                PlaybackLoop = stored.PlaybackLoop,
                PlaybackSpeedIndex = ClampSpeedIndex(stored.PlaybackSpeedIndex),
                OpenAiApiKey = _secureStore.Unprotect(stored.OpenAiApiKeyProtected),
                OpenAiModel = string.IsNullOrWhiteSpace(stored.OpenAiModel) ? "gpt-4o-mini" : stored.OpenAiModel.Trim(),
                UseOpenAiForDescribeSearch = stored.UseOpenAiForDescribeSearch,
                ForYouEnabled = stored.ForYouEnabled || stored.EnableForYouLearning,
                ForYouLearnArtists = stored.ForYouLearnArtists ?? true,
                ForYouLearnSeries = stored.ForYouLearnSeries ?? true,
                ForYouLearnMinorTags = stored.ForYouLearnMinorTags ?? false,
                ForYouCloudSync = stored.ForYouCloudSync || stored.EnableForYouCloudSync,
                UseOpenAiForForYou = stored.UseOpenAiForForYou,
                ForYouFeedSort = ParseEnum(stored.ForYouFeedSort, ForYouFeedSortMode.MostTagMatches),
                ForYouFeedMediaFilter = ParseEnum(stored.ForYouFeedMediaFilter, MediaFilterMode.All),
                RemoteControlEnabled = stored.RemoteControlEnabled,
                RemoteControlPort = stored.RemoteControlPort > 0
                    ? stored.RemoteControlPort
                    : Remote.RemoteProtocol.DefaultPort,
                RemoteControlToken = stored.RemoteControlToken ?? string.Empty,
                SavedSearchPresets = stored.SavedSearchPresets?
                    .Select(p => new SavedTagPreset
                    {
                        Id = p.Id ?? string.Empty,
                        Name = p.Name ?? string.Empty,
                        Tags = p.Tags ?? [],
                        UpdatedAtUnix = p.UpdatedAtUnix,
                    })
                    .ToList() ?? [],
                BrowseLayoutMode = Enum.IsDefined(typeof(BrowseLayoutMode), stored.BrowseLayoutMode)
                    ? (BrowseLayoutMode)stored.BrowseLayoutMode
                    : BrowseLayoutMode.Grid,
                FeedMediaQuality = Enum.IsDefined(typeof(FeedMediaQuality), stored.FeedMediaQuality)
                    ? (FeedMediaQuality)stored.FeedMediaQuality
                    : FeedMediaQuality.Sample,
                ClearMediaPlaybackCacheOnExit = stored.ClearMediaPlaybackCacheOnExit ?? true,
                LastSyncSuccessAtUnix = stored.LastSyncSuccessAtUnix,
                LastSyncAttemptAtUnix = stored.LastSyncAttemptAtUnix,
                LastSyncDeviceId = stored.LastSyncDeviceId ?? string.Empty,
                LastSyncDeviceLabel = stored.LastSyncDeviceLabel ?? string.Empty,
                LastSyncDirection = stored.LastSyncDirection ?? string.Empty,
                LastSyncStatus = stored.LastSyncStatus ?? string.Empty,
                LastSyncError = stored.LastSyncError ?? string.Empty,
                LastSyncSummary = stored.LastSyncSummary ?? string.Empty,
                CheckForUpdatesOnStartup = stored.CheckForUpdatesOnStartup ?? true,
                DismissedUpdateVersion = stored.DismissedUpdateVersion ?? string.Empty,
                LocalLibraries = stored.LocalLibraries ?? [],
                DownloadLibraryId = stored.DownloadLibraryId ?? string.Empty,
            };

            MigrateLocalLibraries(settings.LocalLibraries);
            settings.MigrateLegacyTagsIfNeeded();
            return settings;
        }
        catch
        {
            return null;
        }
    }

    public void Save(UserSettings settings)
    {
        try
        {
            settings.SyncTagsString();

            var directory = Path.GetDirectoryName(_settingsPath)!;
            Directory.CreateDirectory(directory);

            var apiKey = settings.ApiKey ?? string.Empty;
            var stored = new StoredSettings
            {
                ActiveSource = settings.ActiveSource.ToString(),
                UserId = settings.UserId?.Trim() ?? string.Empty,
                ApiKey = string.Empty,
                ApiKeyProtected = _secureStore.Protect(apiKey),
                DanbooruLogin = settings.DanbooruLogin?.Trim() ?? string.Empty,
                DanbooruApiKey = string.Empty,
                DanbooruApiKeyProtected = _secureStore.Protect(settings.DanbooruApiKey ?? string.Empty),
                E621Username = settings.E621Username?.Trim() ?? string.Empty,
                E621ApiKey = string.Empty,
                E621ApiKeyProtected = _secureStore.Protect(settings.E621ApiKey ?? string.Empty),
                FilterAi = settings.FilterAi,
                LimitIndex = settings.LimitIndex,
                Tags = settings.Tags ?? string.Empty,
                IncludeTags = settings.IncludeTags,
                BlacklistTags = settings.BlacklistTags,
                GlobalBlockedTags = settings.GlobalBlockedTags,
                ActiveBlacklistPresetIds = settings.ActiveBlacklistPresetIds,
                ActiveSearchPresetIds = settings.ActiveSearchPresetIds,
                RatingSafe = settings.RatingSafe,
                RatingQuestionable = settings.RatingQuestionable,
                RatingExplicit = settings.RatingExplicit,
                MediaFilter = settings.MediaFilter.ToString(),
                SortMode = settings.SortMode.ToString(),
                MinScore = settings.MinScore,
                MinWidth = settings.MinWidth,
                MinHeight = settings.MinHeight,
                ArtistFilter = settings.ArtistFilter,
                CharacterFilter = settings.CharacterFilter,
                CopyrightFilter = settings.CopyrightFilter,
                SearchOptionsExpanded = settings.SearchOptionsExpanded,
                PlaybackMuted = settings.PlaybackMuted,
                PlaybackVolume = ClampVolume(settings.PlaybackVolume),
                PlaybackLoop = settings.PlaybackLoop,
                PlaybackSpeedIndex = ClampSpeedIndex(settings.PlaybackSpeedIndex),
                OpenAiApiKeyProtected = _secureStore.Protect(settings.OpenAiApiKey ?? string.Empty),
                OpenAiModel = string.IsNullOrWhiteSpace(settings.OpenAiModel) ? "gpt-4o-mini" : settings.OpenAiModel.Trim(),
                UseOpenAiForDescribeSearch = settings.UseOpenAiForDescribeSearch,
                ForYouEnabled = settings.ForYouEnabled,
                ForYouLearnArtists = settings.ForYouLearnArtists,
                ForYouLearnSeries = settings.ForYouLearnSeries,
                ForYouLearnMinorTags = settings.ForYouLearnMinorTags,
                ForYouCloudSync = settings.ForYouCloudSync,
                UseOpenAiForForYou = settings.UseOpenAiForForYou,
                ForYouFeedSort = settings.ForYouFeedSort.ToString(),
                ForYouFeedMediaFilter = settings.ForYouFeedMediaFilter.ToString(),
                RemoteControlEnabled = settings.RemoteControlEnabled,
                RemoteControlPort = settings.RemoteControlPort > 0
                    ? settings.RemoteControlPort
                    : Remote.RemoteProtocol.DefaultPort,
                RemoteControlToken = settings.RemoteControlToken ?? string.Empty,
                SavedSearchPresets = settings.SavedSearchPresets
                    .Select(p => new StoredSavedTagPreset
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Tags = p.Tags,
                        UpdatedAtUnix = p.UpdatedAtUnix,
                    })
                    .ToList(),
                BrowseLayoutMode = (int)settings.BrowseLayoutMode,
                FeedMediaQuality = (int)settings.FeedMediaQuality,
                ClearMediaPlaybackCacheOnExit = settings.ClearMediaPlaybackCacheOnExit,
                LastSyncSuccessAtUnix = settings.LastSyncSuccessAtUnix,
                LastSyncAttemptAtUnix = settings.LastSyncAttemptAtUnix,
                LastSyncDeviceId = settings.LastSyncDeviceId,
                LastSyncDeviceLabel = settings.LastSyncDeviceLabel,
                LastSyncDirection = settings.LastSyncDirection,
                LastSyncStatus = settings.LastSyncStatus,
                LastSyncError = settings.LastSyncError,
                LastSyncSummary = settings.LastSyncSummary,
                CheckForUpdatesOnStartup = settings.CheckForUpdatesOnStartup,
                DismissedUpdateVersion = settings.DismissedUpdateVersion ?? string.Empty,
                LocalLibraries = settings.LocalLibraries,
                DownloadLibraryId = settings.DownloadLibraryId ?? string.Empty,
            };

            var json = JsonSerializer.Serialize(stored, JsonOptions);
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Ignore save failures so the app keeps running.
        }
    }

    private static int ClampVolume(int volume) => Math.Clamp(volume, 0, 100);

    private static int ClampSpeedIndex(int index) => Math.Clamp(index, 0, 5);

    private static void MigrateLocalLibraries(List<LocalLibraryDefinition> libraries)
    {
        foreach (var library in libraries)
        {
            if (string.IsNullOrWhiteSpace(library.RootFolderPath) &&
                !string.IsNullOrWhiteSpace(library.RootNote))
            {
                library.RootFolderPath = library.RootNote.Trim();
                library.RootNote = string.Empty;
            }
        }
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;
    }

    private string UnprotectApiKey(StoredSettings stored)
    {
        if (!string.IsNullOrEmpty(stored.ApiKeyProtected))
        {
            return _secureStore.Unprotect(stored.ApiKeyProtected);
        }

        return stored.ApiKey ?? string.Empty;
    }

    private string UnprotectDanbooruApiKey(StoredSettings stored)
    {
        if (!string.IsNullOrEmpty(stored.DanbooruApiKeyProtected))
        {
            return _secureStore.Unprotect(stored.DanbooruApiKeyProtected);
        }

        return stored.DanbooruApiKey ?? string.Empty;
    }

    private string UnprotectE621ApiKey(StoredSettings stored)
    {
        if (!string.IsNullOrEmpty(stored.E621ApiKeyProtected))
        {
            return _secureStore.Unprotect(stored.E621ApiKeyProtected);
        }

        return stored.E621ApiKey ?? string.Empty;
    }

    private sealed class StoredSettings
    {
        public string? ActiveSource { get; set; }

        public string? UserId { get; set; }

        public string? ApiKey { get; set; }

        public string ApiKeyProtected { get; set; } = string.Empty;

        public string? DanbooruLogin { get; set; }

        public string? DanbooruApiKey { get; set; }

        public string DanbooruApiKeyProtected { get; set; } = string.Empty;

        public string? E621Username { get; set; }

        public string? E621ApiKey { get; set; }

        public string E621ApiKeyProtected { get; set; } = string.Empty;

        public bool FilterAi { get; set; } = true;

        public int LimitIndex { get; set; } = 1;

        public string? Tags { get; set; }

        public List<string>? IncludeTags { get; set; }

        public List<string>? BlacklistTags { get; set; }

        public List<string>? GlobalBlockedTags { get; set; }

        public List<string>? ActiveBlacklistPresetIds { get; set; }

        public List<string>? ActiveSearchPresetIds { get; set; }

        public bool RatingSafe { get; set; } = true;

        public bool RatingQuestionable { get; set; } = true;

        public bool RatingExplicit { get; set; } = true;

        public string? MediaFilter { get; set; }

        public string? SortMode { get; set; }

        public int MinScore { get; set; }

        public int MinWidth { get; set; }

        public int MinHeight { get; set; }

        public string? ArtistFilter { get; set; }

        public string? CharacterFilter { get; set; }

        public string? CopyrightFilter { get; set; }

        public bool SearchOptionsExpanded { get; set; }

        public bool PlaybackMuted { get; set; }

        public int PlaybackVolume { get; set; } = 75;

        public bool PlaybackLoop { get; set; } = true;

        public int PlaybackSpeedIndex { get; set; } = 2;

        public string OpenAiApiKeyProtected { get; set; } = string.Empty;

        public string? OpenAiModel { get; set; }

        public bool UseOpenAiForDescribeSearch { get; set; } = true;

        public bool ForYouEnabled { get; set; }

        public bool? ForYouLearnArtists { get; set; }

        public bool? ForYouLearnSeries { get; set; }

        public bool? ForYouLearnMinorTags { get; set; }

        public bool EnableForYouLearning { get; set; }

        public bool ForYouCloudSync { get; set; } = true;

        public bool EnableForYouCloudSync { get; set; }

        public bool UseOpenAiForForYou { get; set; } = true;

        public string? ForYouFeedSort { get; set; }

        public string? ForYouFeedMediaFilter { get; set; }

        public bool RemoteControlEnabled { get; set; }

        public int RemoteControlPort { get; set; } = Remote.RemoteProtocol.DefaultPort;

        public string? RemoteControlToken { get; set; }

        public List<StoredSavedTagPreset>? SavedSearchPresets { get; set; }

        public List<LocalLibraryDefinition>? LocalLibraries { get; set; }

        public string? DownloadLibraryId { get; set; }

        public int BrowseLayoutMode { get; set; }

        public int FeedMediaQuality { get; set; }

        public bool? ClearMediaPlaybackCacheOnExit { get; set; }

        public long? LastSyncSuccessAtUnix { get; set; }

        public long? LastSyncAttemptAtUnix { get; set; }

        public string? LastSyncDeviceId { get; set; }

        public string? LastSyncDeviceLabel { get; set; }

        public string? LastSyncDirection { get; set; }

        public string? LastSyncStatus { get; set; }

        public string? LastSyncError { get; set; }

        public string? LastSyncSummary { get; set; }

        public bool? CheckForUpdatesOnStartup { get; set; }

        public string? DismissedUpdateVersion { get; set; }
    }

    private sealed class StoredSavedTagPreset
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public List<string>? Tags { get; set; }

        public long UpdatedAtUnix { get; set; }
    }
}
