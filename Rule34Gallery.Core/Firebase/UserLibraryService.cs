using System.Net.Http;
using Rule34Gallery.Core.Abstractions;
using Rule34Gallery.Core.CloudSync;
using Rule34Gallery.Core.Services;

namespace Rule34Gallery.Core.Firebase;

public sealed class UserLibraryService
{
    private readonly FirebaseConfig? _config;
    private readonly FirebaseAuthService? _auth;
    private readonly FirestoreService? _firestore;
    private readonly IGoogleSignInService _googleSignIn;
    private readonly ISecureCredentialStore _secureStore;
    private readonly string _appDataFolder;

    public UserLibraryService(
        FirebaseConfig? config,
        IGoogleSignInService googleSignIn,
        ISecureCredentialStore secureStore,
        string appDataFolder)
    {
        _config = config;
        _googleSignIn = googleSignIn;
        _secureStore = secureStore;
        _appDataFolder = appDataFolder;
        IsAvailable = config?.IsConfigured == true;
        IsGoogleAvailable = config?.IsGoogleConfigured == true;
        IsGoogleConfigured = config?.IsGoogleConfigured == true;
        IsGoogleReady = config?.IsGoogleReady == true;
        if (!IsAvailable || config is null)
        {
            return;
        }

        _auth = new FirebaseAuthService(config, _secureStore, _appDataFolder);
        _firestore = new FirestoreService(config, _auth);
        _auth.AuthStateChanged += (_, _) => AuthStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool IsAvailable { get; }

    public bool IsGoogleAvailable { get; }

    public bool IsGoogleConfigured { get; }

    public bool IsGoogleReady { get; }

    public bool IsSignedIn => _auth?.CurrentUser is not null;

    public string? CurrentEmail => _auth?.CurrentUser?.Email;

    public string AppDataFolder => _appDataFolder;

    internal FirestoreService? Firestore => _firestore;

    public HashSet<int> FavoriteIds { get; } = [];

    public HashSet<int> WatchLaterIds { get; } = [];

    public List<SavedList> CachedLists { get; } = [];

    public event EventHandler? AuthStateChanged;

    public event EventHandler? LibrarySynced;

    public event EventHandler? CloudCredentialsSynced;

    public Action<PostItem>? RecordFavoriteSignal { get; set; }

    public Action<PostItem>? RecordWatchLaterSignal { get; set; }

    public Func<UserSettings>? GetSettings { get; set; }

    public Action<UserSettings>? SaveSettings { get; set; }

    public HttpClient? Http { get; set; }

    public async Task InitializeAsync()
    {
        if (_auth is null)
        {
            return;
        }

        await _auth.TryRestoreSessionAsync().ConfigureAwait(false);
        await SyncLibraryFromCloudAsync().ConfigureAwait(false);
        await SyncForYouProfileFromCloudAsync().ConfigureAwait(false);
    }

    public async Task SignInAsync(string email, string password)
    {
        EnsureAvailable();
        await _auth!.SignInAsync(email, password).ConfigureAwait(false);
        await SyncLibraryFromCloudAsync().ConfigureAwait(false);
    }

    public async Task SignUpAsync(string email, string password)
    {
        EnsureAvailable();
        await _auth!.SignUpAsync(email, password).ConfigureAwait(false);
        await SyncLibraryFromCloudAsync().ConfigureAwait(false);
    }

    public async Task SignInWithGoogleAsync(CancellationToken cancellationToken = default)
    {
        EnsureAvailable();
        if (!IsGoogleAvailable || _config is null)
        {
            throw new InvalidOperationException(
                "Add googleClientId to firebase-config.json (Firebase → Authentication → Google → Web client ID). " +
                "Also add redirect URI http://127.0.0.1:53123/ in Google Cloud Console for that OAuth client.");
        }

        if (!IsGoogleReady)
        {
            throw new InvalidOperationException(_googleSignIn.BuildClientSecretHelp() ?? "Google sign-in is not configured.");
        }

        var googleIdToken = await _googleSignIn.SignInAsync(
                _config.GoogleClientId,
                _config.GoogleClientSecret,
                cancellationToken)
            .ConfigureAwait(false);
        await _auth!.SignInWithGoogleAsync(googleIdToken).ConfigureAwait(false);
        await SyncLibraryFromCloudAsync().ConfigureAwait(false);
    }

    public void SignOut()
    {
        _auth?.SignOut();
        ClearLocalLibraryCache();
        AuthStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearLocalLibraryCache()
    {
        FavoriteIds.Clear();
        WatchLaterIds.Clear();
        CachedLists.Clear();
    }

    public async Task RefreshLibraryCachesAsync()
    {
        await RefreshFavoritesAsync().ConfigureAwait(false);
        await RefreshListsAsync().ConfigureAwait(false);
        LibrarySynced?.Invoke(this, EventArgs.Empty);
    }

    public async Task SyncLibraryFromCloudAsync()
    {
        await RefreshLibraryCachesAsync().ConfigureAwait(false);
        await SyncCredentialsFromCloudAsync().ConfigureAwait(false);
        await SyncSavedTagPresetsFromCloudAsync().ConfigureAwait(false);
        await SyncForYouProfileFromCloudAsync().ConfigureAwait(false);
    }

    public async Task SyncSavedTagPresetsFromCloudAsync()
    {
        if (!IsSignedIn || _firestore is null || GetSettings is null || SaveSettings is null)
        {
            return;
        }

        try
        {
            var cloud = await _firestore.GetSavedTagPresetsAsync().ConfigureAwait(false);
            var settings = GetSettings();
            settings.SavedSearchPresets = SavedTagPresetSync.Merge(settings.SavedSearchPresets, cloud);
            SaveSettings(settings);
        }
        catch
        {
            // Best-effort cloud merge.
        }
    }

    public async Task SaveSavedTagPresetsToCloudAsync()
    {
        if (!IsSignedIn || _firestore is null || GetSettings is null || SaveSettings is null)
        {
            return;
        }

        try
        {
            var cloud = await _firestore.GetSavedTagPresetsAsync().ConfigureAwait(false);
            var settings = GetSettings();
            var merged = SavedTagPresetSync.Merge(settings.SavedSearchPresets, cloud);
            settings.SavedSearchPresets = merged;
            SaveSettings(settings);
            await _firestore.SyncSavedTagPresetsAsync(merged).ConfigureAwait(false);
        }
        catch
        {
            // Ignore upload failures; local presets remain.
        }
    }

    public async Task<ForYouProfile?> LoadForYouProfileFromCloudAsync()
    {
        if (!IsSignedIn || _firestore is null)
        {
            return null;
        }

        try
        {
            var cloud = await _firestore.GetForYouProfileAsync().ConfigureAwait(false);
            return cloud is null ? null : ForYouProfile.FromCloudProfile(cloud);
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveForYouProfileToCloudAsync(ForYouProfile profile, bool allowEmpty = false)
    {
        if (!IsSignedIn || _firestore is null || GetSettings is null)
        {
            return;
        }

        if (!GetSettings().ForYouCloudSync)
        {
            return;
        }

        try
        {
            var cloud = profile.ToCloudProfile();
            if (!allowEmpty &&
                cloud.Topics.Count == 0 &&
                cloud.Activities.Count == 0 &&
                cloud.SearchLines.Count == 0)
            {
                return;
            }

            await _firestore.SetForYouProfileAsync(cloud).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"For You cloud upload failed: {ex.Message}", ex);
        }
    }

    public async Task ClearForYouProfileFromCloudAsync()
    {
        if (!IsSignedIn || _firestore is null || GetSettings is null)
        {
            return;
        }

        if (!GetSettings().ForYouCloudSync)
        {
            return;
        }

        try
        {
            await _firestore.DeleteForYouProfileAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"For You cloud reset failed: {ex.Message}", ex);
        }
    }

    public async Task SyncForYouProfileFromCloudAsync()
    {
        if (!IsSignedIn || _firestore is null || GetSettings is null || SaveSettings is null)
        {
            return;
        }

        if (!GetSettings().ForYouCloudSync)
        {
            return;
        }

        try
        {
            var cloud = await _firestore.GetForYouProfileAsync().ConfigureAwait(false);
            if (cloud is null)
            {
                return;
            }

            var settings = GetSettings();
            settings.ForYouEnabled = cloud.Enabled;
            settings.ForYouCloudSync = cloud.CloudSyncEnabled;
            SaveSettings(settings);
            CloudCredentialsSynced?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            // Best-effort cloud merge.
        }
    }

    private Task NotifyLibraryChangedAsync() => RefreshLibraryCachesAsync();

    public async Task RefreshFavoritesAsync()
    {
        FavoriteIds.Clear();
        if (!IsSignedIn || _firestore is null)
        {
            return;
        }

        var favorites = await _firestore.GetFavoritesAsync().ConfigureAwait(false);
        foreach (var post in favorites)
        {
            FavoriteIds.Add(post.Id);
        }
    }

    public async Task<IReadOnlyList<PostItem>> LoadFavoritesAsync()
    {
        EnsureSignedIn();
        return await _firestore!.GetFavoritesAsync().ConfigureAwait(false);
    }

    public async Task ToggleFavoriteAsync(PostItem post)
    {
        EnsureSignedIn();
        var add = !FavoriteIds.Contains(post.Id);
        await _firestore!.SetFavoriteAsync(post, add).ConfigureAwait(false);
        if (add)
        {
            FavoriteIds.Add(post.Id);
            post.IsFavorite = true;
        }
        else
        {
            FavoriteIds.Remove(post.Id);
            post.IsFavorite = false;
        }

        if (add)
        {
            RecordFavoriteSignal?.Invoke(post);
        }

        await NotifyLibraryChangedAsync().ConfigureAwait(false);
    }

    public async Task RefreshListsAsync()
    {
        CachedLists.Clear();
        if (!IsSignedIn || _firestore is null)
        {
            WatchLaterIds.Clear();
            return;
        }

        await _firestore.EnsureWatchLaterListAsync().ConfigureAwait(false);
        var lists = await _firestore.GetListsAsync().ConfigureAwait(false);
        CachedLists.AddRange(
            lists.OrderByDescending(l => l.IsSystem).ThenBy(l => l.Name, StringComparer.OrdinalIgnoreCase));
        await RefreshWatchLaterIdsAsync().ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<SavedList>> LoadListsAsync()
    {
        EnsureSignedIn();
        if (CachedLists.Count == 0)
        {
            await RefreshListsAsync().ConfigureAwait(false);
        }

        return CachedLists.ToList();
    }

    public IReadOnlyList<SavedList> UserLists() =>
        CachedLists.Where(l => !l.IsSystem).ToList();

    public async Task<SavedList> CreateListAsync(string name)
    {
        EnsureSignedIn();
        var list = await _firestore!.CreateListAsync(name).ConfigureAwait(false);
        await NotifyLibraryChangedAsync().ConfigureAwait(false);
        return list;
    }

    public async Task DeleteListAsync(string listId)
    {
        EnsureSignedIn();
        await _firestore!.DeleteListAsync(listId).ConfigureAwait(false);
        await NotifyLibraryChangedAsync().ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<PostItem>> LoadListPostsAsync(string listId)
    {
        EnsureSignedIn();
        return await _firestore!.GetListPostsAsync(listId).ConfigureAwait(false);
    }

    public async Task AddToListAsync(string listId, PostItem post)
    {
        EnsureSignedIn();
        if (string.Equals(listId, SavedList.WatchLaterId, StringComparison.Ordinal))
        {
            await AddToWatchLaterAsync(post).ConfigureAwait(false);
            return;
        }

        await _firestore!.AddPostToListAsync(listId, post).ConfigureAwait(false);
        await NotifyLibraryChangedAsync().ConfigureAwait(false);
    }

    public async Task RefreshWatchLaterIdsAsync()
    {
        WatchLaterIds.Clear();
        if (!IsSignedIn || _firestore is null)
        {
            return;
        }

        var posts = await _firestore.GetListPostsAsync(SavedList.WatchLaterId).ConfigureAwait(false);
        foreach (var post in posts)
        {
            WatchLaterIds.Add(post.Id);
        }
    }

    public bool IsInWatchLater(int postId) => WatchLaterIds.Contains(postId);

    public async Task AddToWatchLaterAsync(PostItem post)
    {
        EnsureSignedIn();
        await _firestore!.EnsureWatchLaterListAsync().ConfigureAwait(false);
        await _firestore.AddPostToListAsync(SavedList.WatchLaterId, post).ConfigureAwait(false);
        WatchLaterIds.Add(post.Id);
        post.IsInWatchLater = true;
        RecordWatchLaterSignal?.Invoke(post);
        await NotifyLibraryChangedAsync().ConfigureAwait(false);
    }

    public async Task RemoveFromWatchLaterAsync(int postId)
    {
        EnsureSignedIn();
        await _firestore!.RemovePostFromListAsync(SavedList.WatchLaterId, postId).ConfigureAwait(false);
        WatchLaterIds.Remove(postId);
        await NotifyLibraryChangedAsync().ConfigureAwait(false);
    }

    public async Task ToggleWatchLaterAsync(PostItem post)
    {
        EnsureSignedIn();
        if (WatchLaterIds.Contains(post.Id))
        {
            await RemoveFromWatchLaterAsync(post.Id).ConfigureAwait(false);
            post.IsInWatchLater = false;
        }
        else
        {
            await AddToWatchLaterAsync(post).ConfigureAwait(false);
        }
    }

    public void ApplyFavoriteState(IEnumerable<PostItem> posts)
    {
        foreach (var post in posts)
        {
            post.IsFavorite = FavoriteIds.Contains(post.Id);
        }
    }

    public void ApplyWatchLaterState(IEnumerable<PostItem> posts)
    {
        foreach (var post in posts)
        {
            post.IsInWatchLater = WatchLaterIds.Contains(post.Id);
        }
    }

    public void ApplyCloudLibraryState(IEnumerable<PostItem> posts)
    {
        ApplyFavoriteState(posts);
        ApplyWatchLaterState(posts);
    }

    public async Task<PostItem> RefreshPostMetadataAsync(PostItem post, CancellationToken cancellationToken = default)
    {
        EnsureSignedIn();
        var settings = GetSettings?.Invoke() ?? throw new InvalidOperationException("Settings unavailable.");
        var fresh = await Rule34Api.FetchPostByIdAsync(
                RequireHttp(),
                post.Id,
                settings.UserId,
                settings.ApiKey,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Post #{post.Id} was not found on Rule34.");

        PostMetadataMerge.Merge(post, fresh);
        await PersistRefreshedPostAsync(post).ConfigureAwait(false);
        await NotifyLibraryChangedAsync().ConfigureAwait(false);
        return post;
    }

    public async Task<CloudMetadataRefreshResult> RefreshFavoritesMetadataAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureSignedIn();
        var posts = await LoadFavoritesAsync().ConfigureAwait(false);
        return await RefreshPostsMetadataAsync(
            posts,
            async p => await _firestore!.SetFavoriteAsync(p, true).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<CloudMetadataRefreshResult> RefreshListMetadataAsync(
        string listId,
        CancellationToken cancellationToken = default)
    {
        EnsureSignedIn();
        var posts = await LoadListPostsAsync(listId).ConfigureAwait(false);
        return await RefreshPostsMetadataAsync(
            posts,
            async p =>
            {
                if (string.Equals(listId, SavedList.WatchLaterId, StringComparison.Ordinal))
                {
                    await _firestore!.EnsureWatchLaterListAsync().ConfigureAwait(false);
                }

                await _firestore!.AddPostToListAsync(listId, p).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<CloudMetadataRefreshResult> RefreshPostsMetadataAsync(
        IReadOnlyList<PostItem> posts,
        Func<PostItem, Task> persist,
        CancellationToken cancellationToken)
    {
        var settings = GetSettings?.Invoke() ?? throw new InvalidOperationException("Settings unavailable.");
        if (string.IsNullOrWhiteSpace(settings.UserId) || string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("Add Rule34 API credentials on the Account page to refresh metadata.");
        }

        var updated = 0;
        var failed = 0;
        foreach (var post in posts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var fresh = await Rule34Api.FetchPostByIdAsync(
                        RequireHttp(),
                        post.Id,
                        settings.UserId,
                        settings.ApiKey,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (fresh is null)
                {
                    failed++;
                    continue;
                }

                PostMetadataMerge.Merge(post, fresh);
                await persist(post).ConfigureAwait(false);
                updated++;
            }
            catch
            {
                failed++;
            }
        }

        await NotifyLibraryChangedAsync().ConfigureAwait(false);
        return new CloudMetadataRefreshResult
        {
            Updated = updated,
            Failed = failed,
            Total = posts.Count,
        };
    }

    private async Task PersistRefreshedPostAsync(PostItem post)
    {
        if (FavoriteIds.Contains(post.Id))
        {
            await _firestore!.SetFavoriteAsync(post, true).ConfigureAwait(false);
        }

        if (WatchLaterIds.Contains(post.Id))
        {
            await _firestore!.EnsureWatchLaterListAsync().ConfigureAwait(false);
            await _firestore.AddPostToListAsync(SavedList.WatchLaterId, post).ConfigureAwait(false);
        }
    }

    private HttpClient RequireHttp() =>
        Http ?? throw new InvalidOperationException("HTTP client is not configured.");

    public async Task SyncCredentialsFromCloudAsync()
    {
        if (!IsSignedIn || _firestore is null || GetSettings is null || SaveSettings is null)
        {
            return;
        }

        try
        {
            var settings = GetSettings();
            var local = CloudUserCredentials.FromSettings(settings);
            var cloud = await _firestore.GetCredentialsAsync().ConfigureAwait(false);
            if (cloud is null || !cloud.HasAny)
            {
                if (local.HasAny)
                {
                    await _firestore.SetCredentialsAsync(local).ConfigureAwait(false);
                }

                return;
            }

            if (!local.HasAny)
            {
                cloud.ApplyTo(settings);
            }
            else
            {
                CloudUserCredentials.MergeCombine(local, cloud).ApplyTo(settings);
                await _firestore.SetCredentialsAsync(CloudUserCredentials.FromSettings(settings)).ConfigureAwait(false);
            }

            SaveSettings(settings);
            CloudCredentialsSynced?.Invoke(this, EventArgs.Empty);
        }
        catch
        {
            // Cloud sync is best-effort; local settings remain usable.
        }
    }

    public async Task SaveCredentialsToCloudAsync()
    {
        if (!IsSignedIn || _firestore is null || GetSettings is null)
        {
            return;
        }

        try
        {
            var settings = GetSettings();
            var local = CloudUserCredentials.FromSettings(settings);
            var cloud = await _firestore.GetCredentialsAsync().ConfigureAwait(false);
            var merged = CloudUserCredentials.MergeForCloudUpload(local, cloud);
            merged.ApplyTo(settings);
            SaveSettings?.Invoke(settings);
            await _firestore.SetCredentialsAsync(merged).ConfigureAwait(false);
        }
        catch
        {
            // Ignore upload failures so settings still save locally.
        }
    }

    public async Task<CredentialSyncResult> SyncCredentialsNowAsync()
    {
        if (!IsSignedIn || _firestore is null || GetSettings is null || SaveSettings is null)
        {
            return new CredentialSyncResult { Detail = "Not signed in" };
        }

        var settings = GetSettings();
        var local = CloudUserCredentials.FromSettings(settings);
        var cloud = await _firestore.GetCredentialsAsync().ConfigureAwait(false);

        var uploaded = false;
        var downloaded = false;

        if (cloud is not null && cloud.HasAny && !local.HasAny)
        {
            cloud.ApplyTo(settings);
            SaveSettings(settings);
            downloaded = true;
            CloudCredentialsSynced?.Invoke(this, EventArgs.Empty);
        }
        else if (local.HasAny && (cloud is null || !cloud.HasAny))
        {
            await _firestore.SetCredentialsAsync(local).ConfigureAwait(false);
            uploaded = true;
        }
        else if (local.HasAny && cloud is not null && cloud.HasAny)
        {
            var merged = CloudUserCredentials.MergeCombine(local, cloud);
            merged.ApplyTo(settings);
            SaveSettings(settings);
            await _firestore.SetCredentialsAsync(merged).ConfigureAwait(false);
            uploaded = true;
            downloaded = true;
            CloudCredentialsSynced?.Invoke(this, EventArgs.Empty);
        }

        var detail = uploaded && downloaded
            ? "Merged with cloud"
            : uploaded
                ? "Uploaded to cloud"
                : downloaded
                    ? "Downloaded from cloud"
                    : "No credentials saved yet";

        return new CredentialSyncResult
        {
            Uploaded = uploaded,
            Downloaded = downloaded,
            Detail = detail,
        };
    }

    public Task<CloudSyncResult> SyncCloudNowAsync() =>
        throw new NotSupportedException("Use AppServices.CloudSync.RunSyncAsync() for cloud sync.");

    public async Task SaveCredentialsSnapshotToCloudAsync(CloudUserCredentials credentials)
    {
        EnsureSignedIn();
        await _firestore!.SetCredentialsAsync(credentials).ConfigureAwait(false);
        CloudCredentialsSynced?.Invoke(this, EventArgs.Empty);
    }

    public async Task EnsureFavoriteOnCloudAsync(PostItem post)
    {
        EnsureSignedIn();
        await _firestore!.SetFavoriteAsync(post, isFavorite: true).ConfigureAwait(false);
        FavoriteIds.Add(post.Id);
    }

    public async Task EnsurePostOnListCloudAsync(string listId, PostItem post)
    {
        EnsureSignedIn();
        if (string.Equals(listId, SavedList.WatchLaterId, StringComparison.Ordinal))
        {
            await AddToWatchLaterAsync(post).ConfigureAwait(false);
            return;
        }

        await _firestore!.AddPostToListAsync(listId, post).ConfigureAwait(false);
    }

    public async Task DeleteCloudFavoriteAsync(int postId)
    {
        EnsureSignedIn();
        await _firestore!.SetFavoriteAsync(new PostItem { Id = postId }, isFavorite: false).ConfigureAwait(false);
    }

    public async Task DeleteCloudPresetAsync(string presetId)
    {
        EnsureSignedIn();
        await _firestore!.DeleteSavedTagPresetAsync(presetId).ConfigureAwait(false);
    }

    public async Task RecordSyncSessionAsync(
        SyncDirection direction,
        SyncSessionStatus status,
        SyncDeviceInfo device,
        string? summary = null,
        string? error = null)
    {
        if (!IsSignedIn || _firestore is null || GetSettings is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        device.LastSyncAtUnix = now;
        device.LastSeenAtUnix = now;

        var settings = GetSettings();
        if (status == SyncSessionStatus.Success)
        {
            settings.LastSyncSuccessAtUnix = now;
            settings.LastSyncDeviceId = device.DeviceId;
            settings.LastSyncDeviceLabel = device.DisplayName;
            settings.LastSyncDirection = direction.ToString();
            settings.LastSyncStatus = status.ToString();
            SaveSettings?.Invoke(settings);
        }

        settings.LastSyncAttemptAtUnix = now;
        if (!string.IsNullOrWhiteSpace(error))
        {
            settings.LastSyncError = error;
        }

        if (!string.IsNullOrWhiteSpace(summary))
        {
            settings.LastSyncSummary = summary;
        }

        SaveSettings?.Invoke(settings);

        try
        {
            await _firestore.SetSyncMetaAsync(new FirestoreSyncMeta
            {
                LastFullSyncAtUnix = status == SyncSessionStatus.Success ? now : settings.LastSyncSuccessAtUnix,
                LastSyncDeviceId = device.DeviceId,
                LastSyncDirection = direction.ToString(),
                LastSyncStatus = status.ToString(),
            }).ConfigureAwait(false);
            await _firestore.SetDeviceAsync(device).ConfigureAwait(false);
        }
        catch
        {
            // Best-effort metadata.
        }
    }

    private void EnsureAvailable()
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException(
                "Firebase is not configured. Add firebase-config.json (see firebase-config.example.json).");
        }
    }

    private void EnsureSignedIn()
    {
        EnsureAvailable();
        if (!IsSignedIn)
        {
            throw new InvalidOperationException("Sign in to use favorites and lists.");
        }
    }
}
