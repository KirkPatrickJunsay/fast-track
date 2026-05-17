using FastTrack.Models;

namespace FastTrack.Services.Interfaces;

public interface IDataExportService
{
    Task<FastTrackExport> BuildExportAsync();
    Task<string> BuildJsonAsync();
}
