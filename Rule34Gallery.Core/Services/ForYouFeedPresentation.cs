namespace Rule34Gallery.Core.Services;

public static class ForYouFeedPresentation
{
    public const int DisplayLimit = 48;

    public const int PoolLimit = 120;

    public static IReadOnlyList<ForYouFeedItem> Apply(
        IEnumerable<ForYouFeedItem> pool,
        ForYouFeedSortMode sort,
        MediaFilterMode filter,
        Random? random = null)
    {
        var filtered = pool
            .Where(item => item.Post is not null && PostSearchFilter.MatchesMedia(item.Post, filter))
            .ToList();

        if (filtered.Count == 0 && filter != MediaFilterMode.All)
        {
            filtered = pool
                .Where(item => item.Post is not null)
                .ToList();
        }

        IEnumerable<ForYouFeedItem> ordered = sort switch
        {
            ForYouFeedSortMode.HighestScore => filtered
                .OrderByDescending(item => item.TrainingScore)
                .ThenByDescending(item => item.WeightedMatchScore)
                .ThenByDescending(item => item.MatchedTopicCount)
                .ThenByDescending(item => item.QueryHitCount)
                .ThenByDescending(item => item.Post!.Score)
                .ThenBy(item => item.Post!.Id),
            ForYouFeedSortMode.Random => Shuffle(filtered, random ?? Random.Shared),
            _ => filtered
                .OrderByDescending(item => item.MatchedTopicCount)
                .ThenByDescending(item => item.WeightedMatchScore)
                .ThenByDescending(item => item.QueryHitCount)
                .ThenByDescending(item => item.TrainingScore)
                .ThenByDescending(item => item.Post!.Score)
                .ThenBy(item => item.Post!.Id),
        };

        return ordered.Take(DisplayLimit).ToList();
    }

    public static string DescribeSort(ForYouFeedSortMode sort) => sort switch
    {
        ForYouFeedSortMode.HighestScore => "Highest training score",
        ForYouFeedSortMode.Random => "Random",
        _ => "Most topic matches",
    };

    public static string DescribeFilter(MediaFilterMode filter) => filter switch
    {
        MediaFilterMode.Images => "Images",
        MediaFilterMode.Videos => "Videos",
        MediaFilterMode.Gifs => "GIFs",
        MediaFilterMode.Animated => "Animated",
        _ => "All",
    };

    private static IEnumerable<ForYouFeedItem> Shuffle(IReadOnlyList<ForYouFeedItem> items, Random random)
    {
        if (items.Count <= 1)
        {
            return items;
        }

        var copy = items.ToList();
        for (var i = copy.Count - 1; i > 0; i--)
        {
            var swapIndex = random.Next(i + 1);
            (copy[i], copy[swapIndex]) = (copy[swapIndex], copy[i]);
        }

        return copy;
    }
}
