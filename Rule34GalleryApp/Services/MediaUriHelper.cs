using System.IO;

namespace Rule34GalleryApp.Services;

internal static class MediaUriHelper
{
    public static bool TryCreate(string? url, out Uri uri)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var trimmed = url.Trim();
        try
        {
            if (trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                uri = new Uri("https:" + trimmed, UriKind.Absolute);
                return true;
            }

            if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                uri = new Uri(trimmed, UriKind.Absolute);
                return true;
            }

            if (Path.IsPathRooted(trimmed) || trimmed.Contains('\\') || trimmed.Contains(':'))
            {
                uri = new Uri(Path.GetFullPath(trimmed), UriKind.Absolute);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
