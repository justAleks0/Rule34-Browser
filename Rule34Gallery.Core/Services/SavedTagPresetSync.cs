using System.Text.Json;

namespace Rule34Gallery.Core.Services;

public static class SavedTagPresetSync
{
    public static List<SavedTagPreset> Merge(
        IReadOnlyList<SavedTagPreset> local,
        IReadOnlyList<SavedTagPreset> cloud)
    {
        var map = new Dictionary<string, SavedTagPreset>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in cloud)
        {
            if (!string.IsNullOrWhiteSpace(item.Id))
            {
                map[item.Id] = item;
            }
        }

        foreach (var item in local)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                continue;
            }

            if (!map.TryGetValue(item.Id, out var existing))
            {
                map[item.Id] = item;
                continue;
            }

            map[item.Id] = item.UpdatedAtUnix >= existing.UpdatedAtUnix ? item : existing;
        }

        return DeduplicateByTagSignature(map.Values)
            .OrderByDescending(p => p.UpdatedAtUnix)
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// When the same tag bundle exists on both devices under different ids, keep the newest copy.
    /// </summary>
    private static IEnumerable<SavedTagPreset> DeduplicateByTagSignature(IEnumerable<SavedTagPreset> presets)
    {
        var bySignature = new Dictionary<string, SavedTagPreset>(StringComparer.OrdinalIgnoreCase);
        foreach (var preset in presets.OrderByDescending(p => p.UpdatedAtUnix))
        {
            var signature = BuildTagSignature(preset.Tags);
            if (!bySignature.ContainsKey(signature))
            {
                bySignature[signature] = preset;
            }
        }

        return bySignature.Values;
    }

    private static string BuildTagSignature(IReadOnlyList<string> tags)
    {
        var normalized = tags
            .Select(UserSettings.NormalizeTagToken)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return string.Join("|", normalized);
    }

    public static string SerializeTags(IReadOnlyList<string> tags) =>
        JsonSerializer.Serialize(tags);

    public static List<string> DeserializeTags(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static long NowUnix() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
