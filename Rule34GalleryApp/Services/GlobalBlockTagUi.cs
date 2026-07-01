using System.Windows;
using System.Windows.Controls;
using Rule34GalleryApp.Controls;

namespace Rule34GalleryApp.Services;

public static class GlobalBlockTagUi
{
    public static void RebuildChips(Panel panel, UserSettings settings, Action onChanged)
    {
        panel.Children.Clear();

        foreach (var tag in settings.GetGlobalBlockedTags().OrderBy(t => t, StringComparer.OrdinalIgnoreCase))
        {
            panel.Children.Add(CreateChip(tag, settings, onChanged));
        }

        panel.Visibility = panel.Children.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static RemovableTagChip CreateChip(string tag, UserSettings settings, Action onChanged)
    {
        var chip = new RemovableTagChip
        {
            TagText = tag,
            IsExcluded = true,
            ToolTip = "Always hidden app-wide — remove in Settings",
        };

        chip.RemoveClicked += (_, _) =>
        {
            settings.RemoveGlobalBlockedTag(tag);
            onChanged();
        };

        return chip;
    }
}
