using FastTrack.ViewModels;

namespace FastTrack.Views;

public partial class OnboardingPage : ContentPage
{
    public OnboardingPage(OnboardingViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        vm.OnFinished = () => Shell.Current.GoToAsync("//MainPage");
    }
}
