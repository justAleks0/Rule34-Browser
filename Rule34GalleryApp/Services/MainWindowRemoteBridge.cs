using System.Windows;
using Rule34Gallery.Core;
using Rule34Gallery.Core.Remote;
using Rule34Gallery.Core.Services;

namespace Rule34GalleryApp.Services;

public sealed class MainWindowRemoteBridge : IRemoteControlBridge
{
    private readonly MainWindow _window;
    private readonly AppServices _app;

    public MainWindowRemoteBridge(MainWindow window, AppServices app)
    {
        _window = window;
        _app = app;
    }

    public RemoteStateSnapshot CaptureState()
    {
        try
        {
            if (_window.Dispatcher.CheckAccess())
            {
                return BuildStateSafe();
            }

            return _window.Dispatcher.Invoke(BuildStateSafe);
        }
        catch (Exception ex)
        {
            return CreateFallbackState(ex.Message);
        }
    }

    public Task<RemoteCommandResponse> ExecuteAsync(RemoteCommandRequest command, CancellationToken cancellationToken)
    {
        if (_window.Dispatcher.CheckAccess())
        {
            return ExecuteCoreSafeAsync(command);
        }

        return _window.Dispatcher.InvokeAsync(() => ExecuteCoreSafeAsync(command)).Task.Unwrap();
    }

    private async Task<RemoteCommandResponse> ExecuteCoreSafeAsync(RemoteCommandRequest command)
    {
        try
        {
            return await ExecuteCoreAsync(command).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            return RemoteCommandResponse.Fail(ex.Message);
        }
    }

    private RemoteStateSnapshot BuildStateSafe()
    {
        try
        {
            return BuildState();
        }
        catch (Exception ex)
        {
            return CreateFallbackState(ex.Message);
        }
    }

    private RemoteStateSnapshot CreateFallbackState(string status) =>
        new()
        {
            Status = status,
            IncludeTags = _app.Settings.IncludeTags.ToList(),
            RemoteClientActive = RemoteClientSession.IsActive,
            FeedModeAvailable = true,
            ActivePage = _window.ActivePageId.ToLowerInvariant(),
            Library = _window.GetLibraryRemoteState(),
            Local = _window.GetLocalRemoteState(),
            Downloads = _window.GetDownloadsRemoteState(),
        };

