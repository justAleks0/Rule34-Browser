namespace Rule34Gallery.Core.Services;

public enum AppMessageKind
{
    Info,
    Warning,
    Error,
}

public sealed class AppToastMessage
{
    public string Title { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public AppMessageKind Kind { get; init; }
}

public sealed class AppMessenger
{
    public event EventHandler<AppToastMessage>? ToastRequested;

    public event EventHandler? ToastDismissed;

    public event EventHandler<bool>? CredentialHighlightChanged;

    public void Show(string title, string body, AppMessageKind kind = AppMessageKind.Info)
        => ToastRequested?.Invoke(this, new AppToastMessage { Title = title, Body = body, Kind = kind });

    public void Dismiss() => ToastDismissed?.Invoke(this, EventArgs.Empty);

    public void SetCredentialHighlight(bool highlighted)
        => CredentialHighlightChanged?.Invoke(this, highlighted);
}
