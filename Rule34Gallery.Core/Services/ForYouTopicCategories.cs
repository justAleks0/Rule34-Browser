namespace Rule34Gallery.Core.Services;

public enum ForYouManageTopicGroup
{
    Characters,
    Series,
    PositionsAndActs,
    General,
    Artists,
    Other,
}

public static class ForYouTopicCategories
{
    private static readonly HashSet<string> PositionTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "anal", "vaginal", "oral", "fellatio", "cunnilingus", "missionary", "cowgirl_position",
        "doggystyle", "sex", "group_sex", "threesome", "bondage", "bdsm", "rape", "gangbang",
        "double_penetration", "masturbation", "fingering", "paizuri", "footjob", "handjob",
        "69", "spooning", "standing_sex", "from_behind", "reverse_cowgirl_position",
    };

    public static ForYouManageTopicGroup Group(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return ForYouManageTopicGroup.Other;
        }

        if (ForYouTopicFocus.IsCharacterTopic(topic))
        {
            return ForYouManageTopicGroup.Characters;
        }

        var inferred = TagCategoryColors.InferCategory(topic);
        if (inferred == TagCategory.Artist)
        {
            return ForYouManageTopicGroup.Artists;
        }

        if (inferred == TagCategory.Copyright)
        {
            return ForYouManageTopicGroup.Series;
        }

        if (IsPositionOrAct(topic))
        {
            return ForYouManageTopicGroup.PositionsAndActs;
        }

        if (ForYouTopicFocus.IsGenericTopic(topic))
        {
            return ForYouManageTopicGroup.General;
        }

        var normalized = UserSettings.NormalizeTagToken(topic).ToLowerInvariant();
        if (normalized.Contains('_') && !normalized.Contains('('))
        {
            return ForYouManageTopicGroup.Series;
        }

        return ForYouManageTopicGroup.Other;
    }

    public static string DisplayName(ForYouManageTopicGroup group) => group switch
    {
        ForYouManageTopicGroup.Characters => "Characters",
        ForYouManageTopicGroup.Series => "Series & franchises",
        ForYouManageTopicGroup.PositionsAndActs => "Positions & acts",
        ForYouManageTopicGroup.General => "General tags",
        ForYouManageTopicGroup.Artists => "Artists",
        _ => "Other",
    };

    public static int SortOrder(ForYouManageTopicGroup group) => group switch
    {
        ForYouManageTopicGroup.Characters => 0,
        ForYouManageTopicGroup.Series => 1,
        ForYouManageTopicGroup.PositionsAndActs => 2,
        ForYouManageTopicGroup.General => 3,
        ForYouManageTopicGroup.Artists => 4,
        _ => 5,
    };

    private static bool IsPositionOrAct(string topic)
    {
        var normalized = UserSettings.NormalizeTagToken(topic).ToLowerInvariant();
        return PositionTags.Contains(normalized);
    }
}
