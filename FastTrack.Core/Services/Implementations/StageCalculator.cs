using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

public sealed class StageCalculator : IStageCalculator
{
    // Default thresholds per Epic 01 US-01.8. Configurability comes later via settings.
    // Educational content kept calm, evidence-flavoured, never prescriptive.
    private static readonly IReadOnlyList<FastingStage> DefaultStages = new[]
    {
        new FastingStage(
            "anabolic", "Anabolic", 0, 4,
            "Digesting your last meal.",
            "stage_anabolic.svg",
            "Your body is in fed mode. Insulin is elevated as nutrients from your last meal are absorbed and stored.",
            new[]
            {
                "Comfortable and energized",
                "Steady focus",
                "No hunger signals yet",
            }),

        new FastingStage(
            "catabolic", "Catabolic", 4, 12,
            "Glycogen stores beginning to deplete.",
            "stage_catabolic.svg",
            "Blood sugar normalizes and insulin drops. The liver starts breaking down stored glycogen for fuel.",
            new[]
            {
                "Mild hunger waves",
                "Generally clear-headed",
                "Energy stays even",
            }),

        new FastingStage(
            "fat-burning", "Fat-burning", 12, 18,
            "Metabolic shift toward fat for fuel.",
            "stage_fatburning.svg",
            "Liver glycogen is largely depleted. Lipolysis ramps up — your body begins burning stored fat for energy.",
            new[]
            {
                "Stronger hunger waves that pass",
                "Mental clarity often improves",
                "Mood usually steady",
            }),

        new FastingStage(
            "ketosis", "Ketosis", 18, 24,
            "Ketone production ramping up.",
            "stage_ketosis.svg",
            "Liver produces ketones from fat. Your brain begins using them as an efficient alternative fuel to glucose.",
            new[]
            {
                "Reduced cravings",
                "Calm, sustained energy",
                "Mild thirst — drink water with electrolytes",
            }),

        new FastingStage(
            "autophagy", "Autophagy", 24, 48,
            "Cellular cleanup engaged.",
            "stage_autophagy.svg",
            "Cells start recycling damaged components. Growth hormone rises and inflammation markers may begin to drop.",
            new[]
            {
                "Often feel lighter and clearer",
                "Some warmth or temperature sensitivity",
                "Hunger typically fades into the background",
            }),

        new FastingStage(
            "deep-ketosis", "Deep ketosis", 48, 72,
            "Growth hormone surge and deeper ketosis.",
            "stage_deepketosis.svg",
            "Growth hormone can rise multiple-fold above baseline. Ketones are high and insulin sensitivity continues to improve.",
            new[]
            {
                "Calm, monk-mode focus",
                "Refeeding becomes important — plan a gentle break-fast",
                "Listen to your body closely",
            }),

        new FastingStage(
            "extended", "Extended fast", 72, null,
            "Immune cell regeneration territory.",
            "stage_extended.svg",
            "Stem cell activity rises and older immune cells are recycled. Multi-day fasts should be supervised — please consult your doctor.",
            new[]
            {
                "Energy can dip — rest more",
                "Electrolytes are critical",
                "Break the fast gently with light, nourishing food",
            }),
    };

    public IReadOnlyList<FastingStage> Stages => DefaultStages;

    public FastingStage GetStage(double elapsedHours)
    {
        if (elapsedHours < 0) elapsedHours = 0;
        foreach (var stage in DefaultStages)
        {
            if (stage.Contains(elapsedHours)) return stage;
        }
        return DefaultStages[^1];
    }
}
