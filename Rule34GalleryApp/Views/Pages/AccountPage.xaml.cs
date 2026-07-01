using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Rule34Gallery.Core.Services;
using Rule34GalleryApp.Firebase;
using Rule34GalleryApp.Helpers;
using Rule34GalleryApp.Services;

namespace Rule34GalleryApp.Views.Pages;

public partial class AccountPage : Page
{
    private readonly AppServices _app = AppServices.Current;

    public AccountPage()
    {
        InitializeComponent();
        PageScrollHelper.Attach(this, AccountScroll);
        _app.Library.AuthStateChanged += (_, _) => Dispatcher.Invoke(RefreshUi);
        _app.CredentialsSynced += (_, _) => Dispatcher.Invoke(ApplyCredentials);
        _app.Messenger.CredentialHighlightChanged += (_, highlighted) =>
            Dispatcher.Invoke(() => CredentialsPanel.SetCredentialHighlight(highlighted));
        _app.Messenger.ToastDismissed += (_, _) =>
            Dispatcher.Invoke(() => CredentialsPanel.SetCredentialHighlight(false));
        RefreshUi();
        ApplyCredentials();
    }

    public void ApplyCredentials() => CredentialsPanel.ApplyFromSettings();

    public void RefreshUi()
    {
        if (!_app.Library.IsAvailable)
        {
            AccountStatusText.Text = "Firebase not configured. Add firebase-config.json to enable cloud library.";
            SignInButton.Visibility = Visibility.Collapsed;
            GoogleSignInButton.Visibility = Visibility.Collapsed;
            SignOutButton.Visibility = Visibility.Collapsed;
            RefreshCloudSyncUi();
            return;
        }

        if (_app.Library.IsSignedIn)
        {
            AccountStatusText.Text = $"Signed in as {_app.Library.CurrentEmail}";
            SignInButton.Visibility = Visibility.Collapsed;
            GoogleSignInButton.Visibility = Visibility.Collapsed;
            SignOutButton.Visibility = Visibility.Visible;
        }
        else
        {
            AccountStatusText.Text = _app.Library.IsGoogleAvailable
                ? "Sign in with Google or email to save favorites, lists, and API keys across devices."
                : "Sign in to save favorites, lists, and API keys across devices.";
            SignInButton.Visibility = Visibility.Visible;
            GoogleSignInButton.Visibility = _app.Library.IsGoogleAvailable ? Visibility.Visible : Visibility.Collapsed;
            SignOutButton.Visibility = Visibility.Collapsed;
        }

        RefreshCloudSyncUi();
    }

    private void RefreshCloudSyncUi()
    {
        OpenSyncTabButton.IsEnabled = _app.Library.IsAvailable;

        CloudSyncStatusText.Text = !_app.Library.IsAvailable
            ? "Add firebase-config.json to enable cloud sync (see firebase-config.example.json)."
            : !_app.Library.IsSignedIn
                ? "Sign in above, then use the Cloud sync tab to upload and download your library and For You profile."
                : "Use the Cloud sync tab in the sidebar to sync credentials, favorites, lists, saved tags, and For You.";
    }

    private void OpenSyncTabButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow main)
        {
            main.NavigateToSync();
        }
    }

    private void SignInButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow main)
        {
            main.ShowLoginDialog();
        }
    }

    private async void GoogleSignInButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is not MainWindow main)
        {
            return;
        }

        if (!_app.Library.IsGoogleReady)
        {
            main.ShowLoginDialog(
                _app.Library.IsGoogleConfigured
                    ? GoogleSignInService.BuildClientSecretHelp()
                    : "Add googleClientId and googleClientSecret to firebase-config.json.");
            return;
        }

        try
        {
            LoadingOverlay.Show();
            await _app.Library.SignInWithGoogleAsync();
            RefreshUi();
            main.ApplySettingsToPages();
            if (main.GetLibraryPage() is LibraryPage library)
            {
                await library.RefreshListsAsync();
            }
        }
        catch (Exception ex)
        {
            main.ShowLoginDialog(ex.Message);
            if (ex.Message.Contains("redirect URI", StringComparison.OrdinalIgnoreCase))
            {
                TryOpenGoogleCredentialsPage();
            }
        }
        finally
        {
            LoadingOverlay.Hide();
        }
    }

    private void SignOutButton_OnClick(object sender, RoutedEventArgs e)
    {
        _app.Library.SignOut();
        RefreshUi();
        if (Window.GetWindow(this) is MainWindow main && main.GetLibraryPage() is LibraryPage library)
        {
            library.SetSignedInState(false);
            _ = library.RefreshListsAsync();
        }
    }

    private static void TryOpenGoogleCredentialsPage()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://console.cloud.google.com/apis/credentials?project=r34-browser",
                UseShellExecute = true,
            });
        }
        catch
        {
            // Ignore.
        }
    }
}
