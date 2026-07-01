using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Rule34Gallery.Core;
using Rule34GalleryApp.Controls;
using Rule34GalleryApp.Services;

namespace Rule34GalleryApp.Views.Pages;

public partial class BrowsePage : Page
{
    private readonly AppServices _app = AppServices.Current;
    private bool _suppressOptionRefresh;

    public BrowsePage()
    {
        InitializeComponent();
        Gallery.ItemsSource = _app.Posts;
        Gallery.SetEmptyMessage(
            "No results yet",
            "Add tags under Search for, then click Search. Set API credentials on Settings if needed.");
        AutocompleteList.ItemsSource = _app.Autocomplete;
        BlacklistAutocompleteList.ItemsSource = _app.BlacklistAutocomplete;
        _app.Gallery.ViewModeChanged += (_, _) => Dispatcher.Invoke(UpdateBottomPager);
        _app.Gallery.PostsChanged += (_, _) => Dispatcher.Invoke(UpdateBottomPager);
        _app.Navigation.Changed += (_, _) => Dispatcher.Invoke(UpdateNavHistoryButtons);
        SearchExpander.Collapsed += (_, _) =>
        {
            UpdateSearchExpanderSummary();
            Gallery.RefreshViewport();
        };
        SearchExpander.Expanded += (_, _) =>
        {
            UpdateSearchExpanderSummary();
            Gallery.RefreshViewport();
        };
        Loaded += (_, _) =>
        {
            UpdateBottomPager();
            UpdateNavHistoryButtons();
            UpdateSearchExpanderSummary();
        };
        BottomPagerPanel.SizeChanged += (_, _) => UpdateGalleryViewport();
        FeedPanel.IndexChanged += (_, _) => { };
    }

    public void ApplyRemoteSessionState() => UpdateBrowseLayoutMode();

    public bool IsFeedModeActive => _app.Settings.BrowseLayoutMode == BrowseLayoutMode.Feed;

    public int FeedIndex => FeedPanel.CurrentIndex;

    public void FeedNavigateNext() => FeedPanel.NavigateNext();

    public void FeedNavigatePrevious() => FeedPanel.NavigatePrevious();

    public void SetBrowseLayoutMode(BrowseLayoutMode mode)
    {
        _app.Settings.BrowseLayoutMode = mode;
        _app.SaveSettings();
        UpdateBrowseLayoutMode();
    }

    public void UpdateBrowseLayoutMode()
    {
        var feed = IsFeedModeActive;
        Gallery.Visibility = feed ? Visibility.Collapsed : Visibility.Visible;
        FeedPanel.Visibility = feed ? Visibility.Visible : Visibility.Collapsed;
        RemoteFeedHint.Visibility = Visibility.Visible;

        if (feed)
        {
            BottomPagerPanel.Visibility = Visibility.Collapsed;
            try
            {
                FeedPanel.BindPosts();
            }
            catch
            {
                Gallery.Visibility = Visibility.Visible;
                FeedPanel.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            UpdateBottomPager();
        }
    }

    public void ApplySettings()
    {
        _app.Gallery.BeginLoadSettings();
        _suppressOptionRefresh = true;
        try
        {
            _app.Settings.MigrateLegacyTagsIfNeeded();
            _app.Settings.SyncPresetTagsToBlacklist();
            TagInput.Text = string.Empty;
            BlacklistInput.Text = string.Empty;
            FilterAiCheckBox.IsChecked = _app.Settings.FilterAi;
            RatingSafeCheck.IsChecked = _app.Settings.RatingSafe;
            RatingQuestionableCheck.IsChecked = _app.Settings.RatingQuestionable;
            RatingExplicitCheck.IsChecked = _app.Settings.RatingExplicit;
            MediaFilterCombo.SelectedIndex = (int)_app.Settings.MediaFilter;
            SortModeCombo.SelectedIndex = (int)_app.Settings.SortMode;
            if (_app.Settings.LimitIndex >= 0 && _app.Settings.LimitIndex < LimitCombo.Items.Count)
            {
                LimitCombo.SelectedIndex = _app.Settings.LimitIndex;
            }

            MinScoreInput.Text = _app.Settings.MinScore > 0 ? _app.Settings.MinScore.ToString() : string.Empty;
            MinWidthInput.Text = _app.Settings.MinWidth > 0 ? _app.Settings.MinWidth.ToString() : string.Empty;
            MinHeightInput.Text = _app.Settings.MinHeight > 0 ? _app.Settings.MinHeight.ToString() : string.Empty;
            ArtistFilterInput.Text = _app.Settings.ArtistFilter;
            CharacterFilterInput.Text = _app.Settings.CharacterFilter;
            CopyrightFilterInput.Text = _app.Settings.CopyrightFilter;

            RebuildTagPanels();
            UpdateSearchPresetUi();
            UpdateBlacklistPresetUi();
            UpdateQueryPreview();
            SearchFilterUi.UpdateFiltersButton(FiltersButton, _app.Settings);
            UpdateBrowseLayoutMode();
        }
        finally
        {
            _suppressOptionRefresh = false;
            _app.Gallery.EndLoadSettings();
        }

        UpdateBottomPager();
        UpdateSearchExpanderSummary();
    }

    public void BindStatus(string status)
    {
        StatusText.Text = status;
        if (IsVisible)
        {
            if (PageLoadingHelper.IsBusyStatus(status))
            {
                LoadingOverlay.Show(status);
            }
            else
            {
                LoadingOverlay.Hide();
            }
        }

        UpdateSearchExpanderSummary();
    }

    public void SyncTagsFromSettings()
    {
        RebuildTagPanels();
        UpdateSearchPresetUi();
        UpdateBlacklistPresetUi();
    }

    private void UpdateSearchPresetUi()
        => SearchPresetUi.UpdatePresetButton(SearchPresetsButton, _app.Settings);

    private void UpdateBlacklistPresetUi()
        => BlacklistPresetUi.UpdatePresetButton(PresetsButton, _app.Settings);

    private void SearchPresetsButton_OnClick(object sender, RoutedEventArgs e)
        => PresetPickerService.ShowSearchPresets(_app.Settings, OnSearchPresetsChanged);

    private void SaveTagsButton_OnClick(object sender, RoutedEventArgs e)
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
                _app.Messenger.Show("Save tags", error, AppMessageKind.Info);
            }

