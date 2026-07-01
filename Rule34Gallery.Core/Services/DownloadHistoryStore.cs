using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rule34Gallery.Core.Services;

public sealed class DownloadHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly string _filePath;

    public DownloadHistoryStore(string appDataFolder)
    {
        _filePath = Path.Combine(appDataFolder, "download-history.json");
    }

    public IReadOnlyList<DownloadHistoryEntry> Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return [];
            }

            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            var entries = JsonSerializer.Deserialize<List<DownloadHistoryEntry>>(json, JsonOptions);
            return entries ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void Save(IEnumerable<DownloadHistoryEntry> entries)
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var list = entries.ToList();
            var json = JsonSerializer.Serialize(list, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // History is best-effort.
        }
    }

    public void Clear()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
        catch
        {
            // Best effort.
        }
    }
}
