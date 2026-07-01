namespace Rule34Gallery.Core.Services;

public static class PostSearchFilter
{
    public static bool Matches(PostItem post, UserSettings settings)
    {
        if (!post.HasDisplayableThumbnail)
        {
            return false;
        }

        if (settings.MinWidth > 0 && post.Width > 0 && post.Width < settings.MinWidth)
        {
            return false;
        }

        if (settings.MinHeight > 0 && post.Height > 0 && post.Height < settings.MinHeight)
        {
            return false;
        }

        if (settings.MinScore > 0 && post.Score > 0 && post.Score < settings.MinScore)
        {
            return false;
        }

        if (!MatchesRating(post, settings))
        {
            return false;
        }

        if (!MatchesMedia(post, settings.MediaFilter))
        {
            return false;
        }

        if (TagBlockFilter.PostHasBlockingTag(post, settings))
        {
            return false;
        }

        return true;
    }

    private static bool MatchesRating(PostItem post, UserSettings settings)
    {
        if (!settings.RatingSafe && !settings.RatingQuestionable && !settings.RatingExplicit)
        {
            return true;
        }

        if (settings.RatingSafe && settings.RatingQuestionable && settings.RatingExplicit)
        {
            return true;
        }

        var rating = post.Rating?.Trim().ToLowerInvariant() ?? string.Empty;
        return rating switch
        {
            "s" or "safe" => settings.RatingSafe,
            "q" or "questionable" => settings.RatingQuestionable,
            "e" or "explicit" => settings.RatingExplicit,
            _ => true,
        };
    }

    public static bool MatchesMedia(PostItem post, MediaFilterMode mode) => mode switch
    {
        MediaFilterMode.Images => post.MediaType == PostMediaType.Image,
        MediaFilterMode.Videos => post.MediaType == PostMediaType.Video,
        MediaFilterMode.Gifs => post.MediaType == PostMediaType.Gif,
        MediaFilterMode.Animated => post.IsPlayableMedia || post.GetTagList()
            .Any(t => t.Equals("animated", StringComparison.OrdinalIgnoreCase)),
        _ => true,
    };
}
