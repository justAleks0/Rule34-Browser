using System.Windows;
using System.Windows.Controls;
using Rule34Gallery.Core.Services;

namespace Rule34GalleryApp.Services;

public static class SearchPresetUi
{
    public static int RebuildPresetToggles(
        Panel container,
        UserSettings settings,
        Action onChanged,
        Func<bool> suppressEvents,
        string? searchFilter = null,
        Action<string>? onDeleteSaved = null)
    {
        container.Children.Clear();
        var visibleCount = 0;

        var savedPresets = settings.GetSavedSearchPresetsAsSearchPresets()
            .Where(p => PresetFilter.Matches(searchFilter, p.Name, p.Description, p.Id, p.Tags))
            .ToList();

        if (savedPresets.Count > 0)
        {
            container.Children.Add(CreateSectionHeader("Saved tags"));
            foreach (var preset in savedPresets)
            {
                visibleCount++;
                container.Children.Add(CreatePresetRow(
                    preset,
                    settings,
                    onChanged,
                    suppressEvents,
                    isSaved: true,
                    onDeleteSaved));
            }
        }

        var builtIn = SearchPresetCatalog.All
            .Where(p => PresetFilter.Matches(searchFilter, p.Name, p.Description, p.Id, p.Tags))
            .ToList();

        if (builtIn.Count > 0)
        {
            if (savedPresets.Count > 0)
            {
                container.Children.Add(CreateSectionHeader("Built-in presets"));
            }

            foreach (var preset in builtIn)
            {
                visibleCount++;
                container.Children.Add(CreatePresetRow(
                    preset,
                    settings,
                    onChanged,
                    suppressEvents,
                    isSaved: false,
                    onDeleteSaved: null));
            }
        }

        return visibleCount;
    }

    private static TextBlock CreateSectionHeader(string text)
        => new()
        {
            Text = text,
            Foreground = Application.Current.Resources["SubtleBrush"] as System.Windows.Media.Brush,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 8, 0, 6),
            Width = 480,
        };

    private static UIElement CreatePresetRow(
        SearchPreset preset,
        UserSettings settings,
        Action onChanged,
        Func<bool> suppressEvents,
        bool isSaved,
        Action<string>? onDeleteSaved)
    {
        var isActive = settings.IsSearchPresetActive(preset.Id);
        var toggle = new CheckBox
        {
            Content = preset.Name,
            IsChecked = isActive,
            ToolTip = $"{preset.Description}\n\nRequires ALL: {string.Join(" · ", preset.Tags)}",
            VerticalAlignment = VerticalAlignment.Center,
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

            settings.SetSearchPresetActive(preset.Id, true);
            toggle.FontWeight = FontWeights.SemiBold;
            onChanged();
        };

        toggle.Unchecked += (_, _) =>
        {
            if (suppressEvents())
            {
                return;
            }

            settings.SetSearchPresetActive(preset.Id, false);
            toggle.FontWeight = FontWeights.Normal;
            onChanged();
        };

        if (!isSaved)
        {
            toggle.Margin = new Thickness(0, 0, 8, 6);
            return toggle;
        }

        var deleteButton = new Button
        {
            Content = "×",
            ToolTip = "Remove saved tags",
            Padding = new Thickness(8, 2, 8, 2),
            Margin = new Thickness(4, 0, 0, 6),
            MinWidth = 28,
            Style = Application.Current.TryFindResource("SecondaryButton") as Style,
        };

        deleteButton.Click += (_, _) =>
        {
            if (suppressEvents())
            {
                return;
            }

            settings.RemoveSavedSearchPreset(preset.Id);
            onDeleteSaved?.Invoke(preset.Id);
            onChanged();
        };

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 8, 0),
        };
        row.Children.Add(toggle);
        row.Children.Add(deleteButton);
        return row;
    }

    public static void UpdatePresetSummary(TextBlock summary, UserSettings settings)
    {
        var activePresets = settings.ActiveSearchPresetIds
            .Select(settings.ResolveSearchPreset)
            .Where(p => p is not null)
            .ToList()!;

        if (activePresets.Count == 0)
        {
            summary.Text = "Each preset adds tags that must all match (AND).";
            return;
        }

        var parts = activePresets.Select(p => $"{p!.Name} ({string.Join(" + ", p.Tags)})");
        summary.Text = string.Join("  |  ", parts);
    }

    public static void UpdatePresetButton(Button button, UserSettings settings)
    {
        var count = settings.ActiveSearchPresetIds.Count;
        button.Content = count == 0 ? "Search presets…" : $"Search presets ({count})";
        button.ToolTip = count == 0
            ? "Quick searches where every tag in a bundle must match"
            : $"{count} preset(s) active — click to change";
    }
}
