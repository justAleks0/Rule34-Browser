using System.Windows.Controls;

namespace Rule34GalleryApp.Services;

public static class SearchFilterUi
{
    public static int CountActiveFilters(UserSettings settings)
    {
        var count = 0;
        if (!settings.RatingSafe || !settings.RatingQuestionable || !settings.RatingExplicit)
        {
            count++;
        }

        if (settings.MediaFilter != MediaFilterMode.All)
        {
            count++;
        }

        if (settings.SortMode != SearchSortMode.ScoreDesc)
        {
            count++;
        }

        if (settings.LimitIndex != 1)
        {
            count++;
        }

        if (settings.MinScore > 0)
        {
            count++;
        }

        if (settings.MinWidth > 0)
        {
            count++;
        }

        if (settings.MinHeight > 0)
        {
            count++;
        }

        if (!string.IsNullOrWhiteSpace(settings.ArtistFilter))
        {
            count++;
        }

        if (!string.IsNullOrWhiteSpace(settings.CharacterFilter))
        {
            count++;
        }

        if (!string.IsNullOrWhiteSpace(settings.CopyrightFilter))
        {
            count++;
        }

        return count;
    }

    public static void UpdateFiltersButton(Button button, UserSettings settings)
    {
        var count = CountActiveFilters(settings);
        button.Content = count == 0 ? "Filter results…" : $"Filter results ({count} active)";
        button.ToolTip = count == 0
            ? "Rating, media type, sort order, size limits, and more"
            : $"{count} filter(s) active — click to change";
    }
}
