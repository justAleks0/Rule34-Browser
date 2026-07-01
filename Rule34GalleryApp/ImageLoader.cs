using System.Windows;
using System.Windows.Controls;

namespace Rule34GalleryApp;

public static class ImageLoader
{
    public static readonly DependencyProperty SourceUrlProperty =
        DependencyProperty.RegisterAttached(
            "SourceUrl",
            typeof(string),
            typeof(ImageLoader),
            new PropertyMetadata(null, OnSourceUrlChanged));

    public static readonly DependencyProperty DecodePixelWidthProperty =
        DependencyProperty.RegisterAttached(
            "DecodePixelWidth",
            typeof(int),
            typeof(ImageLoader),
            new PropertyMetadata(0));

    private static readonly DependencyProperty LoadVersionProperty =
        DependencyProperty.RegisterAttached(
            "LoadVersion",
            typeof(int),
            typeof(ImageLoader),
            new PropertyMetadata(0));

    public static void SetSourceUrl(Image element, string? value) => element.SetValue(SourceUrlProperty, value);

    public static string? GetSourceUrl(Image element) => (string?)element.GetValue(SourceUrlProperty);

    public static void SetDecodePixelWidth(Image element, int value) => element.SetValue(DecodePixelWidthProperty, value);

    public static int GetDecodePixelWidth(Image element) => (int)element.GetValue(DecodePixelWidthProperty);

    private static void OnSourceUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Image image)
        {
            return;
        }

        var version = (int)image.GetValue(LoadVersionProperty) + 1;
        image.SetValue(LoadVersionProperty, version);
        image.Source = null;

        if (e.NewValue is not string url || string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        var decodeWidth = GetDecodePixelWidth(image);
        int? width = decodeWidth > 0 ? decodeWidth : null;

        if (ImageCache.TryGet(url, width, out var cached) && cached is not null)
        {
            image.Source = cached;
            return;
        }

        _ = LoadImageAsync(image, url, width, version);
    }

    public static void Reload(Image image)
    {
        var url = GetSourceUrl(image);
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        SetSourceUrl(image, string.Empty);
        SetSourceUrl(image, url);
    }

    private static async Task LoadImageAsync(Image image, string url, int? decodeWidth, int version)
    {
        try
        {
            var bitmap = await ImageCache.LoadAsync(url, decodeWidth).ConfigureAwait(true);
            if (bitmap is null)
            {
                return;
            }

            if ((int)image.GetValue(LoadVersionProperty) != version)
            {
                return;
            }

            image.Source = bitmap;
        }
        catch
        {
            // Thumbnail failed — leave placeholder empty.
        }
    }
}
