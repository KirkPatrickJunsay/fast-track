namespace FastTrack.Models;

public enum FastingGoal
{
    WeightManagement = 0,
    MentalClarity = 1,
    Autophagy = 2,
    MetabolicHealth = 3,
    Longevity = 4,
    ReligiousOrSpiritual = 5,
    SimplifyingEating = 6,
    AthleticPerformance = 7,
}

public static class FastingGoalExtensions
{
    public static string DisplayName(this FastingGoal g) => g switch
    {
        FastingGoal.WeightManagement => "Weight management",
        FastingGoal.MentalClarity => "Mental clarity",
        FastingGoal.Autophagy => "Autophagy",
        FastingGoal.MetabolicHealth => "Metabolic health",
        FastingGoal.Longevity => "Longevity",
        FastingGoal.ReligiousOrSpiritual => "Religious / spiritual",
        FastingGoal.SimplifyingEating => "Simplifying eating",
        FastingGoal.AthleticPerformance => "Athletic performance",
        _ => g.ToString(),
    };
}
