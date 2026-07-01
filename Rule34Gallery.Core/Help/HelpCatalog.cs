using System.Reflection;
using System.Text.Json;

namespace Rule34Gallery.Core.Help;

public static class HelpCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static HelpTopicsFile? _cache;

    public static IReadOnlyList<HelpTopic> GetTopics(HelpPlatform platform)
    {
        var file = Load();
        var key = PlatformKey(platform);
        return file.Topics
            .Where(t => t.Platforms.Any(p => p.Equals(key, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public static IReadOnlyList<string> GetBullets(HelpTopic topic, HelpPlatform platform)
    {
        var bullets = new List<string>(topic.Bullets);
        if (topic.PlatformBullets is not null &&
            topic.PlatformBullets.TryGetValue(PlatformKey(platform), out var extra) &&
            extra is { Count: > 0 })
        {
            bullets.AddRange(extra);
        }

        return bullets;
    }

    public static string? GetNavigateTarget(HelpTopic topic, HelpPlatform platform)
    {
        if (topic.NavigateTo is null)
        {
            return null;
        }

        return topic.NavigateTo.TryGetValue(PlatformKey(platform), out var target) ? target : null;
    }

    private static HelpTopicsFile Load()
    {
        if (_cache is not null)
        {
            return _cache;
        }

        var assembly = typeof(HelpCatalog).Assembly;
        const string resourceName = "Rule34Gallery.Core.Content.help-topics.json";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        _cache = JsonSerializer.Deserialize<HelpTopicsFile>(json, JsonOptions)
            ?? new HelpTopicsFile();
        return _cache;
    }

    private static string PlatformKey(HelpPlatform platform) => platform switch
    {
        HelpPlatform.Desktop => "desktop",
        HelpPlatform.Android => "android",
        HelpPlatform.Maui => "maui",
        _ => "desktop",
    };
}
