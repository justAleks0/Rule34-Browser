using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Rule34GalleryApp.Controls;
using Rule34GalleryApp.Helpers;
using Rule34GalleryApp.Services;
using Rule34Gallery.Core.Services;

namespace Rule34GalleryApp.Views.Overlays;

public partial class ImageViewerOverlay : UserControl
{
    private static readonly TagCategory[] CategoryDisplayOrder =
    [
        TagCategory.Artist,
        TagCategory.Character,
        TagCategory.Copyright,
        TagCategory.General,
        TagCategory.Meta,
    ];

    private static readonly double[] PlaybackSpeeds = [0.5, 0.75, 1.0, 1.25, 1.5, 2.0];
    private static readonly TimeSpan PlaybackChromeHideDelay = TimeSpan.FromSeconds(2.5);
    private const double ZoomStepFactor = 1.2;
    private const double MinZoomScale = 0.02;
    private const double MaxZoomScale = 12.0;

    private readonly AppServices _app = AppServices.Current;
    private readonly ViewerPlaybackController _playback;
    private readonly DispatcherTimer _playbackChromeHideTimer;
    private int _viewerIndex = -1;
    private CancellationTokenSource? _viewerLoadCts;
    private CancellationTokenSource? _tagLoadCts;
    private bool _tagsSidebarVisible;
    private bool _isScrubbing;
    private bool _wasPlayingBeforeScrub;
    private bool _syncingSliderFromPlayback;
    private bool _suppressPlaybackUi;
    private bool _autoPlayPending;
    private string? _playbackRemoteUrl;
    private bool _playbackTriedRemoteFallback;
    private double _zoomScale = 1.0;
    private double _bitmapLogicalWidth;
    private double _bitmapLogicalHeight;
    private bool _fitToWindow = true;
    private bool _isPanning;
    private Point _panStart;
    private double _panStartHorizontalOffset;
    private double _panStartVerticalOffset;
    private string? _pendingBrowserUrl;
    private IList<PostItem> _activePosts = [];
    private PostItem? _engagementPost;
    private DateTimeOffset _engagementStartedAt;
    private bool _playbackCompleted;
    private bool _playbackChromePinned;
    private bool _playbackChromeExpanded;
    private string _viewerLearningSource = ForYouLearningSources.Browse;

