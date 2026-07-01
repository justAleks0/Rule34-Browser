using System.Diagnostics;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace Rule34GalleryApp.Helpers;

internal static class HyperlinkHelper
{
    public static Hyperlink Create(string url, string label)
    {
        var link = new Hyperlink(new Run(label)) { NavigateUri = new Uri(url) };
        link.RequestNavigate += (_, e) =>
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true,
            });
            e.Handled = true;
        };
        return link;
    }

    public static void OpenUri(RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true,
        });
        e.Handled = true;
    }
}
