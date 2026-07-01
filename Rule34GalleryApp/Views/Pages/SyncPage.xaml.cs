using System.Windows;
using System.Windows.Controls;
using Rule34Gallery.Core;
using Rule34Gallery.Core.CloudSync;
using Rule34GalleryApp.Helpers;
using Rule34GalleryApp.Services;

namespace Rule34GalleryApp.Views.Pages;

public partial class SyncPage : Page
{
    private readonly AppServices _app = AppServices.Current;
    private SyncDataNode? _selectedNode;
    private List<SyncDataNode>? _boundDataTree;

    public List<SyncDataNode> DataTree => _app.CloudSync.DataTree;

    public SyncPage()
    {
        InitializeComponent();
        DataContext = this;
        PageScrollHelper.Attach(this, SyncScroll);
        StepsList.ItemsSource = _app.CloudSync.Steps;
        _app.CloudSync.StepsChanged += (_, _) => Dispatcher.Invoke(RefreshUi);
        _app.CloudSync.PreviewChanged += (_, _) => Dispatcher.Invoke(RefreshUi);
        _app.Library.AuthStateChanged += (_, _) => Dispatcher.Invoke(OnAuthChanged);
        Loaded += async (_, _) =>
        {
            RefreshUi();
            await RefreshPreviewSafeAsync();
        };
    }

    private async void OnAuthChanged()
    {
        RefreshUi();
        await RefreshPreviewSafeAsync();
    }

    private async Task RefreshPreviewSafeAsync()
    {
        try
        {
            await _app.CloudSync.RefreshPreviewAsync().ConfigureAwait(true);
        }
        catch
        {
            // Shown via PreviewError.
        }

        RefreshUi();
    }

    public void RefreshUi()
    {
        var library = _app.Library;
        var sync = _app.CloudSync;

        if (!library.IsAvailable)
        {
            SyncStatusHint.Text = "Firebase not configured.";
            StepsSummaryText.Text = "Add firebase-config.json to enable cloud sync.";
            SetActionsEnabled(false);
        }
        else if (!library.IsSignedIn)
        {
            SyncStatusHint.Text = "Not signed in.";
            StepsSummaryText.Text = "Sign in on the Account page, then return here.";
            SetActionsEnabled(false);
        }
        else
        {
            SyncStatusHint.Text = $"Signed in as {library.CurrentEmail}";
            SetActionsEnabled(!sync.IsRunning && !sync.IsPreviewLoading);

            if (sync.IsRunning && sync.Steps.Count > 0)
            {
                var done = sync.Steps.Count(s =>
                    s.Status is CloudSyncStepStatus.Completed
                        or CloudSyncStepStatus.Failed
                        or CloudSyncStepStatus.Skipped);
                StepsSummaryText.Text = $"Syncing… {done}/{sync.Steps.Count} steps.";
            }
            else if (!string.IsNullOrWhiteSpace(sync.LastSummary))
            {
                StepsSummaryText.Text = sync.LastSummary;
            }
            else
            {
                StepsSummaryText.Text = "Preview loaded. Choose upload or download when ready.";
            }
        }

        StatusChipText.Text = sync.SessionMeta.Status switch
        {
            SyncSessionStatus.Syncing => "Syncing",
            SyncSessionStatus.Success => "OK",
            SyncSessionStatus.Failed => "Failed",
            SyncSessionStatus.Partial => "Partial",
            _ => "Idle",
        };

        LastSyncText.Text = FormatLastSync(sync.SessionMeta);

        if (!string.IsNullOrWhiteSpace(sync.PreviewError))
        {
            PreviewErrorText.Text = sync.PreviewError;
            PreviewErrorText.Visibility = Visibility.Visible;
        }
        else
        {
            PreviewErrorText.Visibility = Visibility.Collapsed;
        }

        var hasSteps = sync.Steps.Count > 0;
        EmptyStepsPanel.Visibility = hasSteps ? Visibility.Collapsed : Visibility.Visible;
        StepsList.Visibility = hasSteps ? Visibility.Visible : Visibility.Collapsed;

        BindDataExplorerIfNeeded(sync.DataTree);

        if (sync.IsRunning || sync.IsPreviewLoading)
        {
            LoadingOverlay.Show(StepsSummaryText.Text);
        }
        else
        {
            LoadingOverlay.Hide();
        }
    }

    private void BindDataExplorerIfNeeded(List<SyncDataNode> tree)
    {
        if (ReferenceEquals(_boundDataTree, tree))
        {
            return;
        }

        _boundDataTree = tree;
        DataExplorer.ItemsSource = tree;
    }

