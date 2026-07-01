using System.IO;
using System.Text;
using System.Text.Json;

namespace Rule34Gallery.Core.Services;

public static class DownloadPathBuilder
{
    private static readonly JsonSerializerOptions MetaJsonOptions = new() { WriteIndented = true };

    public sealed record DownloadTarget(string DirectoryPath, string FileName, string RelativeCategory);

    public static DownloadTarget BuildTarget(LocalLibraryDefinition library, PostItem post)
    {
        var root = LocalLibraryService.NormalizeFolderPath(library.RootFolderPath);
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new InvalidOperationException("Download library has no root folder set.");
        }

        var mediaFolder = post.MediaType == PostMediaType.Video ? "Videos" : "Images";
        var styleFolder = InferStyleFolder(post);
        var categoryFolder = InferCategoryFolder(post);
        var relativeCategory = Path.Combine(mediaFolder, styleFolder, categoryFolder);
        var directory = LocalLibraryService.NormalizeFolderPath(Path.Combine(root, relativeCategory));
        var extension = InferExtension(post);
        var fileName = BuildFileName(post, extension);

        return new DownloadTarget(directory, fileName, relativeCategory.Replace('/', Path.DirectorySeparatorChar));
    }

    public static string InferStyleFolder(PostItem post)
    {
        var tags = post.GetTagList();
        var hasAnimated = tags.Any(t =>
            t.Contains("animated", StringComparison.OrdinalIgnoreCase) ||
            t.Equals("gif", StringComparison.OrdinalIgnoreCase));

        if (post.MediaType is PostMediaType.Video or PostMediaType.Gif)
        {
            return hasAnimated || post.MediaType == PostMediaType.Gif ? "animated" : "real";
        }

        return hasAnimated ? "animated" : "real";
    }

    public static string InferCategoryFolder(PostItem post)
    {
        var map = post.GetTagCategoryMap();

        foreach (var tag in map.Where(kv => kv.Value == TagCategory.Copyright).Select(kv => kv.Key))
        {
            return SanitizeFolderSegment(tag);
        }

        foreach (var tag in map.Where(kv => kv.Value == TagCategory.Character).Select(kv => kv.Key))
        {
            return SanitizeFolderSegment(tag);
        }

        foreach (var tag in map.Where(kv => kv.Value == TagCategory.Artist).Select(kv => kv.Key))
        {
            return SanitizeFolderSegment(tag);
        }

        var general = map.Where(kv => kv.Value == TagCategory.General).Select(kv => kv.Key).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(general))
        {
            return SanitizeFolderSegment(general);
        }

        return "Uncategorized";
    }

    public static string BuildFileName(PostItem post, string extension)
    {
        var map = post.GetTagCategoryMap();
        var nameParts = new List<string> { post.Id.ToString() };

        void AddTag(TagCategory category, int max = 2)
        {
            foreach (var tag in map.Where(kv => kv.Value == category).Select(kv => kv.Key).Take(max))
            {
                nameParts.Add(SanitizeFileSegment(tag));
            }
        }

        AddTag(TagCategory.Copyright, 1);
        AddTag(TagCategory.Character, 1);
        AddTag(TagCategory.Artist, 1);

        if (nameParts.Count == 1)
        {
            var fallback = post.GetTagList().Take(3).Select(SanitizeFileSegment);
            nameParts.AddRange(fallback);
        }

        var baseName = string.Join('_', nameParts.Where(p => !string.IsNullOrWhiteSpace(p)));
        baseName = TrimToLength(baseName, 180);
        return $"{baseName}{extension}";
    }

    public static string InferExtension(PostItem post)
    {
        foreach (var url in new[] { post.FileUrl, post.SampleUrl, post.PreviewUrl })
        {
            var ext = Path.GetExtension(url);
            if (!string.IsNullOrWhiteSpace(ext) && ext.Length <= 6)
            {
                return ext;
            }
        }

        return post.MediaType switch
        {
            PostMediaType.Video => ".mp4",
            PostMediaType.Gif => ".gif",
            _ => ".jpg",
        };
    }

    public static string EnsureUniqueFilePath(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);
        if (!File.Exists(path))
        {
            return path;
        }

        var name = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        for (var i = 2; i < 10_000; i++)
        {
            var candidate = Path.Combine(directory, $"{name}_{i}{ext}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.Combine(directory, $"{name}_{Guid.NewGuid():N}{ext}");
    }

    public static string WriteSidecar(PostItem post, string mediaPath, string libraryName, string relativeCategory)
    {
        var sidecarPath = Path.ChangeExtension(mediaPath, ".meta.json");
        var meta = new
        {
            post.Id,
            post.Rating,
            post.Score,
            post.Width,
            post.Height,
            Tags = post.GetTagList(),
            post.TagInfo,
            post.FileUrl,
            post.SampleUrl,
            post.PreviewUrl,
            SitePostUrl = post.SitePostUrl,
            Library = libraryName,
            Category = relativeCategory,
            DownloadedAt = DateTimeOffset.Now,
        };

        File.WriteAllText(sidecarPath, JsonSerializer.Serialize(meta, MetaJsonOptions));
        return sidecarPath;
    }

    public static void RegisterCategoryInLibrary(LocalLibraryDefinition library, string relativeCategory, string folderPath)
    {
        var normalized = LocalLibraryService.NormalizeFolderPath(folderPath);
        if (library.Categories.Any(c =>
                LocalLibraryService.NormalizeFolderPath(c.FolderPath)
                    .Equals(normalized, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        library.Categories.Add(new LocalCategoryDefinition
        {
            Label = relativeCategory,
            FolderPath = normalized,
        });

        library.Categories = library.Categories
            .OrderBy(c => c.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string SanitizeFolderSegment(string value)
    {
        var cleaned = SanitizeFileSegment(value);
        return string.IsNullOrWhiteSpace(cleaned) ? "Uncategorized" : cleaned;
    }

    private static string SanitizeFileSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length);
        foreach (var ch in value.Trim())
        {
            sb.Append(Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch);
        }

        return sb.ToString().Trim('_', '.', ' ');
    }

    private static string TrimToLength(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength].TrimEnd('_', '.', ' ');
    }
}
