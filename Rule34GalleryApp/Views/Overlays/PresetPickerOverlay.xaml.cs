using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Rule34Gallery.Core;
using Rule34GalleryApp.Services;

namespace Rule34GalleryApp.Views.Overlays;

public partial class PresetPickerOverlay : UserControl
{
    private UserSettings? _settings;
    private Action? _onChanged;
    private PresetPickerKind _kind;

    public PresetPickerOverlay()
    {
        InitializeComponent();
    }

    public void Show(PresetPickerKind kind, UserSettings settings, Action onChanged)
    {
        _settings = settings;
        _onChanged = onChanged;
        _kind = kind;
        SearchInput.Text = string.Empty;
        ConfigureChrome(kind);
        RebuildToggles();
        Visibility = Visibility.Visible;
        SearchInput.Focus();
    }

    public void Hide()
    {
        Visibility = Visibility.Collapsed;
        SearchInput.Text = string.Empty;
        _settings = null;
        _onChanged = null;
    }

    private void ConfigureChrome(PresetPickerKind kind)
    {
        SaveTagsButton.Visibility = kind == PresetPickerKind.Search
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (kind == PresetPickerKind.Search)
        {
            HeaderText.Text = "Search presets";
            DescriptionText.Text =
                "Saved tags appear at the top. Each preset adds tags that must all match (AND). " +
                "You can enable several presets; their tags combine with your manual search tags.";
            SearchInput.ToolTip = "Filter presets by name, description, or tag (e.g. pokemon, milf)";
        }
        else
        {
            HeaderText.Text = "Exclude topic presets";
            DescriptionText.Text =
                "Each preset excludes a bundle of related tags. Enabled presets add red exclude chips " +
                "and send -tag to the API.";
            SearchInput.ToolTip = "Filter presets by name, description, or tag (e.g. furry, gore)";
        }
    }

    private void RebuildToggles()
    {
        if (_settings is null)
        {
            return;
        }

        var filter = SearchInput.Text;
        int visibleCount;

        if (_kind == PresetPickerKind.Search)
        {
            visibleCount = SearchPresetUi.RebuildPresetToggles(
                PresetsPanel,
                _settings,
                OnPresetChanged,
                () => false,
                filter);
            SearchPresetUi.UpdatePresetSummary(SummaryText, _settings);
        }
        else
        {
            visibleCount = BlacklistPresetUi.RebuildPresetToggles(
                PresetsPanel,
                _settings,
                OnPresetChanged,
                () => false,
                filter);
            BlacklistPresetUi.UpdatePresetSummary(SummaryText, _settings);
        }

        NoResultsText.Visibility = visibleCount == 0 && !string.IsNullOrWhiteSpace(filter)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnPresetChanged()
    {
        if (_settings is null)
        {
            return;
        }

        RebuildToggles();
        _onChanged?.Invoke();
    }

    private void SearchInput_OnTextChanged(object sender, TextChangedEventArgs e) => RebuildToggles();

    private void SearchInput_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        if (!string.IsNullOrEmpty(SearchInput.Text))
        {
            SearchInput.Text = string.Empty;
        }
        else
        {
            Hide();
        }

        e.Handled = true;
    }

    private void SaveTagsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_settings is null || _kind != PresetPickerKind.Search)
        {
            return;
        }

        var owner = Window.GetWindow(this);
        if (owner is null)
        {
            return;
        }

        if (!SavedTagPresetDialog.TryPromptSave(owner, _settings, out var error))
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                AppServices.Current.Messenger.Show("Save tags", error, AppMessageKind.Info);
            }

            return;
        }

        RebuildToggles();
        _onChanged?.Invoke();
        AppServices.Current.Messenger.Show(
            "Tags saved",
            "Your combination is at the top under Saved tags.",
            AppMessageKind.Info);
    }

    private void DoneButton_OnClick(object sender, RoutedEventArgs e) => Hide();

    private void PresetPickerOverlay_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && !SearchInput.IsKeyboardFocusWithin)
        {
            Hide();
            e.Handled = true;
        }
    }

    private void Backdrop_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource == sender)
        {
            Hide();
        }
    }

    private void DialogPanel_OnMouseDown(object sender, MouseButtonEventArgs e)
        => e.Handled = true;
}
