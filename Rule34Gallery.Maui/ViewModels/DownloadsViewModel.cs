using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Rule34Gallery.Maui.ViewModels;

public partial class DownloadsViewModel : ObservableObject
{
    private readonly AppServices _app = AppServices.Current;

    public ObservableCollection<DownloadJob> Jobs => _app.Downloads.Jobs;

    public DownloadsViewModel()
    {
        _app.Downloads.JobsChanged += (_, _) => OnPropertyChanged(nameof(Jobs));
    }

    [RelayCommand]
    public void Cancel(DownloadJob job) => _app.Downloads.CancelJob(job);

    [RelayCommand]
    private void ClearFinished() => _app.Downloads.ClearFinished();
}
