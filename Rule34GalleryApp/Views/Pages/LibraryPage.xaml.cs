using System.Windows;
using System.Windows.Controls;
using Rule34Gallery.Core;
using Rule34Gallery.Core.Firebase;
using Rule34Gallery.Core.Remote;
using Rule34GalleryApp.Controls;
using Rule34GalleryApp.Services;

namespace Rule34GalleryApp.Views.Pages;

public partial class LibraryPage : Page
{
    private readonly AppServices _app = AppServices.Current;
    private LibraryTab _activeTab = LibraryTab.Favorites;
    private string? _activeListId;

    private enum LibraryTab
    {
        Favorites,
        WatchLater,
        Lists,
    }

    public LibraryPage()
    {
        InitializeComponent();
        Gallery.ItemsSource = _app.Posts;
        Gallery.SetEmptyMessage("Your library is empty", "Sign in and save favorites or create lists.");
        _app.Gallery.StatusChanged += (_, status) =>
            Dispatcher.Invoke(() => BindGalleryStatus(status));
    }

    private void BindGalleryStatus(string status)
    {
        if (!IsVisible)
        {
            return;
        }

        if (PageLoadingHelper.IsBusyStatus(status))
        {
            LoadingOverlay.Show(status);
        }
        else
        {
            LoadingOverlay.Hide();
        }
    }

    public async Task RefreshListsAsync()
    {
        if (!_app.Library.IsSignedIn)
        {
            UserListsBox.ItemsSource = null;
            return;
        }

        try
        {
            await _app.Library.RefreshListsAsync();
            UserListsBox.ItemsSource = _app.Library.UserLists();
        }
        catch (Exception ex)
        {
            _app.Messenger.Show("Lists error", ex.Message, AppMessageKind.Warning);
        }
    }

    public void SetSignedInState(bool signedIn)
    {
        if (!signedIn)
        {
            ListsPanel.Visibility = Visibility.Collapsed;
        }

        if (!signedIn)
        {
            Gallery.SetEmptyMessage(
                "Sign in to use your library",
                "Go to Account and sign in to sync favorites and lists.");
        }
    }

    private void SelectTab(LibraryTab tab)
    {
        FavoritesTab.Style = (Style)FindResource(tab == LibraryTab.Favorites ? "PrimaryButton" : "SecondaryButton");
        WatchLaterTab.Style = (Style)FindResource(tab == LibraryTab.WatchLater ? "PrimaryButton" : "SecondaryButton");
        ListsTab.Style = (Style)FindResource(tab == LibraryTab.Lists ? "PrimaryButton" : "SecondaryButton");
        ListsPanel.Visibility = tab == LibraryTab.Lists ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void FavoritesTab_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_app.Library.IsSignedIn)
        {
            _app.Messenger.Show("Sign in required", "Sign in on the Account page first.", AppMessageKind.Info);
            return;
        }

