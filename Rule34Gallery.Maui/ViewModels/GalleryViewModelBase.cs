using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rule34Gallery.Core.Services;

namespace Rule34Gallery.Maui.ViewModels;

public partial class GalleryViewModelBase : ObservableObject
{
    protected readonly AppServices App = AppServices.Current;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<PostItem> Posts => App.Posts;

    [RelayCommand]
    protected async Task SearchAsync()
    {
        IsBusy = true;
        try
        {
            await App.Gallery.SearchAsync(resetPage: true);
            StatusText = App.Gallery.ViewMode switch
            {
                GalleryViewMode.Favorites => $"Favorites | {Posts.Count} posts",
                GalleryViewMode.List => $"List | {Posts.Count} posts",
                _ => $"Page {App.Gallery.CurrentPage} | Results: {Posts.Count}",
            };
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    protected async Task NextPageAsync()
    {
        IsBusy = true;
        try
        {
            await App.Gallery.NextPageAsync();
            StatusText = $"Page {App.Gallery.CurrentPage} | Results: {Posts.Count}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    protected async Task PrevPageAsync()
    {
        IsBusy = true;
        try
        {
            await App.Gallery.PrevPageAsync();
            StatusText = $"Page {App.Gallery.CurrentPage} | Results: {Posts.Count}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected async Task OpenViewerAsync(PostItem post)
    {
        var index = Posts.IndexOf(post);
        if (index < 0)
        {
            return;
        }

        App.ViewerLearningSource = ForYouLearningSources.Browse;
        await Shell.Current.GoToAsync($"{nameof(Views.ViewerPage)}?index={index}&local=false");
    }

    [RelayCommand]
    public Task OpenPostAsync(PostItem post) => OpenViewerAsync(post);

    [RelayCommand]
    public async Task ToggleFavoriteAsync(PostItem post)
    {
        if (!App.Library.IsSignedIn)
        {
            await Shell.Current.DisplayAlert("Sign in required", "Open Account to sign in.", "OK");
            return;
        }

        await App.Library.ToggleFavoriteAsync(post);
    }

    [RelayCommand]
    public void DownloadPost(PostItem post)
    {
        App.Downloads.Enqueue(post);
        App.Messenger.Show("Download queued", post.FileUrl ?? post.SampleUrl ?? "post", AppMessageKind.Info);
    }
}
