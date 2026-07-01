using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Rule34Gallery.Core;
using Rule34GalleryApp.Services;

namespace Rule34GalleryApp.Controls;

public partial class BrowseFeedPanel : UserControl
{
    private readonly AppServices _app = AppServices.Current;
    private readonly ViewerPlaybackController _playback;
    private int _index;
    private bool _suppressWheel;

    public BrowseFeedPanel()
    {
        InitializeComponent();
        _playback = new ViewerPlaybackController(FeedMedia);
        _playback.Opened += (_, _) =>
        {
            if (Dispatcher.CheckAccess())
            {
                FeedMedia.Visibility = Visibility.Visible;
            }
            else
            {
                Dispatcher.BeginInvoke(() => FeedMedia.Visibility = Visibility.Visible);
            }
        };

        _playback.MediaFailed += (_, _) => Dispatcher.BeginInvoke(ShowImageFallbackForCurrentPost);
    }

    public int CurrentIndex => _index;

    public event EventHandler? IndexChanged;

    private int _feedPlaybackGeneration;

    public void BindPosts()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(BindPosts);
            return;
        }

        if (_app.Posts.Count == 0)
        {
            _index = 0;
            ShowEmpty();
            return;
        }

        _index = Math.Clamp(_index, 0, _app.Posts.Count - 1);
        ShowPostAt(_index);
    }

    public void NavigateNext()
    {
        if (_app.Posts.Count == 0)
        {
            return;
        }

        if (_index < _app.Posts.Count - 1)
        {
            ShowPostAt(_index + 1);
            return;
        }

        if (_app.Gallery.HasMorePages)
        {
            _ = LoadMoreThenShowAsync();
        }
    }

    public void NavigatePrevious()
    {
        if (_index > 0)
        {
            ShowPostAt(_index - 1);
        }
    }

    private async Task LoadMoreThenShowAsync()
    {
        try
        {
            var countBefore = _app.Posts.Count;
            await _app.Gallery.AppendNextPageAsync();
            if (_app.Posts.Count > countBefore && _index < _app.Posts.Count - 1)
            {
                ShowPostAt(_index + 1);
            }
        }
        catch
        {
            // Keep current post visible if pagination fails.
        }
    }

    private void ShowEmpty()
    {
        _playback.Close();
        FeedImage.Source = null;
        FeedImage.Visibility = Visibility.Collapsed;
        FeedMedia.Visibility = Visibility.Collapsed;
        EmptyText.Visibility = Visibility.Visible;
        FeedStatusText.Text = "Feed mode · phone remote connected";
        UpdateFeedAudioDeviceLabel(show: false);
    }

    private void UpdateFeedAudioDeviceLabel(bool show)
    {
        if (!show)
        {
            FeedAudioDeviceText.Visibility = Visibility.Collapsed;
            FeedAudioDeviceText.Text = string.Empty;
            return;
        }

        FeedAudioDeviceText.Text = $"Audio: {AudioOutputDeviceHelper.GetDefaultPlaybackDeviceLabel()}";
        FeedAudioDeviceText.Visibility = Visibility.Visible;
    }

    private void ShowPostAt(int index)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => ShowPostAt(index));
            return;
        }

        if (_app.Posts.Count == 0)
        {
            ShowEmpty();
            return;
        }

        _index = Math.Clamp(index, 0, _app.Posts.Count - 1);
        var post = _app.Posts[_index];
        EmptyText.Visibility = Visibility.Collapsed;

        FeedStatusText.Text = $"{_index + 1} / {_app.Posts.Count} · #{post.Id} · {post.Rating}";
        UpdateFeedActionButtons(post);
        UpdateFeedAudioDeviceLabel(show: true);

        _playback.Close();
        FeedMedia.Visibility = Visibility.Collapsed;

        if (post.IsPlayableMedia && !string.IsNullOrWhiteSpace(post.PlaybackUrl))
        {
            FeedImage.Visibility = Visibility.Collapsed;
            FeedImage.Source = null;
            var generation = ++_feedPlaybackGeneration;
            _ = OpenFeedPlaybackAsync(post, generation);
            NotifyIndexChanged();
            MaybePrefetchMore();
            return;
        }

        ShowImageFallback(post);
        UpdateFeedAudioDeviceLabel(show: false);
        NotifyIndexChanged();
        MaybePrefetchMore();
    }

    private async Task OpenFeedPlaybackAsync(PostItem post, int generation)
    {
        try
        {
            var playbackUri = ImageCache.IsLocalPath(post.PlaybackUrl)
                ? new Uri(Path.GetFullPath(post.PlaybackUrl), UriKind.Absolute)
                : await _app.MediaPlaybackCache.ResolvePlaybackUriAsync(post.PlaybackUrl).ConfigureAwait(true);

            if (generation != _feedPlaybackGeneration)
            {
                return;
            }

            _playback.Open(playbackUri);
            _playback.IsMuted = _app.Settings.PlaybackMuted;
            _playback.Volume = _app.Settings.PlaybackVolume / 100.0;
        }
        catch
        {
            OpenFeedPlaybackRemoteFallback(post, generation);
        }
    }

    private void OpenFeedPlaybackRemoteFallback(PostItem post, int generation)
    {
        if (generation != _feedPlaybackGeneration)
        {
            return;
        }

        if (!ImageCache.IsLocalPath(post.PlaybackUrl))
        {
            _app.MediaPlaybackCache.Invalidate(post.PlaybackUrl);
        }

        if (MediaUriHelper.TryCreate(post.PlaybackUrl, out var remoteUri))
        {
            try
            {
                _playback.Open(remoteUri);
                _playback.IsMuted = _app.Settings.PlaybackMuted;
                _playback.Volume = _app.Settings.PlaybackVolume / 100.0;
                return;
            }
            catch
            {
                // Fall through to poster image.
            }
        }

        ShowImageFallback(post);
    }

    private void ShowImageFallbackForCurrentPost()
    {
        if (_app.Posts.Count == 0 || _index < 0 || _index >= _app.Posts.Count)
        {
            return;
        }

        ShowImageFallback(_app.Posts[_index]);
    }

    private void ShowImageFallback(PostItem post)
    {
        _playback.Close();
        FeedMedia.Visibility = Visibility.Collapsed;

        var quality = _app.Settings.FeedMediaQuality;
        var mediaUrl = quality == FeedMediaQuality.Full ? post.FullViewerUrl : post.FastViewerUrl;

        FeedImage.Visibility = Visibility.Visible;
        if (MediaUriHelper.TryCreate(mediaUrl, out var imageUri))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = imageUri;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                FeedImage.Source = bitmap;
            }
            catch
            {
                FeedImage.Source = null;
            }
        }
        else
        {
            FeedImage.Source = null;
        }
    }

    private void NotifyIndexChanged() => IndexChanged?.Invoke(this, EventArgs.Empty);

    private void MaybePrefetchMore()
    {
        if (_index >= _app.Posts.Count - 3 && _app.Gallery.HasMorePages)
        {
            _ = _app.Gallery.AppendNextPageAsync();
        }

        _app.Gallery.PreloadViewerNeighbors(_index);
    }

    private void BrowseFeedPanel_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_suppressWheel)
        {
            return;
        }

        _suppressWheel = true;
        try
        {
            if (e.Delta < 0)
            {
                NavigateNext();
            }
            else if (e.Delta > 0)
            {
                NavigatePrevious();
            }
        }
        finally
        {
            _suppressWheel = false;
        }

        e.Handled = true;
    }

    private void UpdateFeedActionButtons(PostItem post)
    {
        var show = post.ShowCloudActions;
        FeedFavoriteButton.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        FeedWatchLaterButton.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (!show)
        {
            return;
        }

        FeedFavoriteButton.Content = post.IsFavorite ? "★ Favorited" : "☆ Favorite";
        FeedWatchLaterButton.Content = post.IsInWatchLater ? "◷ Saved" : "◷ Watch later";
    }

    private async void FeedFavoriteButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_app.Posts.Count == 0 || _index < 0 || _index >= _app.Posts.Count)
        {
            return;
        }

        var post = _app.Posts[_index];
        if (!GalleryActions.EnsureSignedIn(Window.GetWindow(this)!))
        {
            return;
        }

        await GalleryActions.ToggleFavoriteAsync(post);
        UpdateFeedActionButtons(post);
    }

    private async void FeedWatchLaterButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_app.Posts.Count == 0 || _index < 0 || _index >= _app.Posts.Count)
        {
            return;
        }

        var post = _app.Posts[_index];
        if (!GalleryActions.EnsureSignedIn(Window.GetWindow(this)!))
        {
            return;
        }

        await GalleryActions.ToggleWatchLaterAsync(post);
        UpdateFeedActionButtons(post);
    }
}
