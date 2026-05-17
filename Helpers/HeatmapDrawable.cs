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

        // Build matrix indexed by (col, row).
        // Last day in Days is "today". Align columns so the rightmost column is the current week.
        var lastDate = Days[^1].LocalDate;
        // Row 0 = Monday, Row 6 = Sunday.
        int RowOf(DateTime d) => ((int)d.DayOfWeek + 6) % 7;

        var totalDays = Days.Count;
        var lastRow = RowOf(lastDate);

        // Determine column count: ceil((totalDays + lastRow) / 7).
        var cols = (int)Math.Ceiling((totalDays + RowOf(Days[0].LocalDate)) / 7.0);
        cols = Math.Min(cols, 53);

        // Compute x offset so the grid aligns to the right edge of the canvas.
        var gridWidth = cols * cell + (cols - 1) * cellGap;
        var startX = dirtyRect.X + Math.Max(0, dirtyRect.Width - gridWidth);
        var startY = dirtyRect.Y;

        // Walk all days, place each into (col, row).
        // Establish the first column's Monday by going back from the first date to its preceding Monday.
        var firstDate = Days[0].LocalDate;
        var firstRow = RowOf(firstDate);

        foreach (var day in Days)
        {
            var daysFromFirst = (day.LocalDate - firstDate).Days;
            var col = (daysFromFirst + firstRow) / 7;
            var row = RowOf(day.LocalDate);

            var x = startX + col * (cell + cellGap);
            var y = startY + row * (cell + cellGap);

            canvas.FillColor = day.Intensity switch
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
