using Rule34Gallery.Core.Abstractions;
using Rule34Gallery.Core.Settings;

namespace Rule34Gallery.Maui.Platform;

internal sealed class MauiPlatformServices : IPlatformServices
{
    public MauiPlatformServices()
    {
        AppDataFolder = FileSystem.AppDataDirectory;
        CacheFolder = FileSystem.CacheDirectory;
        Directory.CreateDirectory(CacheFolder);

        SecureStore = new MauiSecureStore();
        SettingsStore = new JsonSettingsStore(SecureStore, AppDataFolder);
        ImageCache = new MauiImageCache(CacheFolder);
        MediaPlaybackCache = new PassthroughMediaPlaybackCache();
        Thumbnails = new MauiThumbnailProvider();
        LocalFiles = new MauiLocalFileAccess();
        Browser = new MauiBrowserLauncher();
        GoogleSignIn = new MauiGoogleSignInService();
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
