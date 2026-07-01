using Rule34Gallery.Maui.ViewModels;

namespace Rule34Gallery.Maui.Views;

public partial class ForYouPage : ContentPage
{
    private readonly ForYouViewModel _vm;

    public ForYouPage()
    {
        InitializeComponent();
        BindingContext = _vm = new ForYouViewModel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCommand.ExecuteAsync(null);
    }

    private async void OnFeedItemSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ForYouFeedItem item && item.Post is not null)
        {
            ((CollectionView)sender!).SelectedItem = null;
            await _vm.OpenPostCommand.ExecuteAsync(item.Post);
        }
    }
}

