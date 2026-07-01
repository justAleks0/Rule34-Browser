using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rule34Gallery.Core.Services;

public sealed class OpenAiQuestionnaireStep
{
    public bool IsComplete { get; init; }

    public OpenAiDiscoveryResult? RawResult { get; init; }

    public TagQuestionnaireQuestion? Question { get; init; }

    public string Note { get; init; } = string.Empty;
}

public sealed class OpenAiTagDiscoveryException : Exception
{
    public OpenAiTagDiscoveryException(string message)
        : base(message)
    {
    }
}

public static class OpenAiTagDiscoveryService
{
    private const string ApiUrl = "https://api.openai.com/v1/chat/completions";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly Dictionary<string, string> CanonicalAliasMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["female"] = "1girl",
        ["woman"] = "1girl",
        ["girl"] = "1girl",
        ["male"] = "1boy",
        ["man"] = "1boy",
        ["boy"] = "1boy",
    };

    private const string SystemPrompt =
        """
        You recommend search tags for booru-style image boards (Danbooru / Rule34 tag format).
        The user describes what they want in plain language.

        IMPORTANT: Search uses AND — every tag in a line must match. Suggest complete search LINES (combinations of tags that work together), not only isolated tags.

        Rules:
        - Tags are lowercase with underscores instead of spaces (e.g. blue_hair, 1girl, futanari)
        - Use real, commonly used tag names from danbooru/rule34 ecosystems
        - Do NOT use minus prefixes or blacklist tags
        - Return 3 to 6 combinations (primary output), each with 2 to 8 tags that belong together
        - Give each combination a short label (e.g. "Core match", "Broader", "Character focus") and reason
        - Optionally add single tags only when useful alone (max 8)

        Respond with JSON only:
        {"combinations":[{"label":"short title","reason":"why this line fits","tags":["tag_a","tag_b"]}],"tags":[{"tag":"optional_single","reason":"why"}]}
        """;

    private const string ReferenceResolutionSystemPrompt =
        """
        You interpret vague user descriptions before booru/Rule34 tag lookup.
        Given informal text, answer: what does the user MOST LIKELY reference?

        Use real-world and fandom knowledge. Every salient clue in the input must fit your answer.
        Do NOT output Rule34/Danbooru tag strings (no underscores) — plain names only.

        Examples:
        - "those japanese gyaru girls" → reference "gyaru" (subculture), kind general
        - "hololive EN member, blue, time and clocks" → reference "Ouro Kronii", kind character — NOT Nanashi Mumei (civilization, not time)
        - "the blue shark vtuber from hololive" → reference "Gawr Gura", kind character

        JSON only:
        {"reference":"plain name","kind":"character|copyright|general|artist|meta","summary":"one sentence why this fits all clues","search_names":["most specific lookup name first","alternate name"]}
        search_names: 1–4 plain names or short phrases to search on Rule34 — prefer specific character/series/subculture names.
        """;

    private const string QuestionnaireSystemPrompt =
        """
        You help users find accurate Rule34/Danbooru tags through a short guided questionnaire.
        Colloquial words often do not match real tag names (e.g. "rippeling ass" might be "ass_ripple", "jiggle", etc.).

        You will receive a base theme and prior answers. Either ask ONE clarifying question or finish with tag suggestions.

        When asking a question:
        - Ask about ONE new topic only — never repeat or rephrase a topic the user already answered
        - Read "Already answered" carefully; if the user said any/all/no preference/whatever/doesn't matter, treat that topic as CLOSED
        - Move to a different dimension: pose, body focus, art style, rating, clothing, action, etc. — not character type again
        - If 2+ answers exist or the user gave broad answers, strongly prefer done=true unless one critical tag is still unknown
        - Focus on disambiguation: subject, body traits, motion/animation, character, series, clothing, POV, etc.
        - Offer 3 to 5 options with plain-language labels
        - Each option includes hint_terms: likely Rule34 tag fragments to look up (lowercase, underscores)
        - Use allow_multiple true when several options can apply together
        - Never ask the same question id twice

        When done (set done true):
        - Return 3 to 6 combinations and optional single tags
        - Prefer real booru tag names; refine based on all answers
        - Do NOT use minus prefixes

        JSON when asking:
        {"done":false,"note":"short progress note","question":{"id":"snake_case_id","prompt":"question text","allow_multiple":false,"options":[{"id":"opt_id","label":"plain label","description":"optional detail","hint_terms":["tag_hint","other_hint"]}]}}

        JSON when finished:
        {"done":true,"note":"short summary","combinations":[{"label":"title","reason":"why","tags":["tag_a","tag_b"]}],"tags":[{"tag":"single","reason":"why"}]}
        """;

    public static Task<string> SendChatJsonPublicAsync(
        HttpClient http,
        string apiKey,
        string model,
        string systemPrompt,
        string userContent,
        CancellationToken cancellationToken,
        double temperature = 0.25)
        => SendChatJsonAsync(http, apiKey, model, systemPrompt, userContent, cancellationToken, temperature);

    public static async Task<OpenAiDiscoveryResult> SuggestAsync(
        HttpClient http,
        string apiKey,
        string model,
        string description,
        CancellationToken cancellationToken = default)
        => await SuggestAsync(http, apiKey, model, description, DescribeDiscoveryMode.Theme, cancellationToken)
            .ConfigureAwait(false);

    public static async Task<OpenAiDiscoveryResult> SuggestAsync(
        HttpClient http,
        string apiKey,
        string model,
        string description,
        DescribeDiscoveryMode mode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new OpenAiTagDiscoveryException("OpenAI API key is not set. Add it in Settings.");
        }

        if (mode == DescribeDiscoveryMode.ConceptLookup)
        {
            return await ConceptReferenceTagLookup.DiscoverAsync(
                http,
                apiKey,
                model,
                description,
                cancellationToken).ConfigureAwait(false);
        }

        if (mode == DescribeDiscoveryMode.IntentSearch)
        {
            throw new InvalidOperationException(
                "IntentSearch uses PlainSearchDiscoveryService, not tag discovery.");
        }

        var (systemPrompt, userContent, temperature) = (SystemPrompt, description.Trim(), 0.35);

        var resolvedModel = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model.Trim();
        var requestBody = new ChatCompletionRequest
        {
            Model = resolvedModel,
            Temperature = temperature,
            ResponseFormat = new ResponseFormat { Type = "json_object" },
            Messages =
            [
                new ChatMessage { Role = "system", Content = systemPrompt },
                new ChatMessage { Role = "user", Content = userContent },
            ],
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new OpenAiTagDiscoveryException(ParseErrorMessage(responseJson, response.StatusCode));
        }

        var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson, JsonOptions);
        var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new OpenAiTagDiscoveryException("OpenAI returned an empty response.");
        }

        var parsed = JsonSerializer.Deserialize<AiTagResponse>(content, JsonOptions);
        var combinations = ParseCombinations(parsed);
        var tags = ParseSingleTags(parsed);

        if (combinations.Count == 0 && tags.Count == 0)
        {
            throw new OpenAiTagDiscoveryException("OpenAI did not return any suggestions. Try rephrasing your description.");
        }

        if (combinations.Count == 0 && tags.Count > 0)
        {
            combinations.Add(new TagCombinationRecommendation
            {
                Label = "Suggested line",
                Reason = "All recommended tags together (AND)",
                Tags = tags.Select(t => t.Tag).ToList(),
                Score = 88,
            });
        }

        return new OpenAiDiscoveryResult
        {
            Combinations = combinations,
            Tags = tags,
            HintTerms = NormalizeTagList(parsed?.HintTerms),
        };
    }

    public static async Task<ConceptReferenceResolution> ResolveUserReferenceAsync(
        HttpClient http,
        string apiKey,
        string model,
        string description,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new OpenAiTagDiscoveryException("OpenAI API key is not set. Add it in Settings.");
        }

        var content = await SendChatJsonAsync(
            http,
            apiKey,
            model,
            ReferenceResolutionSystemPrompt,
            $"""
             User input:
             {description.Trim()}

             What does the user most likely reference here?
             """,
            cancellationToken,
            temperature: 0.15).ConfigureAwait(false);

        var parsed = JsonSerializer.Deserialize<AiReferenceResponse>(content, JsonOptions);
        var reference = parsed?.Reference?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new OpenAiTagDiscoveryException(
                "OpenAI could not identify what you meant. Try adding more detail.");
        }

        var searchNames = new List<string>();
        if (parsed?.SearchNames is { Count: > 0 })
        {
            foreach (var name in parsed.SearchNames)
            {
                var trimmed = name?.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    searchNames.Add(trimmed);
                }
            }
        }

        if (searchNames.Count == 0)
        {
            searchNames.Add(reference);
        }

        return new ConceptReferenceResolution
        {
            Reference = reference,
            Kind = parsed?.Kind?.Trim() ?? string.Empty,
            Summary = parsed?.Summary?.Trim() ?? string.Empty,
            SearchNames = searchNames,
        };
    }

    public static async Task<OpenAiQuestionnaireStep> GetQuestionnaireStepAsync(
        HttpClient http,
        string apiKey,
        string model,
        string theme,
        IReadOnlyList<TagQuestionnaireAnswer> answers,
        TagQuestionnaireQuestion? previousQuestion,
        bool forceFinish,
        string? additionalUserInstruction = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new OpenAiTagDiscoveryException("OpenAI API key is not set. Add it in Settings.");
        }

        var userContent = TagQuestionnaireService.FormatAnswersForPrompt(theme, answers, previousQuestion);
        if (TagQuestionnaireRepeatGuard.ShouldBiasTowardFinish(answers))
        {
            userContent += Environment.NewLine +
                           "The user already gave broad or no-preference answers. Prefer done=true with tag suggestions unless one essential detail is still missing.";
        }

        if (forceFinish)
        {
            userContent += Environment.NewLine +
                           "You have asked enough questions. Set done=true and return final tag combinations now.";
        }

        if (!string.IsNullOrWhiteSpace(additionalUserInstruction))
        {
            userContent += Environment.NewLine + additionalUserInstruction.Trim();
        }

        var content = await SendChatJsonAsync(
            http,
            apiKey,
            model,
            QuestionnaireSystemPrompt,
            userContent,
            cancellationToken).ConfigureAwait(false);

        var parsed = JsonSerializer.Deserialize<AiQuestionnaireResponse>(content, JsonOptions);
        if (parsed is null)
        {
            throw new OpenAiTagDiscoveryException("OpenAI returned an unexpected questionnaire response.");
        }

        if (parsed.Done || forceFinish)
        {
            var tagResponse = new AiTagResponse
            {
                Combinations = parsed.Combinations,
                Tags = parsed.Tags,
            };
            var combinations = ParseCombinations(tagResponse);
            var tags = ParseSingleTags(tagResponse);
            if (combinations.Count == 0 && tags.Count == 0)
            {
                throw new OpenAiTagDiscoveryException(
                    "Questionnaire finished without tag suggestions. Try different answers.");
            }

            if (combinations.Count == 0 && tags.Count > 0)
            {
                combinations.Add(new TagCombinationRecommendation
                {
                    Label = "Suggested line",
                    Reason = "Combined from your answers",
                    Tags = tags.Select(t => t.Tag).ToList(),
                    Score = 88,
                });
            }

            return new OpenAiQuestionnaireStep
            {
                IsComplete = true,
                RawResult = new OpenAiDiscoveryResult
                {
                    Combinations = combinations,
                    Tags = tags,
                },
                Note = parsed.Note?.Trim() ?? string.Empty,
            };
        }

        if (parsed.Question is null ||
            string.IsNullOrWhiteSpace(parsed.Question.Prompt) ||
            parsed.Question.Options is not { Count: > 0 })
        {
            throw new OpenAiTagDiscoveryException("OpenAI did not return a valid question. Try again.");
        }

        var options = new List<TagQuestionnaireOption>();
        foreach (var option in parsed.Question.Options)
        {
            if (string.IsNullOrWhiteSpace(option.Label))
            {
                continue;
            }

            var id = string.IsNullOrWhiteSpace(option.Id)
                ? UserSettings.NormalizeTagToken(option.Label)
                : option.Id.Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            options.Add(new TagQuestionnaireOption
            {
                Id = id,
                Label = option.Label.Trim(),
                Description = option.Description?.Trim() ?? string.Empty,
                HintTerms = NormalizeTagList(option.HintTerms),
            });
        }

        if (options.Count == 0)
        {
            throw new OpenAiTagDiscoveryException("OpenAI returned a question without usable options.");
        }

        var questionId = string.IsNullOrWhiteSpace(parsed.Question.Id)
            ? $"q{answers.Count + 1}"
            : parsed.Question.Id.Trim();

        return new OpenAiQuestionnaireStep
        {
            IsComplete = false,
            Question = new TagQuestionnaireQuestion
            {
                Id = questionId,
                Prompt = parsed.Question.Prompt.Trim(),
                AllowMultiple = parsed.Question.AllowMultiple,
                Options = options,
            },
            Note = parsed.Note?.Trim() ?? string.Empty,
        };
    }

    private static async Task<string> SendChatJsonAsync(
        HttpClient http,
        string apiKey,
        string model,
        string systemPrompt,
        string userContent,
        CancellationToken cancellationToken,
        double temperature = 0.25)
    {
        var resolvedModel = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model.Trim();
        var requestBody = new ChatCompletionRequest
        {
            Model = resolvedModel,
            Temperature = temperature,
            ResponseFormat = new ResponseFormat { Type = "json_object" },
            Messages =
            [
                new ChatMessage { Role = "system", Content = systemPrompt },
                new ChatMessage { Role = "user", Content = userContent },
            ],
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new OpenAiTagDiscoveryException(ParseErrorMessage(responseJson, response.StatusCode));
        }

        var completion = JsonSerializer.Deserialize<ChatCompletionResponse>(responseJson, JsonOptions);
        var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new OpenAiTagDiscoveryException("OpenAI returned an empty response.");
        }

        return content;
    }

    private static List<TagCombinationRecommendation> ParseCombinations(AiTagResponse? parsed)
    {
        var results = new List<TagCombinationRecommendation>();
        if (parsed?.Combinations is not { Count: > 0 })
        {
            return results;
        }

        var rank = parsed.Combinations.Count;
        foreach (var item in parsed.Combinations)
        {
            var tags = NormalizeTagList(item.Tags);
            if (tags.Count == 0)
            {
                continue;
            }

            results.Add(new TagCombinationRecommendation
            {
                Label = string.IsNullOrWhiteSpace(item.Label) ? "Search line" : item.Label.Trim(),
                Reason = string.IsNullOrWhiteSpace(item.Reason) ? "Tags that match together" : item.Reason.Trim(),
                Tags = tags,
                Score = 92 + Math.Min(rank, 8),
            });
            rank--;
        }

        return results;
    }

    private static List<TagRecommendation> ParseSingleTags(AiTagResponse? parsed)
    {
        var results = new List<TagRecommendation>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (parsed?.Tags is not { Count: > 0 })
        {
            return results;
        }

        var rank = parsed.Tags.Count;
        foreach (var item in parsed.Tags)
        {
            var normalized = UserSettings.NormalizeTagToken(item.Tag ?? string.Empty);
            if (string.IsNullOrWhiteSpace(normalized) || !seen.Add(normalized))
            {
                continue;
            }

            results.Add(new TagRecommendation
            {
                Tag = normalized,
                Category = TagCategoryColors.InferCategory(normalized),
                Reason = string.IsNullOrWhiteSpace(item.Reason) ? "AI suggestion" : item.Reason.Trim(),
                Score = 70 + Math.Min(rank, 10),
            });
            rank--;
        }

        return results;
    }

    private static List<string> NormalizeTagList(IEnumerable<string>? tags)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<string>();
        if (tags is null)
        {
            return list;
        }

        foreach (var tag in tags)
        {
            var normalized = UserSettings.NormalizeTagToken(tag);
            if (!string.IsNullOrWhiteSpace(normalized) && seen.Add(normalized))
            {
                list.Add(normalized);
            }
        }

        return list;
    }

    public static async Task<OpenAiDiscoveryResult> ResolveWithAutocompleteAsync(
        HttpClient http,
        OpenAiDiscoveryResult raw,
        CancellationToken cancellationToken = default,
        bool strict = false)
    {
        var combinations = new List<TagCombinationRecommendation>();
        foreach (var combo in raw.Combinations)
        {
            var resolvedTags = new List<string>();
            foreach (var tag in combo.Tags)
            {
                var resolved = await ResolveCanonicalTagAsync(http, tag, cancellationToken, strict).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(resolved))
                {
                    resolvedTags.Add(resolved);
                }
            }

            if (resolvedTags.Count == 0)
            {
                continue;
            }

            combinations.Add(new TagCombinationRecommendation
            {
                Label = combo.Label,
                Reason = combo.Reason,
                Tags = resolvedTags,
                Score = combo.Score,
            });
        }

        var tags = await ResolveTagsWithAutocompleteAsync(http, raw.Tags, cancellationToken).ConfigureAwait(false);
        if (strict)
        {
            tags = tags.Where(t => !string.IsNullOrWhiteSpace(t.Tag)).ToList();
        }
        return new OpenAiDiscoveryResult
        {
            Combinations = combinations,
            Tags = tags,
        };
    }

    public static async Task<IReadOnlyList<TagRecommendation>> ResolveTagsWithAutocompleteAsync(
        HttpClient http,
        IEnumerable<TagRecommendation> aiTags,
        CancellationToken cancellationToken = default)
    {
        var resolved = new List<TagRecommendation>();
        foreach (var item in aiTags)
        {
            var canonical = await ResolveCanonicalTagAsync(http, item.Tag, cancellationToken).ConfigureAwait(false);
            resolved.Add(new TagRecommendation
            {
                Tag = canonical,
                Category = item.Category,
                Reason = item.Reason,
                Score = item.Score,
            });
        }

        return resolved;
    }

    private static async Task<string> ResolveCanonicalTagAsync(
        HttpClient http,
        string tag,
        CancellationToken cancellationToken,
        bool strict = false)
    {
        var normalizedInput = UserSettings.NormalizeTagToken(tag);
        if (CanonicalAliasMap.TryGetValue(normalizedInput, out var alias))
        {
            return alias;
        }

        var suggestions = await TagAutocompleteService.FetchSuggestionsForQueryAsync(
            http,
            normalizedInput,
            maxResults: 5,
            cancellationToken).ConfigureAwait(false);

        if (suggestions.Count == 0)
        {
            return strict ? string.Empty : normalizedInput;
        }

        var exact = suggestions.FirstOrDefault(s => s.Value.Equals(normalizedInput, StringComparison.OrdinalIgnoreCase));
        return exact?.Value ?? suggestions[0].Value;
    }

    private static string ParseErrorMessage(string json, System.Net.HttpStatusCode statusCode)
    {
        try
        {
            var error = JsonSerializer.Deserialize<OpenAiErrorResponse>(json, JsonOptions);
            if (!string.IsNullOrWhiteSpace(error?.Error?.Message))
            {
                return error.Error.Message;
            }
        }
        catch
        {
            // ignore parse failure
        }

        return statusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => "Invalid OpenAI API key.",
            System.Net.HttpStatusCode.TooManyRequests => "OpenAI rate limit reached. Try again in a moment.",
            _ => $"OpenAI request failed ({(int)statusCode}).",
        };
    }

    private sealed class ChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("response_format")]
        public ResponseFormat ResponseFormat { get; set; } = new();

        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; set; } = [];
    }

    private sealed class ResponseFormat
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "json_object";
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<ChatChoice>? Choices { get; set; }
    }

    private sealed class ChatChoice
    {
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; set; }
    }

    private sealed class AiTagResponse
    {
        [JsonPropertyName("combinations")]
        public List<AiCombinationItem>? Combinations { get; set; }

        [JsonPropertyName("tags")]
        public List<AiTagItem>? Tags { get; set; }

        [JsonPropertyName("hint_terms")]
        public List<string>? HintTerms { get; set; }
    }

    private sealed class AiReferenceResponse
    {
        [JsonPropertyName("reference")]
        public string? Reference { get; set; }

        [JsonPropertyName("kind")]
        public string? Kind { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }

        [JsonPropertyName("search_names")]
        public List<string>? SearchNames { get; set; }
    }

    private sealed class AiCombinationItem
    {
        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }
    }

    private sealed class AiTagItem
    {
        [JsonPropertyName("tag")]
        public string? Tag { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }
    }

    private sealed class AiQuestionnaireResponse
    {
        [JsonPropertyName("done")]
        public bool Done { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("question")]
        public AiQuestionnaireQuestionItem? Question { get; set; }

        [JsonPropertyName("combinations")]
        public List<AiCombinationItem>? Combinations { get; set; }

        [JsonPropertyName("tags")]
        public List<AiTagItem>? Tags { get; set; }
    }

    private sealed class AiQuestionnaireQuestionItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("prompt")]
        public string? Prompt { get; set; }

        [JsonPropertyName("allow_multiple")]
        public bool AllowMultiple { get; set; }

        [JsonPropertyName("options")]
        public List<AiQuestionnaireOptionItem>? Options { get; set; }
    }

    private sealed class AiQuestionnaireOptionItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("hint_terms")]
        public List<string>? HintTerms { get; set; }
    }

    private sealed class OpenAiErrorResponse
    {
        [JsonPropertyName("error")]
        public OpenAiError? Error { get; set; }
    }

    private sealed class OpenAiError
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }
}
