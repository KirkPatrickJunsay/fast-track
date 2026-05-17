using FastTrack.Data;

namespace FastTrack.Services.Implementations;

public sealed class MauiDatabasePathProvider : IDatabasePathProvider
{
    public string GetDatabasePath(string fileName) =>
        Path.Combine(FileSystem.AppDataDirectory, fileName);
}
