using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Rule34Gallery.Core.Firebase;
using Rule34Gallery.Core.Services;

namespace Rule34GalleryApp.Services;

public static class GalleryActions
{
    public static bool EnsureSignedIn(Window owner)
    {
        if (AppServices.Current.Library.IsSignedIn)
        {
            return true;
        }

        AppServices.Current.Messenger.Show(
            "Sign in required",
            "Sign in on the Account page to use favorites and lists.",
            AppMessageKind.Info);
        return false;
    }

    public static async Task ToggleFavoriteAsync(PostItem post)
    {
        if (!AppServices.Current.Library.IsSignedIn)
        {
            return;
        }

        try
        {
            await AppServices.Current.Library.ToggleFavoriteAsync(post);
            post.IsFavorite = AppServices.Current.Library.FavoriteIds.Contains(post.Id);
            var gallery = AppServices.Current.Gallery;
            if (gallery.ViewMode == GalleryViewMode.Favorites && !post.IsFavorite)
            {
                gallery.RemovePost(post);
                gallery.NotifyStatus($"Favorites | {AppServices.Current.Posts.Count} posts");
            }
        }
        catch (Exception ex)
        {
            AppServices.Current.Messenger.Show("Favorite error", ex.Message, AppMessageKind.Warning);
        }
    }

    public static async Task<CloudMetadataRefreshResult> RefreshFavoritesMetadataAsync()
    {
        if (!AppServices.Current.Library.IsSignedIn)
        {
            throw new InvalidOperationException("Sign in to refresh metadata.");
        }

        return await AppServices.Current.Library.RefreshFavoritesMetadataAsync();
    }

    public static async Task<CloudMetadataRefreshResult> RefreshListMetadataAsync(string listId)
    {
        if (!AppServices.Current.Library.IsSignedIn)
        {
            throw new InvalidOperationException("Sign in to refresh metadata.");
        }

        return await AppServices.Current.Library.RefreshListMetadataAsync(listId);
    }

    public static async Task RefreshPostMetadataAsync(PostItem post)
    {
        if (!AppServices.Current.Library.IsSignedIn)
        {
            throw new InvalidOperationException("Sign in to refresh metadata.");
        }

        await AppServices.Current.Library.RefreshPostMetadataAsync(post);
    }

    public static async Task ToggleWatchLaterAsync(PostItem post)
    {
        if (!AppServices.Current.Library.IsSignedIn)
        {
            return;
        }

        try
        {
            await AppServices.Current.Library.ToggleWatchLaterAsync(post);
            post.IsInWatchLater = AppServices.Current.Library.WatchLaterIds.Contains(post.Id);
            var gallery = AppServices.Current.Gallery;
            if (gallery.ViewMode == GalleryViewMode.List &&
                gallery.SelectedListId == SavedList.WatchLaterId &&
                !post.IsInWatchLater)
            {
                gallery.RemovePost(post);
                gallery.NotifyStatus($"Watch Later | {AppServices.Current.Posts.Count} posts");
            }
        }
        catch (Exception ex)
        {
            AppServices.Current.Messenger.Show("Watch Later error", ex.Message, AppMessageKind.Warning);
        }
    }

    public static async Task PromptAddToListAsync(Window owner, PostItem post)
    {
        if (!EnsureSignedIn(owner))
        {
            return;
        }

        try
        {
            var lists = AppServices.Current.Library.UserLists();
            if (lists.Count == 0)
            {
                await AppServices.Current.Library.RefreshListsAsync().ConfigureAwait(false);
                lists = AppServices.Current.Library.UserLists();
            }

            if (lists.Count == 0)
            {
                AppServices.Current.Messenger.Show(
                    "No lists",
                    "Create a list on the Library page first.",
                    AppMessageKind.Info);
                return;
            }

            if (lists.Count == 1)
            {
                await AppServices.Current.Library.AddToListAsync(lists[0].Id, post);
                AppServices.Current.Messenger.Show("Added to list", $"Added to \"{lists[0].Name}\".", AppMessageKind.Info);
                return;
            }

            var dialog = new Window
            {
                Title = "Add to list",
                Width = 380,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = owner,
                Background = Application.Current.Resources["SurfaceRaisedBrush"] as Brush,
                ResizeMode = ResizeMode.NoResize,
            };

            var combo = new ComboBox
            {
                ItemsSource = lists,
                DisplayMemberPath = nameof(SavedList.Name),
                Margin = new Thickness(16, 8, 16, 8),
                SelectedIndex = 0,
            };
            var addButton = new Button
            {
                Content = "Add",
                Style = Application.Current.Resources["PrimaryButton"] as Style,
                Margin = new Thickness(16, 0, 8, 16),
                Padding = new Thickness(12, 6, 12, 6),
                IsDefault = true,
            };
            var cancelButton = new Button
            {
                Content = "Cancel",
                Style = Application.Current.Resources["SecondaryButton"] as Style,
                Margin = new Thickness(0, 0, 16, 16),
                Padding = new Thickness(12, 6, 12, 6),
            };

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            buttons.Children.Add(addButton);
            buttons.Children.Add(cancelButton);

            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = "Choose a list:",
                Margin = new Thickness(16, 16, 16, 0),
                Foreground = Application.Current.Resources["TextBrush"] as Brush,
            });
            panel.Children.Add(combo);
            panel.Children.Add(buttons);
            dialog.Content = panel;

            addButton.Click += (_, _) => { dialog.DialogResult = true; dialog.Close(); };
            cancelButton.Click += (_, _) => { dialog.DialogResult = false; dialog.Close(); };

            if (dialog.ShowDialog() != true || combo.SelectedItem is not SavedList selected)
            {
                return;
            }

            await AppServices.Current.Library.AddToListAsync(selected.Id, post);
            var gallery = AppServices.Current.Gallery;
            if (gallery.ViewMode == GalleryViewMode.List && gallery.SelectedListId == selected.Id)
            {
                if (!AppServices.Current.Posts.Any(p => p.Id == post.Id))
                {
                    AppServices.Current.Posts.Add(post);
                    AppServices.Current.Library.ApplyCloudLibraryState(AppServices.Current.Posts);
                }
            }

            AppServices.Current.Messenger.Show("Added to list", $"Added to \"{selected.Name}\".", AppMessageKind.Info);
        }
        catch (Exception ex)
        {
            AppServices.Current.Messenger.Show("Add to list failed", ex.Message, AppMessageKind.Warning);
        }
    }
}
