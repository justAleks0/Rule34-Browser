namespace Rule34Gallery.Maui.Platform;

internal sealed class MauiBrowserLauncher : IBrowserLauncher
{
    public void OpenUrl(string url)
        => Browser.Default.OpenAsync(new Uri(url));
}
