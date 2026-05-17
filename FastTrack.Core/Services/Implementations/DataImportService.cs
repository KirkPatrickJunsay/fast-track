using System.Text.Json;
using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

public sealed class DataImportService : IDataImportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IDatabaseInitializer _db;

    public DataImportService(IDatabaseInitializer db) => _db = db;

    public async Task<ImportResult> ApplyAsync(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new ImportResult(false, "The selected file is empty.", 0, 0, 0, 0, 0, 0);

        FastTrackExport? dto;
        try
        {
            dto = JsonSerializer.Deserialize<FastTrackExport>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            return new ImportResult(false, $"Invalid JSON: {ex.Message}", 0, 0, 0, 0, 0, 0);
        }

        if (dto is null)
            return new ImportResult(false, "The file did not contain a recognised export.", 0, 0, 0, 0, 0, 0);

        if (dto.SchemaVersion <= 0)
            return new ImportResult(false, "Missing or invalid schema version.", 0, 0, 0, 0, 0, 0);

        if (dto.SchemaVersion > FastTrackExport.CurrentSchemaVersion)
            return new ImportResult(false,
                $"This file was created by a newer version of FastTrack (schema {dto.SchemaVersion}). Please update the app and try again.",
                0, 0, 0, 0, 0, 0);

        var conn = await _db.InitializeAsync();

        try
        {
            await conn.RunInTransactionAsync(c =>
            {
                // Wipe in order that respects (currently absent) FK semantics; safe to do as flat tables.
                c.DeleteAll<Fast>();
                c.DeleteAll<EarnedBadge>();
                c.DeleteAll<DailyQuest>();
                c.DeleteAll<WeightEntry>();
                c.DeleteAll<MoodEntry>();
                c.DeleteAll<WaterEntry>();
                c.DeleteAll<UserProfile>();
                c.DeleteAll<FastingProtocol>();

                // Re-insert.
                if (dto.Profile is not null) c.Insert(dto.Profile);

                // Restore protocols from export — covers custom protocols. Presets reapply via the export too.
                if (dto.Protocols.Count > 0)
                    c.InsertAll(dto.Protocols);
                else
                    c.InsertAll(PresetProtocols.Build()); // safety net

                if (dto.Fasts.Count > 0) c.InsertAll(dto.Fasts);
                if (dto.Weights.Count > 0) c.InsertAll(dto.Weights);
                if (dto.Moods.Count > 0) c.InsertAll(dto.Moods);
                if (dto.Water.Count > 0) c.InsertAll(dto.Water);
                if (dto.EarnedBadges.Count > 0) c.InsertAll(dto.EarnedBadges);
                if (dto.DailyQuests.Count > 0) c.InsertAll(dto.DailyQuests);
            });
        }
        catch (Exception ex)
        {
            return new ImportResult(false, $"Import failed: {ex.Message}. Your data was not changed.",
                0, 0, 0, 0, 0, 0);
        }

        return new ImportResult(true, "Import complete.",
            dto.Fasts.Count, dto.Weights.Count, dto.Moods.Count, dto.Water.Count,
            dto.EarnedBadges.Count, dto.DailyQuests.Count);
    }
}
