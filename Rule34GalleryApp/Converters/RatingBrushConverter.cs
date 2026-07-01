using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Rule34GalleryApp.Converters;

public sealed class RatingBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var rating = value?.ToString()?.ToLowerInvariant() ?? string.Empty;
        return rating switch
        {
            "s" or "safe" => Application.Current.Resources["RatingSafeBrush"] as Brush ?? Brushes.Green,
            "q" or "questionable" => Application.Current.Resources["RatingQuestionableBrush"] as Brush ?? Brushes.Gold,
            "e" or "explicit" => Application.Current.Resources["RatingExplicitBrush"] as Brush ?? Brushes.IndianRed,
            _ => Application.Current.Resources["MutedBrush"] as Brush ?? Brushes.Gray,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
