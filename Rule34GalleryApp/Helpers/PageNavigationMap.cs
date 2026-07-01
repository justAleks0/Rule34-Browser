using Rule34GalleryApp.Views.Pages;

namespace Rule34GalleryApp.Helpers;

internal static class PageNavigationMap
{
    private static readonly Dictionary<string, Type> IdToType = new(StringComparer.Ordinal)
    {
        [AppPageIds.ForYou] = typeof(ForYouPage),
        [AppPageIds.Browse] = typeof(BrowsePage),
        [AppPageIds.SavedTags] = typeof(SavedTagsPage),
        [AppPageIds.Library] = typeof(LibraryPage),
        [AppPageIds.Local] = typeof(LocalLibraryPage),
        [AppPageIds.Settings] = typeof(SettingsPage),
        [AppPageIds.Account] = typeof(AccountPage),
        [AppPageIds.Downloads] = typeof(DownloadsPage),
        [AppPageIds.Sync] = typeof(SyncPage),
        [AppPageIds.Help] = typeof(HelpPage),
    };

    public static string FromType(Type pageType)
    {
        foreach (var pair in IdToType)
        {
            if (pair.Value == pageType)
            {
                return pair.Key;
            }
        }

        return AppPageIds.Browse;
    }

    public static Type ToType(string pageId) =>
        IdToType.TryGetValue(pageId, out var type) ? type : typeof(BrowsePage);
}
