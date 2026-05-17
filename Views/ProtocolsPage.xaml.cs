using FastTrack.ViewModels;

namespace FastTrack.Views;

public partial class ProtocolsPage : ContentPage
{
    private readonly ProtocolsViewModel _vm;

    public ProtocolsPage(ProtocolsViewModel vm)
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
