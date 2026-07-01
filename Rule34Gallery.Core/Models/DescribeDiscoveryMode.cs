namespace Rule34Gallery.Core;

/// <summary>How the Describe overlay interprets user input.</summary>
public enum DescribeDiscoveryMode
{
    /// <summary>Full search lines from a scene description.</summary>
    Theme,

    /// <summary>Guided Q&amp;A to narrow tags.</summary>
    Questionnaire,

    /// <summary>Map slang or vague wording to established tag names (not a literal translation).</summary>
    ConceptLookup,

    /// <summary>Forgot-the-name lookup: plain-language → what it probably is (no booru tags).</summary>
    IntentSearch,
}
