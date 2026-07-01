using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Rule34GalleryApp.Controls;

public partial class GalleryCard : UserControl
{
    public event EventHandler<PostItem>? CardClicked;
    public event EventHandler<PostItem>? FavoriteClicked;
    public event EventHandler<PostItem>? WatchLaterClicked;
    public event EventHandler<PostItem>? AddToListClicked;
    public event EventHandler<PostItem>? DownloadClicked;
    public event EventHandler<PostItem>? ThumbEditClicked;

    public GalleryCard()
    {
        InitializeComponent();
        MouseLeftButtonUp += OnMouseLeftButtonUp;
    }

    private PostItem? Post => DataContext as PostItem;

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Button)
        {
            return;
        }

        if (Post is not null)
        {
            CardClicked?.Invoke(this, Post);
            e.Handled = true;
        }
    }

    private void Card_OnMouseEnter(object sender, MouseEventArgs e)
    {
        ((Storyboard)Resources["HoverIn"]).Begin(this);
        ActionPanel.Opacity = 1;
        LocalActionPanel.Opacity = 1;
        CardBorder.BorderBrush = Application.Current.Resources["R34GreenBrush"] as System.Windows.Media.Brush
            ?? CardBorder.BorderBrush;
    }

    private void Card_OnMouseLeave(object sender, MouseEventArgs e)
    {
        ((Storyboard)Resources["HoverOut"]).Begin(this);
        ActionPanel.Opacity = 0;
        LocalActionPanel.Opacity = 0;
        CardBorder.BorderBrush = Application.Current.Resources["BorderBrush"] as System.Windows.Media.Brush
            ?? CardBorder.BorderBrush;
    }

    private void Favorite_OnClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (Post is not null)
        {
            FavoriteClicked?.Invoke(this, Post);
        }
    }

    private void WatchLater_OnClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (Post is not null)
        {
            WatchLaterClicked?.Invoke(this, Post);
        }
    }

    private void AddToList_OnClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (Post is not null)
        {
            AddToListClicked?.Invoke(this, Post);
        }
    }

    private void Download_OnClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (Post is not null)
        {
            DownloadClicked?.Invoke(this, Post);
        }
    }

    private void ThumbEdit_OnClick(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (Post is not null)
        {
            ThumbEditClicked?.Invoke(this, Post);
        }
    }
}
