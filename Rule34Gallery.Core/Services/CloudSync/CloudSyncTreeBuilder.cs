using Rule34Gallery.Core.Firebase;
using Rule34Gallery.Core.Services;

namespace Rule34Gallery.Core.CloudSync;

public static class CloudSyncTreeBuilder
{
    private const int MaxVisiblePostLeaves = 200;

    private const int MaxVisibleActivityLeaves = 100;

    public static List<SyncDataNode> Build(CloudSyncSnapshot local, CloudSyncSnapshot cloud)
    {
        return
        [
            BuildCredentials(local.Credentials, cloud.Credentials),
            BuildFavorites(local.Favorites, cloud.Favorites),
            BuildLists(local, cloud),
            BuildWatchLater(local.WatchLaterPosts, cloud.WatchLaterPosts),
            BuildSavedTags(local.SavedTagPresets, cloud.SavedTagPresets),
            BuildForYou(local, cloud),
        ];
    }

    private static SyncDataNode BuildCredentials(CloudUserCredentials local, CloudUserCredentials cloud)
    {
        var children = new List<SyncDataNode>();
        AddCredentialLeaf(children, "rule34", "Rule34 API", local.HasRule34Credentials, cloud.HasRule34Credentials);
        AddCredentialLeaf(children, "danbooru", "Danbooru API", local.HasDanbooruCredentials, cloud.HasDanbooruCredentials);
        AddCredentialLeaf(children, "e621", "e621 API", local.HasE621Credentials, cloud.HasE621Credentials);
        AddCredentialLeaf(children, "openai", "OpenAI API", local.HasOpenAiCredentials, cloud.HasOpenAiCredentials);

        return Category("credentials", "API credentials", SyncDataCategory.Credentials, children);
    }

    private static void AddCredentialLeaf(
        List<SyncDataNode> children,
        string id,
        string label,
        bool localHas,
        bool cloudHas)
    {
        children.Add(new SyncDataNode
        {
            Id = $"cred:{id}",
            Label = label,
            Kind = SyncNodeKind.Leaf,
            Category = SyncDataCategory.Credentials,
            LocalCount = localHas ? 1 : 0,
            CloudCount = cloudHas ? 1 : 0,
            Status = StatusFor(localHas, cloudHas),
            Detail = localHas && cloudHas ? "Saved on both" : localHas ? "Local only" : cloudHas ? "Cloud only" : "Empty",
        });
    }

    private static SyncDataNode BuildFavorites(
        IReadOnlyList<PostItem> local,
        IReadOnlyList<PostItem> cloud)
    {
        var localIds = local.Select(p => p.Id).ToHashSet();
        var cloudIds = cloud.Select(p => p.Id).ToHashSet();
        var (children, leafIds) = BuildPostChildren("fav", localIds, cloudIds, SyncDataCategory.Favorites);
        var node = Category("favorites", "Favorites", SyncDataCategory.Favorites, children, leafIds);
        node.LocalCount = localIds.Count;
        node.CloudCount = cloudIds.Count;
        return node;
    }

    private static SyncDataNode BuildLists(CloudSyncSnapshot local, CloudSyncSnapshot cloud)
    {
        var listMap = new Dictionary<string, (string Name, bool IsSystem)>(StringComparer.Ordinal);
        foreach (var list in local.Lists.Concat(cloud.Lists))
        {
            if (string.IsNullOrWhiteSpace(list.Id) ||
                string.Equals(list.Id, SavedList.WatchLaterId, StringComparison.Ordinal))
            {
                continue;
            }

            listMap[list.Id] = (list.Name, list.IsSystem);
        }

        var children = listMap
            .OrderByDescending(kv => kv.Value.IsSystem)
            .ThenBy(kv => kv.Value.Name, StringComparer.OrdinalIgnoreCase)
            .Select(kv =>
            {
                local.ListPosts.TryGetValue(kv.Key, out var localPosts);
                cloud.ListPosts.TryGetValue(kv.Key, out var cloudPosts);
                localPosts ??= [];
                cloudPosts ??= [];

                var localIds = localPosts.Select(p => p.Id).ToHashSet();
                var cloudIds = cloudPosts.Select(p => p.Id).ToHashSet();
                var (postChildren, leafIds) = BuildPostChildren($"list:{kv.Key}", localIds, cloudIds, SyncDataCategory.Lists);

                return new SyncDataNode
                {
                    Id = $"list:{kv.Key}",
                    Label = kv.Value.Name,
                    Kind = SyncNodeKind.Container,
                    Category = SyncDataCategory.Lists,
                    LocalCount = localIds.Count,
                    CloudCount = cloudIds.Count,
                    Status = StatusFor(localIds.Count > 0, cloudIds.Count > 0, localIds, cloudIds),
                    Children = postChildren,
                    DescendantLeafIds = leafIds,
                };
            })
            .ToList();

        return Category("lists", "Lists", SyncDataCategory.Lists, children);
    }

