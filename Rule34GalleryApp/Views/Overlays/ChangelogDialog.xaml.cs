using System.Windows;
using System.Windows.Controls;
using Rule34GalleryApp.Services;

namespace Rule34GalleryApp.Views.Overlays;

public partial class ChangelogDialog : UserControl
{
    public ChangelogDialog()
    {
        InitializeComponent();
    }

    public void Show(string changelogText)
    {
        ChangelogViewer.Document = ChangelogDocumentBuilder.Build(
            string.IsNullOrWhiteSpace(changelogText) ? "No changelog available." : changelogText);
        Visibility = Visibility.Visible;
    }

    public void Hide() => Visibility = Visibility.Collapsed;

    private void CloseButton_OnClick(object sender, RoutedEventArgs e) => Hide();
}
