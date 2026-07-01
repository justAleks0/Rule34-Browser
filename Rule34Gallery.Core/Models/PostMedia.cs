using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Rule34Gallery.Core;

public enum PostMediaType
{
    Image,
    Gif,
    Video,
}

public static class PostMedia
{
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4",
        ".webm",
        ".mov",
        ".m4v",
        ".mkv",
    };

    private static readonly HashSet<string> GifExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".gif",
    };

    private static readonly HashSet<string> RasterExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".avif",
    };

    public static PostMediaType DetectType(string? fileUrl, string? sampleUrl, string? tags)
    {
        var fileType = DetectTypeFromUrl(fileUrl);
        if (fileType != PostMediaType.Image)
        {
            return fileType;
        }

        var sampleType = DetectTypeFromUrl(sampleUrl);
        if (sampleType != PostMediaType.Image)
        {
            return sampleType;
        }

        if (ContainsAnimatedTag(tags) && fileUrl?.EndsWith(".webm", StringComparison.OrdinalIgnoreCase) == true)
        {
            return PostMediaType.Gif;
        }

        return PostMediaType.Image;
    }

    public static PostMediaType DetectTypeFromUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return PostMediaType.Image;
        }

        var extension = Path.GetExtension(url);
        if (VideoExtensions.Contains(extension))
        {
            return PostMediaType.Video;
        }

        if (GifExtensions.Contains(extension))
        {
            return PostMediaType.Gif;
        }

        if (string.IsNullOrEmpty(extension) && TryGetLocalFilePath(url, out var localPath))
        {
            return DetectTypeFromFileHeader(localPath);
        }

        return PostMediaType.Image;
    }

    private static bool TryGetLocalFilePath(string url, out string localPath)
    {
        localPath = string.Empty;
        if (!Path.IsPathRooted(url) || !File.Exists(url))
        {
            return false;
        }

        localPath = url;
        return true;
    }

    private static PostMediaType DetectTypeFromFileHeader(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            Span<byte> header = stackalloc byte[12];
            var read = stream.Read(header);
            if (read >= 8 &&
                header[4] == (byte)'f' && header[5] == (byte)'t' &&
                header[6] == (byte)'y' && header[7] == (byte)'p')
            {
                return PostMediaType.Video;
            }

            if (read >= 4 &&
                header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3)
            {
                return PostMediaType.Video;
            }

            if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            {
                return PostMediaType.Image;
            }

            if (read >= 4 &&
                header[0] == (byte)'G' && header[1] == (byte)'I' && header[2] == (byte)'F')
            {
                return PostMediaType.Gif;
            }
        }
        catch
        {
            // Fall through.
        }

        return PostMediaType.Image;
    }

    public static bool IsPlayableMedia(PostMediaType type) => type is PostMediaType.Video or PostMediaType.Gif;

    public static bool IsRasterImageUrl([NotNullWhen(true)] string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        return DetectTypeFromUrl(url) == PostMediaType.Image;
    }

    public static bool IsRasterImageFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }

        var extension = Path.GetExtension(filePath);
        if (!string.IsNullOrEmpty(extension))
        {
            return RasterExtensions.Contains(extension);
        }

        return DetectTypeFromFileHeader(filePath) == PostMediaType.Image;
    }

    public static bool ShouldUseShellThumbnail(string filePath)
        => File.Exists(filePath) && !IsRasterImageFile(filePath);

    public static string GetMediaBadge(PostMediaType type) => type switch
    {
        PostMediaType.Video => "▶ VIDEO",
        PostMediaType.Gif => "GIF",
        _ => string.Empty,
    };

    private static bool ContainsAnimatedTag(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return false;
        }

        return tags.Contains("animated", StringComparison.OrdinalIgnoreCase);
    }
}
