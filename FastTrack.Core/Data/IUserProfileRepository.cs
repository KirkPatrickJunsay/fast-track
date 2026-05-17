using FastTrack.Models;

namespace FastTrack.Data;

public interface IUserProfileRepository
{
    Task<UserProfile> GetOrCreateAsync();
    Task UpdateAsync(UserProfile profile);
}
