namespace Rule34Gallery.Core;

/// <summary>User-defined search tag bundle (stored in settings, shown above built-in presets).</summary>
public sealed class SavedTagPreset
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = [];

    /// <summary>UTC unix seconds; used when merging presets across devices.</summary>
    public long UpdatedAtUnix { get; set; }
}
