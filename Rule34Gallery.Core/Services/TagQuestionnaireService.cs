using System.Net.Http;
using System.Text;

namespace Rule34Gallery.Core.Services;

public static class TagQuestionnaireService
{
    public const int MaxQuestionRounds = 5;

    public const string WriteOwnOptionId = "write_own";

    public static async Task<TagQuestionnaireStepResult> StartAsync(
        HttpClient http,
        string theme,
        UserSettings settings,
        CancellationToken cancellationToken = default)
        => await GetStepAsync(
            http,
            theme,
            [],
            null,
            settings,
            forceFinishNow: false,
            cancellationToken).ConfigureAwait(false);

    public static async Task<TagQuestionnaireStepResult> ContinueAsync(
        HttpClient http,
        string theme,
        IReadOnlyList<TagQuestionnaireAnswer> answers,
        TagQuestionnaireQuestion? previousQuestion,
        UserSettings settings,
        CancellationToken cancellationToken = default)
        => await GetStepAsync(
            http,
            theme,
            answers,
            previousQuestion,
            settings,
            forceFinishNow: false,
            cancellationToken).ConfigureAwait(false);

    public static async Task<TagQuestionnaireStepResult> FinishAsync(
        HttpClient http,
        string theme,
        IReadOnlyList<TagQuestionnaireAnswer> answers,
        TagQuestionnaireQuestion? previousQuestion,
        UserSettings settings,
        CancellationToken cancellationToken = default)
        => await GetStepAsync(
            http,
            theme,
            answers,
            previousQuestion,
            settings,
            forceFinishNow: true,
            cancellationToken).ConfigureAwait(false);

    private static async Task<TagQuestionnaireStepResult> GetStepAsync(
        HttpClient http,
        string theme,
        IReadOnlyList<TagQuestionnaireAnswer> answers,
        TagQuestionnaireQuestion? previousQuestion,
        UserSettings settings,
        bool forceFinishNow,
        CancellationToken cancellationToken)
    {
        if (!settings.HasOpenAiForDescribeSearch)
        {
            throw new OpenAiTagDiscoveryException(
                "Guided questionnaire needs an OpenAI API key on the Account tab.");
        }

        var trimmedTheme = theme.Trim();
        if (string.IsNullOrWhiteSpace(trimmedTheme))
        {
            throw new OpenAiTagDiscoveryException("Enter a theme or topic first.");
        }

        var forceFinish = forceFinishNow || answers.Count >= MaxQuestionRounds;
        var aiStep = await OpenAiTagDiscoveryService.GetQuestionnaireStepAsync(
            http,
            settings.OpenAiApiKey,
            settings.OpenAiModel,
            trimmedTheme,
            answers,
            previousQuestion,
            forceFinish,
            additionalUserInstruction: null,
            cancellationToken).ConfigureAwait(false);

        if (!aiStep.IsComplete &&
            aiStep.Question is not null &&
            TagQuestionnaireRepeatGuard.IsRepeatedQuestion(aiStep.Question, answers))
        {
            aiStep = await OpenAiTagDiscoveryService.GetQuestionnaireStepAsync(
                http,
                settings.OpenAiApiKey,
                settings.OpenAiModel,
                trimmedTheme,
                answers,
                previousQuestion,
                forceFinish: false,
                additionalUserInstruction:
                "CRITICAL: Your question repeats a topic the user already answered. " +
                "Do NOT ask about character type, subject, or series again. " +
                "Either ask about a different dimension (pose, body part, style, clothing, action) or set done=true.",
                cancellationToken).ConfigureAwait(false);
        }

        if (!aiStep.IsComplete &&
            aiStep.Question is not null &&
            TagQuestionnaireRepeatGuard.IsRepeatedQuestion(aiStep.Question, answers))
        {
            aiStep = await OpenAiTagDiscoveryService.GetQuestionnaireStepAsync(
                http,
                settings.OpenAiApiKey,
                settings.OpenAiModel,
                trimmedTheme,
                answers,
                previousQuestion,
                forceFinish: true,
                additionalUserInstruction:
                "Stop asking questions. The user already answered this topic. Return final tag combinations now.",
                cancellationToken).ConfigureAwait(false);
        }

        if (aiStep.IsComplete)
        {
            var resolved = await OpenAiTagDiscoveryService.ResolveWithAutocompleteAsync(
                http,
                aiStep.RawResult!,
                cancellationToken).ConfigureAwait(false);

            var presets = TagRecommendationService.FindMatchingPresetsPublic(trimmedTheme);
            var discovery = TagRecommendationService.BuildDiscoveryFromOpenAi(
                presets,
                resolved,
                "Guided questionnaire · tags verified on Rule34");

            return new TagQuestionnaireStepResult
            {
                IsComplete = true,
                FinalResult = discovery,
                Note = aiStep.Note,
            };
        }

        var question = aiStep.Question!;
        var resolvedQuestion = await ResolveQuestionOptionsAsync(http, question, cancellationToken)
            .ConfigureAwait(false);

        return new TagQuestionnaireStepResult
        {
            IsComplete = false,
            Question = resolvedQuestion,
            Note = aiStep.Note,
        };
    }

