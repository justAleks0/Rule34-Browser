namespace Rule34Gallery.Core.Firebase;

public sealed class CredentialSyncResult
{
    public bool Uploaded { get; init; }

    public bool Downloaded { get; init; }

    public string Detail { get; init; } = string.Empty;
}
