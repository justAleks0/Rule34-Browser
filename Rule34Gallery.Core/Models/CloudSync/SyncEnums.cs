namespace Rule34Gallery.Core.CloudSync;

public enum SyncSessionStatus
{
    Idle,
    Syncing,
    Success,
    Failed,
    Partial,
}

public enum SyncNodeStatus
{
    Both,
    LocalOnly,
    CloudOnly,
    Changed,
    Conflict,
}

public enum SyncNodeKind
{
    Category,
    Container,
    Leaf,
}

public enum SyncDirection
{
    Upload,
    Download,
}

public enum SyncApplyMode
{
    ReplaceAll,
    MergeSkipDuplicates,
    SelectItems,
}

public enum SyncDataCategory
{
    Credentials,
    Favorites,
    Lists,
    WatchLater,
    SavedTags,
    ForYou,
}
