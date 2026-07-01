namespace Rule34Gallery.Core.Services;

internal static class TagQuestionnaireRepeatGuard
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "what", "which", "type", "kind", "being", "the", "are", "you", "your", "for", "with", "from",
        "that", "this", "about", "does", "have", "would", "like", "want", "should", "there", "their",
        "when", "where", "how", "who", "and", "or", "of", "in", "on", "at", "to", "a", "an", "is",
        "it", "do", "did", "can", "could", "will", "be", "been", "was", "were", "into", "any",
    };

    private static readonly string[] BroadAnswerMarkers =
    [
        "any character",
        "any ",
        "all character",
        "either",
        "whatever",
        "doesn't matter",
        "does not matter",
        "don't care",
        "dont care",
        "no preference",
        "no specific",
        "not specific",
        "will do",
        "anything",
        "everything",
        "no particular",
        "doesn't matter",
        "general",
        "anyone",
        "anybody",
    ];

    public static bool IsBroadOrNoPreferenceAnswer(string? freeText, IReadOnlyList<string> optionLabels)
    {
        var combined = string.Join(" ", new[] { freeText ?? string.Empty }.Concat(optionLabels)).Trim();
        if (combined.Length == 0)
        {
            return false;
        }

        var lower = combined.ToLowerInvariant();
        return BroadAnswerMarkers.Any(marker => lower.Contains(marker, StringComparison.Ordinal));
    }

    public static bool IsRepeatedQuestion(
        TagQuestionnaireQuestion question,
        IReadOnlyList<TagQuestionnaireAnswer> answers)
    {
        if (answers.Count == 0)
        {
            return false;
        }

        foreach (var answer in answers)
        {
            if (answer.QuestionId.Equals(question.Id, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(answer.QuestionPrompt) &&
                PromptsOverlap(answer.QuestionPrompt, question.Prompt))
            {
                return true;
            }
        }

        return false;
    }

    public static bool ShouldBiasTowardFinish(IReadOnlyList<TagQuestionnaireAnswer> answers)
    {
        if (answers.Count >= 2)
        {
            return true;
        }

        return answers.Any(a =>
            IsBroadOrNoPreferenceAnswer(a.FreeText, a.SelectedOptionLabels) ||
            a.SelectedOptionIds.Contains(TagQuestionnaireService.WriteOwnOptionId, StringComparer.OrdinalIgnoreCase) &&
            IsBroadOrNoPreferenceAnswer(a.FreeText, []));
    }

    private static bool PromptsOverlap(string previousPrompt, string newPrompt)
    {
        var prevWords = ExtractSignificantWords(previousPrompt);
        var newWords = ExtractSignificantWords(newPrompt);
        if (prevWords.Count == 0 || newWords.Count == 0)
        {
            return false;
        }

        var intersection = prevWords.Intersect(newWords, StringComparer.OrdinalIgnoreCase).Count();
        var union = prevWords.Union(newWords, StringComparer.OrdinalIgnoreCase).Count();
        var jaccard = union == 0 ? 0 : (double)intersection / union;

        if (jaccard >= 0.45)
        {
            return true;
        }

        var topicWords = new[] { "character", "subject", "series", "gender", "anime", "furry", "game", "original", "fictional" };
        var prevTopics = topicWords.Count(w => previousPrompt.Contains(w, StringComparison.OrdinalIgnoreCase));
        var newTopics = topicWords.Count(w => newPrompt.Contains(w, StringComparison.OrdinalIgnoreCase));
        return prevTopics >= 2 && newTopics >= 2 && jaccard >= 0.3;
    }

    private static HashSet<string> ExtractSignificantWords(string text)
    {
        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in text.Split([' ', '\t', '\n', '?', '.', ',', '!', ';', ':', '-'], StringSplitOptions.RemoveEmptyEntries))
        {
            var w = part.Trim().ToLowerInvariant();
            if (w.Length < 3 || StopWords.Contains(w))
            {
                continue;
            }

            words.Add(w);
        }

        return words;
    }
}
