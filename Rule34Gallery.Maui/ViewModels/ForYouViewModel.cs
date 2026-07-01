using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rule34Gallery.Core.Services;

namespace Rule34Gallery.Maui.ViewModels;

public partial class ForYouViewModel : ObservableObject
{
    private readonly AppServices _app = AppServices.Current;

    [ObservableProperty]
    private string _statusText = "Turn on For You in Settings to start learning.";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _learnArtists = true;

    [ObservableProperty]
    private bool _learnSeries = true;

    [ObservableProperty]
    private bool _learnMinorTags;

    private bool _loadingLearnSettings;

    private bool _loadingFeedPresentation;

    private List<ForYouFeedItem> _feedPool = [];

    public IReadOnlyList<string> FeedSortOptions { get; } =
    [
        "Most topic matches",
        "Highest training score",
        "Random",
    ];

    public IReadOnlyList<string> FeedFilterOptions { get; } =
    [
        "All",
        "Images",
        "Videos",
        "GIFs",
        "Animated",
    ];

    [ObservableProperty]
    private int _feedSortIndex;

    [ObservableProperty]
    private int _feedFilterIndex;

    public ObservableCollection<ForYouFeedItem> FeedItems { get; } = [];

    public ObservableCollection<ForYouTopicProfile> Topics { get; } = [];

    public ObservableCollection<SearchPresetRecommendation> SearchIdeas { get; } = [];

    public bool IsLearningEnabled => _app.ForYou.IsEnabled;

    public bool IsCloudSyncEnabled => _app.ForYou.CloudSyncEnabled;

    public bool LearningCategoriesEnabled => _app.ForYou.IsEnabled;

    public ForYouViewModel()
    {
        _app.ForYou.Changed += OnForYouChanged;
        _loadingFeedPresentation = true;
        FeedSortIndex = MapSortToIndex(_app.Settings.ForYouFeedSort);
        FeedFilterIndex = MapFilterToIndex(_app.Settings.ForYouFeedMediaFilter);
        _loadingFeedPresentation = false;
        RefreshFromProfile();
    }

    partial void OnFeedSortIndexChanged(int value)
    {
        if (_loadingFeedPresentation)
        {
            return;
        }

        _app.Settings.ForYouFeedSort = MapIndexToSort(value);
        _app.SaveSettings();
        ApplyFeedPresentation();
    }

    partial void OnFeedFilterIndexChanged(int value)
    {
        if (_loadingFeedPresentation)
        {
            return;
        }

        _app.Settings.ForYouFeedMediaFilter = MapIndexToFilter(value);
        _app.SaveSettings();
        ApplyFeedPresentation();
    }

    private void ApplyFeedPresentation()
    {
        var items = ForYouFeedPresentation.Apply(
            _feedPool,
            _app.Settings.ForYouFeedSort,
            _app.Settings.ForYouFeedMediaFilter);

        FeedItems.Clear();
        foreach (var item in items)
        {
            FeedItems.Add(item);
        }

        _app.Posts.Clear();
        foreach (var item in items.Where(i => i.Post is not null))
        {
            _app.Posts.Add(item.Post!);
        }

        _app.Library.ApplyCloudLibraryState(_app.Posts);
    }

    private static int MapSortToIndex(ForYouFeedSortMode sort) => sort switch
    {
        ForYouFeedSortMode.HighestScore => 1,
        ForYouFeedSortMode.Random => 2,
        _ => 0,
    };

    private static ForYouFeedSortMode MapIndexToSort(int index) => index switch
    {
        1 => ForYouFeedSortMode.HighestScore,
        2 => ForYouFeedSortMode.Random,
        _ => ForYouFeedSortMode.MostTagMatches,
    };

    private static int MapFilterToIndex(MediaFilterMode filter) => filter switch
    {
        MediaFilterMode.Images => 1,
        MediaFilterMode.Videos => 2,
        MediaFilterMode.Gifs => 3,
        MediaFilterMode.Animated => 4,
        _ => 0,
    };

