using Rule34Gallery.Core.Firebase;
using Rule34Gallery.Core.Services;

namespace Rule34Gallery.Core.CloudSync;

public static class DeepMergeEngine
{
    public static CloudSyncSnapshot Merge(
        CloudSyncSnapshot local,
        CloudSyncSnapshot cloud,
        SyncDirection direction,
        SyncApplyMode mode,
        IReadOnlySet<string>? selectedLeafIds = null)
    {
        return mode switch
        {
            SyncApplyMode.ReplaceAll => direction == SyncDirection.Upload ? Clone(local) : Clone(cloud),
            SyncApplyMode.MergeSkipDuplicates => MergeDeep(local, cloud, direction, selectedLeafIds),
            SyncApplyMode.SelectItems => MergeSelected(local, cloud, direction, selectedLeafIds ?? new HashSet<string>()),
            _ => MergeDeep(local, cloud, direction, selectedLeafIds),
        };
    }

    private static CloudSyncSnapshot MergeSelected(
        CloudSyncSnapshot local,
        CloudSyncSnapshot cloud,
        SyncDirection direction,
        IReadOnlySet<string> selected)
    {
        var baseSnapshot = direction == SyncDirection.Upload ? Clone(local) : Clone(cloud);
        var other = direction == SyncDirection.Upload ? cloud : local;

        if (SelectedCategory(selected, SyncDataCategory.Credentials))
        {
            baseSnapshot.Credentials = direction == SyncDirection.Upload
                ? CloudUserCredentials.MergeForCloudUpload(local.Credentials, cloud.Credentials)
                : CloudUserCredentials.MergeCombine(local.Credentials, cloud.Credentials);
        }

        if (SelectedCategory(selected, SyncDataCategory.Favorites))
        {
            baseSnapshot.Favorites = MergeFavoritePosts(local.Favorites, cloud.Favorites, selected, direction);
        }

        if (SelectedCategory(selected, SyncDataCategory.Lists) ||
            SelectedCategory(selected, SyncDataCategory.WatchLater))
        {
            var (lists, listPosts, watchLater) = MergeLists(local, cloud, selected, direction);
            baseSnapshot.Lists = lists;
            baseSnapshot.ListPosts = listPosts;
            baseSnapshot.WatchLaterPosts = watchLater;
        }

        if (SelectedCategory(selected, SyncDataCategory.SavedTags))
        {
            baseSnapshot.SavedTagPresets = MergePresets(local.SavedTagPresets, cloud.SavedTagPresets, selected);
        }

        if (SelectedCategory(selected, SyncDataCategory.ForYou))
        {
            baseSnapshot.ForYouProfile = MergeForYou(local, cloud, direction);
            baseSnapshot.ForYouEnabled = direction == SyncDirection.Upload ? local.ForYouEnabled : cloud.ForYouEnabled;
            baseSnapshot.ForYouCloudSyncEnabled = direction == SyncDirection.Upload
                ? local.ForYouCloudSyncEnabled
                : cloud.ForYouCloudSyncEnabled;
        }

        return baseSnapshot;
    }

    private static CloudSyncSnapshot MergeDeep(
        CloudSyncSnapshot local,
        CloudSyncSnapshot cloud,
        SyncDirection direction,
        IReadOnlySet<string>? selectedLeafIds)
    {
        var favorites = MergeFavoritePosts(local.Favorites, cloud.Favorites, selectedLeafIds, direction);
        var (lists, listPosts, watchLater) = MergeLists(local, cloud, selectedLeafIds, direction);
        var presets = MergePresets(local.SavedTagPresets, cloud.SavedTagPresets, selectedLeafIds);
        var credentials = direction == SyncDirection.Upload
            ? CloudUserCredentials.MergeForCloudUpload(local.Credentials, cloud.Credentials)
            : CloudUserCredentials.MergeCombine(local.Credentials, cloud.Credentials);

        return new CloudSyncSnapshot
        {
            Favorites = favorites,
            Lists = lists,
            ListPosts = listPosts,
            WatchLaterPosts = watchLater,
            Credentials = credentials,
            SavedTagPresets = presets,
            ForYouProfile = MergeForYou(local, cloud, direction),
            ForYouEnabled = direction == SyncDirection.Upload ? local.ForYouEnabled : cloud.ForYouEnabled,
            ForYouCloudSyncEnabled = direction == SyncDirection.Upload
                ? local.ForYouCloudSyncEnabled
                : cloud.ForYouCloudSyncEnabled,
        };
    }

