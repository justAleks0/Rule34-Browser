namespace Rule34Gallery.Maui.Services;

internal static class MauiBootstrap
{
    public static async Task InitializeAsync()
    {
        var configDest = Path.Combine(FileSystem.AppDataDirectory, "firebase-config.json");
        if (!File.Exists(configDest))
        {
            try
            {
                await using var input = await FileSystem.OpenAppPackageFileAsync("firebase-config.json");
                await using var output = File.Create(configDest);
                await input.CopyToAsync(output);
            }
            catch
            {
                // Optional config.
            }
        }
    }
}