            return;
        }

        RebuildTagPanels();
        UpdateSearchPresetUi();
        _app.Gallery.ScheduleSaveSettings();
        _app.Messenger.Show(
            "Tags saved",
            "Open Search presets — your bundle is at the top under Saved tags.",
            AppMessageKind.Info);
    }

    private void DescribeSearchButton_OnClick(object sender, RoutedEventArgs e)
        => TagDiscoverService.Show(_app.Settings, OnTagDiscoveryChanged);

    private void OnTagDiscoveryChanged()
    {
        RebuildTagPanels();
        _app.Gallery.ScheduleSaveSettings();
        UpdateSearchPresetUi();
        UpdateQueryPreview();
        MaybeRefreshSearch();
    }

    private void OnSearchPresetsChanged()
    {
        RebuildTagPanels();
        _app.ForYou.RecordSavedTags(_app.Settings.GetSearchPresetIncludeTags(), "search-preset");
        _app.Gallery.ScheduleSaveSettings();
        UpdateSearchPresetUi();
        UpdateQueryPreview();
        MaybeRefreshSearch();
    }

    private void OnSearchBundleRemoved()
    {
        RebuildTagPanels();
        _app.Gallery.ScheduleSaveSettings();
        UpdateSearchPresetUi();
        UpdateQueryPreview();
        MaybeRefreshSearch();
    }

    private void RebuildSearchBundleChips()
        => SearchPresetTagUi.RebuildBundleChips(SearchPresetsPanel, _app.Settings, OnSearchBundleRemoved);

    private void MaybeRefreshSearch()
    {
        if (_app.Gallery.ViewMode == GalleryViewMode.Search &&
            (_app.Posts.Count > 0 || _app.Settings.HasSearchCriteria()))
        {
            _ = _app.Gallery.SearchAsync(resetPage: true, recordHistory: false);
        }
    }

    private void PresetsButton_OnClick(object sender, RoutedEventArgs e)
        => PresetPickerService.ShowBlacklistPresets(_app.Settings, OnBlacklistPresetsChanged);

    private void FiltersButton_OnClick(object sender, RoutedEventArgs e)
        => FiltersPopup.IsOpen = !FiltersPopup.IsOpen;

    private void OnBlacklistPresetsChanged()
    {
        RebuildExcludeChips();
        _app.Gallery.ScheduleSaveSettings();
        UpdateBlacklistPresetUi();
        UpdateQueryPreview();
        MaybeRefreshSearch();
    }

    private void OnBlacklistTagsChanged()
    {
        RebuildExcludeChips();
        _app.Gallery.ScheduleSaveSettings();
        UpdateQueryPreview();
        MaybeRefreshSearch();
    }

    private void RebuildExcludeChips()
        => BlacklistTagUi.RebuildChips(BlacklistTagsPanel, _app.Settings, OnBlacklistTagsChanged);

    private void RebuildTagPanels()
    {
        IncludeTagsPanel.Children.Clear();
        foreach (var tag in _app.Settings.IncludeTags)
        {
            IncludeTagsPanel.Children.Add(CreateIncludeChip(tag));
        }

        RebuildSearchBundleChips();
        RebuildExcludeChips();

        IncludeTagsPanel.Visibility = _app.Settings.IncludeTags.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private RemovableTagChip CreateIncludeChip(string tag)
    {
        var chip = new RemovableTagChip
        {
            TagText = tag,
            Category = TagCategoryColors.InferCategory(tag),
        };
        chip.RemoveClicked += (_, _) =>
        {
            _app.Settings.RemoveIncludeTag(tag);
            RebuildTagPanels();
            _app.Gallery.ScheduleSaveSettings();
            UpdateQueryPreview();
        };

        var saveItem = new MenuItem { Header = "Save to presets…" };
        saveItem.Click += (_, _) =>
        {
            var owner = Window.GetWindow(this);
            if (owner is null)
            {
                return;
            }

            if (!SavedTagPresetDialog.TryPromptSaveTags(owner, [tag], out var error))
            {
                if (!string.IsNullOrWhiteSpace(error))
                {
                    _app.Messenger.Show("Save tags", error, AppMessageKind.Info);
                }

                return;
            }

            UpdateSearchPresetUi();
            _app.Gallery.ScheduleSaveSettings();
            _app.Messenger.Show("Tag saved", "Find it at the top of Search presets under Saved tags.", AppMessageKind.Info);
        };
        chip.ContextMenu = new ContextMenu { Items = { saveItem } };

        return chip;
    }

    private void UpdateQueryPreview()
    {
        var preview = SearchQueryBuilder.BuildPreview(_app.Settings);
        QueryPreviewText.Text = preview;
        QueryPreviewText.ToolTip = preview;
        UpdateSearchExpanderSummary();
    }

    private void UpdateSearchExpanderSummary()
    {
        if (SearchExpander.IsExpanded)
        {
            SearchExpanderSummary.Text = "Click to collapse";
            return;
        }

        var parts = new List<string>();
        var preview = SearchQueryBuilder.BuildPreview(_app.Settings);
        if (preview.StartsWith("(no tags", StringComparison.OrdinalIgnoreCase))
        {
            parts.Add("No tags set");
        }
        else
        {
            parts.Add(preview.Length > 72 ? preview[..69] + "…" : preview);
        }

        if (_app.Gallery.ViewMode == GalleryViewMode.Search && _app.Posts.Count > 0)
        {
            parts.Add($"Page {_app.Gallery.CurrentPage + 1} · {_app.Posts.Count} results");
        }
        else if (!string.IsNullOrWhiteSpace(StatusText.Text) &&
                 !StatusText.Text.Equals("Ready", StringComparison.OrdinalIgnoreCase))
        {
            parts.Add(StatusText.Text);
        }

        SearchExpanderSummary.Text = string.Join(" · ", parts);
    }

    private void NavBackButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            _ = mainWindow.NavigateBackAsync();
        }
    }

    private void NavForwardButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            _ = mainWindow.NavigateForwardAsync();
        }
    }

    private void UpdateNavHistoryButtons()
    {
        NavBackButton.IsEnabled = _app.Navigation.CanGoBack;
        NavForwardButton.IsEnabled = _app.Navigation.CanGoForward;
    }

    private async Task RunSearchAsync(bool resetPage)
    {
        CommitPendingInputTags();
        SyncOptionsFromUi();
        await _app.Gallery.SearchAsync(resetPage: resetPage);
        UpdateQueryPreview();
    }

    private async void SearchButton_OnClick(object sender, RoutedEventArgs e)
        => await RunSearchAsync(resetPage: true);

    private async void PrevButton_OnClick(object sender, RoutedEventArgs e)
        => await _app.Gallery.PrevPageAsync();

    private async void NextButton_OnClick(object sender, RoutedEventArgs e)
        => await _app.Gallery.NextPageAsync();

    private void TagInput_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                AddTagsFromInput();
                e.Handled = true;
                return;
            }

            _ = RunSearchAsync(resetPage: true);
            e.Handled = true;
        }
        else if (e.Key == Key.OemComma || e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.None)
        {
            AddTagsFromInput();
            e.Handled = true;
        }
    }

    private void BlacklistInput_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddBlacklistFromInput();
            e.Handled = true;
        }
        else if (e.Key == Key.OemComma || e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.None)
        {
            AddBlacklistFromInput();
            e.Handled = true;
        }
    }

    private void AddTagButton_OnClick(object sender, RoutedEventArgs e) => AddTagsFromInput();

    private void ExcludeTagButton_OnClick(object sender, RoutedEventArgs e) => AddBlacklistFromInput();

    private void CommitPendingInputTags()
    {
        if (!string.IsNullOrWhiteSpace(TagInput.Text))
        {
            AddTagsFromInput();
        }
    }

    private void AddTagsFromInput()
    {
        var raw = TagInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        foreach (var token in raw.Split([' ', '\t', ','], StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.StartsWith('-'))
            {
                _app.Settings.AddBlacklistTag(token);
            }
            else
            {
                _app.Settings.AddIncludeTag(token);
            }
        }

        TagInput.Text = string.Empty;
        RebuildTagPanels();
        _app.Gallery.ScheduleSaveSettings();
        UpdateQueryPreview();
    }

    private void AddBlacklistFromInput()
    {
        var raw = BlacklistInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        foreach (var token in raw.Split([' ', '\t', ','], StringSplitOptions.RemoveEmptyEntries))
        {
            _app.Settings.AddBlacklistTag(token);
        }

        BlacklistInput.Text = string.Empty;
        BlacklistAutocompletePopup.IsOpen = false;
        RebuildTagPanels();
        _app.Gallery.ScheduleSaveSettings();
        UpdateQueryPreview();
    }

    private async void TagInput_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        BlacklistAutocompletePopup.IsOpen = false;
        var hasSuggestions = await TagAutocompleteService.TryPopulateSuggestionsAsync(
            _app.Http,
            _app.Autocomplete,
            TagInput.Text);
        AutocompletePopup.IsOpen = hasSuggestions;
    }

    private async void BlacklistInput_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        AutocompletePopup.IsOpen = false;
        var hasSuggestions = await TagAutocompleteService.TryPopulateSuggestionsAsync(
            _app.Http,
            _app.BlacklistAutocomplete,
            BlacklistInput.Text);
        BlacklistAutocompletePopup.IsOpen = hasSuggestions;
    }

    private void AutocompleteList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AutocompleteList.SelectedItem is not TagSuggestion selected)
        {
            return;
        }

        TagAutocompleteWpf.ApplySuggestionToInput(TagInput, selected);
        AutocompletePopup.IsOpen = false;
        AutocompleteList.SelectedItem = null;
    }

    private void BlacklistAutocompleteList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BlacklistAutocompleteList.SelectedItem is not TagSuggestion selected)
        {
            return;
        }

        TagAutocompleteWpf.ApplySuggestionToInput(BlacklistInput, selected);
        BlacklistAutocompletePopup.IsOpen = false;
        BlacklistAutocompleteList.SelectedItem = null;
    }

    private void SyncOptionsFromUi()
    {
        _app.Settings.FilterAi = FilterAiCheckBox.IsChecked == true;
        _app.Settings.RatingSafe = RatingSafeCheck.IsChecked == true;
        _app.Settings.RatingQuestionable = RatingQuestionableCheck.IsChecked == true;
        _app.Settings.RatingExplicit = RatingExplicitCheck.IsChecked == true;
        _app.Settings.MediaFilter = (MediaFilterMode)Math.Clamp(MediaFilterCombo.SelectedIndex, 0, 4);
        _app.Settings.SortMode = (SearchSortMode)Math.Clamp(SortModeCombo.SelectedIndex, 0, 4);
        _app.Settings.LimitIndex = LimitCombo.SelectedIndex >= 0 ? LimitCombo.SelectedIndex : _app.Settings.LimitIndex;
        _app.Settings.MinScore = ParseOptionalInt(MinScoreInput.Text);
        _app.Settings.MinWidth = ParseOptionalInt(MinWidthInput.Text);
        _app.Settings.MinHeight = ParseOptionalInt(MinHeightInput.Text);
        _app.Settings.ArtistFilter = ArtistFilterInput.Text.Trim();
        _app.Settings.CharacterFilter = CharacterFilterInput.Text.Trim();
        _app.Settings.CopyrightFilter = CopyrightFilterInput.Text.Trim();
        _app.Settings.SyncTagsString();
        SearchFilterUi.UpdateFiltersButton(FiltersButton, _app.Settings);
    }

    private static int ParseOptionalInt(string text)
        => int.TryParse(text.Trim(), out var value) ? Math.Max(0, value) : 0;

    private async void FilterAi_OnChanged(object sender, RoutedEventArgs e)
        => await OnSearchOptionChangedAsync();

    private async void SearchOption_OnChanged(object sender, RoutedEventArgs e)
        => await OnSearchOptionChangedAsync();

    private async void LimitCombo_OnChanged(object sender, SelectionChangedEventArgs e)
        => await OnSearchOptionChangedAsync();

    private async Task OnSearchOptionChangedAsync()
    {
        if (_app.Gallery.IsLoadingSettings || _suppressOptionRefresh)
        {
            return;
        }

        SyncOptionsFromUi();
        _app.Gallery.ScheduleSaveSettings();
        UpdateQueryPreview();

        if (_app.Gallery.ViewMode == GalleryViewMode.Search &&
            (_app.Posts.Count > 0 || _app.Settings.HasSearchCriteria()))
        {
            await _app.Gallery.SearchAsync(resetPage: true, recordHistory: false);
        }
    }

    private void NumericFilter_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SyncOptionsFromUi();
            _app.Gallery.ScheduleSaveSettings();
            UpdateQueryPreview();
            SearchFilterUi.UpdateFiltersButton(FiltersButton, _app.Settings);
            e.Handled = true;
        }
    }

    private void Gallery_OnCardClicked(object? sender, PostItem post)
    {
        if (Window.GetWindow(this) is MainWindow main)
        {
            var index = _app.Posts.IndexOf(post);
            if (index >= 0)
            {
                main.OpenViewer(index);
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
        Gallery.UpdateEmptyState();
    }

    private async void Gallery_OnWatchLaterClicked(object? sender, PostItem post)
    {
        if (!GalleryActions.EnsureSignedIn(Window.GetWindow(this)!))
        {
            return;
        }

        await GalleryActions.ToggleWatchLaterAsync(post);
        Gallery.UpdateEmptyState();
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

    public void OnPostsChanged()
    {
        Gallery.UpdateEmptyState();
        if (IsFeedModeActive)
        {
            FeedPanel.BindPosts();
        }

        UpdateBottomPager();
        UpdateSearchExpanderSummary();
    }

    private void FeedLayoutButton_OnClick(object sender, RoutedEventArgs e)
        => SetBrowseLayoutMode(BrowseLayoutMode.Feed);

    private void GridLayoutButton_OnClick(object sender, RoutedEventArgs e)
        => SetBrowseLayoutMode(BrowseLayoutMode.Grid);

    public void UpdateBottomPager()
    {
        if (IsFeedModeActive)
        {
            BottomPagerPanel.Visibility = Visibility.Collapsed;
            return;
        }

        var isSearch = _app.Gallery.ViewMode == GalleryViewMode.Search;
        var show = isSearch && _app.Posts.Count > 0;
        BottomPagerPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        if (show)
        {
            var pageNumber = _app.Gallery.CurrentPage + 1;
            BottomPageText.Text = $"Page {pageNumber}  ·  {_app.Posts.Count} results";
            BottomPrevButton.IsEnabled = _app.Gallery.CurrentPage > 0;
            BottomNextButton.IsEnabled = true;
        }

        UpdateGalleryViewport();
    }

    private void UpdateGalleryViewport()
    {
        if (BottomPagerPanel.Visibility == Visibility.Visible)
        {
            const double fallbackPagerHeight = 52;
            var pagerHeight = BottomPagerPanel.ActualHeight > 0
                ? BottomPagerPanel.ActualHeight + BottomPagerPanel.Margin.Top
                : fallbackPagerHeight;
            Gallery.SetReservedBottomHeight(pagerHeight);
        }
        else
        {
            Gallery.SetReservedBottomHeight(0);
        }
    }
}
