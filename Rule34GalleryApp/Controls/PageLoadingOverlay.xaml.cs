using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Rule34GalleryApp.Controls;

public partial class PageLoadingOverlay : UserControl
{
    public static readonly DependencyProperty KindProperty =
        DependencyProperty.Register(
            nameof(Kind),
            typeof(PageLoadingKind),
            typeof(PageLoadingOverlay),
            new PropertyMetadata(PageLoadingKind.Browse, OnKindChanged));

    private Storyboard? _storyboard;

    public PageLoadingOverlay()
    {
        InitializeComponent();
        ApplyKind();
    }

    public PageLoadingKind Kind
    {
        get => (PageLoadingKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public void Show(string? message = null)
    {
        MessageText.Text = string.IsNullOrWhiteSpace(message)
            ? PageLoadingAnimations.DefaultMessage(Kind)
            : message;
        Visibility = Visibility.Visible;
        IsHitTestVisible = true;
        StartAnimation();
    }

    public void Hide()
    {
        StopAnimation();
        Visibility = Visibility.Collapsed;
        IsHitTestVisible = false;
    }

    private static void OnKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PageLoadingOverlay overlay)
        {
            overlay.ApplyKind();
            if (overlay.Visibility == Visibility.Visible)
            {
                overlay.StartAnimation();
            }
        }
    }

    private void ApplyKind()
    {
        IconText.Text = PageLoadingAnimations.Glyph(Kind);
        if (Visibility == Visibility.Visible && string.IsNullOrWhiteSpace(MessageText.Text))
        {
            MessageText.Text = PageLoadingAnimations.DefaultMessage(Kind);
        }
    }

    private void StartAnimation()
    {
        StopAnimation();
        _storyboard = PageLoadingAnimations.Create(Kind, IconText);
        _storyboard.Begin();
    }

    private void StopAnimation()
    {
        if (_storyboard is null)
        {
            return;
        }

        _storyboard.Stop();
        _storyboard = null;
        IconText.RenderTransform = null;
    }
}
