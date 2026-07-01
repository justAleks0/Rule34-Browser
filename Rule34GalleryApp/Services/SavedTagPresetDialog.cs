using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Rule34Gallery.Core;

namespace Rule34GalleryApp.Services;

public static class SavedTagPresetDialog
{
    public static bool TryPromptSave(Window owner, UserSettings settings, out string? error)
    {
        error = null;
        var tags = settings.GetApiIncludeTags().ToList();
        if (tags.Count == 0)
        {
            error = "Add at least one search tag or enable a preset before saving.";
            return false;
        }

        var defaultName = tags.Count <= 3
            ? string.Join(" + ", tags)
            : string.Join(" + ", tags.Take(3)) + "…";

        if (!TryPromptName(owner, "Save tags", tags, defaultName, out var name))
        {
            return false;
        }

        var id = settings.AddSavedSearchPreset(name, tags);
        if (id is null)
        {
            error = "Nothing to save.";
            return false;
        }

        AppServices.Current.SaveSettings();
        return true;
    }

    public static bool TryPromptSaveTags(Window owner, IEnumerable<string> tags, out string? error)
    {
        error = null;
        var list = tags
            .Select(UserSettings.NormalizeTagToken)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (list.Count == 0)
        {
            error = "No tags to save.";
            return false;
        }

        var defaultName = list.Count <= 3
            ? string.Join(" + ", list)
            : string.Join(" + ", list.Take(3)) + "…";

        if (!TryPromptName(owner, "Save tags", list, defaultName, out var name))
        {
            return false;
        }

        var id = AppServices.Current.Settings.AddSavedSearchPreset(name, list);
        if (id is null)
        {
            error = "Nothing to save.";
            return false;
        }

        AppServices.Current.SaveSettings();
        return true;
    }

    private static bool TryPromptName(
        Window owner,
        string title,
        IReadOnlyList<string> tags,
        string defaultName,
        out string name)
    {
        name = string.Empty;
        var dialog = new Window
        {
            Title = title,
            Width = 420,
            Height = 220,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = owner,
            Background = Application.Current.Resources["SurfaceRaisedBrush"] as Brush,
            ResizeMode = ResizeMode.NoResize,
        };

        var nameBox = new TextBox
        {
            Text = defaultName,
            Style = Application.Current.Resources["R34FieldTextBox"] as Style,
            Margin = new Thickness(16, 8, 16, 8),
        };

        var saveButton = new Button
        {
            Content = "Save",
            Style = Application.Current.Resources["PrimaryButton"] as Style,
            Margin = new Thickness(16, 0, 8, 16),
            Padding = new Thickness(12, 6, 12, 6),
            IsDefault = true,
        };

        var cancelButton = new Button
        {
            Content = "Cancel",
            Style = Application.Current.Resources["SecondaryButton"] as Style,
            Margin = new Thickness(0, 0, 16, 16),
            Padding = new Thickness(12, 6, 12, 6),
            IsCancel = true,
        };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
        };
        buttons.Children.Add(saveButton);
        buttons.Children.Add(cancelButton);

        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = "Name this tag combination:",
            Margin = new Thickness(16, 16, 16, 0),
            Foreground = Application.Current.Resources["TextBrush"] as Brush,
        });
        panel.Children.Add(new TextBlock
        {
            Text = string.Join(" · ", tags),
            Margin = new Thickness(16, 4, 16, 0),
            Foreground = Application.Current.Resources["MutedBrush"] as Brush,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
        });
        panel.Children.Add(nameBox);
        panel.Children.Add(buttons);
        dialog.Content = panel;

        saveButton.Click += (_, _) => { dialog.DialogResult = true; dialog.Close(); };
        cancelButton.Click += (_, _) => { dialog.DialogResult = false; dialog.Close(); };

        if (dialog.ShowDialog() != true)
        {
            return false;
        }

        name = nameBox.Text;
        return true;
    }
}
