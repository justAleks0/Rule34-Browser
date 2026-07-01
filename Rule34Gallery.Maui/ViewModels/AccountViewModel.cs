using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rule34Gallery.Core.Services;

namespace Rule34Gallery.Maui.ViewModels;

public partial class AccountViewModel : ObservableObject
{
    private readonly AppServices _app = AppServices.Current;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isSignedIn;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private string _cloudSyncStatus = string.Empty;

    [ObservableProperty]
    private bool _canSyncCloud;

    [ObservableProperty]
    private bool _isBusy;

    public bool IsFirebaseAvailable => _app.Library.IsAvailable;

    public bool IsGoogleAvailable => _app.Library.IsGoogleAvailable;

    public AccountViewModel()
    {
        Refresh();
        _app.Library.AuthStateChanged += (_, _) => Refresh();
    }

    private void Refresh()
    {
        IsSignedIn = _app.Library.IsSignedIn;
        Status = IsSignedIn ? $"Signed in as {_app.Library.CurrentEmail}" : "Not signed in";
        CanSyncCloud = _app.Library.IsAvailable && _app.Library.IsSignedIn;
        CloudSyncStatus = !_app.Library.IsAvailable
            ? "Firebase not configured."
            : !IsSignedIn
                ? "Sign in to sync API keys and favorites across devices."
                : "Syncs API keys from Settings and refreshes favorites.";
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        IsBusy = true;
        try
        {
            await _app.Library.SignInAsync(Email.Trim(), Password);
            Refresh();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SignUpAsync()
    {
        IsBusy = true;
        try
        {
            await _app.Library.SignUpAsync(Email.Trim(), Password);
            Refresh();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SignInGoogleAsync()
    {
        IsBusy = true;
        try
        {
            await _app.Library.SignInWithGoogleAsync();
            Refresh();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SignOut()
    {
        _app.Library.SignOut();
        Refresh();
    }

    [RelayCommand(CanExecute = nameof(CanSyncCloud))]
    private async Task SyncCloudAsync()
    {
        IsBusy = true;
        try
        {
            _app.SaveSettings();
            var result = await _app.SyncCloudNowAsync().ConfigureAwait(false);
            var kind = result.Success ? AppMessageKind.Info : AppMessageKind.Warning;
            _app.Messenger.Show(result.Title, result.Message, kind);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
