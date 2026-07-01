namespace Rule34Gallery.Maui.Controls;

public partial class PageLoadingOverlay : ContentView
{
    public static readonly BindableProperty KindProperty =
        BindableProperty.Create(
            nameof(Kind),
            typeof(PageLoadingKind),
            typeof(PageLoadingOverlay),
            PageLoadingKind.Browse,
            propertyChanged: OnKindChanged);

    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create(
            nameof(Message),
            typeof(string),
            typeof(PageLoadingOverlay),
            null,
            propertyChanged: OnMessageChanged);

    public static readonly BindableProperty IsBusyProperty =
        BindableProperty.Create(
            nameof(IsBusy),
            typeof(bool),
            typeof(PageLoadingOverlay),
            false,
            propertyChanged: OnIsBusyChanged);

    private CancellationTokenSource? _animationCts;

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

    public string? Message
    {
        get => (string?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public bool IsBusy
    {
        get => (bool)GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    private static void OnKindChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PageLoadingOverlay overlay)
        {
            overlay.ApplyKind();
            if (overlay.IsBusy)
            {
                overlay.RestartAnimation();
            }
        }
    }

    private static void OnMessageChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PageLoadingOverlay overlay)
        {
            overlay.ApplyMessage();
        }
    }

    private static void OnIsBusyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not PageLoadingOverlay overlay)
        {
            return;
        }

        var busy = (bool)newValue;
        overlay.IsVisible = busy;
        overlay.InputTransparent = !busy;
        if (busy)
        {
            overlay.ApplyMessage();
            overlay.RestartAnimation();
        }
        else
        {
            overlay.StopAnimation();
        }
    }

    private void ApplyKind()
    {
        IconLabel.Text = PageLoadingAnimations.Glyph(Kind);
    }

    private void ApplyMessage()
    {
        MessageLabel.Text = string.IsNullOrWhiteSpace(Message)
            ? PageLoadingAnimations.DefaultMessage(Kind)
            : Message;
    }

    private void RestartAnimation()
    {
        StopAnimation();
        _animationCts = new CancellationTokenSource();
        _ = RunAnimationLoopAsync(_animationCts.Token);
    }

    private void StopAnimation()
    {
        _animationCts?.Cancel();
        _animationCts?.Dispose();
        _animationCts = null;
        IconLabel.AbortAnimation("PageLoadingScale");
        IconLabel.AbortAnimation("PageLoadingRotate");
        IconLabel.AbortAnimation("PageLoadingBounce");
        IconLabel.Scale = 1;
        IconLabel.Rotation = 0;
        IconLabel.TranslationY = 0;
    }

    private async Task RunAnimationLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && IsBusy)
        {
            try
            {
                switch (Kind)
                {
                    case PageLoadingKind.Browse:
                        await AnimatePulseAsync(1.0, 1.18, 550, cancellationToken);
                        await AnimateRotateAsync(0, 18, 900, cancellationToken);
                        break;
                    case PageLoadingKind.ForYou:
                        await AnimateRotateAsync(0, 360, 1400, cancellationToken);
                        await AnimatePulseAsync(0.92, 1.08, 700, cancellationToken);
                        break;
                    case PageLoadingKind.SavedTags:
                        await AnimateBounceAsync(0, -10, 450, cancellationToken);
                        await AnimatePulseAsync(0.95, 1.1, 450, cancellationToken);
                        break;
                    case PageLoadingKind.Library:
                        await AnimatePulseAsync(0.88, 1.14, 550, cancellationToken);
                        break;
                    case PageLoadingKind.LocalLibrary:
                        await AnimateBounceAsync(0, -8, 500, cancellationToken);
                        break;
                    case PageLoadingKind.Downloads:
                        await AnimateBounceAsync(-6, 8, 350, cancellationToken);
                        break;
                    case PageLoadingKind.Sync:
                    case PageLoadingKind.Settings:
                        await AnimateRotateAsync(0, 360, 1100, cancellationToken);
                        break;
                    case PageLoadingKind.Help:
                        await AnimatePulseAsync(0.94, 1.06, 650, cancellationToken);
                        break;
                    case PageLoadingKind.Account:
                        await AnimatePulseAsync(0.9, 1.12, 750, cancellationToken);
                        break;
                    default:
                        await AnimatePulseAsync(0.95, 1.05, 600, cancellationToken);
                        break;
                }
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task AnimatePulseAsync(double from, double to, uint length, CancellationToken cancellationToken)
    {
        IconLabel.Scale = from;
        await IconLabel.ScaleTo(to, length, Easing.SinInOut);
        cancellationToken.ThrowIfCancellationRequested();
        await IconLabel.ScaleTo(from, length, Easing.SinInOut);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task AnimateRotateAsync(double from, double to, uint length, CancellationToken cancellationToken)
    {
        IconLabel.Rotation = from;
        await IconLabel.RotateTo(to, length, Easing.Linear);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task AnimateBounceAsync(double from, double to, uint length, CancellationToken cancellationToken)
    {
        IconLabel.TranslationY = from;
        await IconLabel.TranslateTo(0, to, length, 0, Easing.SinInOut);
        cancellationToken.ThrowIfCancellationRequested();
        await IconLabel.TranslateTo(0, from, length, 0, Easing.SinInOut);
        cancellationToken.ThrowIfCancellationRequested();
    }
}
