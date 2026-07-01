using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Rule34Gallery.Core;
using Rule34GalleryApp.Controls;
using Rule34GalleryApp.Services;

namespace Rule34GalleryApp.Views.Overlays;

public partial class TagDiscoverOverlay : UserControl
{
    private enum DiscoverMode
    {
        Theme,
        Questionnaire,
        ConceptLookup,
        IntentSearch,
    }

    private readonly AppServices _app = AppServices.Current;
    private UserSettings? _settings;
    private Action? _onChanged;
    private CancellationTokenSource? _discoverCts;
    private DiscoverMode _mode = DiscoverMode.Theme;
    private readonly List<TagQuestionnaireAnswer> _answers = [];
    private TagQuestionnaireQuestion? _currentQuestion;
    private string _questionnaireTheme = string.Empty;

    public TagDiscoverOverlay()
    {
        InitializeComponent();
    }

    public void Show(UserSettings settings, Action onChanged, string? initialDescription = null)
    {
        _settings = settings;
        _onChanged = onChanged;
        DescriptionInput.Text = initialDescription ?? string.Empty;
        StatusText.Text = string.Empty;
        ThemeModeRadio.IsChecked = true;
        _mode = DiscoverMode.Theme;
        ResetQuestionnaireState();
        ApplyModeUi();
        ClearResults();
        Visibility = Visibility.Visible;
        DescriptionInput.Focus();
        DescriptionInput.CaretIndex = DescriptionInput.Text.Length;
    }

    public void Hide()
    {
        _discoverCts?.Cancel();
        _discoverCts = null;
        ResetQuestionnaireState();
        Visibility = Visibility.Collapsed;
        _settings = null;
        _onChanged = null;
    }

