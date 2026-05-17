using System.Text.Json;
using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

public sealed class DataExportService : IDataExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IDatabaseInitializer _db;

    public DataExportService(IDatabaseInitializer db) => _db = db;

    public async Task<FastTrackExport> BuildExportAsync()
    {
        var conn = await _db.InitializeAsync();

        var profile = await conn.Table<UserProfile>().FirstOrDefaultAsync();
        var protocols = await conn.Table<FastingProtocol>().ToListAsync();
        var fasts = await conn.Table<Fast>().ToListAsync();
        var weights = await conn.Table<WeightEntry>().ToListAsync();
        var moods = await conn.Table<MoodEntry>().ToListAsync();
        var water = await conn.Table<WaterEntry>().ToListAsync();
        var badges = await conn.Table<EarnedBadge>().ToListAsync();
        var quests = await conn.Table<DailyQuest>().ToListAsync();

        return new FastTrackExport
        {
            SchemaVersion = FastTrackExport.CurrentSchemaVersion,
            ExportedAtUtc = DateTime.UtcNow,
            Profile = profile,
            Protocols = protocols,
            Fasts = fasts,
            Weights = weights,
            Moods = moods,
            Water = water,
            EarnedBadges = badges,
            DailyQuests = quests,
        };
    }

    public async Task<string> BuildJsonAsync()
    {
        var dto = await BuildExportAsync();
        return JsonSerializer.Serialize(dto, JsonOptions);
    }
}
