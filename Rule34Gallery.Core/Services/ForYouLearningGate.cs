namespace Rule34Gallery.Core.Services;

public enum ForYouLearningCategory
{
    Artist,
    Series,
    MinorTags,
    Other,
}

public static class ForYouLearningGate
{
    public static ForYouLearningCategory GetCategory(string topic)
    {
        var normalized = UserSettings.NormalizeTagToken(topic).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return ForYouLearningCategory.Other;
        }

        if (ForYouTopicFocus.IsGenericTopic(normalized))
        {
            return ForYouLearningCategory.MinorTags;
        }

        var manageGroup = ForYouTopicCategories.Group(normalized);
        if (manageGroup is ForYouManageTopicGroup.PositionsAndActs or ForYouManageTopicGroup.General)
        {
            return ForYouLearningCategory.MinorTags;
        }

        if (manageGroup == ForYouManageTopicGroup.Artists)
        {
            return ForYouLearningCategory.Artist;
        }

        if (ForYouTopicFocus.IsCharacterTopic(normalized) || manageGroup == ForYouManageTopicGroup.Characters)
        {
            return ForYouLearningCategory.Other;
        }

        if (TagCategoryColors.InferCategory(normalized) == TagCategory.Copyright ||
            normalized.Contains(':') ||
            manageGroup == ForYouManageTopicGroup.Series)
        {
            return ForYouLearningCategory.Series;
        }

        return ForYouLearningCategory.Other;
    }

    public static ForYouLearningCategory GetCategoryFromApiType(TagCategory apiCategory) => apiCategory switch
    {
        TagCategory.Artist => ForYouLearningCategory.Artist,
        TagCategory.Copyright => ForYouLearningCategory.Series,
        TagCategory.Character => ForYouLearningCategory.Other,
        TagCategory.General => ForYouLearningCategory.MinorTags,
        _ => ForYouLearningCategory.Other,
    };

    public static bool CanLearn(string? topic, UserSettings settings)
        => CanLearnWithCategory(topic, settings, null);

    public static bool CanLearnWithCategory(string? topic, UserSettings settings, TagCategory? apiCategory)
    {
        if (!settings.ForYouEnabled)
        {
            return false;
        }

        if (ForYouLearningFilter.IsExcluded(topic))
        {
            return false;
        }

        var normalized = UserSettings.NormalizeTagToken(topic).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        var category = apiCategory is TagCategory cat
            ? GetCategoryFromApiType(cat)
            : GetCategory(normalized);

        return category switch
        {
            ForYouLearningCategory.Artist => settings.ForYouLearnArtists,
            ForYouLearningCategory.Series => settings.ForYouLearnSeries,
            ForYouLearningCategory.MinorTags => settings.ForYouLearnMinorTags,
            _ => false,
        };
    }

    public static bool IsActiveForFeed(ForYouTopicProfile topic, UserSettings settings)
        => topic.IsManuallyCurated ||
           topic.IsPinned ||
           CanLearn(topic.Topic, settings);

    public static IEnumerable<ForYouTopicProfile> FilterForFeed(
        IEnumerable<ForYouTopicProfile> topics,
        UserSettings settings)
        => topics.Where(t => IsActiveForFeed(t, settings));

    public static IEnumerable<string> FilterLearnable(IEnumerable<string> tags, UserSettings settings)
        => ForYouLearningFilter.Sanitize(tags)
            .Where(tag => CanLearn(tag, settings));

    public static IEnumerable<string> FilterLearnableFromPost(PostItem post, UserSettings settings)
    {
        var categoryMap = post.GetTagCategoryMap();
        foreach (var tag in ForYouLearningFilter.Sanitize(post.GetTagList()))
        {
            categoryMap.TryGetValue(tag, out var category);
            if (CanLearnWithCategory(tag, settings, category))
            {
                yield return tag;
            }
        }
    }

    public static string DescribeEnabledCategories(UserSettings settings)
    {
        var parts = new List<string>();
        if (settings.ForYouLearnSeries)
        {
            parts.Add("copyright/series tags from posts (franchises like big_hero_6)");
        }

        if (settings.ForYouLearnArtists)
        {
            parts.Add("artist names");
        }

        if (settings.ForYouLearnMinorTags)
        {
            parts.Add("general/count/body tags (1girl, solo, etc.)");
        }

        return parts.Count > 0
            ? string.Join("; ", parts)
            : "none (do not add new topics)";
    }
}
