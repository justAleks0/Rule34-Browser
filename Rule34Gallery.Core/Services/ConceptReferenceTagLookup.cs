using System.Net.Http;

namespace Rule34Gallery.Core.Services;

/// <summary>
/// Two-step concept lookup: OpenAI resolves what the user means, then Rule34 autocomplete finds the tag.
/// </summary>
public static class ConceptReferenceTagLookup
{
    public static async Task<OpenAiDiscoveryResult> DiscoverAsync(
        HttpClient http,
        string apiKey,
        string model,
        string description,
        CancellationToken cancellationToken = default)
    {
        var trimmed = description.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new OpenAiTagDiscoveryException("Enter a description first.");
        }

        var resolution = await OpenAiTagDiscoveryService.ResolveUserReferenceAsync(
            http,
            apiKey,
            model,
            trimmed,
            cancellationToken).ConfigureAwait(false);

        var tag = await FindTagAsync(http, resolution, cancellationToken).ConfigureAwait(false);
        var reason = string.IsNullOrWhiteSpace(resolution.Summary)
            ? $"Likely means: {resolution.Reference}"
            : $"{resolution.Reference} — {resolution.Summary}";

        var recommendation = new TagRecommendation
        {
            Tag = tag,
            Category = TagCategoryColors.InferCategory(tag),
            Reason = reason,
            Score = 95,
        };

