using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Rule34GalleryApp.Firebase;

internal static class GoogleSignInService
{
    private const int RedirectPort = 53123;

    /// <summary>Exact URI to register in Google Cloud Console → Credentials → your OAuth client.</summary>
    public static string RedirectUri => $"http://127.0.0.1:{RedirectPort}/";

    private static readonly HttpClient Http = new();

    public static async Task<string> SignInAsync(
        string clientId,
        string? clientSecret,
        CancellationToken cancellationToken = default)
    {
        var redirectUri = RedirectUri;
        var codeVerifier = CreateCodeVerifier();
        var codeChallenge = CreateCodeChallenge(codeVerifier);

        using var listener = new HttpListener();
        listener.Prefixes.Add(redirectUri);
        listener.Start();

        try
        {
            var authUrl =
                "https://accounts.google.com/o/oauth2/v2/auth?" +
                $"client_id={Uri.EscapeDataString(clientId)}&" +
                $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                "response_type=code&" +
                "scope=openid%20email%20profile&" +
                $"code_challenge={Uri.EscapeDataString(codeChallenge)}&" +
                "code_challenge_method=S256&" +
                "prompt=select_account";

            Process.Start(new ProcessStartInfo
            {
                FileName = authUrl,
                UseShellExecute = true,
            });

            var context = await listener.GetContextAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
            var code = ParseAuthorizationCode(context.Request.Url);
            await WriteSuccessResponseAsync(context.Response).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(code))
            {
                throw new InvalidOperationException("Google sign-in was cancelled or failed.");
            }

            return await ExchangeCodeForIdTokenAsync(clientId, clientSecret, redirectUri, code, codeVerifier, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            listener.Stop();
            listener.Close();
        }
    }

    private static async Task<string> ExchangeCodeForIdTokenAsync(
        string clientId,
        string? clientSecret,
        string redirectUri,
        string code,
        string codeVerifier,
        CancellationToken cancellationToken)
    {
        var fields = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
            ["code_verifier"] = codeVerifier,
        };

        if (!string.IsNullOrWhiteSpace(clientSecret))
        {
            fields["client_secret"] = clientSecret;
        }

        using var content = new FormUrlEncodedContent(fields);

        var response = await Http.PostAsync("https://oauth2.googleapis.com/token", content, cancellationToken)
            .ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(ParseGoogleError(body));
        }

        var token = System.Text.Json.JsonSerializer.Deserialize<GoogleTokenResponse>(body)
            ?? throw new InvalidOperationException("Empty Google token response.");

        if (string.IsNullOrWhiteSpace(token.IdToken))
        {
            throw new InvalidOperationException("Google did not return an ID token.");
        }

        return token.IdToken;
    }

    private static string? ParseAuthorizationCode(Uri? uri)
    {
        if (uri is null)
        {
            return null;
        }

        var query = uri.Query.TrimStart('?');
        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 2 && pair[0] == "code")
            {
                return Uri.UnescapeDataString(pair[1]);
            }
        }

        return null;
    }

    private static async Task WriteSuccessResponseAsync(HttpListenerResponse response)
    {
        const string html =
            """
            <html><body style="font-family:Segoe UI;background:#111;color:#eee;text-align:center;padding:48px;">
            <h2>Signed in successfully</h2>
            <p>You can close this tab and return to the app.</p>
            </body></html>
            """;
        var buffer = Encoding.UTF8.GetBytes(html);
        response.ContentType = "text/html";
        response.ContentLength64 = buffer.Length;
        response.StatusCode = 200;
        await response.OutputStream.WriteAsync(buffer).ConfigureAwait(false);
        response.OutputStream.Close();
    }

    private static string CreateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    private static string CreateCodeChallenge(string verifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string ParseGoogleError(string body)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error_description", out var description))
            {
                var text = description.GetString() ?? body;
                if (text.Contains("redirect_uri", StringComparison.OrdinalIgnoreCase))
                {
                    return BuildRedirectUriHelp();
                }

                if (text.Contains("client_secret", StringComparison.OrdinalIgnoreCase))
                {
                    return BuildClientSecretHelp();
                }

                return text;
            }

            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                var code = error.GetString() ?? string.Empty;
                if (code.Contains("redirect_uri", StringComparison.OrdinalIgnoreCase))
                {
                    return BuildRedirectUriHelp();
                }

                return code;
            }
        }
        catch
        {
            // Fall through.
        }

        if (body.Contains("redirect_uri", StringComparison.OrdinalIgnoreCase))
        {
            return BuildRedirectUriHelp();
        }

        if (body.Contains("client_secret", StringComparison.OrdinalIgnoreCase))
        {
            return BuildClientSecretHelp();
        }

        return string.IsNullOrWhiteSpace(body) ? "Google sign-in failed." : body;
    }

    public static string BuildClientSecretHelp() =>
        "Add googleClientSecret to firebase-config.json. " +
        "Google Cloud Console → Credentials → your Web client → copy the Client secret.";

    public static string BuildRedirectUriHelp() =>
        "Redirect URI mismatch. In Google Cloud Console → APIs & Services → Credentials, " +
        "open your Web client ID (the one in firebase-config.json) and add this Authorized redirect URI:\n\n" +
        RedirectUri +
        "\n\nSave, wait ~1 minute, then try again.";

    private sealed class GoogleTokenResponse
    {
        [JsonPropertyName("id_token")]
        public string IdToken { get; set; } = string.Empty;
    }
}
