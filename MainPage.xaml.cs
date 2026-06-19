using FastTrack.Helpers;
using FastTrack.ViewModels;

namespace FastTrack;

public partial class MainPage : ContentPage
{
	private readonly HomeViewModel _vm;
	private readonly FastingRingDrawable _ring = new();

	public MainPage(HomeViewModel vm)
	{
		InitializeComponent();
		_vm = vm;
		BindingContext = vm;
		FastingRingView.Drawable = _ring;

		_vm.PropertyChanged += (_, e) =>
		{
			switch (e.PropertyName)
			{
				case nameof(HomeViewModel.RawProgress):
				case nameof(HomeViewModel.IsGoalMet):
					_ring.Progress = _vm.RawProgress;
					_ring.GoalMet = _vm.IsGoalMet;
					FastingRingView.Invalidate();
					break;

				case nameof(HomeViewModel.CurrentStageIndex):
					// ScrollTo(0) on Android can overshoot to item 1 with snap points enabled;
					// item 0 is already the natural starting offset, so skip scrolling for it.
					if (_vm.CurrentStageIndex > 0)
					{
						StagesView.ScrollTo(_vm.CurrentStageIndex,
							position: ScrollToPosition.Start,
							animate: true);
					}
					break;
			}
		};

		_vm.StageJustChanged += async (_, _) =>
		{
			// Gentle pulse: scale up 4% then back, plus a quick brightness spike on the ring.
			await Task.WhenAll(
				RingContainer.ScaleTo(1.04, 250, Easing.SinOut),
				FlashRingAsync());
			await RingContainer.ScaleTo(1.0, 350, Easing.SinIn);
		};
	}

	private async Task FlashRingAsync()
	{
		await FastingRingView.FadeTo(0.7, 120);
		await FastingRingView.FadeTo(1.0, 220);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _vm.LoadAsync();
		_ring.Progress = _vm.RawProgress;
		_ring.GoalMet = _vm.IsGoalMet;
		FastingRingView.Invalidate();

		// Auto-snap the roadmap to the current stage on appearance.
		// Skip index 0 — it's the natural starting offset and ScrollTo(0) can overshoot on Android.
		if (_vm.CurrentStageIndex > 0)
		{
			Dispatcher.Dispatch(() => StagesView.ScrollTo(_vm.CurrentStageIndex,
				position: ScrollToPosition.Start, animate: false));
		}
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_vm.Dispose();
	}
}
