using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Rule34Gallery.Core.Abstractions;

namespace Rule34Gallery.Core.Firebase;

public sealed class FirebaseAuthService
{
    private readonly string _sessionPath;
    private readonly FirebaseConfig _config;
    private readonly ISecureCredentialStore _secureStore;
    private readonly HttpClient _http = new();

    public FirebaseAuthService(FirebaseConfig config, ISecureCredentialStore secureStore, string appDataFolder)
    {
        _config = config;
        _secureStore = secureStore;
        _sessionPath = Path.Combine(appDataFolder, "firebase-session.dat");
    }

    public FirebaseUser? CurrentUser { get; private set; }

    public event EventHandler? AuthStateChanged;

    public async Task<bool> TryRestoreSessionAsync()
    {
        var session = LoadSession();
        if (session is null || string.IsNullOrWhiteSpace(session.RefreshToken))
        {
            return false;
        }

        try
        {
            var refreshed = await RefreshAsync(session.RefreshToken).ConfigureAwait(false);
            ApplySession(refreshed, session.Email);
            return true;
        }
        catch
        {
            ClearSession();
            return false;
        }
    }

    public async Task SignInAsync(string email, string password)
    {
        var url =
            $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={Uri.EscapeDataString(_config.ApiKey)}";
        var payload = new { email, password, returnSecureToken = true };
        var response = await _http.PostAsJsonAsync(url, payload).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>().ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empty auth response.");

        ApplySession(result, email);
    }

    public async Task SignUpAsync(string email, string password)
    {
        var url =
            $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={Uri.EscapeDataString(_config.ApiKey)}";
        var payload = new { email, password, returnSecureToken = true };
        var response = await _http.PostAsJsonAsync(url, payload).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>().ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empty auth response.");

        ApplySession(result, email);
    }

    public async Task SignInWithGoogleAsync(string googleIdToken)
    {
        var url =
            $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key={Uri.EscapeDataString(_config.ApiKey)}";
        var postBody = $"id_token={Uri.EscapeDataString(googleIdToken)}&providerId=google.com";
        var payload = new
        {
            postBody,
            requestUri = "http://localhost",
            returnIdpCredential = true,
            returnSecureToken = true,
        };

        var response = await _http.PostAsJsonAsync(url, payload).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>().ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empty auth response.");

        var email = string.IsNullOrWhiteSpace(result.Email) ? "Google user" : result.Email;
        ApplySession(result, email);
    }

    public void SignOut()
    {
        CurrentUser = null;
        ClearSession();
        AuthStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task<string> GetIdTokenAsync()
    {
        if (CurrentUser is null)
        {
            throw new InvalidOperationException("Not signed in.");
        }

        if (DateTimeOffset.UtcNow < CurrentUser.TokenExpiresAt.AddMinutes(-2))
        {
            return CurrentUser.IdToken;
        }

        var refreshed = await RefreshAsync(CurrentUser.RefreshToken).ConfigureAwait(false);
        ApplySession(refreshed, CurrentUser.Email);
        return CurrentUser.IdToken;
    }

    private async Task<AuthResponse> RefreshAsync(string refreshToken)
    {
        var url =
            $"https://securetoken.googleapis.com/v1/token?key={Uri.EscapeDataString(_config.ApiKey)}";
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
        });

        var response = await _http.PostAsync(url, content).ConfigureAwait(false);
        await EnsureSuccessAsync(response).ConfigureAwait(false);

        var token = await response.Content.ReadFromJsonAsync<RefreshResponse>().ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empty refresh response.");

        return new AuthResponse
        {
            IdToken = token.IdToken,
            RefreshToken = token.RefreshToken,
            ExpiresIn = token.ExpiresIn,
            LocalId = token.UserId,
        };
    }

    private void ApplySession(AuthResponse response, string email)
    {
        var expiresIn = int.TryParse(response.ExpiresIn, out var seconds) ? seconds : 3600;
        CurrentUser = new FirebaseUser
        {
            UserId = response.LocalId,
            Email = email,
            IdToken = response.IdToken,
            RefreshToken = response.RefreshToken,
            TokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn),
        };

        SaveSession(new StoredSession
        {
            Email = email,
            RefreshToken = response.RefreshToken,
            UserId = response.LocalId,
        });

        AuthStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        throw new InvalidOperationException(ExtractError(body));
    }

    private static string ExtractError(string body)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? body;
            }
        }
        catch
        {
            // Fall through.
        }

        return string.IsNullOrWhiteSpace(body) ? "Authentication failed." : body;
    }

    private void SaveSession(StoredSession session)
    {
        var directory = Path.GetDirectoryName(_sessionPath)!;
        Directory.CreateDirectory(directory);

        var json = System.Text.Json.JsonSerializer.Serialize(session);
        File.WriteAllText(_sessionPath, _secureStore.Protect(json));
    }

    private StoredSession? LoadSession()
    {
        try
        {
            if (!File.Exists(_sessionPath))
            {
                return null;
            }

            var protectedText = File.ReadAllText(_sessionPath);
            var json = _secureStore.Unprotect(protectedText);
            return System.Text.Json.JsonSerializer.Deserialize<StoredSession>(json);
        }
        catch
        {
            return null;
        }
    }

    private void ClearSession()
    {
        try
        {
            if (File.Exists(_sessionPath))
            {
                File.Delete(_sessionPath);
            }
        }
        catch
        {
            // Ignore.
        }
    }

    private sealed class AuthResponse
    {
        [JsonPropertyName("idToken")]
        public string IdToken { get; set; } = string.Empty;

        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("expiresIn")]
        public string ExpiresIn { get; set; } = "3600";

        [JsonPropertyName("localId")]
        public string LocalId { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
    }

    private sealed class RefreshResponse
    {
        [JsonPropertyName("id_token")]
        public string IdToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public string ExpiresIn { get; set; } = "3600";

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;
    }

    private sealed class StoredSession
    {
        public string Email { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;
    }
}

public sealed class FirebaseUser
{
    public string UserId { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string IdToken { get; init; } = string.Empty;

    public string RefreshToken { get; init; } = string.Empty;

    public DateTimeOffset TokenExpiresAt { get; init; }
}
