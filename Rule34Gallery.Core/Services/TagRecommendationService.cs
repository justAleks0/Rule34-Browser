using System.Net.Http;

namespace Rule34Gallery.Core.Services;

public sealed class TagDiscoveryResult
{
    public IReadOnlyList<SearchPresetRecommendation> Presets { get; init; } = [];

    public IReadOnlyList<TagCombinationRecommendation> Combinations { get; init; } = [];

    public IReadOnlyList<TagRecommendation> Tags { get; init; } = [];

    /// <summary>One-line summary for memory / forgot-the-name search (no tags).</summary>
    public string InterpretationSummary { get; init; } = string.Empty;

    public IReadOnlyList<SearchInterpretationHit> SearchHits { get; init; } = [];

    public bool UsedOpenAi { get; init; }

    public string SourceNote { get; init; } = string.Empty;
}

public static class TagRecommendationService
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "with", "without", "in", "on", "at", "to", "of", "for",
        "is", "are", "was", "were", "be", "been", "being", "have", "has", "had", "do", "does", "did",
        "will", "would", "could", "should", "may", "might", "must", "shall",
        "i", "you", "he", "she", "it", "we", "they", "me", "him", "her", "us", "them",
        "my", "your", "his", "its", "our", "their",
        "this", "that", "these", "those",
        "what", "which", "who", "whom", "where", "when", "why", "how",
        "some", "any", "no", "not", "very", "just", "also", "too", "as", "by", "from", "into",
        "about", "like", "want", "see", "looking", "look", "something", "someone", "anything",
        "pic", "pics", "picture", "pictures", "art", "please", "maybe", "show", "find",
    };

    public static async Task<TagDiscoveryResult> DiscoverAsync(
        HttpClient http,
        string description,
        UserSettings settings,
        CancellationToken cancellationToken = default)
        => await DiscoverAsync(http, description, settings, DescribeDiscoveryMode.Theme, cancellationToken)
            .ConfigureAwait(false);

    public static async Task<TagDiscoveryResult> DiscoverAsync(
        HttpClient http,
        string description,
        UserSettings settings,
        DescribeDiscoveryMode mode,
        CancellationToken cancellationToken = default)
    {
        var trimmed = description.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return new TagDiscoveryResult();
        }

        if ((mode == DescribeDiscoveryMode.ConceptLookup || mode == DescribeDiscoveryMode.IntentSearch) &&
            !settings.HasOpenAiForDescribeSearch)
        {
            throw new OpenAiTagDiscoveryException(
                mode == DescribeDiscoveryMode.IntentSearch
                    ? "Forgot the name? needs an OpenAI API key on the Account tab."
                    : "Find tag name needs an OpenAI API key on the Account tab.");
        }

        if (mode == DescribeDiscoveryMode.IntentSearch)
        {
            try
            {
                return await PlainSearchDiscoveryService.DiscoverAsync(
                    http,
                    settings.OpenAiApiKey,
                    settings.OpenAiModel,
                    trimmed,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OpenAiTagDiscoveryException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                throw new OpenAiTagDiscoveryException(
                    "Could not figure out what you mean. Check your connection or try rephrasing.");
            }
        }

        var presets = FindMatchingPresets(trimmed);

        if (settings.HasOpenAiForDescribeSearch)
        {
            try
            {
                var aiRaw = await OpenAiTagDiscoveryService.SuggestAsync(
                    http,
                    settings.OpenAiApiKey,
                    settings.OpenAiModel,
                    trimmed,
                    mode,
                    cancellationToken).ConfigureAwait(false);

                var aiResolved = mode == DescribeDiscoveryMode.ConceptLookup
                    ? aiRaw
                    : await OpenAiTagDiscoveryService.ResolveWithAutocompleteAsync(
                        http,
                        aiRaw,
                        cancellationToken).ConfigureAwait(false);

                var sourceNote = mode switch
                {
                    DescribeDiscoveryMode.ConceptLookup when !string.IsNullOrWhiteSpace(aiResolved.ResolvedReference) =>
                        $"Likely means: {aiResolved.ResolvedReference} · tag found on Rule34",
                    DescribeDiscoveryMode.ConceptLookup =>
                        "Reference identified by OpenAI · tag found on Rule34",
                    _ => "Suggestions from OpenAI",
                };

                return BuildDiscoveryFromOpenAi(
                    presets,
                    aiResolved,
                    sourceNote);
            }
            catch (OpenAiTagDiscoveryException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                if (mode == DescribeDiscoveryMode.ConceptLookup)
                {
                    throw new OpenAiTagDiscoveryException(
                        "Could not find tag names. Check your connection or try rephrasing.");
                }

                var fallback = await DiscoverHeuristicAsync(http, trimmed, presets, cancellationToken)
                    .ConfigureAwait(false);
                return new TagDiscoveryResult
                {
                    Presets = fallback.Presets,
                    Combinations = fallback.Combinations,
                    Tags = fallback.Tags,
                    UsedOpenAi = false,
                    SourceNote = "OpenAI failed — showing local tag lookup instead.",
                };
            }
        }

        if (mode == DescribeDiscoveryMode.ConceptLookup)
        {
            throw new OpenAiTagDiscoveryException(
                "Find tag name needs an OpenAI API key on the Account tab.");
        }

        return await DiscoverHeuristicAsync(http, trimmed, presets, cancellationToken).ConfigureAwait(false);
    }

    internal static IReadOnlyList<SearchPresetRecommendation> FindMatchingPresetsPublic(string description)
        => FindMatchingPresets(description);

    internal static TagDiscoveryResult BuildDiscoveryFromOpenAi(
        IReadOnlyList<SearchPresetRecommendation> presets,
        OpenAiDiscoveryResult aiResolved,
        string sourceNote)
    {
        var tagMap = BuildTagMap(presets);
        foreach (var tag in aiResolved.Tags)
        {
            MergeTag(tagMap, tag.Tag, tag.Category, tag.Reason, tag.Score);
        }

        return BuildResult(
            presets,
            aiResolved.Combinations,
            tagMap,
            usedOpenAi: true,
            sourceNote: sourceNote);
    }

    private static async Task<TagDiscoveryResult> DiscoverHeuristicAsync(
        HttpClient http,
        string trimmed,
        IReadOnlyList<SearchPresetRecommendation> presets,
        CancellationToken cancellationToken)
    {
        var tagMap = BuildTagMap(presets);

        var queries = BuildAutocompleteQueries(trimmed);
        using var gate = new SemaphoreSlim(4);
        var autocompleteTasks = queries.Select(async query =>
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return (query, await TagAutocompleteService.FetchSuggestionsForQueryAsync(
                    http,
                    query,
                    maxResults: 12,
                    cancellationToken).ConfigureAwait(false));
            }
            finally
            {
                gate.Release();
            }
        });

        var autocompleteResults = await Task.WhenAll(autocompleteTasks).ConfigureAwait(false);
        foreach (var (query, suggestions) in autocompleteResults)
        {
            foreach (var suggestion in suggestions)
            {
                var score = ScoreSuggestion(trimmed, query, suggestion);
                MergeTag(tagMap, suggestion.Value, suggestion.Category, $"Matches \"{query}\"", score);
            }
        }

        var tags = tagMap.Values
            .Select(v => v.Item)
            .OrderByDescending(t => t.Score)
            .ThenBy(t => t.Tag, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var combinations = BuildHeuristicCombinations(tags);

        return new TagDiscoveryResult
        {
            Presets = presets,
            Combinations = combinations,
            Tags = tags.Take(48).ToList(),
            UsedOpenAi = false,
            SourceNote = "Local lookup — add an OpenAI key in Settings for smarter suggestions.",
        };
    }

    private static List<TagCombinationRecommendation> BuildHeuristicCombinations(IReadOnlyList<TagRecommendation> tags)
    {
        if (tags.Count < 2)
        {
            return [];
        }

        var ordered = tags.OrderByDescending(t => t.Score).ToList();
        var combinations = new List<TagCombinationRecommendation>();

        var fullLine = ordered.Take(Math.Min(8, ordered.Count)).Select(t => t.Tag).ToList();
        if (fullLine.Count >= 2)
        {
            combinations.Add(new TagCombinationRecommendation
            {
                Label = "Full search line",
                Reason = "Top matches together — all tags must match (AND)",
                Tags = fullLine,
                Score = 85,
            });
        }

        var focused = ordered.Take(Math.Min(4, ordered.Count)).Select(t => t.Tag).ToList();
        if (focused.Count >= 2 && !TagListsEqual(focused, fullLine))
        {
            combinations.Add(new TagCombinationRecommendation
            {
                Label = "Focused line",
                Reason = "Strongest tags only",
                Tags = focused,
                Score = 80,
            });
        }

        var characters = ordered
            .Where(t => t.Category == TagCategory.Character)
            .Select(t => t.Tag)
            .Take(3)
            .ToList();
        if (characters.Count >= 1)
        {
            var withGeneral = characters
                .Concat(ordered.Where(t => t.Category == TagCategory.General).Select(t => t.Tag).Take(3))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(6)
                .ToList();
            if (withGeneral.Count >= 2 && combinations.All(c => !TagListsEqual(c.Tags, withGeneral)))
            {
                combinations.Add(new TagCombinationRecommendation
                {
                    Label = "Character + traits",
                    Reason = "Character tags with related general tags",
                    Tags = withGeneral,
                    Score = 78,
                });
            }
        }

        return combinations.Take(4).ToList();
    }

    private static bool TagListsEqual(IReadOnlyList<string> a, IReadOnlyList<string> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        for (var i = 0; i < a.Count; i++)
        {
            if (!a[i].Equals(b[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static Dictionary<string, (TagRecommendation Item, int Score)> BuildTagMap(
        IReadOnlyList<SearchPresetRecommendation> presets)
    {
        var tagMap = new Dictionary<string, (TagRecommendation Item, int Score)>(StringComparer.OrdinalIgnoreCase);
        foreach (var preset in presets)
        {
            foreach (var tag in preset.Tags)
            {
                MergeTag(
                    tagMap,
                    tag,
                    TagCategoryColors.InferCategory(tag),
                    $"From search preset: {preset.Name}",
                    75);
            }
        }

        return tagMap;
    }

    private static TagDiscoveryResult BuildResult(
        IReadOnlyList<SearchPresetRecommendation> presets,
        IReadOnlyList<TagCombinationRecommendation> combinations,
        Dictionary<string, (TagRecommendation Item, int Score)> tagMap,
        bool usedOpenAi,
        string sourceNote)
    {
        var tags = tagMap.Values
            .Select(v => v.Item)
            .OrderByDescending(t => t.Score)
            .ThenBy(t => t.Tag, StringComparer.OrdinalIgnoreCase)
            .Take(48)
            .ToList();

        var comboTags = new HashSet<string>(
            combinations.SelectMany(c => c.Tags),
            StringComparer.OrdinalIgnoreCase);
        var singles = tags
            .Where(t => !comboTags.Contains(t.Tag))
            .Take(24)
            .ToList();

        return new TagDiscoveryResult
        {
            Presets = presets,
            Combinations = combinations,
            Tags = singles,
            UsedOpenAi = usedOpenAi,
            SourceNote = sourceNote,
        };
    }

    private static List<SearchPresetRecommendation> FindMatchingPresets(string description)
    {
        var results = new List<SearchPresetRecommendation>();
        foreach (var preset in SearchPresetCatalog.All)
        {
            if (!PresetFilter.Matches(description, preset.Name, preset.Description, preset.Id, preset.Tags))
            {
                continue;
            }

            results.Add(new SearchPresetRecommendation
            {
                PresetId = preset.Id,
                Name = preset.Name,
                Description = preset.Description,
                Tags = preset.Tags,
            });
        }

        return results
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();
    }

    private static IReadOnlyList<string> BuildAutocompleteQueries(string description)
    {
        var words = description
            .ToLowerInvariant()
            .Split([' ', '\t', ',', ';', '.', '!', '?', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Length >= 2 && !StopWords.Contains(w))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var queries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (words.Count >= 2)
        {
            queries.Add(string.Join('_', words));
            for (var i = 0; i < words.Count - 1; i++)
            {
                queries.Add($"{words[i]}_{words[i + 1]}");
            }

            if (words.Count >= 3)
            {
                for (var i = 0; i < words.Count - 2; i++)
                {
                    queries.Add($"{words[i]}_{words[i + 1]}_{words[i + 2]}");
                }
            }
        }

        foreach (var word in words)
        {
            queries.Add(word);
        }

        return queries
            .Where(q => q.Length >= 2)
            .OrderByDescending(q => q.Length)
            .Take(14)
            .ToList();
    }

    private static int ScoreSuggestion(string description, string query, TagSuggestion suggestion)
    {
        var tag = suggestion.Value;
        var score = 40;
        var tagLower = tag.ToLowerInvariant();
        var queryLower = query.ToLowerInvariant();
        var descLower = description.ToLowerInvariant();

        if (tagLower.Equals(queryLower, StringComparison.Ordinal))
        {
            score += 35;
        }
        else if (tagLower.StartsWith(queryLower, StringComparison.Ordinal))
        {
            score += 25;
        }
        else if (tagLower.Contains(queryLower, StringComparison.Ordinal))
        {
            score += 12;
        }

        if (descLower.Contains(tagLower.Replace('_', ' '), StringComparison.Ordinal))
        {
            score += 10;
        }

        if (suggestion.Category == TagCategory.Character)
        {
            score += 4;
        }

        return score;
    }

    private static void MergeTag(
        Dictionary<string, (TagRecommendation Item, int Score)> map,
        string tag,
        TagCategory category,
        string reason,
        int score)
    {
        var normalized = UserSettings.NormalizeTagToken(tag);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        if (map.TryGetValue(normalized, out var existing) && existing.Score >= score)
        {
            return;
        }

        map[normalized] = (
            new TagRecommendation
            {
                Tag = normalized,
                Category = category,
                Reason = reason,
                Score = score,
            },
            score);
    }
}
