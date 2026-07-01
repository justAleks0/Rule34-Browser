using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Rule34Gallery.Core.Services;
using Rule34GalleryApp.Services;

namespace Rule34GalleryApp.Controls;

public partial class AccountCredentialsPanel : UserControl
{
    private readonly AppServices _app = AppServices.Current;
    private bool _applyingCredentialParse;
    private bool _loading;

    public AccountCredentialsPanel()
    {
        InitializeComponent();
    }

    public void ApplyFromSettings()
    {
        _loading = true;
        try
        {
            UserIdInput.Text = _app.Settings.UserId;
            ApiKeyInput.Password = _app.Settings.ApiKey;
            OpenAiApiKeyInput.Password = _app.Settings.OpenAiApiKey;
            OpenAiModelInput.Text = string.IsNullOrWhiteSpace(_app.Settings.OpenAiModel)
                ? "gpt-4o-mini"
                : _app.Settings.OpenAiModel;
            UseOpenAiDescribeCheckBox.IsChecked = _app.Settings.UseOpenAiForDescribeSearch;
        }
        finally
        {
            _loading = false;
        }
    }

    public void SetCredentialHighlight(bool highlighted)
    {
        var border = highlighted
            ? new SolidColorBrush(Color.FromRgb(0xc4, 0x5c, 0x5c))
            : Application.Current.Resources["BorderBrush"] as Brush;
        var thickness = highlighted ? new Thickness(2) : new Thickness(1);
        UserIdInput.BorderBrush = border;
        ApiKeyInput.BorderBrush = border;
        CredentialBlobInput.BorderBrush = border;
        UserIdInput.BorderThickness = thickness;
        ApiKeyInput.BorderThickness = thickness;
        CredentialBlobInput.BorderThickness = thickness;
    }

    private void SyncCredentialsToApp(bool saveToDisk = true)
    {
        if (_applyingCredentialParse || _loading)
        {
            return;
        }

        if (TryParseCredentialsFromInputs(blobOnly: true))
        {
            return;
        }

        var userIdFromUi = UserIdInput.Text.Trim();
        var apiKeyFromUi = ApiKeyInput.Password;

        if (Rule34Api.TryParseCredentialBlob(userIdFromUi, out var parsedUserId, out var parsedApiKey) ||
            Rule34Api.TryParseCredentialBlob(apiKeyFromUi, out parsedUserId, out parsedApiKey) ||
            Rule34Api.TryParseCredentialBlob(CredentialBlobInput.Text, out parsedUserId, out parsedApiKey))
        {
            ApplyParsedCredentials(parsedUserId, parsedApiKey);
            return;
        }

        if (!string.IsNullOrWhiteSpace(userIdFromUi) && !Rule34Api.LooksLikeCredentialBlob(userIdFromUi))
        {
            _app.Settings.UserId = userIdFromUi;
        }

        if (!string.IsNullOrWhiteSpace(apiKeyFromUi))
        {
            _app.Settings.ApiKey = apiKeyFromUi.Trim();
        }

        if (saveToDisk)
        {
            _app.SaveSettings();
        }
    }

    private void CredentialBlobInput_OnTextChanged(object sender, TextChangedEventArgs e)
        => TryParseCredentialsFromInputs(blobOnly: true);

    private void CredentialInput_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_applyingCredentialParse || _loading)
        {
            return;
        }

        if (TryParseCredentialsFromInputs(blobOnly: false))
        {
            return;
        }

        var userId = UserIdInput.Text.Trim();
        if (!string.IsNullOrWhiteSpace(userId) && !Rule34Api.LooksLikeCredentialBlob(userId))
        {
            _app.Settings.UserId = userId;
            _app.SaveSettings();
        }
    }

    private void ApiKeyInput_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_applyingCredentialParse || _loading)
        {
            return;
        }

        if (TryParseCredentialsFromInputs(blobOnly: false))
        {
            return;
        }

        if (!string.IsNullOrEmpty(ApiKeyInput.Password))
        {
            _app.Settings.ApiKey = ApiKeyInput.Password;
            _app.SaveSettings();
        }
    }

    private void Credentials_OnLostFocus(object sender, RoutedEventArgs e)
        => SyncCredentialsToApp(saveToDisk: true);

    private bool TryParseCredentialsFromInputs(bool blobOnly)
    {
        if (_applyingCredentialParse || _loading)
        {
            return false;
        }

        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(CredentialBlobInput.Text))
        {
            candidates.Add(CredentialBlobInput.Text);
        }

        if (!blobOnly)
        {
            if (!string.IsNullOrWhiteSpace(UserIdInput.Text))
            {
                candidates.Add(UserIdInput.Text);
            }

            if (!string.IsNullOrWhiteSpace(ApiKeyInput.Password))
            {
                candidates.Add(ApiKeyInput.Password);
            }

            candidates.Add(UserIdInput.Text + ApiKeyInput.Password);
        }

        foreach (var candidate in candidates)
        {
            if (Rule34Api.TryParseCredentialBlob(candidate, out var userId, out var apiKey))
            {
                ApplyParsedCredentials(userId, apiKey);
                return true;
            }
        }

        return false;
    }

    private void ApplyParsedCredentials(string userId, string apiKey)
    {
        _applyingCredentialParse = true;
        try
        {
            UserIdInput.Text = userId;
            ApiKeyInput.Password = apiKey;
            CredentialBlobInput.Text = string.Empty;
            _app.SetCredentials(userId, apiKey, persist: true);
            SetCredentialHighlight(false);
            _app.Messenger.Show("Credentials parsed", $"User ID {userId} and API key loaded.", AppMessageKind.Info);
        }
        finally
        {
            _applyingCredentialParse = false;
        }
    }

    private void OpenAiApiKeyInput_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        if (!string.IsNullOrEmpty(OpenAiApiKeyInput.Password))
        {
            _app.Settings.OpenAiApiKey = OpenAiApiKeyInput.Password;
            _app.SaveSettings();
        }
    }

    private void OpenAiModelInput_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        var model = OpenAiModelInput.Text.Trim();
        _app.Settings.OpenAiModel = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model;
        _app.SaveSettings();
    }

    private void UseOpenAiDescribe_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        _app.Settings.UseOpenAiForDescribeSearch = UseOpenAiDescribeCheckBox.IsChecked == true;
        _app.SaveSettings();
    }

    private void OpenAiSettings_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        if (!string.IsNullOrEmpty(OpenAiApiKeyInput.Password))
        {
            _app.Settings.OpenAiApiKey = OpenAiApiKeyInput.Password;
        }

        var model = OpenAiModelInput.Text.Trim();
        _app.Settings.OpenAiModel = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model;
        _app.SaveSettings();
    }

    private void OpenAiHelpLink_OnRequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true,
        });
        e.Handled = true;
    }
}
