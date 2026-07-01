using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Rule34GalleryApp.Controls;

public partial class TagChip : UserControl
{
    public static readonly DependencyProperty TagTextProperty =
        DependencyProperty.Register(nameof(TagText), typeof(string), typeof(TagChip),
            new PropertyMetadata(string.Empty, OnTagChanged));

    public static readonly DependencyProperty CategoryProperty =
        DependencyProperty.Register(nameof(Category), typeof(TagCategory), typeof(TagChip),
            new PropertyMetadata(TagCategory.General, OnTagChanged));

    public event RoutedEventHandler? TagClicked;

    public TagChip()
    {
        InitializeComponent();
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

    private static void OnTagChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TagChip chip)
        {
            chip.ApplyStyle();
        }
    }

    private void ApplyStyle()
    {
        ChipText.Text = TagText;
        ChipBorder.Background = WpfTagBrushes.Background(Category);
        ChipText.Foreground = WpfTagBrushes.Foreground(Category);
    }

    private void ChipBorder_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TagText))
        {
            return;
        }

        TagClicked?.Invoke(this, e);
        e.Handled = true;
    }
}
