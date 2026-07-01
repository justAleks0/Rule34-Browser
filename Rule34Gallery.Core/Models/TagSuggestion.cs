namespace Rule34Gallery.Core;

public enum TagCategory
{
    General,
    Artist,
    Copyright,
    Character,
    Meta,
}

public sealed class TagSuggestion
{
    public TagSuggestion(string value, TagCategory category)
    {
        Value = value;
        Category = category;
    }

    public string Value { get; }

    public TagCategory Category { get; }

    public string ForegroundColor => TagCategoryColors.GetForeground(Category);

    public string BackgroundColor => TagCategoryColors.GetBackground(Category);

    public static TagSuggestion FromTag(string tag) => new(tag, TagCategoryColors.InferCategory(tag));
}

public static class TagCategoryColors
{
    public static TagCategory InferCategory(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return TagCategory.General;
        }

        var lower = tag.ToLowerInvariant();
        if (lower.StartsWith("artist:", StringComparison.Ordinal))
        {
            return TagCategory.Artist;
        }

        if (lower.StartsWith("copyright:", StringComparison.Ordinal))
        {
            return TagCategory.Copyright;
        }

        if (lower.StartsWith("character:", StringComparison.Ordinal))
        {
            return TagCategory.Character;
        }

        if (lower.StartsWith("meta:", StringComparison.Ordinal) || lower.StartsWith("metadata:", StringComparison.Ordinal))
        {
            return TagCategory.Meta;
        }

        if (lower.EndsWith("(artist)", StringComparison.Ordinal))
        {
            return TagCategory.Artist;
        }

        return TagCategory.General;
    }

    public static string GetForeground(TagCategory category) => category switch
    {
        TagCategory.Artist => "#F99FAC",
        TagCategory.Copyright => "#DDCC77",
        TagCategory.Character => "#A0E060",
        TagCategory.Meta => "#B4A7D6",
        _ => "#AAAAAA",
    };

    public static string GetBackground(TagCategory category) => category switch
    {
        TagCategory.Artist => "#3a2228",
        TagCategory.Copyright => "#3a3420",
        TagCategory.Character => "#243a1a",
        TagCategory.Meta => "#2a2438",
        _ => "#2a2a2a",
    };
}
