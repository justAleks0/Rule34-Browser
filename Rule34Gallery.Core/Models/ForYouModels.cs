namespace Rule34Gallery.Core;

public enum ForYouSignalType
{
    Search,
    ManualTopic,
    SavedTag,
    Favorite,
    WatchLater,
    Download,
    RepeatedTagView,
    SimilarTagSearch,
    PostReopened,
    PostCompleted,
    FinishesPostsWithTag,
    SimilarTag,
    TagClicked,
    PostOpened,
    LongView,
    QuickSkip,
    NotInterested,
    ReportedPost,
    RecommendationLiked,
    RecommendationDismissed,
    TopicPinned,
    TopicBlocked,
    TopicWeightAdjusted,
}

public enum ForYouFeedSortMode
{
    MostTagMatches,
    HighestScore,
    Random,
}

public enum ForYouRecommendationKind
{
    Topic,
    SearchLine,
    Post,
}

public sealed class ForYouTopicProfile
{
    public string Topic { get; set; } = string.Empty;

    public double Weight { get; set; }

    /// <summary>Interest score 0–100 (decimals). Learning signals still apply in 0–1 space.</summary>
    public double Score => Services.ForYouSignalStrengths.NormalizeTopicScore(Weight);

    /// <summary>Alias for <see cref="Score"/>.</summary>
    public double Strength => Score;

    public bool IsPinned { get; set; }

    public bool IsBlocked { get; set; }

    public long LastSeenUnix { get; set; }

    public int SearchHits { get; set; }

    public int OpenHits { get; set; }

    public int FavoriteHits { get; set; }

    public int WatchLaterHits { get; set; }

    public int DownloadHits { get; set; }

    public int ManualAdjustments { get; set; }

    public string Reason { get; set; } = string.Empty;

    public bool IsManuallyCurated =>
        ManualAdjustments > 0 ||
        Reason.Equals("Set manually", StringComparison.OrdinalIgnoreCase) ||
        Reason.Equals("Added manually", StringComparison.OrdinalIgnoreCase) ||
        Reason.Equals("Boosted from signal", StringComparison.OrdinalIgnoreCase) ||
        Reason.Equals("Promoted to manual", StringComparison.OrdinalIgnoreCase);

    public string Why => !string.IsNullOrWhiteSpace(Reason)
        ? Reason
        : IsBlocked
        ? "Hidden by you"
        : IsPinned
            ? "Pinned by you"
            : Score >= 75
                ? "Strong signal"
                : Score >= 45
                    ? "Recent interest"
                    : "Light signal";
}

public sealed class ForYouActivityEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public ForYouSignalType SignalType { get; set; }

    public string Topic { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public long TimestampUnix { get; set; }

    public double WeightDelta { get; set; }

    public string DisplayLabel => SignalType switch
    {
        ForYouSignalType.Search => $"Search: {Topic}",
        ForYouSignalType.ManualTopic => $"Manual: {Topic}",
        ForYouSignalType.SavedTag => $"Saved tag: {Topic}",
        ForYouSignalType.PostOpened => $"Opened: {Topic}",
        ForYouSignalType.PostReopened => $"Reopened: {Topic}",
        ForYouSignalType.PostCompleted => $"Finished viewing: {Topic}",
        ForYouSignalType.RepeatedTagView => $"Repeat view: {Topic}",
        ForYouSignalType.Favorite => $"Favorite: {Topic}",
        ForYouSignalType.WatchLater => "Watch later",
        ForYouSignalType.Download => "Download",
        ForYouSignalType.TagClicked => $"Clicked tag: {Topic}",
        ForYouSignalType.LongView => $"Long view: {Topic}",
        ForYouSignalType.QuickSkip => $"Quick skip: {Topic}",
        ForYouSignalType.TopicBlocked => $"Blocked: {Topic}",
        ForYouSignalType.NotInterested => $"Not interested: {Topic}",
        _ => $"{SignalType}: {Topic}",
    };
}

public sealed class ForYouSearchLine
{
    public string Query { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public double Score { get; set; }

    public bool IsPinned { get; set; }

    public bool IsHidden { get; set; }

    public string Source { get; set; } = string.Empty;

    public List<string> ApiTags { get; set; } = [];

    public IReadOnlyList<string> Tags =>
        Query.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(UserSettings.NormalizeTagToken)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}

public sealed class ForYouFeedItem
{
    public ForYouRecommendationKind Kind { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string Topic { get; set; } = string.Empty;

    public IReadOnlyList<string> Tags { get; set; } = [];

    public PostItem? Post { get; set; }

    public double Score { get; set; }

    public int MatchedTopicCount { get; set; }

    public double WeightedMatchScore { get; set; }

    public int QueryHitCount { get; set; }

    /// <summary>Combined training relevance used for feed ranking.</summary>
    public double TrainingScore { get; set; }
}

public sealed class ForYouFeedResult
{
    public IReadOnlyList<ForYouFeedItem> Items { get; set; } = [];

    /// <summary>Full candidate pool before sort/filter/limit — used to re-present without refetching.</summary>
    public IReadOnlyList<ForYouFeedItem> Pool { get; set; } = [];

    public IReadOnlyList<ForYouTopicProfile> Topics { get; set; } = [];

    public IReadOnlyList<SearchPresetRecommendation> SearchIdeas { get; set; } = [];

    public IReadOnlyList<ForYouSearchLine> SearchLines { get; set; } = [];

    public IReadOnlyList<ForYouActivityEntry> RecentActivity { get; set; } = [];

    public string Summary { get; set; } = string.Empty;

    public string SourceNote { get; set; } = string.Empty;

    public bool UsedOpenAi { get; set; }

    public bool IsEmpty => Items.Count == 0 && Topics.Count == 0 && SearchIdeas.Count == 0 && SearchLines.Count == 0;

    public string StatusMessage { get; set; } = string.Empty;
}

public sealed class ForYouCloudActivity
{
    public string Kind { get; set; } = string.Empty;

    public long TimestampUtc { get; set; }

    public string Topic { get; set; } = string.Empty;

    public string SearchText { get; set; } = string.Empty;

    public string PostId { get; set; } = string.Empty;

    public string PostTitle { get; set; } = string.Empty;

    public string PostTags { get; set; } = string.Empty;

    public string Context { get; set; } = string.Empty;

    public string SourcePageId { get; set; } = string.Empty;
}

public sealed class ForYouCloudSearchLine
{
    public string Query { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public double Score { get; set; }

    public bool IsPinned { get; set; }

    public bool IsHidden { get; set; }

    public string Source { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = [];
}

public sealed class ForYouCloudProfile
{
    public bool Enabled { get; set; }

    public bool CloudSyncEnabled { get; set; } = true;

    public double LearningRate { get; set; } = 1.0;

    public int ScoreSchemaVersion { get; set; }

    public long UpdatedAtUnix { get; set; }

    public string Summary { get; set; } = string.Empty;

    public bool UsedOpenAi { get; set; }

    public List<ForYouTopicProfile> Topics { get; set; } = [];

    public List<ForYouCloudSearchLine> SearchLines { get; set; } = [];

    public List<ForYouCloudActivity> Activities { get; set; } = [];

    public List<string> RemovedTopics { get; set; } = [];
}
