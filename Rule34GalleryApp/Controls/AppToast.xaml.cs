using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using Rule34GalleryApp.Helpers;

namespace Rule34GalleryApp.Controls;

public partial class AppToast : UserControl
{
    public AppToast()
    {
        InitializeComponent();
        AppServices.Current.Messenger.ToastRequested += OnToastRequested;
        AppServices.Current.Messenger.ToastDismissed += (_, _) => Hide();
    }

    private void OnToastRequested(object? sender, AppToastMessage message)
    {
        var (background, border, titleBrush) = WpfAppMessengerStyles.GetStyle(message.Kind);
        ToastRoot.Background = background;
        ToastRoot.BorderBrush = border;
        ToastTitle.Foreground = titleBrush;
        ToastTitle.Text = message.Title;
        ToastBody.Inlines.Clear();
        ToastBody.Inlines.Add(new Run(message.Body));

        Visibility = Visibility.Visible;
        ((Storyboard)Resources["SlideIn"]).Begin(this);
    }

    public void Hide() => Visibility = Visibility.Collapsed;

    private void Dismiss_OnClick(object sender, RoutedEventArgs e)
        => AppServices.Current.Messenger.Dismiss();
}
