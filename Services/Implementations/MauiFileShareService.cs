using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

public sealed class MauiFileShareService : IFileShareService
{
    public async Task ShareTextFileAsync(string fileName, string content, string title = "Share")
    {
        var path = Path.Combine(FileSystem.CacheDirectory, fileName);
        await File.WriteAllTextAsync(path, content);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = title,
            File = new ShareFile(path),
        });
    }
}
