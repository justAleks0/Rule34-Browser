namespace Rule34GalleryApp.Controls;

internal static class PageLoadingHelper
{
    public static bool IsBusyStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        return status.Contains("Searching", StringComparison.OrdinalIgnoreCase) ||
               status.Contains("Loading", StringComparison.OrdinalIgnoreCase) ||
               status.Contains("Scanning", StringComparison.OrdinalIgnoreCase);
    }
}
