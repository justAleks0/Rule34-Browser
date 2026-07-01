using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Rule34Gallery.Core;

public enum CloudSyncStepStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped,
}

public sealed class CloudSyncStep : INotifyPropertyChanged
{
    private CloudSyncStepStatus _status = CloudSyncStepStatus.Pending;
    private double _progress;
    private string _statusText = "Waiting";
    private string _detail = string.Empty;
    private string? _errorMessage;

    public event PropertyChangedEventHandler? PropertyChanged;

    public required string Id { get; init; }

    public required string Label { get; init; }

    public required string Direction { get; init; }

    public CloudSyncStepStatus Status
    {
        get => _status;
        set
        {
            if (_status == value)
            {
                return;
            }

            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusLabel));
        }
    }

    public double Progress
    {
        get => _progress;
        set
        {
            var clamped = Math.Clamp(value, 0, 100);
            if (Math.Abs(_progress - clamped) < 0.01)
            {
                return;
            }

            _progress = clamped;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get => _statusText;
        set
        {
            if (_statusText == value)
            {
                return;
            }

            _statusText = value;
            OnPropertyChanged();
        }
    }

    public string Detail
    {
        get => _detail;
        set
        {
            if (_detail == value)
            {
                return;
            }

            _detail = value;
            OnPropertyChanged();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage == value)
            {
                return;
            }

            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public string DisplayLine => string.IsNullOrWhiteSpace(Detail)
        ? Label
        : $"{Label} — {Detail}";

    public string StatusLabel => Status switch
    {
        CloudSyncStepStatus.Pending => "Waiting",
        CloudSyncStepStatus.Running => "Syncing",
        CloudSyncStepStatus.Completed => "Done",
        CloudSyncStepStatus.Failed => "Failed",
        CloudSyncStepStatus.Skipped => "Skipped",
        _ => Status.ToString(),
    };

    public void SetPending()
    {
        Status = CloudSyncStepStatus.Pending;
        Progress = 0;
        StatusText = "Waiting";
        Detail = string.Empty;
        ErrorMessage = null;
    }

    public void SetRunning(string statusText = "Syncing…")
    {
        Status = CloudSyncStepStatus.Running;
        Progress = 40;
        StatusText = statusText;
        ErrorMessage = null;
    }

    public void SetCompleted(string detail, string statusText = "Done")
    {
        Status = CloudSyncStepStatus.Completed;
        Progress = 100;
        StatusText = statusText;
        Detail = detail;
        ErrorMessage = null;
    }

    public void SetFailed(string message)
    {
        Status = CloudSyncStepStatus.Failed;
        StatusText = "Failed";
        ErrorMessage = message;
    }

    public void SetSkipped(string detail)
    {
        Status = CloudSyncStepStatus.Skipped;
        Progress = 100;
        StatusText = "Skipped";
        Detail = detail;
        ErrorMessage = null;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
