namespace FastTrack.Models;

public sealed record Level(int Number, string Name, int XpThreshold)
{
    public static IReadOnlyList<Level> All { get; } = new[]
    {
        new Level(1,  "Novice Faster", 0),
        new Level(2,  "Apprentice",    500),
        new Level(3,  "Practitioner",  1_500),
        new Level(4,  "Disciplined",   3_500),
        new Level(5,  "Monk",          7_500),
        new Level(6,  "Sage",          15_000),
        new Level(7,  "Zen Master",    30_000),
        new Level(8,  "Enlightened",   60_000),
        new Level(9,  "Transcendent",  120_000),
        new Level(10, "Legendary",     250_000),
    };

    public static Level ForXp(int totalXp)
    {
        Level current = All[0];
        foreach (var lvl in All)
        {
            if (totalXp >= lvl.XpThreshold) current = lvl;
            else break;
        }
        return current;
    }

    public static Level? Next(Level current) =>
        All.FirstOrDefault(l => l.Number == current.Number + 1);
}
