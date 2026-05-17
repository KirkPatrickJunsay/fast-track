using FastTrack.Models;

namespace FastTrack.Data;

public sealed class UserProfileRepository : IUserProfileRepository
{
    private readonly IDatabaseInitializer _db;

    public UserProfileRepository(IDatabaseInitializer db)
    {
        _db = db;
    }

    public async Task<UserProfile> GetOrCreateAsync()
    {
        var conn = await _db.InitializeAsync();
        var existing = await conn.Table<UserProfile>().FirstOrDefaultAsync();
        if (existing is not null)
        {
            return existing;
        }

        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow,
            Level = ExperienceLevel.Beginner,
            OnboardingCompleted = false,
        };
        await conn.InsertAsync(profile);
        return profile;
    }

    public async Task UpdateAsync(UserProfile profile)
    {
        var conn = await _db.InitializeAsync();
        await conn.UpdateAsync(profile);
    }
}
