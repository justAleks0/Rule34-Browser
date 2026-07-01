using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Rule34Gallery.Core.CloudSync;

namespace Rule34GalleryApp.Services;

public static class SyncApplyDialog
{
    public static bool TryConfirm(
        Window owner,
        SyncDirection direction,
        out SyncApplyMode mode,
        bool allowSelectItems = true)
    {
        mode = SyncApplyMode.MergeSkipDuplicates;
        var title = direction == SyncDirection.Upload ? "Upload to cloud" : "Download from cloud";

        var window = new Window
        {
            Title = title,
            Width = 460,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = owner,
            Background = (Brush)owner.FindResource("BgBrush"),
            ResizeMode = ResizeMode.NoResize,
        };

        var root = new StackPanel { Margin = new Thickness(20) };
        root.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)owner.FindResource("TextBrush"),
            Margin = new Thickness(0, 0, 0, 8),
        });

        var merge = new RadioButton
        {
            Content = "Merge — keep both sides; duplicates skipped at item level",
            IsChecked = true,
            Foreground = (Brush)owner.FindResource("TextBrush"),
            Margin = new Thickness(0, 0, 0, 6),
        };
        var replace = new RadioButton
        {
            Content = direction == SyncDirection.Upload
                ? "Replace all on cloud — overwrites remote data with this device"
                : "Replace all locally — overwrites this device with cloud data",
            Foreground = (Brush)owner.FindResource("TextBrush"),
            Margin = new Thickness(0, 0, 0, 6),
        };
        RadioButton? select = null;
        if (allowSelectItems)
        {
            select = new RadioButton
            {
                Content = "Select items — apply only checked rows in the data explorer",
                Foreground = (Brush)owner.FindResource("TextBrush"),
                Margin = new Thickness(0, 0, 0, 6),
            };
        }
        root.Children.Add(new TextBlock
        {
            Text = "Choose how to apply changes:",
            Foreground = (Brush)owner.FindResource("MutedBrush"),
            Margin = new Thickness(0, 0, 0, 12),
            TextWrapping = TextWrapping.Wrap,
        });
        root.Children.Add(merge);
        root.Children.Add(replace);
        if (select is not null)
        {
            root.Children.Add(select);
        }

        var warning = new TextBlock
        {
            Foreground = new SolidColorBrush(Color.FromRgb(0xC4, 0x5C, 0x5C)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 10, 0, 0),
            Visibility = Visibility.Collapsed,
        };
        root.Children.Add(warning);

        void UpdateWarning()
        {
            warning.Visibility = replace.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            warning.Text = replace.IsChecked == true
                ? direction == SyncDirection.Upload
                    ? "Warning: cloud copies of favorites, lists, tag sets, and For You will be replaced."
                    : "Warning: local favorites, lists, tag sets, credentials, and For You will be replaced."
                : string.Empty;
        }

        merge.Checked += (_, _) => UpdateWarning();
        replace.Checked += (_, _) => UpdateWarning();

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0),
        };
        var cancel = new Button { Content = "Cancel", Margin = new Thickness(0, 0, 8, 0), MinWidth = 88 };
        var ok = new Button { Content = direction == SyncDirection.Upload ? "Upload" : "Download", MinWidth = 100 };
        if (owner.TryFindResource("PrimaryButton") is Style primary)
        {
            ok.Style = primary;
        }

        cancel.Click += (_, _) =>
        {
            window.DialogResult = false;
            window.Close();
        };
        SyncApplyMode chosenMode = SyncApplyMode.MergeSkipDuplicates;
        ok.Click += (_, _) =>
        {
            chosenMode = replace.IsChecked == true
                ? SyncApplyMode.ReplaceAll
                : select?.IsChecked == true
                    ? SyncApplyMode.SelectItems
                    : SyncApplyMode.MergeSkipDuplicates;
            window.DialogResult = true;
            window.Close();
        };

        buttons.Children.Add(cancel);
        buttons.Children.Add(ok);
        root.Children.Add(buttons);
        window.Content = root;

        if (window.ShowDialog() != true)
        {
            return false;
        }

        mode = chosenMode;
        return true;
    }
}
