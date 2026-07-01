using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Rule34Gallery.Core;
using Rule34Gallery.Core.Services;
using Rule34GalleryApp.Services;

namespace Rule34GalleryApp.Views.Pages;

public partial class ForYouPage : Page
{
    private readonly AppServices _app = AppServices.Current;
    private bool _isLoading;
    private bool _suppressSortFilter;
    private List<ForYouFeedItem> _feedPool = [];

    public ObservableCollection<ForYouSearchLine> SearchLines { get; } = [];

    public ObservableCollection<ForYouTopicProfile> Topics { get; } = [];

    public ObservableCollection<ForYouActivityEntry> RecentSignals { get; } = [];

    public ObservableCollection<ForYouGalleryEntry> FeedEntries { get; } = [];

    public ObservableCollection<PostItem> FeedPosts { get; } = [];

    public ForYouPage()
    {
        InitializeComponent();
        FeedItemsControl.ItemsSource = FeedEntries;
        _app.ForYou.Changed += (_, _) => Dispatcher.InvokeAsync(RefreshLearningPanels);
        Loaded += async (_, _) =>
        {
            RefreshSortFilterControls();
            _app.ForYou.ApplyLearningCategorySettings();
            await LoadAsync();
        };
    }

    private void RefreshSortFilterControls()
    {
        _suppressSortFilter = true;
        try
        {
            var settings = _app.Settings;
            SortMostMatchesRadio.IsChecked = settings.ForYouFeedSort == ForYouFeedSortMode.MostTagMatches;
            SortHighestScoreRadio.IsChecked = settings.ForYouFeedSort == ForYouFeedSortMode.HighestScore;
            SortRandomRadio.IsChecked = settings.ForYouFeedSort == ForYouFeedSortMode.Random;

            FilterAllRadio.IsChecked = settings.ForYouFeedMediaFilter == MediaFilterMode.All;
            FilterImagesRadio.IsChecked = settings.ForYouFeedMediaFilter == MediaFilterMode.Images;
            FilterVideosRadio.IsChecked = settings.ForYouFeedMediaFilter == MediaFilterMode.Videos;
            FilterGifsRadio.IsChecked = settings.ForYouFeedMediaFilter == MediaFilterMode.Gifs;
            FilterAnimatedRadio.IsChecked = settings.ForYouFeedMediaFilter == MediaFilterMode.Animated;

            UpdateSortFilterButtonLabel();
        }
        finally
        {
            _suppressSortFilter = false;
        }
    }

    private void UpdateSortFilterButtonLabel()
    {
        SortFilterButton.Content =
            $"{ForYouFeedPresentation.DescribeSort(_app.Settings.ForYouFeedSort)} · {ForYouFeedPresentation.DescribeFilter(_app.Settings.ForYouFeedMediaFilter)}";
    }

    private void ApplyFeedPresentation()
    {
        var items = ForYouFeedPresentation.Apply(
            _feedPool,
            _app.Settings.ForYouFeedSort,
            _app.Settings.ForYouFeedMediaFilter);

        FeedPosts.Clear();
        FeedEntries.Clear();
        foreach (var item in items.Where(item => item.Post is not null))
        {
            FeedPosts.Add(item.Post!);
            FeedEntries.Add(new ForYouGalleryEntry
            {
                Post = item.Post!,
                Reason = string.IsNullOrWhiteSpace(item.Reason)
                    ? "Matched from your activity"
                    : item.Reason,
                MatchedTopicCount = item.MatchedTopicCount,
            });
        }

        _app.Library.ApplyCloudLibraryState(FeedPosts);
        UpdateFeedEmptyState();
    }

    private void UpdateFeedEmptyState()
    {
        FeedEmptyPanel.Visibility = FeedEntries.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void SortFilterButton_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshSortFilterControls();
        SortFilterPopup.IsOpen = !SortFilterPopup.IsOpen;
    }