    private static SyncDataNode BuildWatchLater(
        IReadOnlyList<PostItem> local,
        IReadOnlyList<PostItem> cloud)
    {
        var localIds = local.Select(p => p.Id).ToHashSet();
        var cloudIds = cloud.Select(p => p.Id).ToHashSet();
        var (children, leafIds) = BuildPostChildren("wl", localIds, cloudIds, SyncDataCategory.WatchLater);
        var node = Category("watch_later", "Watch Later", SyncDataCategory.WatchLater, children, leafIds);
        node.LocalCount = localIds.Count;
        node.CloudCount = cloudIds.Count;
        return node;
    }

    private static SyncDataNode BuildSavedTags(
        IReadOnlyList<SavedTagPreset> local,
        IReadOnlyList<SavedTagPreset> cloud)
    {
        var map = new Dictionary<string, (SavedTagPreset? Local, SavedTagPreset? Cloud)>(StringComparer.OrdinalIgnoreCase);
        foreach (var preset in local.Where(p => !string.IsNullOrWhiteSpace(p.Id)))
        {
            map[preset.Id] = (preset, map.GetValueOrDefault(preset.Id).Cloud);
        }

        foreach (var preset in cloud.Where(p => !string.IsNullOrWhiteSpace(p.Id)))
        {
            var existing = map.GetValueOrDefault(preset.Id);
            map[preset.Id] = (existing.Local, preset);
        }

        var children = map
            .OrderBy(kv => kv.Value.Local?.Name ?? kv.Value.Cloud?.Name ?? kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv =>
            {
                var localPreset = kv.Value.Local;
                var cloudPreset = kv.Value.Cloud;
                var localTags = localPreset?.Tags ?? [];
                var cloudTags = cloudPreset?.Tags ?? [];
                var localSet = localTags.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var cloudSet = cloudTags.ToHashSet(StringComparer.OrdinalIgnoreCase);

                var tagChildren = localSet.Union(cloudSet, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                    .Select(tag => new SyncDataNode
                    {
                        Id = $"preset:{kv.Key}:tag:{tag}",
                        Label = tag,
                        Kind = SyncNodeKind.Leaf,
                        Category = SyncDataCategory.SavedTags,
                        LocalCount = localSet.Contains(tag) ? 1 : 0,
                        CloudCount = cloudSet.Contains(tag) ? 1 : 0,
                        Status = StatusFor(localSet.Contains(tag), cloudSet.Contains(tag)),
                    })
                    .ToList();

                var changed = localPreset is not null && cloudPreset is not null &&
                              (localPreset.UpdatedAtUnix != cloudPreset.UpdatedAtUnix ||
                               !localSet.SetEquals(cloudSet));

                return new SyncDataNode
                {
                    Id = $"preset:{kv.Key}",
                    Label = localPreset?.Name ?? cloudPreset?.Name ?? kv.Key,
                    Kind = SyncNodeKind.Container,
                    Category = SyncDataCategory.SavedTags,
                    LocalCount = localTags.Count,
                    CloudCount = cloudTags.Count,
                    Status = changed
                        ? SyncNodeStatus.Changed
                        : StatusFor(localPreset is not null, cloudPreset is not null),
                    Children = tagChildren,
                };
            })
            .ToList();

        return Category("saved_tags", "Saved tag sets", SyncDataCategory.SavedTags, children);
    }

    private static SyncDataNode BuildForYou(CloudSyncSnapshot local, CloudSyncSnapshot cloud)
    {
        var localProfile = local.ForYouProfile;
        var cloudProfile = cloud.ForYouProfile;
        var children = new List<SyncDataNode>();

        var localTopics = localProfile?.Topics ?? [];
        var cloudTopics = cloudProfile?.Topics ?? [];
        var topicMap = localTopics
            .Select(t => t.Topic)
            .Concat(cloudTopics.Select(t => t.Topic))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Select(topic =>
            {
                var lt = localTopics.FirstOrDefault(t => t.Topic.Equals(topic, StringComparison.OrdinalIgnoreCase));
                var ct = cloudTopics.FirstOrDefault(t => t.Topic.Equals(topic, StringComparison.OrdinalIgnoreCase));
                var localHas = lt is not null;
                var cloudHas = ct is not null;
                var changed = localHas && cloudHas &&
                              Math.Abs(lt!.Weight - ct!.Weight) > 0.001;
                return new SyncDataNode
                {
                    Id = $"foryou:topic:{topic}",
                    Label = topic,
                    Kind = SyncNodeKind.Leaf,
                    Category = SyncDataCategory.ForYou,
                    LocalCount = localHas ? 1 : 0,
                    CloudCount = cloudHas ? 1 : 0,
                    Status = changed ? SyncNodeStatus.Changed : StatusFor(localHas, cloudHas),
                    Detail = localHas && cloudHas
                        ? $"Weight local {lt!.Weight:F2}, cloud {ct!.Weight:F2}"
                        : string.Empty,
                };
            })
            .ToList();

        children.Add(Category("foryou:topics", "Topics", SyncDataCategory.ForYou, topicMap));

        var localSearches = localProfile?.SearchLines ?? [];
        var cloudSearches = cloudProfile?.SearchLines ?? [];
        var searchChildren = localSearches
            .Select(s => s.Query)
            .Concat(cloudSearches.Select(s => s.Query))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(q => q, StringComparer.OrdinalIgnoreCase)
            .Select(query =>
            {
                var localHas = localSearches.Any(s => s.Query.Equals(query, StringComparison.OrdinalIgnoreCase));
                var cloudHas = cloudSearches.Any(s => s.Query.Equals(query, StringComparison.OrdinalIgnoreCase));
                return new SyncDataNode
                {
                    Id = $"foryou:search:{query}",
                    Label = query,
                    Kind = SyncNodeKind.Leaf,
                    Category = SyncDataCategory.ForYou,
                    LocalCount = localHas ? 1 : 0,
                    CloudCount = cloudHas ? 1 : 0,
                    Status = StatusFor(localHas, cloudHas),
                };
            })
            .ToList();
        children.Add(Category("foryou:searches", "Search lines", SyncDataCategory.ForYou, searchChildren));

        var localActs = localProfile?.Activities ?? [];
        var cloudActs = cloudProfile?.Activities ?? [];
        var activityEntries = localActs
            .Select(a => (Key: ActivityKey(a), Activity: a, IsLocal: true))
            .Concat(cloudActs.Select(a => (Key: ActivityKey(a), Activity: a, IsLocal: false)))
            .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Max(x => x.Activity.TimestampUtc))
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var allActLeafIds = activityEntries
            .Select(g => $"foryou:activity:{g.Key}")
            .ToList();

        var actChildren = activityEntries
            .Take(MaxVisibleActivityLeaves)
            .Select(g =>
            {
                var sample = g.First().Activity;
                var localHas = g.Any(x => x.IsLocal);
                var cloudHas = g.Any(x => !x.IsLocal);
                return new SyncDataNode
                {
                    Id = $"foryou:activity:{g.Key}",
                    Label = FormatActivityLabel(sample),
                    Kind = SyncNodeKind.Leaf,
                    Category = SyncDataCategory.ForYou,
                    LocalCount = localHas ? 1 : 0,
                    CloudCount = cloudHas ? 1 : 0,
                    Status = StatusFor(localHas, cloudHas),
                    Detail = g.Key,
                };
            })
            .ToList();

        if (activityEntries.Count > MaxVisibleActivityLeaves)
        {
            var overflow = activityEntries.Count - MaxVisibleActivityLeaves;
            actChildren.Add(new SyncDataNode
            {
                Id = "foryou:activities:overflow",
                Label = $"… and {overflow:N0} more learning data rows (use parent checkbox to select all)",
                Kind = SyncNodeKind.Leaf,
                Category = SyncDataCategory.ForYou,
                IsSelectable = false,
                LocalCount = 0,
                CloudCount = 0,
                Status = SyncNodeStatus.Both,
            });
        }

        children.Add(Category(
            "foryou:activities",
            "Learning data",
            SyncDataCategory.ForYou,
            actChildren,
            allActLeafIds));

        var localEnabled = local.ForYouEnabled;
        var cloudEnabled = cloudProfile?.Enabled ?? false;
        return new SyncDataNode
        {
            Id = "for_you",
            Label = "For You profile",
            Kind = SyncNodeKind.Category,
            Category = SyncDataCategory.ForYou,
            LocalCount = localTopics.Count + localSearches.Count,
            CloudCount = cloudTopics.Count + cloudSearches.Count,
            Status = localEnabled != cloudEnabled
                ? SyncNodeStatus.Changed
                : StatusFor(localTopics.Count + localSearches.Count > 0, cloudTopics.Count + cloudSearches.Count > 0),
            Detail = local.ForYouCloudSyncEnabled
                ? "Cloud sync enabled locally"
                : "Cloud sync off locally",
            Children = children,
        };
    }

