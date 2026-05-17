using FastTrack.ViewModels;

namespace FastTrack;

public partial class MainPage : ContentPage
{
	private readonly HomeViewModel _vm;

	public MainPage(HomeViewModel vm)
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

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_vm.Dispose();
	}
}