        _activeTab = LibraryTab.Favorites;
        _activeListId = null;
        SelectTab(LibraryTab.Favorites);
        await _app.Gallery.LoadFavoritesAsync();
        Gallery.UpdateEmptyState();
    }

    private async void WatchLaterTab_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_app.Library.IsSignedIn)
        {
            _app.Messenger.Show("Sign in required", "Sign in on the Account page first.", AppMessageKind.Info);
            return;
        }

        _activeTab = LibraryTab.WatchLater;
        _activeListId = SavedList.WatchLaterId;
        SelectTab(LibraryTab.WatchLater);
        await _app.Gallery.LoadWatchLaterAsync();
        Gallery.UpdateEmptyState();
    }

    private void ListsTab_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_app.Library.IsSignedIn)
        {
            _app.Messenger.Show("Sign in required", "Sign in on the Account page first.", AppMessageKind.Info);
            return;
        }

        _activeTab = LibraryTab.Lists;
        _activeListId = null;
        SelectTab(LibraryTab.Lists);
        _ = RefreshListsAsync();
    }

    private async void RefreshMetadataButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!GalleryActions.EnsureSignedIn(Window.GetWindow(this)!))
        {
            return;
        }

        if (_app.Posts.Count == 0)
        {
            _app.Messenger.Show("Nothing to refresh", "Load favorites or a list first.", AppMessageKind.Info);
            return;
        }

        try
        {
            RefreshMetadataButton.IsEnabled = false;
            LoadingOverlay.Show("Refreshing metadata…");
            CloudMetadataRefreshResult result = _activeTab switch
            {
                LibraryTab.Favorites => await GalleryActions.RefreshFavoritesMetadataAsync(),
                LibraryTab.WatchLater => await GalleryActions.RefreshListMetadataAsync(SavedList.WatchLaterId),
                LibraryTab.Lists when UserListsBox.SelectedItem is SavedList list =>
                    await GalleryActions.RefreshListMetadataAsync(list.Id),
                _ => throw new InvalidOperationException("Select a list to refresh, or open Favorites / Watch Later."),
            };

            switch (_activeTab)
            {
                case LibraryTab.Favorites:
                    await _app.Gallery.LoadFavoritesAsync();
                    break;
                case LibraryTab.WatchLater:
                    await _app.Gallery.LoadWatchLaterAsync();
                    break;
                case LibraryTab.Lists when UserListsBox.SelectedItem is SavedList list:
                    await _app.Gallery.LoadListAsync(list.Id, list.Name);
                    break;
            }

            Gallery.UpdateEmptyState();
            _app.Messenger.Show("Metadata refreshed", result.Summary, AppMessageKind.Info);
        }
        catch (Exception ex)
        {
            _app.Messenger.Show("Refresh failed", ex.Message, AppMessageKind.Warning);
        }
        finally
        {
            LoadingOverlay.Hide();
            RefreshMetadataButton.IsEnabled = true;
        }
    }

    private async void CreateListButton_OnClick(object sender, RoutedEventArgs e)
    {
        var name = NewListNameInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        try
        {
            await _app.Library.CreateListAsync(name);
            NewListNameInput.Text = string.Empty;
            await RefreshListsAsync();
        }
        catch (Exception ex)
        {
            _app.Messenger.Show("Create list failed", ex.Message, AppMessageKind.Warning);
        }
    }

    private async void UserListsBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UserListsBox.SelectedItem is not SavedList list)
        {
            DeleteListButton.Visibility = Visibility.Collapsed;
            return;
        }

        DeleteListButton.Visibility = Visibility.Visible;
        _activeListId = list.Id;
        await _app.Gallery.LoadListAsync(list.Id, list.Name);
        Gallery.UpdateEmptyState();
    }

    private async void DeleteListButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (UserListsBox.SelectedItem is not SavedList list)
        {
            return;
        }

        try
        {
            await _app.Library.DeleteListAsync(list.Id);
            await RefreshListsAsync();
            _app.Gallery.ResetToSearchView();
            _app.Posts.Clear();
            Gallery.UpdateEmptyState();
        }
        catch (Exception ex)
        {
            _app.Messenger.Show("Delete list failed", ex.Message, AppMessageKind.Warning);
        }
    }

    private void Gallery_OnCardClicked(object? sender, PostItem post)
    {
        if (Window.GetWindow(this) is MainWindow main)
        {
            var index = _app.Posts.IndexOf(post);
            if (index >= 0)
            {
                main.OpenViewer(index);
            }
        }
    }

    private async void Gallery_OnFavoriteClicked(object? sender, PostItem post)
    {
        if (!GalleryActions.EnsureSignedIn(Window.GetWindow(this)!))
        {
            return;
        }

        await GalleryActions.ToggleFavoriteAsync(post);
        Gallery.UpdateEmptyState();
    }

    private async void Gallery_OnWatchLaterClicked(object? sender, PostItem post)
    {
        if (!GalleryActions.EnsureSignedIn(Window.GetWindow(this)!))
        {
            return;
        }

        await GalleryActions.ToggleWatchLaterAsync(post);
        Gallery.UpdateEmptyState();
    }

    private async void Gallery_OnAddToListClicked(object? sender, PostItem post)
        => await GalleryActions.PromptAddToListAsync(Window.GetWindow(this)!, post);

    private void Gallery_OnDownloadClicked(object? sender, PostItem post)
    {
        if (Window.GetWindow(this) is MainWindow main)
        {
            main.EnqueueDownload(post);
        }
    }

    public void OnPostsChanged() => Gallery.UpdateEmptyState();

    public RemoteLibraryState CaptureRemoteState()
    {
        var lists = _app.Library.IsSignedIn
            ? _app.Library.UserLists()
                .Select(l => new RemoteListSummary { Id = l.Id, Name = l.Name })
                .ToList()
            : [];

        return new RemoteLibraryState
        {
            SignedIn = _app.Library.IsSignedIn,
            Tab = RemoteLibraryTabId,
            SelectedListId = _activeListId,
            SelectedListName = UserListsBox.SelectedItem is SavedList selected ? selected.Name : null,
            Lists = lists,
        };
    }

    private string RemoteLibraryTabId => _activeTab switch
    {
        LibraryTab.WatchLater => "watchLater",
        LibraryTab.Lists => "lists",
        _ => "favorites",
    };

    public async Task RemoteSelectTabAsync(string tab)
    {
        if (!_app.Library.IsSignedIn)
        {
            throw new InvalidOperationException("Sign in on the PC Account page first.");
        }

        switch (tab.Trim().ToLowerInvariant())
        {
            case "favorites":
                _activeTab = LibraryTab.Favorites;
                _activeListId = null;
                SelectTab(LibraryTab.Favorites);
                await _app.Gallery.LoadFavoritesAsync();
                Gallery.UpdateEmptyState();
                break;
            case "watchlater":
            case "watch_later":
                _activeTab = LibraryTab.WatchLater;
                _activeListId = SavedList.WatchLaterId;
                SelectTab(LibraryTab.WatchLater);
                await _app.Gallery.LoadWatchLaterAsync();
                Gallery.UpdateEmptyState();
                break;
            case "lists":
                _activeTab = LibraryTab.Lists;
                _activeListId = null;
                SelectTab(LibraryTab.Lists);
                await RefreshListsAsync();
                Gallery.UpdateEmptyState();
                break;
            default:
                throw new ArgumentException("Use tab favorites, watchLater, or lists.");
        }
    }

    public async Task RemoteSelectListAsync(string listId)
    {
        if (!_app.Library.IsSignedIn)
        {
            throw new InvalidOperationException("Sign in on the PC Account page first.");
        }

        _activeTab = LibraryTab.Lists;
        _activeListId = listId;
        SelectTab(LibraryTab.Lists);
        await RefreshListsAsync();

        var list = _app.Library.UserLists().FirstOrDefault(l => l.Id == listId);
        if (list is null)
        {
            throw new ArgumentException("List not found on this account.");
        }

        UserListsBox.SelectedItem = list;
        await _app.Gallery.LoadListAsync(list.Id, list.Name);
        Gallery.UpdateEmptyState();
    }

    public async Task RemoteRefreshMetadataAsync()
    {
        if (!GalleryActions.EnsureSignedIn(Window.GetWindow(this)!))
        {
            throw new InvalidOperationException("Sign in on the PC Account page first.");
        }

        if (_app.Posts.Count == 0)
        {
            throw new InvalidOperationException("Load favorites or a list on the PC first.");
        }

        RefreshMetadataButton.IsEnabled = false;
        try
        {
            _ = _activeTab switch
            {
                LibraryTab.Favorites => await GalleryActions.RefreshFavoritesMetadataAsync(),
                LibraryTab.WatchLater => await GalleryActions.RefreshListMetadataAsync(SavedList.WatchLaterId),
                LibraryTab.Lists when UserListsBox.SelectedItem is SavedList list =>
                    await GalleryActions.RefreshListMetadataAsync(list.Id),
                _ => throw new InvalidOperationException("Select a list, or open Favorites / Watch Later."),
            };

            switch (_activeTab)
            {
                case LibraryTab.Favorites:
                    await _app.Gallery.LoadFavoritesAsync();
                    break;
                case LibraryTab.WatchLater:
                    await _app.Gallery.LoadWatchLaterAsync();
                    break;
                case LibraryTab.Lists when UserListsBox.SelectedItem is SavedList list:
                    await _app.Gallery.LoadListAsync(list.Id, list.Name);
                    break;
            }

            Gallery.UpdateEmptyState();
        }
        finally
        {
            RefreshMetadataButton.IsEnabled = true;
        }
    }
}
