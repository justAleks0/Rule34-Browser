using System.Collections.Generic;

namespace Rule34Gallery.Core.Services;

/// <summary>Canonical For You learning signal strengths (−1.0 to 1.0 per event).</summary>
public static class ForYouSignalStrengths
{
    public const double ManualSearch = 1.0;
    public const double SavedTag = 1.0;
    public const double SavedPost = 0.9;
    public const double RepeatedTagView = 0.7;
    public const double SimilarTagSearchRepeated = 0.7;
    public const double PostReopened = 0.6;
    public const double FullyWatched = 0.6;
    public const double FinishesPostsWithTag = 0.6;
    public const double SimilarTag = 0.5;
    public const double TagClicked = 0.5;
    public const double PostOpened = 0.4;
    public const double LongView = 0.4;
    public const double QuickSkip = -0.4;
    public const double NotInterested = -1.0;
    public const double BlockedTag = -1.0;
    public const double ReportedPost = -1.0;

    /// <summary>Small nudge when OpenAI names a topic; never replaces signal-based scores.</summary>
    public const double AiTopicHint = 0.12;

    public const double MinTopicScore = 0.0;
    public const double MaxTopicScore = 100.0;
    public const double MinBlockedTopicScore = -100.0;

    public const int CurrentScoreSchemaVersion = 3;

    /// <summary>Rescale inflated pre-1:1 topic scores into the direct signal scale.</summary>
    public const double LegacyScoreRescaleDivisor = 6.0;

    /// <summary>Add one event signal directly to the stored 0–100 score (signal 0.12 → +0.12).</summary>
    public static double MergeTopicScore(double currentScore, double signal, double learningRate = 1.0)
        => Math.Clamp(NormalizeTopicScore(currentScore) + signal * learningRate, MinBlockedTopicScore, MaxTopicScore);

    /// <summary>Clamp stored topic score to the supported range.</summary>
    public static double NormalizeTopicScore(double weight)
    {
        if (double.IsNaN(weight) || double.IsInfinity(weight))
        {
            return 0;
        }

        return Math.Clamp(weight, MinBlockedTopicScore, MaxTopicScore);
    }

    public static void RescaleLegacyTopicScores(IList<ForYouTopicProfile> topics)
    {
        foreach (var topic in topics)
        {
            if (topic.IsBlocked || topic.Weight <= MinBlockedTopicScore + 0.01)
            {
                topic.Weight = MinBlockedTopicScore;
                continue;
            }

            if (Math.Abs(topic.Weight) < 0.0001)
            {
                topic.Weight = 0;
                continue;
            }

            topic.Weight = NormalizeTopicScore(topic.Weight / LegacyScoreRescaleDivisor);
        }
    }

    public static void MigrateProfileTopicScores(IList<ForYouTopicProfile> topics)
    {
        foreach (var topic in topics)
        {
            topic.Weight = NormalizeTopicScore(topic.Weight);
        }
    }

    [Obsolete("Signals map 1:1 to score points.")]
    public static double ToNormalizedStrength(double topicScore)
        => NormalizeTopicScore(topicScore) / MaxTopicScore;
}
