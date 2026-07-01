using System.Windows;
using System.Windows.Controls;
using Rule34GalleryApp.Controls;

namespace Rule34GalleryApp.Services;

public static class BlacklistTagUi
{
    public static void RebuildChips(Panel panel, UserSettings settings, Action onChanged)
    {
        panel.Children.Clear();

        var presetTags = settings.GetPresetBlacklistTags().ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var tag in settings.BlacklistTags.OrderBy(t => t, StringComparer.OrdinalIgnoreCase))
        {
            var fromPreset = presetTags.Contains(tag);
            panel.Children.Add(CreateChip(tag, isManual: true, fromPreset, settings, onChanged));
        }

        panel.Visibility = panel.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static RemovableTagChip CreateChip(
        string tag,
        bool isManual,
        bool fromPreset,
        UserSettings settings,
        Action onChanged)
    {
        var chip = new RemovableTagChip
        {
            TagText = tag,
            IsExcluded = true,
        };

        if (fromPreset)
        {
            var presetNames = BlacklistPresetCatalog.All
                .Where(p => settings.IsPresetActive(p.Id) &&
                            p.Tags.Any(t => UserSettings.NormalizeTagToken(t)
                                .Equals(tag, StringComparison.OrdinalIgnoreCase)))
                .Select(p => p.Name)
                .ToList();

            if (presetNames.Count > 0)
            {
                chip.ToolTip = isManual
                    ? $"Excluded manually and via preset: {string.Join(", ", presetNames)}"
                    : $"From preset: {string.Join(", ", presetNames)}";
            }
        }

        chip.RemoveClicked += (_, _) =>
        {
            settings.RemoveBlacklistTag(tag);

            if (fromPreset)
            {
                settings.DeactivatePresetsContainingTag(tag);
            }

            onChanged();
        };

        return chip;
    }
}
