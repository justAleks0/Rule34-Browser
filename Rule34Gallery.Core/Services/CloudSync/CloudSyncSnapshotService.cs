using Rule34Gallery.Core.Firebase;
using Rule34Gallery.Core.Services;

namespace Rule34Gallery.Core.CloudSync;

public sealed class CloudSyncSnapshotService
{
    private readonly AppServices _app;

    public CloudSyncSnapshotService(AppServices app)
    {
        _app = app;
    }

    public async Task<CloudSyncSnapshot> FetchCloudSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var library = _app.Library;
        var firestore = library.Firestore
            ?? throw new InvalidOperationException("Sign in to load cloud data.");

        if (!library.IsSignedIn)
        {
            throw new InvalidOperationException("Sign in to load cloud data.");
        }

        var favorites = await firestore.GetFavoritesAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var lists = await firestore.GetListsAsync().ConfigureAwait(false);
        var listPosts = new Dictionary<string, IReadOnlyList<PostItem>>(StringComparer.Ordinal);
        foreach (var list in lists)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(list.Id))
            {
                continue;
            }

            listPosts[list.Id] = await firestore.GetListPostsAsync(list.Id).ConfigureAwait(false);
        }

        var watchLater = listPosts.TryGetValue(SavedList.WatchLaterId, out var wl)
            ? wl
            : [];

        var credentials = await firestore.GetCredentialsAsync().ConfigureAwait(false)
                          ?? new CloudUserCredentials();
        var presets = await firestore.GetSavedTagPresetsAsync().ConfigureAwait(false);
        var forYou = await firestore.GetForYouProfileAsync().ConfigureAwait(false);

        return new CloudSyncSnapshot
        {
            Favorites = favorites,
            Lists = lists,
            ListPosts = listPosts,
            WatchLaterPosts = watchLater,
            Credentials = credentials,
            SavedTagPresets = presets,
            ForYouProfile = forYou,
            ForYouEnabled = forYou?.Enabled ?? false,
            ForYouCloudSyncEnabled = forYou?.CloudSyncEnabled ?? true,
        };
    }

    public CloudSyncSnapshot BuildLocalSnapshot()
    {
        var library = _app.Library;
        var settings = _app.Settings;
        var listPosts = new Dictionary<string, IReadOnlyList<PostItem>>(StringComparer.Ordinal);

        foreach (var list in library.CachedLists)
        {
            if (string.IsNullOrWhiteSpace(list.Id))
            {
                continue;
            }

            if (string.Equals(list.Id, SavedList.WatchLaterId, StringComparison.Ordinal))
            {
                listPosts[list.Id] = library.WatchLaterIds
                    .Select(id => new PostItem { Id = id })
                    .ToList();
            }
            else
            {
                listPosts[list.Id] = [];
            }
        }

        if (!listPosts.ContainsKey(SavedList.WatchLaterId))
        {
            listPosts[SavedList.WatchLaterId] = library.WatchLaterIds
                .Select(id => new PostItem { Id = id })
                .ToList();
        }

        return new CloudSyncSnapshot
        {
            Favorites = library.FavoriteIds.Select(id => new PostItem { Id = id }).ToList(),
            Lists = library.CachedLists.ToList(),
            ListPosts = listPosts,
            WatchLaterPosts = library.WatchLaterIds.Select(id => new PostItem { Id = id }).ToList(),
            Credentials = CloudUserCredentials.FromSettings(settings),
            SavedTagPresets = settings.SavedSearchPresets.ToList(),
            ForYouProfile = _app.ForYou.ExportCloudProfile(),
            ForYouEnabled = settings.ForYouEnabled,
            ForYouCloudSyncEnabled = settings.ForYouCloudSync,
        };
    }
}
