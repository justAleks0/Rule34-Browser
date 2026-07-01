using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Rule34GalleryApp.Controls;

public partial class RemovableTagChip : UserControl
{
    public static readonly DependencyProperty TagTextProperty =
        DependencyProperty.Register(nameof(TagText), typeof(string), typeof(RemovableTagChip),
            new PropertyMetadata(string.Empty, OnVisualChanged));

    public static readonly DependencyProperty CategoryProperty =
        DependencyProperty.Register(nameof(Category), typeof(TagCategory), typeof(RemovableTagChip),
            new PropertyMetadata(TagCategory.General, OnVisualChanged));

    public static readonly DependencyProperty IsExcludedProperty =
        DependencyProperty.Register(nameof(IsExcluded), typeof(bool), typeof(RemovableTagChip),
            new PropertyMetadata(false, OnVisualChanged));

    public static readonly DependencyProperty IsBundleProperty =
        DependencyProperty.Register(nameof(IsBundle), typeof(bool), typeof(RemovableTagChip),
            new PropertyMetadata(false, OnVisualChanged));

    public event RoutedEventHandler? RemoveClicked;

    public RemovableTagChip()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyStyle();
    }

    public string TagText
    {
        get => (string)GetValue(TagTextProperty);
        set => SetValue(TagTextProperty, value);
    }

    public TagCategory Category
    {
        get => (TagCategory)GetValue(CategoryProperty);
        set => SetValue(CategoryProperty, value);
    }

    public bool IsExcluded
    {
        get => (bool)GetValue(IsExcludedProperty);
        set => SetValue(IsExcludedProperty, value);
    }

    public bool IsBundle
    {
        get => (bool)GetValue(IsBundleProperty);
        set => SetValue(IsBundleProperty, value);
    }

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RemovableTagChip chip)
        {
            chip.ApplyStyle();
        }
    }

    private void ApplyStyle()
    {
        if (IsBundle)
        {
            ChipText.Text = TagText;
            ChipBorder.Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x2a, 0x14));
            ChipBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x6b, 0x9e, 0x3a));
            ChipText.Foreground = new SolidColorBrush(Color.FromRgb(0xc8, 0xf0, 0x90));
            return;
        }

        ChipText.Text = IsExcluded ? "-" + TagText.TrimStart('-') : TagText;

        if (IsExcluded)
        {
            ChipBorder.Background = new SolidColorBrush(Color.FromRgb(0x3a, 0x1a, 0x1a));
            ChipBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x8a, 0x3a, 0x3a));
            ChipText.Foreground = new SolidColorBrush(Color.FromRgb(0xff, 0x99, 0x99));
        }
        else
        {
            ChipBorder.Background = WpfTagBrushes.Background(Category);
            ChipBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44));
            ChipText.Foreground = WpfTagBrushes.Foreground(Category);
        }
    }

    private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
    {
        RemoveClicked?.Invoke(this, e);
        e.Handled = true;
    }
}