    private static MediaFilterMode MapIndexToFilter(int index) => index switch
    {
        1 => MediaFilterMode.Images,
        2 => MediaFilterMode.Videos,
        3 => MediaFilterMode.Gifs,
        4 => MediaFilterMode.Animated,
        _ => MediaFilterMode.All,
    };

    private void OnForYouChanged(object? sender, EventArgs e)
        => MainThread.BeginInvokeOnMainThread(() =>
        {
            OnPropertyChanged(nameof(IsLearningEnabled));
            OnPropertyChanged(nameof(IsCloudSyncEnabled));
            OnPropertyChanged(nameof(LearningCategoriesEnabled));
            RefreshFromProfile();
        });

    private void RefreshFromProfile()
    {
        Topics.Clear();
        foreach (var topic in _app.ForYou.GetRankedTopics())
        {
            Topics.Add(topic);
        }

        SearchIdeas.Clear();
        foreach (var idea in _app.ForYou.BuildSearchIdeas())
        {
            SearchIdeas.Add(idea);
        }

        StatusText = _app.ForYou.Profile.Enabled
            ? (Topics.Count > 0 ? $"Learning from {Topics.Count} topic(s)." : "Learning, but there are no strong topics yet.")
            : "Turn on For You in Settings to start learning.";

        _loadingLearnSettings = true;
        LearnArtists = _app.Settings.ForYouLearnArtists;
        LearnSeries = _app.Settings.ForYouLearnSeries;
        LearnMinorTags = _app.Settings.ForYouLearnMinorTags;
        _loadingLearnSettings = false;
    }

    partial void OnLearnArtistsChanged(bool value)
    {
        if (_loadingLearnSettings)
        {
            return;
        }

        _app.Settings.ForYouLearnArtists = value;
        _app.SaveSettings();
        _app.ForYou.ApplyLearningCategorySettings();
    }

    partial void OnLearnSeriesChanged(bool value)
    {
        if (_loadingLearnSettings)
        {
            return;
        }

        _app.Settings.ForYouLearnSeries = value;
        _app.SaveSettings();
        _app.ForYou.ApplyLearningCategorySettings();
    }

