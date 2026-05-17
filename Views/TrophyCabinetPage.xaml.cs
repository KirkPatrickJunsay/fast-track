using FastTrack.ViewModels;

namespace FastTrack.Views;

public partial class TrophyCabinetPage : ContentPage
{
    private readonly TrophyCabinetViewModel _vm;

    public TrophyCabinetPage(TrophyCabinetViewModel vm)
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
