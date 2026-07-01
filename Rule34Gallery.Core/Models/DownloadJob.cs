using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Rule34Gallery.Core;

public enum DownloadJobStatus
{
    Queued,
    Downloading,
    Completed,
    Failed,
    Cancelled,
}

public sealed class DownloadJob : INotifyPropertyChanged
{
    private DownloadJobStatus _status = DownloadJobStatus.Queued;
    private double _progress;
    private string _statusText = "Queued";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public int PostId { get; init; }

    public string SourceUrl { get; init; } = string.Empty;

    public string Tags { get; init; } = string.Empty;

    public string Rating { get; init; } = string.Empty;

    public int Score { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public string FileUrl { get; init; } = string.Empty;

    public string SampleUrl { get; init; } = string.Empty;

    public string PreviewUrl { get; init; } = string.Empty;

    public string LibraryName { get; init; } = string.Empty;

    public string RelativeCategory { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string DestinationDirectory { get; init; } = string.Empty;

    public string DestinationPath { get; private set; } = string.Empty;

    public string? SidecarPath { get; private set; }

    public string? ErrorMessage { get; private set; }

    public DownloadJobStatus Status
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
            OnPropertyChanged(nameof(CanRetry));
        }
    }

    public bool CanRetry =>
        Status is DownloadJobStatus.Failed or DownloadJobStatus.Cancelled;

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

    public string DisplayTitle => $"Post #{PostId}";

    public string DisplayPath =>
        string.IsNullOrWhiteSpace(RelativeCategory)
            ? FileName
            : $"{RelativeCategory}\\{FileName}";

    public string StatusLabel => Status switch
    {
        DownloadJobStatus.Queued => "Queued",
        DownloadJobStatus.Downloading => "Downloading",
        DownloadJobStatus.Completed => "Complete",
        DownloadJobStatus.Failed => "Failed",
        DownloadJobStatus.Cancelled => "Cancelled",
        _ => Status.ToString(),
    };

    public void SetDestinationPath(string path)
    {
        DestinationPath = path;
        OnPropertyChanged(nameof(DestinationPath));
        OnPropertyChanged(nameof(DisplayPath));
    }

    public void SetSidecarPath(string? path)
    {
        SidecarPath = path;
        OnPropertyChanged(nameof(SidecarPath));
    }

    public void SetError(string? message)
    {
        ErrorMessage = message;
        OnPropertyChanged(nameof(ErrorMessage));
    }

    public void PrepareForRetry()
    {
        SetError(null);
        SetDestinationPath(string.Empty);
        SetSidecarPath(null);
        Progress = 0;
        StatusText = "Queued";
        Status = DownloadJobStatus.Queued;
    }

    public static DownloadJob FromHistory(DownloadHistoryEntry entry)
    {
        var job = new DownloadJob
        {
            Id = entry.Id,
            PostId = entry.PostId,
            SourceUrl = entry.SourceUrl,
            FileUrl = entry.FileUrl,
            SampleUrl = entry.SampleUrl,
            PreviewUrl = entry.PreviewUrl,
            Tags = entry.Tags,
            Rating = entry.Rating,
            Score = entry.Score,
            Width = entry.Width,
            Height = entry.Height,
            LibraryName = entry.LibraryName,
            RelativeCategory = entry.RelativeCategory,
            FileName = entry.FileName,
            DestinationDirectory = entry.DestinationDirectory,
        };

        if (!string.IsNullOrWhiteSpace(entry.DestinationPath))
        {
            job.SetDestinationPath(entry.DestinationPath);
        }

        job.SetSidecarPath(entry.SidecarPath);
        job.SetError(entry.ErrorMessage);
        job.Progress = entry.Progress;
        job.StatusText = entry.StatusText;
        job.Status = entry.Status is DownloadJobStatus.Downloading
            ? DownloadJobStatus.Queued
            : entry.Status;
        return job;
    }

    public DownloadHistoryEntry ToHistory() => new()
    {
        Id = Id,
        PostId = PostId,
        SourceUrl = SourceUrl,
        FileUrl = FileUrl,
        SampleUrl = SampleUrl,
        PreviewUrl = PreviewUrl,
        Tags = Tags,
        Rating = Rating,
        Score = Score,
        Width = Width,
        Height = Height,
        LibraryName = LibraryName,
        RelativeCategory = RelativeCategory,
        FileName = FileName,
        DestinationDirectory = DestinationDirectory,
        DestinationPath = DestinationPath,
        SidecarPath = SidecarPath,
        ErrorMessage = ErrorMessage,
        Status = Status,
        Progress = Progress,
        StatusText = StatusText,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
