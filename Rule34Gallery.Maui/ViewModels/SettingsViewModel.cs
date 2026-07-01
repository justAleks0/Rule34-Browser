using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rule34Gallery.Core.Services;

namespace Rule34Gallery.Maui.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly AppServices _app = AppServices.Current;

    [ObservableProperty]
    private string _userId = string.Empty;

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _openAiApiKey = string.Empty;

    [ObservableProperty]
    private string _openAiModel = "gpt-4o-mini";

    [ObservableProperty]
    private bool _filterAi = true;

    [ObservableProperty]
    private bool _ratingSafe = true;

    [ObservableProperty]
    private bool _ratingQuestionable = true;

    [ObservableProperty]
    private bool _ratingExplicit = true;

    [ObservableProperty]
    private bool _forYouEnabled;

    [ObservableProperty]
    private bool _forYouCloudSync = true;

    [ObservableProperty]
    private bool _forYouLearnArtists = true;

    [ObservableProperty]
    private bool _forYouLearnSeries = true;

    [ObservableProperty]
    private bool _forYouLearnMinorTags;

    public SettingsViewModel()
    {
        LoadFromSettings();
        _app.CredentialsSynced += (_, _) => MainThread.BeginInvokeOnMainThread(LoadFromSettings);
    }

    private void LoadFromSettings()
    {
        UserId = _app.Settings.UserId;
        ApiKey = _app.Settings.ApiKey;
        OpenAiApiKey = _app.Settings.OpenAiApiKey;
        OpenAiModel = _app.Settings.OpenAiModel;
        FilterAi = _app.Settings.FilterAi;
        ForYouEnabled = _app.Settings.ForYouEnabled;
        ForYouCloudSync = _app.Settings.ForYouCloudSync;
        ForYouLearnArtists = _app.Settings.ForYouLearnArtists;
        ForYouLearnSeries = _app.Settings.ForYouLearnSeries;
        ForYouLearnMinorTags = _app.Settings.ForYouLearnMinorTags;
        RatingSafe = _app.Settings.RatingSafe;
        RatingQuestionable = _app.Settings.RatingQuestionable;
        RatingExplicit = _app.Settings.RatingExplicit;
    }

    [RelayCommand]
    private void Save()
    {
        _app.SetCredentials(UserId, ApiKey);
        _app.Settings.OpenAiApiKey = OpenAiApiKey;
        _app.Settings.OpenAiModel = OpenAiModel;
        _app.Settings.FilterAi = FilterAi;
        _app.Settings.ForYouEnabled = ForYouEnabled;
        _app.Settings.ForYouCloudSync = ForYouCloudSync;
        _app.Settings.ForYouLearnArtists = ForYouLearnArtists;
        _app.Settings.ForYouLearnSeries = ForYouLearnSeries;
        _app.Settings.ForYouLearnMinorTags = ForYouLearnMinorTags;
        _app.ForYou.IsEnabled = ForYouEnabled;
        _app.ForYou.CloudSyncEnabled = ForYouCloudSync;
        _app.Settings.RatingSafe = RatingSafe;
        _app.Settings.RatingQuestionable = RatingQuestionable;
        _app.Settings.RatingExplicit = RatingExplicit;
        _app.SaveSettings();
        _app.Messenger.Show("Saved", "Settings saved.", AppMessageKind.Info);
    }
}
