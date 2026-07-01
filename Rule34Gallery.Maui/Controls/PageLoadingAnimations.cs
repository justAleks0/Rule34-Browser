namespace Rule34Gallery.Maui.Controls;

internal static class PageLoadingAnimations
{
    public static string Glyph(PageLoadingKind kind) => kind switch
    {
        PageLoadingKind.Browse => "⌕",
        PageLoadingKind.ForYou => "✦",
        PageLoadingKind.SavedTags => "🏷",
        PageLoadingKind.Library => "♥",
        PageLoadingKind.LocalLibrary => "▤",
        PageLoadingKind.Downloads => "↓",
        PageLoadingKind.Sync => "↻",
        PageLoadingKind.Help => "📖",
        PageLoadingKind.Settings => "⚙",
        PageLoadingKind.Account => "◎",
        _ => "…",
    };

    public static string DefaultMessage(PageLoadingKind kind) => kind switch
    {
        PageLoadingKind.Browse => "Searching…",
        PageLoadingKind.ForYou => "Building your feed…",
        PageLoadingKind.SavedTags => "Loading tag sets…",
        PageLoadingKind.Library => "Loading library…",
        PageLoadingKind.LocalLibrary => "Scanning folders…",
        PageLoadingKind.Downloads => "Downloading…",
        PageLoadingKind.Sync => "Syncing…",
        PageLoadingKind.Help => "Loading help…",
        PageLoadingKind.Settings => "Loading settings…",
        PageLoadingKind.Account => "Signing in…",
        _ => "Loading…",
    };
}
