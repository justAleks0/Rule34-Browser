namespace Rule34Gallery.Core.Services;

public static class ForYouSearchQuery
{
    public static string NormalizeToken(string token)
    {
        var t = token.Trim().Replace(' ', '_');
        if (t.Equals("honkai:_star_rail", StringComparison.OrdinalIgnoreCase) ||
            t.Equals("honkai:star_rail", StringComparison.OrdinalIgnoreCase))
        {
            t = "honkai_star_rail";
        }

        return UserSettings.NormalizeTagToken(t);
    }

    public static string Resolve(ForYouSearchLine line)
    {
        if (line.ApiTags.Count > 0)
        {
            var normalized = line.ApiTags
                .Select(NormalizeToken)
                .Where(t => !string.IsNullOrWhiteSpace(t) && !ForYouLearningFilter.IsExcluded(t))
                .ToList();
            if (normalized.Count == 1)
            {
                return normalized[0];
            }

            if (normalized.SequenceEqual(["honkai", "star", "rail"], StringComparer.OrdinalIgnoreCase))
            {
                return "honkai_star_rail";
            }

            if (normalized.Count > 0 && normalized.All(t => t.Contains('_') || t.Contains('(')))
            {
                return string.Join(' ', normalized);
            }
        }

        var raw = line.Query.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        if (raw.Contains(' ') && !raw.Contains('_') && !raw.Contains('('))
        {
            return NormalizeToken(raw.Replace(' ', '_'));
        }

        var resolved = raw
            .Split([' ', '\t', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeToken)
            .Where(t => !string.IsNullOrWhiteSpace(t) && !ForYouLearningFilter.IsExcluded(t))
            .ToList();
        return resolved.Count > 0
            ? string.Join(' ', resolved)
            : NormalizeToken(raw);
    }

    public static bool IsGenericQuery(string query)
    {
        var tokens = query
            .Split([' ', '\t', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeToken)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.ToLowerInvariant())
            .ToList();
        return tokens.Count > 0 && tokens.All(ForYouTopicFocus.IsGenericTopic);
    }
}
