using System.Windows.Controls;

namespace Rule34GalleryApp.Helpers;

internal static class TagAutocompleteWpf
{
    public static void ApplySuggestionToInput(TextBox input, TagSuggestion selected)
    {
        input.Text = TagAutocompleteService.ApplySuggestionToText(input.Text, selected);
        input.CaretIndex = input.Text.Length;
    }
}
