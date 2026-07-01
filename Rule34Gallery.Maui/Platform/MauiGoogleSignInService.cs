namespace Rule34Gallery.Maui.Platform;

internal sealed class MauiGoogleSignInService : IGoogleSignInService
{
    private const string RedirectUri = "rule34gallery://auth";

    public async Task<string> SignInAsync(string clientId, string? clientSecret, CancellationToken cancellationToken = default)
    {
        var authUrl =
            "https://accounts.google.com/o/oauth2/v2/auth?" +
            $"client_id={Uri.EscapeDataString(clientId)}&" +
            $"redirect_uri={Uri.EscapeDataString(RedirectUri)}&" +
            "response_type=code&" +
            "scope=openid%20email%20profile&" +
            "prompt=select_account";

        var callback = await WebAuthenticator.Default.AuthenticateAsync(
            new Uri(authUrl),
            new Uri(RedirectUri)).ConfigureAwait(false);

        if (!callback.Properties.TryGetValue("code", out var code) || string.IsNullOrWhiteSpace(code))
        {
            throw new InvalidOperationException("Google sign-in was cancelled or failed.");
        }

        using var http = new HttpClient();
        var fields = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = clientId,
            ["redirect_uri"] = RedirectUri,
            ["grant_type"] = "authorization_code",
        };
        if (!string.IsNullOrWhiteSpace(clientSecret))
        {
            fields["client_secret"] = clientSecret;
        }

        var response = await http.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(fields),
            cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(body);
        }

        using var doc = System.Text.Json.JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("id_token", out var idToken))
        {
            return idToken.GetString() ?? throw new InvalidOperationException("Missing id_token.");
        }

        throw new InvalidOperationException("Google token response missing id_token.");
    }

    public string? BuildClientSecretHelp() =>
        "For Android, configure an OAuth client with redirect URI rule34gallery://auth in Google Cloud Console.";
}
