using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

public sealed class ShellNavigationService : INavigationService
{
    public Task GoToAsync(string route) => Shell.Current is null
        ? Task.CompletedTask
        : Shell.Current.GoToAsync(route);

    public Task GoBackAsync() => Shell.Current is null
        ? Task.CompletedTask
        : Shell.Current.GoToAsync("..");
}