    private static (List<SyncDataNode> Children, List<string> LeafIds) BuildPostChildren(
        string prefix,
        HashSet<int> localIds,
        HashSet<int> cloudIds,
        SyncDataCategory category)
    {
        var allIds = UnionIds(localIds, cloudIds).OrderBy(id => id).ToList();
        var leafIds = allIds.Select(id => $"{prefix}:post:{id}").ToList();

        if (allIds.Count == 0)
        {
            return ([], leafIds);
        }

        if (allIds.Count > MaxVisiblePostLeaves)
        {
            var overflow = allIds.Count - MaxVisiblePostLeaves;
            var visible = allIds
                .Take(MaxVisiblePostLeaves)
                .Select(id => PostLeaf(prefix, id, localIds, cloudIds, category))
                .ToList();
            visible.Add(new SyncDataNode
            {
                Id = $"{prefix}:overflow",
                Label = $"… and {overflow:N0} more (use parent checkbox to select all)",
                Kind = SyncNodeKind.Leaf,
                Category = category,
                IsSelectable = false,
                LocalCount = 0,
                CloudCount = 0,
                Status = SyncNodeStatus.Both,
            });
            return (visible, leafIds);
        }

        return (allIds.Select(id => PostLeaf(prefix, id, localIds, cloudIds, category)).ToList(), leafIds);
    }

