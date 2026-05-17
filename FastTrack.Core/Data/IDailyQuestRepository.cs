using FastTrack.Models;

namespace FastTrack.Data;

public interface IDailyQuestRepository
{
    Task<IReadOnlyList<DailyQuest>> GetForDateAsync(DateTime localDateUtc);
    Task UpsertAsync(DailyQuest quest);
    Task UpsertManyAsync(IEnumerable<DailyQuest> quests);
}
