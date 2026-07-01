using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Rule34Gallery.Core;
using Rule34Gallery.Core.Remote;
using Rule34Gallery.Core.Services;
using Rule34GalleryApp.Controls;
using Rule34GalleryApp.Helpers;
using Rule34GalleryApp.Services;



namespace Rule34GalleryApp.Views.Pages;



public partial class SettingsPage : Page

{

    private readonly AppServices _app = AppServices.Current;

    public SettingsPage()
    {
        InitializeComponent();

        BlacklistAutocompleteList.ItemsSource = _app.BlacklistAutocomplete;
        GlobalBlockAutocompleteList.ItemsSource = _app.BlacklistAutocomplete;

        PageScrollHelper.Attach(this, SettingsScroll);

        _app.CredentialsSynced += (_, _) => Dispatcher.Invoke(ApplySettings);
    }



    public void ApplySettings()

    {

        _app.Gallery.BeginLoadSettings();

        try

        {

            _app.Settings.SyncPresetTagsToBlacklist();

            FilterAiCheckBox.IsChecked = _app.Settings.FilterAi;

            if (_app.Settings.LimitIndex >= 0 && _app.Settings.LimitIndex < LimitCombo.Items.Count)

            {

                LimitCombo.SelectedIndex = _app.Settings.LimitIndex;

            }

            BrowseLayoutCombo.SelectedIndex = _app.Settings.BrowseLayoutMode == BrowseLayoutMode.Feed ? 1 : 0;
            FeedMediaQualityCombo.SelectedIndex = _app.Settings.FeedMediaQuality == FeedMediaQuality.Full ? 1 : 0;
            KeepMediaCacheCheckBox.IsChecked = !_app.Settings.ClearMediaPlaybackCacheOnExit;
            CheckUpdatesOnStartupCheckBox.IsChecked = _app.Settings.CheckForUpdatesOnStartup;
            ForYouEnabledCheckBox.IsChecked = _app.Settings.ForYouEnabled;
            ForYouLearnArtistsCheckBox.IsChecked = _app.Settings.ForYouLearnArtists;
            ForYouLearnSeriesCheckBox.IsChecked = _app.Settings.ForYouLearnSeries;
            ForYouLearnMinorTagsCheckBox.IsChecked = _app.Settings.ForYouLearnMinorTags;
            ForYouCloudSyncCheckBox.IsChecked = _app.Settings.ForYouCloudSync;
            UseOpenAiForForYouCheckBox.IsChecked = _app.Settings.UseOpenAiForForYou;

            UpdateForYouCategoryToggleState();

            RebuildGlobalBlockedPanel();
            RebuildBlacklistPanel();
            ReloadDownloadLibraryCombo();
            ApplyRemoteControlFields();

        }

        finally

        {

            _app.Gallery.EndLoadSettings();

        }

    }

    private void ReloadDownloadLibraryCombo()
    {
        var libraries = _app.Settings.LocalLibraries;
        DownloadLibraryCombo.ItemsSource = libraries;
        if (libraries.Count == 0)
        {
            DownloadLibraryCombo.SelectedItem = null;
            DownloadLibraryPathText.Text = "Create a library on the Local tab first.";
            return;
        }

        var selected = libraries.FirstOrDefault(l => l.Id == _app.Settings.DownloadLibraryId) ?? libraries[0];
        DownloadLibraryCombo.SelectedItem = selected;
        DownloadLibraryPathText.Text = string.IsNullOrWhiteSpace(selected.RootFolderPath)
            ? "Set the parent folder on the Local tab."
            : selected.RootFolderPath;
    }

