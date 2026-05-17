using FastTrack.Models;

namespace FastTrack.Data;

/// <summary>
/// Single source of truth for the bundled preset fasting protocols.
/// Used by the V001 initial migration and the data-reset service.
/// </summary>
public static class PresetProtocols
{
    public static IReadOnlyList<FastingProtocol> Build() => new[]
    {
        new FastingProtocol { Id = new Guid("a1000000-0000-0000-0000-000000000001"), Name = "16:8",   FastHours = 16, EatHours = 8, Difficulty = Difficulty.Beginner,     IsPreset = true, Description = "Classic intermittent fast." },
        new FastingProtocol { Id = new Guid("a1000000-0000-0000-0000-000000000002"), Name = "18:6",   FastHours = 18, EatHours = 6, Difficulty = Difficulty.Intermediate, IsPreset = true, Description = "Tighter eating window." },
        new FastingProtocol { Id = new Guid("a1000000-0000-0000-0000-000000000003"), Name = "20:4",   FastHours = 20, EatHours = 4, Difficulty = Difficulty.Advanced,     IsPreset = true, Description = "Warrior-style window." },
        new FastingProtocol { Id = new Guid("a1000000-0000-0000-0000-000000000004"), Name = "OMAD",   FastHours = 23, EatHours = 1, Difficulty = Difficulty.Advanced,     IsPreset = true, Description = "One meal a day." },
        new FastingProtocol { Id = new Guid("a1000000-0000-0000-0000-000000000005"), Name = "5:2",    FastHours = 24, EatHours = 0, Difficulty = Difficulty.Intermediate, IsPreset = true, Description = "Two low-calorie days per week." },
    };
}
