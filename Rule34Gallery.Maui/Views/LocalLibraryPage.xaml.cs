using Rule34Gallery.Maui.ViewModels;

namespace Rule34Gallery.Maui.Views;

public partial class LocalLibraryPage : ContentPage
{
    private readonly LocalLibraryViewModel _vm;

    public LocalLibraryPage()
    {
        InitializeComponent();
        BindingContext = _vm = new LocalLibraryViewModel();
    }

    private async void OnPostSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is PostItem post)
        {
            LocalGallery.SelectedItem = null;
            await _vm.OpenLocalPostCommand.ExecuteAsync(post);
        }
    }
}
