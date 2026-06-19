using FastTrack.ViewModels;

namespace FastTrack.Views;

/// <summary>
/// Cold-launch entry point. Decides the user's actual destination
/// (MainPage / OnboardingPage) without ever painting either of them first.
/// </summary>
public partial class BootPage : ContentPage
{
    private readonly BootViewModel _vm;
    private bool _routed;

    public BootPage(BootViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_routed) return; // OnAppearing fires again when the user navigates back; we never want to re-route.
        _routed = true;

        var route = await _vm.DecideRouteAsync();
        await Shell.Current.GoToAsync(route);
    }
}