    private static string ActivityKey(ForYouCloudActivity activity) =>
        $"{activity.TimestampUtc}:{activity.Kind}:{activity.Topic}:{activity.PostId}";

    private static string FormatActivityLabel(ForYouCloudActivity activity) =>
        ForYouActivityLabels.FormatSyncTreeLabel(activity);

    private static SyncDataNode PostLeaf(
        string prefix,
        int postId,
        HashSet<int> localIds,
        HashSet<int> cloudIds,
        SyncDataCategory category)
    {
        var localHas = localIds.Contains(postId);
        var cloudHas = cloudIds.Contains(postId);
        return new SyncDataNode
        {
            Id = $"{prefix}:post:{postId}",
            Label = $"Post #{postId}",
            Kind = SyncNodeKind.Leaf,
            Category = category,
            LocalCount = localHas ? 1 : 0,
            CloudCount = cloudHas ? 1 : 0,
            Status = StatusFor(localHas, cloudHas),
        };
    }

    private static SyncDataNode Category(
        string id,
        string label,
        SyncDataCategory category,
        List<SyncDataNode> children,
        List<string>? descendantLeafIds = null)
    {
        var localCount = children.Sum(c => c.LocalCount);
        var cloudCount = children.Sum(c => c.CloudCount);
        var status = children.Count == 0
            ? SyncNodeStatus.Both
            : children.Any(c => c.Status is SyncNodeStatus.Changed or SyncNodeStatus.Conflict)
                ? SyncNodeStatus.Changed
                : children.Any(c => c.Status == SyncNodeStatus.LocalOnly)
                    ? SyncNodeStatus.LocalOnly
                    : children.Any(c => c.Status == SyncNodeStatus.CloudOnly)
                        ? SyncNodeStatus.CloudOnly
                        : SyncNodeStatus.Both;

        return new SyncDataNode
        {
            Id = id,
            Label = label,
            Kind = SyncNodeKind.Category,
            Category = category,
            LocalCount = localCount,
            CloudCount = cloudCount,
            Status = status,
            Children = children,
            DescendantLeafIds = descendantLeafIds ?? [],
        };
    }

    private static IEnumerable<int> UnionIds(HashSet<int> local, HashSet<int> cloud) =>
        local.Union(cloud);

    private static SyncNodeStatus StatusFor(bool localHas, bool cloudHas) =>
        localHas switch
        {
            true when cloudHas => SyncNodeStatus.Both,
            true => SyncNodeStatus.LocalOnly,
            false when cloudHas => SyncNodeStatus.CloudOnly,
            _ => SyncNodeStatus.Both,
        };

    private static SyncNodeStatus StatusFor(
        bool localHas,
        bool cloudHas,
        HashSet<int> localIds,
        HashSet<int> cloudIds)
    {
        if (!localHas && !cloudHas)
        {
            return SyncNodeStatus.Both;
        }

        if (!localHas)
        {
            return SyncNodeStatus.CloudOnly;
        }

        if (!cloudHas)
        {
            return SyncNodeStatus.LocalOnly;
        }

        return localIds.SetEquals(cloudIds) ? SyncNodeStatus.Both : SyncNodeStatus.Changed;
    }
}
