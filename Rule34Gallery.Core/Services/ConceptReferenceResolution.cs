namespace Rule34Gallery.Core.Services;

public sealed class ConceptReferenceResolution
{
    public string Reference { get; init; } = string.Empty;

    /// <summary>character, copyright, general, artist, or meta</summary>
    public string Kind { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public IReadOnlyList<string> SearchNames { get; init; } = [];
}
