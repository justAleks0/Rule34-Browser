using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Windows.Media;
using Rule34Gallery.Core;
using Rule34Gallery.Core.Remote;
using Rule34GalleryApp.Services;
using Rule34GalleryApp.Views.Overlays;
using Rule34GalleryApp.Views.Pages;
using Wpf.Ui.Controls;

namespace Rule34GalleryApp;

public partial class MainWindow : FluentWindow
{
    private readonly AppServices _app = AppServices.Current;
    private ForYouPage? _forYouPage;
    private BrowsePage? _browsePage;
    private LibraryPage? _libraryPage;
    private LocalLibraryPage? _localLibraryPage;
    private SettingsPage? _settingsPage;
    private AccountPage? _accountPage;
    private DownloadsPage? _downloadsPage;
    private SyncPage? _syncPage;
    private Type? _activePageType;
    private readonly HashSet<string> _notifiedDownloadOutcomes = [];
    private DateTime _lastForegroundLibrarySync = DateTime.MinValue;
    private static readonly TimeSpan ForegroundLibrarySyncCooldown = TimeSpan.FromSeconds(3);
    private readonly RemoteControlServer _remoteServer;
    private readonly MainWindowRemoteBridge _remoteBridge;
    private readonly System.Windows.Threading.DispatcherTimer _remoteSessionTimer;

    public MainWindow()
    {
        InitializeComponent();
        _remoteBridge = new MainWindowRemoteBridge(this, _app);
        _remoteServer = new RemoteControlServer(() => _remoteBridge);
        _remoteSessionTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _remoteSessionTimer.Tick += (_, _) => OnRemoteSessionTimerTick();
        _remoteSessionTimer.Start();
        _app.Library.AuthStateChanged += (_, _) => Dispatcher.Invoke(OnAuthStateChanged);
        _app.Library.LibrarySynced += (_, _) => Dispatcher.Invoke(OnLibrarySynced);
        _app.CredentialsSynced += (_, _) => Dispatcher.Invoke(ApplySettingsToPages);

        _app.Gallery.StatusChanged += (_, status) =>
        {
            Dispatcher.Invoke(() => _browsePage?.BindStatus(status));
        };

        _app.Gallery.PostsChanged += (_, _) =>
        {
            Dispatcher.Invoke(() =>
            {
                _browsePage?.OnPostsChanged();
                _libraryPage?.OnPostsChanged();
            });
        };

        _app.Navigation.Changed += (_, _) => Dispatcher.Invoke(UpdateNavigationButtons);

        _app.Downloads.JobsChanged += (_, _) => Dispatcher.Invoke(OnDownloadsChanged);

        Loaded += MainWindow_OnLoaded;
        Activated += MainWindow_OnActivated;
        RootNavigation.Navigated += RootNavigation_OnNavigated;
        RootNavigation.SizeChanged += (_, _) => SyncTitleBarInset();
        RootNavigation.LayoutUpdated += (_, _) => SyncTitleBarInset();
        AppTitleBar.SizeChanged += (_, _) => SyncTitleBarInset();
    }

    private async void MainWindow_OnActivated(object? sender, EventArgs e)
    {
        if (!_app.Library.IsSignedIn)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (now - _lastForegroundLibrarySync < ForegroundLibrarySyncCooldown)
        {
            return;
        }

        _lastForegroundLibrarySync = now;
        try
        {
            await _app.Library.SyncLibraryFromCloudAsync();
        }
        catch
        {
            // Best-effort sync when the window gains focus.
        }
    }

    private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        SyncTitleBarInset();
        UpdateNavigationButtons();
        ApplySettingsToPages();
        UpdateVersionBadge();

        if (_app.Library.IsAvailable)
        {
            try
            {
                await _app.Library.InitializeAsync();
                OnAuthStateChanged();
                if (_libraryPage is not null)
                {
                    await _libraryPage.RefreshListsAsync();
                }
            }
            catch
            {
                // Session restore failed silently.
            }
        }

