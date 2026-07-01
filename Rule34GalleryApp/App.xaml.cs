using System.Windows;
using Rule34Gallery.Core.Services;
using Rule34GalleryApp.Platform;

namespace Rule34GalleryApp;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppServices.Initialize(new WpfPlatformServices());
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            if (AppServices.Current.Settings.ClearMediaPlaybackCacheOnExit)
            {
                AppServices.Current.MediaPlaybackCache.ClearAll();
            }
        }
        catch
        {
            // Best effort during shutdown.
        }

        base.OnExit(e);
    }
}