    private void SortFilterOption_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressSortFilter)
        {
            return;
        }

        if (SortMostMatchesRadio.IsChecked == true)
        {
            _app.Settings.ForYouFeedSort = ForYouFeedSortMode.MostTagMatches;
        }
        else if (SortHighestScoreRadio.IsChecked == true)
        {
            _app.Settings.ForYouFeedSort = ForYouFeedSortMode.HighestScore;
        }
        else if (SortRandomRadio.IsChecked == true)
        {
            _app.Settings.ForYouFeedSort = ForYouFeedSortMode.Random;
        }

        if (FilterAllRadio.IsChecked == true)
        {
            _app.Settings.ForYouFeedMediaFilter = MediaFilterMode.All;
        }
        else if (FilterImagesRadio.IsChecked == true)
        {
            _app.Settings.ForYouFeedMediaFilter = MediaFilterMode.Images;
        }
        else if (FilterVideosRadio.IsChecked == true)
        {
            _app.Settings.ForYouFeedMediaFilter = MediaFilterMode.Videos;
        }
        else if (FilterGifsRadio.IsChecked == true)
        {
            _app.Settings.ForYouFeedMediaFilter = MediaFilterMode.Gifs;
        }
        else if (FilterAnimatedRadio.IsChecked == true)
        {
            _app.Settings.ForYouFeedMediaFilter = MediaFilterMode.Animated;
        }

        _app.SaveSettings();
        UpdateSortFilterButtonLabel();
        ApplyFeedPresentation();
    }

    public async Task LoadAsync()
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        RefreshButtons();
        LoadingOverlay.Show();

        try
        {
            var result = await _app.ForYou.BuildFeedAsync();

            SearchLines.Clear();
            foreach (var line in result.SearchLines)
            {
                SearchLines.Add(line);
            }

            Topics.Clear();
            foreach (var topic in result.Topics)
            {
                Topics.Add(topic);
            }

            RecentSignals.Clear();
            foreach (var entry in result.RecentActivity)
            {
                RecentSignals.Add(entry);
            }

            _feedPool = result.Pool.Count > 0
                ? result.Pool.ToList()
                : result.Items.ToList();
            ApplyFeedPresentation();

            SummaryText.Text = string.IsNullOrWhiteSpace(result.Summary)
                ? "Your feed gets smarter as you browse."
                : result.Summary;
            SourceNoteText.Text = result.SourceNote;
            SourceNoteText.Visibility = string.IsNullOrWhiteSpace(result.SourceNote)
                ? Visibility.Collapsed
                : Visibility.Visible;

            StatusText.Text = result.StatusMessage;
            RefreshProfileToggles();
            RefreshManageButtons();
            UpdateControlsExpanderSummary();
            BindInterestTimeline();
            UpdateSortFilterButtonLabel();
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
        finally
        {
            _isLoading = false;
            LoadingOverlay.Hide();
            RefreshButtons();
        }
    }

    private void RefreshManageButtons()
    {
        ManageTopicsButton.Content = Topics.Count > 0 ? $"Topics ({Topics.Count})" : "Topics";
        ManageSearchLinesButton.Content = SearchLines.Count > 0
            ? $"Suggested searches ({SearchLines.Count})"
            : "Suggested searches";
        ManageSignalsButton.Content = RecentSignals.Count > 0
            ? $"Recent signals ({RecentSignals.Count})"
            : "Recent signals";

        var count = RecentSignals.Count;
        SignalsCountText.Text = count > 0
            ? $"{count} recent signal(s) saved."
            : string.Empty;
        UpdateControlsExpanderSummary();
    }

    private void UpdateControlsExpanderSummary()
    {
        var parts = new List<string>();
        var firstLine = SummaryText.Text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(firstLine))
        {
            parts.Add(firstLine.Length > 72 ? firstLine[..69] + "…" : firstLine);
        }

        if (Topics.Count > 0)
        {
            parts.Add($"{Topics.Count} topics");
        }

        if (SearchLines.Count > 0)
        {
            parts.Add($"{SearchLines.Count} searches");
        }

        if (RecentSignals.Count > 0)
        {
            parts.Add($"{RecentSignals.Count} signals");
        }

        ControlsExpanderSummary.Text = parts.Count > 0
            ? string.Join(" · ", parts)
            : "Click to expand";
    }

    private void BindInterestTimeline()
    {
        var timeline = _app.ForYou.BuildInterestTimeline();
        InterestTimelineChart.Bind(timeline);

        if (!timeline.HasData)
        {
            InterestTimelineSummary.Text = "Needs more activity";
            return;
        }

        var rising = timeline.Series.FirstOrDefault(s => s.IsRising)?.Label;
        var declining = timeline.Series.FirstOrDefault(s => s.IsDeclining)?.Label;
        if (!string.IsNullOrWhiteSpace(rising) && !string.IsNullOrWhiteSpace(declining))
        {
            InterestTimelineSummary.Text = $"{rising} ⬆ · {declining} ⬇";
        }
        else if (!string.IsNullOrWhiteSpace(rising))
        {
            InterestTimelineSummary.Text = $"{rising} rising";
        }
        else if (!string.IsNullOrWhiteSpace(declining))
        {
            InterestTimelineSummary.Text = $"{declining} fading";
        }
        else
        {
            InterestTimelineSummary.Text = $"{timeline.Series.Count} topics tracked";
        }
    }

    private void RefreshProfileToggles()
    {
        _suppressProfileToggle = true;
        try
        {
            var settings = _app.Settings;
            LearningCheckBox.IsChecked = _app.ForYou.IsEnabled;
            LearnArtistsCheckBox.IsChecked = settings.ForYouLearnArtists;
            LearnSeriesCheckBox.IsChecked = settings.ForYouLearnSeries;
            LearnMinorTagsCheckBox.IsChecked = settings.ForYouLearnMinorTags;
            LearnArtistsMenuCheckBox.IsChecked = settings.ForYouLearnArtists;
            LearnSeriesMenuCheckBox.IsChecked = settings.ForYouLearnSeries;
            LearnMinorTagsMenuCheckBox.IsChecked = settings.ForYouLearnMinorTags;
            OpenAiCheckBox.IsChecked = settings.UseOpenAiForForYou;
            OpenAiCheckBox.IsEnabled = _app.ForYou.IsEnabled;

            var categoriesEnabled = _app.ForYou.IsEnabled;
            LearnCategoriesPanel.IsEnabled = categoriesEnabled;
            LearnArtistsMenuCheckBox.IsEnabled = categoriesEnabled;
            LearnSeriesMenuCheckBox.IsEnabled = categoriesEnabled;
            LearnMinorTagsMenuCheckBox.IsEnabled = categoriesEnabled;
        }
        finally
        {
            _suppressProfileToggle = false;
        }
    }

    private bool _suppressProfileToggle;

    public void RefreshUi()
    {
        StatusText.Text = _app.ForYou.Profile.Enabled
            ? (_app.ForYou.GetRankedTopics().Count > 0
                ? $"Learning from {_app.ForYou.GetRankedTopics().Count} topic(s)."
                : "Learning, but there are no strong topics yet.")
            : "Turn on For You in Settings to start learning.";

        ToggleLearningButton.Content = _app.ForYou.IsEnabled ? "Pause learning" : "Enable learning";
        RefreshProfileToggles();
        RefreshManageButtons();
    }

    private void RefreshLearningPanels()
    {
        if (!IsVisible)
        {
            return;
        }

        Topics.Clear();
        foreach (var topic in _app.ForYou.GetRankedTopics())
        {
            Topics.Add(topic);
        }

        RecentSignals.Clear();
        foreach (var entry in _app.ForYou.GetRecentActivity(40))
        {
            RecentSignals.Add(entry);
        }

        BindInterestTimeline();
        RefreshUi();
        UpdateControlsExpanderSummary();
    }

    private void LearningMenuButton_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshProfileToggles();
        LearningMenuPopup.IsOpen = !LearningMenuPopup.IsOpen;
    }

    private void LearnCategory_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressProfileToggle || sender is not CheckBox changed)
        {
            return;
        }

        if (ReferenceEquals(changed, LearnArtistsCheckBox) || ReferenceEquals(changed, LearnArtistsMenuCheckBox))
        {
            _app.Settings.ForYouLearnArtists = changed.IsChecked == true;
            LearnArtistsCheckBox.IsChecked = changed.IsChecked;
            LearnArtistsMenuCheckBox.IsChecked = changed.IsChecked;
        }
        else if (ReferenceEquals(changed, LearnSeriesCheckBox) || ReferenceEquals(changed, LearnSeriesMenuCheckBox))
        {
            _app.Settings.ForYouLearnSeries = changed.IsChecked == true;
            LearnSeriesCheckBox.IsChecked = changed.IsChecked;
            LearnSeriesMenuCheckBox.IsChecked = changed.IsChecked;
        }
        else if (ReferenceEquals(changed, LearnMinorTagsCheckBox) || ReferenceEquals(changed, LearnMinorTagsMenuCheckBox))
        {
            _app.Settings.ForYouLearnMinorTags = changed.IsChecked == true;
            LearnMinorTagsCheckBox.IsChecked = changed.IsChecked;
            LearnMinorTagsMenuCheckBox.IsChecked = changed.IsChecked;
        }

        _app.SaveSettings();
        _app.ForYou.ApplyLearningCategorySettings();
        RefreshUi();
        _ = LoadAsync();
    }

    private void RefreshButtons()
    {
        RefreshUi();
        RefreshButton.IsEnabled = !_isLoading;
        ToggleLearningButton.IsEnabled = !_isLoading;
    }

    private async void RefreshButton_OnClick(object sender, RoutedEventArgs e)
        => await LoadAsync();

    private void RefreshMenuButton_OnClick(object sender, RoutedEventArgs e)
        => RefreshMenuPopup.IsOpen = !RefreshMenuPopup.IsOpen;

    private async void ForceUpdateButton_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshMenuPopup.IsOpen = false;
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        RefreshButtons();
        LoadingOverlay.Show("Rebuilding profile from signals…");
        try
        {
            StatusText.Text = "Rebuilding profile from signals…";
            await _app.ForYou.ForceRebuildProfileAsync();
            await LoadAsync();
            StatusText.Text = "Profile rebuilt from recent signals and search history.";
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
        finally
        {
            _isLoading = false;
            RefreshButtons();
        }
    }

    private void DataViewerButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is not MainWindow main)
        {
            return;
        }

        main.ShowForYouDataViewer(_app.ForYou.BuildDataViewerText());
    }

    private async void ToggleLearningButton_OnClick(object sender, RoutedEventArgs e)
    {
        _app.ForYou.IsEnabled = !_app.ForYou.IsEnabled;
        _app.Settings.ForYouEnabled = _app.ForYou.IsEnabled;
        if (!_app.ForYou.IsEnabled)
        {
            _app.Settings.UseOpenAiForForYou = false;
        }

        _app.SaveSettings();
        await LoadAsync();
    }

    private void LearningCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressProfileToggle)
        {
            return;
        }

        var enabled = LearningCheckBox.IsChecked == true;
        _app.ForYou.IsEnabled = enabled;
        _app.Settings.ForYouEnabled = enabled;
        if (!enabled)
        {
            _app.Settings.UseOpenAiForForYou = false;
        }

        _app.SaveSettings();
        RefreshUi();
    }

    private void OpenAiCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressProfileToggle)
        {
            return;
        }

        _app.Settings.UseOpenAiForForYou = OpenAiCheckBox.IsChecked == true;
        _app.SaveSettings();
    }

    private async void ClearButton_OnClick(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "This clears your taste profile, topics, searches, and all saved signals but keeps learning enabled.",
            "Clear For You data?",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.OK)
        {
            return;
        }

        await _app.ForYou.ClearDataAsync();
        await LoadAsync();
    }

    private async void ResetProfileButton_OnClick(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "This clears your taste profile: topics, suggested searches, and saved signals. Learning stays on.",
            "Reset profile?",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.OK)
        {
            return;
        }

        await _app.ForYou.ResetProfileAsync();
        await LoadAsync();
    }

    private async void ResetAllButton_OnClick(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "This clears all learned data on this device and in the cloud, then turns off learning.",
            "Reset For You?",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.OK)
        {
            return;
        }

        await _app.ForYou.ResetAsync();
        _app.Settings.ForYouEnabled = false;
        _app.Platform.SettingsStore.Save(_app.Settings);
        await LoadAsync();
    }

    private void ManageTopicsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is not MainWindow main)
        {
            return;
        }

        main.ShowForYouTopicsManage(
            () => _app.ForYou.GetTopicsForManage(),
            async (tag, strength) =>
            {
                _app.ForYou.AddManualTopic(tag, strength);
                await LoadAsync();
            },
            async (topic, strength) =>
            {
                _app.ForYou.SetTopicStrength(topic.Topic, strength);
                await LoadAsync();
            },
            topic => _app.ForYou.SetTopicPinned(topic.Topic, true),
            topic => _app.ForYou.SetTopicBlocked(topic.Topic, true),
            topic => _app.ForYou.RemoveTopic(topic.Topic),
            topic => _app.ForYou.PromoteTopicToManual(topic.Topic),
            LoadAsync);
    }

    private void ManageSearchLinesButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is not MainWindow main)
        {
            return;
        }

        main.ShowForYouSearchLinesManage(
            () => SearchLines.ToList(),
            RunSearchLineAsync,
            line => _app.ForYou.SetSearchLinePinned(line.Query, true),
            line => _app.ForYou.SetSearchLineHidden(line.Query, true),
            LoadAsync);
    }

    private void ManageSignalsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is not MainWindow main)
        {
            return;
        }

        main.ShowForYouSignalsManage(
            () => _app.ForYou.GetRecentActivity(40),
            async entry =>
            {
                _app.ForYou.RemoveActivity(entry.Id);
                await Task.CompletedTask;
            },
            async entry =>
            {
                _app.ForYou.BoostSignalToTopic(entry);
                await Task.CompletedTask;
            },
            LoadAsync);
    }

    private async Task RunSearchLineAsync(ForYouSearchLine line)
    {
        if (line.Tags.Count == 0)
        {
            return;
        }

        _app.Settings.SetIncludeTags(line.Tags);
        _app.ForYou.RecordSearch(line.Tags, "for-you-search-line");
        if (Window.GetWindow(this) is MainWindow main)
        {
            main.NavigateToBrowse();
        }

        await _app.Gallery.SearchAsync(resetPage: true);
    }

    private void FeedCard_OnCardClicked(object? sender, PostItem post) => Gallery_OnCardClicked(sender, post);

    private void FeedCard_OnFavoriteClicked(object? sender, PostItem post) => Gallery_OnFavoriteClicked(sender, post);

    private void FeedCard_OnWatchLaterClicked(object? sender, PostItem post) => Gallery_OnWatchLaterClicked(sender, post);

    private void FeedCard_OnAddToListClicked(object? sender, PostItem post) => Gallery_OnAddToListClicked(sender, post);

    private void FeedCard_OnDownloadClicked(object? sender, PostItem post) => Gallery_OnDownloadClicked(sender, post);

    private void Gallery_OnCardClicked(object? sender, PostItem post)
    {
        if (Window.GetWindow(this) is MainWindow main)
        {
            var index = FeedPosts.IndexOf(post);
            if (index >= 0)
            {
                main.OpenViewer(index, FeedPosts, ForYouLearningSources.ForYouFeed);
            }
        }
    }

    private async void Gallery_OnFavoriteClicked(object? sender, PostItem post)
    {
        if (!GalleryActions.EnsureSignedIn(Window.GetWindow(this)!))
        {
            return;
        }

        await GalleryActions.ToggleFavoriteAsync(post);
        UpdateFeedEmptyState();
    }

    private async void Gallery_OnWatchLaterClicked(object? sender, PostItem post)
    {
        if (!GalleryActions.EnsureSignedIn(Window.GetWindow(this)!))
        {
            return;
        }

        await GalleryActions.ToggleWatchLaterAsync(post);
        UpdateFeedEmptyState();
    }

    private async void Gallery_OnAddToListClicked(object? sender, PostItem post)
        => await GalleryActions.PromptAddToListAsync(Window.GetWindow(this)!, post);

    private void Gallery_OnDownloadClicked(object? sender, PostItem post)
    {
        if (Window.GetWindow(this) is MainWindow main)
        {
            main.EnqueueDownload(post);
        }
    }
}
