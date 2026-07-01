using Rule34Gallery.Core.Help;

namespace Rule34Gallery.Maui.Views;

public partial class HelpPage : ContentPage
{
    private static readonly Dictionary<string, string> NavigateLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["browse"] = "Go to Browse",
        ["ForYou"] = "Go to For You",
        ["foryou"] = "Go to For You",
        ["library"] = "Go to Library",
        ["Library"] = "Go to Library",
        ["local"] = "Go to Local",
        ["Local"] = "Go to Local",
        ["settings"] = "Go to Settings",
        ["Settings"] = "Go to Settings",
        ["account"] = "Go to Account",
        ["Account"] = "Go to Account",
    };

    public HelpPage()
    {
        InitializeComponent();
        LoadingOverlay.IsBusy = true;
        BuildTopics();
        LoadingOverlay.IsBusy = false;
    }

    private void BuildTopics()
    {
        foreach (var topic in HelpCatalog.GetTopics(HelpPlatform.Maui))
        {
            TopicsLayout.Children.Add(CreateTopicExpander(topic));
        }
    }

    private Expander CreateTopicExpander(HelpTopic topic)
    {
        var content = new VerticalStackLayout { Spacing = 6, Padding = new Thickness(0, 6, 0, 0) };

        if (!string.IsNullOrWhiteSpace(topic.Summary))
        {
            content.Children.Add(new Label
            {
                Text = topic.Summary,
                TextColor = Color.FromArgb("#888"),
                FontSize = 12,
                LineHeight = 17,
            });
        }

        foreach (var bullet in HelpCatalog.GetBullets(topic, HelpPlatform.Maui))
        {
            content.Children.Add(new Label
            {
                Text = $"•  {bullet}",
                TextColor = Color.FromArgb("#f2f2f2"),
                FontSize = 13,
                LineHeight = 18,
            });
        }

        if (topic.Link is { Url: { Length: > 0 } url, Label: { Length: > 0 } label })
        {
            var linkButton = new Button
            {
                Text = label,
                BackgroundColor = Color.FromArgb("#1a1a1a"),
                TextColor = Color.FromArgb("#a0e070"),
                Margin = new Thickness(0, 8, 0, 0),
            };
            linkButton.Clicked += async (_, _) =>
            {
                await Launcher.Default.OpenAsync(new Uri(url));
            };
            content.Children.Add(linkButton);
        }

        var navigateTarget = HelpCatalog.GetNavigateTarget(topic, HelpPlatform.Maui);
        if (!string.IsNullOrWhiteSpace(navigateTarget))
        {
            var navButton = new Button
            {
                Text = NavigateLabels.GetValueOrDefault(navigateTarget, "Open"),
                BackgroundColor = Color.FromArgb("#a0e070"),
                TextColor = Color.FromArgb("#0d0d0d"),
                Margin = new Thickness(0, 8, 0, 0),
            };
            navButton.Clicked += async (_, _) =>
            {
                var route = navigateTarget.StartsWith("//", StringComparison.Ordinal)
                    ? navigateTarget
                    : $"//{navigateTarget}";
                await Shell.Current.GoToAsync(route);
            };
            content.Children.Add(navButton);
        }

        return new Expander
        {
            IsExpanded = false,
            Header = new Label
            {
                Text = topic.Title,
                TextColor = Color.FromArgb("#a0e070"),
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
            },
            Content = new Border
            {
                Stroke = Color.FromArgb("#333"),
                StrokeThickness = 1,
                BackgroundColor = Color.FromArgb("#141414"),
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Padding = 14,
                Margin = new Thickness(0, 6, 0, 0),
                Content = content,
            },
        };
    }
}
