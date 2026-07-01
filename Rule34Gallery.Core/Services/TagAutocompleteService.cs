using System.Collections.ObjectModel;
using System.Net.Http;
using Rule34Gallery.Core.Services.Browse;

namespace Rule34Gallery.Core.Services;

public static class TagAutocompleteService
{
    public static string? GetActiveToken(string input)
    {
        var query = input.Trim();
        if (string.IsNullOrEmpty(query))
        {
            return null;
        }

        var lastToken = query.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        return string.IsNullOrWhiteSpace(lastToken) ? null : lastToken;
    }

    public static Task<IReadOnlyList<TagSuggestion>> FetchSuggestionsForQueryAsync(
        HttpClient http,
        string query,
        int maxResults = 12,
        CancellationToken cancellationToken = default)
        => FetchSuggestionsForQueryAsync(
            http,
            GallerySource.Rule34,
            BrowseSourceCredentials.FromRule34(new UserSettings()),
            query,
            maxResults,
            cancellationToken);

    public static async Task<IReadOnlyList<TagSuggestion>> FetchSuggestionsForQueryAsync(
        HttpClient http,
        GallerySource source,
        BrowseSourceCredentials credentials,
        string query,
        int maxResults = 12,
        CancellationToken cancellationToken = default)
    {
        var lastToken = GetActiveToken(query) ?? query.Trim();
        if (lastToken.Length < 2)
        {
            return [];
        }

        var client = BrowseSourceClientFactory.Get(source);
        return await client.AutocompleteAsync(http, lastToken, credentials, maxResults, cancellationToken);
    }

    public static Task<IReadOnlyList<TagSuggestion>> FetchRule34SuggestionsAsync(
        HttpClient http,
        string prefix,
        int maxResults = 12,
        CancellationToken cancellationToken = default)
    {
        var trimmed = prefix.Trim();
        if (trimmed.Length < 2)
        {
            return Task.FromResult<IReadOnlyList<TagSuggestion>>([]);
        }

        try
        {
            var url = $"https://api.rule34.xxx/autocomplete.php?q={Uri.EscapeDataString(trimmed)}";
            return FetchRule34FromUrlAsync(http, url, maxResults, cancellationToken);
        }
        catch
        {
            return Task.FromResult<IReadOnlyList<TagSuggestion>>([]);
        }
    }

    private static async Task<IReadOnlyList<TagSuggestion>> FetchRule34FromUrlAsync(
        HttpClient http,
        string url,
        int maxResults,
        CancellationToken cancellationToken)
    {
        var json = await http.GetStringAsync(url, cancellationToken);
        return Rule34Api.ParseAutocompleteSuggestions(json).Take(maxResults).ToList();
    }

    public static Task<bool> TryPopulateSuggestionsAsync(
        HttpClient http,
        ObservableCollection<TagSuggestion> target,
        string inputText)
        => TryPopulateSuggestionsAsync(
            http,
            GallerySource.Rule34,
            BrowseSourceCredentials.FromRule34(new UserSettings()),
            target,
            inputText);

    public static async Task<bool> TryPopulateSuggestionsAsync(
        HttpClient http,
        GallerySource source,
        BrowseSourceCredentials credentials,
        ObservableCollection<TagSuggestion> target,
        string inputText)
    {
        try
        {
            var lastToken = GetActiveToken(inputText);
            if (lastToken is null || lastToken.Length < 2)
            {
                target.Clear();
                return false;
            }

            var tags = await FetchSuggestionsForQueryAsync(http, source, credentials, lastToken, maxResults: 30);
            target.Clear();
            foreach (var item in tags)
            {
                target.Add(item);
            }

            return target.Count > 0;
        }
        catch
        {
            target.Clear();
            return false;
        }
    }

    public static string ApplySuggestionToText(string currentText, TagSuggestion selected)
    {
        var current = currentText.Trim();
        var parts = current.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries).ToList();
        if (parts.Count == 0)
        {
            return selected.Value + " ";
        }

        parts[^1] = selected.Value;
        return string.Join(' ', parts) + " ";
    }
}
