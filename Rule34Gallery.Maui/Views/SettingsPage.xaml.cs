using Rule34Gallery.Maui.ViewModels;

namespace Rule34Gallery.Maui.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        BindingContext = new SettingsViewModel();
    }
}