    private void DownloadLibraryCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DownloadLibraryCombo.SelectedItem is not LocalLibraryDefinition library)
        {
            return;
        }

        _app.Settings.DownloadLibraryId = library.Id;
        DownloadLibraryPathText.Text = string.IsNullOrWhiteSpace(library.RootFolderPath)
            ? "Set the parent folder on the Local tab."
            : library.RootFolderPath;
        _app.SaveSettings();
    }



    private void OpenDownloadManagerButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow main)
        {
            main.ShowDownloadManager();
        }
    }

    private void ManageBlacklistPresetsButton_OnClick(object sender, RoutedEventArgs e)

        => PresetPickerService.ShowBlacklistPresets(_app.Settings, OnBlacklistPresetsChanged);



    private void OnBlacklistPresetsChanged()

    {

        RebuildBlacklistPanel();

        _app.SaveSettings();

    }



    private void OnBlacklistTagsChanged()

    {

        RebuildBlacklistPanel();

        _app.SaveSettings();

    }



    private void RebuildGlobalBlockedPanel()
        => GlobalBlockTagUi.RebuildChips(GlobalBlockedTagsPanel, _app.Settings, OnGlobalBlockedTagsChanged);

    private void OnGlobalBlockedTagsChanged()
    {
        _app.SaveSettings();
        RebuildGlobalBlockedPanel();
        _app.Gallery.RemoveBlockedPosts();
    }

    private void RebuildBlacklistPanel()
        => BlacklistTagUi.RebuildChips(BlacklistTagsPanel, _app.Settings, OnBlacklistTagsChanged);

    private void GlobalBlockInput_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddGlobalBlockFromInput();
            e.Handled = true;
        }
        else if (e.Key == Key.OemComma || e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.None)
        {
            AddGlobalBlockFromInput();
            e.Handled = true;
        }
    }

    private async void GlobalBlockInput_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            var hasSuggestions = await TagAutocompleteService.TryPopulateSuggestionsAsync(
                _app.Http,
                _app.BlacklistAutocomplete,
                GlobalBlockInput.Text);
            GlobalBlockAutocompletePopup.IsOpen = hasSuggestions;
        }
        catch
        {
            GlobalBlockAutocompletePopup.IsOpen = false;
        }
    }

    private void GlobalBlockAutocompleteList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GlobalBlockAutocompleteList.SelectedItem is not TagSuggestion selected)
        {
            return;
        }

        TagAutocompleteWpf.ApplySuggestionToInput(GlobalBlockInput, selected);
        GlobalBlockAutocompletePopup.IsOpen = false;
        GlobalBlockAutocompleteList.SelectedItem = null;
    }

    private void AddGlobalBlockButton_OnClick(object sender, RoutedEventArgs e) => AddGlobalBlockFromInput();

    private void AddGlobalBlockFromInput()
    {
        var raw = GlobalBlockInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        foreach (var token in raw.Split([' ', '\t', ','], StringSplitOptions.RemoveEmptyEntries))
        {
            _app.Settings.AddGlobalBlockedTag(token);
        }

        GlobalBlockInput.Text = string.Empty;
        OnGlobalBlockedTagsChanged();
    }

    private void BlacklistInput_OnKeyDown(object sender, KeyEventArgs e)

    {

        if (e.Key == Key.Enter)

        {

            AddBlacklistFromInput();

            e.Handled = true;

        }

        else if (e.Key == Key.OemComma || e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.None)

        {

            AddBlacklistFromInput();

            e.Handled = true;

        }

    }



    private async void BlacklistInput_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            var hasSuggestions = await TagAutocompleteService.TryPopulateSuggestionsAsync(
                _app.Http,
                _app.BlacklistAutocomplete,
                BlacklistInput.Text);
            BlacklistAutocompletePopup.IsOpen = hasSuggestions;
        }
        catch
        {
            BlacklistAutocompletePopup.IsOpen = false;
        }
    }



    private void BlacklistAutocompleteList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)

    {

        if (BlacklistAutocompleteList.SelectedItem is not TagSuggestion selected)

        {

            return;

        }



        TagAutocompleteWpf.ApplySuggestionToInput(BlacklistInput, selected);

        BlacklistAutocompletePopup.IsOpen = false;

        BlacklistAutocompleteList.SelectedItem = null;

    }



    private void AddBlacklistButton_OnClick(object sender, RoutedEventArgs e) => AddBlacklistFromInput();



    private void AddBlacklistFromInput()

    {

        var raw = BlacklistInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(raw))

        {

            return;

        }



        foreach (var token in raw.Split([' ', '\t', ','], StringSplitOptions.RemoveEmptyEntries))

        {

            _app.Settings.AddBlacklistTag(token);

        }



        BlacklistInput.Text = string.Empty;

        RebuildBlacklistPanel();

        _app.SaveSettings();

    }



    private void LimitCombo_OnChanged(object sender, SelectionChangedEventArgs e)

    {

        if (_app.Gallery.IsLoadingSettings || LimitCombo.SelectedIndex < 0)

        {

            return;

        }



        _app.Settings.LimitIndex = LimitCombo.SelectedIndex;

        _app.Gallery.ScheduleSaveSettings();

    }



    private void FilterAi_OnChanged(object sender, RoutedEventArgs e)

    {

        if (_app.Gallery.IsLoadingSettings)

        {

            return;

        }



        _app.Settings.FilterAi = FilterAiCheckBox.IsChecked == true;

        _app.Gallery.ScheduleSaveSettings();
    }

    private void BrowseLayoutCombo_OnChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_app.Gallery.IsLoadingSettings || BrowseLayoutCombo.SelectedIndex < 0)
        {
            return;
        }

        _app.Settings.BrowseLayoutMode = BrowseLayoutCombo.SelectedIndex == 1
            ? BrowseLayoutMode.Feed
            : BrowseLayoutMode.Grid;
        _app.SaveSettings();

        if (Window.GetWindow(this) is MainWindow main)
        {
            main.RefreshBrowseFromSettings();
        }
    }

    private void FeedMediaQualityCombo_OnChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_app.Gallery.IsLoadingSettings || FeedMediaQualityCombo.SelectedIndex < 0)
        {
            return;
        }

        _app.Settings.FeedMediaQuality = FeedMediaQualityCombo.SelectedIndex == 1
            ? FeedMediaQuality.Full
            : FeedMediaQuality.Sample;
        _app.SaveSettings();

        if (Window.GetWindow(this) is MainWindow main)
        {
            main.RefreshBrowseFromSettings();
        }
    }

    private void KeepMediaCache_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_app.Gallery.IsLoadingSettings)
        {
            return;
        }

        _app.Settings.ClearMediaPlaybackCacheOnExit = KeepMediaCacheCheckBox.IsChecked != true;
        _app.SaveSettings();
    }

    private void CheckUpdatesOnStartup_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_app.Gallery.IsLoadingSettings)
        {
            return;
        }

        _app.Settings.CheckForUpdatesOnStartup = CheckUpdatesOnStartupCheckBox.IsChecked == true;
        _app.SaveSettings();
    }

    private async void CheckForUpdatesNow_OnClick(object sender, RoutedEventArgs e)
    {
        if (Application.Current.MainWindow is MainWindow main)
        {
            await main.CheckForUpdatesNowAsync();
        }
    }

    private void ForYouEnabled_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_app.Gallery.IsLoadingSettings)
        {
            return;
        }

        _app.Settings.ForYouEnabled = ForYouEnabledCheckBox.IsChecked == true;
        _app.ForYou.IsEnabled = _app.Settings.ForYouEnabled;
        UpdateForYouCategoryToggleState();
        _app.SaveSettings();
    }

    private void ForYouLearnCategory_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_app.Gallery.IsLoadingSettings)
        {
            return;
        }

        _app.Settings.ForYouLearnArtists = ForYouLearnArtistsCheckBox.IsChecked == true;
        _app.Settings.ForYouLearnSeries = ForYouLearnSeriesCheckBox.IsChecked == true;
        _app.Settings.ForYouLearnMinorTags = ForYouLearnMinorTagsCheckBox.IsChecked == true;
        _app.SaveSettings();
        _app.ForYou.ApplyLearningCategorySettings();
    }

    private void UpdateForYouCategoryToggleState()
    {
        var enabled = ForYouEnabledCheckBox.IsChecked == true;
        ForYouLearnCategoriesPanel.IsEnabled = enabled;
    }

    private void ForYouCloudSync_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_app.Gallery.IsLoadingSettings)
        {
            return;
        }

        _app.Settings.ForYouCloudSync = ForYouCloudSyncCheckBox.IsChecked == true;
        _app.ForYou.CloudSyncEnabled = _app.Settings.ForYouCloudSync;
        _app.SaveSettings();
    }

    private void UseOpenAiForForYou_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_app.Gallery.IsLoadingSettings)
        {
            return;
        }

        _app.Settings.UseOpenAiForForYou = UseOpenAiForForYouCheckBox.IsChecked == true;
        _app.SaveSettings();
    }

    public void RefreshRemoteControlUi() => ApplyRemoteControlFields();

    private void ApplyRemoteControlFields()
    {
        _app.Settings.EnsureRemoteControlToken();
        RemoteEnabledCheckBox.IsChecked = _app.Settings.RemoteControlEnabled;
        RemotePortInput.Text = _app.Settings.RemoteControlPort.ToString();
        RemoteConnectionText.Text = BuildConnectionString();

        var enabled = _app.Settings.RemoteControlEnabled;
        var serverReady = enabled &&
                          Application.Current.MainWindow is MainWindow mw &&
                          string.IsNullOrWhiteSpace(mw.RemoteServer.StartError) &&
                          mw.RemoteServer.IsRunning;
        RemotePairingPanel.Visibility = serverReady ? Visibility.Visible : Visibility.Collapsed;
        if (serverReady && Application.Current.MainWindow is MainWindow main)
        {
            var connectionString = BuildConnectionString();
            RemoteQrImage.Source = QrCodeBitmapHelper.CreateImageSource(connectionString);
            var pin = main.RemoteServer.PairingPin;
            RemotePinText.Text = pin.Length == 6 ? $"{pin[..3]} {pin[3..]}" : pin;
            var remaining = main.RemoteServer.PairingPinTimeRemaining;
            RemotePinExpiryText.Text = remaining.TotalSeconds > 60
                ? $"Code expires in {(int)remaining.TotalMinutes} min"
                : "Code expires in under a minute";
        }
        else
        {
            RemoteQrImage.Source = null;
            RemotePinText.Text = string.Empty;
            RemotePinExpiryText.Text = string.Empty;
        }

        RefreshRemoteStatusText();
    }

    private string BuildConnectionString()
    {
        var host = RemoteNetworkHelper.GetLocalIPv4Addresses().FirstOrDefault() ?? "127.0.0.1";
        var port = _app.Settings.RemoteControlPort > 0
            ? _app.Settings.RemoteControlPort
            : RemoteProtocol.DefaultPort;
        return new RemotePairingPayload
        {
            Host = host,
            Port = port,
            Token = _app.Settings.RemoteControlToken,
        }.ToConnectionString();
    }

    private void RefreshRemoteStatusText()
    {
        if (Application.Current.MainWindow is not MainWindow main)
        {
            RemoteStatusText.Text = string.Empty;
            return;
        }

        if (!_app.Settings.RemoteControlEnabled)
        {
            RemoteStatusText.Text = "Remote is off.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(main.RemoteServer.StartError))
        {
            RemoteStatusText.Text = main.RemoteServer.StartError;
            return;
        }

        if (!main.RemoteServer.IsRunning)
        {
            RemoteStatusText.Text = "Remote enabled but server is not listening.";
            return;
        }

        var ips = RemoteNetworkHelper.GetLocalIPv4Addresses();
        var port = _app.Settings.RemoteControlPort;
        var lines = ips.Count > 0
            ? ips.Select(ip => $"http://{ip}:{port}/")
            : main.RemoteServer.ActivePrefixes;
        RemoteStatusText.Text =
            "Listening on:\n" + string.Join("\n", lines) +
            "\nPhone must be on the same Wi‑Fi.";
    }

    private void PersistRemoteSettings()
    {
        if (_app.Gallery.IsLoadingSettings)
        {
            return;
        }

        _app.Settings.RemoteControlEnabled = RemoteEnabledCheckBox.IsChecked == true;
        if (int.TryParse(RemotePortInput.Text.Trim(), out var port) && port is > 0 and < 65536)
        {
            _app.Settings.RemoteControlPort = port;
        }

        _app.Settings.EnsureRemoteControlToken();
        _app.SaveSettings();
        RemoteConnectionText.Text = BuildConnectionString();

        if (Application.Current.MainWindow is MainWindow main)
        {
            main.ApplyRemoteControlSettings();
            ApplyRemoteControlFields();
        }
    }

    private void RemoteSettings_OnChanged(object sender, RoutedEventArgs e) => PersistRemoteSettings();

    private void RefreshRemotePin_OnClick(object sender, RoutedEventArgs e)
    {
        if (Application.Current.MainWindow is MainWindow main)
        {
            main.RemoteServer.RefreshPairingPin();
            ApplyRemoteControlFields();
            _app.Messenger.Show("New code", "Scan again or enter the new 6-digit code on your phone.", AppMessageKind.Info);
        }
    }

    private void CopyRemoteConnection_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(RemoteConnectionText.Text);
            _app.Messenger.Show("Copied", "Connection string copied to clipboard.", AppMessageKind.Info);
        }
        catch
        {
            _app.Messenger.Show("Copy failed", "Could not access the clipboard.", AppMessageKind.Warning);
        }
    }

    private void RegenerateRemoteToken_OnClick(object sender, RoutedEventArgs e)
    {
        _app.Settings.RemoteControlToken = RemoteTokenGenerator.CreateToken();
        PersistRemoteSettings();
        if (Application.Current.MainWindow is MainWindow main)
        {
            main.RemoteServer.RefreshPairingPin();
            ApplyRemoteControlFields();
        }

        _app.Messenger.Show("New token", "Pair again on your phone (QR or code).", AppMessageKind.Info);
    }
}