    private void DiscoverModeRadio_OnChecked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        _mode = QuestionnaireModeRadio.IsChecked == true
            ? DiscoverMode.Questionnaire
            : ConceptLookupModeRadio.IsChecked == true
                ? DiscoverMode.ConceptLookup
                : IntentSearchModeRadio.IsChecked == true
                    ? DiscoverMode.IntentSearch
                    : DiscoverMode.Theme;
        ResetQuestionnaireState();
        ApplyModeUi();
        ClearResults();
        StatusText.Text = string.Empty;
    }

    private void ApplyModeUi()
    {
        var hasOpenAi = _settings?.HasOpenAiForDescribeSearch == true;
        ApiHelpExpander.Visibility = Visibility.Visible;
        ThemeInputLabel.Text = _mode switch
        {
            DiscoverMode.Theme => "Description",
            DiscoverMode.ConceptLookup => "What do you mean?",
            DiscoverMode.IntentSearch => "What are you trying to remember?",
            _ => "Base theme",
        };
        DescriptionInput.MinHeight = 56;
        DescriptionInput.MaxHeight = 96;
        ResultsPanel.Visibility = Visibility.Visible;

        if (_mode == DiscoverMode.Theme)
        {
            DescriptionHintText.Text = hasOpenAi
                ? "Plain-language description → OpenAI suggests search lines and tags. Click Add line to apply."
                : "Plain-language description → tag suggestions. Add an OpenAI key in Account for smarter lines.";
            ThemeActionsPanel.Visibility = Visibility.Visible;
            QuestionnaireActionsPanel.Visibility = Visibility.Collapsed;
            QuestionPanel.Visibility = Visibility.Collapsed;
            EmptyText.Text = "Enter a description and click Suggest tags.";
            DescriptionInput.ToolTip = "Example: blonde elf girl in a forest at night";
            SuggestButton.Content = "Suggest tags";
            return;
        }

        if (_mode == DiscoverMode.ConceptLookup)
        {
            DescriptionHintText.Text = hasOpenAi
                ? "Describe in your own words — OpenAI figures out what you mean, then we find the real Rule34 tag name."
                : "Find tag name needs an OpenAI API key on the Account tab.";
            ThemeActionsPanel.Visibility = Visibility.Visible;
            QuestionnaireActionsPanel.Visibility = Visibility.Collapsed;
            QuestionPanel.Visibility = Visibility.Collapsed;
            EmptyText.Text = "Example: those japanese gyaru girls or whatever it's called";
            DescriptionInput.ToolTip = "Example: those japanese gals / gyaru style / big tiddy goth";
            SuggestButton.Content = "Find tag";
            return;
        }

        if (_mode == DiscoverMode.IntentSearch)
        {
            DescriptionHintText.Text = hasOpenAi
                ? "Describe who or what you forgot — we search the web and only show fact-checked matches (no booru tags)."
                : "Forgot the name? needs an OpenAI API key on the Account tab.";
            ThemeActionsPanel.Visibility = Visibility.Visible;
            QuestionnaireActionsPanel.Visibility = Visibility.Collapsed;
            QuestionPanel.Visibility = Visibility.Collapsed;
            EmptyText.Text = "Example: female viltrumite in white clothing";
            DescriptionInput.ToolTip = "Example: female viltrumite in white clothing";
            SuggestButton.Content = "Find what I mean";
            return;
        }

        DescriptionHintText.Text = hasOpenAi
            ? "Answer a few questions to narrow down real Rule34 tag names."
            : "Questionnaire requires an OpenAI API key on the Account tab.";
        ThemeActionsPanel.Visibility = Visibility.Collapsed;
        QuestionnaireActionsPanel.Visibility = Visibility.Visible;
        StartQuestionnaireButton.Visibility = _currentQuestion is null ? Visibility.Visible : Visibility.Collapsed;
        QuestionPanel.Visibility = _currentQuestion is null ? Visibility.Collapsed : Visibility.Visible;
        EmptyText.Text = "Enter a theme and click Start questionnaire.";
        DescriptionInput.ToolTip = "Example: jiggling hips, dark elf in armor";
    }

    private void SetQuestionnaireActiveUi(bool active)
    {
        if (_mode != DiscoverMode.Questionnaire)
        {
            return;
        }

        QuestionPanel.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
        QuestionnaireActionsPanel.Visibility = active ? Visibility.Collapsed : Visibility.Visible;
        StartQuestionnaireButton.Visibility = active ? Visibility.Collapsed : Visibility.Visible;
        ApiHelpExpander.Visibility = active ? Visibility.Collapsed : Visibility.Visible;
        ResultsPanel.Visibility = active ? Visibility.Collapsed : Visibility.Visible;
        ThemeInputLabel.Text = "Base theme";
        DescriptionInput.MinHeight = active ? 40 : 56;
        DescriptionInput.MaxHeight = active ? 52 : 96;
    }

    private void ResetQuestionnaireState()
    {
        _answers.Clear();
        _currentQuestion = null;
        _questionnaireTheme = string.Empty;
        QuestionOptionsPanel.Children.Clear();
        QuestionFreeTextInput.Text = string.Empty;
        QuestionProgressText.Text = string.Empty;
        QuestionPromptText.Text = string.Empty;
        SetQuestionnaireActiveUi(false);
        if (_mode == DiscoverMode.Questionnaire)
        {
            StartQuestionnaireButton.Visibility = Visibility.Visible;
            QuestionnaireActionsPanel.Visibility = Visibility.Visible;
        }
    }

    private async void SuggestButton_OnClick(object sender, RoutedEventArgs e)
        => await RunDiscoveryAsync();

    private async void StartQuestionnaireButton_OnClick(object sender, RoutedEventArgs e)
        => await RunQuestionnaireStepAsync(startNew: true);

    private async void ContinueQuestionnaireButton_OnClick(object sender, RoutedEventArgs e)
        => await RunQuestionnaireStepAsync(startNew: false);

    private async void SkipToTagsButton_OnClick(object sender, RoutedEventArgs e)
        => await FinishQuestionnaireAsync();

    private async Task RunDiscoveryAsync()
    {
        var description = DescriptionInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(description))
        {
            StatusText.Text = "Type what you want to see first.";
            return;
        }

        _discoverCts?.Cancel();
        _discoverCts = new CancellationTokenSource();
        var token = _discoverCts.Token;

        SuggestButton.IsEnabled = false;
        StatusText.Text = _mode == DiscoverMode.ConceptLookup
            ? "Identifying what you mean, then finding the tag…"
            : _mode == DiscoverMode.IntentSearch
                ? "Figuring out what you are looking for…"
            : _settings?.HasOpenAiForDescribeSearch == true
                ? "Asking OpenAI for search lines…"
                : "Looking up tags…";
        ClearResults();

        try
        {
            if (_settings is null)
            {
                return;
            }

            var result = await TagRecommendationService.DiscoverAsync(
                _app.Http,
                description,
                _settings,
                ToCoreMode(_mode),
                token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            RenderResults(result);
            UpdateStatusFromResult(result);
        }
        catch (OpenAiTagDiscoveryException ex)
        {
            StatusText.Text = ex.Message;
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = string.Empty;
        }
        catch
        {
            StatusText.Text = "Could not fetch suggestions. Check your connection.";
        }
        finally
        {
            SuggestButton.IsEnabled = true;
        }
    }

    private async Task RunQuestionnaireStepAsync(bool startNew)
    {
        if (_settings is null)
        {
            return;
        }

        if (!_settings.HasOpenAiForDescribeSearch)
        {
            StatusText.Text = "Add an OpenAI API key on the Account tab first.";
            return;
        }

        var theme = DescriptionInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(theme))
        {
            StatusText.Text = "Enter a base theme first.";
            return;
        }

        if (startNew)
        {
            _answers.Clear();
            _currentQuestion = null;
            _questionnaireTheme = theme;
            ClearResults();
        }
        else
        {
            if (_currentQuestion is null)
            {
                StatusText.Text = "Start the questionnaire first.";
                return;
            }

            var selectedIds = CollectSelectedOptionIds();
            var freeText = QuestionFreeTextInput.Text.Trim();
            if (selectedIds.Contains(TagQuestionnaireService.WriteOwnOptionId))
            {
                if (string.IsNullOrWhiteSpace(freeText))
                {
                    StatusText.Text = "Write your answer in the box below.";
                    QuestionFreeTextInput.Focus();
                    return;
                }
            }
            else if (selectedIds.Count == 0 && string.IsNullOrWhiteSpace(freeText))
            {
                StatusText.Text = "Pick an option or choose Write my own.";
                return;
            }

            _answers.Add(new TagQuestionnaireAnswer
            {
                QuestionId = _currentQuestion.Id,
                QuestionPrompt = _currentQuestion.Prompt,
                SelectedOptionIds = selectedIds,
                SelectedOptionLabels = TagQuestionnaireService.ResolveSelectedOptionLabels(
                    _currentQuestion,
                    selectedIds),
                FreeText = string.IsNullOrWhiteSpace(freeText) ? null : freeText,
            });
            _questionnaireTheme = theme;
        }

        _discoverCts?.Cancel();
        _discoverCts = new CancellationTokenSource();
        var token = _discoverCts.Token;

        SetQuestionnaireBusy(true);
        StatusText.Text = startNew
            ? "Preparing first question…"
            : "Loading next question…";

        try
        {
            var step = startNew
                ? await TagQuestionnaireService.StartAsync(_app.Http, theme, _settings, token)
                : await TagQuestionnaireService.ContinueAsync(
                    _app.Http,
                    theme,
                    _answers,
                    _currentQuestion,
                    _settings,
                    token);

            if (token.IsCancellationRequested)
            {
                return;
            }

            ApplyQuestionnaireStep(step);
        }
        catch (OpenAiTagDiscoveryException ex)
        {
            StatusText.Text = ex.Message;
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = string.Empty;
        }
        catch
        {
            StatusText.Text = "Could not continue the questionnaire. Check your connection.";
        }
        finally
        {
            SetQuestionnaireBusy(false);
        }
    }

    private async Task FinishQuestionnaireAsync()
    {
        if (_settings is null)
        {
            return;
        }

        var theme = DescriptionInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(theme))
        {
            StatusText.Text = "Enter a base theme first.";
            return;
        }

        _discoverCts?.Cancel();
        _discoverCts = new CancellationTokenSource();
        var token = _discoverCts.Token;

        SetQuestionnaireBusy(true);
        StatusText.Text = "Finding tags from your answers…";

        try
        {
            var step = await TagQuestionnaireService.FinishAsync(
                _app.Http,
                theme,
                _answers,
                _currentQuestion,
                _settings,
                token);

            if (token.IsCancellationRequested)
            {
                return;
            }

            ApplyQuestionnaireStep(step);
        }
        catch (OpenAiTagDiscoveryException ex)
        {
            StatusText.Text = ex.Message;
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = string.Empty;
        }
        catch
        {
            StatusText.Text = "Could not finish the questionnaire. Check your connection.";
        }
        finally
        {
            SetQuestionnaireBusy(false);
        }
    }

    private void ApplyQuestionnaireStep(TagQuestionnaireStepResult step)
    {
        if (step.IsComplete)
        {
            _currentQuestion = null;
            SetQuestionnaireActiveUi(false);
            StartQuestionnaireButton.Visibility = Visibility.Visible;
            QuestionnaireActionsPanel.Visibility = Visibility.Visible;
            ResultsPanel.Visibility = Visibility.Visible;

            if (step.FinalResult is not null)
            {
                RenderResults(step.FinalResult);
                UpdateStatusFromResult(step.FinalResult);
            }
            else
            {
                StatusText.Text = "No tag suggestions returned.";
            }

            return;
        }

        _currentQuestion = step.Question;
        SetQuestionnaireActiveUi(true);

        var round = _answers.Count + 1;
        var progress = $"Question {round} of {TagQuestionnaireService.MaxQuestionRounds}";
        QuestionProgressText.Text = string.IsNullOrWhiteSpace(step.Note)
            ? progress
            : $"{progress} · {step.Note}";
        QuestionPromptText.Text = step.Question?.Prompt ?? string.Empty;
        QuestionFreeTextInput.Text = string.Empty;
        UpdateFreeTextPrompt(false);
        RenderQuestionOptions(step.Question!);
        StatusText.Text = string.Empty;
    }

    private void RenderQuestionOptions(TagQuestionnaireQuestion question)
    {
        QuestionOptionsPanel.Children.Clear();
        foreach (var option in question.Options)
        {
            var row = new Grid
            {
                Margin = new Thickness(0, 0, 0, 4),
                Cursor = Cursors.Hand,
                Tag = option.Id,
            };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            if (question.AllowMultiple)
            {
                var checkBox = new CheckBox
                {
                    Tag = option.Id,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 2, 8, 0),
                };
                checkBox.Checked += (_, _) => HandleOptionChecked(option.Id, true, question.AllowMultiple);
                checkBox.Unchecked += (_, _) => HandleOptionChecked(option.Id, false, question.AllowMultiple);
                Grid.SetColumn(checkBox, 0);
                row.Children.Add(checkBox);
            }
            else
            {
                var radio = new RadioButton
                {
                    Tag = option.Id,
                    GroupName = $"q_{question.Id}",
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 2, 8, 0),
                };
                radio.Checked += (_, _) => HandleOptionChecked(option.Id, true, question.AllowMultiple);
                Grid.SetColumn(radio, 0);
                row.Children.Add(radio);
            }

            var text = new StackPanel();
            text.Children.Add(new TextBlock
            {
                Text = option.Label,
                Foreground = GetBrush("TextBrush"),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
            });

            if (!string.IsNullOrWhiteSpace(option.Description))
            {
                text.Children.Add(new TextBlock
                {
                    Text = option.Description,
                    Foreground = GetBrush("MutedBrush"),
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 1, 0, 0),
                });
            }

            Grid.SetColumn(text, 1);
            row.Children.Add(text);

            if (option.ResolvedTags.Count > 0)
            {
                var tooltip = "Rule34 tags: " + string.Join(", ", option.ResolvedTags);
                row.ToolTip = tooltip;
                ToolTipService.SetShowDuration(row, 8000);
            }

            row.MouseLeftButtonUp += (_, e) =>
            {
                if (e.OriginalSource is CheckBox or RadioButton)
                {
                    return;
                }

                foreach (var element in row.Children)
                {
                    if (element is CheckBox check)
                    {
                        check.IsChecked = !check.IsChecked;
                    }
                    else if (element is RadioButton radio)
                    {
                        radio.IsChecked = true;
                    }
                }

                e.Handled = true;
            };

            QuestionOptionsPanel.Children.Add(row);
        }
    }

    private void HandleOptionChecked(string optionId, bool isChecked, bool allowMultiple)
    {
        if (!isChecked)
        {
            UpdateFreeTextPrompt(CollectSelectedOptionIds().Contains(TagQuestionnaireService.WriteOwnOptionId));
            return;
        }

        if (optionId.Equals(TagQuestionnaireService.WriteOwnOptionId, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var child in QuestionOptionsPanel.Children)
            {
                if (child is not Grid row)
                {
                    continue;
                }

                foreach (var element in row.Children)
                {
                    if (element is CheckBox check &&
                        check.Tag is string id &&
                        !id.Equals(TagQuestionnaireService.WriteOwnOptionId, StringComparison.OrdinalIgnoreCase))
                    {
                        check.IsChecked = false;
                    }
                }
            }

            QuestionFreeTextInput.Focus();
            UpdateFreeTextPrompt(true);
            return;
        }

        if (allowMultiple)
        {
            foreach (var child in QuestionOptionsPanel.Children)
            {
                if (child is not Grid row)
                {
                    continue;
                }

                foreach (var element in row.Children)
                {
                    if (element is CheckBox check &&
                        check.Tag is string id &&
                        id.Equals(TagQuestionnaireService.WriteOwnOptionId, StringComparison.OrdinalIgnoreCase))
                    {
                        check.IsChecked = false;
                    }
                }
            }
        }

        UpdateFreeTextPrompt(false);
    }

    private void UpdateFreeTextPrompt(bool writeOwnSelected)
    {
        if (writeOwnSelected)
        {
            QuestionFreeTextLabel.Text = "Your answer";
            QuestionFreeTextInput.ToolTip = "Required when Write my own is selected";
            return;
        }

        QuestionFreeTextLabel.Text = "Optional details";
        QuestionFreeTextInput.ToolTip = "Optional — add details in your own words";
    }

    private List<string> CollectSelectedOptionIds()
    {
        var ids = new List<string>();
        foreach (var child in QuestionOptionsPanel.Children)
        {
            if (child is not Grid row)
            {
                continue;
            }

            foreach (var element in row.Children)
            {
                switch (element)
                {
                    case CheckBox { IsChecked: true, Tag: string checkId }:
                        ids.Add(checkId);
                        break;
                    case RadioButton { IsChecked: true, Tag: string radioId }:
                        ids.Add(radioId);
                        break;
                }
            }
        }

        return ids;
    }

    private void SetQuestionnaireBusy(bool busy)
    {
        StartQuestionnaireButton.IsEnabled = !busy;
        ContinueQuestionnaireButton.IsEnabled = !busy;
        SkipToTagsButton.IsEnabled = !busy;
        DescriptionInput.IsEnabled = !busy;
        ThemeModeRadio.IsEnabled = !busy;
        QuestionnaireModeRadio.IsEnabled = !busy;
    }

    private void UpdateStatusFromResult(TagDiscoveryResult result)
    {
        if (result.SearchHits.Count > 0)
        {
            var hitCount = result.SearchHits.Count;
            StatusText.Text = string.IsNullOrWhiteSpace(result.SourceNote)
                ? $"{hitCount} match(es)"
                : $"{hitCount} match(es) · {result.SourceNote}";
            return;
        }

        var presetCount = result.Presets.Count;
        var lineCount = result.Combinations.Count;
        var tagCount = result.Tags.Count;
        if (presetCount == 0 && lineCount == 0 && tagCount == 0)
        {
            StatusText.Text = string.IsNullOrWhiteSpace(result.SourceNote)
                ? "No matches — try different words or shorter phrases."
                : result.SourceNote;
            return;
        }

        var parts = new List<string>();
        if (lineCount > 0)
        {
            parts.Add($"{lineCount} line(s)");
        }

        if (tagCount > 0)
        {
            parts.Add($"{tagCount} tag(s)");
        }

        if (presetCount > 0)
        {
            parts.Add($"{presetCount} preset(s)");
        }

        var counts = string.Join(", ", parts);
        StatusText.Text = string.IsNullOrWhiteSpace(result.SourceNote)
            ? counts
            : $"{counts} · {result.SourceNote}";
    }

    private void RenderResults(TagDiscoveryResult result)
    {
        PresetsPanel.Children.Clear();
        CombinationsPanel.Children.Clear();
        TagsPanel.Children.Clear();
        SearchHitsPanel.Children.Clear();

        var hasSearchHits = result.SearchHits.Count > 0;
        var hasSummary = !string.IsNullOrWhiteSpace(result.InterpretationSummary);
        InterpretationSummaryText.Text = result.InterpretationSummary;
        InterpretationSummaryText.Visibility = hasSummary ? Visibility.Visible : Visibility.Collapsed;
        SearchHitsHeader.Visibility = hasSearchHits ? Visibility.Visible : Visibility.Collapsed;
        SearchHitsPanel.Visibility = hasSearchHits ? Visibility.Visible : Visibility.Collapsed;

        if (hasSearchHits)
        {
            PresetsHeader.Visibility = Visibility.Collapsed;
            CombinationsHeader.Visibility = Visibility.Collapsed;
            TagsHeader.Visibility = Visibility.Collapsed;
            EmptyText.Visibility = Visibility.Collapsed;

            foreach (var hit in result.SearchHits)
            {
                SearchHitsPanel.Children.Add(BuildSearchHitRow(hit));
            }

            return;
        }

        var hasPresets = result.Presets.Count > 0;
        var hasCombinations = result.Combinations.Count > 0;
        var hasTags = result.Tags.Count > 0;
        PresetsHeader.Visibility = hasPresets ? Visibility.Visible : Visibility.Collapsed;
        CombinationsHeader.Visibility = hasCombinations ? Visibility.Visible : Visibility.Collapsed;
        TagsHeader.Visibility = hasTags ? Visibility.Visible : Visibility.Collapsed;
        EmptyText.Visibility = hasPresets || hasCombinations || hasTags ? Visibility.Collapsed : Visibility.Visible;

        foreach (var preset in result.Presets)
        {
            PresetsPanel.Children.Add(BuildPresetRow(preset));
        }

        foreach (var combination in result.Combinations)
        {
            CombinationsPanel.Children.Add(BuildCombinationRow(combination));
        }

        foreach (var tag in result.Tags)
        {
            TagsPanel.Children.Add(BuildTagRow(tag));
        }
    }

    private UIElement BuildSearchHitRow(SearchInterpretationHit hit)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x18)),
            BorderBrush = GetBrush("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 0, 8),
        };

        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var content = new StackPanel();
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
        titleRow.Children.Add(new TextBlock
        {
            Text = hit.Title,
            Foreground = GetBrush("TextBrush"),
            FontWeight = FontWeights.SemiBold,
            FontSize = 14,
        });
        if (hit.ConfidencePercent > 0)
        {
            titleRow.Children.Add(new TextBlock
            {
                Text = $"  ·  {hit.ConfidencePercent}%",
                Foreground = GetBrush("MutedBrush"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
            });
        }

        content.Children.Add(titleRow);
        if (!string.IsNullOrWhiteSpace(hit.Subtitle))
        {
            content.Children.Add(new TextBlock
            {
                Text = hit.Subtitle,
                Foreground = GetBrush("R34GreenBrush"),
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 4),
            });
        }

        if (!string.IsNullOrWhiteSpace(hit.Snippet))
        {
            content.Children.Add(new TextBlock
            {
                Text = hit.Snippet,
                Foreground = GetBrush("MutedBrush"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
            });
        }

        Grid.SetColumn(content, 0);
        root.Children.Add(content);

        var copyButton = new Button
        {
            Content = "Copy name",
            Style = Application.Current.TryFindResource("SecondaryButton") as Style,
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(10, 4, 10, 4),
            VerticalAlignment = VerticalAlignment.Top,
            Tag = hit.Title,
        };
        copyButton.Click += CopySearchHitButton_OnClick;
        Grid.SetColumn(copyButton, 1);
        root.Children.Add(copyButton);

        border.Child = root;
        return border;
    }

    private void CopySearchHitButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string title } && !string.IsNullOrWhiteSpace(title))
        {
            Clipboard.SetText(title);
            StatusText.Text = $"Copied \"{title}\"";
        }
    }

    private UIElement BuildPresetRow(SearchPresetRecommendation preset)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var stack = new StackPanel();
        var title = new TextBlock
        {
            Text = preset.Name,
            Foreground = GetBrush("TextBrush"),
            FontWeight = FontWeights.SemiBold,
        };
        var meta = new TextBlock
        {
            Text = $"{preset.Description} · {string.Join(" + ", preset.Tags)}",
            Foreground = GetBrush("MutedBrush"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0),
        };
        stack.Children.Add(title);
        stack.Children.Add(meta);
        Grid.SetColumn(stack, 0);
        grid.Children.Add(stack);

        var enableButton = new Button
        {
            Content = "Enable preset",
            Style = Application.Current.TryFindResource("SecondaryButton") as Style,
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(10, 4, 10, 4),
            VerticalAlignment = VerticalAlignment.Center,
            Tag = preset.PresetId,
        };
        enableButton.Click += EnablePresetButton_OnClick;
        Grid.SetColumn(enableButton, 1);
        grid.Children.Add(enableButton);

        return grid;
    }

    private UIElement BuildCombinationRow(TagCombinationRecommendation combination)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x18)),
            BorderBrush = GetBrush("BorderBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 0, 8),
        };

        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var content = new StackPanel();
        content.Children.Add(new TextBlock
        {
            Text = combination.Label,
            Foreground = GetBrush("TextBrush"),
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
        });
        content.Children.Add(new TextBlock
        {
            Text = combination.Reason,
            Foreground = GetBrush("MutedBrush"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 6),
        });
        content.Children.Add(new TextBlock
        {
            Text = combination.LineDisplay,
            Foreground = GetBrush("R34GreenBrush"),
            FontFamily = new FontFamily("Consolas, Courier New"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 6),
        });

        var chips = new WrapPanel();
        foreach (var tag in combination.Tags)
        {
            chips.Children.Add(new TagChip
            {
                TagText = tag,
                Category = TagCategoryColors.InferCategory(tag),
                Margin = new Thickness(0, 0, 6, 4),
            });
        }

        content.Children.Add(chips);
        Grid.SetColumn(content, 0);
        root.Children.Add(content);

        var addLineButton = new Button
        {
            Content = "Add line",
            Style = Application.Current.TryFindResource("PrimaryButton") as Style,
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(12, 6, 12, 6),
            VerticalAlignment = VerticalAlignment.Top,
            Tag = combination,
        };
        addLineButton.Click += AddLineButton_OnClick;
        Grid.SetColumn(addLineButton, 1);
        root.Children.Add(addLineButton);

        border.Child = root;
        return border;
    }

    private UIElement BuildTagRow(TagRecommendation recommendation)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var stack = new StackPanel { Orientation = Orientation.Horizontal };
        stack.Children.Add(new TagChip
        {
            TagText = recommendation.Tag,
            Category = recommendation.Category,
            Margin = new Thickness(0, 0, 8, 0),
        });
        stack.Children.Add(new TextBlock
        {
            Text = recommendation.Reason,
            Foreground = GetBrush("MutedBrush"),
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        });
        Grid.SetColumn(stack, 0);
        grid.Children.Add(stack);

        var addButton = new Button
        {
            Content = "Add",
            Style = Application.Current.TryFindResource("SecondaryButton") as Style,
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(10, 4, 10, 4),
            VerticalAlignment = VerticalAlignment.Center,
            Tag = recommendation.Tag,
        };
        addButton.Click += AddTagButton_OnClick;
        Grid.SetColumn(addButton, 1);
        grid.Children.Add(addButton);

        return grid;
    }

    private static Brush GetBrush(string key)
        => Application.Current.TryFindResource(key) as Brush ?? Brushes.Gray;

    private void EnablePresetButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_settings is null || sender is not Button { Tag: string presetId })
        {
            return;
        }

        _settings.SetSearchPresetActive(presetId, true);
        _onChanged?.Invoke();
        StatusText.Text = "Preset enabled.";
    }

    private void AddLineButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_settings is null || sender is not Button { Tag: TagCombinationRecommendation combination })
        {
            return;
        }

        foreach (var tag in combination.Tags)
        {
            _settings.AddIncludeTag(tag);
        }

        _onChanged?.Invoke();
        StatusText.Text = $"Added line ({combination.Tags.Count} tags).";
    }

    private void AddTagButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_settings is null || sender is not Button { Tag: string tag })
        {
            return;
        }

        _settings.AddIncludeTag(tag);
        _onChanged?.Invoke();
        StatusText.Text = $"Added {tag}.";
    }

    private void ClearResults()
    {
        PresetsPanel.Children.Clear();
        CombinationsPanel.Children.Clear();
        TagsPanel.Children.Clear();
        SearchHitsPanel.Children.Clear();
        InterpretationSummaryText.Visibility = Visibility.Collapsed;
        SearchHitsHeader.Visibility = Visibility.Collapsed;
        SearchHitsPanel.Visibility = Visibility.Collapsed;
        PresetsHeader.Visibility = Visibility.Collapsed;
        CombinationsHeader.Visibility = Visibility.Collapsed;
        TagsHeader.Visibility = Visibility.Collapsed;
        EmptyText.Visibility = Visibility.Visible;
    }

    private void DoneButton_OnClick(object sender, RoutedEventArgs e) => Hide();

    private void DescriptionInput_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (_mode is DiscoverMode.Theme or DiscoverMode.ConceptLookup or DiscoverMode.IntentSearch)
            {
                _ = RunDiscoveryAsync();
            }
            else if (_currentQuestion is null)
            {
                _ = RunQuestionnaireStepAsync(startNew: true);
            }
            else
            {
                _ = RunQuestionnaireStepAsync(startNew: false);
            }

            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
    }

    private static DescribeDiscoveryMode ToCoreMode(DiscoverMode mode) => mode switch
    {
        DiscoverMode.Questionnaire => DescribeDiscoveryMode.Questionnaire,
        DiscoverMode.ConceptLookup => DescribeDiscoveryMode.ConceptLookup,
        DiscoverMode.IntentSearch => DescribeDiscoveryMode.IntentSearch,
        _ => DescribeDiscoveryMode.Theme,
    };

    private void TagDiscoverOverlay_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && !DescriptionInput.IsKeyboardFocusWithin)
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
