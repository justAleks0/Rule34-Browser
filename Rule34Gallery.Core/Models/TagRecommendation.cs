namespace Rule34Gallery.Core;

public sealed class TagRecommendation
{
    public required string Tag { get; init; }

    public TagCategory Category { get; init; }

    public string Reason { get; init; } = string.Empty;

    public int Score { get; init; }
}

public sealed class TagCombinationRecommendation
{
    public required string Label { get; init; }

    public string Reason { get; init; } = string.Empty;

    public IReadOnlyList<string> Tags { get; init; } = [];

    public int Score { get; init; }

    public string LineDisplay => string.Join(' ', Tags);
}

public sealed class SearchPresetRecommendation
{
    public required string PresetId { get; init; }

    public required string Name { get; init; }

    public string Description { get; init; } = string.Empty;

    public IReadOnlyList<string> Tags { get; init; } = [];
}
