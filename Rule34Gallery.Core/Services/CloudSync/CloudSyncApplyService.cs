using Rule34Gallery.Core.Firebase;
using Rule34Gallery.Core.Services;

namespace Rule34Gallery.Core.CloudSync;

public sealed class CloudSyncApplyService
{
    private readonly AppServices _app;

    public CloudSyncApplyService(AppServices app)
    {
        _app = app;
    }

    public async Task ApplyDownloadAsync(CloudSyncSnapshot merged, CancellationToken cancellationToken = default)
    {
        var library = _app.Library;
        var settings = _app.Settings;

        merged.Credentials.ApplyTo(settings);
        settings.SavedSearchPresets = merged.SavedTagPresets.ToList();
        settings.ForYouEnabled = merged.ForYouEnabled;
        settings.ForYouCloudSync = merged.ForYouCloudSyncEnabled;
        _app.SaveSettings();

        if (merged.ForYouProfile is not null)
        {
            _app.ForYou.ImportProfile(ForYouProfile.FromCloudProfile(merged.ForYouProfile));
        }

        library.FavoriteIds.Clear();
        foreach (var post in merged.Favorites)
        {
            library.FavoriteIds.Add(post.Id);
        }

        library.WatchLaterIds.Clear();
        foreach (var post in merged.WatchLaterPosts)
        {
            library.WatchLaterIds.Add(post.Id);
        }

        await library.RefreshLibraryCachesAsync().ConfigureAwait(false);
    }

    public async Task ApplyUploadAsync(CloudSyncSnapshot merged, CancellationToken cancellationToken = default)
    {
        var library = _app.Library;
        if (!library.IsSignedIn)
        {
            throw new InvalidOperationException("Sign in to upload.");
        }

        await library.SaveCredentialsSnapshotToCloudAsync(merged.Credentials).ConfigureAwait(false);

        foreach (var favorite in merged.Favorites)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await library.EnsureFavoriteOnCloudAsync(favorite).ConfigureAwait(false);
        }

        foreach (var list in merged.Lists)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(list.Id))
            {
                continue;
            }

            if (!merged.ListPosts.TryGetValue(list.Id, out var posts))
            {
                continue;
            }

            foreach (var post in posts)
            {
                await library.EnsurePostOnListCloudAsync(list.Id, post).ConfigureAwait(false);
            }
        }

        _app.Settings.SavedSearchPresets = merged.SavedTagPresets.ToList();
        await library.SaveSavedTagPresetsToCloudAsync().ConfigureAwait(false);

        if (merged.ForYouProfile is not null && _app.ForYou.CloudSyncEnabled)
        {
            var profile = ForYouProfile.FromCloudProfile(merged.ForYouProfile);
            await library.SaveForYouProfileToCloudAsync(profile, allowEmpty: false).ConfigureAwait(false);
        }
    }
}
