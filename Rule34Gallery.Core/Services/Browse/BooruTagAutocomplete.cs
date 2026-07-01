using System.Text.Json;

namespace Rule34Gallery.Core.Services.Browse;

internal static class BooruTagAutocomplete
{
    public static IReadOnlyList<TagSuggestion> Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var results = new List<TagSuggestion>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var name = item.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var category = InferDanbooruCategory(item);
            results.Add(new TagSuggestion(name, category));
        }

        return results;
    }

    private static TagCategory InferDanbooruCategory(JsonElement item)
    {
        if (!item.TryGetProperty("category", out var cat))
        {
            return TagCategory.General;
        }

        return cat.GetInt32() switch
        {
            1 => TagCategory.Artist,
            3 => TagCategory.Copyright,
            4 => TagCategory.Character,
            5 => TagCategory.Meta,
            _ => TagCategory.General,
        };
    }
}
