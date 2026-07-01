using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Rule34Gallery.Core;
using Rule34Gallery.Core.Services;

namespace Rule34GalleryApp.Views.Overlays;

public partial class ForYouManageOverlay : UserControl
{
    private enum ManageMode
    {
        Topics,
        SearchLines,
        Signals,
    }

    private ManageMode _mode;
    private Func<Task>? _refreshPageAsync;

    private Func<IReadOnlyList<ForYouTopicProfile>>? _loadTopics;
    private Func<string, double, Task>? _addTopicAsync;
    private Func<ForYouTopicProfile, double, Task>? _setTopicStrengthAsync;
    private Action<ForYouTopicProfile>? _pinTopic;
    private Action<ForYouTopicProfile>? _hideTopic;
    private Action<ForYouTopicProfile>? _removeTopic;
    private Action<ForYouTopicProfile>? _promoteTopic;

    private Func<IReadOnlyList<ForYouSearchLine>>? _loadSearchLines;
    private Func<ForYouSearchLine, Task>? _runSearchLineAsync;
    private Action<ForYouSearchLine>? _pinSearchLine;
    private Action<ForYouSearchLine>? _hideSearchLine;

    private Func<IReadOnlyList<ForYouActivityEntry>>? _loadSignals;
    private Func<ForYouActivityEntry, Task>? _removeSignalAsync;
    private Func<ForYouActivityEntry, Task>? _boostSignalAsync;

    private ForYouTopicProfile? _editingTopic;
    private bool _strengthEditorIsAdd;
    private readonly HashSet<string> _knownTopicKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _sessionNewTopicKeys = new(StringComparer.OrdinalIgnoreCase);

    public ForYouManageOverlay()
    {
        InitializeComponent();
        StrengthSlider.ValueChanged += (_, _) => UpdateStrengthLabel();
    }

    public void ShowTopics(
        Func<IReadOnlyList<ForYouTopicProfile>> loadTopics,
        Func<string, double, Task> addTopicAsync,
        Func<ForYouTopicProfile, double, Task> setTopicStrengthAsync,
        Action<ForYouTopicProfile> pinTopic,
        Action<ForYouTopicProfile> hideTopic,
        Action<ForYouTopicProfile> removeTopic,
        Action<ForYouTopicProfile> promoteTopic,
        Func<Task>? refreshPageAsync = null)
    {
        _mode = ManageMode.Topics;
        _loadTopics = loadTopics;
        _addTopicAsync = addTopicAsync;
        _setTopicStrengthAsync = setTopicStrengthAsync;
        _pinTopic = pinTopic;
        _hideTopic = hideTopic;
        _removeTopic = removeTopic;
        _promoteTopic = promoteTopic;
        _refreshPageAsync = refreshPageAsync;
        ClearSearchLineCallbacks();
        ClearSignalCallbacks();

        TitleText.Text = "Manage topics";
        HintText.Text = "Manual topics (typed, boosted, or promoted) never decay, are never auto-removed, and do not use algorithm topic slots. Promote an algorithm topic to move it to Manual. Set score 0–100, pin, hide (block), or remove (frees an algorithm slot).";
        AddTopicButton.Visibility = Visibility.Visible;
        HideStrengthEditor();
        ResetTopicHighlightTracking();
        SeedKnownTopics();
        ReloadList();
        ShowOverlay();
    }

    public void ShowSearchLines(
        Func<IReadOnlyList<ForYouSearchLine>> loadLines,
        Func<ForYouSearchLine, Task> runLineAsync,
        Action<ForYouSearchLine> pinLine,
        Action<ForYouSearchLine> hideLine,
        Func<Task>? refreshPageAsync = null)
    {
        _mode = ManageMode.SearchLines;
        _loadSearchLines = loadLines;
        _runSearchLineAsync = runLineAsync;
        _pinSearchLine = pinLine;
        _hideSearchLine = hideLine;
        _refreshPageAsync = refreshPageAsync;
        ClearTopicCallbacks();
        ClearSignalCallbacks();

        TitleText.Text = "Suggested searches";
        HintText.Text = "Run a search, pin lines you like, or hide ones you do not want suggested again.";
        AddTopicButton.Visibility = Visibility.Collapsed;
        HideStrengthEditor();
        ReloadList();
        ShowOverlay();
    }

