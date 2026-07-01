using System.Windows;
using System.Windows.Controls;
using Rule34GalleryApp.Services;

namespace Rule34GalleryApp.Views.Overlays;

public partial class LoginDialog : UserControl
{
    private readonly AppServices _app = AppServices.Current;

    public event EventHandler? Closed;

    public LoginDialog()
    {
        InitializeComponent();
        UpdateGoogleUi();
    }

    public void Show(string? error = null)
    {
        LoginErrorText.Visibility = Visibility.Collapsed;
        if (!string.IsNullOrWhiteSpace(error))
        {
            LoginErrorText.Text = error;
            LoginErrorText.Visibility = Visibility.Visible;
        }

        UpdateGoogleUi();
        Visibility = Visibility.Visible;
        LoginEmailInput.Focus();
    }

    public void Hide()
    {
        Visibility = Visibility.Collapsed;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateGoogleUi()
    {
        if (!_app.Library.IsAvailable)
        {
            GoogleSignInButton.Visibility = Visibility.Collapsed;
            GoogleSetupHintText.Visibility = Visibility.Collapsed;
            return;
        }

        GoogleSignInButton.Visibility = Visibility.Visible;
        GoogleSetupHintText.Visibility = _app.Library.IsGoogleReady ? Visibility.Collapsed : Visibility.Visible;
        GoogleSetupHintText.Text = _app.Library.IsGoogleConfigured
            ? "Add googleClientSecret to firebase-config.json (Google Cloud → Web client → Client secret)"
            : "Add googleClientId to firebase-config.json";
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e) => Hide();

    private async void LoginSubmitButton_OnClick(object sender, RoutedEventArgs e)
        => await CompleteAuthAsync(signUp: false);

    private async void SignUpSubmitButton_OnClick(object sender, RoutedEventArgs e)
        => await CompleteAuthAsync(signUp: true);

    private async void GoogleSignInButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (!_app.Library.IsGoogleReady)
        {
            LoginErrorText.Text = _app.Library.IsGoogleConfigured
                ? Firebase.GoogleSignInService.BuildClientSecretHelp()
                : "Add googleClientId and googleClientSecret to firebase-config.json.";
            LoginErrorText.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            LoginErrorText.Visibility = Visibility.Collapsed;
            await _app.Library.SignInWithGoogleAsync();
            Hide();
            if (Window.GetWindow(this) is MainWindow main)
            {
                main.OnAuthCompleted();
            }
        }
        catch (Exception ex)
        {
            LoginErrorText.Text = ex.Message;
            LoginErrorText.Visibility = Visibility.Visible;
        }
    }

    private async Task CompleteAuthAsync(bool signUp)
    {
        try
        {
            LoginErrorText.Visibility = Visibility.Collapsed;
            var email = LoginEmailInput.Text.Trim();
            var password = LoginPasswordInput.Password;
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Enter email and password.");
            }

            if (signUp)
            {
                await _app.Library.SignUpAsync(email, password);
            }
            else
            {
                await _app.Library.SignInAsync(email, password);
            }

            LoginPasswordInput.Password = string.Empty;
            Hide();
            if (Window.GetWindow(this) is MainWindow main)
            {
                main.OnAuthCompleted();
            }
        }
        catch (Exception ex)
        {
            LoginErrorText.Text = ex.Message;
            LoginErrorText.Visibility = Visibility.Visible;
        }
    }
}
