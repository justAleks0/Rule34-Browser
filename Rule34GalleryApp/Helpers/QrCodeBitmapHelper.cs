using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QRCoder;

namespace Rule34GalleryApp.Helpers;

public static class QrCodeBitmapHelper
{
    public static ImageSource? CreateImageSource(string text, int pixelsPerModule = 6)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var qr = new PngByteQRCode(data);
        var png = qr.GetGraphic(pixelsPerModule);
        using var stream = new MemoryStream(png);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
