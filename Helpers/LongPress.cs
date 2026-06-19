using System.Windows.Input;

namespace FastTrack.Helpers;

/// <summary>
/// Attached property for binding a long-press gesture to a Command on any VisualElement.
/// Usage:
///   <Frame h:LongPress.Command="{Binding ...}" h:LongPress.CommandParameter="{Binding .}" />
/// </summary>
public static class LongPress
{
	public static readonly BindableProperty CommandProperty = BindableProperty.CreateAttached(
		"Command",
		typeof(ICommand),
		typeof(LongPress),
		null,
		propertyChanged: OnCommandChanged);

	public static readonly BindableProperty CommandParameterProperty = BindableProperty.CreateAttached(
		"CommandParameter",
		typeof(object),
		typeof(LongPress),
		null);

	public static readonly BindableProperty DurationMsProperty = BindableProperty.CreateAttached(
		"DurationMs",
		typeof(int),
		typeof(LongPress),
		500);

	public static ICommand? GetCommand(BindableObject view) => (ICommand?)view.GetValue(CommandProperty);
	public static void SetCommand(BindableObject view, ICommand? value) => view.SetValue(CommandProperty, value);

	public static object? GetCommandParameter(BindableObject view) => view.GetValue(CommandParameterProperty);
	public static void SetCommandParameter(BindableObject view, object? value) => view.SetValue(CommandParameterProperty, value);

	public static int GetDurationMs(BindableObject view) => (int)view.GetValue(DurationMsProperty);
	public static void SetDurationMs(BindableObject view, int value) => view.SetValue(DurationMsProperty, value);

	private static readonly Dictionary<View, State> _states = new();

	private sealed class State
	{
		public PointerGestureRecognizer Recognizer = null!;
		public IDispatcherTimer? Timer;
		public bool Fired;
	}

	private static void OnCommandChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if (bindable is not View element) return;

		if (_states.TryGetValue(element, out var existing))
		{
			DetachExisting(element, existing);
		}

		if (newValue is null) return;

		var state = new State();
		state.Recognizer = new PointerGestureRecognizer();
		state.Recognizer.PointerPressed += (_, _) => OnPressed(element, state);
		state.Recognizer.PointerReleased += (_, _) => OnReleased(state);
		state.Recognizer.PointerExited += (_, _) => OnReleased(state);
		element.GestureRecognizers.Add(state.Recognizer);
		_states[element] = state;
	}

	private static void OnPressed(View element, State state)
	{
		state.Fired = false;
		state.Timer?.Stop();
		state.Timer = element.Dispatcher.CreateTimer();
		state.Timer.Interval = TimeSpan.FromMilliseconds(GetDurationMs(element));
		state.Timer.IsRepeating = false;
		state.Timer.Tick += (_, _) =>
		{
			if (state.Fired) return;
			state.Fired = true;
			var cmd = GetCommand(element);
			var param = GetCommandParameter(element);
			if (cmd is not null && cmd.CanExecute(param))
			{
				cmd.Execute(param);
			}
		};
		state.Timer.Start();
	}

	private static void OnReleased(State state)
	{
		state.Timer?.Stop();
		state.Timer = null;
	}

	private static void DetachExisting(View element, State existing)
	{
		element.GestureRecognizers.Remove(existing.Recognizer);
		existing.Timer?.Stop();
		_states.Remove(element);
	}
}
