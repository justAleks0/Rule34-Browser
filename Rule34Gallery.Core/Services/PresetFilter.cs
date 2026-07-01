namespace Rule34Gallery.Core.Services;

public static class PresetFilter
{
    public static bool Matches(
        string? filter,
        string name,
        string description,
        string id,
        IReadOnlyList<string> tags)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        var haystack = $"{name} {description} {id.Replace('_', ' ')} {string.Join(' ', tags)}"
            .ToLowerInvariant();

        foreach (var term in filter.Split(
                     [' ', '\t'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!haystack.Contains(term.ToLowerInvariant(), StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