    public void ShowSignals(
        Func<IReadOnlyList<ForYouActivityEntry>> loadSignals,
        Func<ForYouActivityEntry, Task> removeSignalAsync,
        Func<ForYouActivityEntry, Task> boostSignalAsync,
        Func<Task>? refreshPageAsync = null)
    {
        _mode = ManageMode.Signals;
        _loadSignals = loadSignals;
        _removeSignalAsync = removeSignalAsync;
        _boostSignalAsync = boostSignalAsync;
        _refreshPageAsync = refreshPageAsync;
        ClearTopicCallbacks();
        ClearSearchLineCallbacks();

        TitleText.Text = "Recent signals";
        HintText.Text = "Boost a signal into your topics, or remove signals you do not want influencing your profile.";
        AddTopicButton.Visibility = Visibility.Collapsed;
        HideStrengthEditor();
        ReloadList();
        ShowOverlay();
    }

    public void Hide()
    {
        Visibility = Visibility.Collapsed;
        HideStrengthEditor();
        ListPanel.Children.Clear();
        ClearTopicCallbacks();
        ClearSearchLineCallbacks();
        ClearSignalCallbacks();
        _refreshPageAsync = null;
        ResetTopicHighlightTracking();
    }

    private void ResetTopicHighlightTracking()
    {
        _knownTopicKeys.Clear();
        _sessionNewTopicKeys.Clear();
    }

    private void SeedKnownTopics()
    {
        if (_loadTopics is null)
        {
            return;
        }

        foreach (var topic in _loadTopics())
        {
            _knownTopicKeys.Add(topic.Topic);
        }
    }

    private void TrackNewTopics(IReadOnlyList<ForYouTopicProfile> topics)
    {
        foreach (var topic in topics)
        {
            if (!_knownTopicKeys.Contains(topic.Topic))
            {
                _sessionNewTopicKeys.Add(topic.Topic);
            }

            _knownTopicKeys.Add(topic.Topic);
        }
    }

    private bool IsTopicHighlighted(ForYouTopicProfile topic)
        => AppServices.Current.ForYou.IsTopicNew(topic.Topic) || _sessionNewTopicKeys.Contains(topic.Topic);

    private void ClearTopicHighlight(ForYouTopicProfile topic, Border card)
    {
        if (!IsTopicHighlighted(topic))
        {
            return;
        }

        AppServices.Current.ForYou.ClearTopicNewHighlight(topic.Topic);
        _sessionNewTopicKeys.Remove(topic.Topic);
        ApplyNewTopicCardStyle(card, highlighted: false);
        if (card.Tag is TopicHighlightChrome chrome)
        {
            chrome.SetVisible(false);
        }
    }

    private sealed class TopicHighlightChrome(TextBlock dot, TextBlock badge)
    {
        public void SetVisible(bool visible)
        {
            var v = visible ? Visibility.Visible : Visibility.Collapsed;
            dot.Visibility = v;
            badge.Visibility = v;
        }
    }

    private void ShowOverlay()
    {
        Visibility = Visibility.Visible;
        Focus();
    }

    private void ReloadList()
    {
        ListPanel.Children.Clear();
        EmptyText.Visibility = Visibility.Collapsed;

        switch (_mode)
        {
            case ManageMode.Topics:
                ReloadTopics();
                break;
            case ManageMode.SearchLines:
                ReloadSearchLines();
                break;
            case ManageMode.Signals:
                ReloadSignals();
                break;
        }
    }

