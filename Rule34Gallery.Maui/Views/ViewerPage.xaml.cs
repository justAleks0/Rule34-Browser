using Rule34Gallery.Maui.ViewModels;

namespace Rule34Gallery.Maui.Views;

public partial class ViewerPage : ContentPage
{
    private readonly ViewerViewModel _vm;

    public ViewerPage()
    {
        InitializeComponent();
        BindingContext = _vm = new ViewerViewModel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.LoadCurrent();
    }
}
