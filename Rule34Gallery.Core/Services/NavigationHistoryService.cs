namespace Rule34Gallery.Core.Services;

public sealed class NavigationSnapshot
{
    public required string PageId { get; init; }

    public GalleryViewMode ViewMode { get; init; }

    public string Tags { get; init; } = string.Empty;

    public List<string> IncludeTags { get; init; } = [];

    public List<string> BlacklistTags { get; init; } = [];

    public List<string> ActiveBlacklistPresetIds { get; init; } = [];

    public List<string> ActiveSearchPresetIds { get; init; } = [];

    public int Page { get; init; }

    public bool FilterAi { get; init; }

    public int LimitIndex { get; init; }

    public bool RatingSafe { get; init; } = true;

    public bool RatingQuestionable { get; init; } = true;

    public bool RatingExplicit { get; init; } = true;

    public MediaFilterMode MediaFilter { get; init; }

    public SearchSortMode SortMode { get; init; }

    public int MinScore { get; init; }

    public int MinWidth { get; init; }

    public int MinHeight { get; init; }

    public string ArtistFilter { get; init; } = string.Empty;

    public string CharacterFilter { get; init; } = string.Empty;

    public string CopyrightFilter { get; init; } = string.Empty;

    public string? ListId { get; init; }

    public string? ListName { get; init; }

    public static NavigationSnapshot Capture(AppServices app, string pageId) => new()
    {
        PageId = pageId,
        ViewMode = app.Gallery.ViewMode,
        Tags = app.Settings.Tags,
        IncludeTags = app.Settings.IncludeTags.ToList(),
        BlacklistTags = app.Settings.BlacklistTags.ToList(),
        ActiveBlacklistPresetIds = app.Settings.ActiveBlacklistPresetIds.ToList(),
        ActiveSearchPresetIds = app.Settings.ActiveSearchPresetIds.ToList(),
        Page = app.Gallery.CurrentPage,
        FilterAi = app.Settings.FilterAi,
        LimitIndex = app.Settings.LimitIndex,
        RatingSafe = app.Settings.RatingSafe,
        RatingQuestionable = app.Settings.RatingQuestionable,
        RatingExplicit = app.Settings.RatingExplicit,
        MediaFilter = app.Settings.MediaFilter,
        SortMode = app.Settings.SortMode,
        MinScore = app.Settings.MinScore,
        MinWidth = app.Settings.MinWidth,
        MinHeight = app.Settings.MinHeight,
        ArtistFilter = app.Settings.ArtistFilter,
        CharacterFilter = app.Settings.CharacterFilter,
        CopyrightFilter = app.Settings.CopyrightFilter,
        ListId = app.Gallery.SelectedListId,
        ListName = null,
    };

    public static NavigationSnapshot CaptureList(AppServices app, string listId, string listName) => new()
    {
        PageId = AppPageIds.Library,
        ViewMode = GalleryViewMode.List,
        Tags = app.Settings.Tags,
        IncludeTags = app.Settings.IncludeTags.ToList(),
        BlacklistTags = app.Settings.BlacklistTags.ToList(),
        ActiveBlacklistPresetIds = app.Settings.ActiveBlacklistPresetIds.ToList(),
        ActiveSearchPresetIds = app.Settings.ActiveSearchPresetIds.ToList(),
        Page = 0,
        FilterAi = app.Settings.FilterAi,
        LimitIndex = app.Settings.LimitIndex,
        RatingSafe = app.Settings.RatingSafe,
        RatingQuestionable = app.Settings.RatingQuestionable,
        RatingExplicit = app.Settings.RatingExplicit,
        MediaFilter = app.Settings.MediaFilter,
        SortMode = app.Settings.SortMode,
        MinScore = app.Settings.MinScore,
        MinWidth = app.Settings.MinWidth,
        MinHeight = app.Settings.MinHeight,
        ArtistFilter = app.Settings.ArtistFilter,
        CharacterFilter = app.Settings.CharacterFilter,
        CopyrightFilter = app.Settings.CopyrightFilter,
        ListId = listId,
        ListName = listName,
    };

