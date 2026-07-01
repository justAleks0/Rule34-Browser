using System.Collections.ObjectModel;
using Rule34Gallery.Core.CloudSync;
using Rule34Gallery.Core.Firebase;

namespace Rule34Gallery.Core.Services;

public sealed class CloudSyncService
{
    private readonly AppServices _app;
    private readonly CloudSyncSnapshotService _snapshots;
    private readonly CloudSyncApplyService _apply;
    private readonly SemaphoreSlim _runGate = new(1, 1);

    private static readonly (string Id, string Label, string Direction)[] StepDefs =
    [
        ("credentials", "API credentials", "Merge with cloud"),
        ("favorites", "Favorites", "Download from cloud"),
        ("lists", "Lists & Watch Later", "Download from cloud"),
        ("saved_tags", "Saved tag sets", "Merge with cloud"),
        ("for_you", "For You profile", "Download & upload"),
    ];

    public CloudSyncService(AppServices app)
    {
        _app = app;
        _snapshots = new CloudSyncSnapshotService(app);
        _apply = new CloudSyncApplyService(app);
    }

    public ObservableCollection<CloudSyncStep> Steps { get; } = [];

    public List<SyncDataNode> DataTree { get; private set; } = [];

    public CloudSyncSnapshot? LocalSnapshot { get; private set; }

    public CloudSyncSnapshot? CloudSnapshot { get; private set; }

    public CloudSyncDiff? CurrentDiff { get; private set; }

    public SyncSessionMeta SessionMeta { get; } = new();

    public event EventHandler? StepsChanged;

    public event EventHandler? PreviewChanged;

    public bool IsRunning { get; private set; }

    public bool IsPreviewLoading { get; private set; }

    public string PreviewError { get; private set; } = string.Empty;

    public string LastSummary { get; private set; } = string.Empty;

    public async Task RefreshPreviewAsync()
    {
        var library = _app.Library;
        if (!library.IsAvailable || !library.IsSignedIn)
        {
            DataTree = [];
            LocalSnapshot = null;
            CloudSnapshot = null;
            CurrentDiff = null;
            PreviewError = !library.IsAvailable
                ? "Firebase not configured."
                : "Sign in to preview cloud data.";
            NotifyPreviewChanged();
            return;
        }

        IsPreviewLoading = true;
        PreviewError = string.Empty;
        NotifyPreviewChanged();

        try
        {
            LocalSnapshot = _snapshots.BuildLocalSnapshot();
            CloudSnapshot = await _snapshots.FetchCloudSnapshotAsync().ConfigureAwait(false);
            DataTree = CloudSyncTreeBuilder.Build(LocalSnapshot, CloudSnapshot);
            CurrentDiff = CloudSyncDiffService.BuildDiff(LocalSnapshot, CloudSnapshot);
            await LoadSessionMetaAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            PreviewError = ex.Message;
        }
        finally
        {
            IsPreviewLoading = false;
            NotifyPreviewChanged();
        }
    }

    public Task<CloudSyncResult> RunSyncAsync() =>
        RunDownloadAsync(SyncApplyMode.MergeSkipDuplicates);

    public async Task<CloudSyncResult> RunUploadAsync(SyncApplyMode mode, IReadOnlySet<string>? selectedLeafIds = null)
        => await RunDirectionalAsync(SyncDirection.Upload, mode, selectedLeafIds).ConfigureAwait(true);

    public async Task<CloudSyncResult> RunDownloadAsync(SyncApplyMode mode, IReadOnlySet<string>? selectedLeafIds = null)
        => await RunDirectionalAsync(SyncDirection.Download, mode, selectedLeafIds).ConfigureAwait(true);

    public async Task<CloudSyncResult> DeleteCloudItemAsync(SyncDataNode node)
    {
        var library = _app.Library;
        if (!library.IsSignedIn)
        {
            return CloudSyncResult.Fail("Not signed in", "Sign in to manage cloud data.");
        }

        try
        {
            if (node.Id.StartsWith("fav:post:", StringComparison.Ordinal) &&
                int.TryParse(node.Id["fav:post:".Length..], out var favId))
            {
                await library.DeleteCloudFavoriteAsync(favId).ConfigureAwait(false);
            }
            else if (node.Id.StartsWith("preset:", StringComparison.Ordinal) &&
                     !node.Id.Contains(":tag:", StringComparison.Ordinal))
            {
                var presetId = node.Id["preset:".Length..];
                await library.DeleteCloudPresetAsync(presetId).ConfigureAwait(false);
            }
            else
            {
                return CloudSyncResult.Fail("Unsupported", "This item cannot be deleted from cloud here.");
            }

            await RefreshPreviewAsync().ConfigureAwait(false);
            return CloudSyncResult.Ok("Deleted", "Removed from cloud.", false, false, library.FavoriteIds.Count);
        }
        catch (Exception ex)
        {
            return CloudSyncResult.Fail("Delete failed", ex.Message);
        }
    }

