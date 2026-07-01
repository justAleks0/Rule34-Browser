using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Rule34Gallery.Core.Services;

public sealed class WebSearchResultItem
{
    public string Title { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public string Snippet { get; init; } = string.Empty;
}

/// <summary>Multi-provider web search for fact-backed memory lookup (Reddit, Wikipedia, DDG fallback).</summary>
public static class WebSearchClient
{
    private const string DuckDuckGoLiteUrl = "https://lite.duckduckgo.com/lite/";
    private const string RedditUserAgent = "Rule34Gallery/1.0 (memory-search; +https://github.com)";
    private const string BrowserUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    private static readonly HttpClient SearchHttp = CreateSearchHttp();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly Regex ResultLinkRegex = new(
        """class=['"]result-link['"][^>]*>(?<title>.*?)</a>""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly Regex SnippetRegex = new(
        """class=['"]result-snippet['"][^>]*>(?<snippet>.*?)</td>""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly Regex UrlRegex = new(
        """class=['"]link-text['"][^>]*>(?<url>.*?)</span>""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    private static readonly (string Pattern, string Subreddit)[] FranchiseSubreddits =
    [
        ("honkai star rail", "HonkaiStarRail"),
        ("hsr", "HonkaiStarRail"),
        ("genshin impact", "Genshin_Impact"),
        ("genshin", "Genshin_Impact"),
        ("zenless zone zero", "ZenlessZoneZero"),
        ("zzz", "ZenlessZoneZero"),
        ("fate/", "fatestaynight"),
        ("hololive", "Hololive"),
        ("invincible", "Invincible"),
    ];

    /// <summary>Runs all providers for one query; prefers Reddit + Wikipedia when DDG is blocked.</summary>
    public static Task<IReadOnlyList<WebSearchResultItem>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default,
        int maxResults = 8)
        => SearchComprehensiveAsync(query, cancellationToken, maxResults);

    public static async Task<IReadOnlyList<WebSearchResultItem>> SearchComprehensiveAsync(
        string userQuery,
        CancellationToken cancellationToken = default,
        int maxResults = 20)
    {
        var trimmed = userQuery.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return [];
        }

        var batches = new List<WebSearchResultItem>();

        var subreddit = DetectSubreddit(trimmed);
        var focusTerm = ExtractFocusSearchTerm(trimmed);
        if (subreddit is not null && !string.IsNullOrWhiteSpace(focusTerm))
        {
            batches.AddRange(await SearchRedditSubredditAsync(subreddit, focusTerm, cancellationToken).ConfigureAwait(false));
        }

        batches.AddRange(await SearchRedditAsync(trimmed, cancellationToken).ConfigureAwait(false));
        batches.AddRange(await SearchWikipediaAsync(trimmed, cancellationToken).ConfigureAwait(false));

        var wikiShort = BuildWikipediaQuery(trimmed);
        if (!string.Equals(wikiShort, trimmed, StringComparison.OrdinalIgnoreCase))
        {
            batches.AddRange(await SearchWikipediaAsync(wikiShort, cancellationToken).ConfigureAwait(false));
        }

        var merged = MergeDistinct(batches, maxResults);
        if (merged.Count >= 6)
        {
            return merged;
        }

        batches.AddRange(await SearchDuckDuckGoLiteAsync(trimmed, cancellationToken).ConfigureAwait(false));
        return MergeDistinct(batches, maxResults);
    }

    public static IReadOnlyList<WebSearchResultItem> MergeDistinct(
        IEnumerable<WebSearchResultItem> items,
        int maxResults = 20)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var merged = new List<WebSearchResultItem>();
        foreach (var item in items)
        {
            var key = !string.IsNullOrWhiteSpace(item.Url)
                ? item.Url
                : $"{item.Title}|{item.Snippet}";
            if (!seen.Add(key))
            {
                continue;
            }

            merged.Add(item);
            if (merged.Count >= maxResults)
            {
                break;
            }
        }

        return merged;
    }

    public static string FormatEvidenceBlock(IReadOnlyList<WebSearchResultItem> items)
    {
        if (items.Count == 0)
        {
            return "(no web results)";
        }

        var sb = new StringBuilder();
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            sb.AppendLine($"{i + 1}. Title: {item.Title}");
            if (!string.IsNullOrWhiteSpace(item.Url))
            {
                sb.AppendLine($"   URL: {item.Url}");
            }

            if (!string.IsNullOrWhiteSpace(item.Snippet))
            {
                sb.AppendLine($"   Snippet: {item.Snippet}");
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static async Task<IReadOnlyList<WebSearchResultItem>> SearchRedditAsync(
        string query,
        CancellationToken cancellationToken,
        int limit = 8)
    {
        var url =
            $"https://www.reddit.com/search.json?q={Uri.EscapeDataString(query)}&limit={limit}&sort=relevance";
        return await FetchRedditJsonAsync(url, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<WebSearchResultItem>> SearchRedditSubredditAsync(
        string subreddit,
        string query,
        CancellationToken cancellationToken,
        int limit = 10)
    {
        var url =
            $"https://www.reddit.com/r/{Uri.EscapeDataString(subreddit)}/search.json?q={Uri.EscapeDataString(query)}&restrict_sr=1&limit={limit}&sort=relevance";
        return await FetchRedditJsonAsync(url, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<WebSearchResultItem>> FetchRedditJsonAsync(
        string url,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", RedditUserAgent);

            using var response = await SearchHttp.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var parsed = JsonSerializer.Deserialize<RedditListingResponse>(json, JsonOptions);
            var children = parsed?.Data?.Children;
            if (children is not { Count: > 0 })
            {
                return [];
            }

            var results = new List<WebSearchResultItem>();
            foreach (var child in children)
            {
                var post = child?.Data;
                if (post is null || string.IsNullOrWhiteSpace(post.Title))
                {
                    continue;
                }

                var snippet = CleanHtml(post.SelfText ?? string.Empty);
                if (snippet.Length > 280)
                {
                    snippet = snippet[..280] + "…";
                }

                var link = post.Permalink?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(link) && link.StartsWith('/'))
                {
                    link = "https://www.reddit.com" + link;
                }

                results.Add(new WebSearchResultItem
                {
                    Title = CleanHtml(post.Title),
                    Snippet = snippet,
                    Url = link,
                });
            }

            return results;
        }
        catch
        {
            return [];
        }
    }

    private static async Task<IReadOnlyList<WebSearchResultItem>> SearchWikipediaAsync(
        string query,
        CancellationToken cancellationToken,
        int limit = 8)
    {
        try
        {
            var url =
                "https://en.wikipedia.org/w/api.php?action=query&list=search" +
                $"&srsearch={Uri.EscapeDataString(query)}&srlimit={limit}&format=json&utf8=1";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("User-Agent", RedditUserAgent);

            using var response = await SearchHttp.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var parsed = JsonSerializer.Deserialize<WikipediaSearchResponse>(json, JsonOptions);
            var hits = parsed?.Query?.Search;
            if (hits is not { Count: > 0 })
            {
                return [];
            }

            var results = new List<WebSearchResultItem>();
            foreach (var hit in hits)
            {
                if (string.IsNullOrWhiteSpace(hit.Title))
                {
                    continue;
                }

                var wikiUrl = "https://en.wikipedia.org/wiki/" +
                              Uri.EscapeDataString(hit.Title.Replace(' ', '_'));

                results.Add(new WebSearchResultItem
                {
                    Title = hit.Title,
                    Snippet = CleanHtml(hit.Snippet ?? string.Empty),
                    Url = wikiUrl,
                });
            }

            return results;
        }
        catch
        {
            return [];
        }
    }

    private static async Task<IReadOnlyList<WebSearchResultItem>> SearchDuckDuckGoLiteAsync(
        string query,
        CancellationToken cancellationToken,
        int maxResults = 8)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (attempt > 0)
            {
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }

            try
            {
                var parsed = await FetchDuckDuckGoLiteAsync(query, maxResults, cancellationToken)
                    .ConfigureAwait(false);
                if (parsed.Count > 0)
                {
                    return parsed;
                }
            }
            catch
            {
                // DDG often blocks bots — other providers are primary.
            }
        }

        return [];
    }

    private static async Task<IReadOnlyList<WebSearchResultItem>> FetchDuckDuckGoLiteAsync(
        string query,
        int maxResults,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, DuckDuckGoLiteUrl);
        request.Headers.TryAddWithoutValidation("User-Agent", BrowserUserAgent);
        request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml;q=0.9,*/*;q=0.8");
        request.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["q"] = query,
        });

        using var response = await SearchHttp.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(html) ||
            !response.IsSuccessStatusCode ||
            !html.Contains("result-link", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        return ParseDuckDuckGoHtml(html, maxResults);
    }

    private static IReadOnlyList<WebSearchResultItem> ParseDuckDuckGoHtml(string html, int maxResults)
    {
        var titles = ResultLinkRegex.Matches(html).Cast<Match>()
            .Select(m => CleanHtml(m.Groups["title"].Value))
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();
        if (titles.Count == 0)
        {
            return [];
        }

        var snippets = SnippetRegex.Matches(html).Cast<Match>()
            .Select(m => CleanHtml(m.Groups["snippet"].Value))
            .ToList();
        var urls = UrlRegex.Matches(html).Cast<Match>()
            .Select(m => CleanHtml(m.Groups["url"].Value))
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .ToList();

        var count = Math.Min(maxResults, titles.Count);
        var results = new List<WebSearchResultItem>(count);
        for (var i = 0; i < count; i++)
        {
            results.Add(new WebSearchResultItem
            {
                Title = titles[i],
                Snippet = i < snippets.Count ? snippets[i] : string.Empty,
                Url = i < urls.Count ? urls[i] : string.Empty,
            });
        }

        return results;
    }

    private static string? DetectSubreddit(string query)
    {
        var lower = query.ToLowerInvariant();
        foreach (var (pattern, subreddit) in FranchiseSubreddits)
        {
            if (lower.Contains(pattern, StringComparison.Ordinal))
            {
                return subreddit;
            }
        }

        return null;
    }

    private static string ExtractFocusSearchTerm(string query)
    {
        var lower = query.ToLowerInvariant();
        string[] priority =
        [
            "bow", "archer", "sword", "spear", "viltrumite", "vtuber", "elf", "shark",
            "gyaru", "time", "clock", "blue hair", "pink hair",
        ];

        foreach (var term in priority)
        {
            if (lower.Contains(term, StringComparison.Ordinal))
            {
                return term;
            }
        }

        var words = query.Split([' ', ',', '.'], StringSplitOptions.RemoveEmptyEntries);
        return words.Length > 0 ? words[^1] : query;
    }

    private static string BuildWikipediaQuery(string query)
    {
        var lower = query.ToLowerInvariant();
        if (lower.Contains("honkai star rail", StringComparison.Ordinal))
        {
            if (lower.Contains("bow", StringComparison.Ordinal))
            {
                return "Honkai Star Rail bow character";
            }

            return "Honkai Star Rail character";
        }

        return query;
    }

    private static HttpClient CreateSearchHttp()
    {
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        };

        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(25),
        };
    }

    private static string CleanHtml(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var noTags = Regex.Replace(raw, "<[^>]+>", " ");
        return WebUtility.HtmlDecode(noTags)
            .Replace('\u00a0', ' ')
            .Trim();
    }

    private sealed class RedditListingResponse
    {
        [JsonPropertyName("data")]
        public RedditListingData? Data { get; set; }
    }

    private sealed class RedditListingData
    {
        [JsonPropertyName("children")]
        public List<RedditChild>? Children { get; set; }
    }

    private sealed class RedditChild
    {
        [JsonPropertyName("data")]
        public RedditPostData? Data { get; set; }
    }

    private sealed class RedditPostData
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("selftext")]
        public string? SelfText { get; set; }

        [JsonPropertyName("permalink")]
        public string? Permalink { get; set; }
    }

    private sealed class WikipediaSearchResponse
    {
        [JsonPropertyName("query")]
        public WikipediaQuery? Query { get; set; }
    }

    private sealed class WikipediaQuery
    {
        [JsonPropertyName("search")]
        public List<WikipediaHit>? Search { get; set; }
    }

    private sealed class WikipediaHit
    {
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("snippet")]
        public string? Snippet { get; set; }
    }
}
