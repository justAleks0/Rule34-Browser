using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Rule34Gallery.Maui.ViewModels;

public partial class LocalLibraryViewModel : GalleryViewModelBase
{
    [ObservableProperty]
    private string _rootPath = string.Empty;

    [ObservableProperty]
    private string _categoryFilter = string.Empty;

    public ObservableCollection<PostItem> LocalPosts => App.LocalPosts;

    public ObservableCollection<LocalLibraryDefinition> Libraries => new(App.Settings.LocalLibraries);

    [RelayCommand]
    private async Task PickFolderAsync()
    {
        var result = await FolderPicker.Default.PickAsync();
        if (!result.IsSuccessful || result.Folder is null)
        {
            return;
        }

        RootPath = result.Folder.Path;
    }

    [RelayCommand]
    private async Task ScanAsync()
    {
        if (string.IsNullOrWhiteSpace(RootPath))
        {
            await Shell.Current.DisplayAlert("Folder required", "Pick a library root folder first.", "OK");
            return;
        }

        var library = App.Settings.LocalLibraries.FirstOrDefault()
                      ?? new LocalLibraryDefinition { Name = "Default" };
        library.RootFolderPath = RootPath;
        if (!App.Settings.LocalLibraries.Contains(library))
        {
            App.Settings.LocalLibraries.Add(library);
        }

        IsBusy = true;
        try
        {
            var posts = LocalLibraryService.ScanLibrary(
                library,
                null,
                string.IsNullOrWhiteSpace(CategoryFilter) ? null : CategoryFilter,
                App.Settings);
            App.LocalPosts.Clear();
            foreach (var post in posts)
            {
                App.LocalPosts.Add(post);
            }

            StatusText = $"Local | {App.LocalPosts.Count} items";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenLocalPostAsync(PostItem post)
    {
        var index = App.LocalPosts.IndexOf(post);
        await Shell.Current.GoToAsync($"{nameof(Views.ViewerPage)}?index={index}&local=true");
    }
}
