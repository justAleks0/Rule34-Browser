using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Rule34Gallery.Core;
using Rule34Gallery.Core.Remote;
using Rule34GalleryApp.Services;
namespace Rule34GalleryApp.Views.Pages;
public partial class LocalLibraryPage : Page
{
    private readonly AppServices _app = AppServices.Current;
    private LocalLibraryDefinition? _selectedLibrary;
    private string? _selectedTopFilter = "All";
    private string? _selectedLeafFilter = "All";
    private bool _suppressLeafComboEvents;
    private bool _suppressLibraryEvents;
    public LocalLibraryPage()
    {
        InitializeComponent();
        Gallery.ItemsSource = _app.LocalPosts;
        Gallery.SetEmptyMessage(
            "No local media",
            "Set your stash root and scan folders to load nested categories.");
        Gallery.CardClicked += Gallery_OnCardClicked;
        Gallery.ThumbEditClicked += Gallery_OnThumbEditClicked;
        LocalVideoThumbnailService.ThumbnailUpdated += (_, path) =>
            Dispatcher.Invoke(() => Gallery.RefreshThumbnailsForPath(path));
        SetupExpander.Collapsed += (_, _) =>
        {
            UpdateSetupExpanderSummary();
            Gallery.RefreshViewport();
        };
        SetupExpander.Expanded += (_, _) =>
        {
            UpdateSetupExpanderSummary();
            Gallery.RefreshViewport();
        };
        Loaded += (_, _) =>
        {
            RefreshUi();
            Gallery.RefreshViewport();
        };
    }

    public void RefreshUi()
    {
        ReloadLibraryCombo();
        RebuildCategoryEditors();
        RebuildCategoryFilters();
        UpdateRootFolderStatus();
        UpdateSetupExpanderSummary();
        _ = LoadGalleryAsync();
    }

    private void UpdateSetupExpanderSummary()
    {
        if (SetupExpander.IsExpanded)
        {
            SetupExpanderSummary.Text = "Click to collapse";
            return;
        }

        if (_selectedLibrary is null)
        {
            SetupExpanderSummary.Text = "No library configured";
            return;
        }

        var parts = new List<string> { _selectedLibrary.Name };
        var categoryCount = _selectedLibrary.Categories.Count;
        if (categoryCount > 0)
        {
            parts.Add($"{categoryCount} categor{(categoryCount == 1 ? "y" : "ies")}");
        }

        var fileCount = _app.LocalPosts.Count;
        if (fileCount > 0)
        {
            parts.Add($"{fileCount} file{(fileCount == 1 ? "" : "s")}");
        }

        var filter = LocalLibraryService.DescribeActiveFilter(_selectedTopFilter, _selectedLeafFilter);
        if (!filter.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            parts.Add(filter);
        }

        SetupExpanderSummary.Text = string.Join(" · ", parts);
    }

