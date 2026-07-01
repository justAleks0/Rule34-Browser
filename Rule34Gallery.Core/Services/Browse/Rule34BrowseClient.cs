namespace Rule34Gallery.Core.Services.Browse;

internal sealed class Rule34BrowseClient : IBrowseSourceClient
{
    public async Task<IReadOnlyList<TagSuggestion>> AutocompleteAsync(
        HttpClient http,
        string prefix,
        BrowseSourceCredentials credentials,
        int maxResults = 12,
        CancellationToken cancellationToken = default)
    {
        var trimmed = prefix.Trim();
        if (trimmed.Length < 2)
        {
            return [];
        }

        try
        {
            var url = $"https://api.rule34.xxx/autocomplete.php?q={Uri.EscapeDataString(trimmed)}";
            var json = await http.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
            return Rule34Api.ParseAutocompleteSuggestions(json).Take(maxResults).ToList();
        }
        catch
        {
            return [];
        }
    }
}
