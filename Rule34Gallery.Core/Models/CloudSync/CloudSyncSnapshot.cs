using Rule34Gallery.Core.Firebase;

namespace Rule34Gallery.Core.CloudSync;

public sealed class CloudSyncSnapshot
{
    public IReadOnlyList<PostItem> Favorites { get; set; } = [];

    public IReadOnlyList<SavedList> Lists { get; set; } = [];

    public IReadOnlyDictionary<string, IReadOnlyList<PostItem>> ListPosts { get; set; }
        = new Dictionary<string, IReadOnlyList<PostItem>>();

    public IReadOnlyList<PostItem> WatchLaterPosts { get; set; } = [];

    public CloudUserCredentials Credentials { get; set; } = new();

    public IReadOnlyList<SavedTagPreset> SavedTagPresets { get; set; } = [];

    public ForYouCloudProfile? ForYouProfile { get; set; }

    public bool ForYouEnabled { get; set; }

    public bool ForYouCloudSyncEnabled { get; set; } = true;
}
