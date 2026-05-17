using FastTrack.ViewModels;

namespace FastTrack.Views;

public partial class LogWeightPage : ContentPage
{
    public LogWeightPage(LogWeightViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
