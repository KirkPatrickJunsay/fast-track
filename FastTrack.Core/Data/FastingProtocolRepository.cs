using FastTrack.Models;

namespace FastTrack.Data;

public sealed class FastingProtocolRepository : IFastingProtocolRepository
{
    private readonly IDatabaseInitializer _db;

    public FastingProtocolRepository(IDatabaseInitializer db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<FastingProtocol>> GetAllAsync()
    {
        var conn = await _db.InitializeAsync();
        var rows = await conn.Table<FastingProtocol>()
            .OrderBy(p => p.FastHours)
            .ToListAsync();
        return rows;
    }

    public async Task<FastingProtocol?> GetByIdAsync(Guid id)
    {
        var conn = await _db.InitializeAsync();
        return await conn.FindAsync<FastingProtocol>(id);
    }

    public async Task UpsertAsync(FastingProtocol protocol)
    {
        var conn = await _db.InitializeAsync();
        if (protocol.Id == Guid.Empty)
        {
            protocol.Id = Guid.NewGuid();
        }
        await conn.InsertOrReplaceAsync(protocol);
    }
}
