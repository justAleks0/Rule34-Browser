namespace Rule34Gallery.Core.Abstractions;

public interface ISecureCredentialStore
{
    string Protect(string value);

    string Unprotect(string protectedValue);
}
