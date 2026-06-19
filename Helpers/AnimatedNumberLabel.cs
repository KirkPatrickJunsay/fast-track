namespace FastTrack.Helpers;

/// <summary>
/// A Label that tweens between numeric values when bound to <see cref="Value"/>.
/// Use Format to render (e.g. "{0:0} ml", "{0:0} XP").
/// </summary>
public class AnimatedNumberLabel : Label
{
	public static readonly BindableProperty ValueProperty = BindableProperty.Create(
		nameof(Value),
		typeof(double),
		typeof(AnimatedNumberLabel),
		0.0,
		propertyChanged: OnValueChanged);

	public static readonly BindableProperty FormatProperty = BindableProperty.Create(
		nameof(Format),
		typeof(string),
		typeof(AnimatedNumberLabel),
		"{0:0}");

	public static readonly BindableProperty DurationMsProperty = BindableProperty.Create(
		nameof(DurationMs),
		typeof(uint),
		typeof(AnimatedNumberLabel),
		(uint)450);

	public double Value
	{
		get => (double)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	public string Format
	{
		get => (string)GetValue(FormatProperty);
		set => SetValue(FormatProperty, value);
	}

	public uint DurationMs
	{
		get => (uint)GetValue(DurationMsProperty);
		set => SetValue(DurationMsProperty, value);
	}

	private double _displayValue;
	private bool _initialized;

	private static void OnValueChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var label = (AnimatedNumberLabel)bindable;
		var target = newValue is double d ? d : System.Convert.ToDouble(newValue);

		// On first assignment, snap without animating so the screen never starts at 0.
		if (!label._initialized)
		{
			label._initialized = true;
			label._displayValue = target;
			label.Text = string.Format(label.Format, target);
			return;
		}

		var from = label._displayValue;
		if (System.Math.Abs(from - target) < 0.0001)
		{
			label.Text = string.Format(label.Format, target);
			return;
		}

		label.AbortAnimation("number");
		new Animation(v =>
		{
			label._displayValue = v;
			label.Text = string.Format(label.Format, v);
		}, from, target, Easing.CubicOut)
		.Commit(label, "number", length: label.DurationMs, finished: (_, _) =>
		{
			label._displayValue = target;
			label.Text = string.Format(label.Format, target);
		});
	}
}
