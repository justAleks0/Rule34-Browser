using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rule34Gallery.Core.Services;

/// <summary>
/// "Forgot the name" mode: web search + OpenAI synthesis with strict fact verification — no tags.
/// </summary>
public static class PlainSearchDiscoveryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private const string QueryPlanSystemPrompt =
        """
        You plan web searches to help someone remember a name they forgot.
        Given their vague description, output 2-3 additional Google-style search queries.

        Rules:
        - Do NOT guess or output the answer (no character names unless the user already wrote them).
        - Queries should target wikis, fandom pages, Reddit threads, official sites.
        - Include franchise/series keywords from the user when present.
        - Focus on distinctive clues (weapon, color, role, outfit, species).

        JSON only:
        {"queries":["query one","query two"]}
        """;

    private const string SynthesizeSystemPrompt =
        """
        You identify what a user forgot using ONLY the web search snippets provided.
        Do NOT use outside knowledge. Do NOT invent facts.

        Rules:
        - Every result must be supported by at least one snippet.
        - Each salient clue in the user query (franchise, weapon, appearance, role) must appear in snippets for that result — or exclude the result.
        - Snippet text must describe what the web sources say, not fanfiction or guesses.
        - Do NOT output booru/danbooru/rule34 tags or underscore syntax.
        - Prefer specific named characters/people over generic list pages.
        - Return 1-6 results, best match first. Fewer results is fine if evidence is thin.

        JSON only:
        {
          "interpretation": "one sentence summary grounded in the snippets",
          "results": [
            {
              "title": "Primary name",
              "subtitle": "Type · Franchise or context",
              "snippet": "2-3 sentences citing what the web snippets actually say",
              "confidence": 0.0-1.0,
              "evidence_indexes": [1, 3]
            }
          ]
        }
        evidence_indexes: 1-based indexes into the numbered web results you were given.
        """;

    private const string VerifySystemPrompt =
        """
        You are a strict fact-checker. Review candidate memory-search answers against web search snippets ONLY.

        REJECT a candidate if ANY of these apply:
        - A factual claim is not supported by the cited snippets
        - The entity is assigned to the wrong franchise/series/game
        - Weapons, outfits, affiliations, or traits were invented to force a match
        - evidence_indexes point to snippets that do not mention the entity or the matching clues
        - Confidence is too high for thin evidence

        Do NOT add new candidates from your own knowledge — only keep or drop what you receive.
        Lower confidence when evidence is indirect.

        If nothing survives verification, return {"interpretation":"...","results":[]} explaining why.

        JSON only — same schema as input candidates (including evidence_indexes).
        """;

    public static async Task<TagDiscoveryResult> DiscoverAsync(
        HttpClient http,
        string apiKey,
        string model,
        string description,
        CancellationToken cancellationToken = default)
    {
        var trimmed = description.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return new TagDiscoveryResult();
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new OpenAiTagDiscoveryException("OpenAI API key is not set. Add it on the Account tab.");
        }

        // Web search first (Reddit + Wikipedia); optional extra queries if thin.
        var webResults = await WebSearchClient.SearchComprehensiveAsync(trimmed, cancellationToken)
            .ConfigureAwait(false);

        if (webResults.Count < 6)
        {
            var extraQueries = BuildSearchQueries(trimmed, await BuildAiPlannedQueriesAsync(
                http,
                apiKey,
                model,
                trimmed,
                cancellationToken).ConfigureAwait(false));

            webResults = WebSearchClient.MergeDistinct(
                webResults.Concat(await RunWebSearchesAsync(extraQueries, cancellationToken).ConfigureAwait(false)),
                maxResults: 20);
        }
        if (webResults.Count == 0)
        {
            throw new OpenAiTagDiscoveryException(
                "Could not fetch web results (Reddit/Wikipedia). Check your connection or try rephrasing.");
        }

        var evidenceBlock = WebSearchClient.FormatEvidenceBlock(webResults);
        var userEvidencePrompt = $"""
            User query:
            {trimmed}

            WEB SEARCH RESULTS (only allowed source of facts):
            {evidenceBlock}
            """;

        var draftJson = await OpenAiTagDiscoveryService.SendChatJsonPublicAsync(
            http,
            apiKey,
            model,
            SynthesizeSystemPrompt,
            userEvidencePrompt,
            cancellationToken,
            temperature: 0.1).ConfigureAwait(false);

        var verifiedJson = await OpenAiTagDiscoveryService.SendChatJsonPublicAsync(
            http,
            apiKey,
            model,
            VerifySystemPrompt,
            $"""
             {userEvidencePrompt}

             CANDIDATE ANSWERS TO VERIFY:
             {draftJson}
             """,
            cancellationToken,
            temperature: 0.0).ConfigureAwait(false);

        var parsed = JsonSerializer.Deserialize<MemorySearchResponse>(verifiedJson, JsonOptions);
        var interpretation = parsed?.Interpretation?.Trim() ?? string.Empty;
        var hits = ParseHits(parsed, webResults);

        if (hits.Count == 0)
        {
            throw new OpenAiTagDiscoveryException(
                string.IsNullOrWhiteSpace(interpretation)
                    ? "Nothing verified — web results did not support a confident match. Add more specific clues."
                    : interpretation);
        }

        return new TagDiscoveryResult
        {
            InterpretationSummary = interpretation,
            SearchHits = hits,
            UsedOpenAi = true,
            SourceNote = "Fact-checked against web search",
        };
    }

    private static async Task<IReadOnlyList<string>> BuildAiPlannedQueriesAsync(
        HttpClient http,
        string apiKey,
        string model,
        string trimmed,
        CancellationToken cancellationToken)
    {
        try
        {
            var planJson = await OpenAiTagDiscoveryService.SendChatJsonPublicAsync(
                http,
                apiKey,
                model,
                QueryPlanSystemPrompt,
                $"""
                 User description:
                 {trimmed}

                 Suggest additional web search queries (do not guess the answer).
                 """,
                cancellationToken,
                temperature: 0.1).ConfigureAwait(false);

            var plan = JsonSerializer.Deserialize<QueryPlanResponse>(planJson, JsonOptions);
            if (plan?.Queries is { Count: > 0 })
            {
                return plan.Queries
                    .Select(q => q?.Trim() ?? string.Empty)
                    .Where(q => !string.IsNullOrWhiteSpace(q))
                    .ToList();
            }
        }
        catch
        {
            // Optional — heuristics still run.
        }

        return [];
    }

    private static IReadOnlyList<string> BuildSearchQueries(string trimmed, IReadOnlyList<string> aiQueries)
    {
        var queries = new List<string>();
        queries.Add(trimmed);
        queries.AddRange(BuildHeuristicQueries(trimmed));
        queries.AddRange(aiQueries);

        return queries
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();
    }

    private static IEnumerable<string> BuildHeuristicQueries(string trimmed)
    {
        yield return $"{trimmed} wiki";

        if (trimmed.Contains("bow", StringComparison.OrdinalIgnoreCase))
        {
            yield return trimmed.Replace(" that uses a bow", " bow character", StringComparison.OrdinalIgnoreCase);
            yield return trimmed.Replace(" uses a bow", " bow character", StringComparison.OrdinalIgnoreCase);
            yield return $"{trimmed} archer";
        }
    }

    private static async Task<IReadOnlyList<WebSearchResultItem>> RunWebSearchesAsync(
        IReadOnlyList<string> queries,
        CancellationToken cancellationToken)
    {
        var batches = new List<WebSearchResultItem>();
        var isFirst = true;
        foreach (var query in queries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!isFirst)
            {
                await Task.Delay(450, cancellationToken).ConfigureAwait(false);
            }

            isFirst = false;

            try
            {
                var batch = await WebSearchClient.SearchAsync(query, cancellationToken).ConfigureAwait(false);
                batches.AddRange(batch);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Try remaining queries.
            }

            if (WebSearchClient.MergeDistinct(batches).Count >= 12)
            {
                break;
            }
        }

        return WebSearchClient.MergeDistinct(batches, maxResults: 20);
    }

    private static List<SearchInterpretationHit> ParseHits(
        MemorySearchResponse? parsed,
        IReadOnlyList<WebSearchResultItem> webResults)
    {
        var hits = new List<SearchInterpretationHit>();
        if (parsed?.Results is not { Count: > 0 })
        {
            return hits;
        }

        foreach (var item in parsed.Results)
        {
            var title = item.Title?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            if (!HasEvidenceSupport(item, webResults))
            {
                continue;
            }

            var confidence = item.Confidence is >= 0 and <= 1
                ? (int)Math.Round(item.Confidence.Value * 100)
                : 0;

            hits.Add(new SearchInterpretationHit
            {
                Title = title,
                Subtitle = item.Subtitle?.Trim() ?? string.Empty,
                Snippet = item.Snippet?.Trim() ?? string.Empty,
                ConfidencePercent = confidence,
            });
        }

        return hits;
    }

    private static bool HasEvidenceSupport(MemorySearchResultItem item, IReadOnlyList<WebSearchResultItem> webResults)
    {
        if (item.EvidenceIndexes is not { Count: > 0 })
        {
            return true;
        }

        foreach (var index in item.EvidenceIndexes)
        {
            if (index < 1 || index > webResults.Count)
            {
                return false;
            }

            var evidence = webResults[index - 1];
            var haystack = $"{evidence.Title} {evidence.Snippet}".ToLowerInvariant();
            if (haystack.Length == 0)
            {
                return false;
            }

            var titleToken = item.Title?.Trim();
            if (!string.IsNullOrWhiteSpace(titleToken) &&
                haystack.Contains(titleToken.ToLowerInvariant(), StringComparison.Ordinal))
            {
                continue;
            }

            var nameParts = titleToken?
                .Split([' ', '(', '-', '’', '\''], StringSplitOptions.RemoveEmptyEntries)
                .Where(p => p.Length >= 3)
                .ToList();
            if (nameParts is { Count: > 0 } &&
                nameParts.Any(part => haystack.Contains(part.ToLowerInvariant(), StringComparison.Ordinal)))
            {
                continue;
            }

            // Snippet may use a short name ("March") while title is "March 7th".
            if (nameParts is { Count: > 0 } &&
                nameParts[0].Length >= 4 &&
                haystack.Contains(nameParts[0].ToLowerInvariant(), StringComparison.Ordinal))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private sealed class QueryPlanResponse
    {
        [JsonPropertyName("queries")]
        public List<string>? Queries { get; set; }
    }

    private sealed class MemorySearchResponse
    {
        [JsonPropertyName("interpretation")]
        public string? Interpretation { get; set; }

        [JsonPropertyName("results")]
        public List<MemorySearchResultItem>? Results { get; set; }
    }

    private sealed class MemorySearchResultItem
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("subtitle")]
        public string? Subtitle { get; set; }

        [JsonPropertyName("snippet")]
        public string? Snippet { get; set; }

        [JsonPropertyName("confidence")]
        public double? Confidence { get; set; }

        [JsonPropertyName("evidence_indexes")]
        public List<int>? EvidenceIndexes { get; set; }
    }
}
