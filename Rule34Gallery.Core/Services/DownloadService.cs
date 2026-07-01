using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;

namespace Rule34Gallery.Core.Services;

public sealed class DownloadService
{
    private readonly AppServices _app;
    private readonly DownloadHistoryStore _history;
    private readonly SemaphoreSlim _workerLock = new(1, 1);
    private readonly HashSet<int> _activePostIds = [];
    private CancellationTokenSource? _queueCts;

    public DownloadService(AppServices app)
    {
        _app = app;
        _history = new DownloadHistoryStore(app.Platform.AppDataFolder);
        RestoreHistory();
    }

    public ObservableCollection<DownloadJob> Jobs { get; } = [];

    public event EventHandler? JobsChanged;

    public DownloadJob? Enqueue(PostItem post)
    {
        if (post.IsLocal)
        {
            return null;
        }

        var library = ResolveDownloadLibrary();
        if (library is null)
        {
            throw new InvalidOperationException(
                "No download library configured. Set one under Settings → Downloads or create a library on the Local tab.");
        }

        var existing = Jobs.FirstOrDefault(j => j.PostId == post.Id);
        if (existing is not null)
        {
            if (existing.Status is DownloadJobStatus.Queued or DownloadJobStatus.Downloading)
            {
                return existing;
            }

            if (existing.CanRetry)
            {
                RetryJob(existing);
                return existing;
            }
        }

        if (!_activePostIds.Add(post.Id))
        {
            return Jobs.FirstOrDefault(j =>
                j.PostId == post.Id &&
                j.Status is DownloadJobStatus.Queued or DownloadJobStatus.Downloading);
        }

        var target = DownloadPathBuilder.BuildTarget(library, post);
        var job = CreateJobFromPost(post, library, target);
        Jobs.Insert(0, job);
        _app.ForYou.RecordDownload(post);
        NotifyJobsChanged();
        StartQueue();
        return job;
    }

    public void RetryJob(DownloadJob job)
    {
        if (!job.CanRetry)
        {
            return;
        }

        job.PrepareForRetry();
        _activePostIds.Add(job.PostId);
        NotifyJobsChanged();
        StartQueue();
    }

    public void CancelJob(DownloadJob job)
    {
        if (job.Status is DownloadJobStatus.Completed or DownloadJobStatus.Failed or DownloadJobStatus.Cancelled)
        {
            return;
        }

        job.Status = DownloadJobStatus.Cancelled;
        job.StatusText = "Cancelled";
        _activePostIds.Remove(job.PostId);
        NotifyJobsChanged();
    }

    public void ClearFinished()
    {
        for (var i = Jobs.Count - 1; i >= 0; i--)
        {
            var job = Jobs[i];
            if (job.Status is DownloadJobStatus.Completed or DownloadJobStatus.Failed or DownloadJobStatus.Cancelled)
            {
                _activePostIds.Remove(job.PostId);
                Jobs.RemoveAt(i);
            }
        }

        PersistHistory();
        NotifyJobsChanged();
    }

    public LocalLibraryDefinition? ResolveDownloadLibrary()
    {
        var libraries = _app.Settings.LocalLibraries;
        if (libraries.Count == 0)
        {
            return null;
        }

        var id = _app.Settings.DownloadLibraryId;
        if (!string.IsNullOrWhiteSpace(id))
        {
            var match = libraries.FirstOrDefault(l => l.Id == id);
            if (match is not null)
            {
                return match;
            }
        }

        return libraries[0];
    }

    private void RestoreHistory()
    {
        foreach (var entry in _history.Load().OrderByDescending(e => e.UpdatedAt))
        {
            var job = DownloadJob.FromHistory(entry);
            Jobs.Add(job);
            if (job.Status is DownloadJobStatus.Queued or DownloadJobStatus.Downloading)
            {
                _activePostIds.Add(job.PostId);
            }
        }

        if (Jobs.Any(j => j.Status is DownloadJobStatus.Queued or DownloadJobStatus.Downloading))
        {
            StartQueue();
        }
    }

    private static DownloadJob CreateJobFromPost(
        PostItem post,
        LocalLibraryDefinition library,
        DownloadPathBuilder.DownloadTarget target) =>
        new()
        {
            PostId = post.Id,
            SourceUrl = post.FullViewerUrl,
            Tags = post.Tags,
            Rating = post.Rating,
            Score = post.Score,
            Width = post.Width,
            Height = post.Height,
            FileUrl = post.FileUrl,
            SampleUrl = post.SampleUrl,
            PreviewUrl = post.PreviewUrl,
            LibraryName = library.Name,
            RelativeCategory = target.RelativeCategory,
            FileName = target.FileName,
            DestinationDirectory = target.DirectoryPath,
        };

