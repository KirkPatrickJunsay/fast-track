using FastTrack.Models;

namespace FastTrack.Helpers;

/// <summary>
/// Renders a GitHub-style fasting heatmap into a <see cref="GraphicsView"/>.
/// 7 day rows × N week columns. Intensity 0–3 maps to four shades from the design palette.
/// </summary>
public sealed class HeatmapDrawable : IDrawable
{
    public IReadOnlyList<HeatmapDay> Days { get; set; } = Array.Empty<HeatmapDay>();

    // Tokens — kept hex so this is testable without the MAUI app resources.
    private static readonly Color CellEmpty = Color.FromArgb("#474553");           // OutlineVariant
    private static readonly Color CellLow = Color.FromArgb("#4A3FBE");             // PrimaryContainer
    private static readonly Color CellMed = Color.FromArgb("#7A6FD8");             // mid step
    private static readonly Color CellHigh = Color.FromArgb("#C4C0FF");            // Primary

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Days.Count == 0)
        {
            canvas.FontColor = Color.FromArgb("#918F9F");
            canvas.FontSize = 12;
            canvas.DrawString("No data yet", dirtyRect, HorizontalAlignment.Center, VerticalAlignment.Center);
            return;
        }

        // Layout: 7 rows × W columns where each column is one ISO week.
        const float cellGap = 2f;
        var maxCellByWidth = (dirtyRect.Width - (52 * cellGap)) / 53f;
        var maxCellByHeight = (dirtyRect.Height - (6 * cellGap)) / 7f;
        var cell = Math.Max(2f, Math.Min(maxCellByWidth, maxCellByHeight));

        // Row 0 = Monday, Row 6 = Sunday.
        int RowOf(DateTime d) => ((int)d.DayOfWeek + 6) % 7;

        var firstDate = Days[0].LocalDate;
        var lastDate = Days[^1].LocalDate;
        var firstRow = RowOf(firstDate);
        var totalDays = Days.Count;

        // Determine column count: ceil((totalDays + firstRow) / 7).
        var cols = (int)Math.Ceiling((totalDays + firstRow) / 7.0);
        cols = Math.Min(cols, 53);

        // Compute x offset so the grid aligns to the right edge of the canvas.
        var gridWidth = cols * cell + (cols - 1) * cellGap;
        var startX = dirtyRect.X + Math.Max(0, dirtyRect.Width - gridWidth);
        var startY = dirtyRect.Y;

        // Populate a (col, row) intensity matrix from the data. Cells outside
        // the data range stay at -1 so we can distinguish "before first day"
        // and "after today" from genuine intensity=0 days.
        const int OutOfRange = -1;
        var matrix = new int[cols, 7];
        for (var c = 0; c < cols; c++)
            for (var r = 0; r < 7; r++)
                matrix[c, r] = OutOfRange;

        foreach (var day in Days)
        {
            var daysFromFirst = (day.LocalDate - firstDate).Days;
            var col = (daysFromFirst + firstRow) / 7;
            var row = RowOf(day.LocalDate);
            if (col < 0 || col >= cols || row < 0 || row > 6) continue;
            matrix[col, row] = day.Intensity;
        }

        // Draw EVERY cell in the rectangle so the grid edges read as a clean
        // rectangle even when firstDate isn't a Monday or today isn't a Sunday.
        // Before-range / after-range cells get the empty colour — same dark grey
        // as "no fast today" cells, which is what we want: a uniform muted backdrop.
        for (var c = 0; c < cols; c++)
        {
            for (var r = 0; r < 7; r++)
            {
                var x = startX + c * (cell + cellGap);
                var y = startY + r * (cell + cellGap);
                canvas.FillColor = matrix[c, r] switch
                {
                    1 => CellLow,
                    2 => CellMed,
                    3 => CellHigh,
                    _ => CellEmpty,
                };
                canvas.FillRoundedRectangle(new RectF(x, y, cell, cell), 2);
            }
        }
    }
}
