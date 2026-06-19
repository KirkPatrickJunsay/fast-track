using FastTrack.Helpers;
using FastTrack.ViewModels;

namespace FastTrack.Views;

public partial class CelebrationPage : ContentPage
{
	private readonly CelebrationViewModel _vm;
	private readonly ConfettiDrawable _confetti = new();
	private IDispatcherTimer? _confettiTimer;
	private DateTime _lastTick;

	public CelebrationPage(CelebrationViewModel vm)
	{
		InitializeComponent();
		_vm = vm;
		BindingContext = vm;
		ConfettiView.Drawable = _confetti;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		RootGrid.Opacity = 0;
		EmojiLabel.Scale = 0.4;

		// Hide reward cards until we can pop them in sequence.
		XpCard.Opacity = 0;       XpCard.Scale = 0.85;
		StreakCard.Opacity = 0;   StreakCard.Scale = 0.85;
		LevelUpCard.Opacity = 0;  LevelUpCard.Scale = 0.85;
		BadgesCard.Opacity = 0;   BadgesCard.Scale = 0.85;
		QuestsCard.Opacity = 0;   QuestsCard.Scale = 0.85;

		await _vm.LoadAsync();

		await Task.WhenAll(
			RootGrid.FadeTo(1, 280, Easing.SinOut),
			EmojiLabel.ScaleTo(1.0, 480, Easing.SpringOut));

		// Pop reward cards in sequence — only the visible ones.
		await PopInAsync(XpCard,      _vm.HasXp);
		await PopInAsync(StreakCard,  _vm.HasStreak);
		await PopInAsync(LevelUpCard, _vm.HasLevelUp);
		await PopInAsync(BadgesCard,  _vm.HasBadges);
		await PopInAsync(QuestsCard,  _vm.HasQuests);

		// Confetti only on successful completions.
		if (_vm.IsGoalMet)
		{
			StartConfetti();
		}
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		StopConfetti();
	}

	private static async Task PopInAsync(View v, bool isVisible)
	{
		if (!isVisible) return;
		await Task.WhenAll(
			v.FadeTo(1, 220, Easing.SinOut),
			v.ScaleTo(1.0, 320, Easing.SpringOut));
		await Task.Delay(80);
	}

	private void StartConfetti()
	{
		var width = (float)(ConfettiView.Width > 0 ? ConfettiView.Width : Width);
		_confetti.Burst(80, width);
		// Stagger a second smaller burst.
		Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(220), () => _confetti.Burst(40, width));

		_lastTick = DateTime.UtcNow;
		_confettiTimer = Dispatcher.CreateTimer();
		_confettiTimer.Interval = TimeSpan.FromMilliseconds(33);
		_confettiTimer.Tick += OnConfettiTick;
		_confettiTimer.Start();
	}

	private void OnConfettiTick(object? sender, EventArgs e)
	{
		var now = DateTime.UtcNow;
		var dt = (float)(now - _lastTick).TotalSeconds;
		_lastTick = now;
		var alive = _confetti.Advance(dt);
		ConfettiView.Invalidate();
		if (!alive) StopConfetti();
	}

	private void StopConfetti()
	{
		if (_confettiTimer is null) return;
		_confettiTimer.Stop();
		_confettiTimer.Tick -= OnConfettiTick;
		_confettiTimer = null;
	}
}
