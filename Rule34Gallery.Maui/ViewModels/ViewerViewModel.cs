using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rule34Gallery.Core.Services;

namespace Rule34Gallery.Maui.ViewModels;

[QueryProperty(nameof(Index), "index")]
[QueryProperty(nameof(IsLocal), "local")]
public partial class ViewerViewModel : ObservableObject
{
    private readonly AppServices _app = AppServices.Current;

    [ObservableProperty]
    private int _index;

    [ObservableProperty]
    private bool _isLocal;

    [ObservableProperty]
    private ImageSource? _mediaSource;

    [ObservableProperty]
    private bool _isVideo;

    [ObservableProperty]
    private PostItem? _currentPost;

    [ObservableProperty]
    private string _tagsText = string.Empty;

    public void LoadCurrent()
    {
        var posts = IsLocal ? _app.LocalPosts : _app.Posts;
        if (Index < 0 || Index >= posts.Count)
        {
            return;
        }

        CurrentPost = posts[Index];
        TagsText = string.Join(" ", CurrentPost.GetTagList());
        IsVideo = CurrentPost.IsPlayableMedia;
        var url = IsVideo ? CurrentPost.FileUrl : (CurrentPost.FastViewerUrl ?? CurrentPost.SampleUrl);
        if (!string.IsNullOrWhiteSpace(url))
        {
            _ = LoadImageAsync(url);
        }

        if (!IsLocal && ForYouLearningSources.ShouldLearnFromPassiveView(_app.ViewerLearningSource))
        {
            _app.ForYou.RecordPostOpened(CurrentPost);
        }
    }

    private async Task LoadImageAsync(string url)
    {
        var img = await _app.ImageCache.LoadAsync(url, 1280);
        if (img is ImageSource source)
        {
            MediaSource = source;
        }
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        var posts = IsLocal ? _app.LocalPosts : _app.Posts;
        if (Index < posts.Count - 1)
        {
            Index++;
            LoadCurrent();
            _app.Gallery.PreloadViewerNeighbors(posts, Index);
        }
    }

    [RelayCommand]
    private async Task PreviousAsync()
    {
        if (Index > 0)
        {
            Index--;
            LoadCurrent();
            var posts = IsLocal ? _app.LocalPosts : _app.Posts;
            _app.Gallery.PreloadViewerNeighbors(posts, Index);
        }
    }

    [RelayCommand]
    private async Task SearchTagAsync(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        _app.Settings.AddIncludeTag(tag);
        await _app.Gallery.SearchAsync(resetPage: true);
        await Shell.Current.GoToAsync("//Browse");
    }

    [RelayCommand]
    private void OpenInBrowser()
    {
        if (!string.IsNullOrWhiteSpace(CurrentPost?.SitePostUrl))
        {
            _app.Platform.Browser.OpenUrl(CurrentPost.SitePostUrl);
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (CurrentPost is null || !_app.Library.IsSignedIn)
        {
            return;
        }

        await _app.Library.ToggleFavoriteAsync(CurrentPost);
    }

    [RelayCommand]
    private void Download()
    {
        if (CurrentPost is not null)
        {
            _app.Downloads.Enqueue(CurrentPost);
        }
    }
}
