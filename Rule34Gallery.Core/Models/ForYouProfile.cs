using System.Text.Json.Serialization;

namespace Rule34Gallery.Core;

public sealed class ForYouProfile
{
    public bool Enabled { get; set; }

    public bool CloudSyncEnabled { get; set; } = true;

    public double LearningRate { get; set; } = 1.0;

    public int ScoreSchemaVersion { get; set; }

    public long UpdatedAtUnix { get; set; }

    public List<ForYouTopicProfile> Topics { get; set; } = [];

    public List<ForYouSearchLine> SearchLines { get; set; } = [];

    public List<ForYouActivityEntry> RecentActivity { get; set; } = [];

    /// <summary>Topics the user explicitly removed; kept so sync/rebuild does not resurrect them.</summary>
    public List<string> RemovedTopics { get; set; } = [];

    public string Summary { get; set; } = string.Empty;

    public bool UsedOpenAi { get; set; }

    [JsonIgnore]
    public bool HasAnySignals => Topics.Count > 0 || RecentActivity.Count > 0 || SearchLines.Count > 0;

    public bool IsTopicRemoved(string topic)
    {
        var normalized = UserSettings.NormalizeTagToken(topic).Trim().ToLowerInvariant();
        return !string.IsNullOrWhiteSpace(normalized) &&
               RemovedTopics.Contains(normalized, StringComparer.OrdinalIgnoreCase);
    }

