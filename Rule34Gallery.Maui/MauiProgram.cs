using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Rule34Gallery.Core.Services;
using Rule34Gallery.Maui.Platform;
using Rule34Gallery.Maui.Services;

namespace Rule34Gallery.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        AppServices.Initialize(new MauiPlatformServices());

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
