using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace Rule34GalleryApp.Helpers;

internal enum DefaultBrowserKind
{
    Unknown,
    Chrome,
    Edge,
    Firefox,
    Brave,
    Opera,
}

internal static class BrowserLauncher
{
    public static void OpenNormal(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    public static bool TryOpenPrivate(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!TryResolveDefaultBrowser(out var executable, out var kind))
        {
            return false;
        }

        var arguments = kind switch
        {
            DefaultBrowserKind.Chrome or DefaultBrowserKind.Brave => $"--incognito \"{url}\"",
            DefaultBrowserKind.Edge => $"-inprivate \"{url}\"",
            DefaultBrowserKind.Firefox => $"-private-window \"{url}\"",
            DefaultBrowserKind.Opera => $"--private \"{url}\"",
            _ => null,
        };

        if (arguments is null)
        {
            return false;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            UseShellExecute = false,
        });

        return true;
    }

    private static bool TryResolveDefaultBrowser(out string executable, out DefaultBrowserKind kind)
    {
        executable = string.Empty;
        kind = DefaultBrowserKind.Unknown;

        var progId = Registry.CurrentUser
            .OpenSubKey(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice")
            ?.GetValue("ProgId") as string;

        if (string.IsNullOrWhiteSpace(progId))
        {
            return false;
        }

        kind = ClassifyBrowser(progId);

        var command = Registry.ClassesRoot
            .OpenSubKey($@"{progId}\shell\open\command")
            ?.GetValue(null) as string;

        var path = ParseExecutableFromOpenCommand(command);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        executable = path!;

        if (kind == DefaultBrowserKind.Unknown)
        {
            kind = ClassifyBrowser(Path.GetFileName(path));
        }

        return kind != DefaultBrowserKind.Unknown;
    }

    private static DefaultBrowserKind ClassifyBrowser(string value)
    {
        if (value.Contains("ChromeHTML", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("chrome", StringComparison.OrdinalIgnoreCase))
        {
            return DefaultBrowserKind.Chrome;
        }

        if (value.Contains("MSEdgeHTM", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("msedge", StringComparison.OrdinalIgnoreCase))
        {
            return DefaultBrowserKind.Edge;
        }

        if (value.Contains("FirefoxURL", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("firefox", StringComparison.OrdinalIgnoreCase))
        {
            return DefaultBrowserKind.Firefox;
        }

        if (value.Contains("BraveHTML", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("brave", StringComparison.OrdinalIgnoreCase))
        {
            return DefaultBrowserKind.Brave;
        }

        if (value.Contains("Opera", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("opera", StringComparison.OrdinalIgnoreCase))
        {
            return DefaultBrowserKind.Opera;
        }

        return DefaultBrowserKind.Unknown;
    }

    private static string? ParseExecutableFromOpenCommand(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return null;
        }

        command = command.Trim();
        if (command.StartsWith('"'))
        {
            var end = command.IndexOf('"', 1);
            if (end > 1)
            {
                return command[1..end];
            }
        }

        var exeIndex = command.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        if (exeIndex < 0)
        {
            return null;
        }

        return command[..(exeIndex + 4)].Trim().Trim('"');
    }
}
