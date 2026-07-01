using System.Windows;
using Rule34GalleryApp.Views.Overlays;

namespace Rule34GalleryApp.Services;

public enum PresetPickerKind
{
    Search,
    Blacklist,
}

public static class PresetPickerService
{
    public static void ShowSearchPresets(UserSettings settings, Action onChanged)
        => Show(PresetPickerKind.Search, settings, onChanged);

    public static void ShowBlacklistPresets(UserSettings settings, Action onChanged)
        => Show(PresetPickerKind.Blacklist, settings, onChanged);

    private static void Show(PresetPickerKind kind, UserSettings settings, Action onChanged)
    {
        if (Application.Current.MainWindow is not MainWindow mainWindow)
        {
            return;
        }

        mainWindow.ShowPresetPicker(kind, settings, onChanged);
    }
}
