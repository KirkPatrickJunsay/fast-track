using FastTrack.ViewModels;

namespace FastTrack.Views;

public partial class CustomizeHomePage : ContentPage
{
    private readonly CustomizeHomeViewModel _vm;

    public CustomizeHomePage(CustomizeHomeViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