    private static IReadOnlyList<PostItem> MergeFavoritePosts(
        IReadOnlyList<PostItem> local,
        IReadOnlyList<PostItem> cloud,
        IReadOnlySet<string>? selected,
        SyncDirection direction)
    {
        var map = new Dictionary<int, PostItem>();
        foreach (var post in cloud)
        {
            map[post.Id] = post;
        }

        foreach (var post in local)
        {
            if (selected is not null &&
                !selected.Contains($"fav:post:{post.Id}") &&
                map.ContainsKey(post.Id))
            {
                continue;
            }

            map[post.Id] = post;
        }

        if (direction == SyncDirection.Download)
        {
            foreach (var post in cloud)
            {
                if (!map.ContainsKey(post.Id))
                {
                    map[post.Id] = post;
                }
            }
        }

        return map.Values.OrderBy(p => p.Id).ToList();
    }

    private static (IReadOnlyList<SavedList> Lists, IReadOnlyDictionary<string, IReadOnlyList<PostItem>> ListPosts, IReadOnlyList<PostItem> WatchLater)
        MergeLists(
            CloudSyncSnapshot local,
            CloudSyncSnapshot cloud,
            IReadOnlySet<string>? selected,
            SyncDirection direction)
    {
        var listMap = new Dictionary<string, SavedList>(StringComparer.Ordinal);
        foreach (var list in local.Lists.Concat(cloud.Lists))
        {
            if (string.IsNullOrWhiteSpace(list.Id))
            {
                continue;
            }

            listMap[list.Id] = list;
        }

        var postMap = new Dictionary<string, Dictionary<int, PostItem>>(StringComparer.Ordinal);
        void AddPosts(string listId, IReadOnlyList<PostItem> posts, bool prefer)
        {
            if (!postMap.TryGetValue(listId, out var map))
            {
                map = new Dictionary<int, PostItem>();
                postMap[listId] = map;
            }

            foreach (var post in posts)
            {
                var leafId = string.Equals(listId, SavedList.WatchLaterId, StringComparison.Ordinal)
                    ? $"wl:post:{post.Id}"
                    : $"list:{listId}:post:{post.Id}";
                if (selected is not null && !selected.Contains(leafId) && map.ContainsKey(post.Id))
                {
                    continue;
                }

                if (!map.ContainsKey(post.Id) || prefer)
                {
                    map[post.Id] = post;
                }
            }
        }

        foreach (var (listId, posts) in cloud.ListPosts)
        {
            AddPosts(listId, posts, prefer: direction == SyncDirection.Download);
        }

        foreach (var (listId, posts) in local.ListPosts)
        {
            AddPosts(listId, posts, prefer: direction == SyncDirection.Upload);
        }

        var listPosts = postMap.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<PostItem>)kv.Value.Values.OrderBy(p => p.Id).ToList(),
            StringComparer.Ordinal);

        var watchLater = listPosts.TryGetValue(SavedList.WatchLaterId, out var wl)
            ? wl
            : MergeFavoritePosts(local.WatchLaterPosts, cloud.WatchLaterPosts, selected, direction);

        if (!listPosts.ContainsKey(SavedList.WatchLaterId))
        {
            listPosts[SavedList.WatchLaterId] = watchLater;
        }

        return (listMap.Values.ToList(), listPosts, watchLater);
    }

    private static IReadOnlyList<SavedTagPreset> MergePresets(
        IReadOnlyList<SavedTagPreset> local,
        IReadOnlyList<SavedTagPreset> cloud,
        IReadOnlySet<string>? selected)
    {
        var merged = SavedTagPresetSync.Merge(local, cloud);
        if (selected is null || selected.Count == 0)
        {
            return merged;
        }

        return merged
            .Where(p =>
                selected.Contains($"preset:{p.Id}") ||
                p.Tags.Any(tag => selected.Contains($"preset:{p.Id}:tag:{tag}")))
            .ToList();
    }

    private static ForYouCloudProfile? MergeForYou(
        CloudSyncSnapshot local,
        CloudSyncSnapshot cloud,
        SyncDirection direction)
    {
        var localProfile = local.ForYouProfile;
        var cloudProfile = cloud.ForYouProfile;
        if (localProfile is null && cloudProfile is null)
        {
            return null;
        }

        if (localProfile is null)
        {
            return cloudProfile;
        }

        if (cloudProfile is null)
        {
            return localProfile;
        }

        var target = ForYouProfile.FromCloudProfile(
            direction == SyncDirection.Upload ? localProfile : cloudProfile);
        var source = ForYouProfile.FromCloudProfile(
            direction == SyncDirection.Upload ? cloudProfile : localProfile);
        MergeForYouProfiles(target, source, preferLocal: direction == SyncDirection.Upload);
        var result = target.ToCloudProfile();
        result.Enabled = direction == SyncDirection.Upload ? local.ForYouEnabled : cloud.ForYouEnabled;
        result.CloudSyncEnabled = direction == SyncDirection.Upload
            ? local.ForYouCloudSyncEnabled
            : cloud.ForYouCloudSyncEnabled;
        return result;
    }

    private static void MergeForYouProfiles(ForYouProfile target, ForYouProfile source, bool preferLocal)
    {
        foreach (var removed in source.RemovedTopics)
        {
            target.MarkTopicRemoved(removed);
        }

        var topicMap = target.Topics.ToDictionary(t => t.Topic, StringComparer.OrdinalIgnoreCase);
        foreach (var topic in source.Topics)
        {
            if (target.IsTopicRemoved(topic.Topic))
            {
                continue;
            }

            if (!topicMap.TryGetValue(topic.Topic, out var existing))
            {
                target.Topics.Add(topic);
                continue;
            }

            existing.Weight = preferLocal
                ? Math.Max(existing.Weight, topic.Weight)
                : Math.Max(topic.Weight, existing.Weight);
            existing.IsPinned |= topic.IsPinned;
            existing.IsBlocked |= topic.IsBlocked;
        }

        var searchMap = target.SearchLines.ToDictionary(s => s.Query, StringComparer.OrdinalIgnoreCase);
        foreach (var line in source.SearchLines)
        {
            if (!searchMap.ContainsKey(line.Query))
            {
                target.SearchLines.Add(line);
            }
        }

        var activityIds = target.RecentActivity.Select(a => a.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var activity in source.RecentActivity)
        {
            if (activityIds.Add(activity.Id))
            {
                target.RecentActivity.Add(activity);
            }
        }
    }

    private static bool SelectedCategory(IReadOnlySet<string> selected, SyncDataCategory category) =>
        selected.Any(id => id.StartsWith(category switch
        {
            SyncDataCategory.Credentials => "cred:",
            SyncDataCategory.Favorites => "fav:",
            SyncDataCategory.Lists => "list:",
            SyncDataCategory.WatchLater => "wl:",
            SyncDataCategory.SavedTags => "preset:",
            SyncDataCategory.ForYou => "foryou:",
            _ => string.Empty,
        }, StringComparison.OrdinalIgnoreCase));

    private static CloudSyncSnapshot Clone(CloudSyncSnapshot source) => new()
    {
        Favorites = source.Favorites.ToList(),
        Lists = source.Lists.ToList(),
        ListPosts = source.ListPosts.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<PostItem>)kv.Value.ToList(),
            StringComparer.Ordinal),
        WatchLaterPosts = source.WatchLaterPosts.ToList(),
        Credentials = source.Credentials,
        SavedTagPresets = source.SavedTagPresets.ToList(),
        ForYouProfile = source.ForYouProfile,
        ForYouEnabled = source.ForYouEnabled,
        ForYouCloudSyncEnabled = source.ForYouCloudSyncEnabled,
    };
}
