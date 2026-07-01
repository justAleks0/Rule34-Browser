using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rule34Gallery.Core.Services;

public sealed class OpenAiForYouProfileResult
{
    public string Summary { get; init; } = string.Empty;

    public bool UsedOpenAi { get; init; }

    public IReadOnlyList<ForYouTopicProfile> Topics { get; init; } = [];

    public IReadOnlyList<ForYouSearchLine> SearchLines { get; init; } = [];
}

public static class OpenAiForYouDiscoveryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private const string SystemPrompt =
        """
        You build a personalized "For You" profile for a booru-style media browser.

        You will receive a compact history of user searches, opened posts, favorites, watch later actions, downloads, and list activity.
        Infer interests, recurring themes, negative preferences, and useful search lines.

        Rules:
        - Use plain tags or short search phrases.
        - Prefer short, concrete phrases the app can turn into searches.
        - Include a mix of broad interests and a few narrower search lines.
        - If the history suggests a negative preference, include it as a hidden topic.
        - Do not invent details that are not supported by the history.
        - Never add AI-related tags (ai_art, ai_generated, stable_diffusion, etc.) unless the activity history clearly shows the user searched for or engaged with AI content.
        - The app's "Filter AI posts" setting is automatic search filtering, not a user interest. Ignore it completely.
        - Topics do NOT get numeric scores. The app computes strength from real actions (search, open, watch, save).
        - search_lines may include score 10-40 for ranking suggestions only (not topic strength).
        - Only add topics in these enabled categories: {{ENABLED_CATEGORIES}}.
        - If a category is disabled, do not add artist names, series, or generic tags from that category.
        - Use real booru tag tokens (underscores), not English phrases. Example: big_hero_6 not "big hero 6 interest".
        - Every topic must appear literally in the activity history tags or searches, or be a direct series/character name from a post the user opened.

        Return JSON only:
        {
          "summary": "one short paragraph",
          "topics": [
            {"topic":"blue_hair","reason":"why this matters"}
          ],
          "search_lines": [
            {"query":"blue hair solo","label":"Blue hair solo","reason":"why it fits","score":25}
          ],
          "hidden_topics": [
            {"topic":"gore","reason":"why it should be hidden"}
          ]
        }
        """;

    public static async Task<OpenAiForYouProfileResult> BuildAsync(
        HttpClient http,
        string apiKey,
        string model,
        IReadOnlyList<ForYouActivityEntry> activities,
        IReadOnlyList<string> blockedTags,
        bool filterAiPosts,
        UserSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new OpenAiTagDiscoveryException("OpenAI API key is not set. Add it on the Account tab.");
        }

        var enabledCategories = ForYouLearningGate.DescribeEnabledCategories(settings);
        var systemPrompt = SystemPrompt.Replace("{{ENABLED_CATEGORIES}}", enabledCategories, StringComparison.Ordinal);

        var recent = activities
            .OrderByDescending(a => a.TimestampUnix)
            .Take(80)
            .Select(entry => new
            {
                kind = MapKind(entry.SignalType),
                timestampUtc = entry.TimestampUnix * 1000L,
                topic = entry.Topic,
                detail = entry.Detail,
            })
            .ToList();

        var payload = JsonSerializer.Serialize(new
        {
            recent_activity = recent,
            blocked_tags = blockedTags.Where(t => !string.IsNullOrWhiteSpace(t)).Take(40).ToList(),
            filter_ai_posts = filterAiPosts,
        });

        var content = await OpenAiTagDiscoveryService.SendChatJsonPublicAsync(
            http,
            apiKey,
            model,
            systemPrompt,
            payload,
            cancellationToken,
            temperature: 0.2).ConfigureAwait(false);

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        var topics = NormalizeTopics(root, settings);
        topics = MergeHiddenTopics(topics, root, settings);

        return new OpenAiForYouProfileResult
        {
            Summary = root.TryGetProperty("summary", out var summaryEl)
                ? summaryEl.GetString()?.Trim() ?? string.Empty
                : string.Empty,
            UsedOpenAi = true,
            Topics = topics,
            SearchLines = NormalizeSearchLines(root),
        };
    }

    private static string MapKind(ForYouSignalType type) =>
        type == ForYouSignalType.PostOpened ? "OpenPost" : type.ToString();

    private static List<ForYouTopicProfile> NormalizeTopics(JsonElement root, UserSettings settings)
    {
        if (!root.TryGetProperty("topics", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var results = new List<ForYouTopicProfile>();
        foreach (var item in array.EnumerateArray())
        {
            var topic = UserSettings.NormalizeTagToken(item.TryGetProperty("topic", out var topicEl)
                ? topicEl.GetString() ?? string.Empty
                : string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(topic) ||
                ForYouLearningFilter.IsExcluded(topic) ||
                !ForYouLearningGate.CanLearn(topic, settings))
            {
                continue;
            }

            results.Add(new ForYouTopicProfile
            {
                Topic = topic,
                Weight = 0,
                Reason = item.TryGetProperty("reason", out var reasonEl)
                    ? reasonEl.GetString()?.Trim() ?? string.Empty
                    : string.Empty,
            });
        }

        return results;
    }

    private static List<ForYouSearchLine> NormalizeSearchLines(JsonElement root)
    {
        if (!root.TryGetProperty("search_lines", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var results = new List<ForYouSearchLine>();
        foreach (var item in array.EnumerateArray())
        {
            var query = UserSettings.NormalizeTagToken(item.TryGetProperty("query", out var queryEl)
                ? queryEl.GetString() ?? string.Empty
                : string.Empty).Replace('_', ' ').Trim();
            if (string.IsNullOrWhiteSpace(query) || ForYouLearningFilter.IsExcluded(query))
            {
                continue;
            }

            var label = item.TryGetProperty("label", out var labelEl)
                ? labelEl.GetString()?.Trim() ?? string.Empty
                : string.Empty;
            if (string.IsNullOrWhiteSpace(label))
            {
                label = query;
            }

            var score = item.TryGetProperty("score", out var scoreEl) ? scoreEl.GetInt32() : 0;
            results.Add(new ForYouSearchLine
            {
                Query = query,
                Label = label,
                Reason = item.TryGetProperty("reason", out var reasonEl) ? reasonEl.GetString()?.Trim() ?? string.Empty : string.Empty,
                Score = Math.Clamp(score, 0, 100),
                Source = "OpenAI",
            });
        }

        return results;
    }

    private static List<ForYouTopicProfile> MergeHiddenTopics(
        List<ForYouTopicProfile> topics,
        JsonElement root,
        UserSettings settings)
    {
        if (!root.TryGetProperty("hidden_topics", out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return topics;
        }

        var mutable = topics.ToList();
        foreach (var item in array.EnumerateArray())
        {
            var topic = UserSettings.NormalizeTagToken(item.TryGetProperty("topic", out var topicEl)
                ? topicEl.GetString() ?? string.Empty
                : string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(topic) || ForYouLearningFilter.IsExcluded(topic))
            {
                continue;
            }

            var reason = item.TryGetProperty("reason", out var reasonEl) ? reasonEl.GetString()?.Trim() ?? string.Empty : string.Empty;
            var existing = mutable.FirstOrDefault(t => t.Topic.Equals(topic, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                mutable.Add(new ForYouTopicProfile
                {
                    Topic = topic,
                    Weight = ForYouSignalStrengths.MinBlockedTopicScore,
                    IsBlocked = true,
                    Reason = reason,
                });
            }
            else
            {
                existing.IsBlocked = true;
                if (string.IsNullOrWhiteSpace(existing.Reason))
                {
                    existing.Reason = reason;
                }
            }
        }

        return mutable;
    }

    private sealed class AiTopicRow
    {
        [JsonPropertyName("topic")]
        public string? Topic { get; set; }

        [JsonPropertyName("score")]
        public int Score { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }
    }
}
