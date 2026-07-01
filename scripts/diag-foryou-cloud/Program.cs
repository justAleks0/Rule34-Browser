using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Rule34Gallery.Core;
using Rule34Gallery.Core.Abstractions;
using Rule34Gallery.Core.Firebase;
using Rule34Gallery.Core.Services;

var appData = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "Rule34GalleryApp");

var profile = new ForYouProfileStore(appData).Load();
if (profile is null)
{
    Console.WriteLine("No local For You profile.");
    return 1;
}

var cloud = profile.ToCloudProfile();
var jsonOpts = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
};
var json = JsonSerializer.Serialize(cloud, jsonOpts);

Console.WriteLine($"Local topics: {profile.Topics.Count}");
Console.WriteLine($"Local search lines: {profile.SearchLines.Count}");
Console.WriteLine($"Local activity: {profile.RecentActivity.Count}");
Console.WriteLine($"Cloud topics: {cloud.Topics.Count}");
Console.WriteLine($"Cloud activities: {cloud.Activities.Count}");
Console.WriteLine($"Cloud JSON bytes: {json.Length}");
Console.WriteLine($"CloudSyncEnabled (profile): {profile.CloudSyncEnabled}");

bool ReadBool(JsonElement root, params string[] names)
{
    foreach (var name in names)
    {
        if (root.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        if (root.TryGetProperty(name, out el) && el.ValueKind == JsonValueKind.False)
        {
            return false;
        }
    }

    return false;
}

var settingsPath = Path.Combine(appData, "settings.json");
if (File.Exists(settingsPath))
{
    var settingsJson = File.ReadAllText(settingsPath);
    using var doc = JsonDocument.Parse(settingsJson);
    var root = doc.RootElement;
    Console.WriteLine($"ForYouCloudSync (settings): {ReadBool(root, "ForYouCloudSync", "forYouCloudSync")}");
}

var config =
    FirebaseConfig.Load()
    ?? LoadConfigFrom(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Programs",
        "Rule34Gallery",
        "firebase-config.json"));
if (config is null || !config.IsConfigured)
{
    Console.WriteLine("Firebase not configured; skipping Firestore read.");
    return 0;
}

var secure = new DpapiSecureCredentialStore();
var auth = new FirebaseAuthService(config, secure, appData);
if (!await auth.TryRestoreSessionAsync())
{
    Console.WriteLine("Not signed in to Firebase.");
    return 0;
}

Console.WriteLine($"Signed in as {auth.CurrentUser?.Email} ({auth.CurrentUser?.UserId})");

var firestore = new FirestoreService(config, auth);
var rawPath = Path.Combine(appData, "for-you-cloud-raw.json");
await DumpRemoteProfileJson(config, auth, rawPath);

var remote = await firestore.GetForYouProfileAsync();
if (remote is null)
{
    Console.WriteLine("Remote forYouProfile: document missing or empty profileJson.");
    return 0;
}

Console.WriteLine($"Raw dump: {rawPath}");
Console.WriteLine($"Remote topics: {remote.Topics.Count}");
Console.WriteLine($"Remote activities: {remote.Activities.Count}");
Console.WriteLine($"Remote search lines: {remote.SearchLines.Count}");
Console.WriteLine($"Remote enabled: {remote.Enabled}");
Console.WriteLine($"Remote updatedAtUnix: {remote.UpdatedAtUnix}");
return 0;

static async Task DumpRemoteProfileJson(FirebaseConfig config, FirebaseAuthService auth, string outPath)
{
    var token = await auth.GetIdTokenAsync();
    var userId = auth.CurrentUser?.UserId
        ?? throw new InvalidOperationException("No user id.");
    var url =
        $"https://firestore.googleapis.com/v1/projects/{config.ProjectId}/databases/(default)/documents/users/{userId}/private/forYouProfile";
    using var http = new HttpClient();
    using var request = new HttpRequestMessage(HttpMethod.Get, url);
    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    using var response = await http.SendAsync(request);
    response.EnsureSuccessStatusCode();
    var body = await response.Content.ReadAsStringAsync();
    await File.WriteAllTextAsync(outPath, body);
}

static FirebaseConfig? LoadConfigFrom(string path)
{
    if (!File.Exists(path))
    {
        return null;
    }

    try
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<FirebaseConfig>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
    catch
    {
        return null;
    }
}

sealed class DpapiSecureCredentialStore : ISecureCredentialStore
{
    public string Protect(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(value);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    public string Unprotect(string protectedValue)
    {
        if (string.IsNullOrEmpty(protectedValue))
        {
            return string.Empty;
        }

        try
        {
            var protectedBytes = Convert.FromBase64String(protectedValue);
            var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return string.Empty;
        }
    }
}

