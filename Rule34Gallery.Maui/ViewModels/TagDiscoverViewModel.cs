using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rule34Gallery.Core;
using Rule34Gallery.Core.Services;

namespace Rule34Gallery.Maui.ViewModels;

public partial class TagDiscoverViewModel : ObservableObject
{
    private readonly AppServices _app = AppServices.Current;

    public IReadOnlyList<string> ModeLabels { get; } = ["By theme", "Find tag name", "Forgot the name?"];

    [ObservableProperty]
    private int _selectedModeIndex;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _resultsText = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    private DescribeDiscoveryMode SelectedMode =>
        SelectedModeIndex switch
        {
            1 => DescribeDiscoveryMode.ConceptLookup,
            2 => DescribeDiscoveryMode.IntentSearch,
            _ => DescribeDiscoveryMode.Theme,
        };

    [RelayCommand]
    private async Task DiscoverAsync()
    {
        if (string.IsNullOrWhiteSpace(Description))
        {
            return;
        }

        IsBusy = true;
        try
        {
            if (SelectedMode == DescribeDiscoveryMode.IntentSearch)
            {
                var memory = await TagRecommendationService.DiscoverAsync(
                    _app.Http,
                    Description,
                    _app.Settings,
                    DescribeDiscoveryMode.IntentSearch);
                ResultsText = memory.SearchHits.Count > 0
                    ? string.Join(
                        "\n\n",
                        memory.SearchHits.Select(h =>
                            $"{h.Title} ({h.ConfidencePercent}%)\n{h.Subtitle}\n{h.Snippet}"))
                    : memory.InterpretationSummary;
                return;
            }

            var result = await OpenAiTagDiscoveryService.SuggestAsync(
                _app.Http,
                _app.Settings.OpenAiApiKey,
                _app.Settings.OpenAiModel,
                Description,
                SelectedMode);
            ResultsText = string.Join(", ", result.Combinations.SelectMany(c => c.Tags));
        }
        catch (Exception ex)
        {
            ResultsText = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ApplyTagsAsync()
    {
        foreach (var tag in ResultsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            _app.Settings.AddIncludeTag(tag);
        }

        await _app.Gallery.SearchAsync(resetPage: true);
        await Shell.Current.GoToAsync("//Browse");
    }
}
