namespace Rule34Gallery.Core.Updates;

public static class SemVer
{
    public static bool IsNewer(string latest, string current)
    {
        var left = Parse(latest);
        var right = Parse(current);
        var length = Math.Max(left.Length, right.Length);
        for (var i = 0; i < length; i++)
        {
            var a = i < left.Length ? left[i] : 0;
            var b = i < right.Length ? right[i] : 0;
            if (a > b)
            {
                return true;
            }

            if (a < b)
            {
                return false;
            }
        }

        return false;
    }

    public static string NormalizeTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return "0.0.0";
        }

        var trimmed = tag.Trim();
        return trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase)
            ? trimmed[1..]
            : trimmed;
    }

    private static int[] Parse(string version)
    {
        var normalized = NormalizeTag(version);
        return normalized
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => int.TryParse(part, out var n) ? n : 0)
            .ToArray();
    }
}
