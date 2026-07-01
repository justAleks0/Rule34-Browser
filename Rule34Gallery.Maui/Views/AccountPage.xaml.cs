using Rule34Gallery.Maui.ViewModels;

namespace Rule34Gallery.Maui.Views;

public partial class AccountPage : ContentPage
{
    public AccountPage()
    {
        InitializeComponent();
        BindingContext = new AccountViewModel();
    }
}