    private static async Task<TagQuestionnaireQuestion> ResolveQuestionOptionsAsync(
        HttpClient http,
        TagQuestionnaireQuestion question,
        CancellationToken cancellationToken)
    {
        var options = new List<TagQuestionnaireOption>();
        foreach (var option in question.Options)
        {
            if (option.Id.Equals(WriteOwnOptionId, StringComparison.OrdinalIgnoreCase))
            {
                options.Add(option);
                continue;
            }

            var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var hint in option.HintTerms)
            {
                var suggestions = await TagAutocompleteService.FetchSuggestionsForQueryAsync(
                    http,
                    hint,
                    maxResults: 4,
                    cancellationToken).ConfigureAwait(false);

                foreach (var suggestion in suggestions)
                {
                    resolved.Add(suggestion.Value);
                }
            }

            foreach (var hint in option.HintTerms)
            {
                var normalized = UserSettings.NormalizeTagToken(hint);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    resolved.Add(normalized);
                }
            }

            options.Add(new TagQuestionnaireOption
            {
                Id = option.Id,
                Label = option.Label,
                Description = option.Description,
                HintTerms = option.HintTerms,
                ResolvedTags = resolved.Take(8).ToList(),
            });
        }

        if (!options.Any(o => o.Id.Equals(WriteOwnOptionId, StringComparison.OrdinalIgnoreCase)))
        {
            options.Add(new TagQuestionnaireOption
            {
                Id = WriteOwnOptionId,
                Label = "Write my own",
                Description = "Describe what you mean in your own words",
            });
        }

        return new TagQuestionnaireQuestion
        {
            Id = question.Id,
            Prompt = question.Prompt,
            AllowMultiple = question.AllowMultiple,
            Options = options,
        };
    }

    internal static string FormatAnswersForPrompt(
        string theme,
        IReadOnlyList<TagQuestionnaireAnswer> answers,
        TagQuestionnaireQuestion? lastQuestion)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Base theme: {theme}");
        if (answers.Count == 0)
        {
            sb.AppendLine("No answers yet — ask the first clarifying question.");
            return sb.ToString();
        }

        sb.AppendLine();
        sb.AppendLine("=== Already answered (NEVER repeat these topics) ===");
        for (var i = 0; i < answers.Count; i++)
        {
            var answer = answers[i];
            var questionLine = string.IsNullOrWhiteSpace(answer.QuestionPrompt)
                ? answer.QuestionId
                : answer.QuestionPrompt.Trim();
            sb.AppendLine($"{i + 1}. Q: {questionLine}");

            var usedWriteOwn = answer.SelectedOptionIds.Any(id =>
                id.Equals(WriteOwnOptionId, StringComparison.OrdinalIgnoreCase));
            var presetLabels = answer.SelectedOptionLabels.Count > 0
                ? answer.SelectedOptionLabels
                : answer.SelectedOptionIds
                    .Where(id => !id.Equals(WriteOwnOptionId, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            if (usedWriteOwn && !string.IsNullOrWhiteSpace(answer.FreeText))
            {
                sb.AppendLine($"   A: {answer.FreeText.Trim()}");
            }
            else if (presetLabels.Count > 0)
            {
                sb.AppendLine($"   A: {string.Join(", ", presetLabels)}");
            }

            if (!usedWriteOwn && !string.IsNullOrWhiteSpace(answer.FreeText))
            {
                sb.AppendLine($"   Note: {answer.FreeText.Trim()}");
            }

            if (TagQuestionnaireRepeatGuard.IsBroadOrNoPreferenceAnswer(answer.FreeText, answer.SelectedOptionLabels))
            {
                sb.AppendLine("   → User has NO PREFERENCE on this topic. Do not ask about it again.");
            }
        }

        if (lastQuestion is not null)
        {
            sb.AppendLine();
            sb.AppendLine("Last question shown (just answered):");
            sb.AppendLine($"  id: {lastQuestion.Id}");
            sb.AppendLine($"  prompt: {lastQuestion.Prompt}");
        }

        return sb.ToString();
    }

    public static IReadOnlyList<string> ResolveSelectedOptionLabels(
        TagQuestionnaireQuestion question,
        IReadOnlyList<string> selectedOptionIds)
    {
        var labels = new List<string>();
        foreach (var id in selectedOptionIds)
        {
            if (id.Equals(WriteOwnOptionId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var option = question.Options.FirstOrDefault(o =>
                o.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            labels.Add(option?.Label ?? id);
        }

        return labels;
    }
}
