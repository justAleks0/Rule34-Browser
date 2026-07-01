namespace Rule34Gallery.Core.CloudSync;

public static class CloudSyncDiffService
{
    public static CloudSyncDiff BuildDiff(CloudSyncSnapshot local, CloudSyncSnapshot cloud)
    {
        var tree = CloudSyncTreeBuilder.Build(local, cloud);
        var diff = new CloudSyncDiff();
        CollectLeaves(tree, diff.Items);
        return diff;
    }

    public static HashSet<string> CollectSelectedLeafIds(IEnumerable<SyncDataNode> roots)
    {
        var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in roots)
        {
            WalkSelection(root, selected);
        }

        return selected;
    }

    private static void WalkSelection(SyncDataNode node, HashSet<string> selected)
    {
        if (node.DescendantLeafIds.Count > 0)
        {
            if (node.IsSelected)
            {
                foreach (var id in node.DescendantLeafIds)
                {
                    selected.Add(id);
                }
            }

            foreach (var child in node.Children)
            {
                if (child.Kind == SyncNodeKind.Leaf && child.IsSelectable && !child.IsSelected)
                {
                    selected.Remove(child.Id);
                }
                else if (child.Kind != SyncNodeKind.Leaf)
                {
                    WalkSelection(child, selected);
                }
            }

            return;
        }

        if (node.Kind == SyncNodeKind.Leaf)
        {
            if (node.IsSelected && node.IsSelectable)
            {
                selected.Add(node.Id);
            }

            return;
        }

        foreach (var child in node.Children)
        {
            WalkSelection(child, selected);
        }
    }

    private static void CollectLeaves(IEnumerable<SyncDataNode> nodes, List<SyncDiffItem> items)
    {
        foreach (var node in nodes)
        {
            if (node.Kind == SyncNodeKind.Leaf &&
                node.Status is not SyncNodeStatus.Both)
            {
                items.Add(new SyncDiffItem
                {
                    Id = node.Id,
                    Label = node.Label,
                    Category = node.Category ?? SyncDataCategory.Favorites,
                    Status = node.Status,
                    IsSelected = node.IsSelected,
                    Detail = node.Detail,
                });
            }

            if (node.Children.Count > 0)
            {
                CollectLeaves(node.Children, items);
            }
        }
    }
}
