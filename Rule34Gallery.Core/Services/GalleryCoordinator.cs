using System.Collections.ObjectModel;
using Rule34Gallery.Core.Firebase;

namespace Rule34Gallery.Core.Services;

public enum GalleryViewMode
{
    Search,
    Favorites,
    List,
}

public sealed class GalleryCoordinator
{
    private readonly AppServices _app;
    private int _currentPage;
    private bool _hasMorePages;
    private CancellationTokenSource? _preloadCts;
    private Timer? _settingsSaveTimer;
    private bool _isLoadingSettings;

    public GalleryCoordinator(AppServices app) => _app = app;

    public GalleryViewMode ViewMode { get; private set; } = GalleryViewMode.Search;

    public string? SelectedListId { get; private set; }

    public int CurrentPage => _currentPage;

    public bool HasMorePages => _hasMorePages;

    private void RecordNavigation(string pageId)
    {
        if (!_app.Navigation.IsRestoring)
        {
            _app.Navigation.Push(NavigationSnapshot.Capture(_app, pageId));
        }
    }

    public event EventHandler<string>? StatusChanged;

    public event EventHandler? PostsChanged;

    public event EventHandler? ViewModeChanged;

    public void BeginLoadSettings()
    {
        _isLoadingSettings = true;
    }

    public void EndLoadSettings()
    {
        _isLoadingSettings = false;
    }

    public bool IsLoadingSettings => _isLoadingSettings;

    public void ScheduleSaveSettings()
    {
        if (_isLoadingSettings)
        {
            return;
        }

        _settingsSaveTimer?.Dispose();
        _settingsSaveTimer = new Timer(_ =>
        {
            _settingsSaveTimer?.Dispose();
            _settingsSaveTimer = null;
            _app.SaveSettings();
        }, null, 400, Timeout.Infinite);
    }

    public void SaveSettingsNow() => _app.SaveSettings();

