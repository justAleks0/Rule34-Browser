using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Rule34Gallery.Core;

namespace Rule34GalleryApp.Services;

public static class SavedTagPresetEditDialog
{
    public static bool TryEdit(
        Window owner,
        string currentName,
        IReadOnlyList<string> currentTags,
        out string name,
        out List<string> tags)
    {
        name = string.Empty;
        tags = [];

        var dialog = new Window
        {
            Title = "Edit tag set",
            Width = 440,
            Height = 280,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = owner,
            Background = Application.Current.Resources["SurfaceRaisedBrush"] as Brush,
            ResizeMode = ResizeMode.NoResize,
        };

        var nameBox = new TextBox
        {
            Text = currentName,
            Style = Application.Current.Resources["R34FieldTextBox"] as Style,
            Margin = new Thickness(16, 8, 16, 8),
        };

        var tagsBox = new TextBox
        {
            Text = string.Join(" ", currentTags),
            Style = Application.Current.Resources["R34FieldTextBox"] as Style,
            Margin = new Thickness(16, 0, 16, 8),
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 72,
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
            Text = "Name",
            Margin = new Thickness(16, 16, 16, 0),
            Foreground = Application.Current.Resources["TextBrush"] as Brush,
        });
        panel.Children.Add(nameBox);
        panel.Children.Add(new TextBlock
        {
            Text = "Tags (space-separated)",
            Margin = new Thickness(16, 8, 16, 0),
            Foreground = Application.Current.Resources["TextBrush"] as Brush,
        });
        panel.Children.Add(tagsBox);
        panel.Children.Add(buttons);
        dialog.Content = panel;

        saveButton.Click += (_, _) => { dialog.DialogResult = true; dialog.Close(); };
        cancelButton.Click += (_, _) => { dialog.DialogResult = false; dialog.Close(); };

        if (dialog.ShowDialog() != true)
        {
            return false;
        }

        name = nameBox.Text;
        tags = tagsBox.Text
            .Split([' ', '\t', ',', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(UserSettings.NormalizeTagToken)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return tags.Count > 0;
    }
}
