using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Rule34GalleryApp.Services;

/// <summary>Uses the Windows Shell thumbnail cache (same as Explorer) for local videos and other non-image files.</summary>
internal static class ShellThumbnailService
{
    private static readonly Guid ShellItemGuid = new("43826d1e-e518-4cee-9ecc-f61129fad6cc");

    public static async Task<BitmapImage?> GetThumbnailAsync(
        string filePath,
        int pixelSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        return await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return GetThumbnailOnUiThread(filePath, pixelSize);
        }).Task.ConfigureAwait(false);
    }

    private static BitmapImage? GetThumbnailOnUiThread(string filePath, int pixelSize)
    {
        var size = Math.Clamp(pixelSize, 64, 1024);
        IntPtr hBitmap = IntPtr.Zero;

        try
        {
            var fullPath = Path.GetFullPath(filePath);
            var shellItemGuid = ShellItemGuid;
            SHCreateItemFromParsingName(fullPath, IntPtr.Zero, ref shellItemGuid, out var shellItem);
            var factory = (IShellItemImageFactory)shellItem;
            var nativeSize = new NativeSize { cx = size, cy = size };
            var hr = factory.GetImage(
                nativeSize,
                SIIGBF.ThumbnailOnly | SIIGBF.BiggerSizeOk,
                out hBitmap);

            if (hr != 0 || hBitmap == IntPtr.Zero)
            {
                return null;
            }

            var source = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            return EncodeToBitmapImage(source, size);
        }
        catch
        {
            return null;
        }
        finally
        {
            if (hBitmap != IntPtr.Zero)
            {
                DeleteObject(hBitmap);
            }
        }
    }

    private static BitmapImage EncodeToBitmapImage(BitmapSource source, int decodePixelWidth)
    {
        var frame = BitmapFrame.Create(source);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(frame);
        using var memory = new MemoryStream();
        encoder.Save(memory);
        memory.Position = 0;

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.DecodePixelWidth = decodePixelWidth;
        image.StreamSource = memory;
        image.EndInit();
        if (image.CanFreeze)
        {
            image.Freeze();
        }

        return image;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SHCreateItemFromParsingName(
        string pszPath,
        IntPtr pbc,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out object ppv);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [ComImport]
    [Guid("bcc18b79-ba16-442f-80e4-39a357daebbc")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        [PreserveSig]
        int GetImage(NativeSize size, SIIGBF flags, out IntPtr phbm);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeSize
    {
        public int cx;
        public int cy;
    }

    [Flags]
    private enum SIIGBF : uint
    {
        ResizeToFit = 0x00,
        BiggerSizeOk = 0x01,
        MemoryOnly = 0x02,
        IconOnly = 0x04,
        ThumbnailOnly = 0x08,
        InCacheOnly = 0x10,
    }
}
