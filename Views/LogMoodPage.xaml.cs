using FastTrack.ViewModels;

namespace FastTrack.Views;

public partial class LogMoodPage : ContentPage, IQueryAttributable
{
    private readonly LogMoodViewModel _vm;

    public LogMoodPage(LogMoodViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("fastId", out var v) && v is string s) _vm.FastId = s;
    }
}
