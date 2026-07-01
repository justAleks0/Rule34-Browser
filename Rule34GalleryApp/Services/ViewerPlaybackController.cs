using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Rule34GalleryApp.Services;

/// <summary>
/// Unified video/GIF playback and seeking through one <see cref="MediaElement"/>.
/// </summary>
public sealed class ViewerPlaybackController : IDisposable
{
    private readonly MediaElement _media;
    private readonly DispatcherTimer _clock;
    private int _seekGeneration;
    private bool _isLoaded;
    private bool _wantsPlay;

    public ViewerPlaybackController(MediaElement media)
    {
        _media = media;
        _media.LoadedBehavior = MediaState.Manual;
        _media.UnloadedBehavior = MediaState.Stop;
        _media.ScrubbingEnabled = true;
        _media.MediaOpened += OnMediaOpened;
        _media.MediaEnded += OnMediaEnded;
        _media.MediaFailed += OnMediaFailed;

        _clock = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _clock.Tick += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Changed;
    public event EventHandler? Opened;
    public event EventHandler? Ended;
    public event EventHandler? MediaFailed;

    public bool IsLoaded => _isLoaded;

    public bool IsPlaying => _wantsPlay;

    public bool IsScrubbing { get; private set; }

    public TimeSpan Position => _isLoaded ? _media.Position : TimeSpan.Zero;

    public TimeSpan Duration =>
        _isLoaded && _media.NaturalDuration.HasTimeSpan
            ? _media.NaturalDuration.TimeSpan
            : TimeSpan.Zero;

    public double Volume
    {
        get => _media.Volume;
        set => _media.Volume = Math.Clamp(value, 0, 1);
    }

    public bool IsMuted
    {
        get => _media.IsMuted;
        set => _media.IsMuted = value;
    }

    public double SpeedRatio
    {
        get => _media.SpeedRatio;
        set => _media.SpeedRatio = Math.Clamp(value, 0.1, 4);
    }

    public void Open(Uri source)
    {
        _seekGeneration++;
        _clock.Stop();
        _isLoaded = false;
        _wantsPlay = true;
        IsScrubbing = false;

        _media.Stop();
        _media.Close();
        _media.Source = source;
        _media.Visibility = Visibility.Visible;

        // Manual MediaElement does not raise MediaOpened until playback starts.
        _media.Play();
    }

    public void Close()
    {
        _seekGeneration++;
        _clock.Stop();
        _isLoaded = false;
        _wantsPlay = false;
        IsScrubbing = false;

        _media.Stop();
        _media.Close();
        _media.Source = null;
        _media.Visibility = Visibility.Collapsed;
    }

    public void Play()
    {
        if (_media.Source is null)
        {
            return;
        }

        _wantsPlay = true;
        _media.Play();
        if (_isLoaded)
        {
            _clock.Start();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Pause()
    {
        _wantsPlay = false;
        if (_media.Source is not null)
        {
            _media.Pause();
        }

        _clock.Stop();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void TogglePlayPause()
    {
        if (_wantsPlay)
        {
            Pause();
        }
        else
        {
            Play();
        }
    }

    public void SeekRelative(TimeSpan offset)
    {
        if (!_isLoaded)
        {
            return;
        }

        var target = Position + offset;
        SeekTo(target, resume: _wantsPlay);
    }

    public void SeekTo(TimeSpan target, bool? resume = null)
    {
        if (!_isLoaded)
        {
            return;
        }

        var shouldResume = resume ?? _wantsPlay;
        _ = ApplyPositionAsync(Clamp(target), shouldResume);
    }

    public void BeginScrub()
    {
        if (!_isLoaded)
        {
            return;
        }

        IsScrubbing = true;
        _wantsPlay = false;
        _media.Pause();
        _clock.Stop();
    }

    public void ScrubTo(TimeSpan target)
    {
        if (!_isLoaded || !IsScrubbing)
        {
            return;
        }

        _media.Position = Clamp(target);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void EndScrub(TimeSpan target, bool resume)
    {
        if (!_isLoaded)
        {
            IsScrubbing = false;
            return;
        }

        IsScrubbing = false;
        SeekTo(Clamp(target), resume);
    }

    public void Dispose() => Close();

    private void OnMediaOpened(object? sender, EventArgs e)
    {
        _isLoaded = true;
        if (_wantsPlay)
        {
            _media.Play();
            _clock.Start();
        }

        Opened?.Invoke(this, EventArgs.Empty);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnMediaEnded(object? sender, EventArgs e)
    {
        _wantsPlay = false;
        _clock.Stop();
        Ended?.Invoke(this, EventArgs.Empty);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnMediaFailed(object? sender, ExceptionRoutedEventArgs e)
    {
        _isLoaded = false;
        _wantsPlay = false;
        _clock.Stop();
        MediaFailed?.Invoke(this, EventArgs.Empty);
    }

    private TimeSpan Clamp(TimeSpan value)
    {
        if (value < TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var duration = Duration;
        return duration > TimeSpan.Zero && value > duration ? duration : value;
    }

    private async Task ApplyPositionAsync(TimeSpan target, bool resume)
    {
        var token = ++_seekGeneration;
        var wasPlaying = _wantsPlay;

        _wantsPlay = false;
        _media.Pause();
        _clock.Stop();

        _media.Position = target;

        for (var attempt = 0; attempt < 40 && token == _seekGeneration; attempt++)
        {
            if (Math.Abs((_media.Position - target).TotalMilliseconds) <= 50)
            {
                break;
            }

            await Task.Delay(25).ConfigureAwait(true);
            if (token != _seekGeneration)
            {
                return;
            }

            _media.Position = target;
        }

        if (token != _seekGeneration)
        {
            return;
        }

        if (resume && wasPlaying)
        {
            Play();
        }
        else
        {
            _media.Pause();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
