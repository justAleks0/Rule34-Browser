namespace Rule34Gallery.Maui.Platform;

internal sealed class MauiSecureStore : ISecureCredentialStore
{
    private const string Prefix = "r34s:";

    public string Protect(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var key = Prefix + Guid.NewGuid().ToString("N");
        SecureStorage.SetAsync(key, value).GetAwaiter().GetResult();
        return key;
    }

    public string Unprotect(string protectedValue)
    {
        if (string.IsNullOrEmpty(protectedValue))
        {
            return string.Empty;
        }

        if (!protectedValue.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return protectedValue;
        }

        try
        {
            return SecureStorage.GetAsync(protectedValue).GetAwaiter().GetResult() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
