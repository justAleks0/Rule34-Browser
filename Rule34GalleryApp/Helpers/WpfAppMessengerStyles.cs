using System.Windows.Media;

namespace Rule34GalleryApp.Helpers;

internal static class WpfAppMessengerStyles
{
    public static (Brush Background, Brush Border, Brush Title) GetStyle(AppMessageKind kind) => kind switch
    {
        AppMessageKind.Warning => (
            new SolidColorBrush(Color.FromRgb(0x3a, 0x2e, 0x14)),
            new SolidColorBrush(Color.FromRgb(0x8a, 0x6a, 0x22)),
            new SolidColorBrush(Color.FromRgb(0xf0, 0xd0, 0x70))),
        AppMessageKind.Error => (
            new SolidColorBrush(Color.FromRgb(0x3a, 0x1a, 0x1a)),
            new SolidColorBrush(Color.FromRgb(0x8a, 0x33, 0x33)),
            new SolidColorBrush(Color.FromRgb(0xf0, 0xa0, 0xa0))),
        _ => (
            new SolidColorBrush(Color.FromRgb(0x1a, 0x2a, 0x1e)),
            new SolidColorBrush(Color.FromRgb(0x4a, 0x7a, 0x3a)),
            new SolidColorBrush(Color.FromRgb(0xa0, 0xe0, 0x70))),
    };
}