    private void UpdateCategoriesExpanderSummary()
    {
        if (_selectedLibrary is null)
        {
            CategoriesExpanderSummary.Text = string.Empty;
            return;
        }

        if (_selectedLibrary.Categories.Count == 0)
        {
            CategoriesExpanderSummary.Text = "— scan root folder to discover";
            return;
        }

        var total = _selectedLibrary.Categories.Count;
        var missing = _selectedLibrary.Categories.Count(c => !LocalLibraryService.FolderExists(c.FolderPath));
        CategoriesExpanderSummary.Text = missing == 0
            ? $"({total})"
            : $"({total}, {missing} missing)";
    }
    private void ReloadLibraryCombo()
    {
        _suppressLibraryEvents = true;
        try
        {
            var libraries = _app.Settings.LocalLibraries;
            LibraryCombo.ItemsSource = libraries;
            var hasLibrary = libraries.Count > 0;
            LibraryDetailsPanel.IsEnabled = hasLibrary;
            DeleteLibraryButton.IsEnabled = hasLibrary;
            if (!hasLibrary)
            {
                _selectedLibrary = null;
                LibraryCombo.SelectedItem = null;
                LibraryNameInput.Text = string.Empty;
                RootFolderInput.Text = string.Empty;
                UpdateRootFolderStatus();
                UpdateCategoriesExpanderSummary();
                return;
            }
            _selectedLibrary = libraries.FirstOrDefault(l => l.Id == _selectedLibrary?.Id) ?? libraries[0];
            LibraryCombo.SelectedItem = _selectedLibrary;
            LibraryNameInput.Text = _selectedLibrary.Name;
            RootFolderInput.Text = _selectedLibrary.RootFolderPath;
            UpdateRootFolderStatus();
        }
        finally
        {
            _suppressLibraryEvents = false;
        }
    }
    private void UpdateRootFolderStatus()
    {
        if (_selectedLibrary is null)
        {
            RootFolderStatus.Text = "Click New to create a library.";
            UpdateCategoriesExpanderSummary();
            return;
        }
        var path = LocalLibraryService.NormalizeFolderPath(_selectedLibrary.RootFolderPath);
        if (string.IsNullOrWhiteSpace(path))
        {
            RootFolderStatus.Text = "Set a root folder, then Scan.";
            RootFolderStatus.Foreground = Application.Current.TryFindResource("MutedBrush") as System.Windows.Media.Brush;
            UpdateCategoriesExpanderSummary();
            return;
        }
        if (!LocalLibraryService.FolderExists(path))
        {
            RootFolderStatus.Text = "Root folder not found — check the path.";
            RootFolderStatus.Foreground = Application.Current.TryFindResource("MutedBrush") as System.Windows.Media.Brush;
            UpdateCategoriesExpanderSummary();
            return;
        }
        var count = _selectedLibrary.Categories.Count;
        RootFolderStatus.Text = count == 0
            ? "Root OK — click Scan to find categories"
            : $"Root OK · {count} categor{(count == 1 ? "y" : "ies")}";
        RootFolderStatus.Foreground = Application.Current.TryFindResource("R34GreenBrush") as System.Windows.Media.Brush;
        UpdateCategoriesExpanderSummary();
    }
    private void RebuildCategoryEditors()
    {
        CategoriesList.Items.Clear();
        if (_selectedLibrary is null)
        {
            return;
        }
        foreach (var category in _selectedLibrary.Categories)
        {
            CategoriesList.Items.Add(BuildCategoryEditorRow(category));
        }

        UpdateCategoriesExpanderSummary();
    }
    private UIElement BuildCategoryEditorRow(LocalCategoryDefinition category)
    {
        var exists = LocalLibraryService.FolderExists(category.FolderPath);
        var displayLabel = string.IsNullOrWhiteSpace(category.Label)
            ? System.IO.Path.GetFileName(category.FolderPath.TrimEnd('\\', '/'))
            : category.Label;

        var grid = new Grid
        {
            Margin = new Thickness(0, 0, 0, 4),
            ToolTip = category.FolderPath,
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var statusDot = new TextBlock
        {
            Text = "●",
            FontSize = 10,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            Foreground = exists
                ? Application.Current.TryFindResource("R34GreenBrush") as System.Windows.Media.Brush
                : Application.Current.TryFindResource("MutedBrush") as System.Windows.Media.Brush,
            ToolTip = exists ? "Folder found" : "Folder not found",
        };
        Grid.SetColumn(statusDot, 0);
        grid.Children.Add(statusDot);

        var labelBox = new TextBox
        {
            Text = displayLabel,
            Style = Application.Current.TryFindResource("R34FieldTextBox") as Style,
            ToolTip = category.FolderPath,
            Tag = category,
            Padding = new Thickness(6, 3, 6, 3),
        };
        labelBox.LostFocus += CategoryLabel_OnLostFocus;
        Grid.SetColumn(labelBox, 1);
        grid.Children.Add(labelBox);

        var removeButton = new Button
        {
            Content = "✕",
            Style = Application.Current.TryFindResource("SecondaryButton") as Style,
            Margin = new Thickness(6, 0, 0, 0),
            Padding = new Thickness(8, 2, 8, 2),
            ToolTip = "Remove category",
            Tag = category,
        };
        removeButton.Click += RemoveCategoryButton_OnClick;
        Grid.SetColumn(removeButton, 2);
        grid.Children.Add(removeButton);

        return grid;
    }
    private void RebuildCategoryFilters()
    {
        TopCategoryFilterPanel.Children.Clear();
        LeafCategoryCombo.IsEnabled = false;
        LeafCategoryCombo.ItemsSource = null;

        if (_selectedLibrary is null || _selectedLibrary.Categories.Count == 0)
        {
            TopCategoryFilterPanel.Children.Add(new TextBlock
            {
                Text = "Scan a library to filter",
                Foreground = Application.Current.TryFindResource("MutedBrush") as System.Windows.Media.Brush,
                FontSize = 11,
            });
            return;
        }

        AddTopFilterButton("All", true);
        foreach (var top in _selectedLibrary.Categories
                     .Select(c => LocalLibraryService.GetTopSegment(c.Label))
                     .Where(s => !string.IsNullOrWhiteSpace(s))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
        {
            AddTopFilterButton(top, false);
        }

        RebuildLeafCategoryCombo();
    }

    private void RebuildLeafCategoryCombo()
    {
        if (_selectedLibrary is null)
        {
            return;
        }

        var items = new List<LeafCategoryFilterItem>
        {
            new("All", "All categories"),
        };

        foreach (var category in _selectedLibrary.Categories
                     .Where(c => LocalLibraryService.CategoryUnderTopSegment(c.Label, _selectedTopFilter ?? "All"))
                     .OrderBy(c => c.Label, StringComparer.OrdinalIgnoreCase))
        {
            var label = string.IsNullOrWhiteSpace(category.Label)
                ? System.IO.Path.GetFileName(category.FolderPath.TrimEnd('\\', '/'))
                : category.Label;
            items.Add(new LeafCategoryFilterItem(label, LocalLibraryService.FormatCategoryDisplay(label)));
        }

        _suppressLeafComboEvents = true;
        try
        {
            LeafCategoryCombo.IsEnabled = items.Count > 1;
            LeafCategoryCombo.DisplayMemberPath = nameof(LeafCategoryFilterItem.Display);
            LeafCategoryCombo.SelectedValuePath = nameof(LeafCategoryFilterItem.Label);
            LeafCategoryCombo.ItemsSource = items;

            var selected = items.FirstOrDefault(i =>
                i.Label.Equals(_selectedLeafFilter ?? "All", StringComparison.OrdinalIgnoreCase));
            LeafCategoryCombo.SelectedItem = selected ?? items[0];
            _selectedLeafFilter = (LeafCategoryCombo.SelectedItem as LeafCategoryFilterItem)?.Label ?? "All";
        }
        finally
        {
            _suppressLeafComboEvents = false;
        }
    }

    private void AddTopFilterButton(string label, bool isAll)
    {
        var isSelected = isAll
            ? string.IsNullOrWhiteSpace(_selectedTopFilter) || _selectedTopFilter == "All"
            : label.Equals(_selectedTopFilter, StringComparison.OrdinalIgnoreCase);

        var button = new Button
        {
            Content = label,
            Style = Application.Current.TryFindResource(isSelected ? "PrimaryButton" : "SecondaryButton") as Style,
            Margin = new Thickness(0, 0, 6, 0),
            Padding = new Thickness(10, 4, 10, 4),
            Tag = label,
        };
        button.Click += TopCategoryFilterButton_OnClick;
        TopCategoryFilterPanel.Children.Add(button);
    }

    private sealed class LeafCategoryFilterItem(string label, string display)
    {
        public string Label { get; } = label;
        public string Display { get; } = display;
        public override string ToString() => Display;
    }

    private async Task LoadGalleryAsync()
    {
        if (_selectedLibrary is null)
        {
            _app.LocalPosts.Clear();
            Gallery.UpdateEmptyState();
            StatusText.Text = "Click New library, set a parent folder, then scan folders.";
            return;
        }

        if (_selectedLibrary.Categories.Count == 0)
        {
            _app.LocalPosts.Clear();
            Gallery.UpdateEmptyState();
            StatusText.Text = "No categories yet — set the parent folder and scan folders.";
            return;
        }

        var library = _selectedLibrary;
        var topFilter = _selectedTopFilter;
        var leafFilter = _selectedLeafFilter;
        StatusText.Text = "Scanning folders…";
        LoadingOverlay.Show();

        try
        {
            var posts = await Task.Run(() =>
                LocalLibraryService.ScanLibrary(
                    library,
                    topFilter,
                    leafFilter,
                    _app.Settings,
                    CancellationToken.None));

            await Dispatcher.InvokeAsync(() =>
            {
                _app.LocalPosts.Clear();
                foreach (var post in posts)
                {
                    _app.LocalPosts.Add(post);
                }

                var count = _app.LocalPosts.Count;
                var filterLabel = LocalLibraryService.DescribeActiveFilter(topFilter, leafFilter);
                StatusText.Text = count == 0
                    ? "No media files found in the selected categories."
                    : $"{count} file(s) · {filterLabel}";
                Gallery.UpdateEmptyState();
                UpdateSetupExpanderSummary();
                Gallery.RefreshViewport();
            });
        }
        catch (Exception ex)
        {
            StatusText.Text = ex.Message;
        }
        finally
        {
            LoadingOverlay.Hide();
        }
    }
    private LocalLibraryDefinition? GetSelectedLibrary()
        => LibraryCombo.SelectedItem as LocalLibraryDefinition ?? _selectedLibrary;
    private void SaveSettings()
    {
        _app.SaveSettings();
        RebuildCategoryFilters();
    }
    private bool ApplyRootFolderFromInput(bool scanSubfolders)
    {
        if (_selectedLibrary is null)
        {
            return false;
        }
        var path = LocalLibraryService.NormalizeFolderPath(RootFolderInput.Text);
        var changed = !path.Equals(
            LocalLibraryService.NormalizeFolderPath(_selectedLibrary.RootFolderPath),
            StringComparison.OrdinalIgnoreCase);
        _selectedLibrary.RootFolderPath = path;
        RootFolderInput.Text = path;
        SaveSettings();
        UpdateRootFolderStatus();
        if (!LocalLibraryService.FolderExists(path))
        {
            StatusText.Text = "Parent folder not found — fix the path, then scan again.";
            return false;
        }
        if (scanSubfolders || changed)
        {
            return ScanSubfoldersInternal(showMessage: scanSubfolders || changed);
        }
        return true;
    }
    private bool ScanSubfoldersInternal(bool showMessage)
    {
        if (_selectedLibrary is null)
        {
            return false;
        }
        if (!LocalLibraryService.FolderExists(_selectedLibrary.RootFolderPath))
        {
            StatusText.Text = "Set a valid parent folder first.";
            UpdateRootFolderStatus();
            return false;
        }
        var count = LocalLibraryService.ApplyDiscoveredCategories(_selectedLibrary);
        SaveSettings();
        RebuildCategoryEditors();
        RebuildCategoryFilters();
        UpdateRootFolderStatus();
        if (showMessage)
        {
            StatusText.Text = count == 0
                ? "No folders with media found under the parent folder."
                : $"Found {count} categor{(count == 1 ? "y" : "ies")} (nested folders with media).";
        }
        _ = LoadGalleryAsync();
        return count > 0;
    }
    private void NewLibraryButton_OnClick(object sender, RoutedEventArgs e)
    {
        var library = new LocalLibraryDefinition
        {
            Name = $"Library {_app.Settings.LocalLibraries.Count + 1}",
        };
        _app.Settings.LocalLibraries.Add(library);
        if (string.IsNullOrWhiteSpace(_app.Settings.DownloadLibraryId))
        {
            _app.Settings.DownloadLibraryId = library.Id;
        }

        _selectedLibrary = library;
        ResetCategoryFilters();
        SaveSettings();
        RefreshUi();
    }
    private void DeleteLibraryButton_OnClick(object sender, RoutedEventArgs e)
    {
        var library = GetSelectedLibrary();
        if (library is null)
        {
            return;
        }
        _app.Settings.LocalLibraries.RemoveAll(l => l.Id == library.Id);
        _selectedLibrary = _app.Settings.LocalLibraries.FirstOrDefault();
        ResetCategoryFilters();
        SaveSettings();
        RefreshUi();
    }
    private void LibraryCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressLibraryEvents)
        {
            return;
        }
        _selectedLibrary = LibraryCombo.SelectedItem as LocalLibraryDefinition;
        if (_selectedLibrary is not null)
        {
            LibraryNameInput.Text = _selectedLibrary.Name;
            RootFolderInput.Text = _selectedLibrary.RootFolderPath;
        }
        ResetCategoryFilters();
        RebuildCategoryEditors();
        RebuildCategoryFilters();
        UpdateRootFolderStatus();
        _ = LoadGalleryAsync();
    }
    private void LibraryNameInput_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (_selectedLibrary is null)
        {
            return;
        }
        _selectedLibrary.Name = LibraryNameInput.Text.Trim();
        SaveSettings();
        ReloadLibraryCombo();
    }
    private void RootFolderInput_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (_selectedLibrary is null)
        {
            return;
        }
        var path = LocalLibraryService.NormalizeFolderPath(RootFolderInput.Text);
        if (path.Equals(
                LocalLibraryService.NormalizeFolderPath(_selectedLibrary.RootFolderPath),
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        _selectedLibrary.RootFolderPath = path;
        SaveSettings();
        UpdateRootFolderStatus();
        if (LocalLibraryService.FolderExists(path))
        {
            ScanSubfoldersInternal(showMessage: false);
        }
    }
    private void BrowseRootFolderButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedLibrary is null)
        {
            NewLibraryButton_OnClick(sender, e);
            if (_selectedLibrary is null)
            {
                return;
            }
        }
        var dialog = new OpenFolderDialog
        {
            Title = "Select parent library folder",
        };
        var current = LocalLibraryService.NormalizeFolderPath(RootFolderInput.Text);
        if (LocalLibraryService.FolderExists(current))
        {
            dialog.InitialDirectory = current;
        }
        if (dialog.ShowDialog() == true)
        {
            RootFolderInput.Text = LocalLibraryService.NormalizeFolderPath(dialog.FolderName);
            ApplyRootFolderFromInput(scanSubfolders: true);
        }
    }
    private void ScanSubfoldersButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedLibrary is null)
        {
            NewLibraryButton_OnClick(sender, e);
        }
        ApplyRootFolderFromInput(scanSubfolders: true);
    }
    private void AddCategoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedLibrary is null)
        {
            return;
        }
        var dialog = new OpenFolderDialog
        {
            Title = "Select a category subfolder",
        };
        var root = LocalLibraryService.NormalizeFolderPath(_selectedLibrary.RootFolderPath);
        if (LocalLibraryService.FolderExists(root))
        {
            dialog.InitialDirectory = root;
        }
        if (dialog.ShowDialog() != true)
        {
            return;
        }
        var path = LocalLibraryService.NormalizeFolderPath(dialog.FolderName);
        var label = LocalLibraryService.GetRelativeCategoryLabel(_selectedLibrary.RootFolderPath, path);
        if (string.IsNullOrWhiteSpace(label))
        {
            label = System.IO.Path.GetFileName(path.TrimEnd('\\', '/'));
        }
        if (_selectedLibrary.Categories.Any(c =>
                LocalLibraryService.NormalizeFolderPath(c.FolderPath)
                    .Equals(path, StringComparison.OrdinalIgnoreCase)))
        {
            StatusText.Text = "That folder is already a category.";
            return;
        }
        _selectedLibrary.Categories.Add(new LocalCategoryDefinition
        {
            Label = label,
            FolderPath = path,
        });
        SaveSettings();
        RebuildCategoryEditors();
        RebuildCategoryFilters();
        UpdateRootFolderStatus();
        _ = LoadGalleryAsync();
    }
    private void CategoryLabel_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox box && box.Tag is LocalCategoryDefinition category)
        {
            category.Label = box.Text.Trim();
            if (string.IsNullOrWhiteSpace(category.Label) && !string.IsNullOrWhiteSpace(category.FolderPath))
            {
                category.Label = System.IO.Path.GetFileName(
                    category.FolderPath.TrimEnd('\\', '/'));
            }
            SaveSettings();
            RebuildCategoryFilters();
        }
    }
    private void RemoveCategoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_selectedLibrary is null || sender is not Button { Tag: LocalCategoryDefinition category })
        {
            return;
        }
        _selectedLibrary.Categories.Remove(category);
        SaveSettings();
        RebuildCategoryEditors();
        RebuildCategoryFilters();
        UpdateRootFolderStatus();
        _ = LoadGalleryAsync();
    }
    private void ResetCategoryFilters()
    {
        _selectedTopFilter = "All";
        _selectedLeafFilter = "All";
    }

    private void TopCategoryFilterButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string label })
        {
            return;
        }

        _selectedTopFilter = label;
        _selectedLeafFilter = "All";
        RebuildCategoryFilters();
        _ = LoadGalleryAsync();
    }

    private void LeafCategoryCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressLeafComboEvents)
        {
            return;
        }

        _selectedLeafFilter = (LeafCategoryCombo.SelectedItem as LeafCategoryFilterItem)?.Label ?? "All";
        _ = LoadGalleryAsync();
    }
    private void Gallery_OnCardClicked(object? sender, PostItem post)
    {
        var index = _app.LocalPosts.IndexOf(post);
        if (index < 0)
        {
            return;
        }
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.OpenLocalViewer(index);
        }
    }

    private void Gallery_OnThumbEditClicked(object? sender, PostItem post)
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
        {
            mainWindow.EditThumbnail(post);
        }
    }

    public RemoteLocalState CaptureRemoteState()
    {
        var libraries = _app.Settings.LocalLibraries
            .Select(l => new RemoteLocalLibrarySummary { Id = l.Id, Name = l.Name })
            .ToList();

        var topFilters = new List<string> { "All" };
        var leafFilters = new List<string> { "All" };
        if (_selectedLibrary is not null && _selectedLibrary.Categories.Count > 0)
        {
            topFilters.AddRange(_selectedLibrary.Categories
                .Select(c => LocalLibraryService.GetTopSegment(c.Label))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase));

            leafFilters.AddRange(_selectedLibrary.Categories
                .Where(c => LocalLibraryService.CategoryUnderTopSegment(c.Label, _selectedTopFilter ?? "All"))
                .Select(c => string.IsNullOrWhiteSpace(c.Label)
                    ? Path.GetFileName(c.FolderPath.TrimEnd('\\', '/'))
                    : c.Label)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase));
        }

        return new RemoteLocalState
        {
            Libraries = libraries,
            SelectedLibraryId = _selectedLibrary?.Id,
            SelectedLibraryName = _selectedLibrary?.Name,
            TopFilters = topFilters,
            LeafFilters = leafFilters,
            TopFilter = _selectedTopFilter ?? "All",
            LeafFilter = _selectedLeafFilter ?? "All",
            PostCount = _app.LocalPosts.Count,
        };
    }

    public async Task RemoteSelectLibraryAsync(string libraryId)
    {
        var library = _app.Settings.LocalLibraries.FirstOrDefault(l => l.Id == libraryId);
        if (library is null)
        {
            throw new ArgumentException("Library not found.");
        }

        _suppressLibraryEvents = true;
        try
        {
            LibraryCombo.SelectedItem = library;
            _selectedLibrary = library;
        }
        finally
        {
            _suppressLibraryEvents = false;
        }

        ResetCategoryFilters();
        RebuildCategoryEditors();
        RebuildCategoryFilters();
        UpdateRootFolderStatus();
        UpdateSetupExpanderSummary();
        await LoadGalleryAsync();
    }

    public async Task RemoteSelectTopFilterAsync(string filter)
    {
        if (_selectedLibrary is null)
        {
            throw new InvalidOperationException("Select a local library on the PC first.");
        }

        _selectedTopFilter = string.IsNullOrWhiteSpace(filter) ? "All" : filter.Trim();
        _selectedLeafFilter = "All";
        RebuildCategoryFilters();
        await LoadGalleryAsync();
    }

    public async Task RemoteSelectLeafFilterAsync(string filter)
    {
        if (_selectedLibrary is null)
        {
            throw new InvalidOperationException("Select a local library on the PC first.");
        }

        _selectedLeafFilter = string.IsNullOrWhiteSpace(filter) ? "All" : filter.Trim();
        await LoadGalleryAsync();
    }
}
