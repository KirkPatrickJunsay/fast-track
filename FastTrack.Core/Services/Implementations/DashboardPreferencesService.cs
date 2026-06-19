using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

public sealed class DashboardPreferencesService : IDashboardPreferencesService
{
    private readonly IUserProfileRepository _profiles;

    public DashboardPreferencesService(IUserProfileRepository profiles)
    {
        _profiles = profiles;
    }

    public async Task<DashboardPreferences> GetAsync()
    {
        var profile = await _profiles.GetOrCreateAsync();
        return DashboardPreferences.FromJson(profile.DashboardWidgetsJson);
    }

    public async Task SaveAsync(DashboardPreferences prefs)
    {
        var profile = await _profiles.GetOrCreateAsync();
        profile.DashboardWidgetsJson = prefs.ToJson();
        await _profiles.UpdateAsync(profile);
    }
}