    public void SetNodeSelected(SyncDataNode node, bool selected, bool includeChildren = true)
    {
        var nodes = EnumerateNodeAndDescendants(node, includeChildren).ToList();
        foreach (var current in nodes)
        {
            current.SetIsSelectedSilently(selected);
        }

        foreach (var current in nodes)
        {
            current.NotifyIsSelectedChanged();
        }
    }

    private static IEnumerable<SyncDataNode> EnumerateNodeAndDescendants(SyncDataNode node, bool includeChildren)
    {
        yield return node;
        if (!includeChildren)
        {
            yield break;
        }

        var stack = new Stack<SyncDataNode>(node.Children);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;
            foreach (var child in current.Children)
            {
                stack.Push(child);
            }
        }
    }

    public void SelectAllNewFromCloud()
    {
        foreach (var node in DataTree)
        {
            SelectByStatus(node, SyncNodeStatus.CloudOnly, selected: true);
        }
    }

    public void SelectAllLocalOnlyUploads()
    {
        foreach (var node in DataTree)
        {
            SelectByStatus(node, SyncNodeStatus.LocalOnly, selected: true);
        }
    }

    private static void SelectByStatus(SyncDataNode node, SyncNodeStatus status, bool selected)
    {
        if (node.Kind == SyncNodeKind.Leaf && node.Status == status)
        {
            node.IsSelected = selected;
        }

        foreach (var child in node.Children)
        {
            SelectByStatus(child, status, selected);
        }
    }

    private async Task<CloudSyncResult> RunDirectionalAsync(
        SyncDirection direction,
        SyncApplyMode mode,
        IReadOnlySet<string>? selectedLeafIds)
    {
        if (!await _runGate.WaitAsync(0).ConfigureAwait(true))
        {
            return CloudSyncResult.Fail("Sync in progress", "Cloud sync is already running.");
        }

        try
        {
            return await RunDirectionalCoreAsync(direction, mode, selectedLeafIds).ConfigureAwait(true);
        }
        finally
        {
            IsRunning = false;
            _runGate.Release();
            NotifyStepsChanged();
        }
    }

    private async Task<CloudSyncResult> RunDirectionalCoreAsync(
        SyncDirection direction,
        SyncApplyMode mode,
        IReadOnlySet<string>? selectedLeafIds)
    {
        var library = _app.Library;
        if (!library.IsAvailable)
        {
            return CloudSyncResult.Fail(
                "Cloud sync unavailable",
                "Add firebase-config.json to enable cloud sync (see firebase-config.example.json).");
        }

        if (!library.IsSignedIn)
        {
            return CloudSyncResult.Fail(
                "Not signed in",
                "Sign in on the Account page to sync your library and For You profile.");
        }

        InitializeSteps(direction);
        IsRunning = true;
        LastSummary = string.Empty;
        SessionMeta.Status = SyncSessionStatus.Syncing;
        NotifyStepsChanged();
        NotifyPreviewChanged();

        var device = SyncDeviceStore.CreateCurrentDevice(
            library.AppDataFolder,
            "Windows",
            typeof(CloudSyncService).Assembly.GetName().Version?.ToString(3) ?? "2.0");

        try
        {
            var local = _snapshots.BuildLocalSnapshot();
            var cloud = await _snapshots.FetchCloudSnapshotAsync().ConfigureAwait(true);
            var selected = selectedLeafIds ??
                           (mode == SyncApplyMode.SelectItems
                               ? CloudSyncDiffService.CollectSelectedLeafIds(DataTree)
                               : null);
            var merged = DeepMergeEngine.Merge(local, cloud, direction, mode, selected);

            var uploaded = false;
            var downloaded = false;

            if (direction == SyncDirection.Upload)
            {
                await RunStepAsync("credentials", async () =>
                {
                    await _apply.ApplyUploadAsync(merged).ConfigureAwait(true);
                    return "Uploaded to cloud";
                }).ConfigureAwait(true);
                uploaded = true;
            }
            else
            {
                await RunStepAsync("credentials", async () =>
                {
                    await _apply.ApplyDownloadAsync(merged).ConfigureAwait(true);
                    return "Downloaded from cloud";
                }).ConfigureAwait(true);
                downloaded = true;
            }

            MarkStepCompleted("favorites", $"{merged.Favorites.Count} favorite(s)");
            MarkStepCompleted("lists", $"{merged.Lists.Count} list(s), {merged.WatchLaterPosts.Count} watch later");
            MarkStepCompleted("saved_tags", $"{merged.SavedTagPresets.Count} tag set(s)");
            if (_app.ForYou.CloudSyncEnabled)
            {
                MarkStepCompleted("for_you", _app.ForYou.BuildSyncSummary());
            }
            else
            {
                MarkSkipped("for_you", "Cloud sync off");
            }

            LastSummary = direction == SyncDirection.Upload
                ? $"Uploaded to cloud — {merged.Favorites.Count} favorites, {merged.SavedTagPresets.Count} tag sets."
                : $"Downloaded from cloud — {merged.Favorites.Count} favorites, {merged.SavedTagPresets.Count} tag sets.";

            SessionMeta.Status = SyncSessionStatus.Success;
            SessionMeta.LastSummary = LastSummary;
            SessionMeta.LastError = string.Empty;
            await library.RecordSyncSessionAsync(
                direction,
                SyncSessionStatus.Success,
                device,
                LastSummary).ConfigureAwait(true);

            await RefreshPreviewAsync().ConfigureAwait(true);
            return CloudSyncResult.Ok(
                direction == SyncDirection.Upload ? "Upload complete" : "Download complete",
                LastSummary,
                uploaded,
                downloaded,
                library.FavoriteIds.Count);
        }
        catch (Exception ex)
        {
            LastSummary = ex.Message;
            SessionMeta.Status = SyncSessionStatus.Failed;
            SessionMeta.LastError = ex.Message;
            await library.RecordSyncSessionAsync(
                direction,
                SyncSessionStatus.Failed,
                device,
                summary: LastSummary,
                error: ex.Message).ConfigureAwait(true);
            return CloudSyncResult.Fail(
                direction == SyncDirection.Upload ? "Upload failed" : "Download failed",
                ex.Message);
        }
    }

    private async Task LoadSessionMetaAsync()
    {
        var settings = _app.Settings;
        SessionMeta.LastSuccessAtUnix = settings.LastSyncSuccessAtUnix;
        SessionMeta.LastAttemptAtUnix = settings.LastSyncAttemptAtUnix;
        SessionMeta.LastDeviceId = settings.LastSyncDeviceId;
        SessionMeta.LastDeviceLabel = settings.LastSyncDeviceLabel;
        SessionMeta.LastDirection = settings.LastSyncDirection;
        SessionMeta.LastError = settings.LastSyncError;
        SessionMeta.LastSummary = settings.LastSyncSummary;
        SessionMeta.Status = Enum.TryParse<SyncSessionStatus>(settings.LastSyncStatus, out var status)
            ? status
            : SyncSessionStatus.Idle;

        var library = _app.Library;
        if (library.Firestore is not null && library.IsSignedIn)
        {
            try
            {
                var remote = await library.Firestore.GetSyncMetaAsync().ConfigureAwait(false);
                if (remote?.LastFullSyncAtUnix is > 0)
                {
                    SessionMeta.LastSuccessAtUnix = remote.LastFullSyncAtUnix;
                    SessionMeta.LastDeviceId = remote.LastSyncDeviceId;
                    SessionMeta.LastDirection = remote.LastSyncDirection;
                }

                if (!string.IsNullOrWhiteSpace(SessionMeta.LastDeviceId))
                {
                    var device = await library.Firestore.GetDeviceAsync(SessionMeta.LastDeviceId).ConfigureAwait(false);
                    if (device is not null && !string.IsNullOrWhiteSpace(device.DisplayName))
                    {
                        SessionMeta.LastDeviceLabel = $"{device.DisplayName} ({device.Platform})";
                    }
                }
            }
            catch
            {
                // Local metadata is enough.
            }
        }
    }

    private void InitializeSteps(SyncDirection direction)
    {
        Steps.Clear();
        foreach (var (id, label, _) in StepDefs)
        {
            var stepDirection = direction == SyncDirection.Upload ? "Upload to cloud" : "Download from cloud";
            Steps.Add(new CloudSyncStep
            {
                Id = id,
                Label = label,
                Direction = stepDirection,
            });
        }
    }

    private async Task<T> RunStepAsync<T>(string id, Func<Task<T>> action) where T : notnull
    {
        var step = Steps.First(s => s.Id == id);
        step.SetRunning();
        NotifyStepsChanged();

        try
        {
            var result = await action().ConfigureAwait(true);
            var detail = result switch
            {
                CredentialSyncResult cred => cred.Detail,
                string text => text,
                _ => result.ToString() ?? string.Empty,
            };
            step.SetCompleted(detail);
            NotifyStepsChanged();
            return result;
        }
        catch (Exception ex)
        {
            step.SetFailed(ex.Message);
            NotifyStepsChanged();
            throw;
        }
    }

    private void MarkStepCompleted(string id, string detail)
    {
        var step = Steps.FirstOrDefault(s => s.Id == id);
        step?.SetCompleted(detail);
        NotifyStepsChanged();
    }

    private void MarkSkipped(string id, string detail)
    {
        var step = Steps.FirstOrDefault(s => s.Id == id);
        if (step is null)
        {
            return;
        }

        step.Status = CloudSyncStepStatus.Skipped;
        step.Detail = detail;
        step.Progress = 100;
        NotifyStepsChanged();
    }

    private void NotifyStepsChanged() => StepsChanged?.Invoke(this, EventArgs.Empty);

    private void NotifyPreviewChanged() => PreviewChanged?.Invoke(this, EventArgs.Empty);
}
