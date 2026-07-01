using Rule34Gallery.Core.Services;
using Rule34Gallery.Maui.Services;

namespace Rule34Gallery.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();

        AppServices.Current.Messenger.ToastRequested += async (_, msg) =>
        {
            var page = Current?.Windows.FirstOrDefault()?.Page;
            if (page is not null)
            {
                await page.DisplayAlert(msg.Title, msg.Body, "OK");
            }
        };

        _ = InitializeAsync();
    }

    private static async Task InitializeAsync()
    {
        await MauiBootstrap.InitializeAsync();
        await AppServices.Current.Library.InitializeAsync();
        await AppServices.Current.ForYou.SyncFromCloudAsync();
    }
}
