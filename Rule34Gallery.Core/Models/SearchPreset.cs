namespace Rule34Gallery.Core;

public sealed class SearchPreset
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string Description { get; init; } = string.Empty;

    /// <summary>Posts must have every tag in this list (AND).</summary>
    public required IReadOnlyList<string> Tags { get; init; }
}