    private void ReloadTopics()
    {
        if (_loadTopics is null)
        {
            return;
        }

        var topics = _loadTopics();
        if (topics.Count == 0)
        {
            EmptyText.Text = "No topics yet. Add one or browse to start learning.";
            EmptyText.Visibility = Visibility.Visible;
            return;
        }

        TrackNewTopics(topics);

        var manual = topics
            .Where(ForYouTopicOrigin.IsManual)
            .OrderByDescending(t => t.IsPinned)
            .ThenByDescending(t => t.Score)
            .ThenBy(t => t.Topic, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var algorithm = topics
            .Where(t => !ForYouTopicOrigin.IsManual(t))
            .OrderByDescending(t => t.IsPinned)
            .ThenByDescending(t => t.Score)
            .ThenBy(t => t.Topic, StringComparer.OrdinalIgnoreCase)
            .ToList();

        ListPanel.Children.Add(BuildTopicOriginExpander(
            "Manual",
            "Added when you type a tag or boost a signal. These never decay and do not count toward the algorithm topic limit.",
            manual,
            isExpanded: true));
        ListPanel.Children.Add(BuildTopicOriginExpander(
            "From algorithm",
            "Learned automatically from browsing. Use Promote to keep a topic without decay or slot limits.",
            algorithm,
            isExpanded: manual.Count == 0));
    }

    private Expander BuildTopicOriginExpander(
        string title,
        string emptyHint,
        IReadOnlyList<ForYouTopicProfile> sectionTopics,
        bool isExpanded)
    {
        var content = new StackPanel();
        if (sectionTopics.Count == 0)
        {
            content.Children.Add(Muted(emptyHint, fontSize: 12, margin: new Thickness(0, 0, 0, 4)));
        }
        else
        {
            foreach (var topic in sectionTopics)
            {
                content.Children.Add(BuildTopicCard(topic, allowPromote: title == "From algorithm"));
            }
        }

        return new Expander
        {
            Header = BuildOriginHeader(title, sectionTopics.Count),
            Content = content,
            IsExpanded = isExpanded,
            Style = Application.Current.Resources["R34Expander"] as Style,
            Margin = new Thickness(0, 0, 0, 8),
        };
    }

    private void ReloadSearchLines()
    {
        if (_loadSearchLines is null)
        {
            return;
        }

        var lines = _loadSearchLines();
        if (lines.Count == 0)
        {
            EmptyText.Text = "No suggested searches yet.";
            EmptyText.Visibility = Visibility.Visible;
            return;
        }

        foreach (var line in lines)
        {
            ListPanel.Children.Add(BuildSearchLineCard(line));
        }
    }

    private void ReloadSignals()
    {
        if (_loadSignals is null)
        {
            return;
        }

        var signals = _loadSignals();
        if (signals.Count == 0)
        {
            EmptyText.Text = "No recent signals saved.";
            EmptyText.Visibility = Visibility.Visible;
            return;
        }

        foreach (var entry in signals)
        {
            ListPanel.Children.Add(BuildSignalCard(entry));
        }
    }

    private Border BuildTopicCard(ForYouTopicProfile topic, bool allowPromote = false)
    {
        var highlighted = IsTopicHighlighted(topic);
        var card = Card(padding: 14);
        ApplyNewTopicCardStyle(card, highlighted);

        var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var info = new StackPanel();
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
        var newDot = new TextBlock
        {
            Text = "●",
            Foreground = Application.Current.Resources["R34GreenBrush"] as Brush,
            FontSize = 14,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = highlighted ? Visibility.Visible : Visibility.Collapsed,
        };
        titleRow.Children.Add(newDot);
        titleRow.Children.Add(Green(topic.Topic.Replace('_', ' '), fontSize: 16));
        var newBadge = NewBadge();
        newBadge.Visibility = highlighted ? Visibility.Visible : Visibility.Collapsed;
        titleRow.Children.Add(newBadge);
        card.Tag = new TopicHighlightChrome(newDot, newBadge);

        info.Children.Add(titleRow);
        if (!string.IsNullOrWhiteSpace(topic.Why))
        {
            info.Children.Add(Muted(topic.Why, fontSize: 12, margin: new Thickness(0, 6, 0, 0)));
        }

        var decay = AppServices.Current.ForYou.GetTopicSearchDecayPenalty(topic.Topic);
        var scoreText = decay > 0
            ? $"Score {topic.Score:F1}  (−{decay:0} search decay)"
            : $"Score {topic.Score:F1}";
        info.Children.Add(Muted(scoreText, fontSize: 12, margin: new Thickness(0, 6, 0, 0)));

        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        var pin = Secondary("Pin", padding: new Thickness(14, 8, 14, 8));
        pin.Margin = new Thickness(0, 0, 8, 0);
        pin.Click += async (_, _) =>
        {
            _pinTopic?.Invoke(topic);
            await RefreshAfterChangeAsync();
        };
        var set = Secondary("Set", padding: new Thickness(14, 8, 14, 8));
        set.Margin = new Thickness(0, 0, 8, 0);
        set.Click += (_, _) => ShowStrengthEditor(topic, isAdd: false);
        var hide = Secondary("Hide", padding: new Thickness(14, 8, 14, 8));
        hide.Margin = new Thickness(0, 0, 8, 0);
        hide.Click += async (_, _) =>
        {
            _hideTopic?.Invoke(topic);
            await RefreshAfterChangeAsync();
        };
        var remove = Secondary("Remove", padding: new Thickness(14, 8, 14, 8));
        remove.Click += async (_, _) =>
        {
            _removeTopic?.Invoke(topic);
            ReloadList();
            if (_refreshPageAsync is not null)
            {
                await _refreshPageAsync();
            }
        };
        if (allowPromote && !ForYouTopicOrigin.IsManual(topic))
        {
            var promote = Secondary("Promote", padding: new Thickness(14, 8, 14, 8));
            promote.Margin = new Thickness(0, 0, 8, 0);
            promote.Click += async (_, _) =>
            {
                _promoteTopic?.Invoke(topic);
                await RefreshAfterChangeAsync();
            };
            actions.Children.Add(promote);
        }

        actions.Children.Add(pin);
        actions.Children.Add(set);
        actions.Children.Add(hide);
        actions.Children.Add(remove);

        Grid.SetColumn(info, 0);
        Grid.SetColumn(actions, 1);
        grid.Children.Add(info);
        grid.Children.Add(actions);
        card.Child = grid;

        card.MouseEnter += (_, _) => ClearTopicHighlight(topic, card);
        return card;
    }

    private static TextBlock NewBadge() => new()
    {
        Text = "new",
        Foreground = Application.Current.Resources["R34GreenBrush"] as Brush,
        Background = new SolidColorBrush(Color.FromArgb(48, 76, 175, 80)),
        FontSize = 10,
        FontWeight = FontWeights.SemiBold,
        Padding = new Thickness(6, 2, 6, 2),
        Margin = new Thickness(8, 0, 0, 0),
        VerticalAlignment = VerticalAlignment.Center,
    };

    private static void ApplyNewTopicCardStyle(Border card, bool highlighted)
    {
        if (highlighted)
        {
            card.BorderBrush = Application.Current.Resources["R34GreenBrush"] as Brush;
            card.BorderThickness = new Thickness(3, 1, 1, 1);
            card.Background = new SolidColorBrush(Color.FromArgb(255, 24, 38, 24));
        }
        else
        {
            card.BorderBrush = Application.Current.Resources["BorderBrush"] as Brush;
            card.BorderThickness = new Thickness(1);
            card.Background = Application.Current.Resources["BgBrush"] as Brush;
        }
    }

    private Border BuildSearchLineCard(ForYouSearchLine line)
    {
        var card = Card(padding: 14);
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var info = new StackPanel();
        info.Children.Add(Body(line.Label, fontSize: 15));
        if (!string.IsNullOrWhiteSpace(line.Reason))
        {
            info.Children.Add(Muted(line.Reason, fontSize: 12, margin: new Thickness(0, 6, 0, 0)));
        }

        info.Children.Add(Muted($"Search: {line.Query}", fontSize: 12, margin: new Thickness(0, 6, 0, 0)));

        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        var run = Secondary("Run", padding: new Thickness(14, 8, 14, 8));
        run.Margin = new Thickness(0, 0, 8, 0);
        run.Click += async (_, _) =>
        {
            if (_runSearchLineAsync is not null)
            {
                await _runSearchLineAsync(line);
            }

            Hide();
        };
        var pin = Secondary("Pin", padding: new Thickness(14, 8, 14, 8));
        pin.Margin = new Thickness(0, 0, 8, 0);
        pin.Click += async (_, _) =>
        {
            _pinSearchLine?.Invoke(line);
            await RefreshAfterChangeAsync();
        };
        var hide = Secondary("Hide", padding: new Thickness(14, 8, 14, 8));
        hide.Click += async (_, _) =>
        {
            _hideSearchLine?.Invoke(line);
            await RefreshAfterChangeAsync();
        };
        actions.Children.Add(run);
        actions.Children.Add(pin);
        actions.Children.Add(hide);

        Grid.SetColumn(info, 0);
        Grid.SetColumn(actions, 1);
        grid.Children.Add(info);
        grid.Children.Add(actions);
        card.Child = grid;
        return card;
    }

    private Border BuildSignalCard(ForYouActivityEntry entry)
    {
        var card = Card(padding: 14);
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var info = new StackPanel();
        info.Children.Add(Body(entry.DisplayLabel, fontSize: 14));

        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        var boost = Secondary("Boost", padding: new Thickness(14, 8, 14, 8));
        boost.Margin = new Thickness(0, 0, 8, 0);
        boost.Click += async (_, _) =>
        {
            if (_boostSignalAsync is not null)
            {
                await _boostSignalAsync(entry);
            }

            await RefreshAfterChangeAsync();
        };
        var remove = Secondary("Remove", padding: new Thickness(14, 8, 14, 8));
        remove.Click += async (_, _) =>
        {
            if (_removeSignalAsync is not null)
            {
                await _removeSignalAsync(entry);
            }

            await RefreshAfterChangeAsync();
        };
        actions.Children.Add(boost);
        actions.Children.Add(remove);

        Grid.SetColumn(info, 0);
        Grid.SetColumn(actions, 1);
        grid.Children.Add(info);
        grid.Children.Add(actions);
        card.Child = grid;
        return card;
    }

    private async Task RefreshAfterChangeAsync()
    {
        ReloadList();
        if (_refreshPageAsync is not null)
        {
            await _refreshPageAsync();
        }
    }

    private void ShowStrengthEditor(ForYouTopicProfile? topic, bool isAdd)
    {
        _editingTopic = topic;
        _strengthEditorIsAdd = isAdd;
        StrengthEditorTitle.Text = isAdd ? "Add topic" : "Set topic score";
        StrengthTagBox.Text = isAdd ? string.Empty : topic?.Topic ?? string.Empty;
        StrengthTagBox.IsReadOnly = !isAdd;
        StrengthTagBox.Visibility = isAdd ? Visibility.Visible : Visibility.Collapsed;
        StrengthTagLabel.Visibility = isAdd ? Visibility.Visible : Visibility.Collapsed;
        StrengthTopicName.Text = isAdd ? string.Empty : topic?.Topic.Replace('_', ' ') ?? string.Empty;
        StrengthTopicName.Visibility = isAdd ? Visibility.Collapsed : Visibility.Visible;
        StrengthSlider.Value = Math.Clamp(topic?.Score ?? 80, 0, 100);
        UpdateStrengthLabel();
        StrengthEditorPanel.Visibility = Visibility.Visible;
        if (isAdd)
        {
            StrengthTagBox.Focus();
        }
    }

    private void HideStrengthEditor()
    {
        StrengthEditorPanel.Visibility = Visibility.Collapsed;
        _editingTopic = null;
    }

    private void UpdateStrengthLabel()
        => StrengthValueText.Text = $"Score: {StrengthSlider.Value:F1}";

    private void AddTopicButton_OnClick(object sender, RoutedEventArgs e)
        => ShowStrengthEditor(null, isAdd: true);

    private void StrengthCancel_OnClick(object sender, RoutedEventArgs e)
        => HideStrengthEditor();

    private async void StrengthSave_OnClick(object sender, RoutedEventArgs e)
    {
        var tag = UserSettings.NormalizeTagToken(
            _strengthEditorIsAdd ? StrengthTagBox.Text : _editingTopic?.Topic ?? string.Empty);
        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        var strength = Math.Clamp(StrengthSlider.Value, 0, 100);
        if (_strengthEditorIsAdd)
        {
            if (_addTopicAsync is not null)
            {
                await _addTopicAsync(tag, strength);
            }
        }
        else if (_editingTopic is not null && _setTopicStrengthAsync is not null)
        {
            await _setTopicStrengthAsync(_editingTopic, strength);
        }

        HideStrengthEditor();
        await RefreshAfterChangeAsync();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        => Hide();

    private void Backdrop_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource == sender)
        {
            Hide();
        }
    }

