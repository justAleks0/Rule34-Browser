using Rule34Gallery.Maui.ViewModels;

namespace Rule34Gallery.Maui.Views;

public partial class PresetPickerPage : ContentPage
{
    public PresetPickerPage()
    {
        InitializeComponent();
        BindingContext = new PresetPickerViewModel();
    }
}
