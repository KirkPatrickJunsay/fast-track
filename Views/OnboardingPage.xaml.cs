using System.ComponentModel;
using FastTrack.ViewModels;

namespace FastTrack.Views;

public partial class OnboardingPage : ContentPage
{
	private readonly OnboardingViewModel _vm;
	private bool _animatingStep;

	public OnboardingPage(OnboardingViewModel vm)
	{
		InitializeComponent();
		_vm = vm;
		BindingContext = vm;
		vm.OnFinished = () => Shell.Current.GoToAsync("//MainPage");
		vm.PropertyChanged += OnVmPropertyChanged;
	}

	private async void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName != nameof(OnboardingViewModel.CurrentStep)) return;
		if (_animatingStep) return;
		_animatingStep = true;
		try
		{
			// Quick cross-fade: fade out, allow the step toggle to take effect, fade back in.
			await StepBody.FadeTo(0, 140, Easing.SinIn);
			await Task.Delay(20); // give the bindings a frame to swap the visible step
			StepBody.TranslationY = 12;
			await Task.WhenAll(
				StepBody.FadeTo(1, 220, Easing.SinOut),
				StepBody.TranslateTo(0, 0, 240, Easing.SinOut));
		}
		finally
		{
			_animatingStep = false;
		}
	}
}
