using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Rule34Gallery.Maui.ViewModels;

[QueryProperty(nameof(Kind), "kind")]
public partial class PresetPickerViewModel : ObservableObject
{
    private readonly AppServices _app = AppServices.Current;

    [ObservableProperty]
    private string _kind = "search";

    public IReadOnlyList<SearchPreset> SearchPresets => SearchPresetCatalog.All;

    public IReadOnlyList<BlacklistPreset> BlacklistPresets => BlacklistPresetCatalog.All;

    [RelayCommand]
    private async Task ToggleSearchPresetAsync(SearchPreset preset)
    {
        if (_app.Settings.ActiveSearchPresetIds.Contains(preset.Id))
        {
            _app.Settings.ActiveSearchPresetIds.Remove(preset.Id);
            foreach (var tag in preset.Tags)
            {
                _app.Settings.RemoveIncludeTag(tag);
            }
        }
        else
        {
            _app.Settings.ActiveSearchPresetIds.Add(preset.Id);
            foreach (var tag in preset.Tags)
            {
                _app.Settings.AddIncludeTag(tag);
            }
        }

        _app.Gallery.ScheduleSaveSettings();
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task ToggleBlacklistPresetAsync(BlacklistPreset preset)
    {
        if (_app.Settings.ActiveBlacklistPresetIds.Contains(preset.Id))
        {
            _app.Settings.ActiveBlacklistPresetIds.Remove(preset.Id);
            foreach (var tag in preset.Tags)
            {
                _app.Settings.RemoveBlacklistTag(tag);
            }
        }
        else
        {
            _app.Settings.ActiveBlacklistPresetIds.Add(preset.Id);
            foreach (var tag in preset.Tags)
            {
                _app.Settings.AddBlacklistTag(tag);
            }
        }

        _app.Gallery.ScheduleSaveSettings();
        await Shell.Current.GoToAsync("..");
    }
}
