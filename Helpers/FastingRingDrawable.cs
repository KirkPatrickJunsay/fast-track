namespace FastTrack.Helpers;

/// <summary>
/// Hero circular progress ring for the active fast.
/// Renders a muted track + bright primary arc starting at 12 o'clock, sweeping clockwise.
/// Time text is rendered by overlay labels in the page — this drawable owns only the ring.
/// </summary>
public sealed class FastingRingDrawable : IDrawable
{
    // Token-aligned colors (kept hex so the drawable is testable without MAUI resources).
    private static readonly Color Track = Color.FromArgb("#2A2A2A");      // SurfaceContainerHigh
    private static readonly Color PrimaryStart = Color.FromArgb("#7A6FD8");
    private static readonly Color PrimaryEnd = Color.FromArgb("#C4C0FF"); // Primary
    private static readonly Color GoalMark = Color.FromArgb("#A9D38B");   // Secondary

    /// <summary>Fast progress 0..1 (visually clamped, can exceed 1 to indicate over-goal).</summary>
    public double Progress { get; set; }

    /// <summary>True when the active fast has crossed its goal — colours the arc sage.</summary>
    public bool GoalMet { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        const float margin = 14f;
        var side = Math.Min(dirtyRect.Width, dirtyRect.Height) - margin * 2;
        if (side <= 0) return;

        var cx = dirtyRect.Center.X;
        var cy = dirtyRect.Center.Y;
        var r = side / 2f;
        var strokeWidth = Math.Max(8f, side / 28f);
        var ringRect = new RectF(cx - r, cy - r, side, side);

        // Track
        canvas.StrokeColor = Track;
        canvas.StrokeSize = strokeWidth;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawEllipse(ringRect);

        // Progress arc (clockwise from 12 o'clock)
        var clamped = (float)Math.Clamp(Progress, 0, 1.5);
        if (clamped <= 0) return;
        var sweep = clamped * 360f;
        if (sweep > 359.9f) sweep = 359.9f; // avoid full-circle artifact

        var paint = new LinearGradientPaint(
            new PointF(0.5f, 0f),
            new PointF(0.5f, 1f))
        {
            StartColor = GoalMet ? GoalMark : PrimaryStart,
            EndColor = GoalMet ? GoalMark : PrimaryEnd,
        };
        canvas.SetFillPaint(paint, ringRect);
        canvas.StrokeColor = GoalMet ? GoalMark : PrimaryEnd;
        canvas.StrokeSize = strokeWidth;
        canvas.StrokeLineCap = LineCap.Round;

        // MAUI Graphics arc: startAngle measured from 3 o'clock counter-clockwise, sweep CCW.
        // We want clockwise from 12 o'clock: start = 90°, sweep negative (in CCW frame).
        var startAngle = 90f;
        var endAngle = 90f - sweep;
        canvas.DrawArc(ringRect, startAngle, endAngle, clockwise: true, closed: false);

        // Tiny goal-met marker if we passed 100%
        if (Progress > 1.0)
        {
            canvas.FillColor = GoalMark;
            canvas.FillCircle(cx, cy - r, strokeWidth * 0.55f);
        }
    }
}