        RootNavigation.Navigate(typeof(BrowsePage));
        ApplyRemoteControlSettings();
    }

    public Task CheckForUpdatesNowAsync() => UpdateBanner.CheckNowAsync();

    private void UpdateVersionBadge()
    {
        try
        {
            var exePath = Assembly.GetExecutingAssembly().Location;
            var productVersion = FileVersionInfo.GetVersionInfo(exePath).ProductVersion;
            VersionBadgeText.Text = string.IsNullOrWhiteSpace(productVersion)
                ? "v?"
                : $"v{productVersion}";
        }
        catch
        {
            VersionBadgeText.Text = "v?";
        }
    }

    private void VersionBadgeText_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        try
        {
            var changelogPath = Path.Combine(AppContext.BaseDirectory, "changelog.md");
            var text = File.Exists(changelogPath)
                ? File.ReadAllText(changelogPath)
                : "changelog.md not found.";

            ChangelogDialog.Show(text);
        }
        catch (Exception ex)
        {
            ChangelogDialog.Show("Could not load changelog: " + ex.Message);
        }
    }

    public void ApplyRemoteControlSettings()
    {
        _app.Settings.EnsureRemoteControlToken();
        _remoteServer.ApplySettings(_app.Settings);
    }

    public void NavigateToForYou() => RootNavigation.Navigate(typeof(ForYouPage));

    public void NavigateToBrowse() => RootNavigation.Navigate(typeof(BrowsePage));

    public string ActivePageId => _activePageType switch
    {
        not null when _activePageType == typeof(ForYouPage) => AppPageIds.ForYou,
        not null when _activePageType == typeof(BrowsePage) => AppPageIds.Browse,
        not null when _activePageType == typeof(SavedTagsPage) => AppPageIds.SavedTags,
        not null when _activePageType == typeof(LibraryPage) => AppPageIds.Library,
        not null when _activePageType == typeof(LocalLibraryPage) => AppPageIds.Local,
        not null when _activePageType == typeof(DownloadsPage) => AppPageIds.Downloads,
        not null when _activePageType == typeof(SyncPage) => AppPageIds.Sync,
        not null when _activePageType == typeof(SettingsPage) => AppPageIds.Settings,
        not null when _activePageType == typeof(AccountPage) => AppPageIds.Account,
        not null when _activePageType == typeof(HelpPage) => AppPageIds.Help,
        _ => AppPageIds.Browse,
    };

    public void NavigateToSection(string section)
    {
        ViewerCloseIfOpen();
        switch (section.Trim().ToLowerInvariant())
        {
            case "browse":
                NavigateToBrowse();
                break;
            case "foryou":
            case "for you":
                NavigateToForYou();
                break;
            case "savedtags":
            case "tagsets":
            case "tag sets":
                RootNavigation.Navigate(typeof(SavedTagsPage));
                break;
            case "library":
                RootNavigation.Navigate(typeof(LibraryPage));
                break;
            case "local":
                RootNavigation.Navigate(typeof(LocalLibraryPage));
                break;
            case "downloads":
                NavigateToDownloads();
                break;
            case "sync":
            case "cloud sync":
                NavigateToSync();
                break;
            case "settings":
                RootNavigation.Navigate(typeof(SettingsPage));
                break;
            case "account":
                RootNavigation.Navigate(typeof(AccountPage));
                break;
            case "help":
            case "help / how to":
                RootNavigation.Navigate(typeof(HelpPage));
                break;
            default:
                throw new ArgumentException($"Unknown section \"{section}\".");
        }
    }

    public void RefreshBrowseFromSettings()
    {
        try
        {
            _browsePage?.ApplySettings();
            _browsePage?.OnPostsChanged();
        }
        catch (Exception ex)
        {
            _app.Messenger.Show("Browse update failed", ex.Message, AppMessageKind.Warning);
        }
    }

    private void OnRemoteSessionTimerTick()
    {
        try
        {
            switch (RemoteClientSession.ConsumeTransition())
            {
                case RemoteSessionTransition.BecameActive:
                    _browsePage?.ApplyRemoteSessionState();
                    break;
                case RemoteSessionTransition.BecameInactive:
                    _browsePage?.ApplyRemoteSessionState();
                    break;
            }
        }
        catch (Exception ex)
        {
            _app.Messenger.Show("Remote session", ex.Message, AppMessageKind.Warning);
        }
    }

    public bool IsViewerOpen => Viewer.IsOpen;

    public int ViewerIndex => Viewer.IsOpen ? Viewer.CurrentIndex : -1;

    public void ViewerNavigateNext() => Viewer.NavigateNext();

    public void ViewerNavigatePrevious() => Viewer.NavigatePrevious();

    public void ViewerCloseIfOpen()
    {
        if (Viewer.IsOpen)
        {
            Viewer.Close();
        }
    }

    public bool ViewerPlaybackActive => Viewer.IsVideoPlaybackActive;

    public bool ViewerPlaybackIsPlaying => Viewer.IsPlaybackPlaying;

    public bool ViewerPlaybackIsMuted => Viewer.IsPlaybackMuted;

    public int ViewerPlaybackVolumePercent => Viewer.PlaybackVolumePercent;

    public double ViewerPlaybackSpeed => Viewer.PlaybackSpeedRatio;

    public double ViewerPlaybackPositionSeconds => Viewer.PlaybackPositionSeconds;

    public double ViewerPlaybackDurationSeconds => Viewer.PlaybackDurationSeconds;

    public void ViewerPlaybackPlayPause() => Viewer.PlaybackPlayPause();

    public void ViewerPlaybackPlay() => Viewer.PlaybackPlay();

    public void ViewerPlaybackPause() => Viewer.PlaybackPause();

    public void ViewerPlaybackSeekRelativeSeconds(double seconds) => Viewer.PlaybackSeekRelativeSeconds(seconds);

    public void ViewerPlaybackSeekToSeconds(double seconds) => Viewer.PlaybackSeekToSeconds(seconds);

    public void ViewerPlaybackToggleMute() => Viewer.PlaybackToggleMute();

    public void ViewerPlaybackSetMuted(bool muted) => Viewer.PlaybackSetMuted(muted);

    public void ViewerPlaybackSetVolumePercent(int percent) => Viewer.PlaybackSetVolumePercent(percent);

    public void ViewerPlaybackSetSpeed(double speed) => Viewer.PlaybackSetSpeed(speed);

    public RemoteControlServer RemoteServer => _remoteServer;

    public bool IsFeedModeActive => _browsePage?.IsFeedModeActive == true;

    public int FeedIndex => _browsePage?.FeedIndex ?? 0;

    public void FeedNavigateNext() => _browsePage?.FeedNavigateNext();

    public void FeedNavigatePrevious() => _browsePage?.FeedNavigatePrevious();

    public RemoteLibraryState GetLibraryRemoteState() =>
        _libraryPage?.CaptureRemoteState() ?? new RemoteLibraryState();

    public RemoteLocalState GetLocalRemoteState() =>
        _localLibraryPage?.CaptureRemoteState() ?? new RemoteLocalState();

    public RemoteDownloadsState GetDownloadsRemoteState() =>
        _downloadsPage?.CaptureRemoteState() ?? new RemoteDownloadsState();

    public Task RemoteLibrarySelectTabAsync(string tab) =>
        _libraryPage?.RemoteSelectTabAsync(tab) ?? Task.FromException(new InvalidOperationException("Open Library on the PC first."));

    public Task RemoteLibrarySelectListAsync(string listId) =>
        _libraryPage?.RemoteSelectListAsync(listId) ?? Task.FromException(new InvalidOperationException("Open Library on the PC first."));

    public Task RemoteLibraryRefreshMetadataAsync() =>
        _libraryPage?.RemoteRefreshMetadataAsync() ?? Task.FromException(new InvalidOperationException("Open Library on the PC first."));

    public Task RemoteLocalSelectLibraryAsync(string libraryId) =>
        _localLibraryPage?.RemoteSelectLibraryAsync(libraryId) ?? Task.FromException(new InvalidOperationException("Open Local on the PC first."));

    public Task RemoteLocalSelectTopFilterAsync(string filter) =>
        _localLibraryPage?.RemoteSelectTopFilterAsync(filter) ?? Task.FromException(new InvalidOperationException("Open Local on the PC first."));

    public Task RemoteLocalSelectLeafFilterAsync(string filter) =>
        _localLibraryPage?.RemoteSelectLeafFilterAsync(filter) ?? Task.FromException(new InvalidOperationException("Open Local on the PC first."));

    public void RemoteDownloadsClearFinished() => _downloadsPage?.RemoteClearFinished();

    public void RefreshRemoteStatusOnSettingsPage()
    {
        _settingsPage?.RefreshRemoteControlUi();
    }

    private void RootNavigation_OnNavigated(NavigationView sender, NavigatedEventArgs e)
    {
        var newPageType = e.Page?.GetType();
        if (!_app.Navigation.IsRestoring &&
            _activePageType is not null &&
            newPageType is not null &&
            _activePageType != newPageType)
        {
            _app.Navigation.Push(NavigationSnapshot.Capture(_app, PageNavigationMap.FromType(_activePageType)));
        }

        _activePageType = newPageType;

        if (e.Page is BrowsePage browse)
        {
            _browsePage = browse;
            browse.ApplySettings();
            browse.BindStatus("Ready");
        }
        else if (e.Page is ForYouPage forYou)
        {
            _forYouPage = forYou;
            _ = forYou.LoadAsync();
        }
        else if (e.Page is SavedTagsPage savedTags)
        {
            savedTags.RefreshUi();
        }
        else if (e.Page is LibraryPage library)
        {
            _libraryPage = library;
            library.SetSignedInState(_app.Library.IsSignedIn);
        }
        else if (e.Page is LocalLibraryPage localLibrary)
        {
            _localLibraryPage = localLibrary;
            localLibrary.RefreshUi();
        }
        else if (e.Page is SettingsPage settings)
        {
            _settingsPage = settings;
            settings.ApplySettings();
        }
        else if (e.Page is AccountPage account)
        {
            _accountPage = account;
            account.RefreshUi();
            account.ApplyCredentials();
        }
        else if (e.Page is DownloadsPage downloads)
        {
            _downloadsPage = downloads;
            downloads.RefreshUi();
        }
        else if (e.Page is SyncPage sync)
        {
            _syncPage = sync;
            sync.RefreshUi();
        }
    }

    public void ApplySettingsToPages()
    {
        _browsePage?.ApplySettings();
        _settingsPage?.ApplySettings();
        _accountPage?.ApplyCredentials();
    }

    private void OnLibrarySynced()
    {
        if (_app.Posts.Count > 0)
        {
            _app.Library.ApplyCloudLibraryState(_app.Posts);
            _browsePage?.OnPostsChanged();
            _libraryPage?.OnPostsChanged();
        }
    }

    private void OnAuthStateChanged()
    {
        _accountPage?.RefreshUi();
        if (_settingsPage is not null)
        {
            _settingsPage.ApplySettings();
        }
        _libraryPage?.SetSignedInState(_app.Library.IsSignedIn);
        if (_app.Library.IsSignedIn && _libraryPage is not null)
        {
            _ = _libraryPage.RefreshListsAsync();
        }
    }

    public void OnAuthCompleted()
    {
        OnAuthStateChanged();
        ApplySettingsToPages();
        _app.Messenger.Show("Signed in", $"Welcome, {_app.Library.CurrentEmail}", AppMessageKind.Info);
    }

    protected override void OnClosed(EventArgs e)
    {
        _remoteServer.Dispose();
        base.OnClosed(e);
    }

    public void OpenViewer(int index, IList<PostItem>? posts = null, string? learningSource = null)
        => Viewer.Open(index, posts, learningSource);

    public void OpenLocalViewer(int index) => Viewer.Open(index, _app.LocalPosts);

    public async Task SearchTagAsync(string tag)
    {
        var trimmed = tag.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return;
        }

        Viewer.Close();
        _app.Settings.AddIncludeTag(trimmed);
        RootNavigation.Navigate(typeof(BrowsePage));
        _browsePage?.ApplySettings();
        await _app.Gallery.SearchAsync(resetPage: true, recordHistory: true);
    }

    public void ShowLoginDialog(string? error = null) => LoginDialog.Show(error);

    public void ShowPresetPicker(PresetPickerKind kind, UserSettings settings, Action onChanged)
        => PresetPicker.Show(kind, settings, onChanged);

    public void ShowTagDiscover(UserSettings settings, Action onChanged)
        => TagDiscover.Show(settings, onChanged);

    public void ShowForYouTopicsManage(
        Func<IReadOnlyList<ForYouTopicProfile>> loadTopics,
        Func<string, double, Task> addTopicAsync,
        Func<ForYouTopicProfile, double, Task> setTopicStrengthAsync,
        Action<ForYouTopicProfile> pinTopic,
        Action<ForYouTopicProfile> hideTopic,
        Action<ForYouTopicProfile> removeTopic,
        Action<ForYouTopicProfile> promoteTopic,
        Func<Task>? refreshPageAsync = null)
        => ForYouManage.ShowTopics(
            loadTopics,
            addTopicAsync,
            setTopicStrengthAsync,
            pinTopic,
            hideTopic,
            removeTopic,
            promoteTopic,
            refreshPageAsync);

    public void ShowForYouSearchLinesManage(
        Func<IReadOnlyList<ForYouSearchLine>> loadLines,
        Func<ForYouSearchLine, Task> runLineAsync,
        Action<ForYouSearchLine> pinLine,
        Action<ForYouSearchLine> hideLine,
        Func<Task>? refreshPageAsync = null)
        => ForYouManage.ShowSearchLines(loadLines, runLineAsync, pinLine, hideLine, refreshPageAsync);

    public void ShowForYouSignalsManage(
        Func<IReadOnlyList<ForYouActivityEntry>> loadSignals,
        Func<ForYouActivityEntry, Task> removeSignalAsync,
        Func<ForYouActivityEntry, Task> boostSignalAsync,
        Func<Task>? refreshPageAsync = null)
        => ForYouManage.ShowSignals(loadSignals, removeSignalAsync, boostSignalAsync, refreshPageAsync);

    public void ShowForYouDataViewer(string text) => ForYouDataViewer.Show(text);

    public void EnqueueDownload(PostItem post)
    {
        try
        {
            var job = _app.Downloads.Enqueue(post);
            if (job is null)
            {
                _app.Messenger.Show(
                    "Download skipped",
                    "Local files cannot be downloaded again.",
                    AppMessageKind.Info);
                return;
            }

            var justQueued = job.Status == DownloadJobStatus.Queued && job.Progress <= 0;
            if (justQueued)
            {
                _app.Messenger.Show(
                    "Download started",
                    $"{job.FileName}\nTrack progress under Downloads in the sidebar.",
                    AppMessageKind.Info);
            }
            else
            {
                _app.Messenger.Show(
                    "Already downloading",
                    $"{job.DisplayPath} is already in the queue ({job.StatusText}).",
                    AppMessageKind.Info);
            }

            OnDownloadsChanged();
        }
        catch (Exception ex)
        {
            _app.Messenger.Show("Download failed", ex.Message, AppMessageKind.Warning);
        }
    }

    public void ShowDownloadManager() => NavigateToDownloads();

    public void NavigateToSync() => RootNavigation.Navigate(typeof(SyncPage));

    public void NavigateToAccount() => RootNavigation.Navigate(typeof(AccountPage));

    private void NavigateToDownloads()
    {
        RootNavigation.Navigate(typeof(DownloadsPage));
    }

    public void EditThumbnail(PostItem post) => ThumbnailPicker.Open(post);

    public BrowsePage? GetBrowsePage() => _browsePage;

    public LibraryPage? GetLibraryPage() => _libraryPage;

    private void MainWindow_OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        => _app.SaveSettings();

    private void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Viewer.IsOpen)
        {
            Viewer.HandleKey(e.Key);
            if (ShouldHandleViewerKey(e.Key))
            {
                e.Handled = true;
            }

            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.Left)
        {
            _ = NavigateBackAsync();
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Alt && e.Key == Key.Right)
        {
            _ = NavigateForwardAsync();
            e.Handled = true;
        }
    }

    private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (!Viewer.IsOpen)
        {
            return;
        }

        if (Viewer.IsVideoPlaybackActive && e.Key is Key.Left or Key.Right)
        {
            Viewer.HandlePlaybackKeyDown(e.Key);
            e.Handled = true;
        }
    }

    private static bool ShouldHandleViewerKey(Key key) =>
        key is Key.Escape or Key.Left or Key.Right or Key.PageUp or Key.PageDown
            or Key.Space or Key.Home or Key.End or Key.B or Key.K or Key.M or Key.L
            or Key.J or Key.OemComma or Key.OemPeriod or Key.OemPlus or Key.Add
            or Key.OemMinus or Key.Subtract or Key.D0 or Key.NumPad0;

    private void SyncTitleBarInset()
    {
        if (!IsLoaded)
        {
            return;
        }

        var paneWidth = ResolveNavigationPaneWidth();
        var margin = new Thickness(paneWidth, 0, 0, 0);
        if (AppTitleBar.Margin != margin)
        {
            AppTitleBar.Margin = margin;
        }
    }

    private double ResolveNavigationPaneWidth()
    {
        var measured = MeasureNavigationPaneWidth(RootNavigation);
        if (measured >= 40)
        {
            return measured;
        }

        return RootNavigation.OpenPaneLength > 0 ? RootNavigation.OpenPaneLength : 220;
    }

    private static double MeasureNavigationPaneWidth(DependencyObject root)
    {
        var best = 0.0;
        WalkVisualTree(root, element =>
        {
            if (element is not FrameworkElement fe || fe.ActualWidth < 40 || fe.ActualHeight < 80)
            {
                return;
            }

            if (fe is NavigationView or System.Windows.Controls.Page or System.Windows.Controls.Frame)
            {
                return;
            }

            var name = fe.Name ?? string.Empty;
            var looksLikePane = name.Contains("Pane", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("Navigation", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("Menu", StringComparison.OrdinalIgnoreCase);
            if (!looksLikePane && fe.ActualWidth > 280)
            {
                return;
            }

            if (fe.ActualWidth > best && fe.ActualWidth <= 320)
            {
                best = fe.ActualWidth;
            }
        });

        return best;
    }

    private static void WalkVisualTree(DependencyObject node, Action<DependencyObject> visit)
    {
        visit(node);
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            WalkVisualTree(VisualTreeHelper.GetChild(node, i), visit);
        }
    }

    private void MainWindow_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.XButton1)
        {
            _ = NavigateBackAsync();
            e.Handled = true;
        }
        else if (e.ChangedButton == MouseButton.XButton2)
        {
            _ = NavigateForwardAsync();
            e.Handled = true;
        }
    }

    private void NavBackButton_OnClick(object sender, RoutedEventArgs e) => _ = NavigateBackAsync();

    private void NavForwardButton_OnClick(object sender, RoutedEventArgs e) => _ = NavigateForwardAsync();

    private void OnDownloadsChanged()
    {
        _downloadsPage?.RefreshUi();
        if (_activePageType == typeof(LocalLibraryPage))
        {
            _localLibraryPage?.RefreshUi();
        }

        NotifyDownloadOutcomes();
    }

    private void NotifyDownloadOutcomes()
    {
        foreach (var job in _app.Downloads.Jobs)
        {
            if (job.Status == DownloadJobStatus.Completed && _notifiedDownloadOutcomes.Add($"{job.Id}:ok"))
            {
                _app.Messenger.Show("Download complete", job.DisplayPath, AppMessageKind.Info);
            }
            else if (job.Status == DownloadJobStatus.Failed && _notifiedDownloadOutcomes.Add($"{job.Id}:fail"))
            {
                var detail = string.IsNullOrWhiteSpace(job.ErrorMessage)
                    ? job.DisplayPath
                    : $"{job.DisplayPath}\n{job.ErrorMessage}";
                _app.Messenger.Show("Download failed", detail, AppMessageKind.Warning);
            }
        }
    }

    private void UpdateNavigationButtons()
    {
        NavBackButton.IsEnabled = _app.Navigation.CanGoBack;
        NavForwardButton.IsEnabled = _app.Navigation.CanGoForward;
    }

    public async Task NavigateBackAsync()
    {
        if (Viewer.IsOpen)
        {
            Viewer.Close();
            return;
        }

        var target = _app.Navigation.GetBackTarget();
        if (target is null)
        {
            return;
        }

        await RestoreSnapshotAsync(target);
        _app.Navigation.CommitBack();
        UpdateNavigationButtons();
    }

    public async Task NavigateForwardAsync()
    {
        if (Viewer.IsOpen)
        {
            Viewer.Close();
            return;
        }

        var target = _app.Navigation.GetForwardTarget();
        if (target is null)
        {
            return;
        }

        await RestoreSnapshotAsync(target);
        _app.Navigation.CommitForward();
        UpdateNavigationButtons();
    }

    private async Task RestoreSnapshotAsync(NavigationSnapshot snapshot)
    {
        _app.Navigation.BeginRestore();
        try
        {
            Viewer.Close();
            snapshot.ApplyTo(_app);

            var pageType = PageNavigationMap.ToType(snapshot.PageId);
            RootNavigation.Navigate(pageType);
            _activePageType = pageType;

            if (snapshot.PageId == AppPageIds.ForYou)
            {
                _forYouPage?.RefreshUi();
            }
            else if (snapshot.PageId == AppPageIds.Browse)
            {
                _browsePage?.ApplySettings();

                if (snapshot.ViewMode == GalleryViewMode.Search)
                {
                    await _app.Gallery.RestoreSearchAsync(snapshot.Page);
                }
            }
            else if (snapshot.PageId == AppPageIds.Library)
            {
                if (snapshot.ViewMode == GalleryViewMode.Favorites)
                {
                    await _app.Gallery.LoadFavoritesAsync(recordHistory: false);
                }
                else if (!string.IsNullOrWhiteSpace(snapshot.ListId))
                {
                    await _app.Gallery.LoadListAsync(
                        snapshot.ListId,
                        snapshot.ListName ?? "List",
                        recordHistory: false);
                }
            }
        }
        finally
        {
            _app.Navigation.EndRestore();
        }
    }
}
