namespace Rule34Gallery.Core.Abstractions;

public interface IPlatformServices
{
    ISecureCredentialStore SecureStore { get; }

    ISettingsStore SettingsStore { get; }

    IImageCache ImageCache { get; }

    IMediaPlaybackCache MediaPlaybackCache { get; }

    IThumbnailProvider Thumbnails { get; }

    ILocalFileAccess LocalFiles { get; }

    IBrowserLauncher Browser { get; }

    IGoogleSignInService GoogleSignIn { get; }

    string AppDataFolder { get; }

    string CacheFolder { get; }
}
