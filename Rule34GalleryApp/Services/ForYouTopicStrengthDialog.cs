using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Rule34Gallery.Core;

namespace Rule34GalleryApp.Services;

public static class ForYouTopicStrengthDialog
{
    public static bool TryPrompt(
        Window owner,
        string title,
        string? initialTag,
        double initialStrength,
        bool allowTagEdit,
        out string tag,
        out double strength)
    {
        tag = string.Empty;
        strength = 0;

        var dialog = new Window
        {
            Title = title,
            Width = 420,
            Height = allowTagEdit ? 240 : 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = owner,
            Background = Application.Current.Resources["SurfaceRaisedBrush"] as Brush,
            ResizeMode = ResizeMode.NoResize,
        };

        var tagBox = new TextBox
        {
            Text = initialTag ?? string.Empty,
            IsReadOnly = !allowTagEdit,
            Style = Application.Current.Resources["R34FieldTextBox"] as Style,
            Margin = new Thickness(16, 8, 16, 4),
        };

        var strengthLabel = new TextBlock
        {
            Margin = new Thickness(16, 4, 16, 0),
            Foreground = Application.Current.Resources["TextBrush"] as Brush,
        };

        var strengthSlider = new Slider
        {
            Minimum = 0,
            Maximum = 100,
            Value = Math.Clamp(initialStrength, 0, 100),
            Margin = new Thickness(16, 4, 16, 4),
        };

        void UpdateStrengthLabel()
            => strengthLabel.Text = $"Score: {strengthSlider.Value:F1}";

        strengthSlider.ValueChanged += (_, _) => UpdateStrengthLabel();
        UpdateStrengthLabel();

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
            Text = allowTagEdit ? "Tag name:" : "Topic:",
            Margin = new Thickness(16, 16, 16, 0),
            Foreground = Application.Current.Resources["TextBrush"] as Brush,
        });
        panel.Children.Add(tagBox);
        panel.Children.Add(strengthLabel);
        panel.Children.Add(strengthSlider);
        panel.Children.Add(new TextBlock
        {
            Text = "0 = weak interest · 100 = strongest",
            Margin = new Thickness(16, 0, 16, 0),
            Foreground = Application.Current.Resources["MutedBrush"] as Brush,
            FontSize = 11,
        });
        panel.Children.Add(buttons);
        dialog.Content = panel;

        saveButton.Click += (_, _) => { dialog.DialogResult = true; dialog.Close(); };
        cancelButton.Click += (_, _) => { dialog.DialogResult = false; dialog.Close(); };

        if (dialog.ShowDialog() != true)
        {
            return false;
        }

        tag = UserSettings.NormalizeTagToken(tagBox.Text);
        if (string.IsNullOrWhiteSpace(tag))
        {
            return false;
        }

        strength = Math.Clamp(
            double.Parse(strengthSlider.Value.ToString("F1", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture),
            0,
            100);
        return true;
    }
}
