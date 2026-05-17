using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

public sealed class MauiFilePickerService : IFilePickerService
{
    public async Task<PickedFile?> PickJsonAsync()
    {
        var options = new PickOptions
        {
            PickerTitle = "Pick a FastTrack export (.json)",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "application/json", "text/plain", "*/*" } },
                { DevicePlatform.iOS, new[] { "public.json", "public.text" } },
                { DevicePlatform.MacCatalyst, new[] { "public.json", "public.text" } },
                { DevicePlatform.WinUI, new[] { ".json" } },
            }),
        };

        var picked = await FilePicker.Default.PickAsync(options);
        if (picked is null) return null;

        await using var stream = await picked.OpenReadAsync();
        using var reader = new StreamReader(stream);
        var contents = await reader.ReadToEndAsync();
        return new PickedFile(picked.FileName, contents);
    }
}
