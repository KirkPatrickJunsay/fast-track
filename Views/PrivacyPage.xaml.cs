using FastTrack.ViewModels;

namespace FastTrack.Views;

public partial class PrivacyPage : ContentPage
{
    public PrivacyPage(PrivacyViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