    private async Task<RemoteCommandResponse> ExecuteCoreAsync(RemoteCommandRequest command)
    {
        try
        {
            switch (command.Type)
            {
                case RemoteCommands.Search:
                    _ = _app.Gallery.SearchAsync(resetPage: true);
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.SetIncludeTags:
                    _app.Settings.SetIncludeTags(command.Tags ?? []);
                    _app.SaveSettings();
                    _browseRefresh();
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.AddIncludeTag:
                    if (string.IsNullOrWhiteSpace(command.Tag))
                    {
                        return RemoteCommandResponse.Fail("Missing tag.");
                    }

                    _app.Settings.AddIncludeTag(command.Tag);
                    _app.SaveSettings();
                    _browseRefresh();
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.RemoveIncludeTag:
                    if (string.IsNullOrWhiteSpace(command.Tag))
                    {
                        return RemoteCommandResponse.Fail("Missing tag.");
                    }

                    _app.Settings.RemoveIncludeTag(command.Tag);
                    _app.SaveSettings();
                    _browseRefresh();
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.ClearIncludeTags:
                    _app.Settings.SetIncludeTags([]);
                    _app.SaveSettings();
                    _browseRefresh();
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.GoToPage:
                    if (command.Page is not int pageNum || pageNum < 1)
                    {
                        return RemoteCommandResponse.Fail("Missing or invalid page (use 1-based page number).");
                    }

                    _ = _app.Gallery.SearchAtPageAsync(pageNum - 1);
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.FocusBrowse:
                    _window.NavigateToBrowse();
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.NavigatePage:
                    if (string.IsNullOrWhiteSpace(command.Section))
                    {
                        return RemoteCommandResponse.Fail("Missing section (foryou, browse, library, local, downloads, settings, account).");
                    }

                    try
                    {
                        _window.NavigateToSection(command.Section);
                    }
                    catch (ArgumentException ex)
                    {
                        return RemoteCommandResponse.Fail(ex.Message);
                    }

                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.PageNext:
                    _ = _app.Gallery.NextPageAsync();
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.PagePrev:
                    _ = _app.Gallery.PrevPageAsync();
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.OpenPost:
                    if (command.PostId is not int postId || postId <= 0)
                    {
                        return RemoteCommandResponse.Fail("Missing postId.");
                    }

                    var onLocal = _window.ActivePageId.Equals(AppPageIds.Local, StringComparison.OrdinalIgnoreCase);
                    var collection = onLocal ? _app.LocalPosts : _app.Posts;
                    var index = collection.ToList().FindIndex(p => p.Id == postId);
                    if (index < 0)
                    {
                        return RemoteCommandResponse.Fail("Post not in current results.");
                    }

                    if (onLocal)
                    {
                        _window.NavigateToSection("local");
                        _window.OpenLocalViewer(index);
                    }
                    else
                    {
                        _window.NavigateToBrowse();
                        _window.OpenViewer(index);
                    }

                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.ViewerNext:
                    _window.ViewerNavigateNext();
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.ViewerPrev:
                    _window.ViewerNavigatePrevious();
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.ViewerClose:
                    _window.ViewerCloseIfOpen();
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.ViewerPlayPause:
                    if (!_window.ViewerPlaybackActive)
                    {
                        return RemoteCommandResponse.Fail("Viewer playback is not active.");
                    }

                    _window.ViewerPlaybackPlayPause();
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.ViewerPlay:
                    if (!_window.ViewerPlaybackActive)
                    {
                        return RemoteCommandResponse.Fail("Viewer playback is not active.");
                    }

                    _window.ViewerPlaybackPlay();
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.ViewerPause:
                    if (!_window.ViewerPlaybackActive)
                    {
                        return RemoteCommandResponse.Fail("Viewer playback is not active.");
                    }

                    _window.ViewerPlaybackPause();
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.ViewerSeekRelative:
                    if (!_window.ViewerPlaybackActive)
                    {
                        return RemoteCommandResponse.Fail("Viewer playback is not active.");
                    }

                    var offsetSeconds = command.Seconds ?? 0;
                    if (Math.Abs(offsetSeconds) < 0.001)
                    {
                        return RemoteCommandResponse.Fail("Missing seconds value.");
                    }

                    _window.ViewerPlaybackSeekRelativeSeconds(offsetSeconds);
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.ViewerSeekTo:
                    if (!_window.ViewerPlaybackActive)
                    {
                        return RemoteCommandResponse.Fail("Viewer playback is not active.");
                    }

                    if (command.Seconds is not double targetSeconds || targetSeconds < 0)
                    {
                        return RemoteCommandResponse.Fail("Missing or invalid target seconds.");
                    }

                    _window.ViewerPlaybackSeekToSeconds(targetSeconds);
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.ViewerToggleMute:
                    if (!_window.ViewerPlaybackActive)
                    {
                        return RemoteCommandResponse.Fail("Viewer playback is not active.");
                    }

                    _window.ViewerPlaybackToggleMute();
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.ViewerSetMuted:
                    if (!_window.ViewerPlaybackActive)
                    {
                        return RemoteCommandResponse.Fail("Viewer playback is not active.");
                    }

                    if (command.Muted is not bool muted)
                    {
                        return RemoteCommandResponse.Fail("Missing muted value.");
                    }

                    _window.ViewerPlaybackSetMuted(muted);
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.ViewerSetVolume:
                    if (!_window.ViewerPlaybackActive)
                    {
                        return RemoteCommandResponse.Fail("Viewer playback is not active.");
                    }

                    if (command.Volume is not int volume)
                    {
                        return RemoteCommandResponse.Fail("Missing volume value.");
                    }

                    _window.ViewerPlaybackSetVolumePercent(volume);
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.ViewerSetSpeed:
                    if (!_window.ViewerPlaybackActive)
                    {
                        return RemoteCommandResponse.Fail("Viewer playback is not active.");
                    }

                    if (command.Speed is not double speed)
                    {
                        return RemoteCommandResponse.Fail("Missing speed value.");
                    }

                    _window.ViewerPlaybackSetSpeed(speed);
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.SetBrowseLayout:
                    var layout = command.Layout?.Trim().ToLowerInvariant();
                    if (layout is "feed")
                    {
                        _app.Settings.BrowseLayoutMode = BrowseLayoutMode.Feed;
                    }
                    else if (layout is "grid")
                    {
                        _app.Settings.BrowseLayoutMode = BrowseLayoutMode.Grid;
                    }
                    else
                    {
                        return RemoteCommandResponse.Fail("Use layout \"feed\" or \"grid\".");
                    }

                    _app.SaveSettings();
                    _window.NavigateToBrowse();
                    _window.RefreshBrowseFromSettings();
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.FeedNext:
                    if (!_window.IsFeedModeActive)
                    {
                        return RemoteCommandResponse.Fail("Feed mode is not active on the PC.");
                    }

                    _window.FeedNavigateNext();
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.FeedPrev:
                    if (!_window.IsFeedModeActive)
                    {
                        return RemoteCommandResponse.Fail("Feed mode is not active on the PC.");
                    }

                    _window.FeedNavigatePrevious();
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.LibrarySelectTab:
                    if (string.IsNullOrWhiteSpace(command.Tab))
                    {
                        return RemoteCommandResponse.Fail("Missing tab (favorites, watchLater, lists).");
                    }

                    _window.NavigateToSection("library");
                    await _window.RemoteLibrarySelectTabAsync(command.Tab).ConfigureAwait(true);
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.LibrarySelectList:
                    if (string.IsNullOrWhiteSpace(command.ListId))
                    {
                        return RemoteCommandResponse.Fail("Missing listId.");
                    }

                    _window.NavigateToSection("library");
                    await _window.RemoteLibrarySelectListAsync(command.ListId).ConfigureAwait(true);
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.LibraryRefreshMetadata:
                    _window.NavigateToSection("library");
                    await _window.RemoteLibraryRefreshMetadataAsync().ConfigureAwait(true);
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.LocalSelectLibrary:
                    if (string.IsNullOrWhiteSpace(command.LibraryId))
                    {
                        return RemoteCommandResponse.Fail("Missing libraryId.");
                    }

                    _window.NavigateToSection("local");
                    await _window.RemoteLocalSelectLibraryAsync(command.LibraryId).ConfigureAwait(true);
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.LocalSelectTopFilter:
                    if (string.IsNullOrWhiteSpace(command.Filter))
                    {
                        return RemoteCommandResponse.Fail("Missing filter.");
                    }

                    _window.NavigateToSection("local");
                    await _window.RemoteLocalSelectTopFilterAsync(command.Filter).ConfigureAwait(true);
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.LocalSelectLeafFilter:
                    if (string.IsNullOrWhiteSpace(command.Filter))
                    {
                        return RemoteCommandResponse.Fail("Missing filter.");
                    }

                    _window.NavigateToSection("local");
                    await _window.RemoteLocalSelectLeafFilterAsync(command.Filter).ConfigureAwait(true);
                    return RemoteCommandResponse.Success(BuildState());

                case RemoteCommands.DownloadsClearFinished:
                    _window.NavigateToSection("downloads");
                    _window.RemoteDownloadsClearFinished();
                    return RemoteCommandResponse.Success(BuildState());

                default:
                    return RemoteCommandResponse.Fail($"Unknown command: {command.Type}");
            }
        }
        catch (Exception ex)
        {
            return RemoteCommandResponse.Fail(ex.Message);
        }
    }