        return new OpenAiDiscoveryResult
        {
            Combinations =
            [
                new TagCombinationRecommendation
                {
                    Label = "Primary tag",
                    Reason = reason,
                    Tags = [tag],
                    Score = 95,
                },
            ],
            Tags = [recommendation],
            HintTerms = BuildHintTerms(resolution),
            ResolvedReference = resolution.Reference,
            ResolvedReferenceSummary = resolution.Summary,
        };
    }

    internal static async Task<string> FindTagAsync(
        HttpClient http,
        ConceptReferenceResolution resolution,
        CancellationToken cancellationToken)
    {
        var preferredCategory = MapKind(resolution.Kind);
        var candidates = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var queries = BuildSearchQueries(resolution);

        using var gate = new SemaphoreSlim(4);
        var searchTasks = queries.Select(async query =>
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return (query, await TagAutocompleteService.FetchSuggestionsForQueryAsync(
                    http,
                    query,
                    maxResults: 20,
                    cancellationToken).ConfigureAwait(false));
            }
            finally
            {
                gate.Release();
            }
        });

        var searchResults = await Task.WhenAll(searchTasks).ConfigureAwait(false);
        foreach (var (query, suggestions) in searchResults)
        {
            for (var i = 0; i < suggestions.Count; i++)
            {
                var suggestion = suggestions[i];
                if (!MatchesPreferredCategory(suggestion.Category, preferredCategory))
                {
                    continue;
                }

                var score = 48 - i * 2 + NameMatchBonus(suggestion.Value, query, resolution);
                if (suggestion.Category == preferredCategory)
                {
                    score += 18;
                }
                else if (preferredCategory == TagCategory.Character &&
                         suggestion.Category == TagCategory.Copyright)
                {
                    score += 6;
                }

                if (candidates.TryGetValue(suggestion.Value, out var existing) && existing >= score)
                {
                    continue;
                }

                candidates[suggestion.Value] = score;
            }
        }

        if (candidates.Count == 0)
        {
            // Fallback: allow converting the resolved name into a plausible tag token
            // even when Rule34 autocomplete doesn't return matches for our queries.
            var fallbackTags = BuildFallbackTagCandidates(resolution);
            if (fallbackTags.Count == 0)
            {
                throw new OpenAiTagDiscoveryException(
                    $"Identified \"{resolution.Reference}\" but could not find a matching Rule34 tag. Try rephrasing.");
            }

            foreach (var fallback in fallbackTags)
            {
                var canonical = await ResolveCanonicalTagAsync(http, fallback, cancellationToken)
                    .ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(canonical))
                {
                    return canonical;
                }
            }

            throw new OpenAiTagDiscoveryException(
                $"Identified \"{resolution.Reference}\" but could not build a valid Rule34 tag from it. Try rephrasing.");
        }

        var best = candidates
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .First()
            .Key;

        return await ResolveCanonicalTagAsync(http, best, cancellationToken).ConfigureAwait(false);
    }

    private static List<string> BuildSearchQueries(ConceptReferenceResolution resolution)
    {
        var queries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in resolution.SearchNames.Append(resolution.Reference))
        {
            var trimmed = name.Trim();
            if (trimmed.Length < 2)
            {
                continue;
            }

            queries.Add(UserSettings.NormalizeTagToken(trimmed));

            var parts = trimmed
                .ToLowerInvariant()
                .Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts.Where(p => p.Length >= 3))
            {
                queries.Add(part);
            }

            if (parts.Length >= 2)
            {
                queries.Add(string.Join('_', parts));
            }
        }

        return queries
            .Where(q => q.Length >= 2)
            .OrderByDescending(q => q.Length)
            .Take(12)
            .ToList();
    }

    private static IReadOnlyList<string> BuildHintTerms(ConceptReferenceResolution resolution)
    {
        return BuildSearchQueries(resolution).Take(8).ToList();
    }

    private static TagCategory MapKind(string kind) => kind.Trim().ToLowerInvariant() switch
    {
        "character" => TagCategory.Character,
        "copyright" or "series" or "franchise" => TagCategory.Copyright,
        "artist" => TagCategory.Artist,
        "meta" => TagCategory.Meta,
        _ => TagCategory.General,
    };

    private static bool MatchesPreferredCategory(TagCategory tagCategory, TagCategory preferred)
    {
        if (preferred == TagCategory.Character)
        {
            return tagCategory is TagCategory.Character or TagCategory.Copyright;
        }

        if (preferred == TagCategory.Copyright)
        {
            return tagCategory is TagCategory.Copyright or TagCategory.Character;
        }

        return true;
    }

    private static int NameMatchBonus(string tag, string query, ConceptReferenceResolution resolution)
    {
        var tagLower = tag.ToLowerInvariant();
        var queryLower = query.ToLowerInvariant();
        var bonus = 0;

        if (tagLower.Equals(queryLower, StringComparison.Ordinal))
        {
            bonus += 36;
        }
        else if (tagLower.StartsWith(queryLower, StringComparison.Ordinal))
        {
            bonus += 22;
        }

        foreach (var name in resolution.SearchNames.Append(resolution.Reference))
        {
            var normalized = UserSettings.NormalizeTagToken(name);
            if (normalized.Length >= 2 && tagLower.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            {
                bonus += 40;
            }
            else if (normalized.Length >= 3 && tagLower.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            {
                bonus += 16;
            }
        }

        return bonus;
    }

    private static async Task<string> ResolveCanonicalTagAsync(
        HttpClient http,
        string tag,
        CancellationToken cancellationToken)
    {
        var suggestions = await TagAutocompleteService.FetchSuggestionsForQueryAsync(
            http,
            tag,
            maxResults: 5,
            cancellationToken).ConfigureAwait(false);

        if (suggestions.Count == 0)
        {
            return tag;
        }

        var exact = suggestions.FirstOrDefault(s => s.Value.Equals(tag, StringComparison.OrdinalIgnoreCase));
        return exact?.Value ?? suggestions[0].Value;
    }

    private static IReadOnlyList<string> BuildFallbackTagCandidates(ConceptReferenceResolution resolution)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(resolution.Reference))
        {
            // Full name → likely tag token: "Ouro Kronii" -> "ouro_kronii"
            var full = UserSettings.NormalizeTagToken(resolution.Reference).ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(full))
            {
                set.Add(full);
            }

            // Last token → common alias: "Ouro Kronii" -> "kronii"
            var parts = resolution.Reference
                .Split([' ', '\t', '_', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length > 0)
            {
                var last = UserSettings.NormalizeTagToken(parts[^1]).ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(last))
                {
                    set.Add(last);
                }
            }

            // Individual normalized tokens ("Ouro" and "Kronii") can sometimes match.
            foreach (var part in resolution.Reference.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var normalized = UserSettings.NormalizeTagToken(part).ToLowerInvariant();
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    set.Add(normalized);
                }
            }

            // Some names are already underscore-separated.
            var alreadyUnderscored = resolution.Reference
                .Replace(' ', '_')
                .Replace('-', '_');
            var normalizedUnderscored = UserSettings.NormalizeTagToken(alreadyUnderscored).ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(normalizedUnderscored))
            {
                set.Add(normalizedUnderscored);
            }
        }

        // Keep stable order: longer tokens first.
        return set
            .Where(t => t.Length >= 2)
            .OrderByDescending(t => t.Length)
            .ThenBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
