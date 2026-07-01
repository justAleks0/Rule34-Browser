using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace Rule34GalleryApp.Helpers;

internal static class PageScrollHelper
{
    public static void Attach(Page page, ScrollViewer scrollViewer, double chromePadding = 16)
    {
        void ApplyBounds()
        {
            var height = GetAvailableContentHeight(page, chromePadding);
            if (height > 100)
            {
                scrollViewer.MaxHeight = height;
            }
        }

        page.Loaded += (_, _) =>
        {
            ApplyBounds();
            page.Dispatcher.BeginInvoke(ApplyBounds, System.Windows.Threading.DispatcherPriority.Loaded);
        };

        page.SizeChanged += (_, _) => ApplyBounds();

        if (Window.GetWindow(page) is Window window)
        {
            window.SizeChanged += (_, _) => ApplyBounds();
        }

        page.PreviewMouseWheel += (_, e) => ForwardMouseWheel(scrollViewer, e);
    }

    private static double GetAvailableContentHeight(Page page, double padding)
    {
        DependencyObject? current = VisualTreeHelper.GetParent(page);
        while (current is not null)
        {
            if (current is NavigationView nav && nav.ActualHeight > padding)
            {
                return nav.ActualHeight - padding;
            }

            if (current is FrameworkElement fe &&
                fe.ActualHeight > 200 &&
                fe.ActualHeight < 10000 &&
                fe is not Page)
            {
                return fe.ActualHeight - padding;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        var navFromWindow = FindDescendant<NavigationView>(Window.GetWindow(page));
        if (navFromWindow?.ActualHeight > padding)
        {
            return navFromWindow.ActualHeight - padding;
        }

        if (Window.GetWindow(page) is Window window && window.ActualHeight > 120)
        {
            return window.ActualHeight - 96;
        }

        return 0;
    }

    private static void ForwardMouseWheel(ScrollViewer scrollViewer, MouseWheelEventArgs e)
    {
        scrollViewer.UpdateLayout();
        if (scrollViewer.ScrollableHeight <= 0)
        {
            return;
        }

        if (e.OriginalSource is DependencyObject source &&
            FindAncestorScrollViewer(source) is { } nested &&
            nested != scrollViewer &&
            nested.ScrollableHeight > 0)
        {
            return;
        }

        var next = scrollViewer.VerticalOffset - e.Delta;
        scrollViewer.ScrollToVerticalOffset(Math.Clamp(next, 0, scrollViewer.ScrollableHeight));
        e.Handled = true;
    }

    private static ScrollViewer? FindAncestorScrollViewer(DependencyObject? node)
    {
        while (node is not null)
        {
            if (node is ScrollViewer scrollViewer)
            {
                return scrollViewer;
            }

            node = VisualTreeHelper.GetParent(node);
        }

        return null;
    }

    private static T? FindDescendant<T>(DependencyObject? root)
        where T : DependencyObject
    {
        if (root is null)
        {
            return null;
        }

        if (root is T match)
        {
            return match;
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            var found = FindDescendant<T>(child);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }
}
