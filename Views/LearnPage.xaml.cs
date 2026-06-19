using FastTrack.ViewModels;

namespace FastTrack.Views;

public partial class LearnPage : ContentPage
{
    private readonly LearnViewModel _vm;

    public LearnPage(LearnViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.Load();
    }
}
