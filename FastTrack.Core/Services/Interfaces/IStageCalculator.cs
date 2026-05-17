using FastTrack.Models;

namespace FastTrack.Services.Interfaces;

public interface IStageCalculator
{
    IReadOnlyList<FastingStage> Stages { get; }
    FastingStage GetStage(double elapsedHours);
}