    public void MarkTopicRemoved(string topic)
    {
        var normalized = UserSettings.NormalizeTagToken(topic).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        if (!RemovedTopics.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            RemovedTopics.Add(normalized);
        }

        Topics.RemoveAll(t => t.Topic.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        SearchLines.RemoveAll(line => line.Query.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (RemovedTopics.Count > 300)
        {
            RemovedTopics = RemovedTopics
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .TakeLast(300)
                .ToList();
        }
    }

    public void UnmarkTopicRemoved(string topic)
    {
        var normalized = UserSettings.NormalizeTagToken(topic).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        RemovedTopics.RemoveAll(t => t.Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }

    public ForYouTopicProfile GetOrCreateTopic(string topic)
    {
        var normalized = UserSettings.NormalizeTagToken(topic).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = string.Empty;
        }

        if (IsTopicRemoved(normalized))
        {
            return Topics.FirstOrDefault(t => t.Topic.Equals(normalized, StringComparison.OrdinalIgnoreCase))
                ?? new ForYouTopicProfile { Topic = normalized };
        }

        var existing = Topics.FirstOrDefault(t => t.Topic.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var created = new ForYouTopicProfile { Topic = normalized };
        Topics.Add(created);
        return created;
    }

    public void Trim(int topicLimit = 120, int activityLimit = 240)
    {
        var manual = Topics.Where(t => t.IsManuallyCurated).ToList();
        var algorithm = Topics
            .Where(t => !t.IsManuallyCurated)
            .OrderByDescending(t => t.IsPinned)
            .ThenBy(t => t.IsBlocked ? 1 : 0)
            .ThenByDescending(t => t.Weight)
            .ThenByDescending(t => t.LastSeenUnix)
            .Take(topicLimit)
            .ToList();

        Topics = manual.Concat(algorithm).ToList();

        if (RecentActivity.Count > activityLimit)
        {
            RecentActivity = RecentActivity
                .OrderByDescending(a => a.TimestampUnix)
                .Take(activityLimit)
                .ToList();
        }
    }

    public ForYouCloudProfile ToCloudProfile() => new()
    {
        Enabled = Enabled,
        CloudSyncEnabled = CloudSyncEnabled,
        LearningRate = LearningRate,
        ScoreSchemaVersion = ScoreSchemaVersion,
        UpdatedAtUnix = UpdatedAtUnix > 0
            ? UpdatedAtUnix
            : DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        Topics = Topics
            .Select(t => new ForYouTopicProfile
            {
                Topic = t.Topic,
                Weight = t.Weight,
                IsPinned = t.IsPinned,
                IsBlocked = t.IsBlocked,
                LastSeenUnix = t.LastSeenUnix,
                SearchHits = t.SearchHits,
                OpenHits = t.OpenHits,
                FavoriteHits = t.FavoriteHits,
                WatchLaterHits = t.WatchLaterHits,
                DownloadHits = t.DownloadHits,
                ManualAdjustments = t.ManualAdjustments,
                Reason = t.Reason,
            })
            .ToList(),
        Activities = RecentActivity
            .Select(ToCloudActivity)
            .ToList(),
        Summary = Summary,
        UsedOpenAi = UsedOpenAi,
        RemovedTopics = RemovedTopics
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList(),
        SearchLines = SearchLines
            .Select(line => new ForYouCloudSearchLine
            {
                Query = line.Query,
                Label = line.Label,
                Reason = line.Reason,
                Score = line.Score,
                IsPinned = line.IsPinned,
                IsHidden = line.IsHidden,
                Source = line.Source,
                Tags = line.ApiTags.ToList(),
            })
            .ToList(),
    };

    public static ForYouProfile FromCloudProfile(ForYouCloudProfile cloud) => new()
    {
        Enabled = cloud.Enabled,
        CloudSyncEnabled = cloud.CloudSyncEnabled,
        LearningRate = cloud.LearningRate,
        ScoreSchemaVersion = cloud.ScoreSchemaVersion,
        UpdatedAtUnix = cloud.UpdatedAtUnix,
        Topics = cloud.Topics
            .Select(t => new ForYouTopicProfile
            {
                Topic = t.Topic,
                Weight = t.Weight,
                IsPinned = t.IsPinned,
                IsBlocked = t.IsBlocked,
                LastSeenUnix = t.LastSeenUnix,
                SearchHits = t.SearchHits,
                OpenHits = t.OpenHits,
                FavoriteHits = t.FavoriteHits,
                WatchLaterHits = t.WatchLaterHits,
                DownloadHits = t.DownloadHits,
                ManualAdjustments = t.ManualAdjustments,
                Reason = t.Reason,
            })
            .ToList(),
        RecentActivity = cloud.Activities
            .Select(FromCloudActivity)
            .OrderByDescending(a => a.TimestampUnix)
            .ToList(),
        Summary = cloud.Summary ?? string.Empty,
        UsedOpenAi = cloud.UsedOpenAi,
        SearchLines = cloud.SearchLines
            .Select(line => new ForYouSearchLine
            {
                Query = line.Query,
                Label = line.Label,
                Reason = line.Reason,
                Score = line.Score,
                IsPinned = line.IsPinned,
                IsHidden = line.IsHidden,
                Source = line.Source,
                ApiTags = line.Tags.ToList(),
            })
            .ToList(),
        RemovedTopics = cloud.RemovedTopics
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList(),
    };

    private static ForYouCloudActivity ToCloudActivity(ForYouActivityEntry entry)
    {
        var timestampUtc = entry.TimestampUnix > 0
            ? entry.TimestampUnix * 1000L
            : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        return new ForYouCloudActivity
        {
            Kind = entry.SignalType.ToString(),
            TimestampUtc = timestampUtc,
            Topic = entry.Topic,
            SearchText = entry.SignalType == ForYouSignalType.Search ? entry.Topic : string.Empty,
            Context = entry.Detail,
            SourcePageId = entry.Detail,
        };
    }

    private static ForYouActivityEntry FromCloudActivity(ForYouCloudActivity activity)
    {
        var signalType = Enum.TryParse<ForYouSignalType>(activity.Kind, ignoreCase: true, out var parsed)
            ? parsed
            : ForYouSignalType.Search;

        var timestampUnix = activity.TimestampUtc > 0
            ? activity.TimestampUtc / 1000L
            : DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return new ForYouActivityEntry
        {
            SignalType = signalType,
            Topic = string.IsNullOrWhiteSpace(activity.Topic) ? activity.SearchText : activity.Topic,
            Detail = activity.Context,
            TimestampUnix = timestampUnix,
        };
    }
}
