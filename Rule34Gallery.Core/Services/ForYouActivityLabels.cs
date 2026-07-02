namespace Rule34Gallery.Core.Services;

/// <summary>Human-readable labels for For You learning signals (sync tree, UI).</summary>
public static class ForYouActivityLabels
{
    public static string CategoryName(ForYouSignalType type) => type switch
    {
        ForYouSignalType.Search => "Search query",
        ForYouSignalType.ManualTopic => "Manual topic",
        ForYouSignalType.SavedTag => "Saved tag set",
        ForYouSignalType.Favorite => "Favorite",
        ForYouSignalType.WatchLater => "Watch later",
        ForYouSignalType.Download => "Download",
        ForYouSignalType.RepeatedTagView => "Repeat tag view",
        ForYouSignalType.SimilarTagSearch => "Similar tag search",
        ForYouSignalType.PostReopened => "Reopened post",
        ForYouSignalType.PostCompleted => "Finished post",
        ForYouSignalType.FinishesPostsWithTag => "Finished posts with tag",
        ForYouSignalType.SimilarTag => "Similar tag",
        ForYouSignalType.TagClicked => "Tag clicked",
        ForYouSignalType.PostOpened => "Opened post",
        ForYouSignalType.LongView => "Long view",
        ForYouSignalType.QuickSkip => "Quick skip",
        ForYouSignalType.NotInterested => "Not interested",
        ForYouSignalType.ReportedPost => "Reported post",
        ForYouSignalType.RecommendationLiked => "Liked recommendation",
        ForYouSignalType.RecommendationDismissed => "Dismissed recommendation",
        ForYouSignalType.TopicPinned => "Pinned topic",
        ForYouSignalType.TopicBlocked => "Blocked topic",
        ForYouSignalType.TopicWeightAdjusted => "Topic weight change",
        _ => "Training signal",
    };

    public static string CategoryName(string kind) =>
        Enum.TryParse<ForYouSignalType>(kind, ignoreCase: true, out var type)
            ? CategoryName(type)
            : string.IsNullOrWhiteSpace(kind) ? "Training signal" : kind.Replace('_', ' ');

    public static string FormatSyncTreeLabel(ForYouCloudActivity activity)
    {
        var category = CategoryName(activity.Kind);
        var subject = DescribeSubject(activity);
        if (string.IsNullOrWhiteSpace(subject))
        {
            return $"Learning data: {category}";
        }

        return $"Learning data: {category} — {subject}";
    }

    private static string DescribeSubject(ForYouCloudActivity activity)
    {
        var tag = activity.Topic?.Trim();
        if (string.IsNullOrWhiteSpace(tag) &&
            !string.IsNullOrWhiteSpace(activity.SearchText))
        {
            tag = activity.SearchText.Trim();
        }

        if (!string.IsNullOrWhiteSpace(tag) &&
            !string.IsNullOrWhiteSpace(activity.PostId) &&
            int.TryParse(activity.PostId, out var postId) &&
            postId > 0)
        {
            return $"{tag} (post #{postId})";
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            return tag;
        }

        if (!string.IsNullOrWhiteSpace(activity.PostId) &&
            int.TryParse(activity.PostId, out var id) &&
            id > 0)
        {
            return $"post #{id}";
        }

        return string.Empty;
    }
}
