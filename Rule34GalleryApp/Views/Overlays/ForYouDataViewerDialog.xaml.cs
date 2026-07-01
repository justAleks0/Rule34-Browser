using System.Windows;
using System.Windows.Controls;

namespace Rule34GalleryApp.Views.Overlays;

public partial class ForYouDataViewerDialog : UserControl
{
    public ForYouDataViewerDialog()
    {
        InitializeComponent();
    }

    public void Show(string text)
    {
        DataText.Text = string.IsNullOrWhiteSpace(text) ? "No profile data yet." : text;
        Visibility = Visibility.Visible;
    }

    public void Hide() => Visibility = Visibility.Collapsed;

    private void CloseButton_OnClick(object sender, RoutedEventArgs e) => Hide();
}
