using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rule34Gallery.Core.Firebase;

public sealed class FirebaseConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("projectId")]
    public string ProjectId { get; set; } = string.Empty;

    [JsonPropertyName("googleClientId")]
    public string GoogleClientId { get; set; } = string.Empty;

    [JsonPropertyName("googleClientSecret")]
    public string GoogleClientSecret { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ProjectId);

    public bool IsGoogleConfigured => IsConfigured && !string.IsNullOrWhiteSpace(GoogleClientId);

    public bool IsGoogleReady => IsGoogleConfigured && !string.IsNullOrWhiteSpace(GoogleClientSecret);

    public static FirebaseConfig? Load()
    {
        foreach (var path in GetCandidatePaths())
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                var json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<FirebaseConfig>(json, JsonOptions);
                if (config?.IsConfigured == true)
                {
                    return config;
                }
            }
            catch
            {
                // Try next path.
            }
        }

        return null;
    }

    private static IEnumerable<string> GetCandidatePaths()
    {
        var appDir = AppContext.BaseDirectory;
        yield return Path.Combine(appDir, "firebase-config.json");

        var currentDir = Directory.GetCurrentDirectory();
        if (!string.Equals(currentDir, appDir, StringComparison.OrdinalIgnoreCase))
        {
            yield return Path.Combine(currentDir, "firebase-config.json");
        }

        var dir = new DirectoryInfo(appDir);
        for (var i = 0; i < 4 && dir.Parent is not null; i++)
        {
            dir = dir.Parent;
            yield return Path.Combine(dir.FullName, "firebase-config.json");
        }

        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Rule34GalleryApp",
            "firebase-config.json");

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            yield return Path.Combine(localAppData, "firebase-config.json");
            yield return Path.Combine(localAppData, "Rule34Gallery", "firebase-config.json");
        }
    }
}
