namespace Rule34Gallery.Core.Services;

public static class SavedTagPresetIds
{
    public const string Prefix = "saved:";

    public static string Create() => Prefix + Guid.NewGuid().ToString("N");

    public static bool IsSaved(string? id)
        => !string.IsNullOrWhiteSpace(id) &&
           id.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
}
