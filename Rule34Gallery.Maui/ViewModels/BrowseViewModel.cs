using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Rule34Gallery.Maui.ViewModels;

public partial class BrowseViewModel : GalleryViewModelBase
{
    [ObservableProperty]
    private string _tagInput = string.Empty;

    [ObservableProperty]
    private string _blacklistInput = string.Empty;

    public ObservableCollection<TagSuggestion> Autocomplete => App.Autocomplete;

    public ObservableCollection<TagSuggestion> BlacklistAutocomplete => App.BlacklistAutocomplete;

    public UserSettings Settings => App.Settings;

    public BrowseViewModel()
    {
        App.Gallery.StatusChanged += (_, status) => StatusText = status;
        App.Gallery.PostsChanged += (_, _) => OnPropertyChanged(nameof(Posts));
    }

    [RelayCommand]
    private async Task AddIncludeTagAsync()
    {
        var tag = TagInput.Trim();
        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        App.Settings.AddIncludeTag(tag);
        TagInput = string.Empty;
        App.Gallery.ScheduleSaveSettings();
        await SearchCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task AddBlacklistTagAsync()
    {
        var tag = BlacklistInput.Trim();
        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        App.Settings.AddBlacklistTag(tag);
        BlacklistInput = string.Empty;
        App.Gallery.ScheduleSaveSettings();
    }

    [RelayCommand]
    private async Task UpdateAutocompleteAsync()
    {
        await TagAutocompleteService.TryPopulateSuggestionsAsync(App.Http, App.Autocomplete, TagInput);
    }

    [RelayCommand]
    private async Task UpdateBlacklistAutocompleteAsync()
    {
        await TagAutocompleteService.TryPopulateSuggestionsAsync(App.Http, App.BlacklistAutocomplete, BlacklistInput);
    }

    [RelayCommand]
    private void RemoveIncludeTag(string tag)
    {
        App.Settings.RemoveIncludeTag(tag);
        App.Gallery.ScheduleSaveSettings();
    }

    [RelayCommand]
    private void RemoveBlacklistTag(string tag)
    {
        App.Settings.RemoveBlacklistTag(tag);
        App.Gallery.ScheduleSaveSettings();
    }

    [RelayCommand]
    private async Task OpenTagDiscoverAsync()
    {
        await Shell.Current.GoToAsync(nameof(Views.TagDiscoverPage));
    }

    [RelayCommand]
    private async Task OpenPresetsAsync()
    {
        await Shell.Current.GoToAsync($"{nameof(Views.PresetPickerPage)}?kind=search");
    }

    [RelayCommand]
    private async Task OpenBlacklistPresetsAsync()
    {
        await Shell.Current.GoToAsync($"{nameof(Views.PresetPickerPage)}?kind=blacklist");
    }
}
