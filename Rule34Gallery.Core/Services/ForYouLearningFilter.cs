namespace Rule34Gallery.Core.Services;

/// <summary>
/// Tags and tokens that must never be learned as For You interests
/// (search filters, global settings, wildcards, etc.).
/// </summary>
public static class ForYouLearningFilter
{
    private static readonly HashSet<string> ExcludedTopics = new(StringComparer.OrdinalIgnoreCase)
    {
        "ai_art",
        "ai_generated",
        "ai_assisted",
        "ai",
        "stable_diffusion",
        "novelai",
        "midjourney",
        "dall_e",
        "dalle",
        "safe",
        "questionable",
        "explicit",
        "animated",
        "video",
        "gif",
    };

    public static bool IsExcluded(string? topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return true;
        }

        var normalized = UserSettings.NormalizeTagToken(topic).Trim().ToLowerInvariant();
        if (normalized.Length == 0)
        {
            return true;
        }

        if (normalized.StartsWith('-'))
        {
            return true;
        }

        if (normalized.Contains('*', StringComparison.Ordinal))
        {
            return true;
        }

        if (normalized.StartsWith("rating:", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("score:", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("sort:", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("order:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (ExcludedTopics.Contains(normalized))
        {
            return true;
        }

        return normalized is "-ai*" or "ai*" or "-ai";
    }

    public static IEnumerable<string> Sanitize(IEnumerable<string> tags)
        => tags
            .Select(UserSettings.NormalizeTagToken)
            .Where(t => !IsExcluded(t))
            .Distinct(StringComparer.OrdinalIgnoreCase);
}
