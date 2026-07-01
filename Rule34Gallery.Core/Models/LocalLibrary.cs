namespace Rule34Gallery.Core;

/// <summary>
/// A user-defined local library rooted at a parent folder. Categories are nested folders that contain media.
/// </summary>
public sealed class LocalLibraryDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    /// <summary>Parent folder path (may be hidden). Nested subfolders with media become categories.</summary>
    public string RootFolderPath { get; set; } = string.Empty;

    /// <summary>Legacy field — migrated to <see cref="RootFolderPath"/> on load.</summary>
    public string RootNote { get; set; } = string.Empty;

    public List<LocalCategoryDefinition> Categories { get; set; } = [];
}

public sealed class LocalCategoryDefinition
{
    public string Label { get; set; } = string.Empty;

    /// <summary>Full path to the folder that contains media for this category.</summary>
    public string FolderPath { get; set; } = string.Empty;
}
