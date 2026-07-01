using Rule34Gallery.Core.Services;

namespace Rule34Gallery.Core;

public sealed partial class UserSettings
{
    public List<SavedTagPreset> SavedSearchPresets { get; set; } = [];

    public SavedTagPreset? FindSavedSearchPreset(string presetId)
        => SavedSearchPresets.FirstOrDefault(p =>
            p.Id.Equals(presetId, StringComparison.OrdinalIgnoreCase));

    public SearchPreset? ResolveSearchPreset(string presetId)
    {
        var builtIn = SearchPresetCatalog.Find(presetId);
        if (builtIn is not null)
        {
            return builtIn;
        }

        var saved = FindSavedSearchPreset(presetId);
        return saved is null ? null : ToSearchPreset(saved);
    }

    public static SearchPreset ToSearchPreset(SavedTagPreset saved)
        => new()
        {
            Id = saved.Id,
            Name = saved.Name,
            Description = "Your saved tags (all must match).",
            Tags = saved.Tags,
        };

    /// <summary>Adds a saved bundle; returns the new preset id, or null if there are no tags.</summary>
    public string? AddSavedSearchPreset(string name, IEnumerable<string> tags)
    {
        var normalizedTags = tags
            .Select(NormalizeTagToken)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedTags.Count == 0)
        {
            return null;
        }

        var displayName = name.Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = normalizedTags.Count <= 3
                ? string.Join(" + ", normalizedTags)
                : string.Join(" + ", normalizedTags.Take(3)) + "…";
        }

        var duplicate = SavedSearchPresets.FirstOrDefault(p =>
            p.Tags.Count == normalizedTags.Count &&
            p.Tags.All(t => normalizedTags.Contains(t, StringComparer.OrdinalIgnoreCase)));

        if (duplicate is not null)
        {
            duplicate.Name = displayName;
            duplicate.Tags = normalizedTags;
            duplicate.UpdatedAtUnix = SavedTagPresetSync.NowUnix();
            return duplicate.Id;
        }

        var preset = new SavedTagPreset
        {
            Id = SavedTagPresetIds.Create(),
            Name = displayName,
            Tags = normalizedTags,
            UpdatedAtUnix = SavedTagPresetSync.NowUnix(),
        };

        SavedSearchPresets.Insert(0, preset);
        return preset.Id;
    }

    public bool UpdateSavedSearchPreset(string presetId, string name, IEnumerable<string> tags)
    {
        var preset = FindSavedSearchPreset(presetId);
        if (preset is null)
        {
            return false;
        }

        var normalizedTags = tags
            .Select(NormalizeTagToken)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedTags.Count == 0)
        {
            return false;
        }

        var displayName = name.Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = normalizedTags.Count <= 3
                ? string.Join(" + ", normalizedTags)
                : string.Join(" + ", normalizedTags.Take(3)) + "…";
        }

        preset.Name = displayName;
        preset.Tags = normalizedTags;
        preset.UpdatedAtUnix = SavedTagPresetSync.NowUnix();
        return true;
    }

    public bool RemoveSavedSearchPreset(string presetId)
    {
        var removed = SavedSearchPresets.RemoveAll(p =>
            p.Id.Equals(presetId, StringComparison.OrdinalIgnoreCase)) > 0;

        if (removed)
        {
            ActiveSearchPresetIds.RemoveAll(id =>
                id.Equals(presetId, StringComparison.OrdinalIgnoreCase));
        }

        return removed;
    }

    public IEnumerable<SearchPreset> GetSavedSearchPresetsAsSearchPresets()
        => SavedSearchPresets.Select(ToSearchPreset);
}
