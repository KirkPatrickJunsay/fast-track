using FastTrack.ViewModels;

namespace FastTrack.Views;

public partial class EditFastPage : ContentPage, IQueryAttributable
{
    private readonly EditFastViewModel _vm;

    public EditFastPage(EditFastViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("fastId", out var v) && v is string s)
        {
            _vm.FastId = s;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
