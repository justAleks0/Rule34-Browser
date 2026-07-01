namespace Rule34Gallery.Core.Remote;

/// <summary>Tracks recent authorized remote API use (phone connected to PC remote).</summary>
public static class RemoteClientSession
{
    private static readonly object Gate = new();
    private static DateTime _lastActivityUtc = DateTime.MinValue;
    private static bool _wasActive;
    private static bool _pendingBecameActive;
    private static bool _pendingBecameInactive;

    public static TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(2);

    public static void RecordActivity()
    {
        lock (Gate)
        {
            _lastActivityUtc = DateTime.UtcNow;
            if (!_wasActive)
            {
                _wasActive = true;
                _pendingBecameActive = true;
            }
        }
    }

    public static bool IsActive
    {
        get
        {
            lock (Gate)
            {
                return _lastActivityUtc != DateTime.MinValue &&
                       DateTime.UtcNow - _lastActivityUtc < IdleTimeout;
            }
        }
    }

    /// <summary>Call from the UI thread (e.g. dispatcher timer) to apply connect/disconnect transitions.</summary>
    public static RemoteSessionTransition ConsumeTransition()
    {
        lock (Gate)
        {
            var active = _lastActivityUtc != DateTime.MinValue &&
                         DateTime.UtcNow - _lastActivityUtc < IdleTimeout;

            if (_wasActive && !active)
            {
                _wasActive = false;
                _pendingBecameInactive = true;
            }

            if (_pendingBecameActive)
            {
                _pendingBecameActive = false;
                return RemoteSessionTransition.BecameActive;
            }

            if (_pendingBecameInactive)
            {
                _pendingBecameInactive = false;
                return RemoteSessionTransition.BecameInactive;
            }

            return RemoteSessionTransition.None;
        }
    }
}

public enum RemoteSessionTransition
{
    None,
    BecameActive,
    BecameInactive,
}
