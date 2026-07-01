using System.IO;
using System.Text.Json;

namespace Rule34Gallery.Core.Services;

/// <summary>Reads <c>.meta.json</c> sidecars written by <see cref="DownloadPathBuilder.WriteSidecar"/>.</summary>
public static class LocalMediaSidecar
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public sealed class Metadata
    {
        public int Id { get; init; }
        public string Rating { get; init; } = string.Empty;
        public int Score { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }
        public IReadOnlyList<string> Tags { get; init; } = [];
        public List<PostTagInfo>? TagInfo { get; init; }
        public string PreviewUrl { get; init; } = string.Empty;
        public string SampleUrl { get; init; } = string.Empty;
        public string RemoteFileUrl { get; init; } = string.Empty;
        public string LibraryName { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
    }

    public static string GetSidecarPath(string mediaPath)
        => Path.ChangeExtension(mediaPath, ".meta.json");

    public static bool TryRead(string mediaPath, out Metadata metadata)
    {
        metadata = new Metadata();
        var sidecarPath = GetSidecarPath(mediaPath);
        if (!File.Exists(sidecarPath))
        {
            return false;
        }

        try
        {
            var json = File.ReadAllText(sidecarPath);
            var dto = JsonSerializer.Deserialize<SidecarDto>(json, JsonOptions);
            if (dto is null)
            {
                return false;
            }

            metadata = new Metadata
            {
                Id = dto.Id,
                Rating = dto.Rating?.Trim() ?? string.Empty,
                Score = dto.Score,
                Width = dto.Width,
                Height = dto.Height,
                Tags = ParseTags(dto.Tags),
                TagInfo = dto.TagInfo is { Count: > 0 } ? dto.TagInfo : null,
                PreviewUrl = dto.PreviewUrl?.Trim() ?? string.Empty,
                SampleUrl = dto.SampleUrl?.Trim() ?? string.Empty,
                RemoteFileUrl = dto.FileUrl?.Trim() ?? string.Empty,
                LibraryName = dto.Library?.Trim() ?? string.Empty,
                Category = dto.Category?.Trim() ?? string.Empty,
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void ApplyToPost(
        PostItem post,
        Metadata metadata,
        string localFilePath,
        string fallbackCategoryLabel,
        string fallbackLibraryName)
    {
        var localPath = LocalLibraryService.NormalizeFolderPath(localFilePath);
        if (File.Exists(localPath))
        {
            localPath = Path.GetFullPath(localPath);
        }

        post.FileUrl = localPath;
        post.SampleUrl = localPath;

        if (metadata.Id > 0)
        {
            post.Id = metadata.Id;
        }

        if (!string.IsNullOrWhiteSpace(metadata.Rating))
        {
            post.Rating = metadata.Rating;
        }

        post.Score = metadata.Score;
        post.Width = metadata.Width;
        post.Height = metadata.Height;

        if (metadata.Tags.Count > 0)
        {
            post.Tags = string.Join(' ', metadata.Tags);
        }

        if (metadata.TagInfo is { Count: > 0 })
        {
            post.TagInfo = metadata.TagInfo;
        }

        post.PreviewUrl = ChoosePreviewUrl(localPath, metadata);

        if (!string.IsNullOrWhiteSpace(metadata.LibraryName))
        {
            post.LocalLibraryName = metadata.LibraryName;
            post.Owner = metadata.LibraryName;
        }
        else if (!string.IsNullOrWhiteSpace(fallbackLibraryName))
        {
            post.LocalLibraryName = fallbackLibraryName;
            post.Owner = fallbackLibraryName;
        }

        if (!string.IsNullOrWhiteSpace(metadata.Category))
        {
            post.LocalCategory = metadata.Category;
        }
        else if (!string.IsNullOrWhiteSpace(fallbackCategoryLabel))
        {
            post.LocalCategory = fallbackCategoryLabel;
        }
    }

    private static string ChoosePreviewUrl(string localPath, Metadata metadata)
    {
        if (IsRemoteUrl(metadata.PreviewUrl))
        {
            return metadata.PreviewUrl;
        }

        if (IsRemoteUrl(metadata.SampleUrl) && PostMedia.IsRasterImageUrl(metadata.SampleUrl))
        {
            return metadata.SampleUrl;
        }

        if (PostMedia.IsRasterImageUrl(localPath))
        {
            return localPath;
        }

        return string.Empty;
    }

    private static bool IsRemoteUrl(string? url)
        => !string.IsNullOrWhiteSpace(url) &&
           (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<string> ParseTags(JsonElement? element)
    {
        if (element is null || element.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        var el = element.Value;
        if (el.ValueKind == JsonValueKind.Array)
        {
            return el.EnumerateArray()
                .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : item.ToString())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (el.ValueKind == JsonValueKind.String)
        {
            var text = el.GetString();
            return string.IsNullOrWhiteSpace(text)
                ? []
                : text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
        }

        return [];
    }

    private sealed class SidecarDto
    {
        public int Id { get; set; }
        public string? Rating { get; set; }
        public int Score { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public JsonElement? Tags { get; set; }
        public List<PostTagInfo>? TagInfo { get; set; }
        public string? PreviewUrl { get; set; }
        public string? SampleUrl { get; set; }
        public string? FileUrl { get; set; }
        public string? Library { get; set; }
        public string? Category { get; set; }
    }
}
