using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Rule34Gallery.Core.Help;
using Rule34GalleryApp.Helpers;

namespace Rule34GalleryApp.Views.Pages;

public partial class HelpPage : Page
{
    private static readonly Dictionary<string, string> NavigateLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["browse"] = "Go to Browse",
        ["foryou"] = "Go to For You",
        ["tagsets"] = "Go to Tag sets",
        ["tag sets"] = "Go to Tag sets",
        ["library"] = "Go to Library",
        ["local"] = "Go to Local",
        ["downloads"] = "Go to Downloads",
        ["settings"] = "Go to Settings",
        ["account"] = "Go to Account",
    };

    public HelpPage()
    {
        InitializeComponent();
        PageScrollHelper.Attach(this, HelpScroll);
        Loaded += (_, _) =>
        {
            LoadingOverlay.Show();
            BuildTopics();
            LoadingOverlay.Hide();
        };
    }

    private void BuildTopics()
    {
        TopicsPanel.Children.Clear();
        foreach (var topic in HelpCatalog.GetTopics(HelpPlatform.Desktop))
        {
            TopicsPanel.Children.Add(CreateTopicExpander(topic));
        }
    }

    private Expander CreateTopicExpander(HelpTopic topic)
    {
        var expander = new Expander
        {
            Style = (Style)FindResource("R34Expander"),
            IsExpanded = false,
            Margin = new Thickness(0, 0, 0, 10),
            Header = new TextBlock
            {
                Text = topic.Title,
                Foreground = (System.Windows.Media.Brush)FindResource("R34GreenBrush"),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
            },
        };

        var card = new Border
        {
            Style = (Style)FindResource("BrowseSectionCard"),
            Margin = new Thickness(0, 6, 0, 0),
        };

        var body = new StackPanel();

        if (!string.IsNullOrWhiteSpace(topic.Summary))
        {
            body.Children.Add(new TextBlock
            {
                Style = (Style)FindResource("BrowseSectionHint"),
                Text = topic.Summary,
                Margin = new Thickness(0, 0, 0, 10),
            });
        }

        foreach (var bullet in HelpCatalog.GetBullets(topic, HelpPlatform.Desktop))
        {
            var line = new TextBlock
            {
                Style = (Style)FindResource("BodyText"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(14, 2, 0, 2),
            };
            line.Inlines.Add(new Run("•  ")
            {
                Foreground = (System.Windows.Media.Brush)FindResource("MutedBrush"),
            });
            line.Inlines.Add(new Run(bullet));
            body.Children.Add(line);
        }

        if (topic.Link is { Url: { Length: > 0 } url, Label: { Length: > 0 } label })
        {
            var linkBlock = new TextBlock
            {
                Margin = new Thickness(0, 10, 0, 0),
            };
            linkBlock.Inlines.Add(new Run("More: ") { Foreground = (System.Windows.Media.Brush)FindResource("MutedBrush") });
            linkBlock.Inlines.Add(HyperlinkHelper.Create(url, label));
            body.Children.Add(linkBlock);
        }

        var navigateTarget = HelpCatalog.GetNavigateTarget(topic, HelpPlatform.Desktop);
        if (!string.IsNullOrWhiteSpace(navigateTarget))
        {
            var buttonLabel = NavigateLabels.GetValueOrDefault(navigateTarget, "Open");
            var button = new Button
            {
                Content = buttonLabel,
                Style = (Style)FindResource("SecondaryButton"),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 12, 0, 0),
                Padding = new Thickness(14, 8, 14, 8),
                Tag = navigateTarget,
            };
            button.Click += NavigateButton_OnClick;
            body.Children.Add(button);
        }

        card.Child = body;
        expander.Content = card;
        return expander;
    }

    private void NavigateButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string section })
        {
            return;
        }

        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.NavigateToSection(section);
        }
    }
}
