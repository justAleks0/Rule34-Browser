using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Rule34Gallery.Core.CloudSync;

public sealed class SyncDataNode : INotifyPropertyChanged
{
    private bool _isSelected = true;

    private bool _isExpanded;

    public string Id { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public SyncNodeKind Kind { get; set; } = SyncNodeKind.Category;

    public SyncDataCategory? Category { get; set; }

    public SyncNodeStatus Status { get; set; } = SyncNodeStatus.Both;

    public int LocalCount { get; set; }

    public int CloudCount { get; set; }

    public string Detail { get; set; } = string.Empty;

    public string CountsSummary => $"local {LocalCount} · cloud {CloudCount}";

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
            {
                return;
            }

            _isExpanded = value;
            OnPropertyChanged();
        }
    }

    public List<SyncDataNode> Children { get; set; } = [];

    /// <summary>All leaf sync ids under this container (includes items not shown in the tree).</summary>
    public List<string> DescendantLeafIds { get; set; } = [];

    /// <summary>False for overflow summary rows that are not individually selectable.</summary>
    public bool IsSelectable { get; set; } = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void SetIsSelectedSilently(bool value) => _isSelected = value;

    public void NotifyIsSelectedChanged() => OnPropertyChanged(nameof(IsSelected));

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
