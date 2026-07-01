namespace Rule34Gallery.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(Views.ViewerPage), typeof(Views.ViewerPage));
        Routing.RegisterRoute(nameof(Views.DownloadsPage), typeof(Views.DownloadsPage));
        Routing.RegisterRoute(nameof(Views.TagDiscoverPage), typeof(Views.TagDiscoverPage));
        Routing.RegisterRoute(nameof(Views.PresetPickerPage), typeof(Views.PresetPickerPage));
    }
}
