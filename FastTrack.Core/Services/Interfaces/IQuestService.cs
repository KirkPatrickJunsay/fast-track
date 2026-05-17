using FastTrack.Models;

namespace FastTrack.Services.Interfaces;

public sealed record QuestUpdate(DailyQuest Quest, QuestDefinition Definition, bool NewlyCompleted);

public interface IQuestService
{
    Task<IReadOnlyList<(DailyQuest Quest, QuestDefinition Definition)>> GetTodayAsync();
    Task<IReadOnlyList<QuestUpdate>> OnFastCompletedAsync(Fast fast, FastingProtocol protocol, IReadOnlyList<Fast> history);
}
