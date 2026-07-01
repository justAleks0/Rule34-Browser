using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rule34Gallery.Core.Services;

public sealed class ForYouProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly string _filePath;

    public ForYouProfileStore(string appDataFolder)
    {
        _filePath = Path.Combine(appDataFolder, "for-you-profile.json");
    }

    public ForYouProfile? Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return null;
            }

            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<ForYouProfile>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void Save(ForYouProfile profile)
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(profile, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Best effort.
        }
    }
}

