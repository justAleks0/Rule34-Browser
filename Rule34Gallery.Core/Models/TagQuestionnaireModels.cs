namespace Rule34Gallery.Core;

using Rule34Gallery.Core.Services;

public sealed class TagQuestionnaireOption
{
    public required string Id { get; init; }

    public required string Label { get; init; }

    public string Description { get; init; } = string.Empty;

    public IReadOnlyList<string> HintTerms { get; init; } = [];

    /// <summary>Rule34 autocomplete matches for <see cref="HintTerms"/>.</summary>
    public IReadOnlyList<string> ResolvedTags { get; init; } = [];
}

public sealed class TagQuestionnaireQuestion
{
    public required string Id { get; init; }

    public required string Prompt { get; init; }

    public bool AllowMultiple { get; init; }

    public IReadOnlyList<TagQuestionnaireOption> Options { get; init; } = [];
}

public sealed class TagQuestionnaireAnswer
{
    public required string QuestionId { get; init; }

    /// <summary>Question text shown to the user (used to avoid repeated topics).</summary>
    public string QuestionPrompt { get; init; } = string.Empty;

    public IReadOnlyList<string> SelectedOptionIds { get; init; } = [];

    public IReadOnlyList<string> SelectedOptionLabels { get; init; } = [];

    public string? FreeText { get; init; }
}

public sealed class TagQuestionnaireStepResult
{
    public bool IsComplete { get; init; }

    public TagQuestionnaireQuestion? Question { get; init; }

    public TagDiscoveryResult? FinalResult { get; init; }

    public string Note { get; init; } = string.Empty;
}