    private void StartQueue()
    {
        _queueCts?.Cancel();
        _queueCts = new CancellationTokenSource();
        _ = ProcessQueueAsync(_queueCts.Token);
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        if (!await _workerLock.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var job = Jobs.FirstOrDefault(j => j.Status == DownloadJobStatus.Queued);
                if (job is null)
                {
                    break;
                }

                await RunJobAsync(job, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Queue restarted.
        }
        finally
        {
            _workerLock.Release();
        }
    }

    private async Task RunJobAsync(DownloadJob job, CancellationToken cancellationToken)
    {
        if (job.Status == DownloadJobStatus.Cancelled)
        {
            _activePostIds.Remove(job.PostId);
            return;
        }

        job.Status = DownloadJobStatus.Downloading;
        job.StatusText = "Starting…";
        job.Progress = 0;
        NotifyJobsChanged();

        try
        {
            Directory.CreateDirectory(job.DestinationDirectory);
            var destination = DownloadPathBuilder.EnsureUniqueFilePath(
                job.DestinationDirectory,
                job.FileName);
            job.SetDestinationPath(destination);

            var url = ResolveDownloadUrl(job);
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new InvalidOperationException("No download URL for this post.");
            }

            using var response = await _app.Http.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            var total = response.Content.Headers.ContentLength;
            await using var network = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var file = File.Create(destination);

            var buffer = new byte[81920];
            long readTotal = 0;
            int read;
            while ((read = await network.ReadAsync(buffer, cancellationToken)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (job.Status == DownloadJobStatus.Cancelled)
                {
                    file.Close();
                    TryDeleteFile(destination);
                    _activePostIds.Remove(job.PostId);
                    NotifyJobsChanged();
                    return;
                }

                await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                readTotal += read;
                if (total > 0)
                {
                    job.Progress = readTotal * 100d / total.Value;
                    job.StatusText = $"{job.Progress:0}%";
                }
                else
                {
                    job.StatusText = $"{readTotal / 1024} KB";
                }
            }

            var library = ResolveDownloadLibrary();
            if (library is not null)
            {
                var post = JobToPost(job);
                var sidecar = DownloadPathBuilder.WriteSidecar(
                    post,
                    destination,
                    library.Name,
                    job.RelativeCategory);
                job.SetSidecarPath(sidecar);
                DownloadPathBuilder.RegisterCategoryInLibrary(
                    library,
                    job.RelativeCategory,
                    job.DestinationDirectory);
                _app.SaveSettings();
            }

            job.Progress = 100;
            job.Status = DownloadJobStatus.Completed;
            job.StatusText = "Saved";
            job.SetError(null);
        }
        catch (Exception ex)
        {
            if (job.Status != DownloadJobStatus.Cancelled)
            {
                job.Status = DownloadJobStatus.Failed;
                job.StatusText = "Failed";
                job.SetError(ex.Message);
                if (!string.IsNullOrWhiteSpace(job.DestinationPath))
                {
                    TryDeleteFile(job.DestinationPath);
                    job.SetDestinationPath(string.Empty);
                }
            }
        }
        finally
        {
            _activePostIds.Remove(job.PostId);
            NotifyJobsChanged();
        }
    }

    private static string ResolveDownloadUrl(DownloadJob job)
    {
        if (!string.IsNullOrWhiteSpace(job.SourceUrl))
        {
            return job.SourceUrl;
        }

        if (!string.IsNullOrWhiteSpace(job.FileUrl))
        {
            return job.FileUrl;
        }

        return job.SampleUrl;
    }

    private static PostItem JobToPost(DownloadJob job) => new()
    {
        Id = job.PostId,
        FileUrl = job.FileUrl,
        SampleUrl = job.SampleUrl,
        PreviewUrl = job.PreviewUrl,
        Tags = job.Tags,
        Rating = job.Rating,
        Score = job.Score,
        Width = job.Width,
        Height = job.Height,
    };

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best effort.
        }
    }

    private void PersistHistory()
    {
        if (Jobs.Count == 0)
        {
            _history.Clear();
            return;
        }

        _history.Save(Jobs.Select(j => j.ToHistory()));
    }

    private void NotifyJobsChanged()
    {
        PersistHistory();
        JobsChanged?.Invoke(this, EventArgs.Empty);
    }
}
