using System.Net.Http;

namespace Rule34Gallery.Core.Services;

public sealed class ForYouService
{
    private readonly AppServices _app;
    private readonly ForYouProfileStore _store;
    private readonly SemaphoreSlim _buildGate = new(1, 1);
    private readonly object _sync = new();
    private long _lastProfileBuildUnix;
    private readonly HashSet<string> _newTopicHighlights = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> LastFeedQueries { get; private set; } = [];

    public ForYouService(AppServices app)
    {
        _app = app;
        _store = new ForYouProfileStore(app.Platform.AppDataFolder);
        Profile = _store.Load() ?? new ForYouProfile();
        lock (_sync)
        {
            var migrated = NormalizeTopicWeightsLocked();
            PurgeDisabledCategoryTopicsLocked();
            PurgeExcludedTopicsLocked();
            Profile.Trim();
            if (migrated)
            {
                SaveLocked();
                ScheduleSaveToCloud();
            }
        }
    }

    public event EventHandler? Changed;

    public ForYouProfile Profile { get; private set; }

    public bool IsEnabled
    {
        get => Profile.Enabled;
        set
        {
            if (Profile.Enabled == value)
            {
                return;
            }

            Profile.Enabled = value;
            Touch();
        }
    }

    public bool CloudSyncEnabled
    {
        get => Profile.CloudSyncEnabled;
        set
        {
            if (Profile.CloudSyncEnabled == value)
            {
                return;
            }

            Profile.CloudSyncEnabled = value;
            Touch();
        }
    }