    public void SetViewMode(GalleryViewMode mode)
    {
        ViewMode = mode;
        ViewModeChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetSelectedList(string? listId) => SelectedListId = listId;

    public void ResetToSearchView()
    {
        SetViewMode(GalleryViewMode.Search);
        SelectedListId = null;
        SetStatus(_app.Posts.Count > 0 ? $"Page {_currentPage} | Results: {_app.Posts.Count}" : "Ready");
    }

    public async Task SearchAsync(bool resetPage = false, bool recordHistory = true)
    {
        if (resetPage)
        {
            _currentPage = 0;
        }

        try
        {
            SetStatus("Searching...");
            _app.Settings.MigrateLegacyTagsIfNeeded();
            var apiTags = SearchQueryBuilder.BuildApiTags(_app.Settings);
            _app.ForYou.RecordSearch(_app.Settings.GetApiIncludeTags(), $"source:{_app.Settings.ActiveSource}");
            var limit = _app.GetSelectedLimit();
            var userId = _app.Settings.UserId.Trim();
            var apiKey = _app.Settings.ApiKey.Trim();

            if (!Rule34Api.HasCredentials(userId, apiKey))
            {
                SetStatus("API credentials required");
                ShowCredentialsMessage();
                return;
            }

            if (!Rule34Api.LooksLikeUserId(userId))
            {
                SetStatus("Check User ID");
                _app.Messenger.Show(
                    "Check your API credentials",
                    "User ID should be the numeric ID from your account page, not your username.",
                    AppMessageKind.Info);
                return;
            }

            _app.Messenger.Dismiss();
            _app.Messenger.SetCredentialHighlight(false);

            var url = Rule34Api.BuildPostSearchUrl(_currentPage, limit, apiTags, userId, apiKey);
            var json = await Rule34Api.FetchPostSearchJsonAsync(_app.Http, url);
            var posts = Rule34Api.ParsePostSearchResponse(json, out var error);

            if (!string.IsNullOrWhiteSpace(error))
            {
                if (Rule34Api.IsAuthenticationError(error))
                {
                    SetStatus("Invalid API credentials");
                    _app.Messenger.Show(
                        "Check your API credentials",
                        "Rule34 rejected the User ID or API key. Copy both again from Account → Options.",
                        AppMessageKind.Info);
                    return;
                }

                SetStatus("Search failed");
                _app.Messenger.Show("Search failed", error, AppMessageKind.Warning);
                return;
            }

            SetViewMode(GalleryViewMode.Search);
            var filtered = TagBlockFilter.Apply(
                posts.Where(p => PostSearchFilter.Matches(p, _app.Settings)),
                _app.Settings).ToList();
            _hasMorePages = posts.Count >= ParseLimit(limit);
            var status = filtered.Count == posts.Count
                ? $"Page {_currentPage + 1} | {filtered.Count} results"
                : $"Page {_currentPage + 1} | {filtered.Count} shown ({posts.Count} from API)";
            SetPosts(filtered, status);
            ScheduleSaveSettings();

            if (recordHistory)
            {
                RecordNavigation(AppPageIds.Browse);
            }
        }
        catch (Exception ex)
        {
            SetStatus("Search failed");
            _app.Messenger.Show("Search failed", ex.Message, AppMessageKind.Error);
        }
    }

    public Task RestoreSearchAsync(int page) => SearchAtPageAsync(page, recordHistory: false);

    public async Task SearchAtPageAsync(int page, bool recordHistory = true)
    {
        _currentPage = Math.Max(0, page);
        await SearchAsync(resetPage: false, recordHistory: recordHistory);
    }

    public async Task NextPageAsync()
    {
        _currentPage++;
        await SearchAsync(recordHistory: true);
    }

    public async Task PrevPageAsync()
    {
        if (_currentPage == 0)
        {
            return;
        }

        _currentPage--;
        await SearchAsync(recordHistory: true);
    }

    public async Task LoadFavoritesAsync(bool recordHistory = true)
    {
        try
        {
            SetStatus("Loading favorites...");
            var favorites = TagBlockFilter.Apply(
                await _app.Library.LoadFavoritesAsync(),
                _app.Settings).ToList();
            SetViewMode(GalleryViewMode.Favorites);
            SetPosts(favorites, $"Favorites | {favorites.Count} posts");

            if (recordHistory)
            {
                RecordNavigation(AppPageIds.Library);
            }
        }
        catch (Exception ex)
        {
            _app.Messenger.Show("Favorites error", ex.Message, AppMessageKind.Warning);
        }
    }

    public async Task LoadWatchLaterAsync(bool recordHistory = true)
    {
        try
        {
            SetStatus("Loading Watch Later...");
            SelectedListId = SavedList.WatchLaterId;
            SetViewMode(GalleryViewMode.List);
            var posts = TagBlockFilter.Apply(
                await _app.Library.LoadListPostsAsync(SavedList.WatchLaterId),
                _app.Settings).ToList();
            SetPosts(posts, $"Watch Later | {posts.Count} posts");

            if (recordHistory)
            {
                _app.Navigation.Push(NavigationSnapshot.CaptureList(_app, SavedList.WatchLaterId, SavedList.WatchLaterName));
            }
        }
        catch (Exception ex)
        {
            _app.Messenger.Show("Watch Later error", ex.Message, AppMessageKind.Warning);
        }
    }

    public async Task LoadListAsync(string listId, string listName, bool recordHistory = true)
    {
        try
        {
            SetStatus("Loading list...");
            SelectedListId = listId;
            SetViewMode(GalleryViewMode.List);
            var posts = TagBlockFilter.Apply(
                await _app.Library.LoadListPostsAsync(listId),
                _app.Settings).ToList();
            SetPosts(posts, $"List: {listName} | {posts.Count} posts");

            if (recordHistory)
            {
                _app.Navigation.Push(NavigationSnapshot.CaptureList(_app, listId, listName));
            }
        }
        catch (Exception ex)
        {
            _app.Messenger.Show("List error", ex.Message, AppMessageKind.Warning);
        }
    }

    public void SetPosts(IEnumerable<PostItem> posts, string status)
    {
        _app.Posts.Clear();
        foreach (var post in posts)
        {
            _app.Posts.Add(post);
        }

        _app.Library.ApplyCloudLibraryState(_app.Posts);
        SetStatus(status);
        PostsChanged?.Invoke(this, EventArgs.Empty);
        StartImagePreload();
    }

    public async Task AppendNextPageAsync()
    {
        if (!_hasMorePages || ViewMode != GalleryViewMode.Search)
        {
            return;
        }

        var savedPage = _currentPage;
        _currentPage++;
        try
        {
            SetStatus("Loading more...");
            var apiTags = SearchQueryBuilder.BuildApiTags(_app.Settings);
            var limit = _app.GetSelectedLimit();
            var userId = _app.Settings.UserId.Trim();
            var apiKey = _app.Settings.ApiKey.Trim();
            var url = Rule34Api.BuildPostSearchUrl(_currentPage, limit, apiTags, userId, apiKey);
            var json = await Rule34Api.FetchPostSearchJsonAsync(_app.Http, url);
            var posts = Rule34Api.ParsePostSearchResponse(json, out var error);
            if (!string.IsNullOrWhiteSpace(error))
            {
                _currentPage = savedPage;
                SetStatus($"Page {_currentPage + 1} | {_app.Posts.Count} results");
                return;
            }

            var filtered = TagBlockFilter.Apply(
                posts.Where(p => PostSearchFilter.Matches(p, _app.Settings)),
                _app.Settings).ToList();
            _hasMorePages = posts.Count >= ParseLimit(limit);
            var existingIds = new HashSet<int>(_app.Posts.Select(p => p.Id));
            foreach (var post in filtered)
            {
                if (existingIds.Add(post.Id))
                {
                    _app.Posts.Add(post);
                }
            }

            _app.Library.ApplyCloudLibraryState(_app.Posts);
            var total = _app.Posts.Count;
            SetStatus(_hasMorePages
                ? $"Page {_currentPage + 1} | {total} posts · swipe for more"
                : $"Page {_currentPage + 1} | {total} posts · end");
            PostsChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _currentPage = savedPage;
            _app.Messenger.Show("Load more failed", ex.Message, AppMessageKind.Warning);
            SetStatus($"Page {_currentPage + 1} | {_app.Posts.Count} results");
        }
    }

    public void RemovePost(PostItem post)
    {
        _app.Posts.Remove(post);
        PostsChanged?.Invoke(this, EventArgs.Empty);
    }

    public int RemoveBlockedPosts()
    {
        var blocked = _app.Posts.Where(p => TagBlockFilter.PostHasBlockingTag(p, _app.Settings)).ToList();
        foreach (var post in blocked)
        {
            _app.Posts.Remove(post);
        }

        if (blocked.Count > 0)
        {
            PostsChanged?.Invoke(this, EventArgs.Empty);
        }

        return blocked.Count;
    }

    public void NotifyStatus(string status) => StatusChanged?.Invoke(this, status);

    private void SetStatus(string status) => NotifyStatus(status);

    private void ShowCredentialsMessage()
    {
        _app.Messenger.SetCredentialHighlight(true);
        _app.Messenger.Show(
            "API credentials required",
            "Add your User ID and API key on the Account page (from rule34.xxx Account → Options).",
            AppMessageKind.Info);
    }

    private void StartImagePreload()
    {
        _preloadCts?.Cancel();
        _preloadCts = new CancellationTokenSource();
        var token = _preloadCts.Token;

        var thumbnails = _app.Posts
            .Select(p => p.ThumbnailUrl)
            .Where(url => PostMedia.IsRasterImageUrl(url));
        var samples = _app.Posts
            .Select(p => p.FastViewerUrl)
            .Where(url => PostMedia.IsRasterImageUrl(url));

        _ = Task.Run(async () =>
        {
            try
            {
                await _app.ImageCache.WarmAsync(thumbnails, 240, token).ConfigureAwait(false);
                await _app.ImageCache.WarmAsync(samples, 1280, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // New search started.
            }
        }, token);
    }

    public void PreloadViewerNeighbors(int index)
        => PreloadViewerNeighbors(_app.Posts, index);

    public void PreloadViewerNeighbors(IList<PostItem> posts, int index)
    {
        if (posts.Count == 0)
        {
            return;
        }

        index = Math.Clamp(index, 0, posts.Count - 1);
        var urls = new List<string>();
        if (index > 0)
        {
            AddViewerWarmUrls(urls, posts[index - 1]);
        }

        if (index < posts.Count - 1)
        {
            AddViewerWarmUrls(urls, posts[index + 1]);
        }

        _ = _app.ImageCache.WarmAsync(urls.Where(PostMedia.IsRasterImageUrl), 1280, CancellationToken.None);
    }

    private static int ParseLimit(string limit)
        => int.TryParse(limit, out var value) && value > 0 ? value : 50;

    private void AddViewerWarmUrls(List<string> urls, PostItem post)
    {
        if (PostMedia.IsRasterImageUrl(post.FastViewerUrl))
        {
            urls.Add(post.FastViewerUrl);
        }

        if (PostMedia.IsRasterImageUrl(post.PosterUrl) && !urls.Contains(post.PosterUrl))
        {
            urls.Add(post.PosterUrl);
        }

        if (post.IsPlayableMedia && !string.IsNullOrWhiteSpace(post.PlaybackUrl))
        {
            _app.MediaPlaybackCache.Prewarm(post.PlaybackUrl);
        }
    }
}
