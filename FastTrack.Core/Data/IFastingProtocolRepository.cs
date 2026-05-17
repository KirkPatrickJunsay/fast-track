using FastTrack.Models;

namespace FastTrack.Data;

public interface IFastingProtocolRepository
{
    Task<IReadOnlyList<FastingProtocol>> GetAllAsync();
    Task<FastingProtocol?> GetByIdAsync(Guid id);
    Task UpsertAsync(FastingProtocol protocol);
}