    private void DialogPanel_OnMouseDown(object sender, MouseButtonEventArgs e)
        => e.Handled = true;

    private void ForYouManageOverlay_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (StrengthEditorPanel.Visibility == Visibility.Visible)
            {
                HideStrengthEditor();
            }
            else
            {
                Hide();
            }

            e.Handled = true;
        }
    }

    private void ClearTopicCallbacks()
    {
        _loadTopics = null;
        _addTopicAsync = null;
        _setTopicStrengthAsync = null;
        _pinTopic = null;
        _hideTopic = null;
        _removeTopic = null;
        _promoteTopic = null;
    }

    private void ClearSearchLineCallbacks()
    {
        _loadSearchLines = null;
        _runSearchLineAsync = null;
        _pinSearchLine = null;
        _hideSearchLine = null;
    }

    private void ClearSignalCallbacks()
    {
        _loadSignals = null;
        _removeSignalAsync = null;
        _boostSignalAsync = null;
    }

    private static TextBlock BuildOriginHeader(string title, int count) => new()
    {
        Text = count > 0 ? $"{title} ({count})" : title,
        Foreground = Application.Current.Resources["TextBrush"] as Brush,
        FontWeight = FontWeights.SemiBold,
        FontSize = 15,
        Margin = new Thickness(0, 12, 0, 8),
    };

    private static Border Card(double padding = 10) => new()
    {
        Background = Application.Current.Resources["BgBrush"] as Brush,
        BorderBrush = Application.Current.Resources["BorderBrush"] as Brush,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(10),
        Padding = new Thickness(padding),
        Margin = new Thickness(0, 0, 0, 10),
    };

    private static TextBlock Muted(string text, double fontSize = 11, Thickness? margin = null) => new()
    {
        Text = text,
        Foreground = Application.Current.Resources["MutedBrush"] as Brush,
        FontSize = fontSize,
        TextWrapping = TextWrapping.Wrap,
        Margin = margin ?? new Thickness(0, 0, 0, 6),
    };

    private static TextBlock Green(string text, double fontSize = 14) => new()
    {
        Text = text,
        Foreground = Application.Current.Resources["R34GreenBrush"] as Brush,
        FontWeight = FontWeights.SemiBold,
        FontSize = fontSize,
        TextWrapping = TextWrapping.Wrap,
    };

    private static TextBlock Body(string text, double fontSize = 14) => new()
    {
        Text = text,
        Foreground = Application.Current.Resources["TextBrush"] as Brush,
        FontWeight = FontWeights.SemiBold,
        FontSize = fontSize,
        TextWrapping = TextWrapping.Wrap,
    };

    private static Button Secondary(string text, Thickness? padding = null) => new()
    {
        Content = text,
        Style = Application.Current.Resources["SecondaryButton"] as Style,
        Padding = padding ?? new Thickness(10, 6, 10, 6),
    };
}
