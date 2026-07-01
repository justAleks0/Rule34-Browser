using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace Rule34GalleryApp.Controls;

public partial class GalleryGrid : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(GalleryGrid),
            new PropertyMetadata(null, OnItemsSourceChanged));

    private Window? _hostWindow;
    private double _reservedBottom;
    private bool _viewportUpdateScheduled;

    public event EventHandler<PostItem>? CardClicked;
    public event EventHandler<PostItem>? FavoriteClicked;
    public event EventHandler<PostItem>? WatchLaterClicked;
    public event EventHandler<PostItem>? AddToListClicked;
    public event EventHandler<PostItem>? DownloadClicked;
    public event EventHandler<PostItem>? ThumbEditClicked;

    public GalleryGrid()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += (_, _) => UpdateScrollViewport();
        LayoutUpdated += OnLayoutUpdated;
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public void SetEmptyMessage(string title, string subtitle)
    {
        EmptyTitleText.Text = title;
        EmptySubtitleText.Text = subtitle;
        UpdateEmptyState();
    }

    public void SetReservedBottomHeight(double pixels)
    {
        _reservedBottom = Math.Max(0, pixels);
        UpdateScrollViewport();
    }

    public void RefreshViewport() => UpdateScrollViewport();

    public void UpdateEmptyState()
    {
        var count = 0;
        if (GalleryItems.ItemsSource is ICollection collection)
        {
            count = collection.Count;
        }

        EmptyPanel.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GalleryGrid grid)
        {
            grid.GalleryItems.ItemsSource = e.NewValue as IEnumerable;
            grid.UpdateEmptyState();
            grid.Dispatcher.BeginInvoke(grid.UpdateScrollViewport);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateScrollViewport();
        _hostWindow = Window.GetWindow(this);
        if (_hostWindow is not null)
        {
            _hostWindow.SizeChanged += HostWindow_OnSizeChanged;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_hostWindow is not null)
        {
            _hostWindow.SizeChanged -= HostWindow_OnSizeChanged;
            _hostWindow = null;
        }
    }

    private void HostWindow_OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateScrollViewport();

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_viewportUpdateScheduled || !IsLoaded)
        {
            return;
        }

        _viewportUpdateScheduled = true;
        Dispatcher.BeginInvoke(() =>
        {
            _viewportUpdateScheduled = false;
            UpdateScrollViewport();
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void UpdateScrollViewport()
    {
        if (!IsLoaded || !IsVisible)
        {
            return;
        }

        var available = ResolveAvailableViewportHeight();
        if (available < 120)
        {
            return;
        }

        // Only constrain the scroll viewer — capping the whole control caused a single
        // content-sized row to float vertically in the page.
        ClearValue(MaxHeightProperty);
        GalleryScroll.MaxHeight = available;
        GalleryScroll.Height = available;
    }

    private double ResolveAvailableViewportHeight()
    {
        var window = _hostWindow ?? Window.GetWindow(this);
        if (window is not null)
        {
            try
            {
                var topLeft = TransformToAncestor(window).Transform(new Point(0, 0));
                var bottomMargin = 16.0 + _reservedBottom;
                var fromWindow = window.ActualHeight - topLeft.Y - bottomMargin;
                if (fromWindow >= 120)
                {
                    return fromWindow;
                }
            }
            catch (InvalidOperationException)
            {
                // Transform not ready yet.
            }
        }

        return ActualHeight >= 120 ? ActualHeight : 120;
    }

    private void GalleryGrid_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (GalleryScroll.ScrollableHeight <= 0)
        {
            UpdateScrollViewport();
            GalleryScroll.UpdateLayout();
        }

        if (GalleryScroll.ScrollableHeight <= 0)
        {
            return;
        }

        var next = GalleryScroll.VerticalOffset - e.Delta;
        next = Math.Clamp(next, 0, GalleryScroll.ScrollableHeight);
        if (Math.Abs(next - GalleryScroll.VerticalOffset) < 0.5)
        {
            return;
        }

        GalleryScroll.ScrollToVerticalOffset(next);
        e.Handled = true;
    }

    private void GalleryCard_OnCardClicked(object? sender, PostItem post) => CardClicked?.Invoke(this, post);

    private void GalleryCard_OnFavoriteClicked(object? sender, PostItem post) => FavoriteClicked?.Invoke(this, post);

    private void GalleryCard_OnWatchLaterClicked(object? sender, PostItem post) => WatchLaterClicked?.Invoke(this, post);

    private void GalleryCard_OnAddToListClicked(object? sender, PostItem post) => AddToListClicked?.Invoke(this, post);

    private void GalleryCard_OnDownloadClicked(object? sender, PostItem post) => DownloadClicked?.Invoke(this, post);

    private void GalleryCard_OnThumbEditClicked(object? sender, PostItem post) => ThumbEditClicked?.Invoke(this, post);

    public void RefreshThumbnailsForPath(string filePath)
    {
        foreach (var card in FindVisualChildren<GalleryCard>(this))
        {
            if (card.DataContext is PostItem post &&
                string.Equals(post.FileUrl, filePath, StringComparison.OrdinalIgnoreCase))
            {
                var image = FindVisualChild<Image>(card);
                if (image is not null)
                {
                    ImageLoader.Reload(image);
                }
            }
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
        where T : DependencyObject
    {
        if (parent is null)
        {
            yield break;
        }

        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var nested in FindVisualChildren<T>(child))
            {
                yield return nested;
            }
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent)
        where T : DependencyObject
        => FindVisualChildren<T>(parent).FirstOrDefault();
}
