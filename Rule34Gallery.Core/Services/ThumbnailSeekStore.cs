using System.IO;
using System.Text.Json;

namespace Rule34Gallery.Core.Services;

/// <summary>Persists per-file thumbnail seek position (0–1 fraction of duration).</summary>
public static class ThumbnailSeekStore
{
    private const double DefaultFraction = 0.5;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static readonly string StorePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Rule34GalleryApp",
        "thumbnail-seeks.json");

    private static Dictionary<string, double>? _cache;

    public static double GetFraction(string filePath)
    {
        var key = NormalizeKey(filePath);
        if (string.IsNullOrWhiteSpace(key))
        {
            return DefaultFraction;
        }

        EnsureLoaded();
        return _cache!.TryGetValue(key, out var value) ? ClampFraction(value) : DefaultFraction;
    }

    public static void SetFraction(string filePath, double fraction)
    {
        var key = NormalizeKey(filePath);
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        EnsureLoaded();
        _cache![key] = ClampFraction(fraction);
        Save();
    }

    private static void EnsureLoaded()
    {
        if (_cache is not null)
        {
            return;
        }

        try
        {
            if (File.Exists(StorePath))
            {
                var json = File.ReadAllText(StorePath);
                _cache = JsonSerializer.Deserialize<Dictionary<string, double>>(json, JsonOptions)
                         ?? new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                _cache = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            }
        }
        catch
        {
            _cache = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(StorePath)!;
            Directory.CreateDirectory(directory);
            var json = JsonSerializer.Serialize(_cache, JsonOptions);
            File.WriteAllText(StorePath, json);
        }
        catch
        {
            // Ignore persistence errors.
        }
    }

    private static string NormalizeKey(string filePath)
    {
        try
        {
            return Path.GetFullPath(filePath.Trim().Trim('"'));
        }
        catch
        {
            return filePath.Trim();
        }
    }

    private static double ClampFraction(double fraction) => Math.Clamp(fraction, 0.02, 0.98);
}