    public void ApplySettings(UserSettings settings)
    {
        lock (_sync)
        {
            Profile.Enabled = settings.ForYouEnabled;
            Profile.CloudSyncEnabled = settings.ForYouCloudSync;
            PurgeDisabledCategoryTopicsLocked();
            PurgeExcludedTopicsLocked();
            SaveLocked();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Reload()
    {
        lock (_sync)
        {
            Profile = _store.Load() ?? new ForYouProfile();
            var migrated = NormalizeTopicWeightsLocked();
            PurgeExcludedTopicsLocked();
            Profile.Trim();
            if (migrated)
            {
                SaveLocked();
                ScheduleSaveToCloud();
            }
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Reset()
    {
        ResetLocal(clearLearning: true, enabled: false);
        ScheduleSaveToCloud(overwriteRemote: true);
    }

    public Task ResetAsync()
    {
        ResetLocal(clearLearning: true, enabled: false);
        return ClearCloudProfileAsync();
    }

    public void ClearData()
    {
        ResetLocal(clearLearning: true, enabled: Profile.Enabled);
        ScheduleSaveToCloud(overwriteRemote: true);
    }

    public Task ClearDataAsync()
    {
        var enabled = Profile.Enabled;
        ResetLocal(clearLearning: true, enabled: enabled);
        return ClearCloudProfileAsync();
    }

    public void ResetProfile()
    {
        ResetLocalProfileOnly();
        ScheduleSaveToCloud(overwriteRemote: true);
    }

    public Task ResetProfileAsync()
    {
        ResetLocalProfileOnly();
        return ClearCloudProfileAsync();
    }

    private void ResetLocal(bool clearLearning, bool enabled)
    {
        lock (_sync)
        {
            Profile = new ForYouProfile
            {
                Enabled = enabled,
                CloudSyncEnabled = Profile.CloudSyncEnabled,
            };
            SaveLocked();
        }

        _lastProfileBuildUnix = 0;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void ResetLocalProfileOnly()
    {
        lock (_sync)
        {
            Profile.Topics.Clear();
            Profile.SearchLines.Clear();
            Profile.RecentActivity.Clear();
            Profile.Summary = string.Empty;
            Profile.UsedOpenAi = false;
            Profile.RemovedTopics.Clear();
            Profile.UpdatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            SaveLocked();
        }

        _lastProfileBuildUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private async Task ClearCloudProfileAsync()
    {
        if (!Profile.CloudSyncEnabled || !_app.Library.IsSignedIn)
        {
            return;
        }

        try
        {
            await _app.Library.ClearForYouProfileFromCloudAsync().ConfigureAwait(false);
        }
        catch
        {
            // Best-effort; local reset still applies.
        }
    }

    public void RemoveActivity(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        lock (_sync)
        {
            Profile.RecentActivity.RemoveAll(a => a.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            SaveLocked();
        }

        Changed?.Invoke(this, EventArgs.Empty);
        ScheduleSaveToCloud();
    }

    public void BoostTopic(string topic)
    {
        ApplySignal(ForYouSignalType.TopicWeightAdjusted, [topic], "manual-boost", 0.2);
    }

    public bool IsTopicNew(string topic)
    {
        var normalized = UserSettings.NormalizeTagToken(topic).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        lock (_sync)
        {
            return _newTopicHighlights.Contains(normalized);
        }
    }

    public bool ClearTopicNewHighlight(string topic)
    {
        var normalized = UserSettings.NormalizeTagToken(topic).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        lock (_sync)
        {
            return _newTopicHighlights.Remove(normalized);
        }
    }

    public bool BoostSignalToTopic(ForYouActivityEntry entry)
    {
        var tags = ExtractBoostTagsFromActivity(entry);
        if (tags.Count == 0)
        {
            return false;
        }

        lock (_sync)
        {
            foreach (var tag in tags.Take(4))
            {
                Profile.UnmarkTopicRemoved(tag);
                var existed = Profile.Topics.Any(t => t.Topic.Equals(tag, StringComparison.OrdinalIgnoreCase));
                var profile = Profile.GetOrCreateTopic(tag);
                profile.IsBlocked = false;
                profile.IsPinned = false;
                profile.Weight = Math.Max(
                    profile.Weight,
                    ForYouSignalStrengths.MergeTopicScore(profile.Weight, ForYouSignalStrengths.ManualSearch, Profile.LearningRate));
                profile.Reason = "Boosted from signal";
                profile.ManualAdjustments++;
                profile.LastSeenUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (!existed)
                {
                    MarkTopicNewLocked(tag);
                }
            }

            Profile.Trim();
            SaveLocked();
        }

        Changed?.Invoke(this, EventArgs.Empty);
        ScheduleSaveToCloud();
        return true;
    }

    public bool RemoveTopic(string topic)
    {
        var normalized = UserSettings.NormalizeTagToken(topic).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        lock (_sync)
        {
            Profile.MarkTopicRemoved(normalized);
            _newTopicHighlights.Remove(normalized);
            Profile.Trim();
            SaveLocked();
        }

        Changed?.Invoke(this, EventArgs.Empty);
        ScheduleSaveToCloud(overwriteRemote: true);
        return true;
    }

    public void ApplyLearningCategorySettings()
    {
        lock (_sync)
        {
            PurgeDisabledCategoryTopicsLocked();
            PurgeExcludedTopicsLocked();
            Profile.Trim();
            SaveLocked();
        }

        Changed?.Invoke(this, EventArgs.Empty);
        ScheduleSaveToCloud(overwriteRemote: true);
    }

    private void PurgeDisabledCategoryTopicsLocked()
    {
        foreach (var topic in Profile.Topics.ToList())
        {
            if (topic.IsManuallyCurated || topic.IsPinned)
            {
                continue;
            }

            if (ForYouLearningGate.CanLearn(topic.Topic, _app.Settings))
            {
                continue;
            }

            Profile.MarkTopicRemoved(topic.Topic);
        }
    }

    public IReadOnlyList<ForYouTopicProfile> GetFeedTopics(int take = 24, bool includeBlocked = false)
    {
        lock (_sync)
        {
            return ForYouTopicFocus.RankTopicsByStrength(
                ForYouLearningGate.FilterForFeed(
                    Profile.Topics
                        .Where(t => !string.IsNullOrWhiteSpace(t.Topic))
                        .Where(t => includeBlocked || (!t.IsBlocked && !ForYouLearningFilter.IsExcluded(t.Topic))),
                    _app.Settings),
                take);
        }
    }

    public IReadOnlyList<ForYouTopicProfile> GetTopicsForManage()
    {
        lock (_sync)
        {
            PurgeDisabledCategoryTopicsLocked();
            SaveLocked();

            return Profile.Topics
                .Where(t => !string.IsNullOrWhiteSpace(t.Topic))
                .Where(t => !ForYouLearningFilter.IsExcluded(t.Topic))
                .OrderBy(t => ForYouTopicCategories.SortOrder(ForYouTopicCategories.Group(t.Topic)))
                .ThenByDescending(t => t.IsPinned)
                .ThenByDescending(t => t.Weight)
                .ThenBy(t => t.Topic, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public async Task ForceRebuildProfileAsync(CancellationToken cancellationToken = default)
    {
        if (!Profile.Enabled)
        {
            return;
        }

        lock (_sync)
        {
            RebuildTopicsFromActivityLocked();
            SaveLocked();
        }

        _lastProfileBuildUnix = 0;
        await RefreshProfileAsync(cancellationToken).ConfigureAwait(false);
        ScheduleSaveToCloud();
    }

    public void SetSearchLinePinned(string query, bool pinned)
    {
        lock (_sync)
        {
            var line = GetOrCreateSearchLineLocked(query);
            line.IsPinned = pinned;
            if (pinned)
            {
                line.IsHidden = false;
            }

            SaveLocked();
        }

        Changed?.Invoke(this, EventArgs.Empty);
        ScheduleSaveToCloud();
    }

    public void SetSearchLineHidden(string query, bool hidden)
    {
        lock (_sync)
        {
            var line = GetOrCreateSearchLineLocked(query);
            line.IsHidden = hidden;
            if (hidden)
            {
                line.IsPinned = false;
            }

            SaveLocked();
        }

        Changed?.Invoke(this, EventArgs.Empty);
        ScheduleSaveToCloud();
    }

    public IReadOnlyList<ForYouActivityEntry> GetRecentActivity(int take = 20)
        => Profile.RecentActivity
            .OrderByDescending(a => a.TimestampUnix)
            .Take(take)
            .ToList();

    public ForYouInterestTimelineResult BuildInterestTimeline()
        => ForYouInterestTimelineBuilder.Build(
            GetRankedTopics(48),
            Profile.RecentActivity,
            Profile.LearningRate);

    public IReadOnlyList<ForYouSearchLine> BuildSearchLines(int take = 12)
    {
        var lines = new Dictionary<string, ForYouSearchLine>(StringComparer.OrdinalIgnoreCase);
        foreach (var stored in Profile.SearchLines)
        {
            if (string.IsNullOrWhiteSpace(stored.Query))
            {
                continue;
            }

            lines[stored.Query] = CloneSearchLine(stored);
        }

        foreach (var topic in GetRankedTopics(8).Where(t => !t.IsBlocked))
        {
            var query = topic.Topic;
            if (lines.ContainsKey(query))
            {
                continue;
            }

            lines[query] = new ForYouSearchLine
            {
                Query = query,
                Label = TopicToTitle(topic.Topic),
                Reason = topic.Why,
                Score = (int)Math.Clamp(topic.Score, 0, 100),
                Source = "Topics",
            };
        }

        var ranked = GetRankedTopics(6).Where(t => !t.IsBlocked).Select(t => t.Topic).ToList();
        if (ranked.Count >= 3)
        {
            var triple = string.Join(' ', ranked.Take(3));
            if (!lines.ContainsKey(triple))
            {
                lines[triple] = new ForYouSearchLine
                {
                    Query = triple,
                    Label = $"{TopicToTitle(ranked[0])} + {TopicToTitle(ranked[1])} + {TopicToTitle(ranked[2])}",
                    Reason = "Top topics combined",
                    Score = 95,
                    Source = "Topics",
                };
            }
        }

        if (ranked.Count >= 2)
        {
            var query = string.Join(' ', ranked.Take(2));
            if (!lines.ContainsKey(query))
            {
                lines[query] = new ForYouSearchLine
                {
                    Query = query,
                    Label = $"{TopicToTitle(ranked[0])} + {TopicToTitle(ranked[1])}",
                    Reason = "Your strongest topics together",
                    Score = 85,
                    Source = "Topics",
                };
            }
        }

        foreach (var activity in Profile.RecentActivity
                     .Where(a => a.SignalType == ForYouSignalType.Search)
                     .OrderByDescending(a => a.TimestampUnix)
                     .Take(6))
        {
            var terms = activity.Topic;
            if (string.IsNullOrWhiteSpace(terms) || lines.ContainsKey(terms))
            {
                continue;
            }

            lines[terms] = new ForYouSearchLine
            {
                Query = terms,
                Label = TopicToTitle(terms),
                Reason = "From your search history",
                Score = 60,
                Source = "Search",
            };
        }

        return lines.Values
            .Where(l => !l.IsHidden)
            .OrderByDescending(l => l.IsPinned)
            .ThenByDescending(l => l.Score)
            .Take(take)
            .ToList();
    }

    public async Task RefreshProfileAsync(CancellationToken cancellationToken = default)
    {
        if (!Profile.Enabled)
        {
            return;
        }

        if (_app.Settings.HasOpenAiForForYou && Profile.RecentActivity.Count > 0)
        {
            try
            {
                var blocked = _app.Settings.GetEffectiveBlacklistTags().ToList();
                var ai = await OpenAiForYouDiscoveryService.BuildAsync(
                    _app.Http,
                    _app.Settings.OpenAiApiKey,
                    _app.Settings.OpenAiModel,
                    Profile.RecentActivity,
                    blocked,
                    _app.Settings.FilterAi,
                    _app.Settings,
                    cancellationToken).ConfigureAwait(false);

                lock (_sync)
                {
                    MergeAiProfileLocked(ai);
                    if (!string.IsNullOrWhiteSpace(ai.Summary))
                    {
                        Profile.Summary = ai.Summary;
                    }

                    Profile.UsedOpenAi = ai.UsedOpenAi;
                    SaveLocked();
                }
            }
            catch
            {
                lock (_sync)
                {
                    Profile.UsedOpenAi = false;
                    SaveLocked();
                }
            }
        }

        lock (_sync)
        {
            PurgeDisabledCategoryTopicsLocked();
            PurgeExcludedTopicsLocked();
            SaveLocked();
        }

        _lastProfileBuildUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void PauseLearning(bool paused)
    {
        IsEnabled = !paused;
    }

    public void RecordSearch(IEnumerable<string> tags, string source = "search")
    {
        if (!Profile.Enabled)
        {
            return;
        }

        var normalized = ForYouLearningFilter.Sanitize(tags)
            .Take(12)
            .ToList();
        if (normalized.Count == 0)
        {
            return;
        }

        lock (_sync)
        {
            foreach (var tag in normalized)
            {
                var strength = ForYouSignalEngine.HasSimilarSearchHistory(Profile, tag)
                    ? ForYouSignalStrengths.SimilarTagSearchRepeated
                    : ForYouSignalStrengths.ManualSearch;
                ApplyStrengthLocked(tag, strength, ForYouSignalType.Search, source, recordActivity: true);

                foreach (var similar in ForYouSignalEngine.FindSimilarTags(tag, normalized))
                {
                    ApplyStrengthLocked(
                        similar,
                        ForYouSignalStrengths.SimilarTag,
                        ForYouSignalType.SimilarTag,
                        $"similar:{tag}",
                        recordActivity: true);
                }
            }

            var query = string.Join(' ', normalized);
            var line = GetOrCreateSearchLineLocked(query);
            line.Score = Math.Max(line.Score, 100);
            line.Reason = string.IsNullOrWhiteSpace(line.Reason) ? "From your search history" : line.Reason;
            line.Source = "Search";
            ApplySearchDecayLocked();
            Profile.Trim();
            SaveLocked();
        }

        Changed?.Invoke(this, EventArgs.Empty);
        ScheduleSaveToCloud();
    }

    public void RecordSavedTags(IEnumerable<string> tags, string source = "saved-tag")
    {
        if (!Profile.Enabled)
        {
            return;
        }

        var normalized = ForYouLearningFilter.Sanitize(tags).Take(12).ToList();
        if (normalized.Count == 0)
        {
            return;
        }

        ApplySignal(ForYouSignalType.SavedTag, normalized, source, ForYouSignalStrengths.SavedTag);
    }

    public void RecordPostOpened(PostItem post)
    {
        if (!Profile.Enabled)
        {
            return;
        }

        var detail = $"post:{post.Id}";
        var tags = ForYouLearningGate.FilterLearnableFromPost(post, _app.Settings).ToList();
        lock (_sync)
        {
            var reopened = ForYouSignalEngine.HasOpenedPostBefore(Profile, detail);
            var type = reopened ? ForYouSignalType.PostReopened : ForYouSignalType.PostOpened;
            var strength = reopened
                ? ForYouSignalStrengths.PostReopened
                : ForYouSignalStrengths.PostOpened;

            foreach (var tag in tags)
            {
                var profile = Profile.GetOrCreateTopic(tag);
                if (ForYouSignalEngine.HasRepeatedTagViews(profile))
                {
                    ApplyStrengthLocked(tag, ForYouSignalStrengths.RepeatedTagView, ForYouSignalType.RepeatedTagView, detail, recordActivity: true);
                }

                ApplyStrengthLocked(tag, strength, type, detail, recordActivity: true);
            }

            Profile.Trim();
            SaveLocked();
        }

        Changed?.Invoke(this, EventArgs.Empty);
        ScheduleSaveToCloud();
    }

    public void RecordViewerEngagement(PostItem post, TimeSpan dwell, bool completed)
    {
        if (!Profile.Enabled)
        {
            return;
        }

        var detail = $"post:{post.Id}";
        var tags = ForYouLearningGate.FilterLearnableFromPost(post, _app.Settings).ToList();
        if (tags.Count == 0)
        {
            return;
        }

        if (completed || dwell >= TimeSpan.FromSeconds(8))
        {
            ApplySignal(ForYouSignalType.PostCompleted, tags, detail, ForYouSignalStrengths.FullyWatched);
            return;
        }

        if (dwell >= TimeSpan.FromSeconds(3))
        {
            ApplySignal(ForYouSignalType.LongView, tags, detail, ForYouSignalStrengths.LongView);
            return;
        }

        if (dwell < TimeSpan.FromSeconds(1.5) && ForYouSignalEngine.HasOpenedPostBefore(Profile, detail))
        {
            ApplySignal(ForYouSignalType.QuickSkip, tags, detail, ForYouSignalStrengths.QuickSkip);
        }
    }

    public void RecordTagClicked(string tag, string source = "tag-click")
    {
        if (!Profile.Enabled || string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        ApplySignal(ForYouSignalType.TagClicked, [tag], source, ForYouSignalStrengths.TagClicked);
    }

    public void RecordFavorite(PostItem post)
        => ApplyPostSignal(ForYouSignalType.Favorite, post, $"favorite:{post.Id}", ForYouSignalStrengths.SavedPost);

    public void RecordWatchLater(PostItem post)
        => ApplyPostSignal(ForYouSignalType.WatchLater, post, $"watch-later:{post.Id}", ForYouSignalStrengths.SavedPost);

    public void RecordDownload(PostItem post)
        => ApplyPostSignal(ForYouSignalType.Download, post, $"download:{post.Id}", ForYouSignalStrengths.FinishesPostsWithTag);

    public void RecordBlockedTag(string tag, string source = "blocked-tag")
        => ApplySignal(ForYouSignalType.TopicBlocked, [tag], source, ForYouSignalStrengths.BlockedTag);

    public void RecordNotInterested(IEnumerable<string> tags, string source = "not-interested")
        => ApplySignal(ForYouSignalType.NotInterested, tags, source, ForYouSignalStrengths.NotInterested);

    public void RecordReportedPost(PostItem post, string source = "reported-post")
        => ApplyPostSignal(ForYouSignalType.ReportedPost, post, $"reported:{post.Id}", ForYouSignalStrengths.ReportedPost);

    public void RecordRecommendationFeedback(ForYouFeedItem item, bool positive)
    {
        if (!Profile.Enabled || string.IsNullOrWhiteSpace(item.Topic))
        {
            return;
        }

        if (positive)
        {
            ApplySignal(ForYouSignalType.RecommendationLiked, [item.Topic], "recommendation-like", 0.5);
            return;
        }

        ApplySignal(ForYouSignalType.NotInterested, [item.Topic], "recommendation-dismiss", ForYouSignalStrengths.NotInterested);
    }

    private void ApplyPostSignal(ForYouSignalType type, PostItem post, string detail, double strength)
    {
        if (!Profile.Enabled)
        {
            return;
        }

        ApplySignal(type, ForYouLearningGate.FilterLearnableFromPost(post, _app.Settings), detail, strength);
    }

    public void SetTopicPinned(string topic, bool pinned)
    {
        lock (_sync)
        {
            var profile = GetTopicProfileLocked(topic);
            profile.IsPinned = pinned;
            if (pinned)
            {
                profile.IsBlocked = false;
            }

            if (pinned)
            {
                profile.Weight = Math.Max(profile.Weight, ForYouSignalStrengths.SavedTag);
            }

            profile.ManualAdjustments++;
            Profile.Trim();
            SaveLocked();
        }

        Changed?.Invoke(this, EventArgs.Empty);
        ScheduleSaveToCloud();
    }

    public void SetTopicBlocked(string topic, bool blocked)
    {
        lock (_sync)
        {
            var profile = GetTopicProfileLocked(topic);
            profile.IsBlocked = blocked;
            profile.IsPinned = blocked ? false : profile.IsPinned;
            profile.ManualAdjustments++;
            if (blocked)
            {
                profile.Weight = ForYouSignalStrengths.MinBlockedTopicScore;
            }
            else
            {
                profile.Weight = Math.Max(profile.Weight, ForYouSignalStrengths.MinTopicScore);
            }
            Profile.Trim();
            SaveLocked();
        }

        Changed?.Invoke(this, EventArgs.Empty);
        ScheduleSaveToCloud();
    }

    public void SetTopicWeight(string topic, double weight)
        => SetTopicStrength(topic, weight);

    public bool AddManualTopic(string topic, double strength)
    {
        var normalized = UserSettings.NormalizeTagToken(topic).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || ForYouLearningFilter.IsExcluded(normalized))
        {
            return false;
        }

        lock (_sync)
        {
            Profile.UnmarkTopicRemoved(normalized);
            var existed = Profile.Topics.Any(t => t.Topic.Equals(normalized, StringComparison.OrdinalIgnoreCase));
            var clamped = Math.Clamp(strength, ForYouSignalStrengths.MinTopicScore, ForYouSignalStrengths.MaxTopicScore);
            var profile = Profile.GetOrCreateTopic(normalized);
            profile.Weight = clamped;
            profile.IsBlocked = false;
            profile.IsPinned = false;
            profile.Reason = "Added manually";
            profile.ManualAdjustments++;
            profile.LastSeenUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            AddActivityLocked(
                ForYouSignalType.ManualTopic,
                normalized,
                $"strength:{clamped:F2}",
                clamped);
            if (!existed)
            {
                MarkTopicNewLocked(normalized);
            }

            Profile.Trim();
            SaveLocked();
        }

        Changed?.Invoke(this, EventArgs.Empty);
        ScheduleSaveToCloud();
        return true;
    }

    public bool PromoteTopicToManual(string topic)
    {
        var normalized = UserSettings.NormalizeTagToken(topic).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || ForYouLearningFilter.IsExcluded(normalized))
        {
            return false;
        }

        lock (_sync)
        {
            var profile = Profile.Topics.FirstOrDefault(t =>
                t.Topic.Equals(normalized, StringComparison.OrdinalIgnoreCase));
            if (profile is null || profile.IsManuallyCurated || profile.IsBlocked)
            {
                return false;
            }

            profile.ManualAdjustments++;
            profile.Reason = "Promoted to manual";
            profile.LastSeenUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            AddActivityLocked(
                ForYouSignalType.ManualTopic,
                normalized,
                $"strength:{profile.Weight:F2}",
                profile.Weight);
            Profile.Trim();
            SaveLocked();
        }

        Changed?.Invoke(this, EventArgs.Empty);
        ScheduleSaveToCloud();
        return true;
    }

    public bool SetTopicStrength(string topic, double strength)
    {
        var normalized = UserSettings.NormalizeTagToken(topic).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || ForYouLearningFilter.IsExcluded(normalized))
        {
            return false;
        }

        lock (_sync)
        {
            var profile = Profile.GetOrCreateTopic(normalized);
            profile.Weight = Math.Clamp(strength, ForYouSignalStrengths.MinTopicScore, ForYouSignalStrengths.MaxTopicScore);
            profile.IsBlocked = false;
            profile.Reason = "Set manually";
            profile.ManualAdjustments++;
            profile.LastSeenUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            AddActivityLocked(
                ForYouSignalType.ManualTopic,
                normalized,
                $"strength:{profile.Weight:F2}",
                profile.Weight);
            Profile.Trim();
            SaveLocked();
        }

        Changed?.Invoke(this, EventArgs.Empty);
        ScheduleSaveToCloud();
        return true;
    }

    public IReadOnlyList<ForYouTopicProfile> GetRankedTopics(int take = 24, bool includeBlocked = false)
    {
        lock (_sync)
        {
            return ForYouTopicFocus.RankTopicsByStrength(
                Profile.Topics
                    .Where(t => !string.IsNullOrWhiteSpace(t.Topic))
                    .Where(t => includeBlocked || (!t.IsBlocked && !ForYouLearningFilter.IsExcluded(t.Topic))),
                take);
        }
    }

    public string BuildInterestSummary()
    {
        lock (_sync)
        {
            return ForYouInterestSummary.Build(GetRankedTopics(), Profile.RecentActivity);
        }
    }

    public double GetTopicSearchDecayPenalty(string topic)
    {
        lock (_sync)
        {
            var profile = Profile.Topics.FirstOrDefault(t =>
                t.Topic.Equals(topic, StringComparison.OrdinalIgnoreCase));
            if (profile is not null && profile.IsManuallyCurated)
            {
                return 0;
            }

            var searches = ForYouTopicDecay.ListSearchEvents(Profile.RecentActivity).Reverse().ToList();
            return ForYouTopicDecay.DecayPenalty(ForYouTopicDecay.SearchesSinceLastHit(topic, searches));
        }
    }

    public string BuildDataViewerText()
    {
        lock (_sync)
        {
            var ranked = GetRankedTopics();
            var searches = ForYouTopicDecay.ListSearchEvents(Profile.RecentActivity).Reverse().ToList();
            var searchEventCount = ForYouTopicDecay.ListSearchEvents(Profile.RecentActivity).Count;
            var builder = new System.Text.StringBuilder();
            builder.AppendLine(BuildInterestSummary());
            builder.AppendLine();
            if (!string.IsNullOrWhiteSpace(Profile.Summary))
            {
                builder.AppendLine("OpenAI:");
                builder.AppendLine(Profile.Summary);
                builder.AppendLine();
            }

            builder.AppendLine($"Topics: {ranked.Count} ranked");
            foreach (var topic in ranked.Take(16))
            {
                var penalty = ForYouTopicDecay.DecayPenalty(
                    ForYouTopicDecay.SearchesSinceLastHit(topic.Topic, searches));
                var decay = penalty > 0 ? $"  (−{penalty:0} search decay)" : string.Empty;
                builder.AppendLine($"  {ForYouInterestSummary.FormatTopicLabel(topic.Topic)}  score {topic.Score:F1}{decay}");
            }

            builder.AppendLine();
            builder.AppendLine("Search lines (resolved):");
            foreach (var line in Profile.SearchLines.Where(l => !l.IsHidden).Take(12))
            {
                var label = string.IsNullOrWhiteSpace(line.Label) ? line.Query : line.Label;
                builder.AppendLine($"  {label}  →  {ForYouSearchQuery.Resolve(line)}");
            }

            builder.AppendLine();
            builder.AppendLine("Feed queries:");
            if (LastFeedQueries.Count == 0)
            {
                builder.AppendLine("  (refresh feed to populate)");
            }
            else
            {
                foreach (var query in LastFeedQueries)
                {
                    builder.AppendLine($"  {query}");
                }
            }

            builder.AppendLine();
            builder.AppendLine($"Activities: {Profile.RecentActivity.Count}");
            builder.AppendLine($"Search events: {searchEventCount}");
            builder.AppendLine($"Decay grace: {ForYouTopicDecay.SearchGraceCount} searches without a tag");
            return builder.ToString().TrimEnd();
        }
    }

    public string BuildSyncSummary()
    {
        lock (_sync)
        {
            if (!CloudSyncEnabled)
            {
                return "For You sync off";
            }

            var topics = Profile.Topics.Count(t =>
                !t.IsBlocked && !ForYouLearningFilter.IsExcluded(t.Topic));
            var searches = Profile.SearchLines.Count(l => !l.IsHidden);
            var signals = Profile.RecentActivity.Count;

            return topics == 0 && searches == 0 && signals == 0
                ? "For You: empty profile"
                : $"For You: {topics} topic(s), {searches} search line(s), {signals} signal(s)";
        }
    }

    public IReadOnlyList<SearchPresetRecommendation> BuildSearchIdeas(int take = 8)
    {
        var ranked = GetRankedTopics(6).Where(t => !t.IsBlocked).ToList();
        var results = new List<SearchPresetRecommendation>();

        foreach (var topic in ranked.Take(Math.Min(4, ranked.Count)))
        {
            results.Add(new SearchPresetRecommendation
            {
                PresetId = topic.Topic,
                Name = TopicToTitle(topic.Topic),
                Description = topic.Why,
                Tags = [topic.Topic],
            });
        }

        if (ranked.Count >= 2)
        {
            var pair = ranked.Take(2).Select(t => t.Topic).ToList();
            results.Add(new SearchPresetRecommendation
            {
                PresetId = string.Join("+", pair),
                Name = $"{TopicToTitle(pair[0])} + {TopicToTitle(pair[1])}",
                Description = "Your strongest topics together",
                Tags = pair,
            });
        }

        return results
            .DistinctBy(p => p.PresetId, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .ToList();
    }

    public async Task<ForYouFeedResult> BuildFeedAsync(CancellationToken cancellationToken = default)
    {
        if (!Profile.Enabled)
        {
            return new ForYouFeedResult
            {
                StatusMessage = "Turn on For You in Settings to start learning from your searches and opens.",
            };
        }

        ScheduleOpenAiRefreshIfNeeded();

        var rankedTopics = GetFeedTopics(40);
        var queryTopics = BuildQueryTopics();
        var searchLines = BuildSearchLines();
        var searchIdeas = BuildSearchIdeas();
        var recent = GetRecentActivity();
        var summary = ForYouInterestSummary.Build(rankedTopics, Profile.RecentActivity);
        var sourceNote = BuildSourceNote();

        if (!Rule34Api.HasCredentials(_app.Settings.UserId, _app.Settings.ApiKey))
        {
            return new ForYouFeedResult
            {
                StatusMessage = "Add Rule34 credentials to build a personalized feed.",
                Topics = rankedTopics,
                SearchIdeas = searchIdeas,
                SearchLines = searchLines,
                RecentActivity = recent,
                Summary = summary,
                SourceNote = sourceNote,
                UsedOpenAi = Profile.UsedOpenAi,
            };
        }

        await _buildGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var topicQueries = BuildFeedQueries(searchLines, queryTopics, searchIdeas);
            LastFeedQueries = topicQueries.ToList();
            var topicWeights = BuildTopicFeedWeights(rankedTopics, _app.Settings);
            var candidatePosts = await GatherCandidatePostsAsync(topicQueries, cancellationToken).ConfigureAwait(false);
            var recommendedTags = BuildRecommendedTags(rankedTopics, searchLines, _app.Settings);
            foreach (var ranked in candidatePosts.Values)
            {
                ApplyRecommendedTagOverlapBoost(ranked, recommendedTags);
            }

            var pool = candidatePosts.Values
                .Select(v =>
                {
                    var matchedTopicCount = Math.Max(
                        v.MatchedTopicCount,
                        ForYouTopicFocus.CountTagMatches(
                            v.Post.GetTagList(),
                            topicWeights.Select(tw => tw.Tag).ToList()));
                    var weightedMatchScore = ForYouTopicFocus.ComputeWeightedTopicMatchScore(
                        v.Post.GetTagList(),
                        topicWeights);
                    var trainingScore = ForYouFeedReasonBuilder.ComputeTrainingScore(
                        weightedMatchScore,
                        matchedTopicCount,
                        v.QueryHitCount,
                        v.Post.Score);

                    var item = new ForYouFeedItem
                    {
                        Kind = ForYouRecommendationKind.Post,
                        Title = v.Post.GalleryTitle,
                        Subtitle = BuildPostSubtitle(v.Post),
                        Reason = v.Reason,
                        Topic = v.Topic,
                        Tags = v.Post.GetTagList(),
                        Post = v.Post,
                        Score = v.Score,
                        MatchedTopicCount = matchedTopicCount,
                        WeightedMatchScore = weightedMatchScore,
                        QueryHitCount = v.QueryHitCount,
                        TrainingScore = trainingScore,
                    };
                    ForYouFeedReasonBuilder.Enrich(item, topicWeights);
                    return item;
                })
                .OrderByDescending(v => v.WeightedMatchScore)
                .ThenByDescending(v => v.MatchedTopicCount)
                .ThenByDescending(v => v.QueryHitCount)
                .ThenByDescending(v => v.TrainingScore)
                .ThenByDescending(v => v.Post!.Score)
                .ThenBy(v => v.Post!.Id)
                .Take(ForYouFeedPresentation.PoolLimit)
                .ToList();

            var items = ForYouFeedPresentation.Apply(
                pool,
                _app.Settings.ForYouFeedSort,
                _app.Settings.ForYouFeedMediaFilter);

            return new ForYouFeedResult
            {
                Pool = pool,
                Items = items,
                Topics = rankedTopics,
                SearchIdeas = searchIdeas,
                SearchLines = searchLines,
                RecentActivity = recent,
                Summary = summary,
                SourceNote = sourceNote,
                UsedOpenAi = Profile.UsedOpenAi,
                StatusMessage = items.Count > 0
                    ? BuildFeedStatusMessage(items, rankedTopics, topicQueries.Count())
                    : BuildEmptyFeedStatusMessage(topicQueries.Count(), queryTopics.Count, candidatePosts.Count),
            };
        }
        finally
        {
            _buildGate.Release();
        }
    }

    private void ScheduleOpenAiRefreshIfNeeded()
    {
        if (!Profile.Enabled || !_app.Settings.HasOpenAiForForYou || Profile.RecentActivity.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (now - _lastProfileBuildUnix < 300)
        {
            return;
        }

        _ = RefreshProfileAsync();
    }

    public ForYouCloudProfile ExportCloudProfile()
    {
        lock (_sync)
        {
            var cloud = Profile.ToCloudProfile();
            cloud.Enabled = _app.Settings.ForYouEnabled;
            cloud.CloudSyncEnabled = _app.Settings.ForYouCloudSync;
            return cloud;
        }
    }

    public void ImportProfile(ForYouProfile profile)
    {
        lock (_sync)
        {
            Profile = profile;
            PurgeDisabledCategoryTopicsLocked();
            PurgeExcludedTopicsLocked();
            Profile.Trim();
            SaveLocked();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task SyncFromCloudAsync()
    {
        if (!Profile.CloudSyncEnabled || !_app.Library.IsSignedIn)
        {
            return;
        }

        try
        {
            var cloud = await _app.Library.LoadForYouProfileFromCloudAsync().ConfigureAwait(false);
            if (cloud is null)
            {
                return;
            }

            MergeProfiles(Profile, cloud, preferLocal: false);
            lock (_sync)
            {
                PurgeDisabledCategoryTopicsLocked();
                PurgeExcludedTopicsLocked();
                SaveLocked();
            }

            Changed?.Invoke(this, EventArgs.Empty);
            await SaveToCloudAsync(mergeRemoteFirst: false, overwriteRemote: true).ConfigureAwait(false);
        }
        catch
        {
            // Best-effort sync.
        }
    }

    public async Task SaveToCloudAsync(bool mergeRemoteFirst = true, bool overwriteRemote = false)
    {
        if (!Profile.CloudSyncEnabled || !_app.Library.IsSignedIn)
        {
            return;
        }

        ForYouProfile? snapshot = null;
        try
        {
            if (mergeRemoteFirst && !overwriteRemote)
            {
                var cloud = await _app.Library.LoadForYouProfileFromCloudAsync().ConfigureAwait(false);
                if (cloud is not null)
                {
                    lock (_sync)
                    {
                        MergeProfiles(Profile, cloud, preferLocal: false);
                        SaveLocked();
                    }

                    Changed?.Invoke(this, EventArgs.Empty);
                }
            }

            lock (_sync)
            {
                snapshot = Profile;
            }

            await _app.Library.SaveForYouProfileToCloudAsync(snapshot, allowEmpty: overwriteRemote)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (snapshot?.HasAnySignals == true)
        {
            throw new InvalidOperationException($"For You cloud upload failed: {ex.Message}", ex);
        }
        catch
        {
            // Best effort for empty/background uploads.
        }
    }

    private void ApplySignal(ForYouSignalType type, IEnumerable<string> tags, string detail, double strength)
    {
        var normalizedTags = ForYouLearningGate.FilterLearnable(tags, _app.Settings)
            .Select(t => t.Trim().ToLowerInvariant())
            .Take(12)
            .ToList();

        if (normalizedTags.Count == 0)
        {
            return;
        }

        lock (_sync)
        {
            foreach (var tag in normalizedTags)
            {
                ApplyStrengthLocked(tag, strength, type, detail, recordActivity: true);
            }

            Profile.Trim();
            SaveLocked();
        }

        Changed?.Invoke(this, EventArgs.Empty);
        ScheduleSaveToCloud();
    }

    private void ApplyStrengthLocked(string tag, double strength, ForYouSignalType type, string detail, bool recordActivity)
    {
        if (ForYouLearningFilter.IsExcluded(tag))
        {
            return;
        }

        if (type is not (ForYouSignalType.ManualTopic or ForYouSignalType.TopicBlocked or ForYouSignalType.TopicPinned) &&
            !ForYouLearningGate.CanLearn(tag, _app.Settings))
        {
            return;
        }

        if (Profile.IsTopicRemoved(tag) &&
            type is not (ForYouSignalType.ManualTopic or ForYouSignalType.TopicBlocked))
        {
            return;
        }

        var profile = Profile.GetOrCreateTopic(tag);
        if (profile.IsBlocked && type is not (ForYouSignalType.TopicBlocked or ForYouSignalType.NotInterested or ForYouSignalType.ReportedPost))
        {
            return;
        }

        if (profile.IsManuallyCurated &&
            type is not (ForYouSignalType.ManualTopic or ForYouSignalType.TopicBlocked or ForYouSignalType.NotInterested or ForYouSignalType.ReportedPost))
        {
            if (recordActivity)
            {
                AddActivityLocked(type, tag, detail, strength);
            }

            return;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var delta = strength;
        if (type is ForYouSignalType.TopicBlocked or ForYouSignalType.NotInterested or ForYouSignalType.ReportedPost)
        {
            profile.IsBlocked = true;
            profile.Weight = ForYouSignalStrengths.MinBlockedTopicScore;
        }
        else
        {
            profile.Weight = ForYouSignalStrengths.MergeTopicScore(profile.Weight, strength, Profile.LearningRate);
            if (type == ForYouSignalType.FinishesPostsWithTag && profile.OpenHits >= 2)
            {
                profile.Weight = ForYouSignalStrengths.MergeTopicScore(
                    profile.Weight,
                    ForYouSignalStrengths.FinishesPostsWithTag,
                    Profile.LearningRate);
            }
        }

        profile.LastSeenUnix = now;
        UpdateCounts(profile, type);
        if (recordActivity)
        {
            AddActivityLocked(type, tag, detail, delta);
        }
    }

    private void AddActivityLocked(ForYouSignalType type, string topic, string detail, double delta)
    {
        Profile.RecentActivity.Add(new ForYouActivityEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            SignalType = type,
            Topic = topic,
            Detail = detail,
            TimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            WeightDelta = delta,
        });
    }

    private void RebuildTopicsFromActivityLocked()
    {
        var pinned = Profile.Topics
            .Where(t => t.IsPinned)
            .ToDictionary(t => t.Topic, StringComparer.OrdinalIgnoreCase);
        var manual = Profile.Topics
            .Where(t => t.IsManuallyCurated && !t.IsBlocked)
            .ToDictionary(t => t.Topic, StringComparer.OrdinalIgnoreCase);

        Profile.Topics.Clear();

        foreach (var entry in Profile.RecentActivity.OrderBy(a => a.TimestampUnix))
        {
            ReplayActivityLocked(entry);
        }

        foreach (var (topic, snapshot) in pinned)
        {
            var profile = Profile.GetOrCreateTopic(topic);
            profile.IsPinned = true;
            profile.IsBlocked = false;
            profile.Weight = Math.Max(profile.Weight, snapshot.Weight);
            profile.Reason = string.IsNullOrWhiteSpace(profile.Reason) ? snapshot.Reason : profile.Reason;
        }

        foreach (var (topic, snapshot) in manual)
        {
            var profile = Profile.GetOrCreateTopic(topic);
            if (profile.IsPinned)
            {
                continue;
            }

            profile.Weight = snapshot.Weight;
            profile.Reason = snapshot.Reason;
            profile.ManualAdjustments = snapshot.ManualAdjustments;
            profile.IsBlocked = false;
        }

        ApplyHistoricalSearchDecayLocked();
        PurgeExcludedTopicsLocked();
        Profile.Trim();
        Profile.UpdatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private void ReplayActivityLocked(ForYouActivityEntry entry)
    {
        if (entry.SignalType == ForYouSignalType.ManualTopic)
        {
            var topic = UserSettings.NormalizeTagToken(entry.Topic).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(topic) || ForYouLearningFilter.IsExcluded(topic))
            {
                return;
            }

            var manualStrength = ParseManualTopicStrength(entry.Detail);
            var profile = Profile.GetOrCreateTopic(topic);
            profile.Weight = Math.Clamp(manualStrength, ForYouSignalStrengths.MinTopicScore, ForYouSignalStrengths.MaxTopicScore);
            profile.IsBlocked = false;
            profile.Reason = "Set manually";
            profile.ManualAdjustments++;
            profile.LastSeenUnix = entry.TimestampUnix;
            return;
        }

        var tags = ExtractBoostTagsFromActivity(entry);
        if (tags.Count == 0)
        {
            return;
        }

        var strength = StrengthForReplay(entry);
        var type = entry.SignalType;
        foreach (var tag in tags)
        {
            ApplyStrengthLocked(tag, strength, type, entry.Detail, recordActivity: false);
        }
    }

    private static double ParseManualTopicStrength(string detail)
    {
        if (detail.StartsWith("strength:", StringComparison.OrdinalIgnoreCase) &&
            double.TryParse(detail["strength:".Length..], out var parsed))
        {
            return parsed;
        }

        return 80;
    }

    private static double StrengthForReplay(ForYouActivityEntry entry)
    {
        if (Math.Abs(entry.WeightDelta) > 0.0001)
        {
            return entry.WeightDelta;
        }

        return entry.SignalType switch
        {
            ForYouSignalType.Search => ForYouSignalStrengths.ManualSearch,
            ForYouSignalType.SimilarTagSearch => ForYouSignalStrengths.SimilarTagSearchRepeated,
            ForYouSignalType.SavedTag => ForYouSignalStrengths.SavedTag,
            ForYouSignalType.Favorite => ForYouSignalStrengths.SavedPost,
            ForYouSignalType.WatchLater => ForYouSignalStrengths.SavedPost,
            ForYouSignalType.Download => ForYouSignalStrengths.FinishesPostsWithTag,
            ForYouSignalType.PostOpened => ForYouSignalStrengths.PostOpened,
            ForYouSignalType.PostReopened => ForYouSignalStrengths.PostReopened,
            ForYouSignalType.PostCompleted => ForYouSignalStrengths.FullyWatched,
            ForYouSignalType.LongView => ForYouSignalStrengths.LongView,
            ForYouSignalType.QuickSkip => ForYouSignalStrengths.QuickSkip,
            ForYouSignalType.RepeatedTagView => ForYouSignalStrengths.RepeatedTagView,
            ForYouSignalType.SimilarTag => ForYouSignalStrengths.SimilarTag,
            ForYouSignalType.TagClicked => ForYouSignalStrengths.TagClicked,
            ForYouSignalType.TopicBlocked => ForYouSignalStrengths.BlockedTag,
            ForYouSignalType.NotInterested => ForYouSignalStrengths.NotInterested,
            ForYouSignalType.ReportedPost => ForYouSignalStrengths.ReportedPost,
            ForYouSignalType.TopicWeightAdjusted => 0.2,
            _ => 0.4,
        };
    }

    private static List<string> ExtractBoostTagsFromActivity(ForYouActivityEntry entry)
    {
        var tags = new List<string>();
        if (!string.IsNullOrWhiteSpace(entry.Topic))
        {
            tags.Add(UserSettings.NormalizeTagToken(entry.Topic).Trim().ToLowerInvariant());
        }

        if (entry.SignalType == ForYouSignalType.Search &&
            !string.IsNullOrWhiteSpace(entry.Detail) &&
            entry.Detail.Contains(' '))
        {
            tags.AddRange(ForYouLearningFilter.Sanitize(
                entry.Detail.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)));
        }

        return tags
            .Where(t => !string.IsNullOrWhiteSpace(t) && !ForYouLearningFilter.IsExcluded(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private bool NormalizeTopicWeightsLocked()
    {
        var migrated = Profile.ScoreSchemaVersion < ForYouSignalStrengths.CurrentScoreSchemaVersion;
        var previousVersion = Profile.ScoreSchemaVersion;
        if (migrated)
        {
            if (Profile.ScoreSchemaVersion < 2)
            {
                ForYouSignalStrengths.RescaleLegacyTopicScores(Profile.Topics);
            }

            Profile.ScoreSchemaVersion = ForYouSignalStrengths.CurrentScoreSchemaVersion;
        }

        ForYouSignalStrengths.MigrateProfileTopicScores(Profile.Topics);
        foreach (var topic in Profile.Topics)
        {
            topic.Weight = ForYouSignalStrengths.NormalizeTopicScore(topic.Weight);
            if (topic.IsBlocked && topic.Weight > -0.5)
            {
                topic.Weight = ForYouSignalStrengths.MinBlockedTopicScore;
            }
        }

        if (migrated && previousVersion < 3)
        {
            ApplyHistoricalSearchDecayLocked();
        }

        foreach (var line in Profile.SearchLines)
        {
            if (line.Score > 100)
            {
                line.Score = Math.Clamp(line.Score, 0, 100);
            }
        }

        return migrated;
    }

    private void ApplyHistoricalSearchDecayLocked()
    {
        var searches = ForYouTopicDecay.ListSearchEvents(Profile.RecentActivity).Reverse().ToList();
        foreach (var topic in Profile.Topics)
        {
            if (topic.IsPinned || topic.IsBlocked || topic.IsManuallyCurated)
            {
                continue;
            }

            var penalty = ForYouTopicDecay.DecayPenalty(
                ForYouTopicDecay.SearchesSinceLastHit(topic.Topic, searches));
            if (penalty <= 0)
            {
                continue;
            }

            topic.Weight = ForYouSignalStrengths.NormalizeTopicScore(topic.Weight - penalty);
        }
    }

    private void ApplySearchDecayLocked()
    {
        var searches = ForYouTopicDecay.ListSearchEvents(Profile.RecentActivity).Reverse().ToList();
        foreach (var topic in Profile.Topics)
        {
            if (topic.IsPinned || topic.IsBlocked || topic.IsManuallyCurated)
            {
                continue;
            }

            var since = ForYouTopicDecay.SearchesSinceLastHit(topic.Topic, searches);
            if (since <= ForYouTopicDecay.SearchGraceCount)
            {
                continue;
            }

            topic.Weight = ForYouSignalStrengths.MergeTopicScore(topic.Weight, -1.0);
        }
    }

    private void MarkTopicNewLocked(string topic)
    {
        var normalized = UserSettings.NormalizeTagToken(topic).Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            _newTopicHighlights.Add(normalized);
        }
    }

    private void ScheduleSaveToCloud(bool overwriteRemote = false)
    {
        if (!Profile.CloudSyncEnabled || !_app.Library.IsSignedIn)
        {
            return;
        }

        _ = SaveToCloudAsync(mergeRemoteFirst: !overwriteRemote, overwriteRemote: overwriteRemote);
    }

    private static void UpdateCounts(ForYouTopicProfile profile, ForYouSignalType type)
    {
        switch (type)
        {
            case ForYouSignalType.Search:
            case ForYouSignalType.SimilarTagSearch:
            case ForYouSignalType.SavedTag:
                profile.SearchHits++;
                break;
            case ForYouSignalType.PostOpened:
            case ForYouSignalType.PostReopened:
            case ForYouSignalType.PostCompleted:
            case ForYouSignalType.LongView:
            case ForYouSignalType.RepeatedTagView:
                profile.OpenHits++;
                break;
            case ForYouSignalType.Favorite:
                profile.FavoriteHits++;
                break;
            case ForYouSignalType.WatchLater:
                profile.WatchLaterHits++;
                break;
            case ForYouSignalType.Download:
                profile.DownloadHits++;
                break;
        }
    }

    private ForYouTopicProfile GetTopicProfileLocked(string topic)
    {
        var normalized = UserSettings.NormalizeTagToken(topic).Trim().ToLowerInvariant();
        return Profile.GetOrCreateTopic(normalized);
    }

    private void Touch()
    {
        lock (_sync)
        {
            Profile.UpdatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Profile.Trim();
            SaveLocked();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void SaveLocked()
    {
        Profile.UpdatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        _store.Save(Profile);
    }

    private void PurgeExcludedTopicsLocked()
    {
        Profile.Topics.RemoveAll(t =>
            ForYouLearningFilter.IsExcluded(t.Topic) && !t.IsPinned && !t.IsManuallyCurated);
        Profile.RecentActivity.RemoveAll(a => ForYouLearningFilter.IsExcluded(a.Topic));
        Profile.SearchLines.RemoveAll(line =>
        {
            if (ForYouLearningFilter.IsExcluded(line.Query))
            {
                return true;
            }

            var tokens = line.Query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return !ForYouLearningFilter.Sanitize(tokens).Any();
        });
    }

    private IReadOnlyList<string> BuildFeedQueries(
        IReadOnlyList<ForYouSearchLine> searchLines,
        IReadOnlyList<ForYouTopicProfile> rankedTopics,
        IReadOnlyList<SearchPresetRecommendation> searchIdeas)
    {
        var ordered = new List<string>();
        void AddUnique(string query)
        {
            var trimmed = query.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || ForYouSearchQuery.IsGenericQuery(trimmed))
            {
                return;
            }

            if (ordered.Any(q => q.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            ordered.Add(trimmed);
        }

        void AddLine(ForYouSearchLine line) => AddUnique(ForYouSearchQuery.Resolve(line));

        var scoredTopics = rankedTopics
            .Where(t => !t.IsBlocked && (t.Weight > 0 || t.Score > 0 || t.IsManuallyCurated || t.IsPinned))
            .Where(t => t.IsManuallyCurated || t.IsPinned || ForYouLearningGate.CanLearn(t.Topic, _app.Settings))
            .OrderByDescending(t => t.IsPinned)
            .ThenByDescending(t => t.Weight)
            .Select(t => UserSettings.NormalizeTagToken(t.Topic).ToLowerInvariant())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        foreach (var tag in scoredTopics.Take(5))
        {
            AddUnique(tag);
        }

        if (scoredTopics.Count >= 2)
        {
            AddUnique(string.Join(' ', scoredTopics.Take(2)));
        }

        if (scoredTopics.Count >= 3)
        {
            AddUnique(string.Join(' ', scoredTopics.Take(3)));
        }

        foreach (var tag in scoredTopics.Where(ForYouTopicFocus.IsCharacterTopic).Take(2))
        {
            AddUnique(tag);
        }

        foreach (var line in searchLines
                     .Where(l => !l.IsHidden)
                     .Where(line =>
                         ForYouTopicFocus.IsCharacterTopic(line.Query) ||
                         line.ApiTags.Any(ForYouTopicFocus.IsCharacterTopic) ||
                         scoredTopics.Any(tag => line.Query.Contains(tag, StringComparison.OrdinalIgnoreCase)))
                     .OrderByDescending(l => l.Score))
        {
            AddLine(line);
        }

        foreach (var line in searchLines.Where(l => !l.IsHidden && l.IsPinned))
        {
            AddLine(line);
        }

        foreach (var line in searchLines.Where(l => !l.IsHidden).OrderByDescending(l => l.Score))
        {
            AddLine(line);
        }

        if (ordered.Count == 0)
        {
            foreach (var idea in searchIdeas.Take(2))
            {
                if (idea.Tags.Count > 0)
                {
                    AddUnique(string.Join(' ', idea.Tags));
                }
            }
        }

        if (ordered.Count == 0)
        {
            var manualTags = _app.Settings.GetManualIncludeTags().Take(4).ToList();
            if (manualTags.Count > 0)
            {
                AddUnique(string.Join(' ', manualTags));
            }
        }

        if (ordered.Count == 0)
        {
            return ["sort:score:desc"];
        }

        return ordered.Take(6).ToList();
    }

    private async Task<Dictionary<int, RankedPost>> GatherCandidatePostsAsync(
        IEnumerable<string> topicQueries,
        CancellationToken cancellationToken)
    {
        var map = new Dictionary<int, RankedPost>();
        var queries = topicQueries.ToList();
        if (queries.Count == 0)
        {
            return map;
        }

        var limit = _app.GetSelectedLimit();
        var userId = _app.Settings.UserId.Trim();
        var apiKey = _app.Settings.ApiKey.Trim();
        var gate = new SemaphoreSlim(3);

        var tasks = queries.Select(async query =>
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var searchSettings = BuildSearchSettings(query);
                var apiTags = SearchQueryBuilder.BuildApiTags(searchSettings);
                var url = Rule34Api.BuildPostSearchUrl(0, limit, apiTags, userId, apiKey);
                var json = await Rule34Api.FetchPostSearchJsonAsync(_app.Http, url).ConfigureAwait(false);
                var posts = Rule34Api.ParsePostSearchResponse(json, out var error);
                if (!string.IsNullOrWhiteSpace(error))
                {
                    return;
                }

                foreach (var post in TagBlockFilter.Apply(
                             posts.Where(p => PostSearchFilter.Matches(p, searchSettings)),
                             searchSettings))
                {
                    var score = ScorePost(post, query);
                    if (map.TryGetValue(post.Id, out var existing))
                    {
                        existing.QueryHitCount++;
                        existing.Score += score + 35;
                        if (score > existing.SourceScore)
                        {
                            existing.SourceScore = score;
                            existing.Topic = query;
                            existing.Reason = BuildReason(query, post, score);
                        }
                    }
                    else
                    {
                        map[post.Id] = new RankedPost
                        {
                            Post = post,
                            Score = score,
                            SourceScore = score,
                            QueryHitCount = 1,
                            Topic = query,
                            Reason = BuildReason(query, post, score),
                        };
                    }
                }
            }
            catch
            {
                // Keep the feed resilient: one failed query should not take down the page.
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
        return map;
    }

    private UserSettings BuildSearchSettings(string query)
    {
        var includeTags = query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ForYouSearchQuery.NormalizeToken)
            .Where(t => !string.IsNullOrWhiteSpace(t) && !t.StartsWith("sort:", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new UserSettings
        {
            ActiveSource = _app.Settings.ActiveSource,
            IncludeTags = includeTags,
            Tags = string.Join(' ', includeTags),
            RatingSafe = _app.Settings.RatingSafe,
            RatingQuestionable = _app.Settings.RatingQuestionable,
            RatingExplicit = _app.Settings.RatingExplicit,
            MediaFilter = MediaFilterMode.All,
            SortMode = SearchSortMode.ScoreDesc,
            MinScore = _app.Settings.MinScore,
            MinWidth = _app.Settings.MinWidth,
            MinHeight = _app.Settings.MinHeight,
            ArtistFilter = _app.Settings.ArtistFilter,
            CharacterFilter = _app.Settings.CharacterFilter,
            CopyrightFilter = _app.Settings.CopyrightFilter,
            FilterAi = _app.Settings.FilterAi,
            BlacklistTags = _app.Settings.BlacklistTags,
            GlobalBlockedTags = _app.Settings.GlobalBlockedTags,
            ActiveSearchPresetIds = [],
        };
    }

    private IReadOnlyList<ForYouTopicProfile> BuildQueryTopics()
    {
        lock (_sync)
        {
            return Profile.Topics
                .Where(t => !string.IsNullOrWhiteSpace(t.Topic))
                .Where(t => !t.IsBlocked && !ForYouLearningFilter.IsExcluded(t.Topic))
                .Where(t => t.IsManuallyCurated ||
                            t.IsPinned ||
                            t.Score > 0 ||
                            t.Weight > 0 ||
                            ForYouLearningGate.CanLearn(t.Topic, _app.Settings))
                .OrderByDescending(t => t.IsPinned)
                .ThenByDescending(t => t.IsManuallyCurated)
                .ThenByDescending(t => t.Weight)
                .ThenByDescending(t => t.Score)
                .Take(60)
                .ToList();
        }
    }

    private string BuildEmptyFeedStatusMessage(int queryCount, int topicCount, int candidateCount)
    {
        if (topicCount == 0)
        {
            return "No active topics for your learning settings. Add or promote series topics, or turn on more learning categories.";
        }

        if (queryCount == 0)
        {
            return "Could not build search lines from your topics. Try adding a manual series topic.";
        }

        if (candidateCount == 0)
        {
            return $"Searched {queryCount} topic line(s) but got no results. Check Rule34 credentials or try different topics.";
        }

        return "No posts matched your sort/filter. Try Sort & filter → All.";
    }

    private static double ScorePost(PostItem post, string query)
    {
        var score = Math.Min(post.Score * 0.04, 20.0);
        var tags = post.GetTagList();
        var queryParts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var queryTagHits = 0;
        foreach (var tag in tags)
        {
            var lower = tag.ToLowerInvariant();
            if (queryParts.Contains(lower))
            {
                score += 12;
                queryTagHits++;
            }
            else if (queryParts.Any(part => lower.Contains(part, StringComparison.OrdinalIgnoreCase) ||
                                            part.Contains(lower, StringComparison.OrdinalIgnoreCase)))
            {
                score += 4;
                queryTagHits++;
            }
        }

        if (queryParts.Count >= 2 && queryTagHits >= queryParts.Count)
        {
            score += 25 + queryParts.Count * 10;
        }

        return score;
    }

    private static List<(string Tag, double Weight)> BuildTopicFeedWeights(
        IReadOnlyList<ForYouTopicProfile> rankedTopics,
        UserSettings settings)
    {
        return ForYouLearningGate.FilterForFeed(rankedTopics, settings)
            .Where(t => !t.IsBlocked && t.Weight > 0)
            .Select(t =>
            {
                var normalized = UserSettings.NormalizeTagToken(t.Topic).ToLowerInvariant();
                var weight = Math.Max(t.Weight / 100.0, 0.05);
                if (t.IsPinned)
                {
                    weight = Math.Min(1.0, weight * 1.25);
                }

                return (Tag: normalized, Weight: weight);
            })
            .Where(t => !string.IsNullOrWhiteSpace(t.Tag) && !ForYouLearningFilter.IsExcluded(t.Tag))
            .ToList();
    }

    private static List<(string Tag, double Weight)> BuildRecommendedTags(
        IReadOnlyList<ForYouTopicProfile> rankedTopics,
        IReadOnlyList<ForYouSearchLine> searchLines,
        UserSettings settings)
    {
        var map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var topic in ForYouLearningGate.FilterForFeed(rankedTopics, settings).Where(t => !t.IsBlocked))
        {
            var normalized = UserSettings.NormalizeTagToken(topic.Topic).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalized) || ForYouLearningFilter.IsExcluded(normalized))
            {
                continue;
            }

            if (topic.Score <= 0)
            {
                continue;
            }

            var weight = Math.Max(topic.Score / 100.0, 0.05);
            if (topic.IsPinned)
            {
                weight = Math.Min(1.0, weight * 1.25);
            }

            map[normalized] = map.TryGetValue(normalized, out var existing)
                ? Math.Max(existing, weight)
                : weight;
        }

        foreach (var line in searchLines.Where(l => !l.IsHidden))
        {
            var lineWeight = Math.Max(line.Score, 10) / 10.0;
            foreach (var token in line.Query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var normalized = UserSettings.NormalizeTagToken(token).ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(normalized) ||
                    ForYouLearningFilter.IsExcluded(normalized) ||
                    !ForYouLearningGate.CanLearn(normalized, settings))
                {
                    continue;
                }

                map[normalized] = map.TryGetValue(normalized, out var existing)
                    ? Math.Max(existing, lineWeight)
                    : lineWeight;
            }
        }

        return map
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Take(24)
            .Select(kv => (kv.Key, kv.Value))
            .ToList();
    }

    private static void ApplyRecommendedTagOverlapBoost(
        RankedPost ranked,
        IReadOnlyList<(string Tag, double Weight)> recommendedTags)
    {
        if (recommendedTags.Count == 0)
        {
            return;
        }

        var postTags = ranked.Post.GetTagList();
        var matched = new List<(string Tag, double Weight)>();
        foreach (var (tag, weight) in recommendedTags)
        {
            if (PostMatchesRecommendedTag(postTags, tag))
            {
                matched.Add((tag, weight));
            }
        }

        if (matched.Count == 0)
        {
            return;
        }

        ranked.MatchedTopicCount = matched.Count;
        var weightSum = matched.Sum(m => m.Weight);
        var boost = matched.Count switch
        {
            1 => 20 + weightSum * 2,
            2 => 150 + weightSum * 10,
            3 => 350 + weightSum * 15,
            _ => matched.Count * matched.Count * 60 + weightSum * 20,
        };

        ranked.Score += boost + ranked.QueryHitCount * 40;

        if (matched.Count >= 2)
        {
            var labels = matched
                .Take(4)
                .Select(m => m.Tag.Replace('_', ' '))
                .ToList();
            ranked.Reason = $"Matches {matched.Count} of your topics ({string.Join(", ", labels)})";
        }
    }

    private static bool PostMatchesRecommendedTag(IReadOnlyList<string> postTags, string recommendedTag)
        => ForYouTagMatching.PostHasTag(postTags, recommendedTag);

    private static string BuildFeedStatusMessage(
        IReadOnlyList<ForYouFeedItem> items,
        IReadOnlyList<ForYouTopicProfile> rankedTopics,
        int queryCount)
    {
        var topTopics = rankedTopics
            .Where(t => !t.IsBlocked && t.Score > 0)
            .OrderByDescending(t => t.IsPinned)
            .ThenByDescending(t => t.Score)
            .Take(3)
            .Select(t => t.Topic.Replace('_', ' '))
            .ToList();

        var avgMatches = items.Average(i => i.MatchedTopicCount);
        if (topTopics.Count == 0)
        {
            return $"Showing {items.Count} posts from {queryCount} learned search line(s).";
        }

        var topicText = string.Join(", ", topTopics);
        return avgMatches >= 1.5
            ? $"Showing {items.Count} posts matched to your topics ({topicText}). Avg {avgMatches:F1} topic hits/post."
            : $"Showing {items.Count} posts from your topics ({topicText}).";
    }

    private static string BuildReason(string query, PostItem post, double score)
    {
        var tags = post.GetTagList().Take(3).Select(t => t.Replace('_', ' ')).ToList();
        var tagText = tags.Count == 0 ? "tag match" : string.Join(", ", tags);
        return $"Matches {query} · {tagText}";
    }

    private static string BuildPostSubtitle(PostItem post)
    {
        var tags = post.GetTagList().Take(4).Select(t => t.Replace('_', ' ')).ToList();
        return tags.Count == 0 ? $"Score {post.Score}" : string.Join(" · ", tags);
    }

    private static string TopicToTitle(string topic)
    {
        var cleaned = topic.Replace('_', ' ').Trim();
        if (cleaned.Length == 0)
        {
            return "Topic";
        }

        return char.ToUpperInvariant(cleaned[0]) + cleaned[1..];
    }

    private static void MergeProfiles(ForYouProfile target, ForYouProfile source, bool preferLocal)
    {
        if (source.ScoreSchemaVersion < ForYouSignalStrengths.CurrentScoreSchemaVersion)
        {
            ForYouSignalStrengths.RescaleLegacyTopicScores(source.Topics);
            source.ScoreSchemaVersion = ForYouSignalStrengths.CurrentScoreSchemaVersion;
        }

        target.ScoreSchemaVersion = Math.Max(target.ScoreSchemaVersion, source.ScoreSchemaVersion);
        var topicMap = target.Topics.ToDictionary(t => t.Topic, StringComparer.OrdinalIgnoreCase);
        foreach (var removed in source.RemovedTopics)
        {
            target.MarkTopicRemoved(removed);
        }

        foreach (var topic in source.Topics)
        {
            if (target.IsTopicRemoved(topic.Topic))
            {
                continue;
            }

            if (!topicMap.TryGetValue(topic.Topic, out var existing))
            {
                target.Topics.Add(topic);
                continue;
            }

            var localWeight = ForYouSignalStrengths.NormalizeTopicScore(existing.Weight);
            var remoteWeight = ForYouSignalStrengths.NormalizeTopicScore(topic.Weight);
            existing.Weight = Math.Max(localWeight, remoteWeight);
            existing.IsPinned |= topic.IsPinned;
            existing.IsBlocked |= topic.IsBlocked;
            existing.LastSeenUnix = Math.Max(existing.LastSeenUnix, topic.LastSeenUnix);
            existing.SearchHits += topic.SearchHits;
            existing.OpenHits += topic.OpenHits;
            existing.FavoriteHits += topic.FavoriteHits;
            existing.WatchLaterHits += topic.WatchLaterHits;
            existing.DownloadHits += topic.DownloadHits;
            existing.ManualAdjustments += topic.ManualAdjustments;
            if (string.IsNullOrWhiteSpace(existing.Reason))
            {
                existing.Reason = topic.Reason;
            }
        }

        var lineMap = target.SearchLines.ToDictionary(l => l.Query, StringComparer.OrdinalIgnoreCase);
        foreach (var line in source.SearchLines)
        {
            if (target.IsTopicRemoved(line.Query))
            {
                continue;
            }

            if (!lineMap.TryGetValue(line.Query, out var existing))
            {
                target.SearchLines.Add(CloneSearchLine(line));
                continue;
            }

            existing.Score = Math.Max(existing.Score, line.Score);
            existing.IsPinned |= line.IsPinned;
            existing.IsHidden |= line.IsHidden;
            if (string.IsNullOrWhiteSpace(existing.Reason))
            {
                existing.Reason = line.Reason;
            }
        }

        var seen = target.RecentActivity
            .Select(a => string.IsNullOrWhiteSpace(a.Id) ? $"{a.SignalType}:{a.Topic}:{a.TimestampUnix}" : a.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var activity in source.RecentActivity)
        {
            var key = string.IsNullOrWhiteSpace(activity.Id)
                ? $"{activity.SignalType}:{activity.Topic}:{activity.TimestampUnix}"
                : activity.Id;
            if (seen.Add(key))
            {
                target.RecentActivity.Add(activity);
            }
        }

        if (string.IsNullOrWhiteSpace(target.Summary) && !string.IsNullOrWhiteSpace(source.Summary))
        {
            target.Summary = source.Summary;
        }

        target.UsedOpenAi |= source.UsedOpenAi;
        target.Enabled |= source.Enabled;
        target.CloudSyncEnabled = target.CloudSyncEnabled || source.CloudSyncEnabled;
        target.UpdatedAtUnix = Math.Max(target.UpdatedAtUnix, source.UpdatedAtUnix);
        target.Trim();
    }

    private ForYouSearchLine GetOrCreateSearchLineLocked(string query)
    {
        var normalized = query.Trim();
        var existing = Profile.SearchLines.FirstOrDefault(l =>
            l.Query.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var created = new ForYouSearchLine
        {
            Query = normalized,
            Label = normalized,
            Source = "Manual",
        };
        Profile.SearchLines.Add(created);
        return created;
    }

    private static ForYouSearchLine CloneSearchLine(ForYouSearchLine line) => new()
    {
        Query = line.Query,
        Label = line.Label,
        Reason = line.Reason,
        Score = line.Score,
        IsPinned = line.IsPinned,
        IsHidden = line.IsHidden,
        Source = line.Source,
        ApiTags = line.ApiTags.ToList(),
    };

    private void MergeAiProfileLocked(OpenAiForYouProfileResult ai)
    {
        foreach (var topic in ai.Topics)
        {
            if (ForYouLearningFilter.IsExcluded(topic.Topic) ||
                Profile.IsTopicRemoved(topic.Topic) ||
                !ForYouLearningGate.CanLearn(topic.Topic, _app.Settings))
            {
                continue;
            }

            var existing = Profile.GetOrCreateTopic(topic.Topic);
            if (!existing.IsManuallyCurated)
            {
                if (topic.IsBlocked)
                {
                    existing.IsBlocked = true;
                    existing.Weight = Math.Min(
                        ForYouSignalStrengths.NormalizeTopicScore(existing.Weight),
                        ForYouSignalStrengths.NormalizeTopicScore(topic.Weight));
                }
                else
                {
                    existing.Weight = ForYouSignalStrengths.MergeTopicScore(
                        ForYouSignalStrengths.NormalizeTopicScore(existing.Weight),
                        ForYouSignalStrengths.AiTopicHint,
                        Profile.LearningRate);
                }
            }

            existing.IsPinned |= topic.IsPinned;
            if (string.IsNullOrWhiteSpace(existing.Reason) && !string.IsNullOrWhiteSpace(topic.Reason))
            {
                existing.Reason = topic.Reason;
            }
        }

        var lineMap = Profile.SearchLines.ToDictionary(l => l.Query, StringComparer.OrdinalIgnoreCase);
        foreach (var line in ai.SearchLines)
        {
            if (lineMap.TryGetValue(line.Query, out var existing))
            {
                existing.Score = Math.Max(existing.Score, Math.Min(line.Score, 40));
                if (string.IsNullOrWhiteSpace(existing.Reason))
                {
                    existing.Reason = line.Reason;
                }
                existing.Source = line.Source;
            }
            else
            {
                Profile.SearchLines.Add(CloneSearchLine(line));
            }
        }
    }

    private static string BuildLocalSummary(
        IReadOnlyList<ForYouTopicProfile> topics,
        IEnumerable<ForYouActivityEntry> activities)
        => ForYouInterestSummary.Build(topics, activities);

    private string BuildSourceNote()
    {
        if (Profile.UsedOpenAi && _app.Settings.HasOpenAiForForYou)
        {
            return "Profile summarized with OpenAI from your recent activity.";
        }

        if (Profile.RecentActivity.Count > 0)
        {
            return "Built from your recent searches and opens on this device.";
        }

        return string.Empty;
    }

    private sealed class RankedPost
    {
        public required PostItem Post { get; init; }

        public double Score { get; set; }

        public double SourceScore { get; set; }

        public int MatchedTopicCount { get; set; }

        public int QueryHitCount { get; set; } = 1;

        public string Topic { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;
    }
}
