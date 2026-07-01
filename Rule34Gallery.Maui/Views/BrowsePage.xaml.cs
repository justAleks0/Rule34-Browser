using Rule34Gallery.Maui.ViewModels;

namespace Rule34Gallery.Maui.Views;

public partial class BrowsePage : ContentPage
{
    private readonly BrowseViewModel _vm;

    public BrowsePage()
    {
        InitializeComponent();
        BindingContext = _vm = new BrowseViewModel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (ToolbarItems.Count == 0)
        {
            ToolbarItems.Add(new ToolbarItem("Downloads", null, async () =>
                await Shell.Current.GoToAsync(nameof(DownloadsPage))));
        }
    }

    private async void OnPostSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is PostItem post)
        {
            GalleryView.SelectedItem = null;
            await _vm.OpenPostCommand.ExecuteAsync(post);
        }
    }
}
