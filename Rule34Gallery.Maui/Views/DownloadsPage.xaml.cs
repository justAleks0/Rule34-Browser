using Rule34Gallery.Maui.ViewModels;

namespace Rule34Gallery.Maui.Views;

public partial class DownloadsPage : ContentPage
{
    private readonly DownloadsViewModel _vm;

    public DownloadsPage()
    {
        InitializeComponent();
        BindingContext = _vm = new DownloadsViewModel();
    }

    private void OnCancelClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: DownloadJob job })
        {
            _vm.CancelCommand.Execute(job);
        }
    }
}