    private void SetActionsEnabled(bool enabled)
    {
        UploadButton.IsEnabled = enabled;
        DownloadButton.IsEnabled = enabled;
        RefreshPreviewButton.IsEnabled = enabled;
    }

    private static string FormatLastSync(SyncSessionMeta meta)
    {
        if (meta.LastSuccessAtUnix is null or <= 0)
        {
            return "Last sync: never";
        }

        var when = DateTimeOffset.FromUnixTimeSeconds(meta.LastSuccessAtUnix.Value).ToLocalTime();
        var ago = DateTimeOffset.Now - when;
        var rel = ago.TotalMinutes < 1
            ? "just now"
            : ago.TotalHours < 1
                ? $"{(int)ago.TotalMinutes} min ago"
                : ago.TotalDays < 1
                    ? $"{(int)ago.TotalHours} hr ago"
                    : when.ToString("g");
        var device = string.IsNullOrWhiteSpace(meta.LastDeviceLabel)
            ? meta.LastDeviceId
            : meta.LastDeviceLabel;
        var direction = string.IsNullOrWhiteSpace(meta.LastDirection) ? "" : $" · {meta.LastDirection}";
        return $"Last sync: {rel} on {device}{direction}";
    }

    private async void RefreshPreviewButton_OnClick(object sender, RoutedEventArgs e)
    {
        SetActionsEnabled(false);
        await RefreshPreviewSafeAsync();
    }

    private async void UploadButton_OnClick(object sender, RoutedEventArgs e)
    {
        await RunDirectionalAsync(SyncDirection.Upload);
    }

    private async void DownloadButton_OnClick(object sender, RoutedEventArgs e)
    {
        await RunDirectionalAsync(SyncDirection.Download);
    }

    private async Task RunDirectionalAsync(SyncDirection direction)
    {
        var owner = Window.GetWindow(this);
        if (owner is null)
        {
            return;
        }

        if (!SyncApplyDialog.TryConfirm(owner, direction, out var mode))
        {
            return;
        }

        SetActionsEnabled(false);
        try
        {
            _app.SaveSettings();
            var result = direction == SyncDirection.Upload
                ? await _app.CloudSync.RunUploadAsync(mode).ConfigureAwait(true)
                : await _app.CloudSync.RunDownloadAsync(mode).ConfigureAwait(true);

            if (!result.Success)
            {
                _app.Messenger.Show(result.Title, result.Message, AppMessageKind.Warning);
            }
            else if (Window.GetWindow(this) is MainWindow main)
            {
                main.ApplySettingsToPages();
            }
        }
        catch (Exception ex)
        {
            _app.Messenger.Show("Sync failed", ex.Message, AppMessageKind.Warning);
        }
        finally
        {
            RefreshUi();
        }
    }

    private void OpenAccountButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow main)
        {
            main.NavigateToAccount();
        }
    }

    private void DataExplorer_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        _selectedNode = e.NewValue as SyncDataNode;
    }

    private bool _cascadingSelection;

    private void NodeCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (_cascadingSelection || sender is not CheckBox { DataContext: SyncDataNode node })
        {
            return;
        }

        _cascadingSelection = true;
        try
        {
            _app.CloudSync.SetNodeSelected(node, node.IsSelected, includeChildren: true);
        }
        finally
        {
            _cascadingSelection = false;
        }

        e.Handled = true;
    }

    private void SelectCloudOnly_OnClick(object sender, RoutedEventArgs e)
    {
        _app.CloudSync.SelectAllNewFromCloud();
    }

    private void SelectLocalOnly_OnClick(object sender, RoutedEventArgs e)
    {
        _app.CloudSync.SelectAllLocalOnlyUploads();
    }

    private async void DeleteCloudItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedNode is null)
        {
            _app.Messenger.Show("Nothing selected", "Select a cloud item in the tree first.", AppMessageKind.Info);
            return;
        }

        if (_selectedNode.Status != SyncNodeStatus.CloudOnly &&
            _selectedNode.CloudCount <= 0)
        {
            _app.Messenger.Show("Not cloud-only", "Only cloud-only favorites or presets can be deleted here.", AppMessageKind.Info);
            return;
        }

        var confirm = MessageBox.Show(
            Window.GetWindow(this),
            $"Delete “{_selectedNode.Label}” from your cloud account?",
            "Delete from cloud",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        var result = await _app.CloudSync.DeleteCloudItemAsync(_selectedNode).ConfigureAwait(true);
        if (!result.Success)
        {
            _app.Messenger.Show(result.Title, result.Message, AppMessageKind.Warning);
        }

        RefreshUi();
    }
}
