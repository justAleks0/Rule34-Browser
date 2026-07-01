namespace Rule34Gallery.Core.Services;

public sealed class OpenAiDiscoveryResult
{
    public IReadOnlyList<TagCombinationRecommendation> Combinations { get; init; } = [];

    public IReadOnlyList<TagRecommendation> Tags { get; init; } = [];

    /// <summary>Search fragments from OpenAI used to verify concept lookups on Rule34.</summary>
    public IReadOnlyList<string> HintTerms { get; init; } = [];

    public string ResolvedReference { get; init; } = string.Empty;

    public string ResolvedReferenceSummary { get; init; } = string.Empty;
}
