using System.Windows;
using System.Windows.Controls;

namespace Rule34GalleryApp.Services;

public static class BlacklistPresetUi
{
    public static int RebuildPresetToggles(
        Panel container,
        UserSettings settings,
        Action onChanged,
        Func<bool> suppressEvents,
        string? searchFilter = null)
    {
        container.Children.Clear();
        var visibleCount = 0;

        foreach (var preset in BlacklistPresetCatalog.All)
        {
            if (!PresetFilter.Matches(searchFilter, preset.Name, preset.Description, preset.Id, preset.Tags))
            {
                continue;
            }

            visibleCount++;
            var isActive = settings.IsPresetActive(preset.Id);
            var toggle = new CheckBox
            {
                Content = preset.Name,
                IsChecked = isActive,
                ToolTip = $"{preset.Description}\n\nTags: {string.Join(", ", preset.Tags)}",
                Margin = new Thickness(0, 0, 8, 6),
                Style = Application.Current.TryFindResource("R34CheckBox") as Style,
            };

            if (isActive)
            {
                toggle.FontWeight = FontWeights.SemiBold;
            }

            toggle.Checked += (_, _) =>
            {
                if (suppressEvents())
                {
                    return;
                }

                settings.SetPresetActive(preset.Id, true);
                toggle.FontWeight = FontWeights.SemiBold;
                onChanged();
            };

            toggle.Unchecked += (_, _) =>
            {
                if (suppressEvents())
                {
                    return;
                }

                settings.SetPresetActive(preset.Id, false);
                toggle.FontWeight = FontWeights.Normal;
                onChanged();
            };

            container.Children.Add(toggle);
        }

        return visibleCount;
    }

    public static void UpdatePresetSummary(TextBlock summary, UserSettings settings)
    {
        var activePresets = BlacklistPresetCatalog.All
            .Where(p => settings.IsPresetActive(p.Id))
            .ToList();

        if (activePresets.Count == 0)
        {
            summary.Text = "Check topics to block whole tag bundles.";
            return;
        }

        var tagCount = settings.GetPresetBlacklistTags().Count();
        var names = string.Join(", ", activePresets.Select(p => p.Name));
        summary.Text = $"{names} · {tagCount} tags";
    }

    public static void UpdatePresetButton(Button button, UserSettings settings)
    {
        var count = BlacklistPresetCatalog.All.Count(p => settings.IsPresetActive(p.Id));
        button.Content = count == 0 ? "Topic presets…" : $"Topic presets ({count})";
        button.ToolTip = count == 0
            ? "Block whole topics (furry, gore, etc.) without listing every tag"
            : $"{count} preset(s) active — click to change";
    }
}
