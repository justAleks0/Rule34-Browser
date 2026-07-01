namespace Rule34Gallery.Core.Services.Browse;

public interface IBrowseSourceClient
{
    Task<IReadOnlyList<TagSuggestion>> AutocompleteAsync(
        HttpClient http,
        string prefix,
        BrowseSourceCredentials credentials,
        int maxResults = 12,
        CancellationToken cancellationToken = default);
}
