using FastTrack.ViewModels;

namespace FastTrack.Views;

public partial class AddPastFastPage : ContentPage
{
    private readonly AddPastFastViewModel _vm;

    public AddPastFastPage(AddPastFastViewModel vm)
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
