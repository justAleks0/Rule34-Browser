using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Rule34Gallery.Core;
using Rule34Gallery.Core.Remote;
using Rule34GalleryApp.Helpers;
using Rule34GalleryApp.Services;

namespace Rule34GalleryApp.Views.Pages;

public partial class DownloadsPage : Page
{
    private readonly AppServices _app = AppServices.Current;

    public DownloadsPage()
    {
        InitializeComponent();
        PageScrollHelper.Attach(this, DownloadsScroll);
        JobsList.ItemsSource = _app.Downloads.Jobs;
        _app.Downloads.JobsChanged += (_, _) => Dispatcher.Invoke(RefreshUi);
        Loaded += (_, _) => RefreshUi();
    }

    public void RefreshUi()
    {
        var library = _app.Downloads.ResolveDownloadLibrary();
        DownloadLibraryHint.Text = library is null
            ? "Set a download library under Settings → Downloads (or create one on the Local tab)."
            : $"Saving to: {library.Name}";
        if (library is not null && !string.IsNullOrWhiteSpace(library.RootFolderPath))
        {
            DownloadLibraryHint.Text += $"\n{library.RootFolderPath}";
        }

        var jobs = _app.Downloads.Jobs;
        var active = jobs.Count(j =>
            j.Status is DownloadJobStatus.Queued or DownloadJobStatus.Downloading);
        var finished = jobs.Count(j =>
            j.Status is DownloadJobStatus.Completed or DownloadJobStatus.Failed or DownloadJobStatus.Cancelled);
        JobsSummaryText.Text = jobs.Count == 0
            ? "No downloads in queue."
            : $"{active} active · {finished} finished · {jobs.Count} total";

        var hasJobs = jobs.Count > 0;
        EmptyJobsPanel.Visibility = hasJobs ? Visibility.Collapsed : Visibility.Visible;
        JobsList.Visibility = hasJobs ? Visibility.Visible : Visibility.Collapsed;

        if (active > 0)
        {
            LoadingOverlay.Show($"{active} download{(active == 1 ? "" : "s")} in progress…");
        }
        else
        {
            LoadingOverlay.Hide();
        }
    }

    private void ClearFinishedButton_OnClick(object sender, RoutedEventArgs e)
    {
        _app.Downloads.ClearFinished();
        RefreshUi();
    }

    private void OpenFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        var library = _app.Downloads.ResolveDownloadLibrary();
        if (library is null || string.IsNullOrWhiteSpace(library.RootFolderPath))
        {
            return;
        }

        try
        {
            var path = LocalLibraryService.NormalizeFolderPath(library.RootFolderPath);
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            _app.Messenger.Show("Open folder failed", ex.Message, AppMessageKind.Warning);
        }
    }

    private void CancelJobButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: DownloadJob job })
        {
            _app.Downloads.CancelJob(job);
            RefreshUi();
        }
    }

    private void RetryJobButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: DownloadJob job })
        {
            return;
        }

        _app.Downloads.RetryJob(job);
        _app.Messenger.Show(
            "Download queued",
            $"Retrying {job.FileName}",
            AppMessageKind.Info);
        RefreshUi();
    }

    public RemoteDownloadsState CaptureRemoteState()
    {
        var jobs = _app.Downloads.Jobs;
        var active = jobs.Count(j =>
            j.Status is DownloadJobStatus.Queued or DownloadJobStatus.Downloading);
        var finished = jobs.Count(j =>
            j.Status is DownloadJobStatus.Completed or DownloadJobStatus.Failed or DownloadJobStatus.Cancelled);

        return new RemoteDownloadsState
        {
            ActiveCount = active,
            FinishedCount = finished,
            TotalCount = jobs.Count,
            Summary = jobs.Count == 0
                ? "No downloads in queue."
                : $"{active} active · {finished} finished · {jobs.Count} total",
        };
    }

    public void RemoteClearFinished()
    {
        _app.Downloads.ClearFinished();
        RefreshUi();
    }
}
