using System.IO;

namespace Rule34Gallery.Core.Services;
public sealed class LocalLibraryService
{
    private static readonly string[] MediaExtensions =
    [
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".avif",
        ".mp4", ".webm", ".mov", ".m4v", ".mkv",
    ];
    public static string NormalizeFolderPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }
        try
        {
            return Path.GetFullPath(path.Trim().Trim('"'));
        }
        catch
        {
            return path.Trim();
        }
    }
    public static bool FolderExists(string path)
    {
        var normalized = NormalizeFolderPath(path);
        return !string.IsNullOrWhiteSpace(normalized) && Directory.Exists(normalized);
    }
    public static bool IsMediaFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        if (!string.IsNullOrEmpty(extension) &&
            MediaExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(extension))
        {
            return false;
        }

        return LooksLikeMediaWithoutExtension(filePath);
    }

    private static bool LooksLikeMediaWithoutExtension(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            Span<byte> header = stackalloc byte[12];
            var read = stream.Read(header);
            if (read < 4)
            {
                return false;
            }

            // JPEG
            if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            {
                return true;
            }

            // PNG
            if (read >= 8 &&
                header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
            {
                return true;
            }

            // GIF
            if (read >= 4 &&
                header[0] == (byte)'G' && header[1] == (byte)'I' && header[2] == (byte)'F')
            {
                return true;
            }

            // MP4/MOV (ftyp at offset 4)
            if (read >= 8 &&
                header[4] == (byte)'f' && header[5] == (byte)'t' &&
                header[6] == (byte)'y' && header[7] == (byte)'p')
            {
                return true;
            }

            // WebM / MKV (EBML)
            if (read >= 4 &&
                header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3)
            {
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static IEnumerable<string> EnumerateMediaFiles(string folder)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pattern in new[] { "*", "*.*" })
        {
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(folder, pattern, SearchOption.AllDirectories);
            }
            catch
            {
                continue;
            }

            foreach (var file in files)
            {
                if (seen.Add(file) && IsMediaFile(file))
                {
                    yield return file;
                }
            }
        }
    }
    /// <summary>Relative path from library root, e.g. Videos\Animated\Zenless Zone Zero.</summary>
    public static string GetRelativeCategoryLabel(string rootFolderPath, string categoryFolderPath)
    {
        var root = NormalizeFolderPath(rootFolderPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var folder = NormalizeFolderPath(categoryFolderPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(folder))
        {
            return string.Empty;
        }
        if (folder.Equals(root, StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetFileName(folder);
        }
        var relative = Path.GetRelativePath(root, folder);
        return relative.Replace('/', Path.DirectorySeparatorChar);
    }
    public static string FormatCategoryDisplay(string label)
        => string.IsNullOrWhiteSpace(label)
            ? string.Empty
            : label.Replace("\\", " \u203a ").Replace("/", " \u203a ");
    public static string GetTopSegment(string categoryLabel)
    {
        if (string.IsNullOrWhiteSpace(categoryLabel))
        {
            return string.Empty;
        }
        var parts = categoryLabel.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : categoryLabel;
    }
    public static bool CategoryUnderTopSegment(string categoryLabel, string topSegment)
    {
        if (string.IsNullOrWhiteSpace(topSegment) || topSegment.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (categoryLabel.Equals(topSegment, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return categoryLabel.StartsWith(topSegment + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
               categoryLabel.StartsWith(topSegment + '/', StringComparison.OrdinalIgnoreCase);
    }
    /// <summary>
    /// Finds folders that directly contain media files, at any depth under the root
    /// (e.g. Secret Stash\Videos\Animated\Zenless Zone Zero).
    /// </summary>
    public static List<LocalCategoryDefinition> DiscoverMediaCategoryFolders(string rootFolderPath)
    {
        var root = NormalizeFolderPath(rootFolderPath);
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            return [];
        }
        var categories = new List<LocalCategoryDefinition>();
        var stack = new Stack<string>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            try
            {
                var hasDirectMedia = false;
                foreach (var file in Directory.EnumerateFiles(current))
                {
                    if (IsMediaFile(file))
                    {
                        hasDirectMedia = true;
                        break;
                    }
                }
                if (hasDirectMedia)
                {
                    categories.Add(new LocalCategoryDefinition
                    {
                        Label = GetRelativeCategoryLabel(root, current),
                        FolderPath = NormalizeFolderPath(current),
                    });
                }
                foreach (var directory in Directory.EnumerateDirectories(current))
                {
                    stack.Push(directory);
                }
            }
            catch
            {
                // Skip unreadable folders.
            }
        }
        return categories
            .OrderBy(c => c.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
    public static int ApplyDiscoveredCategories(LocalLibraryDefinition library)
    {
        var discovered = DiscoverMediaCategoryFolders(library.RootFolderPath);
        if (discovered.Count == 0)
        {
            return 0;
        }
        var existingLabels = library.Categories
            .Where(c => !string.IsNullOrWhiteSpace(c.FolderPath))
            .ToDictionary(
                c => NormalizeFolderPath(c.FolderPath),
                c => c.Label,
                StringComparer.OrdinalIgnoreCase);
        foreach (var category in discovered)
        {
            if (existingLabels.TryGetValue(category.FolderPath, out var label) &&
                !string.IsNullOrWhiteSpace(label))
            {
                category.Label = label;
            }
        }
        library.Categories = discovered;
        return discovered.Count;
    }
    public static IReadOnlyList<PostItem> ScanCategory(
        LocalCategoryDefinition category,
        string libraryName,
        CancellationToken cancellationToken = default)
    {
        var folder = NormalizeFolderPath(category.FolderPath);
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return [];
        }
        var label = string.IsNullOrWhiteSpace(category.Label)
            ? Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            : category.Label.Trim();
        var posts = new List<PostItem>();
        IEnumerable<string> files;
        try
        {
            files = EnumerateMediaFiles(folder);
        }
        catch
        {
            return [];
        }

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            posts.Add(CreatePostFromFile(file, label, libraryName));
        }
        return posts
            .OrderBy(p => p.LocalCategory, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.FileUrl, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<PostItem> FilterBlockedPosts(
        IEnumerable<PostItem> posts,
        UserSettings settings)
        => TagBlockFilter.Apply(posts, settings).ToList();
    public static PostItem CreatePostFromFile(string filePath, string categoryLabel, string libraryName)
    {
        var fullPath = NormalizeFolderPath(filePath);
        if (File.Exists(fullPath))
        {
            fullPath = Path.GetFullPath(fullPath);
        }

        var post = new PostItem
        {
            Id = fullPath.GetHashCode(),
            FileUrl = fullPath,
            SampleUrl = fullPath,
            PreviewUrl = PostMedia.IsRasterImageUrl(fullPath) ? fullPath : string.Empty,
            Tags = $"local {categoryLabel}",
            Rating = "local",
            Score = 0,
            Owner = libraryName,
            IsLocal = true,
            LocalCategory = categoryLabel,
            LocalLibraryName = libraryName,
        };

        if (LocalMediaSidecar.TryRead(fullPath, out var metadata))
        {
            LocalMediaSidecar.ApplyToPost(post, metadata, fullPath, categoryLabel, libraryName);
        }

        return post;
    }
    public static List<PostItem> ScanLibrary(
        LocalLibraryDefinition library,
        string? topSegmentFilter,
        string? categoryLabelFilter,
        CancellationToken cancellationToken = default)
    {
        if (library.Categories.Count == 0)
        {
            return [];
        }

        var categories = library.Categories.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(topSegmentFilter) &&
            !topSegmentFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            categories = categories.Where(c => CategoryUnderTopSegment(c.Label, topSegmentFilter));
        }

        if (!string.IsNullOrWhiteSpace(categoryLabelFilter) &&
            !categoryLabelFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            categories = categories.Where(c =>
                c.Label.Equals(categoryLabelFilter, StringComparison.OrdinalIgnoreCase));
        }

        var posts = new List<PostItem>();
        foreach (var category in categories)
        {
            posts.AddRange(ScanCategory(category, library.Name, cancellationToken));
        }

        return posts;
    }

    public static List<PostItem> ScanLibrary(
        LocalLibraryDefinition library,
        string? topSegmentFilter,
        string? categoryLabelFilter,
        UserSettings settings,
        CancellationToken cancellationToken = default)
        => FilterBlockedPosts(
            ScanLibrary(library, topSegmentFilter, categoryLabelFilter, cancellationToken),
            settings).ToList();

    public static string DescribeActiveFilter(string? topSegmentFilter, string? categoryLabelFilter)
    {
        var top = string.IsNullOrWhiteSpace(topSegmentFilter) ? "All" : topSegmentFilter;
        var leaf = string.IsNullOrWhiteSpace(categoryLabelFilter) ? "All" : categoryLabelFilter;
        if (leaf.Equals("All", StringComparison.OrdinalIgnoreCase) &&
            top.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            return "All";
        }
        if (leaf.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            return top;
        }
        return FormatCategoryDisplay(leaf);
    }
}
