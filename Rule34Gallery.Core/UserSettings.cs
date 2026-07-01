namespace Rule34Gallery.Core;

public sealed partial class UserSettings
{
    public GallerySource ActiveSource { get; set; } = GallerySource.Rule34;

    public string UserId { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string DanbooruLogin { get; set; } = string.Empty;

    public string DanbooruApiKey { get; set; } = string.Empty;

    public string E621Username { get; set; } = string.Empty;

    public string E621ApiKey { get; set; } = string.Empty;

    public bool FilterAi { get; set; } = true;

    public int LimitIndex { get; set; } = 1;

    /// <summary>Legacy joined include tags; kept in sync with <see cref="IncludeTags"/>.</summary>
    public string Tags { get; set; } = string.Empty;

    public List<string> IncludeTags { get; set; } = [];

    public List<string> BlacklistTags { get; set; } = [];

    /// <summary>Tags hidden everywhere until removed (Settings). Not tied to browse search state.</summary>
    public List<string> GlobalBlockedTags { get; set; } = [];

    public List<string> ActiveBlacklistPresetIds { get; set; } = [];

    public List<string> ActiveSearchPresetIds { get; set; } = [];

    public bool RatingSafe { get; set; } = true;

    public bool RatingQuestionable { get; set; } = true;

    public bool RatingExplicit { get; set; } = true;

    public MediaFilterMode MediaFilter { get; set; } = MediaFilterMode.All;

    public SearchSortMode SortMode { get; set; } = SearchSortMode.ScoreDesc;

    public int MinScore { get; set; }

    public int MinWidth { get; set; }

    public int MinHeight { get; set; }

    public string ArtistFilter { get; set; } = string.Empty;

    public string CharacterFilter { get; set; } = string.Empty;

    public string CopyrightFilter { get; set; } = string.Empty;

    public bool SearchOptionsExpanded { get; set; }

    public bool PlaybackMuted { get; set; }

    public int PlaybackVolume { get; set; } = 75;

    public bool PlaybackLoop { get; set; } = true;

    public int PlaybackSpeedIndex { get; set; } = 2;

    public string OpenAiApiKey { get; set; } = string.Empty;

    public string OpenAiModel { get; set; } = "gpt-4o-mini";

    public bool UseOpenAiForDescribeSearch { get; set; } = true;

    public bool ForYouEnabled { get; set; }

    public bool ForYouLearnArtists { get; set; } = true;

    public bool ForYouLearnSeries { get; set; } = true;

    public bool ForYouLearnMinorTags { get; set; }

    public bool ForYouCloudSync { get; set; } = true;

    public bool UseOpenAiForForYou { get; set; } = true;

    public ForYouFeedSortMode ForYouFeedSort { get; set; } = ForYouFeedSortMode.MostTagMatches;

    public MediaFilterMode ForYouFeedMediaFilter { get; set; } = MediaFilterMode.All;

    public bool RemoteControlEnabled { get; set; }

    public int RemoteControlPort { get; set; } = Remote.RemoteProtocol.DefaultPort;

    public string RemoteControlToken { get; set; } = string.Empty;

    public BrowseLayoutMode BrowseLayoutMode { get; set; } = BrowseLayoutMode.Grid;

    public FeedMediaQuality FeedMediaQuality { get; set; } = FeedMediaQuality.Sample;

    /// <summary>When true, buffered remote video/GIF files are deleted on app exit.</summary>
    public bool ClearMediaPlaybackCacheOnExit { get; set; } = true;

    public long? LastSyncSuccessAtUnix { get; set; }

    public long? LastSyncAttemptAtUnix { get; set; }

    public string LastSyncDeviceId { get; set; } = string.Empty;

    public string LastSyncDeviceLabel { get; set; } = string.Empty;

    public string LastSyncDirection { get; set; } = string.Empty;

    public string LastSyncStatus { get; set; } = string.Empty;

    public string LastSyncError { get; set; } = string.Empty;

    public string LastSyncSummary { get; set; } = string.Empty;

    public bool CheckForUpdatesOnStartup { get; set; } = true;

    public string DismissedUpdateVersion { get; set; } = string.Empty;

    public void EnsureRemoteControlToken()
    {
        if (string.IsNullOrWhiteSpace(RemoteControlToken))
        {
            RemoteControlToken = Remote.RemoteTokenGenerator.CreateToken();
        }
    }

    public bool HasOpenAiForDescribeSearch =>
        UseOpenAiForDescribeSearch && !string.IsNullOrWhiteSpace(OpenAiApiKey);

    public bool HasOpenAiForForYou =>
        UseOpenAiForForYou && !string.IsNullOrWhiteSpace(OpenAiApiKey);

    public List<LocalLibraryDefinition> LocalLibraries { get; set; } = [];

    /// <summary>Local library id used as the download destination root.</summary>
    public string DownloadLibraryId { get; set; } = string.Empty;

    /// <summary>Manual include tags only (not from search presets).</summary>
    public IReadOnlyList<string> GetManualIncludeTags()
    {
        if (IncludeTags.Count > 0)
        {
            return IncludeTags;
        }

        if (string.IsNullOrWhiteSpace(Tags))
        {
            return [];
        }

        return Tags
            .Split([' ', '\t', ',', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => !t.StartsWith('-'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Tags sent to the API: manual tags plus every tag from active search presets (all AND).</summary>
    public IReadOnlyList<string> GetApiIncludeTags()
    {
        var tags = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tag in GetManualIncludeTags())
        {
            if (seen.Add(tag))
            {
                tags.Add(tag);
            }
        }

        foreach (var tag in GetSearchPresetIncludeTags())
        {
            if (seen.Add(tag))
            {
                tags.Add(tag);
            }
        }

        return tags;
    }

    public bool HasSearchCriteria()
        => IncludeTags.Count > 0 || ActiveSearchPresetIds.Count > 0;

    public IReadOnlyList<string> GetIncludeTags() => GetApiIncludeTags();

    public void AddIncludeTag(string tag)
    {
        var normalized = NormalizeTagToken(tag);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        if (IncludeTags.Any(t => t.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        IncludeTags.Add(normalized);
        SyncTagsString();
    }

    public void RemoveIncludeTag(string tag)
    {
        IncludeTags.RemoveAll(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase));
        SyncTagsString();
    }

    public void AddBlacklistTag(string tag)
    {
        var normalized = NormalizeTagToken(tag).TrimStart('-');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        if (BlacklistTags.Any(t => t.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        BlacklistTags.Add(normalized);
    }

    public void RemoveBlacklistTag(string tag)
        => BlacklistTags.RemoveAll(t => t.Equals(tag.Trim().TrimStart('-'), StringComparison.OrdinalIgnoreCase));

    public void AddGlobalBlockedTag(string tag)
    {
        var normalized = NormalizeTagToken(tag).TrimStart('-');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        if (GlobalBlockedTags.Any(t => t.Equals(normalized, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        GlobalBlockedTags.Add(normalized);
    }

    public void RemoveGlobalBlockedTag(string tag)
        => GlobalBlockedTags.RemoveAll(t =>
            t.Equals(tag.Trim().TrimStart('-'), StringComparison.OrdinalIgnoreCase));

    public IEnumerable<string> GetGlobalBlockedTags()
        => GlobalBlockedTags.Where(t => !string.IsNullOrWhiteSpace(t));

    public bool IsPresetActive(string presetId)
        => ActiveBlacklistPresetIds.Any(id => id.Equals(presetId, StringComparison.OrdinalIgnoreCase));

    public void SetPresetActive(string presetId, bool active)
    {
        var preset = Services.BlacklistPresetCatalog.Find(presetId);

        ActiveBlacklistPresetIds.RemoveAll(id => id.Equals(presetId, StringComparison.OrdinalIgnoreCase));
        if (active)
        {
            ActiveBlacklistPresetIds.Add(presetId);
        }

        if (preset is null)
        {
            return;
        }

        if (active)
        {
            foreach (var tag in preset.Tags)
            {
                AddBlacklistTag(tag);
            }
        }
        else
        {
            var stillFromOtherPresets = GetPresetBlacklistTags()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var tag in preset.Tags)
            {
                var normalized = NormalizeTagToken(tag);
                if (!stillFromOtherPresets.Contains(normalized))
                {
                    RemoveBlacklistTag(normalized);
                }
            }
        }
    }

    /// <summary>
    /// Ensures tags from active presets appear in <see cref="BlacklistTags"/> (visible as chips).
    /// </summary>
    public void SyncPresetTagsToBlacklist()
    {
        foreach (var tag in GetPresetBlacklistTags())
        {
            AddBlacklistTag(tag);
        }
    }

    public void DeactivatePresetsContainingTag(string tag)
    {
        var normalized = NormalizeTagToken(tag).TrimStart('-');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        foreach (var preset in Services.BlacklistPresetCatalog.All)
        {
            if (!IsPresetActive(preset.Id))
            {
                continue;
            }

            if (preset.Tags.Any(t =>
                    NormalizeTagToken(t).Equals(normalized, StringComparison.OrdinalIgnoreCase)))
            {
                SetPresetActive(preset.Id, false);
            }
        }
    }

    public bool IsSearchPresetActive(string presetId)
        => ActiveSearchPresetIds.Any(id => id.Equals(presetId, StringComparison.OrdinalIgnoreCase));

    public void SetSearchPresetActive(string presetId, bool active)
    {
        var preset = ResolveSearchPreset(presetId);
        var wasActive = IsSearchPresetActive(presetId);
        ActiveSearchPresetIds.RemoveAll(id => id.Equals(presetId, StringComparison.OrdinalIgnoreCase));

        if (active)
        {
            ActiveSearchPresetIds.Add(presetId);
            if (preset is not null)
            {
                foreach (var tag in preset.Tags)
                {
                    AddIncludeTag(tag);
                }
            }
        }
        else if (wasActive && preset is not null)
        {
            var stillNeeded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in ActiveSearchPresetIds)
            {
                if (ResolveSearchPreset(id) is not { } other)
                {
                    continue;
                }

                foreach (var tag in other.Tags)
                {
                    var normalized = NormalizeTagToken(tag);
                    if (!string.IsNullOrWhiteSpace(normalized))
                    {
                        stillNeeded.Add(normalized);
                    }
                }
            }

            foreach (var tag in preset.Tags)
            {
                var normalized = NormalizeTagToken(tag);
                if (!string.IsNullOrWhiteSpace(normalized) && !stillNeeded.Contains(normalized))
                {
                    RemoveIncludeTag(normalized);
                }
            }
        }
    }

    public void PruneOrphanSearchPresetIds()
    {
        ActiveSearchPresetIds.RemoveAll(id => ResolveSearchPreset(id) is null);
    }

    public void SyncActiveSearchPresetTagsToIncludeTags()
    {
        PruneOrphanSearchPresetIds();
        foreach (var id in ActiveSearchPresetIds)
        {
            if (ResolveSearchPreset(id) is not { } preset)
            {
                continue;
            }

            foreach (var tag in preset.Tags)
            {
                AddIncludeTag(tag);
            }
        }
    }

    public IEnumerable<string> GetSearchPresetIncludeTags()
        => Services.SearchPresetCatalog.GetTagsForPresets(ActiveSearchPresetIds, this);

    public IEnumerable<string> GetPresetBlacklistTags()
        => Services.BlacklistPresetCatalog.GetTagsForPresets(ActiveBlacklistPresetIds);

    public IEnumerable<string> GetEffectiveBlacklistTags()
    {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in BlacklistTags)
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                tags.Add(tag);
            }
        }

        foreach (var tag in GetPresetBlacklistTags())
        {
            tags.Add(tag);
        }

        return tags;
    }

    public void SetIncludeTags(IEnumerable<string> tags)
    {
        IncludeTags = tags
            .Select(NormalizeTagToken)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        SyncTagsString();
    }

    public void SyncTagsString()
        => Tags = string.Join(' ', IncludeTags);

    public void MigrateLegacyTagsIfNeeded()
    {
        if (IncludeTags.Count == 0 && !string.IsNullOrWhiteSpace(Tags))
        {
            IncludeTags = Tags
                .Split([' ', '\t', ',', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(NormalizeTagToken)
                .Where(t => !string.IsNullOrWhiteSpace(t) && !t.StartsWith('-'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        SyncActiveSearchPresetTagsToIncludeTags();
    }

    public static string NormalizeTagToken(string tag)
        => tag.Trim().Replace(" ", "_");
}
