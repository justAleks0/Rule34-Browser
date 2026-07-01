using System.IO;
using Rule34Gallery.Core.Abstractions;
using Rule34Gallery.Core.Settings;
using Rule34GalleryApp.Services;

namespace Rule34GalleryApp.Platform;

internal sealed class WpfPlatformServices : IPlatformServices
{
    public WpfPlatformServices()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Rule34GalleryApp");
        AppDataFolder = appData;
        CacheFolder = Path.Combine(appData, "cache");
        Directory.CreateDirectory(CacheFolder);

        SecureStore = new DpapiSecureStore();
        SettingsStore = new JsonSettingsStore(SecureStore, appData);
        ImageCache = new WpfImageCache();
        MediaPlaybackCache = new WpfMediaPlaybackCache(CacheFolder);
        Thumbnails = new WpfThumbnailProvider();
        LocalFiles = new WpfLocalFileAccess();
        Browser = new WpfBrowserLauncher();
        GoogleSignIn = new WpfGoogleSignInService();
    }

    public ISecureCredentialStore SecureStore { get; }

    public ISettingsStore SettingsStore { get; }

    public IImageCache ImageCache { get; }

    public IMediaPlaybackCache MediaPlaybackCache { get; }

    public IThumbnailProvider Thumbnails { get; }

    public ILocalFileAccess LocalFiles { get; }

    public IBrowserLauncher Browser { get; }

    public IGoogleSignInService GoogleSignIn { get; }

    public string AppDataFolder { get; }

    public string CacheFolder { get; }
}
