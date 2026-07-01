using Rule34Gallery.Maui.ViewModels;

namespace Rule34Gallery.Maui.Views;

public partial class TagDiscoverPage : ContentPage
{
    public TagDiscoverPage()
    {
        InitializeComponent();
        BindingContext = new TagDiscoverViewModel();
    }
}
