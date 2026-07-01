using System.Collections.Concurrent;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Rule34Gallery.Core.Services;

namespace Rule34Gallery.Core;

public static class Rule34Api
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static bool HasCredentials(string? userId, string? apiKey)
        => !string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(apiKey);

    public static bool LooksLikeCredentialBlob(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return text.Contains("api_key", StringComparison.OrdinalIgnoreCase) &&
               text.Contains("user_id", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Parses a Rule34 "API Access Credentials" blob, e.g.
    /// &amp;api_key=...&amp;user_id=123 or api_key=...&amp;user_id=123
    /// </summary>
    private static readonly Regex ApiKeyRegex = new(
        @"api_key\s*=\s*([^&\s""']+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex UserIdRegex = new(
        @"user_id\s*=\s*([^&\s""']+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool TryParseCredentialBlob(string? input, out string userId, out string apiKey)
    {
        userId = string.Empty;
        apiKey = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var text = NormalizeCredentialBlob(input);
        if (!text.Contains("api_key", StringComparison.OrdinalIgnoreCase) ||
            !text.Contains("user_id", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (text.StartsWith('?'))
        {
            text = text[1..];
        }

        text = text.TrimStart('&');

        foreach (var part in text.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var equals = part.IndexOf('=');
            if (equals <= 0)
            {
                continue;
            }

            var name = part[..equals].Trim().TrimStart('?');
            if (name.StartsWith("amp;", StringComparison.OrdinalIgnoreCase))
            {
                name = name[4..];
            }

            var value = DecodeCredentialValue(part[(equals + 1)..].Trim());
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            if (name.Equals("user_id", StringComparison.OrdinalIgnoreCase))
            {
                userId = value;
            }
            else if (name.Equals("api_key", StringComparison.OrdinalIgnoreCase))
            {
                apiKey = value;
            }
        }

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(apiKey))
        {
            var apiMatch = ApiKeyRegex.Match(text);
            var userMatch = UserIdRegex.Match(text);
            if (apiMatch.Success)
            {
                apiKey = DecodeCredentialValue(apiMatch.Groups[1].Value);
            }

            if (userMatch.Success)
            {
                userId = DecodeCredentialValue(userMatch.Groups[1].Value);
            }
        }

        return !string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(apiKey);
    }

    private static string NormalizeCredentialBlob(string input)
    {
        var text = input.Trim()
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Replace("&amp;", "&", StringComparison.OrdinalIgnoreCase);

        return text;
    }

    private static string DecodeCredentialValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        value = value.Trim().Trim('"', '\'');
        try
        {
            return Uri.UnescapeDataString(value);
        }
        catch
        {
            return value;
        }
    }

    public static bool LooksLikeUserId(string userId) => int.TryParse(userId, out _);

    public static bool IsAuthenticationError(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return false;
        }

        return error.Contains("authentication", StringComparison.OrdinalIgnoreCase)
            || error.Contains("api key", StringComparison.OrdinalIgnoreCase)
            || error.Contains("api_key", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Builds the tags query, matching the site's "Filter AI posts" option (-ai*).
    /// </summary>
    public static string BuildSearchTags(string userTags, bool filterAi)
    {
        var parts = userTags
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => !t.Equals("-ai*", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (filterAi)
        {
            parts = parts
                .Where(t => !t.StartsWith("ai", StringComparison.OrdinalIgnoreCase))
                .ToList();
            parts.Add("-ai*");
        }

        return string.Join(' ', parts);
    }

    public static string BuildPostSearchUrl(
        int page,
        string limit,
        string apiTags,
        string userId,
        string apiKey,
        bool includeTagInfo = true)
    {
        var url =
            $"https://api.rule34.xxx/index.php?page=dapi&s=post&q=index&json=1&pid={page}&limit={Uri.EscapeDataString(limit)}&tags={Uri.EscapeDataString(apiTags)}";
        if (includeTagInfo)
        {
            url += "&fields=tag_info";
        }

        url += $"&api_key={Uri.EscapeDataString(apiKey.Trim())}&user_id={Uri.EscapeDataString(userId.Trim())}";
        return url;
    }

    public static string BuildPostByIdUrl(int postId, string userId, string apiKey, bool includeTagInfo = true)
    {
        var url =
            $"https://api.rule34.xxx/index.php?page=dapi&s=post&q=index&json=1&id={postId}";
        if (includeTagInfo)
        {
            url += "&fields=tag_info";
        }

        url += $"&api_key={Uri.EscapeDataString(apiKey.Trim())}&user_id={Uri.EscapeDataString(userId.Trim())}";
        return url;
    }

    public static async Task<PostItem?> FetchPostByIdAsync(
        HttpClient http,
        int postId,
        string userId,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        if (postId <= 0 || string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        var url = BuildPostByIdUrl(postId, userId.Trim(), apiKey);
        var json = await FetchPostSearchJsonAsync(http, url, cancellationToken).ConfigureAwait(false);
        var posts = ParsePostSearchResponse(json, out var error);
        if (!string.IsNullOrWhiteSpace(error))
        {
            throw new InvalidOperationException(error);
        }

        return posts.FirstOrDefault(p => p.Id == postId) ?? posts.FirstOrDefault();
    }

    public static async Task<string> FetchPostSearchJsonAsync(
        HttpClient http,
        string url,
        CancellationToken cancellationToken = default)
    {
        var json = await FetchApiBodyAsync(http, url, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        if (url.Contains("fields=tag_info", StringComparison.Ordinal))
        {
            var fallbackUrl = url.Replace("&fields=tag_info", string.Empty, StringComparison.Ordinal);
            json = await FetchApiBodyAsync(http, fallbackUrl, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(json))
            {
                return json;
            }
        }

        return string.Empty;
    }

    public static async Task<string> FetchApiBodyAsync(
        HttpClient http,
        string url,
        CancellationToken cancellationToken = default)
    {
        Exception? lastError = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                var body = await FetchApiBodyOnceAsync(http, url, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    return body;
                }

                lastError = new InvalidOperationException("Empty response body.");
            }
            catch (Exception ex) when (attempt < 2 && IsTransientFetchError(ex))
            {
                lastError = ex;
            }

            if (attempt < 2)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(400 * (attempt + 1)), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        if (lastError is InvalidOperationException)
        {
            return string.Empty;
        }

        throw lastError ?? new InvalidOperationException("Rule34 request failed.");
    }

    private static async Task<string> FetchApiBodyOnceAsync(
        HttpClient http,
        string url,
        CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                throw new InvalidOperationException(
                    $"Rule34 returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}) with no details.");
            }

            var snippet = body.Trim();
            if (snippet.Length > 160)
            {
                snippet = snippet[..160] + "…";
            }

            throw new InvalidOperationException($"Rule34 returned HTTP {(int)response.StatusCode}: {snippet}");
        }

        return body;
    }

    private static bool IsTransientFetchError(Exception ex)
        => ex is HttpRequestException or TaskCanceledException;

    public static IReadOnlyList<PostItem> ParsePostSearchResponse(string json, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            error = "Rule34 returned an empty response. Confirm your API key on Account → Options, then try again. The site may also be temporarily overloaded.";
            return [];
        }

        var trimmed = json.AsSpan().Trim();
        if (trimmed.Length == 0 ||
            (trimmed[0] != '{' && trimmed[0] != '[' && trimmed[0] != '"'))
        {
            error = "Rule34 returned an unexpected response (not JSON). The site may be down or blocking requests.";
            return [];
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            error = "Could not read Rule34's response. Try again or verify your API credentials on Account.";
            return [];
        }

        using (doc)
        {
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.String)
            {
                error = root.GetString() ?? "API returned an error.";
                return [];
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("success", out var success) &&
                    success.ValueKind == JsonValueKind.False &&
                    root.TryGetProperty("message", out var message))
                {
                    error = message.GetString() ?? "Search failed.";
                    return [];
                }

                if (root.TryGetProperty("post", out var postElement))
                {
                    return DeserializePosts(postElement);
                }

                error = "Unexpected API response format.";
                return [];
            }

            if (root.ValueKind == JsonValueKind.Array)
            {
                return DeserializePosts(root);
            }

            error = "Unexpected API response format.";
            return [];
        }
    }

    public static IReadOnlyList<string> ParseAutocompleteResponse(string json)
        => ParseAutocompleteSuggestions(json).Select(s => s.Value).ToList();

    public static IReadOnlyList<TagSuggestion> ParseAutocompleteSuggestions(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return [];
        }

        using (doc)
        {
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.String || root.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var results = new List<TagSuggestion>();
            foreach (var item in root.EnumerateArray())
            {
                string? value = null;
                string? label = null;

                if (item.ValueKind == JsonValueKind.String)
                {
                    value = item.GetString();
                }
                else if (item.ValueKind == JsonValueKind.Object)
                {
                    if (item.TryGetProperty("value", out var valueProp))
                    {
                        value = valueProp.GetString();
                    }

                    if (item.TryGetProperty("label", out var labelProp))
                    {
                        label = labelProp.GetString();
                    }
                }

                var tag = !string.IsNullOrWhiteSpace(value) ? value : label;
                if (string.IsNullOrWhiteSpace(tag))
                {
                    continue;
                }

                var category = TagCategoryColors.InferCategory(tag);
                if (item.ValueKind == JsonValueKind.Object &&
                    item.TryGetProperty("type", out var typeProp) &&
                    typeProp.ValueKind == JsonValueKind.Number)
                {
                    category = MapApiTagType(typeProp.GetInt32());
                }

                results.Add(new TagSuggestion(tag, category));
            }

            return results;
        }
    }

    private static TagCategory MapApiTagType(int type) => type switch
    {
        1 => TagCategory.Artist,
        3 => TagCategory.Copyright,
        4 => TagCategory.Character,
        5 => TagCategory.Meta,
        _ => TagCategory.General,
    };

    public static TagCategory MapTagInfoType(string? type) => type?.Trim().ToLowerInvariant() switch
    {
        "artist" => TagCategory.Artist,
        "character" => TagCategory.Character,
        "copyright" => TagCategory.Copyright,
        "metadata" => TagCategory.Meta,
        _ => TagCategory.General,
    };

    private static readonly ConcurrentDictionary<string, TagCategory> TagCategoryCache = new(StringComparer.OrdinalIgnoreCase);

    public static async Task<IReadOnlyDictionary<string, TagCategory>> ResolveTagCategoriesAsync(
        HttpClient http,
        IEnumerable<string> tags,
        string userId,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, TagCategory>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in tags.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase).Take(100))
        {
            if (TagCategoryCache.TryGetValue(tag, out var cached))
            {
                result[tag] = cached;
                continue;
            }

            var inferred = TagCategoryColors.InferCategory(tag);
            if (!HasCredentials(userId, apiKey))
            {
                TagCategoryCache[tag] = inferred;
                result[tag] = inferred;
                continue;
            }

            try
            {
                var url =
                    $"https://api.rule34.xxx/index.php?page=dapi&s=tag&json=1&name={Uri.EscapeDataString(tag)}&user_id={Uri.EscapeDataString(userId)}&api_key={Uri.EscapeDataString(apiKey)}";
                var json = await FetchApiBodyAsync(http, url, cancellationToken).ConfigureAwait(false);
                var category = ParseTagCategoryResponse(json, tag);
                TagCategoryCache[tag] = category;
                result[tag] = category;
            }
            catch
            {
                TagCategoryCache[tag] = inferred;
                result[tag] = inferred;
            }
        }

        return result;
    }

    private static TagCategory ParseTagCategoryResponse(string json, string fallbackTag)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.String)
        {
            return TagCategoryColors.InferCategory(fallbackTag);
        }

        if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
        {
            return ReadTagType(root[0], fallbackTag);
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            return ReadTagType(root, fallbackTag);
        }

        return TagCategoryColors.InferCategory(fallbackTag);
    }

    private static TagCategory ReadTagType(JsonElement element, string fallbackTag)
    {
        if (element.TryGetProperty("type", out var typeProp))
        {
            if (typeProp.ValueKind == JsonValueKind.Number && typeProp.TryGetInt32(out var numericType))
            {
                return MapApiTagType(numericType);
            }

            if (typeProp.ValueKind == JsonValueKind.String)
            {
                return MapTagInfoType(typeProp.GetString());
            }
        }

        if (element.TryGetProperty("name", out var nameProp))
        {
            return TagCategoryColors.InferCategory(nameProp.GetString());
        }

        if (element.TryGetProperty("tag", out var tagProp))
        {
            return TagCategoryColors.InferCategory(tagProp.GetString());
        }

        return TagCategoryColors.InferCategory(fallbackTag);
    }

    private static IReadOnlyList<PostItem> DeserializePosts(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return [];
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            var single = element.Deserialize<PostItem>(JsonOptions);
            return single is null ? [] : [single];
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return element.Deserialize<List<PostItem>>(JsonOptions) ?? [];
    }
}