    public ImageViewerOverlay()
    {
        InitializeComponent();
        _playbackChromeHideTimer = new DispatcherTimer { Interval = PlaybackChromeHideDelay };
        _playbackChromeHideTimer.Tick += (_, _) => CollapsePlaybackChrome();
        _playback = new ViewerPlaybackController(ViewerMedia);
        _playback.Changed += (_, _) => Dispatcher.Invoke(UpdatePlaybackProgress);
        _playback.Opened += (_, _) => Dispatcher.Invoke(OnPlaybackOpened);
        _playback.Ended += (_, _) => Dispatcher.Invoke(OnPlaybackEnded);
        _playback.MediaFailed += (_, _) => Dispatcher.Invoke(OnPlaybackMediaFailed);
        SeekSlider.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler(SeekThumb_OnDragStarted));
        SeekSlider.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler(SeekThumb_OnDragCompleted));
        ImageScrollViewer.SizeChanged += (_, _) =>
        {
            if (_fitToWindow && ViewerImage.Source is BitmapSource)
            {
                ResetZoomToFit();
            }
        };
    }

    public bool IsOpen => Visibility == Visibility.Visible;

    public int CurrentIndex => _viewerIndex;

    public bool IsVideoPlaybackActive => IsPlaybackActive();

    public bool IsPlaybackPlaying => _playback.IsPlaying;

    public bool IsPlaybackMuted => _playback.IsMuted;

    public int PlaybackVolumePercent => (int)Math.Round(Math.Clamp(_playback.Volume, 0, 1) * 100);

    public double PlaybackSpeedRatio => _playback.SpeedRatio;

    public double PlaybackPositionSeconds => _playback.Position.TotalSeconds;

    public double PlaybackDurationSeconds => _playback.Duration.TotalSeconds;

    public void NavigateNext()
    {
        if (!IsOpen || _viewerIndex >= _activePosts.Count - 1)
        {
            return;
        }

        ShowPost(_viewerIndex + 1);
    }

    public void NavigatePrevious()
    {
        if (!IsOpen || _viewerIndex <= 0)
        {
            return;
        }

        ShowPost(_viewerIndex - 1);
    }

    public void PlaybackPlayPause() => TogglePlayPause();

    public void PlaybackPlay()
    {
        if (!IsPlaybackActive())
        {
            return;
        }

        _playback.Play();
        UpdatePlayPauseButton();
    }

    public void PlaybackPause()
    {
        if (!IsPlaybackActive())
        {
            return;
        }

        _playback.Pause();
        UpdatePlayPauseButton();
    }

    public void PlaybackSeekRelativeSeconds(double seconds)
    {
        if (!IsPlaybackActive())
        {
            return;
        }

        _playback.SeekRelative(TimeSpan.FromSeconds(seconds));
        UpdatePlaybackProgress();
    }

    public void PlaybackSeekToSeconds(double seconds)
    {
        if (!IsPlaybackActive())
        {
            return;
        }

        _playback.SeekTo(TimeSpan.FromSeconds(Math.Max(0, seconds)), resume: _playback.IsPlaying);
        UpdatePlaybackProgress();
    }

    public void PlaybackToggleMute() => ToggleMute();

    public void PlaybackSetMuted(bool muted)
    {
        if (!IsPlaybackActive())
        {
            return;
        }

        _playback.IsMuted = muted;
        _app.Settings.PlaybackMuted = muted;
        if (!muted)
        {
            _playback.Volume = Math.Clamp(VolumeSlider.Value / 100.0, 0, 1);
        }

        UpdateMuteButton();
        SavePlaybackSettings();
    }

    public void PlaybackSetVolumePercent(int percent)
    {
        if (!IsPlaybackActive())
        {
            return;
        }

        var clamped = Math.Clamp(percent, 0, 100);
        VolumeSlider.Value = clamped;
        _playback.Volume = clamped / 100.0;
        _playback.IsMuted = clamped == 0 || _app.Settings.PlaybackMuted;
        _app.Settings.PlaybackMuted = _playback.IsMuted;
        UpdateMuteButton();
        SavePlaybackSettings();
    }

    public void PlaybackSetSpeed(double speed)
    {
        if (!IsPlaybackActive())
        {
            return;
        }

        var clamped = Math.Clamp(speed, 0.1, 4.0);
        _playback.SpeedRatio = clamped;
        // Keep combo index in sync when command hits known presets.
        var presetIndex = Array.FindIndex(PlaybackSpeeds, s => Math.Abs(s - clamped) < 0.001);
        if (presetIndex >= 0)
        {
            SpeedCombo.SelectedIndex = presetIndex;
        }

        _app.Settings.PlaybackSpeedIndex = presetIndex >= 0 ? presetIndex : _app.Settings.PlaybackSpeedIndex;
        _app.SaveSettings();
    }

    public void HandlePlaybackKeyDown(Key key)
    {
        if (!IsPlaybackActive())
        {
            return;
        }

        ShowPlaybackChrome();

        switch (key)
        {
            case Key.Left:
                SeekRelative(TimeSpan.FromSeconds(-5));
                break;
            case Key.Right:
                SeekRelative(TimeSpan.FromSeconds(5));
                break;
        }
    }

    public void Open(int index) => Open(index, null);

    public void Open(int index, IList<PostItem>? posts, string? learningSource = null)
    {
        _viewerLearningSource = learningSource ?? ForYouLearningSources.Browse;
        _activePosts = posts ?? _app.Posts;
        if (_activePosts.Count == 0)
        {
            return;
        }

        ApplyChromeInsets();
        ResetTagsSidebar();
        Visibility = Visibility.Visible;
        ((Storyboard)Resources["ShowViewer"]).Begin(this);
        ShowPost(index);
        Focus();
    }

    public void Close()
    {
        FinalizeViewerEngagement();
        _activePosts = _app.Posts;
        HideBrowserChoice();
        _viewerLoadCts?.Cancel();
        _tagLoadCts?.Cancel();
        StopMedia();
        SetPlaybackControlsVisible(false);
        _playbackChromeHideTimer.Stop();
        _playbackChromePinned = false;
        _playbackChromeExpanded = false;
        _viewerIndex = -1;
        Visibility = Visibility.Collapsed;
        ViewerImage.Source = null;
        ViewerTagsPanel.Children.Clear();
    }

    private void ApplyChromeInsets()
    {
        if (Window.GetWindow(this) is not MainWindow mainWindow)
        {
            return;
        }

        var titleBarHeight = mainWindow.AppTitleBar.ActualHeight;
        if (titleBarHeight <= 0)
        {
            titleBarHeight = 48;
        }

        ViewerContent.Margin = new Thickness(0, titleBarHeight, 0, 0);

        const double titleBarButtonsWidth = 132;
        ViewerToolbarButtons.Margin = new Thickness(0, 0, titleBarButtonsWidth + 12, 0);
    }

    public void HandleKey(Key key)
    {
        if (!IsOpen)
        {
            return;
        }

        if (IsPlaybackActive())
        {
            ShowPlaybackChrome();

            switch (key)
            {
                case Key.Left:
                case Key.Right:
                    // Seeking uses KeyDown so holding the key repeats.
                    return;
                case Key.Space:
                case Key.K:
                    TogglePlayPause();
                    return;
                case Key.M:
                    ToggleMute();
                    return;
                case Key.L:
                    ToggleLoop();
                    return;
                case Key.J:
                case Key.OemComma:
                    SeekRelative(TimeSpan.FromSeconds(-10));
                    return;
                case Key.OemPeriod:
                    SeekRelative(TimeSpan.FromSeconds(10));
                    return;
                case Key.OemPlus:
                case Key.Add:
                case Key.OemMinus:
                case Key.Subtract:
                case Key.D0:
                case Key.NumPad0:
                    return;
            }
        }

        if (!IsPlaybackActive())
        {
            switch (key)
            {
                case Key.OemPlus:
                case Key.Add:
                    ZoomBy(ZoomStepFactor);
                    return;
                case Key.OemMinus:
                case Key.Subtract:
                    ZoomBy(1.0 / ZoomStepFactor);
                    return;
                case Key.D0:
                case Key.NumPad0:
                    ResetZoomToFit();
                    return;
            }
        }

        switch (key)
        {
            case Key.Escape:
                if (BrowserChoiceLayer.Visibility == Visibility.Visible)
                {
                    HideBrowserChoice();
                }
                else
                {
                    Close();
                }

                break;
            case Key.Left:
            case Key.PageUp:
                if (_viewerIndex > 0)
                {
                    ShowPost(_viewerIndex - 1);
                }

                break;
            case Key.Right:
            case Key.PageDown:
                if (_viewerIndex < _activePosts.Count - 1)
                {
                    ShowPost(_viewerIndex + 1);
                }

                break;
            case Key.Space:
                if (_viewerIndex < _activePosts.Count - 1)
                {
                    ShowPost(_viewerIndex + 1);
                }

                break;
            case Key.Home:
                ShowPost(0);
                break;
            case Key.End:
                ShowPost(_activePosts.Count - 1);
                break;
            case Key.B:
                OpenInBrowser();
                break;
        }
    }

    private void ShowPost(int index)
    {
        if (_activePosts.Count == 0)
        {
            Close();
            return;
        }

        FinalizeViewerEngagement();

        _viewerIndex = Math.Clamp(index, 0, _activePosts.Count - 1);
        var post = _activePosts[_viewerIndex];
        _engagementPost = post.IsLocal ? null : post;
        _engagementStartedAt = DateTimeOffset.UtcNow;
        _playbackCompleted = false;

        if (!post.IsLocal && ForYouLearningSources.ShouldLearnFromPassiveView(_viewerLearningSource))
        {
            _app.ForYou.RecordPostOpened(post);
        }

        ViewerTitle.Text = post.IsLocal
            ? (post.Id > 0 && post.Id != post.FileUrl.GetHashCode()
                ? $"Post #{post.Id}"
                : Path.GetFileName(post.FileUrl))
            : $"Post #{post.Id}";
        var mediaLabel = post.IsPlayableMedia ? $"  ·  {post.MediaBadge}" : string.Empty;
        var categoryLabel = post.IsLocal && !string.IsNullOrWhiteSpace(post.LocalCategory)
            ? $"  ·  {LocalLibraryService.FormatCategoryDisplay(post.LocalCategory)}"
            : string.Empty;
        var scoreLabel = post.Score > 0 ? $"  ·  score {post.Score}" : string.Empty;
        ViewerMeta.Text = $"{_viewerIndex + 1} of {_activePosts.Count}  ·  {post.Rating}{categoryLabel}{scoreLabel}{mediaLabel}";
        ViewerPrevButton.IsEnabled = _viewerIndex > 0;
        ViewerNextButton.IsEnabled = _viewerIndex < _activePosts.Count - 1;
        ViewerFavoriteButton.Visibility = post.ShowCloudActions ? Visibility.Visible : Visibility.Collapsed;
        ViewerWatchLaterButton.Visibility = post.ShowCloudActions ? Visibility.Visible : Visibility.Collapsed;
        ViewerAddToListButton.Visibility = post.ShowCloudActions ? Visibility.Visible : Visibility.Collapsed;
        ViewerDownloadButton.Visibility = post.ShowCloudActions ? Visibility.Visible : Visibility.Collapsed;
        ViewerThumbButton.Visibility = post.ShowLocalThumbEdit ? Visibility.Visible : Visibility.Collapsed;
        if (post.ShowCloudActions)
        {
            UpdateFavoriteButton(post);
            UpdateWatchLaterButton(post);
        }

        PopulateTags(post);
        UpdatePlaybackChrome(post.IsPlayableMedia);

        if (!post.IsLocal)
        {
            _app.Gallery.PreloadViewerNeighbors(_activePosts, _viewerIndex);
        }

        _ = LoadViewerContentAsync(post);
    }

    private void PopulateTags(PostItem post)
    {
        _tagLoadCts?.Cancel();

        var tags = post.GetTagList();
        if (tags.Count == 0)
        {
            ViewerTagsPanel.Children.Clear();
            ViewerTagsEmptyText.Visibility = Visibility.Visible;
            ViewerTagsScroll.Visibility = Visibility.Collapsed;
            ToggleTagsButton.Visibility = Visibility.Collapsed;
            return;
        }

        ViewerTagsEmptyText.Visibility = Visibility.Collapsed;
        ViewerTagsScroll.Visibility = Visibility.Visible;
        ToggleTagsButton.Visibility = Visibility.Visible;

        RenderTagsByCategory(post.GetTagCategoryMap());

        if (post.TagInfo is { Count: > 0 } || post.IsLocal)
        {
            return;
        }

        _tagLoadCts = new CancellationTokenSource();
        _ = LoadTagCategoriesAsync(post, _tagLoadCts.Token);
    }

    private async Task LoadTagCategoriesAsync(PostItem post, CancellationToken token)
    {
        try
        {
            var tags = post.GetTagList();
            var resolved = await Rule34Api.ResolveTagCategoriesAsync(
                    _app.Http,
                    tags,
                    _app.Settings.UserId.Trim(),
                    _app.Settings.ApiKey.Trim(),
                    token)
                .ConfigureAwait(true);
            if (token.IsCancellationRequested)
            {
                return;
            }

            RenderTagsByCategory(resolved);
        }
        catch (OperationCanceledException)
        {
            // Navigated away.
        }
        catch
        {
            // Keep the initial grouped layout.
        }
    }

    private void RenderTagsByCategory(IReadOnlyDictionary<string, TagCategory> tagCategories)
    {
        ViewerTagsPanel.Children.Clear();

        var byCategory = tagCategories
            .GroupBy(pair => pair.Value)
            .ToDictionary(group => group.Key, group => group.Select(pair => pair.Key).OrderBy(tag => tag).ToList());

        foreach (var category in CategoryDisplayOrder)
        {
            if (!byCategory.TryGetValue(category, out var tags) || tags.Count == 0)
            {
                continue;
            }

            ViewerTagsPanel.Children.Add(BuildCategorySection(category, tags));
        }
    }

    private UIElement BuildCategorySection(TagCategory category, IReadOnlyList<string> tags)
    {
        var section = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

        section.Children.Add(new TextBlock
        {
            Text = GetCategoryLabel(category),
            Foreground = WpfTagBrushes.Foreground(category),
            FontWeight = FontWeights.SemiBold,
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 4),
        });

        var chips = new WrapPanel();
        foreach (var tag in tags)
        {
            var chip = new TagChip
            {
                TagText = tag,
                Category = category,
                Margin = new Thickness(0, 0, 4, 4),
            };
            chip.TagClicked += ViewerTagChip_OnTagClicked;
            chips.Children.Add(chip);
        }

        section.Children.Add(chips);
        return section;
    }

    private static string GetCategoryLabel(TagCategory category) => category switch
    {
        TagCategory.Artist => "Artist",
        TagCategory.Character => "Character",
        TagCategory.Copyright => "Copyright",
        TagCategory.Meta => "Meta",
        _ => "General",
    };

    private void ToggleTagsButton_OnClick(object sender, RoutedEventArgs e)
    {
        _tagsSidebarVisible = !_tagsSidebarVisible;
        ApplyTagsSidebarVisibility();
    }

    private void ResetTagsSidebar()
    {
        _tagsSidebarVisible = true;
        ApplyTagsSidebarVisibility();
    }

    private void ApplyTagsSidebarVisibility()
    {
        TagsColumn.Width = _tagsSidebarVisible ? new GridLength(220) : new GridLength(0);
        TagsSidebar.Visibility = _tagsSidebarVisible ? Visibility.Visible : Visibility.Collapsed;
        ToggleTagsButton.Content = _tagsSidebarVisible ? "Hide" : "Show";
        ShowTagsButton.Visibility = _tagsSidebarVisible ? Visibility.Collapsed : Visibility.Visible;
        if (_fitToWindow && ViewerImage.Source is BitmapSource)
        {
            ScheduleZoomToFit();
        }
    }

    private async void ViewerTagChip_OnTagClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not TagChip chip || string.IsNullOrWhiteSpace(chip.TagText))
        {
            return;
        }

        if (Window.GetWindow(this) is not MainWindow mainWindow)
        {
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Shift)
        {
            _app.ForYou.RecordBlockedTag(chip.TagText, "viewer-tag-block");
            _app.Settings.AddGlobalBlockedTag(chip.TagText);
            _app.SaveSettings();
            var removed = _app.Gallery.RemoveBlockedPosts();
            _app.Messenger.Show(
                "Always hidden",
                $"Blocked \"{chip.TagText}\" app-wide.{(removed > 0 ? $" Removed {removed} from the current gallery." : string.Empty)}",
                AppMessageKind.Info);

            if (_app.Gallery.ViewMode == GalleryViewMode.Search)
            {
                await _app.Gallery.SearchAsync(resetPage: false, recordHistory: false);
            }

            return;
        }

        _app.ForYou.RecordTagClicked(chip.TagText, "viewer-tag-click");
        await mainWindow.SearchTagAsync(chip.TagText);
    }

    private void FinalizeViewerEngagement()
    {
        if (_engagementPost is null)
        {
            return;
        }

        var dwell = DateTimeOffset.UtcNow - _engagementStartedAt;
        if (ForYouLearningSources.ShouldLearnFromPassiveView(_viewerLearningSource))
        {
            _app.ForYou.RecordViewerEngagement(_engagementPost, dwell, _playbackCompleted);
        }
        _engagementPost = null;
        _playbackCompleted = false;
    }

    private void StopMedia() => _playback.Close();

    private bool IsPlaybackActive() => _playback.IsLoaded;

    private void SetPlaybackControlsVisible(bool visible)
    {
        _playbackChromeHideTimer.Stop();
        _playbackChromePinned = false;
        _playbackChromeExpanded = false;

        if (!visible)
        {
            PlaybackControlsPanel.Visibility = Visibility.Collapsed;
            PlaybackControlsPanel.IsHitTestVisible = false;
            PlaybackProgressHint.Visibility = Visibility.Collapsed;
            return;
        }

        BindPlaybackControlsFromSettings();
        ShowPlaybackChrome(keepVisible: true);
    }

    private void ShowPlaybackChrome(bool keepVisible = false)
    {
        if (!IsPlaybackActive())
        {
            return;
        }

        _playbackChromeExpanded = true;
        PlaybackProgressHint.Visibility = Visibility.Collapsed;
        PlaybackControlsPanel.Visibility = Visibility.Visible;
        PlaybackControlsPanel.IsHitTestVisible = true;

        _playbackChromeHideTimer.Stop();
        if (!keepVisible && !_playbackChromePinned && _playback.IsPlaying && !_isScrubbing)
        {
            _playbackChromeHideTimer.Start();
        }
    }

    private void CollapsePlaybackChrome()
    {
        _playbackChromeHideTimer.Stop();
        if (!IsPlaybackActive() || _playbackChromePinned || _isScrubbing || !_playback.IsPlaying)
        {
            return;
        }

        _playbackChromeExpanded = false;
        PlaybackControlsPanel.Visibility = Visibility.Collapsed;
        PlaybackControlsPanel.IsHitTestVisible = false;
        UpdatePlaybackProgressHint();
    }

    private void SchedulePlaybackChromeHide()
    {
        if (!IsPlaybackActive() || _playbackChromePinned || _isScrubbing || !_playback.IsPlaying)
        {
            return;
        }

        _playbackChromeHideTimer.Stop();
        _playbackChromeHideTimer.Start();
    }

    private void UpdatePlaybackProgressHint()
    {
        if (_playbackChromeExpanded || !IsPlaybackActive())
        {
            PlaybackProgressHint.Visibility = Visibility.Collapsed;
            return;
        }

        var duration = _playback.Duration;
        if (duration <= TimeSpan.Zero)
        {
            PlaybackProgressHint.Visibility = Visibility.Collapsed;
            return;
        }

        PlaybackProgressHint.Visibility = Visibility.Visible;
        var ratio = Math.Clamp(_playback.Position.TotalMilliseconds / duration.TotalMilliseconds, 0, 1);
        PlaybackProgressFill.Width = Math.Max(0, PlaybackProgressHint.ActualWidth * ratio);
    }

    private void PlaybackProgressHint_OnSizeChanged(object sender, SizeChangedEventArgs e)
        => UpdatePlaybackProgressHint();

    private void PlaybackProgressHint_OnMouseEnter(object sender, MouseEventArgs e)
        => ShowPlaybackChrome();

    private void ViewerStage_OnMouseMove(object sender, MouseEventArgs e)
        => ShowPlaybackChrome();

    private void ViewerStage_OnMouseLeave(object sender, MouseEventArgs e)
        => SchedulePlaybackChromeHide();

    private void PlaybackControlsPanel_OnMouseEnter(object sender, MouseEventArgs e)
    {
        _playbackChromePinned = true;
        _playbackChromeHideTimer.Stop();
    }

    private void PlaybackControlsPanel_OnMouseLeave(object sender, MouseEventArgs e)
    {
        _playbackChromePinned = false;
        SchedulePlaybackChromeHide();
    }

    private void UpdatePlaybackChrome(bool isPlayable)
    {
        SetPlaybackControlsVisible(isPlayable);
        ViewerHintPanel.Visibility = isPlayable ? Visibility.Collapsed : Visibility.Visible;
        UpdateZoomChrome(!isPlayable);
        UpdateViewerHint(isPlayable);
    }

    private void UpdateZoomChrome(bool visible)
    {
        var visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        ZoomOutButton.Visibility = visibility;
        ZoomInButton.Visibility = visibility;
        ZoomFitButton.Visibility = visibility;
        ZoomLevelText.Visibility = visibility;
    }

    private void BindPlaybackControlsFromSettings()
    {
        _suppressPlaybackUi = true;
        try
        {
            var speedIndex = Math.Clamp(_app.Settings.PlaybackSpeedIndex, 0, PlaybackSpeeds.Length - 1);
            SpeedCombo.SelectedIndex = speedIndex;
            VolumeSlider.Value = Math.Clamp(_app.Settings.PlaybackVolume, 0, 100);
            LoopCheckBox.IsChecked = _app.Settings.PlaybackLoop;
            UpdateMuteButton();
        }
        finally
        {
            _suppressPlaybackUi = false;
        }
    }

    private void ApplyPlaybackSettings()
    {
        var speedIndex = Math.Clamp(SpeedCombo.SelectedIndex, 0, PlaybackSpeeds.Length - 1);
        var speed = PlaybackSpeeds[speedIndex];
        var volume = Math.Clamp(VolumeSlider.Value / 100.0, 0, 1);
        var muted = _app.Settings.PlaybackMuted || VolumeSlider.Value <= 0;

        _playback.SpeedRatio = speed;
        _playback.Volume = volume;
        _playback.IsMuted = muted;
        UpdateMuteButton();
    }

    private void SavePlaybackSettings()
    {
        _app.Settings.PlaybackMuted = _playback.IsMuted;
        _app.Settings.PlaybackVolume = (int)VolumeSlider.Value;
        _app.Settings.PlaybackLoop = LoopCheckBox.IsChecked == true;
        _app.Settings.PlaybackSpeedIndex = Math.Clamp(SpeedCombo.SelectedIndex, 0, PlaybackSpeeds.Length - 1);
        _app.SaveSettings();
    }

    private void TogglePlayPause()
    {
        if (!IsPlaybackActive())
        {
            return;
        }

        _playback.TogglePlayPause();
        UpdatePlayPauseButton();
        ShowPlaybackChrome(keepVisible: !_playback.IsPlaying);
    }

    private void ToggleMute()
    {
        if (!IsPlaybackActive())
        {
            return;
        }

        if (_playback.IsMuted && VolumeSlider.Value <= 0)
        {
            VolumeSlider.Value = Math.Max(50, _app.Settings.PlaybackVolume);
        }

        _playback.IsMuted = !_playback.IsMuted;
        _app.Settings.PlaybackMuted = _playback.IsMuted;
        if (!_playback.IsMuted)
        {
            _playback.Volume = Math.Clamp(VolumeSlider.Value / 100.0, 0, 1);
        }

        UpdateMuteButton();
        SavePlaybackSettings();
    }

    private void ToggleLoop()
    {
        LoopCheckBox.IsChecked = LoopCheckBox.IsChecked != true;
    }

    private void UpdateMuteButton()
    {
        var muted = _playback.IsMuted || VolumeSlider.Value <= 0;
        MuteButton.Content = muted ? "\uE74F" : "\uE767";
        MuteButton.ToolTip = $"Mute (M)\nAudio: {AudioOutputDeviceHelper.GetDefaultPlaybackDeviceLabel()}";
    }

    private void UpdatePlayPauseButton()
        => PlayPauseButton.Content = _playback.IsPlaying ? "\uE769" : "\uE768";

    private void UpdateViewerHint(bool isPlayable)
    {
        ViewerHintText.Text = isPlayable
            ? "Space play/pause  ·  ← → seek ±5s (hold)  ·  M mute  ·  L loop  ·  Esc"
            : "Wheel zoom  ·  drag pan  ·  +/-  ·  0 fit  ·  dbl-click fit  ·  ← →  ·  Esc";
    }

    private static Uri CreateMediaUri(string playbackUrl)
    {
        if (playbackUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || playbackUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return new Uri(playbackUrl, UriKind.Absolute);
        }

        return new Uri(Path.GetFullPath(playbackUrl), UriKind.Absolute);
    }

    private void SeekRelative(TimeSpan offset)
    {
        if (!IsPlaybackActive())
        {
            return;
        }

        _playback.SeekRelative(offset);
    }

    private void UpdatePlaybackProgress()
    {
        if (!IsPlaybackActive())
        {
            return;
        }

        var duration = _playback.Duration;
        var position = _playback.Position;

        _syncingSliderFromPlayback = true;
        try
        {
            if (duration > TimeSpan.Zero)
            {
                SeekSlider.Maximum = Math.Max(1, duration.TotalMilliseconds);
                if (!_isScrubbing && !_playback.IsScrubbing)
                {
                    SeekSlider.Value = Math.Clamp(position.TotalMilliseconds, 0, SeekSlider.Maximum);
                }
            }
            else
            {
                SeekSlider.Maximum = 1;
                SeekSlider.Value = 0;
            }
        }
        finally
        {
            _syncingSliderFromPlayback = false;
        }

        PlaybackTimeText.Text = duration > TimeSpan.Zero
            ? $"{FormatTime(position)} / {FormatTime(duration)}"
            : FormatTime(position);

        UpdatePlayPauseButton();
        UpdatePlaybackProgressHint();
    }

    private static string FormatTime(TimeSpan time)
    {
        if (time.TotalHours >= 1)
        {
            return $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}";
        }

        return $"{time.Minutes}:{time.Seconds:D2}";
    }

    private bool ShouldLoopPlayback()
    {
        if (LoopCheckBox.IsChecked != true)
        {
            return false;
        }

        if (_viewerIndex < 0 || _viewerIndex >= _activePosts.Count)
        {
            return _app.Settings.PlaybackLoop;
        }

        var post = _activePosts[_viewerIndex];
        return post.MediaType == PostMediaType.Gif || post.MediaType == PostMediaType.Video;
    }

    private void PlayPauseButton_OnClick(object sender, RoutedEventArgs e) => TogglePlayPause();

    private void MuteButton_OnClick(object sender, RoutedEventArgs e) => ToggleMute();

    private void LoopCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressPlaybackUi)
        {
            return;
        }

        SavePlaybackSettings();
    }

    private void VolumeSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_suppressPlaybackUi || !IsPlaybackActive())
        {
            return;
        }

        var volume = Math.Clamp(e.NewValue / 100.0, 0, 1);
        _playback.Volume = volume;
        if (e.NewValue <= 0)
        {
            _playback.IsMuted = true;
            _app.Settings.PlaybackMuted = true;
        }
        else if (!_app.Settings.PlaybackMuted)
        {
            _playback.IsMuted = false;
        }

        UpdateMuteButton();
        SavePlaybackSettings();
    }

    private void SpeedCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressPlaybackUi || SpeedCombo.SelectedIndex < 0)
        {
            return;
        }

        if (IsPlaybackActive())
        {
            var index = Math.Clamp(SpeedCombo.SelectedIndex, 0, PlaybackSpeeds.Length - 1);
            _playback.SpeedRatio = PlaybackSpeeds[index];
        }

        SavePlaybackSettings();
    }

    private void BeginScrubbing()
    {
        _isScrubbing = true;
        _wasPlayingBeforeScrub = _playback.IsPlaying;
        _playback.BeginScrub();
        ShowPlaybackChrome(keepVisible: true);
    }

    private void EndScrubbing()
    {
        if (!_isScrubbing)
        {
            return;
        }

        _isScrubbing = false;
        if (!IsPlaybackActive())
        {
            return;
        }

        _playback.EndScrub(TimeSpan.FromMilliseconds(SeekSlider.Value), _wasPlayingBeforeScrub);
        ShowPlaybackChrome(keepVisible: !_playback.IsPlaying);
    }

    private void SeekSlider_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || !IsPlaybackActive() || SeekSlider.Maximum <= 0)
        {
            return;
        }

        if (IsClickOnThumb(SeekSlider, e.GetPosition(SeekSlider)))
        {
            return;
        }

        var targetMs = PositionToMilliseconds(e.GetPosition(SeekSlider));
        SeekSlider.Value = targetMs;
        _wasPlayingBeforeScrub = _playback.IsPlaying;
        _playback.SeekTo(TimeSpan.FromMilliseconds(targetMs), resume: _wasPlayingBeforeScrub);
        e.Handled = true;
    }

    private static bool IsClickOnThumb(Slider slider, Point positionOnSlider)
    {
        if (slider.ActualWidth <= 0 || slider.Maximum <= 0)
        {
            return false;
        }

        var thumbCenterX = slider.Value / slider.Maximum * slider.ActualWidth;
        const double thumbHitRadius = 16;
        return Math.Abs(positionOnSlider.X - thumbCenterX) <= thumbHitRadius;
    }

    private double PositionToMilliseconds(Point positionOnSlider)
    {
        if (SeekSlider.ActualWidth <= 0)
        {
            return SeekSlider.Value;
        }

        var ratio = Math.Clamp(positionOnSlider.X / SeekSlider.ActualWidth, 0, 1);
        return ratio * SeekSlider.Maximum;
    }

    private void SeekThumb_OnDragStarted(object sender, DragStartedEventArgs e) => BeginScrubbing();

    private void SeekThumb_OnDragCompleted(object sender, DragCompletedEventArgs e) => EndScrubbing();

    private void SeekSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsPlaybackActive() || _syncingSliderFromPlayback)
        {
            return;
        }

        if (_isScrubbing || _playback.IsScrubbing)
        {
            _playback.ScrubTo(TimeSpan.FromMilliseconds(e.NewValue));
            var duration = _playback.Duration;
            var target = TimeSpan.FromMilliseconds(e.NewValue);
            PlaybackTimeText.Text = duration > TimeSpan.Zero
                ? $"{FormatTime(target)} / {FormatTime(duration)}"
                : FormatTime(target);
        }
    }

    private async Task LoadViewerContentAsync(PostItem post)
    {
        _viewerLoadCts?.Cancel();
        _viewerLoadCts = new CancellationTokenSource();
        var token = _viewerLoadCts.Token;

        StopMedia();
        ImageScrollViewer.Visibility = Visibility.Visible;
        ViewerImage.Source = null;
        _bitmapLogicalWidth = 0;
        _bitmapLogicalHeight = 0;
        _zoomScale = 1.0;
        _fitToWindow = true;
        ViewerImage.Width = double.NaN;
        ViewerImage.Height = double.NaN;
        ImageZoomHost.Width = double.NaN;
        ImageZoomHost.Height = double.NaN;

        if (post.IsPlayableMedia)
        {
            await LoadPlayableMediaAsync(post, token).ConfigureAwait(true);
            return;
        }

        await LoadStillImageAsync(post, token).ConfigureAwait(true);
        UpdatePlaybackChrome(false);
    }

    private async Task LoadPlayableMediaAsync(PostItem post, CancellationToken token)
    {
        var playbackUrl = post.PlaybackUrl;
        if (string.IsNullOrWhiteSpace(playbackUrl))
        {
            ViewerLoadingText.Text = "No media URL for this post.";
            ViewerLoadingText.Visibility = Visibility.Visible;
            return;
        }

        _playbackRemoteUrl = ImageCache.IsLocalPath(playbackUrl) ? null : playbackUrl;
        _playbackTriedRemoteFallback = false;

        ViewerLoadingText.Text = post.MediaType == PostMediaType.Video ? "Loading video..." : "Loading GIF...";
        ViewerLoadingText.Visibility = Visibility.Visible;

        _ = LoadPosterAsync(post.PosterUrl, token);

        if (token.IsCancellationRequested)
        {
            return;
        }

        try
        {
            var playbackUri = ImageCache.IsLocalPath(playbackUrl)
                ? CreateMediaUri(playbackUrl)
                : await _app.MediaPlaybackCache.ResolvePlaybackUriAsync(playbackUrl, token).ConfigureAwait(true);

            if (token.IsCancellationRequested)
            {
                return;
            }

            _autoPlayPending = true;
            _playback.Open(playbackUri);
            _ = WaitForPlaybackReadyAsync(token);
        }
        catch (OperationCanceledException)
        {
            _autoPlayPending = false;
        }
        catch
        {
            _autoPlayPending = false;
            if (!token.IsCancellationRequested)
            {
                TryOpenRemotePlaybackFallback(playbackUrl, token);
            }
        }
    }

    private void TryOpenRemotePlaybackFallback(string playbackUrl, CancellationToken token)
    {
        if (_playbackTriedRemoteFallback || string.IsNullOrWhiteSpace(playbackUrl))
        {
            ViewerLoadingText.Text = "Could not play media. Use Open in browser.";
            ViewerLoadingText.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            _playbackTriedRemoteFallback = true;
            _autoPlayPending = true;
            _playback.Open(CreateMediaUri(playbackUrl));
            _ = WaitForPlaybackReadyAsync(token);
        }
        catch
        {
            _autoPlayPending = false;
            ViewerLoadingText.Text = "Could not play media. Use Open in browser.";
            ViewerLoadingText.Visibility = Visibility.Visible;
        }
    }

    private async Task WaitForPlaybackReadyAsync(CancellationToken token)
    {
        for (var i = 0; i < 120 && !token.IsCancellationRequested; i++)
        {
            if (_playback.IsLoaded)
            {
                return;
            }

            if (!_autoPlayPending)
            {
                return;
            }

            await Task.Delay(500, token).ConfigureAwait(true);
        }

        if (!token.IsCancellationRequested && _autoPlayPending && !_playback.IsLoaded)
        {
            Dispatcher.Invoke(OnPlaybackMediaFailed);
        }
    }

    private void OnPlaybackOpened()
    {
        ViewerLoadingText.Visibility = Visibility.Collapsed;
        ImageScrollViewer.Visibility = Visibility.Collapsed;
        ApplyPlaybackSettings();
        UpdatePlaybackChrome(true);
        UpdatePlaybackProgress();

        if (_autoPlayPending)
        {
            _autoPlayPending = false;
            _playback.Play();
            UpdatePlayPauseButton();
        }

        ShowPlaybackChrome();
    }

    private void OnPlaybackMediaFailed()
    {
        if (!_playbackTriedRemoteFallback && !string.IsNullOrWhiteSpace(_playbackRemoteUrl))
        {
            _app.MediaPlaybackCache.Invalidate(_playbackRemoteUrl);
            var token = _viewerLoadCts?.Token ?? CancellationToken.None;
            TryOpenRemotePlaybackFallback(_playbackRemoteUrl, token);
            return;
        }

        _autoPlayPending = false;
        StopMedia();
        ImageScrollViewer.Visibility = Visibility.Visible;
        ViewerLoadingText.Text = "Playback failed. Use Open in browser.";
        ViewerLoadingText.Visibility = Visibility.Visible;
    }

    private void OnPlaybackEnded()
    {
        if (ShouldLoopPlayback())
        {
            _playback.SeekTo(TimeSpan.Zero, resume: true);
            return;
        }

        _playbackCompleted = true;
        UpdatePlayPauseButton();
        UpdatePlaybackProgress();
        ShowPlaybackChrome(keepVisible: true);
    }

    private async Task LoadStillImageAsync(PostItem post, CancellationToken token)
    {
        var fastUrl = post.FastViewerUrl;
        var fullUrl = post.FullViewerUrl;

        if (string.IsNullOrWhiteSpace(fastUrl) && string.IsNullOrWhiteSpace(fullUrl))
        {
            ViewerLoadingText.Text = "No image URL for this post.";
            ViewerLoadingText.Visibility = Visibility.Visible;
            return;
        }

        ViewerLoadingText.Text = "Loading...";
        ViewerLoadingText.Visibility = Visibility.Visible;

        if (ImageCache.TryGet(fastUrl, 1280, out var cached) && cached is not null)
        {
            ViewerImage.Source = cached;
            ViewerLoadingText.Visibility = Visibility.Collapsed;
            ScheduleZoomToFit();
        }
        else if (ImageCache.TryGet(fastUrl, 240, out cached) && cached is not null)
        {
            ViewerImage.Source = cached;
            ScheduleZoomToFit();
        }

        try
        {
            if (PostMedia.IsRasterImageUrl(fastUrl))
            {
                var sample = await ImageCache.LoadAsync(fastUrl, 1280, token).ConfigureAwait(true);
                if (!token.IsCancellationRequested && sample is not null)
                {
                    ViewerImage.Source = sample;
                    ViewerLoadingText.Visibility = Visibility.Collapsed;
                    ScheduleZoomToFit();
                }
            }

            if (string.IsNullOrWhiteSpace(fullUrl) ||
                string.Equals(fullUrl, fastUrl, StringComparison.OrdinalIgnoreCase) ||
                !PostMedia.IsRasterImageUrl(fullUrl))
            {
                return;
            }

            var full = await ImageCache.LoadAsync(fullUrl, null, token).ConfigureAwait(true);
            if (!token.IsCancellationRequested && full is not null)
            {
                ViewerImage.Source = full;
                ViewerLoadingText.Visibility = Visibility.Collapsed;
                ScheduleZoomToFit();
            }
        }
        catch (OperationCanceledException)
        {
            // Navigated away.
        }
        catch
        {
            if (!token.IsCancellationRequested && ViewerImage.Source is null)
            {
                ViewerLoadingText.Text = "Failed to load image.";
                ViewerLoadingText.Visibility = Visibility.Visible;
            }
        }
    }

    private void ScheduleZoomToFit()
    {
        void TryFit()
        {
            if (!IsPlaybackActive() && ViewerImage.Source is BitmapSource)
            {
                ResetZoomToFit();
            }
        }

        Dispatcher.BeginInvoke(TryFit, DispatcherPriority.Loaded);
        Dispatcher.BeginInvoke(TryFit, DispatcherPriority.Render);
    }

    private static Size GetBitmapLogicalSize(BitmapSource bitmap)
    {
        var dpiX = bitmap.DpiX > 0 ? bitmap.DpiX : 96.0;
        var dpiY = bitmap.DpiY > 0 ? bitmap.DpiY : 96.0;
        return new Size(
            bitmap.PixelWidth * 96.0 / dpiX,
            bitmap.PixelHeight * 96.0 / dpiY);
    }

    private void ResetZoomToFit()
    {
        if (ViewerImage.Source is not BitmapSource bitmap)
        {
            return;
        }

        ViewerImage.Stretch = Stretch.Uniform;
        var logical = GetBitmapLogicalSize(bitmap);
        _bitmapLogicalWidth = Math.Max(1, logical.Width);
        _bitmapLogicalHeight = Math.Max(1, logical.Height);

        ImageScrollViewer.UpdateLayout();
        var viewportWidth = ImageScrollViewer.ViewportWidth;
        var viewportHeight = ImageScrollViewer.ViewportHeight;
        if (viewportWidth <= 0 || viewportHeight <= 0)
        {
            ScheduleZoomToFit();
            return;
        }

        var fitScale = Math.Min(
            viewportWidth / _bitmapLogicalWidth,
            viewportHeight / _bitmapLogicalHeight);
        _zoomScale = Math.Clamp(fitScale, MinZoomScale, MaxZoomScale);
        _fitToWindow = true;
        ApplyZoomTransform();
        ImageScrollViewer.ScrollToHorizontalOffset(0);
        ImageScrollViewer.ScrollToVerticalOffset(0);
    }

    private void ApplyZoomTransform()
    {
        if (_bitmapLogicalWidth <= 0 || _bitmapLogicalHeight <= 0)
        {
            return;
        }

        var displayWidth = _bitmapLogicalWidth * _zoomScale;
        var displayHeight = _bitmapLogicalHeight * _zoomScale;

        ViewerImage.Stretch = Stretch.Uniform;
        ViewerImage.Width = displayWidth;
        ViewerImage.Height = displayHeight;
        ImageZoomHost.Width = displayWidth;
        ImageZoomHost.Height = displayHeight;
        ImageZoomHost.LayoutTransform = Transform.Identity;
        ImageScrollViewer.InvalidateMeasure();
        ImageScrollViewer.UpdateLayout();
        ZoomLevelText.Text = $"{_zoomScale * 100:0}%";
    }

    private void EnsureBitmapDimensions()
    {
        if (ViewerImage.Source is not BitmapSource bitmap)
        {
            return;
        }

        var logical = GetBitmapLogicalSize(bitmap);
        _bitmapLogicalWidth = Math.Max(1, logical.Width);
        _bitmapLogicalHeight = Math.Max(1, logical.Height);
    }

    private void ZoomBy(double factor, Point? focusInViewport = null)
    {
        if (ViewerImage.Source is not BitmapSource || IsPlaybackActive())
        {
            return;
        }

        if (_bitmapLogicalWidth <= 0 || _bitmapLogicalHeight <= 0)
        {
            EnsureBitmapDimensions();
        }

        var oldScale = _zoomScale;
        var newScale = Math.Clamp(_zoomScale * factor, MinZoomScale, MaxZoomScale);
        if (Math.Abs(newScale - oldScale) < 0.0001)
        {
            return;
        }

        _fitToWindow = false;

        var focus = focusInViewport ?? new Point(
            ImageScrollViewer.ViewportWidth / 2,
            ImageScrollViewer.ViewportHeight / 2);

        var contentX = (ImageScrollViewer.HorizontalOffset + focus.X) / oldScale;
        var contentY = (ImageScrollViewer.VerticalOffset + focus.Y) / oldScale;

        _zoomScale = newScale;
        ApplyZoomTransform();
        ImageScrollViewer.UpdateLayout();

        ImageScrollViewer.ScrollToHorizontalOffset(Math.Max(0, contentX * newScale - focus.X));
        ImageScrollViewer.ScrollToVerticalOffset(Math.Max(0, contentY * newScale - focus.Y));
    }

    private void ImageScrollViewer_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (IsPlaybackActive() || ViewerImage.Source is not BitmapSource)
        {
            return;
        }

        var factor = e.Delta > 0 ? ZoomStepFactor : 1.0 / ZoomStepFactor;
        ZoomBy(factor, e.GetPosition(ImageScrollViewer));
        e.Handled = true;
    }

    private void ImageScrollViewer_OnPanStart(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || IsPlaybackActive())
        {
            return;
        }

        _isPanning = true;
        _panStart = e.GetPosition(ImageScrollViewer);
        _panStartHorizontalOffset = ImageScrollViewer.HorizontalOffset;
        _panStartVerticalOffset = ImageScrollViewer.VerticalOffset;
        ImageScrollViewer.CaptureMouse();
        ImageScrollViewer.Cursor = Cursors.Hand;
    }

    private void ImageScrollViewer_OnPanMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning)
        {
            return;
        }

        var position = e.GetPosition(ImageScrollViewer);
        ImageScrollViewer.ScrollToHorizontalOffset(_panStartHorizontalOffset + (_panStart.X - position.X));
        ImageScrollViewer.ScrollToVerticalOffset(_panStartVerticalOffset + (_panStart.Y - position.Y));
    }

    private void ImageScrollViewer_OnPanEnd(object sender, MouseButtonEventArgs e)
    {
        if (!_isPanning)
        {
            return;
        }

        _isPanning = false;
        ImageScrollViewer.ReleaseMouseCapture();
        ImageScrollViewer.Cursor = Cursors.Arrow;
    }

    private void ImageScrollViewer_OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (IsPlaybackActive())
        {
            return;
        }

        ResetZoomToFit();
        e.Handled = true;
    }

    private void ZoomInButton_OnClick(object sender, RoutedEventArgs e)
        => ZoomBy(ZoomStepFactor);

    private void ZoomOutButton_OnClick(object sender, RoutedEventArgs e)
        => ZoomBy(1.0 / ZoomStepFactor);

    private void ZoomFitButton_OnClick(object sender, RoutedEventArgs e) => ResetZoomToFit();

    private async Task LoadPosterAsync(string? posterUrl, CancellationToken token)
    {
        if (!PostMedia.IsRasterImageUrl(posterUrl))
        {
            return;
        }

        if (ImageCache.TryGet(posterUrl, 1280, out var cached) && cached is not null)
        {
            ViewerImage.Source = cached;
            return;
        }

        try
        {
            var poster = await ImageCache.LoadAsync(posterUrl, 1280, token).ConfigureAwait(true);
            if (!token.IsCancellationRequested && poster is not null)
            {
                ViewerImage.Source = poster;
            }
        }
        catch (OperationCanceledException)
        {
            // Navigated away.
        }
        catch
        {
            // Poster is optional for video playback.
        }
    }

    private void UpdateFavoriteButton(PostItem post)
        => ViewerFavoriteButton.Content = post.IsFavorite ? "★ Favorited" : "☆ Favorite";

    private void UpdateWatchLaterButton(PostItem post)
        => ViewerWatchLaterButton.Content = post.IsInWatchLater ? "⏱ Watch Later" : "○ Watch Later";

    private void OpenInBrowser()
    {
        if (_viewerIndex < 0 || _viewerIndex >= _activePosts.Count)
        {
            return;
        }

        var post = _activePosts[_viewerIndex];
        if (post.IsLocal)
        {
            OpenLocalInExplorer(post.FileUrl);
            return;
        }

        var url = post.SitePostUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        _pendingBrowserUrl = url;
        BrowserChoiceLayer.Visibility = Visibility.Visible;
    }

    private static void OpenLocalInExplorer(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        try
        {
            if (File.Exists(filePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{filePath}\"",
                    UseShellExecute = true,
                });
            }
            else
            {
                var folder = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = folder,
                        UseShellExecute = true,
                    });
                }
            }
        }
        catch
        {
            // Explorer launch failed.
        }
    }

    private void HideBrowserChoice()
    {
        BrowserChoiceLayer.Visibility = Visibility.Collapsed;
        _pendingBrowserUrl = null;
    }

    private void BrowserChoiceNormal_OnClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_pendingBrowserUrl))
        {
            BrowserLauncher.OpenNormal(_pendingBrowserUrl);
        }

        HideBrowserChoice();
    }

    private void BrowserChoicePrivate_OnClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_pendingBrowserUrl) &&
            !BrowserLauncher.TryOpenPrivate(_pendingBrowserUrl))
        {
            _app.Messenger.Show(
                "Private tab unavailable",
                "Could not open a private window for your default browser. Use Normal tab, or open the post manually in a private window.",
                AppMessageKind.Warning);
        }

        HideBrowserChoice();
    }

    private void BrowserChoiceCancel_OnClick(object sender, RoutedEventArgs e) => HideBrowserChoice();

    private void BrowserChoiceBackdrop_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource == sender)
        {
            HideBrowserChoice();
        }
    }

    private void BrowserChoicePanel_OnMouseDown(object sender, MouseButtonEventArgs e)
        => e.Handled = true;

    private void CloseButton_OnClick(object sender, RoutedEventArgs e) => Close();

    private void ViewerPrevButton_OnClick(object sender, RoutedEventArgs e) => ShowPost(_viewerIndex - 1);

    private void ViewerNextButton_OnClick(object sender, RoutedEventArgs e) => ShowPost(_viewerIndex + 1);

    private void ViewerOpenInBrowser_OnClick(object sender, RoutedEventArgs e) => OpenInBrowser();

    private async void ViewerFavoriteButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewerIndex < 0 || _viewerIndex >= _activePosts.Count)
        {
            return;
        }

        if (!GalleryActions.EnsureSignedIn(Window.GetWindow(this)!))
        {
            return;
        }

        var post = _activePosts[_viewerIndex];
        await GalleryActions.ToggleFavoriteAsync(post);
        UpdateFavoriteButton(post);
    }

    private async void ViewerWatchLaterButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewerIndex < 0 || _viewerIndex >= _activePosts.Count)
        {
            return;
        }

        if (!GalleryActions.EnsureSignedIn(Window.GetWindow(this)!))
        {
            return;
        }

        var post = _activePosts[_viewerIndex];
        await GalleryActions.ToggleWatchLaterAsync(post);
        UpdateWatchLaterButton(post);
    }

    private async void ViewerRefreshMetadataButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewerIndex < 0 || _viewerIndex >= _activePosts.Count)
        {
            return;
        }

        if (!GalleryActions.EnsureSignedIn(Window.GetWindow(this)!))
        {
            return;
        }

        var post = _activePosts[_viewerIndex];
        try
        {
            ViewerRefreshMetadataButton.IsEnabled = false;
            await GalleryActions.RefreshPostMetadataAsync(post);
            PopulateTags(post);
            AppServices.Current.Messenger.Show("Metadata refreshed", $"Updated post #{post.Id}.", AppMessageKind.Info);
        }
        catch (Exception ex)
        {
            AppServices.Current.Messenger.Show("Refresh failed", ex.Message, AppMessageKind.Warning);
        }
        finally
        {
            ViewerRefreshMetadataButton.IsEnabled = true;
        }
    }

    private async void ViewerAddToListButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewerIndex < 0 || _viewerIndex >= _activePosts.Count)
        {
            return;
        }

        await GalleryActions.PromptAddToListAsync(Window.GetWindow(this)!, _activePosts[_viewerIndex]);
    }

    private void ViewerDownloadButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewerIndex < 0 || _viewerIndex >= _activePosts.Count)
        {
            return;
        }

        if (Window.GetWindow(this) is MainWindow main)
        {
            main.EnqueueDownload(_activePosts[_viewerIndex]);
        }
    }

    private void ViewerThumbButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewerIndex < 0 || _viewerIndex >= _activePosts.Count)
        {
            return;
        }

        if (Window.GetWindow(this) is MainWindow main)
        {
            main.EditThumbnail(_activePosts[_viewerIndex]);
        }
    }
}
