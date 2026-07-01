using System.Windows;
using Rule34GalleryApp.Views.Overlays;

namespace Rule34GalleryApp.Services;

public static class TagDiscoverService
{
    public static void Show(UserSettings settings, Action onChanged)
    {
        if (Application.Current.MainWindow is not MainWindow mainWindow)
        {
            return;
        }

        mainWindow.ShowTagDiscover(settings, onChanged);
    }
}