    public void ApplyTo(AppServices app)
    {
        app.Settings.Tags = Tags;
        app.Settings.IncludeTags = IncludeTags.ToList();
        app.Settings.BlacklistTags = BlacklistTags.ToList();
        app.Settings.ActiveBlacklistPresetIds = ActiveBlacklistPresetIds.ToList();
        app.Settings.ActiveSearchPresetIds = ActiveSearchPresetIds.ToList();
        app.Settings.FilterAi = FilterAi;
        app.Settings.LimitIndex = LimitIndex;
        app.Settings.RatingSafe = RatingSafe;
        app.Settings.RatingQuestionable = RatingQuestionable;
        app.Settings.RatingExplicit = RatingExplicit;
        app.Settings.MediaFilter = MediaFilter;
        app.Settings.SortMode = SortMode;
        app.Settings.MinScore = MinScore;
        app.Settings.MinWidth = MinWidth;
        app.Settings.MinHeight = MinHeight;
        app.Settings.ArtistFilter = ArtistFilter;
        app.Settings.CharacterFilter = CharacterFilter;
        app.Settings.CopyrightFilter = CopyrightFilter;
        app.Settings.SyncTagsString();
    }

    public bool Matches(NavigationSnapshot other) =>
        string.Equals(PageId, other.PageId, StringComparison.Ordinal) &&
        ViewMode == other.ViewMode &&
        Page == other.Page &&
        FilterAi == other.FilterAi &&
        LimitIndex == other.LimitIndex &&
        RatingSafe == other.RatingSafe &&
        RatingQuestionable == other.RatingQuestionable &&
        RatingExplicit == other.RatingExplicit &&
        MediaFilter == other.MediaFilter &&
        SortMode == other.SortMode &&
        MinScore == other.MinScore &&
        MinWidth == other.MinWidth &&
        MinHeight == other.MinHeight &&
        string.Equals(ArtistFilter, other.ArtistFilter, StringComparison.Ordinal) &&
        string.Equals(CharacterFilter, other.CharacterFilter, StringComparison.Ordinal) &&
        string.Equals(CopyrightFilter, other.CopyrightFilter, StringComparison.Ordinal) &&
        string.Equals(Tags, other.Tags, StringComparison.Ordinal) &&
        string.Equals(ListId, other.ListId, StringComparison.Ordinal) &&
        IncludeTags.SequenceEqual(other.IncludeTags, StringComparer.OrdinalIgnoreCase) &&
        BlacklistTags.SequenceEqual(other.BlacklistTags, StringComparer.OrdinalIgnoreCase) &&
        ActiveBlacklistPresetIds.SequenceEqual(other.ActiveBlacklistPresetIds, StringComparer.OrdinalIgnoreCase) &&
        ActiveSearchPresetIds.SequenceEqual(other.ActiveSearchPresetIds, StringComparer.OrdinalIgnoreCase);
}

public sealed class NavigationHistoryService
{
    private readonly List<NavigationSnapshot> _entries = [];
    private int _index = -1;

    public bool IsRestoring { get; private set; }

    public bool CanGoBack => _index > 0;

    public bool CanGoForward => _index >= 0 && _index < _entries.Count - 1;

    public event EventHandler? Changed;

    public void BeginRestore() => IsRestoring = true;

    public void EndRestore() => IsRestoring = false;

    public void Push(NavigationSnapshot snapshot)
    {
        if (IsRestoring)
        {
            return;
        }

        if (_index >= 0 && _entries[_index].Matches(snapshot))
        {
            return;
        }

        if (_index < _entries.Count - 1)
        {
            _entries.RemoveRange(_index + 1, _entries.Count - _index - 1);
        }

        _entries.Add(snapshot);
        _index = _entries.Count - 1;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public NavigationSnapshot? GetBackTarget() => CanGoBack ? _entries[_index - 1] : null;

    public NavigationSnapshot? GetForwardTarget() => CanGoForward ? _entries[_index + 1] : null;

    public void CommitBack()
    {
        if (!CanGoBack)
        {
            return;
        }

        _index--;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void CommitForward()
    {
        if (!CanGoForward)
        {
            return;
        }

        _index++;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
