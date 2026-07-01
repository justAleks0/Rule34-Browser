using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using Rule34Gallery.Core.Updates;

namespace Rule34GalleryApp.Services;

public sealed class WindowsUpdateInstaller
{
    private readonly HttpClient _http;

    public WindowsUpdateInstaller(HttpClient http)
    {
        _http = http;
    }

    public static string InstallDirectory
    {
        get
        {
            var exePath = Environment.ProcessPath
                          ?? Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory;
        }
    }

    public async Task DownloadAsync(
        UpdateInfo update,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var workDir = Path.Combine(Path.GetTempPath(), "Rule34Gallery-update");
        Directory.CreateDirectory(workDir);

        var zipPath = Path.Combine(workDir, UpdateCatalog.WindowsZipAsset);
        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        using var response = await _http.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var output = File.Create(zipPath);

        var buffer = new byte[81920];
        long readTotal = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            readTotal += read;
            if (total is > 0)
            {
                progress?.Report(readTotal / (double)total.Value);
            }
        }

        var extractDir = Path.Combine(workDir, "staged");
        if (Directory.Exists(extractDir))
        {
            Directory.Delete(extractDir, true);
        }

        Directory.CreateDirectory(extractDir);
        ZipFile.ExtractToDirectory(zipPath, extractDir, overwriteFiles: true);

        var stagedRoot = ResolveStagedRoot(extractDir);
        StagedSourceDirectory = stagedRoot;
        StagedZipPath = zipPath;
    }

    public string? StagedSourceDirectory { get; private set; }

    public string? StagedZipPath { get; private set; }

    public void ApplyAndRestart()
    {
        if (string.IsNullOrWhiteSpace(StagedSourceDirectory) || !Directory.Exists(StagedSourceDirectory))
        {
            throw new InvalidOperationException("No staged update folder. Download the update first.");
        }

        var installDir = InstallDirectory;
        var exePath = Environment.ProcessPath ?? Path.Combine(installDir, "Rule34Gallery.exe");
        var applyScript = ResolveApplyScript();
        if (!File.Exists(applyScript))
        {
            throw new FileNotFoundException("apply-windows-update.ps1 was not found next to the app.", applyScript);
        }

        var pid = Environment.ProcessId;
        var args = $"-NoProfile -ExecutionPolicy Bypass -File \"{applyScript}\" -ParentPid {pid} -SourceDir \"{StagedSourceDirectory}\" -InstallDir \"{installDir}\" -ExePath \"{exePath}\"";

        Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = installDir,
        });

        Application.Current.Shutdown();
    }

    private static string ResolveApplyScript()
    {
        var baseDir = AppContext.BaseDirectory;
        var nextToExe = Path.Combine(baseDir, "apply-windows-update.ps1");
        if (File.Exists(nextToExe))
        {
            return nextToExe;
        }

        var repoScript = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "scripts", "apply-windows-update.ps1"));
        if (File.Exists(repoScript))
        {
            return repoScript;
        }

        return nextToExe;
    }

    private static string ResolveStagedRoot(string extractDir)
    {
        var nested = Path.Combine(extractDir, "Rule34Gallery-win-x64");
        if (Directory.Exists(nested) && File.Exists(Path.Combine(nested, "Rule34Gallery.exe")))
        {
            return nested;
        }

        if (File.Exists(Path.Combine(extractDir, "Rule34Gallery.exe")))
        {
            return extractDir;
        }

        foreach (var dir in Directory.EnumerateDirectories(extractDir))
        {
            if (File.Exists(Path.Combine(dir, "Rule34Gallery.exe")))
            {
                return dir;
            }
        }

        return extractDir;
    }
}
