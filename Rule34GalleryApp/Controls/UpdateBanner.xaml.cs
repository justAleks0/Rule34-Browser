using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Rule34Gallery.Core;
using Rule34Gallery.Core.Services;
using Rule34Gallery.Core.Updates;
using Rule34GalleryApp.Services;

namespace Rule34GalleryApp.Controls;

public partial class UpdateBanner : UserControl
{
    private readonly AppServices _app = AppServices.Current;
    private readonly WindowsUpdateInstaller _installer;
    private UpdateInfo? _update;
    private bool _downloaded;
    private CancellationTokenSource? _downloadCts;

    public UpdateBanner()
    {
        InitializeComponent();
        _installer = new WindowsUpdateInstaller(_app.Http);
        Loaded += (_, _) => _ = CheckOnStartupAsync();
    }

    public async Task CheckNowAsync()
    {
        await CheckInternalAsync(force: true).ConfigureAwait(true);
    }

    private async Task CheckOnStartupAsync()
    {
        if (!_app.Settings.CheckForUpdatesOnStartup)
        {
            return;
        }

        await CheckInternalAsync(force: false).ConfigureAwait(true);
    }

    private async Task CheckInternalAsync(bool force)
    {
        try
        {
            var version = GetCurrentVersion();
            var update = await _app.Updates.CheckForUpdateAsync(
                version,
                UpdateCatalog.WindowsZipAsset,
                _app.Settings.DismissedUpdateVersion).ConfigureAwait(true);

            if (update is null)
            {
                if (force)
                {
                    _app.Messenger.Show("Updates", "You're on the latest version.");
                }

                Hide();
                return;
            }

            ShowUpdate(update);
        }
        catch
        {
            if (force)
            {
                _app.Messenger.Show("Updates", "Could not check for updates right now.");
            }
        }
    }

    private void ShowUpdate(UpdateInfo update)
    {
        _update = update;
        _downloaded = false;
        TitleText.Text = $"Update v{update.Version} available";
        BodyText.Text = string.IsNullOrWhiteSpace(update.ReleaseNotes)
            ? "A newer build is ready from GitHub Releases."
            : TruncateNotes(update.ReleaseNotes);
        PrimaryButton.Content = "Download";
        PrimaryButton.IsEnabled = true;
        DismissButton.Visibility = Visibility.Visible;
        DownloadProgress.Visibility = Visibility.Collapsed;
        Visibility = Visibility.Visible;
    }

    private void Hide()
    {
        Visibility = Visibility.Collapsed;
        _update = null;
        _downloaded = false;
    }

    private async void PrimaryButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_update is null)
        {
            return;
        }

        if (_downloaded)
        {
            try
            {
                _installer.ApplyAndRestart();
            }
            catch (Exception ex)
            {
                _app.Messenger.Show("Update failed", ex.Message, AppMessageKind.Error);
            }

            return;
        }

        PrimaryButton.IsEnabled = false;
        DismissButton.IsEnabled = false;
        DownloadProgress.Visibility = Visibility.Visible;
        DownloadProgress.IsIndeterminate = true;
        BodyText.Text = "Downloading update...";

        _downloadCts?.Cancel();
        _downloadCts = new CancellationTokenSource();

        try
        {
            var progress = new Progress<double>(value =>
            {
                Dispatcher.Invoke(() =>
                {
                    DownloadProgress.IsIndeterminate = false;
                    DownloadProgress.Value = value;
                });
            });

            await _installer.DownloadAsync(_update, progress, _downloadCts.Token).ConfigureAwait(true);
            _downloaded = true;
            TitleText.Text = $"Update v{_update.Version} ready";
            BodyText.Text = "Restart to install the new version. Your firebase-config.json is preserved.";
            PrimaryButton.Content = "Restart to update";
            PrimaryButton.IsEnabled = true;
            DismissButton.IsEnabled = true;
            DownloadProgress.Visibility = Visibility.Collapsed;
        }
        catch (OperationCanceledException)
        {
            // Ignore.
        }
        catch (Exception ex)
        {
            _app.Messenger.Show("Download failed", ex.Message, AppMessageKind.Error);
            if (_update is not null)
            {
                ShowUpdate(_update);
            }
        }
    }

    private void DismissButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_update is not null)
        {
            _app.Settings.DismissedUpdateVersion = _update.Version;
            _app.SaveSettings();
        }

        Hide();
    }

    private static string GetCurrentVersion()
    {
        try
        {
            var exePath = Assembly.GetExecutingAssembly().Location;
            var productVersion = FileVersionInfo.GetVersionInfo(exePath).ProductVersion;
            return string.IsNullOrWhiteSpace(productVersion) ? "0.0.0" : productVersion;
        }
        catch
        {
            return "0.0.0";
        }
    }

    private static string TruncateNotes(string notes)
    {
        var oneLine = notes.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return oneLine.Length <= 180 ? oneLine : oneLine[..177] + "...";
    }
}
