namespace FastTrack.Services.Interfaces;

/// <summary>
/// Thin abstraction over Shell navigation so ViewModels stay testable.
/// </summary>
public interface INavigationService
{
    Task GoToAsync(string route);
    Task GoBackAsync();
}
