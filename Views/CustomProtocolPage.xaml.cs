using FastTrack.ViewModels;

namespace FastTrack.Views;

public partial class CustomProtocolPage : ContentPage
{
    public CustomProtocolPage(CustomProtocolViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        vm.OnSaved = () => Shell.Current.GoToAsync("..");
    }
}
