using FastTrack.ViewModels;

namespace FastTrack.Views;

public partial class StageDetailPage : ContentPage, IQueryAttributable
{
	private readonly StageDetailViewModel _vm;

	public StageDetailPage(StageDetailViewModel vm)
	{
		InitializeComponent();
		_vm = vm;
		BindingContext = vm;
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		query.TryGetValue("stageKey", out var key);
		_vm.LoadFromKey(key as string);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		HeroImage.Scale = 0.5;
		HeroImage.Opacity = 0;
		await Task.WhenAll(
			HeroImage.FadeTo(1, 220, Easing.SinOut),
			HeroImage.ScaleTo(1.0, 380, Easing.SpringOut));
	}
}
