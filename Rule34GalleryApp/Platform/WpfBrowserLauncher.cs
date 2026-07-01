using System.Diagnostics;
using Rule34Gallery.Core.Abstractions;

namespace Rule34GalleryApp.Platform;

internal sealed class WpfBrowserLauncher : IBrowserLauncher
{
    public void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
