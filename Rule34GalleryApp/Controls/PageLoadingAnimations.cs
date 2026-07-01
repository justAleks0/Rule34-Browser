using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Rule34GalleryApp.Controls;

internal static class PageLoadingAnimations
{
    public static string Glyph(PageLoadingKind kind) => kind switch
    {
        PageLoadingKind.Browse => "⌕",
        PageLoadingKind.ForYou => "✦",
        PageLoadingKind.SavedTags => "🏷",
        PageLoadingKind.Library => "♥",
        PageLoadingKind.LocalLibrary => "▤",
        PageLoadingKind.Downloads => "↓",
        PageLoadingKind.Sync => "↻",
        PageLoadingKind.Help => "📖",
        PageLoadingKind.Settings => "⚙",
        PageLoadingKind.Account => "◎",
        _ => "…",
    };

    public static string DefaultMessage(PageLoadingKind kind) => kind switch
    {
        PageLoadingKind.Browse => "Searching…",
        PageLoadingKind.ForYou => "Building your feed…",
        PageLoadingKind.SavedTags => "Loading tag sets…",
        PageLoadingKind.Library => "Loading library…",
        PageLoadingKind.LocalLibrary => "Scanning folders…",
        PageLoadingKind.Downloads => "Downloading…",
        PageLoadingKind.Sync => "Syncing…",
        PageLoadingKind.Help => "Loading help…",
        PageLoadingKind.Settings => "Loading settings…",
        PageLoadingKind.Account => "Signing in…",
        _ => "Loading…",
    };

    public static Storyboard Create(PageLoadingKind kind, UIElement target)
    {
        var transformGroup = new TransformGroup();
        var scale = new ScaleTransform(1, 1);
        var rotate = new RotateTransform(0);
        var translate = new TranslateTransform(0, 0);
        transformGroup.Children.Add(scale);
        transformGroup.Children.Add(rotate);
        transformGroup.Children.Add(translate);
        target.RenderTransform = transformGroup;
        target.RenderTransformOrigin = new Point(0.5, 0.5);

        var storyboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };

        switch (kind)
        {
            case PageLoadingKind.Browse:
                AddPulse(storyboard, scale, 1.0, 1.18, 0.55);
                AddRotate(storyboard, rotate, 0, 18, 0.9);
                break;
            case PageLoadingKind.ForYou:
                AddRotate(storyboard, rotate, 0, 360, 1.4);
                AddPulse(storyboard, scale, 0.92, 1.08, 0.7);
                break;
            case PageLoadingKind.SavedTags:
                AddBounce(storyboard, translate, 0, -10, 0.45);
                AddPulse(storyboard, scale, 0.95, 1.1, 0.45);
                break;
            case PageLoadingKind.Library:
                AddPulse(storyboard, scale, 0.88, 1.14, 0.55);
                break;
            case PageLoadingKind.LocalLibrary:
                AddBounce(storyboard, translate, 0, -8, 0.5);
                break;
            case PageLoadingKind.Downloads:
                AddBounce(storyboard, translate, -6, 8, 0.35);
                break;
            case PageLoadingKind.Sync:
            case PageLoadingKind.Settings:
                AddRotate(storyboard, rotate, 0, 360, 1.1);
                break;
            case PageLoadingKind.Help:
                AddPulse(storyboard, scale, 0.94, 1.06, 0.65);
                break;
            case PageLoadingKind.Account:
                AddPulse(storyboard, scale, 0.9, 1.12, 0.75);
                break;
            default:
                AddPulse(storyboard, scale, 0.95, 1.05, 0.6);
                break;
        }

        return storyboard;
    }

    private static void AddPulse(Storyboard storyboard, ScaleTransform scale, double from, double to, double seconds)
    {
        var animX = new DoubleAnimation(from, to, TimeSpan.FromSeconds(seconds))
        {
            AutoReverse = true,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
        };
        var animY = new DoubleAnimation(from, to, TimeSpan.FromSeconds(seconds))
        {
            AutoReverse = true,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
        };
        Storyboard.SetTarget(animX, scale);
        Storyboard.SetTargetProperty(animX, new PropertyPath(ScaleTransform.ScaleXProperty));
        Storyboard.SetTarget(animY, scale);
        Storyboard.SetTargetProperty(animY, new PropertyPath(ScaleTransform.ScaleYProperty));
        storyboard.Children.Add(animX);
        storyboard.Children.Add(animY);
    }

    private static void AddRotate(Storyboard storyboard, RotateTransform rotate, double from, double to, double seconds)
    {
        var anim = new DoubleAnimation(from, to, TimeSpan.FromSeconds(seconds));
        Storyboard.SetTarget(anim, rotate);
        Storyboard.SetTargetProperty(anim, new PropertyPath(RotateTransform.AngleProperty));
        storyboard.Children.Add(anim);
    }

    private static void AddBounce(Storyboard storyboard, TranslateTransform translate, double from, double to, double seconds)
    {
        var anim = new DoubleAnimation(from, to, TimeSpan.FromSeconds(seconds))
        {
            AutoReverse = true,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
        };
        Storyboard.SetTarget(anim, translate);
        Storyboard.SetTargetProperty(anim, new PropertyPath(TranslateTransform.YProperty));
        storyboard.Children.Add(anim);
    }
}