    private void _browseRefresh() => _window.RefreshBrowseFromSettings();

    private RemoteStateSnapshot BuildState()
    {
        var viewerOpen = _window.IsViewerOpen;
        int? viewerPostId = null;
        var viewerIndex = _window.ViewerIndex;
        if (viewerOpen && viewerIndex >= 0 && viewerIndex < _app.Posts.Count)
        {
            viewerPostId = _app.Posts[viewerIndex].Id;
        }

        var activePage = _window.ActivePageId.ToLowerInvariant();
        var resultPosts = activePage == AppPageIds.Local.ToLowerInvariant() ? _app.LocalPosts : _app.Posts;
        var posts = resultPosts
            .Take(60)
            .Select(p => new RemotePostSummary
            {
                Id = p.Id,
                PreviewUrl = p.PreviewUrl ?? string.Empty,
                Rating = p.Rating,
            })
            .ToList();

        return new RemoteStateSnapshot
        {
            Page = _app.Gallery.CurrentPage,
            Status = _app.Gallery is { } g ? GetStatusText() : string.Empty,
            IncludeTags = _app.Settings.IncludeTags.ToList(),
            ViewerOpen = viewerOpen,
            ViewerPostId = viewerPostId,
            ViewerIndex = viewerIndex,
            ViewerPlayback = new RemoteViewerPlaybackState
            {
                Active = _window.ViewerPlaybackActive,
                IsPlaying = _window.ViewerPlaybackIsPlaying,
                IsMuted = _window.ViewerPlaybackIsMuted,
                Volume = _window.ViewerPlaybackVolumePercent,
                Speed = _window.ViewerPlaybackSpeed,
                PositionSeconds = _window.ViewerPlaybackPositionSeconds,
                DurationSeconds = _window.ViewerPlaybackDurationSeconds,
            },
            Posts = posts,
            RemoteClientActive = RemoteClientSession.IsActive,
            FeedModeAvailable = true,
            BrowseLayout = _app.Settings.BrowseLayoutMode == BrowseLayoutMode.Feed ? "feed" : "grid",
            FeedIndex = _window.FeedIndex,
            FeedPostCount = resultPosts.Count,
            ActivePage = activePage,
            Library = _window.GetLibraryRemoteState(),
            Local = _window.GetLocalRemoteState(),
            Downloads = _window.GetDownloadsRemoteState(),
        };
    }

    private string GetStatusText()
    {
        // GalleryCoordinator status is event-driven; use post count as fallback label.
        var count = _app.Posts.Count;
        var page = _app.Gallery.CurrentPage + 1;
        return count > 0 ? $"Page {page} | {count} results" : "Ready";
    }
}
