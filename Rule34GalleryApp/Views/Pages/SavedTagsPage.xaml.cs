using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Rule34Gallery.Core;
using Rule34Gallery.Core.Services;
using Rule34GalleryApp.Services;

namespace Rule34GalleryApp.Views.Pages;

public partial class SavedTagsPage : Page
{
    private readonly AppServices _app = AppServices.Current;
    private readonly ObservableCollection<SavedTagPresetRow> _rows = [];

    public SavedTagsPage()
    {
        InitializeComponent();
        PresetsList.ItemsSource = _rows;
    }

    public void RefreshUi()
    {
        _rows.Clear();
        foreach (var preset in _app.Settings.SavedSearchPresets)
        {
            _rows.Add(SavedTagPresetRow.From(preset));
        }

        StatusText.Text = _rows.Count == 0
            ? "No saved tag sets yet. Save your current search or add tags on Browse."
            : $"{_rows.Count} saved tag set(s).";
    }

    private void SaveCurrentButton_OnClick(object sender, RoutedEventArgs e)
    {
        var owner = Window.GetWindow(this);
        if (owner is null)
        {
            return;
        }

        if (!SavedTagPresetDialog.TryPromptSave(owner, _app.Settings, out var error))
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                StatusText.Text = error;
            }

            return;
        }

        _app.SaveSettings();
        RefreshUi();
        StatusText.Text = "Tag set saved.";
    }

    private async void SyncCloudButton_OnClick(object sender, RoutedEventArgs e)
    {
        SyncCloudButton.IsEnabled = false;
        StatusText.Text = "Syncing…";
        LoadingOverlay.Show("Syncing tag sets…");

        try
        {
            if (!_app.Library.IsSignedIn)
            {
                StatusText.Text = "Sign in on Account to sync tag sets.";
                return;
            }

            await _app.Library.SyncSavedTagPresetsFromCloudAsync().ConfigureAwait(true);
            await _app.Library.SaveSavedTagPresetsToCloudAsync().ConfigureAwait(true);
            _app.ReloadSettings();
            RefreshUi();
            StatusText.Text = "Tag sets synced with cloud.";
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
        finally
        {
            LoadingOverlay.Hide();
            SyncCloudButton.IsEnabled = true;
        }
    }

    private void ApplyButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: SavedTagPresetRow row })
        {
            return;
        }

        ApplyPreset(row.Id);
    }

    private void EditButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: SavedTagPresetRow row })
        {
            return;
        }

        var preset = _app.Settings.FindSavedSearchPreset(row.Id);
        if (preset is null)
        {
            RefreshUi();
            return;
        }

        var owner = Window.GetWindow(this);
        if (owner is null)
        {
            return;
        }

        if (!SavedTagPresetEditDialog.TryEdit(owner, preset.Name, preset.Tags, out var name, out var tags))
        {
            return;
        }

        if (!_app.Settings.UpdateSavedSearchPreset(row.Id, name, tags))
        {
            StatusText.Text = "Could not update tag set.";
            return;
        }

        _app.SaveSettings();
        RefreshUi();
        StatusText.Text = "Tag set updated.";
    }

    private void DeleteButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: SavedTagPresetRow row })
        {
            return;
        }

        if (MessageBox.Show(
                Window.GetWindow(this),
                $"Delete \"{row.Name}\"?",
                "Delete tag set",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        _app.Settings.RemoveSavedSearchPreset(row.Id);
        _app.SaveSettings();
        RefreshUi();
        StatusText.Text = "Tag set deleted.";
    }

    private void PresetsList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PresetsList.SelectedItem is SavedTagPresetRow row)
        {
            ApplyPreset(row.Id);
            PresetsList.SelectedItem = null;
        }
    }

    private void ApplyPreset(string presetId)
    {
        var preset = _app.Settings.FindSavedSearchPreset(presetId);
        if (preset is null)
        {
            RefreshUi();
            return;
        }

        _app.Settings.IncludeTags = preset.Tags.ToList();
        _app.Settings.SyncTagsString();
        if (!_app.Settings.ActiveSearchPresetIds.Contains(presetId, StringComparer.OrdinalIgnoreCase))
        {
            _app.Settings.ActiveSearchPresetIds.Add(presetId);
        }

        _app.SaveSettings();
        StatusText.Text = $"Applied \"{preset.Name}\" to search tags.";
    }

    private sealed class SavedTagPresetRow
    {
        public string Id { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string TagsDisplay { get; init; } = string.Empty;

        public static SavedTagPresetRow From(SavedTagPreset preset) => new()
        {
            Id = preset.Id,
            Name = preset.Name,
            TagsDisplay = string.Join(" · ", preset.Tags),
        };
    }
}
