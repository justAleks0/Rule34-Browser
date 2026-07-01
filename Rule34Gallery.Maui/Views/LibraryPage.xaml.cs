using Rule34Gallery.Maui.ViewModels;

namespace Rule34Gallery.Maui.Views;

public partial class LibraryPage : ContentPage
{
    private readonly LibraryViewModel _vm;

    public LibraryPage()
    {
        InitializeComponent();
        BindingContext = _vm = new LibraryViewModel();
    }

    private async void OnPostSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is PostItem post)
        {
            Gallery.SelectedItem = null;
            await _vm.OpenPostCommand.ExecuteAsync(post);
        }
    }
}
