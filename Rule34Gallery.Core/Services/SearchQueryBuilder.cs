namespace Rule34Gallery.Core.Services;

public static class SearchQueryBuilder
{
    public static string BuildApiTags(UserSettings settings)
        => BuildApiTags(settings.ActiveSource, settings);

    public static string BuildApiTags(GallerySource source, UserSettings settings)
    {
        var parts = new List<string>();

        foreach (var tag in settings.GetApiIncludeTags())
        {
            AddPositiveTag(parts, tag);
        }

        AppendMetaFilter(parts, "artist", settings.ArtistFilter);
        AppendMetaFilter(parts, "character", settings.CharacterFilter);
        AppendMetaFilter(parts, "copyright", settings.CopyrightFilter);

        AppendRatingTags(source, parts, settings);
        AppendMediaTags(source, parts, settings.MediaFilter);
        AppendSortTag(source, parts, settings.SortMode);

        if (settings.MinScore > 0)
        {
            parts.Add(source is GallerySource.Rule34
                ? $"score:>={settings.MinScore}"
                : $"score:>{settings.MinScore}");
        }

        foreach (var tag in TagBlockFilter.GetAllBlockingTags(settings))
        {
            AddExcludedTag(parts, tag);
        }

        if (source == GallerySource.Rule34)
        {
            if (settings.FilterAi)
            {
                parts.RemoveAll(t => t.Equals("-ai*", StringComparison.OrdinalIgnoreCase));
                parts = parts
                    .Where(t => !t.StartsWith("ai", StringComparison.OrdinalIgnoreCase) ||
                                t.StartsWith('-'))
                    .ToList();
                parts.Add("-ai*");
            }
            else
            {
                parts.RemoveAll(t => t.Equals("-ai*", StringComparison.OrdinalIgnoreCase));
            }
        }

        return string.Join(' ', DeduplicateTags(parts));
    }

    public static string BuildPreview(UserSettings settings)
    {
        var api = BuildApiTags(settings.ActiveSource, settings);
        if (string.IsNullOrWhiteSpace(api))
        {
            return "(no tags — browse recent posts)";
        }

        return api;
    }

    private static void AppendRatingTags(GallerySource source, List<string> parts, UserSettings settings)
    {
        var selected = new List<string>();
        if (settings.RatingSafe)
        {
            selected.Add(source == GallerySource.Rule34 ? "rating:safe" : "rating:s");
        }

        if (settings.RatingQuestionable)
        {
            selected.Add(source == GallerySource.Rule34 ? "rating:questionable" : "rating:q");
        }

        if (settings.RatingExplicit)
        {
            selected.Add(source == GallerySource.Rule34 ? "rating:explicit" : "rating:e");
        }

        if (selected.Count is 0 or 3)
        {
            return;
        }

        parts.AddRange(selected);
    }

    private static void AppendMediaTags(GallerySource source, List<string> parts, MediaFilterMode mode)
    {
        switch (mode)
        {
            case MediaFilterMode.Images:
                if (source == GallerySource.Rule34)
                {
                    parts.Add("-video");
                    parts.Add("-animated");
                }
                else if (source == GallerySource.Danbooru)
                {
                    parts.Add("-animated");
                    parts.Add("-video");
                    parts.Add("type:jpg");
                }
                else
                {
                    parts.Add("-animated");
                    parts.Add("-video");
                }

                break;
            case MediaFilterMode.Videos:
                parts.Add("video");
                if (source == GallerySource.Danbooru)
                {
                    parts.Add("type:webm");
                }

                break;
            case MediaFilterMode.Gifs:
                parts.Add("animated");
                parts.Add("-video");
                if (source == GallerySource.Danbooru)
                {
                    parts.Add("-type:webm");
                }

                break;
            case MediaFilterMode.Animated:
                parts.Add("animated");
                break;
        }
    }

    private static void AppendSortTag(GallerySource source, List<string> parts, SearchSortMode mode)
    {
        if (source == GallerySource.Rule34)
        {
            var sortTag = mode switch
            {
                SearchSortMode.ScoreAsc => "sort:score:asc",
                SearchSortMode.DateDesc => "sort:created_at:desc",
                SearchSortMode.DateAsc => "sort:created_at:asc",
                SearchSortMode.Random => "sort:random",
                _ => "sort:score:desc",
            };

            parts.Add(sortTag);
            return;
        }

        var orderTag = mode switch
        {
            SearchSortMode.ScoreAsc => "order:score_asc",
            SearchSortMode.DateDesc => "order:id_desc",
            SearchSortMode.DateAsc => "order:id_asc",
            SearchSortMode.Random => "order:random",
            _ => "order:score",
        };

        parts.Add(orderTag);
    }

    private static void AppendMetaFilter(List<string> parts, string prefix, string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return;
        }

        if (trimmed.Contains(':', StringComparison.Ordinal))
        {
            AddPositiveTag(parts, trimmed);
            return;
        }

        AddPositiveTag(parts, $"{prefix}:{trimmed.Replace(" ", "_")}");
    }

    private static void AddPositiveTag(List<string> parts, string tag)
    {
        var normalized = NormalizeTag(tag);
        if (string.IsNullOrWhiteSpace(normalized) || normalized.StartsWith('-'))
        {
            return;
        }

        parts.Add(normalized);
    }

    private static void AddExcludedTag(List<string> parts, string tag)
    {
        var normalized = NormalizeTag(tag);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        if (normalized.StartsWith('-'))
        {
            parts.Add(normalized);
        }
        else
        {
            parts.Add("-" + normalized);
        }
    }

    private static string NormalizeTag(string tag)
    {
        var trimmed = tag.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return string.Empty;
        }

        return trimmed.Replace(" ", "_");
    }

    private static IEnumerable<string> DeduplicateTags(IEnumerable<string> tags)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            if (seen.Add(tag))
            {
                yield return tag;
            }
        }
    }
}
