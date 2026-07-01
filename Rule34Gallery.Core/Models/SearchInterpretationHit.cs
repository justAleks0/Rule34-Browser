namespace Rule34Gallery.Core;

/// <summary>Search-engine style match when user forgot a name (no booru tags).</summary>
public sealed class SearchInterpretationHit
{
    public string Title { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;

    public string Snippet { get; set; } = string.Empty;

    public int ConfidencePercent { get; set; }
}