    partial void OnLearnMinorTagsChanged(bool value)
    {
        if (_loadingLearnSettings)
        {
            return;
        }

        _app.Settings.ForYouLearnMinorTags = value;
        _app.SaveSettings();
        _app.ForYou.ApplyLearningCategorySettings();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _app.ForYou.BuildFeedAsync();

            _feedPool = result.Pool.Count > 0
                ? result.Pool.ToList()
                : result.Items.ToList();
            ApplyFeedPresentation();

            Topics.Clear();
            foreach (var topic in result.Topics)
            {
                Topics.Add(topic);
            }

            SearchIdeas.Clear();
            foreach (var idea in result.SearchIdeas)
            {
                SearchIdeas.Add(idea);
            }

            StatusText = result.StatusMessage;
            OnPropertyChanged(nameof(IsLearningEnabled));
            OnPropertyChanged(nameof(IsCloudSyncEnabled));
            OnPropertyChanged(nameof(LearningCategoriesEnabled));
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task OpenPostAsync(PostItem post)
    {
        var index = _app.Posts.IndexOf(post);
        if (index < 0)
        {
            return;
        }

        _app.ViewerLearningSource = ForYouLearningSources.ForYouFeed;
        await Shell.Current.GoToAsync($"{nameof(Views.ViewerPage)}?index={index}&local=false");
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(PostItem post)
    {
        if (!_app.Library.IsSignedIn)
        {
            await Shell.Current.DisplayAlert("Sign in required", "Open Account to sign in.", "OK");
            return;
        }

        await _app.Library.ToggleFavoriteAsync(post);
    }

    [RelayCommand]
    private void DownloadPost(PostItem post)
    {
        _app.Downloads.Enqueue(post);
        _app.Messenger.Show("Download queued", post.FileUrl ?? post.SampleUrl ?? "post", AppMessageKind.Info);
    }

    [RelayCommand]
    private async Task OpenSearchIdeaAsync(SearchPresetRecommendation idea)
    {
        if (idea.Tags.Count == 0)
        {
            return;
        }

        _app.Settings.SetIncludeTags(idea.Tags);
        _app.ForYou.RecordSearch(idea.Tags, "for-you-idea");
        await _app.Gallery.SearchAsync(resetPage: true);
        await Shell.Current.GoToAsync("//Browse");
    }

    [RelayCommand]
    private void LikeRecommendation(ForYouFeedItem item)
    {
        if (item.Post is null)
        {
            return;
        }

        _app.ForYou.RecordRecommendationFeedback(item, positive: true);
    }

    [RelayCommand]
    private void HideRecommendation(ForYouFeedItem item)
    {
        if (item.Post is null)
        {
            return;
        }

        _app.ForYou.RecordRecommendationFeedback(item, positive: false);
        if (!string.IsNullOrWhiteSpace(item.Topic))
        {
            _app.ForYou.SetTopicBlocked(item.Topic, true);
        }
    }

    [RelayCommand]
    private void PinTopic(ForYouTopicProfile topic)
    {
        _app.ForYou.SetTopicPinned(topic.Topic, !topic.IsPinned);
    }

    [RelayCommand]
    private void HideTopic(ForYouTopicProfile topic)
    {
        _app.ForYou.SetTopicBlocked(topic.Topic, !topic.IsBlocked);
    }

    [RelayCommand]
    private async Task AddManualTopicAsync()
    {
        var tag = await Shell.Current.DisplayPromptAsync("Add topic", "Tag name:", maxLength: 120);
        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        var strengthText = await Shell.Current.DisplayPromptAsync(
            "Add topic",
            "Score 0–100:",
            initialValue: "80",
            keyboard: Keyboard.Numeric);
        if (!double.TryParse(strengthText, out var strength))
        {
            strength = 80;
        }

        if (_app.ForYou.AddManualTopic(tag, strength))
        {
            RefreshFromProfile();
            await LoadAsync();
        }
    }

    [RelayCommand]
    private async Task SetTopicStrengthAsync(ForYouTopicProfile topic)
    {
        var strengthText = await Shell.Current.DisplayPromptAsync(
            "Set score",
            $"{topic.Topic}\nScore 0–100:",
            initialValue: Math.Clamp(topic.Score, 0, 100).ToString("F1"),
            keyboard: Keyboard.Numeric);
        if (!double.TryParse(strengthText, out var strength))
        {
            return;
        }

        if (_app.ForYou.SetTopicStrength(topic.Topic, strength))
        {
            RefreshFromProfile();
            await LoadAsync();
        }
    }

    [RelayCommand]
    private void ToggleLearning()
    {
        _app.ForYou.IsEnabled = !_app.ForYou.IsEnabled;
        _app.Settings.ForYouEnabled = _app.ForYou.IsEnabled;
        _app.SaveSettings();
        OnPropertyChanged(nameof(IsLearningEnabled));
        OnPropertyChanged(nameof(LearningCategoriesEnabled));
        StatusText = _app.ForYou.IsEnabled
            ? "For You is learning from your activity."
            : "For You is paused.";
    }

    [RelayCommand]
    private async Task ResetProfileAsync()
    {
        var confirm = await Shell.Current.DisplayAlert(
            "Reset For You?",
            "This clears your learned topics and recent activity on this device.",
            "Reset",
            "Cancel");
        if (!confirm)
        {
            return;
        }

        await _app.ForYou.ResetAsync();
        _app.Settings.ForYouEnabled = false;
        _app.Platform.SettingsStore.Save(_app.Settings);
        RefreshFromProfile();
    }
}
