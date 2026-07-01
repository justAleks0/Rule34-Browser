using System.Net.Http.Headers;
using System.Text;

namespace Rule34Gallery.Core.Services.Browse;

internal sealed class DanbooruBrowseClient : IBrowseSourceClient
{
    private const string BaseUrl = "https://danbooru.donmai.us";

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
            var url =
                $"{BaseUrl}/tags.json?search[name_matches]={Uri.EscapeDataString(trimmed)}*&limit={maxResults}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            ApplyAuth(request, credentials);
            using var response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return BooruTagAutocomplete.Parse(json).Take(maxResults).ToList();
        }
        catch
        {
            return [];
        }
    }

    private static void ApplyAuth(HttpRequestMessage request, BrowseSourceCredentials credentials)
    {
        if (string.IsNullOrWhiteSpace(credentials.Login) || string.IsNullOrWhiteSpace(credentials.ApiKey))
        {
            return;
        }

        var token = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{credentials.Login}:{credentials.ApiKey}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
    }
}
