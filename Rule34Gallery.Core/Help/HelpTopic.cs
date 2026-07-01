using System.Text.Json.Serialization;

namespace Rule34Gallery.Core.Help;

public sealed class HelpTopic
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public List<string> Platforms { get; set; } = [];

    public string Summary { get; set; } = string.Empty;

    public List<string> Bullets { get; set; } = [];

    [JsonPropertyName("platformBullets")]
    public Dictionary<string, List<string>>? PlatformBullets { get; set; }

    public HelpTopicLink? Link { get; set; }

    [JsonPropertyName("navigateTo")]
    public Dictionary<string, string>? NavigateTo { get; set; }
}

public sealed class HelpTopicLink
{
    public string Url { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
}

public sealed class HelpTopicsFile
{
    public List<HelpTopic> Topics { get; set; } = [];
}