public sealed class PostItem : INotifyPropertyChanged
{
    private bool _isFavorite;

    private bool _isInWatchLater;

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("preview_url")]
    public string PreviewUrl { get; set; } = string.Empty;

    [JsonPropertyName("sample_url")]
    public string SampleUrl { get; set; } = string.Empty;

    [JsonPropertyName("file_url")]
    public string FileUrl { get; set; } = string.Empty;

    [JsonPropertyName("rating")]
    public string Rating { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("tags")]
    public string Tags { get; set; } = string.Empty;

    [JsonIgnore]
    public bool IsLocal { get; set; }

    [JsonIgnore]
    public string LocalCategory { get; set; } = string.Empty;

    [JsonIgnore]
    public string LocalLibraryName { get; set; } = string.Empty;

    [JsonIgnore]
    public bool ShowCloudActions => !IsLocal;

    public bool ShowLocalThumbEdit => IsLocal && IsPlayableMedia;

    [JsonPropertyName("owner")]
    public string Owner { get; set; } = string.Empty;

    [JsonPropertyName("tag_info")]
    public List<PostTagInfo>? TagInfo { get; set; }

    public IReadOnlyList<string> GetTagList()
    {
        if (string.IsNullOrWhiteSpace(Tags))
        {
            return [];
        }

        return Tags
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(tag => !IsPlaceholderLocalTag(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string GalleryTitle
    {
        get
        {
            if (IsLocal)
            {
                return Id > 0 && !LooksLikePathHashId()
                    ? $"#{Id}"
                    : System.IO.Path.GetFileName(FileUrl);
            }

            var title = BuildTagGalleryLine();
            if (!string.IsNullOrWhiteSpace(title))
            {
                return title;
            }

            return !string.IsNullOrWhiteSpace(Owner) ? Owner : $"#{Id}";
        }
    }

    public string GallerySubtitle
    {
        get
        {
            if (!IsLocal)
            {
                return string.Empty;
            }

            var tags = GetTagList();
            if (tags.Count > 0)
            {
                return BuildTagGalleryLine(tags);
            }

            return string.IsNullOrWhiteSpace(LocalCategory)
                ? string.Empty
                : LocalLibraryService.FormatCategoryDisplay(LocalCategory);
        }
    }

    public bool HasGallerySubtitle => !string.IsNullOrWhiteSpace(GallerySubtitle);

    private bool LooksLikePathHashId()
        => IsLocal && !string.IsNullOrWhiteSpace(FileUrl) && Id == FileUrl.GetHashCode();

    private static bool IsPlaceholderLocalTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return true;
        }

        return tag.Equals("local", StringComparison.OrdinalIgnoreCase) ||
               tag.StartsWith("local\\", StringComparison.OrdinalIgnoreCase) ||
               tag.StartsWith("local/", StringComparison.OrdinalIgnoreCase);
    }

    private string BuildTagGalleryLine(IReadOnlyList<string>? tags = null)
    {
        tags ??= GetTagList();
        if (tags.Count == 0)
        {
            return string.Empty;
        }

        var map = GetTagCategoryMap();
        var picked = new List<string>();

        void AddFromCategory(TagCategory category, int max)
        {
            foreach (var tag in tags.Where(t => map.TryGetValue(t, out var cat) && cat == category).Take(max))
            {
                picked.Add(tag.Replace('_', ' '));
            }
        }

        AddFromCategory(TagCategory.Copyright, 1);
        AddFromCategory(TagCategory.Character, 2);
        AddFromCategory(TagCategory.Artist, 1);

        if (picked.Count == 0)
        {
            picked.AddRange(tags.Take(3).Select(t => t.Replace('_', ' ')));
        }

        var text = string.Join(" · ", picked);
        return text.Length <= 80 ? text : text[..77] + "…";
    }

    public IReadOnlyDictionary<string, TagCategory> GetTagCategoryMap()
    {
        var map = new Dictionary<string, TagCategory>(StringComparer.OrdinalIgnoreCase);

        if (TagInfo is { Count: > 0 })
        {
            foreach (var info in TagInfo)
            {
                if (string.IsNullOrWhiteSpace(info.Tag))
                {
                    continue;
                }

                map[info.Tag] = Rule34Api.MapTagInfoType(info.Type);
            }

            return map;
        }

        foreach (var tag in GetTagList())
        {
            map[tag] = TagCategoryColors.InferCategory(tag);
        }

        return map;
    }

    public bool IsFavorite
    {
        get => _isFavorite;
        set
        {
            if (_isFavorite == value)
            {
                return;
            }

            _isFavorite = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FavoriteGlyph));
        }
    }

    public string FavoriteGlyph => IsFavorite ? "★" : "☆";

    public bool IsInWatchLater
    {
        get => _isInWatchLater;
        set
        {
            if (_isInWatchLater == value)
            {
                return;
            }

            _isInWatchLater = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(WatchLaterGlyph));
        }
    }

    public string WatchLaterGlyph => IsInWatchLater ? "⏱" : "○";

    public PostMediaType MediaType => PostMedia.DetectType(FileUrl, SampleUrl, Tags);

    public bool IsPlayableMedia => PostMedia.IsPlayableMedia(MediaType);

    public string MediaBadge => PostMedia.GetMediaBadge(MediaType);

    public bool ShowMediaBadge => IsPlayableMedia;

    public string ThumbnailUrl
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(PreviewUrl))
            {
                return PreviewUrl;
            }

            if (PostMedia.IsRasterImageUrl(SampleUrl))
            {
                return SampleUrl;
            }

            if (PostMedia.IsRasterImageUrl(FileUrl))
            {
                return FileUrl;
            }

            return string.Empty;
        }
    }

    public bool HasDisplayableThumbnail => !string.IsNullOrWhiteSpace(ThumbnailUrl);

    public string PlaybackUrl =>
        !string.IsNullOrWhiteSpace(FileUrl) ? FileUrl
        : !string.IsNullOrWhiteSpace(SampleUrl) ? SampleUrl
        : string.Empty;

    public string PosterUrl
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(PreviewUrl))
            {
                return PreviewUrl;
            }

            if (PostMedia.IsRasterImageUrl(SampleUrl))
            {
                return SampleUrl;
            }

            return ThumbnailUrl;
        }
    }

    public string FastViewerUrl
    {
        get
        {
            if (IsPlayableMedia)
            {
                return PosterUrl;
            }

            return !string.IsNullOrWhiteSpace(SampleUrl) ? SampleUrl
                : !string.IsNullOrWhiteSpace(PreviewUrl) ? PreviewUrl
                : FileUrl;
        }
    }

    public string FullViewerUrl => IsPlayableMedia ? PlaybackUrl : FileUrl;

    /// <summary>Human-facing post page on rule34.xxx (not the CDN/file URL).</summary>
    public string SitePostUrl => Id > 0
        ? $"https://rule34.xxx/index.php?page=post&s=view&id={Id}"
        : string.Empty;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class PostTagInfo
{
    [JsonPropertyName("tag")]
    public string Tag { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "tag";

    [JsonPropertyName("count")]
    public int Count { get; set; }
}
