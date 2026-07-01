namespace Rule34Gallery.Core;

public sealed class BlacklistPreset
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string Description { get; init; } = string.Empty;

    public required IReadOnlyList<string> Tags { get; init; }
}
