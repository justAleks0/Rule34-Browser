using System.Windows;
using System.Windows.Controls;
using Rule34GalleryApp.Controls;

namespace Rule34GalleryApp.Services;

public static class SearchPresetTagUi
{
    public static void RebuildBundleChips(Panel panel, UserSettings settings, Action onChanged)
    {
        panel.Children.Clear();

        foreach (var presetId in settings.ActiveSearchPresetIds)
        {
            var preset = settings.ResolveSearchPreset(presetId);
            if (preset is null)
            {
                continue;
            }

            panel.Children.Add(CreateBundleChip(preset, settings, onChanged));
        }

        panel.Visibility = panel.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static RemovableTagChip CreateBundleChip(SearchPreset preset, UserSettings settings, Action onChanged)
    {
        var tagList = string.Join(" · ", preset.Tags);
        var chip = new RemovableTagChip
        {
            TagText = preset.Name,
            IsBundle = true,
            ToolTip = $"Requires ALL tags: {tagList}",
        };

        chip.RemoveClicked += (_, _) =>
        {
            settings.SetSearchPresetActive(preset.Id, false);
            onChanged();
        };

        return chip;
    }
}
