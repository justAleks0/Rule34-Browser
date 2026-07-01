using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Rule34Gallery.Maui.ViewModels;

public partial class LibraryViewModel : GalleryViewModelBase
{
    [ObservableProperty]
    private bool _isSignedIn;

    [ObservableProperty]
    private ObservableCollection<SavedList> _lists = [];

    [ObservableProperty]
    private SavedList? _selectedList;

    public LibraryViewModel()
    {
        RefreshAuthState();
        App.Library.AuthStateChanged += (_, _) => RefreshAuthState();
    }

    private void RefreshAuthState() => IsSignedIn = App.Library.IsSignedIn;

    [RelayCommand]
    private async Task LoadFavoritesAsync()
    {
        if (!IsSignedIn)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await App.Gallery.LoadFavoritesAsync();
            StatusText = $"Favorites | {Posts.Count} posts";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshListsAsync()
    {
        if (!IsSignedIn)
        {
            return;
        }

        var lists = await App.Library.LoadListsAsync();
        Lists = new ObservableCollection<SavedList>(lists);
    }

    [RelayCommand]
    private async Task LoadSelectedListAsync()
    {
        if (SelectedList is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await App.Gallery.LoadListAsync(SelectedList.Id, SelectedList.Name);
            StatusText = $"{SelectedList.Name} | {Posts.Count} posts";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateListAsync()
    {
        var name = await Shell.Current.DisplayPromptAsync("New list", "List name");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        await App.Library.CreateListAsync(name.Trim());
        await RefreshListsCommand.ExecuteAsync(null);
    }
}
