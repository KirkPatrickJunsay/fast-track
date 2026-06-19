namespace FastTrack.Helpers;

/// <summary>
/// A lightweight confetti particle system. Particles fall + drift + rotate + fade.
/// Use <see cref="Burst"/> to seed a new wave, and tick <see cref="Advance"/> on a timer
/// then call <c>graphicsView.Invalidate()</c> to redraw.
/// </summary>
public sealed class ConfettiDrawable : IDrawable
{
	private struct Particle
	{
		public float X, Y;          // position (px in canvas)
		public float Vx, Vy;        // velocity (px/sec)
		public float Rot;           // current rotation (deg)
		public float Vr;            // rotation velocity (deg/sec)
		public float Age;           // seconds since spawn
		public float Life;          // total seconds before fade-out
		public float Size;          // square edge length
		public Color Color;
	}

	private static readonly Color[] Palette =
	{
		Color.FromArgb("#C4C0FF"), // primary indigo
		Color.FromArgb("#A9D38B"), // sage
		Color.FromArgb("#FFB3B5"), // brick
		Color.FromArgb("#FFE3B0"), // warm sand
		Color.FromArgb("#7A6FD8"), // deep indigo
	};

	private readonly List<Particle> _particles = new(capacity: 200);
	private readonly Random _rand = new();

	/// <summary>Spawn a burst of <paramref name="count"/> particles from the top of the canvas.</summary>
	public void Burst(int count, float canvasWidth)
	{
		if (canvasWidth <= 0) canvasWidth = 360;
		for (var i = 0; i < count; i++)
		{
			_particles.Add(new Particle
			{
				X = (float)_rand.NextDouble() * canvasWidth,
				Y = -10 - (float)_rand.NextDouble() * 60,
				Vx = ((float)_rand.NextDouble() - 0.5f) * 60f,
				Vy = 90f + (float)_rand.NextDouble() * 140f,
				Rot = (float)_rand.NextDouble() * 360f,
				Vr = ((float)_rand.NextDouble() - 0.5f) * 360f,
				Age = 0f,
				Life = 2.4f + (float)_rand.NextDouble() * 1.6f,
				Size = 6f + (float)_rand.NextDouble() * 7f,
				Color = Palette[_rand.Next(Palette.Length)],
			});
		}
	}

	/// <summary>Advance the simulation by <paramref name="dtSeconds"/>. Returns true while particles remain alive.</summary>
	public bool Advance(float dtSeconds)
	{
		for (var i = _particles.Count - 1; i >= 0; i--)
		{
			var p = _particles[i];
			p.Age += dtSeconds;
			if (p.Age >= p.Life)
			{
				_particles.RemoveAt(i);
				continue;
			}
			p.X += p.Vx * dtSeconds;
			p.Y += p.Vy * dtSeconds;
			p.Vy += 18f * dtSeconds; // light gravity
			p.Rot += p.Vr * dtSeconds;
			_particles[i] = p;
		}
		return _particles.Count > 0;
	}

	public bool HasParticles => _particles.Count > 0;

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		foreach (var p in _particles)
		{
			var t = p.Age / p.Life;
			var alpha = t < 0.85f ? 1f : (1f - (t - 0.85f) / 0.15f);
			canvas.SaveState();
			canvas.Translate(p.X, p.Y);
			canvas.Rotate(p.Rot);
			canvas.FillColor = p.Color.WithAlpha(alpha);
			canvas.FillRectangle(-p.Size / 2f, -p.Size / 2f, p.Size, p.Size * 0.55f);
			canvas.RestoreState();
		}
	}
}
