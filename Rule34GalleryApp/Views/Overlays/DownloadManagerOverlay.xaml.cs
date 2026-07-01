using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Rule34GalleryApp.Services;

namespace Rule34GalleryApp.Views.Overlays;

public partial class DownloadManagerOverlay : UserControl
{
    private readonly AppServices _app = AppServices.Current;

    public DownloadManagerOverlay()
    {
        InitializeComponent();
        JobsList.ItemsSource = _app.Downloads.Jobs;
        _app.Downloads.JobsChanged += (_, _) => Dispatcher.Invoke(RefreshUi);
    }

    public void Show()
    {
        RefreshUi();
        Visibility = Visibility.Visible;
        Focus();
    }

    public void Hide() => Visibility = Visibility.Collapsed;

    public void EnqueueAndShow(PostItem post)
    {
        try
        {
            _app.Downloads.Enqueue(post);
            Show();
        }
        catch (Exception ex)
        {
            _app.Messenger.Show("Download failed", ex.Message, AppMessageKind.Warning);
        }
    }

    private void RefreshUi()
    {
        var library = _app.Downloads.ResolveDownloadLibrary();
        DownloadLibraryHint.Text = library is null
            ? "Set a download library in Settings → Downloads (or create one on Local)."
            : $"Saving under: {library.Name} → {library.RootFolderPath}";

        var hasJobs = _app.Downloads.Jobs.Count > 0;
        EmptyJobsText.Visibility = hasJobs ? Visibility.Collapsed : Visibility.Visible;
        JobsList.Visibility = hasJobs ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e) => Hide();

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
        catch
        {
            // Ignore explorer failures.
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
}
