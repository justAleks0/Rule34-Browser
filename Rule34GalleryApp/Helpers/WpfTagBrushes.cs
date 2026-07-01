using System.Windows.Media;

namespace Rule34GalleryApp.Helpers;

internal static class WpfTagBrushes
{
    public static Brush Foreground(TagCategory category)
        => BrushFromHex(TagCategoryColors.GetForeground(category));

    public static Brush Background(TagCategory category)
        => BrushFromHex(TagCategoryColors.GetBackground(category));

    public static Brush BrushFromHex(string hex)
    {
        var color = (Color)ColorConverter.ConvertFromString(hex)!;
        var brush = new SolidColorBrush(color);
        if (brush.CanFreeze)
        {
            brush.Freeze();
        }

        return brush;
    }
}
