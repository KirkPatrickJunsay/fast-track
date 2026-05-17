using FastTrack.ViewModels;

namespace FastTrack.Views;

public partial class DataManagementPage : ContentPage
{
    public DataManagementPage(DataManagementViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
