using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

/// <summary>
/// Seven seed articles for the Learn tab. Content is intentionally calm,
/// evidence-leaning and never prescriptive — Fast Track is a logging tool, not a
/// medical device, and the Privacy & Terms page already carries the disclaimer.
/// </summary>
public sealed class ArticleCatalog : IArticleCatalog
{
    private static readonly IReadOnlyList<Article> Seed = new[]
    {
        new Article(
            "what-is-if",
            "What is intermittent fasting?",
            "Time-based eating, in plain language.",
            "article_what_is_if.svg",
            3,
            new ArticleSection[]
            {
                new(null,
                    "Intermittent fasting (IF) is a pattern of when you eat, not what you eat. " +
                    "Instead of restricting calories or food groups, you split the day into an eating window and a fasting window. " +
                    "Your body uses the fasted hours to switch fuel sources, drop insulin, and start the cellular maintenance work it can't easily do while it's digesting."),
                new("Why people try it",
                    "Common reasons: simpler eating routines, fewer decisions about snacks, better focus in the morning, " +
                    "and the metabolic effects that kick in once stored glucose runs low. None of these are guaranteed — they're patterns, not promises."),
                new("How Fast Track helps",
                    "The app times your fast, places you on the seven-stage timeline so you can see what your body is doing in real time, " +
                    "and keeps a private log of every fast on your device. There is no scoring of what you ate."),
                new("What it isn't",
                    "IF is not a starvation diet, a cleanse, or a treatment for any medical condition. " +
                    "If you're pregnant, under 18, managing diabetes, on medication that requires food, or have a history of disordered eating, please talk to a healthcare professional before starting."),
            }),

        new Article(
            "stages-overview",
            "The seven fasting stages",
            "From digestion to autophagy — what happens, hour by hour.",
            "article_stages_overview.svg",
            4,
            new ArticleSection[]
            {
                new(null,
                    "Your body doesn't experience a fast as one long event. It moves through several distinct metabolic states. " +
                    "Fast Track shows these as seven stages so you can see the shift instead of just watching a clock."),
                new("The headline arc",
                    "Anabolic (0–4h) — digesting the last meal.\n" +
                    "Catabolic (4–12h) — glycogen begins to drop.\n" +
                    "Fat-burning (12–18h) — lipolysis ramps up.\n" +
                    "Ketosis (18–24h) — ketones become available to the brain.\n" +
                    "Autophagy (24–48h) — cellular cleanup engages.\n" +
                    "Deep ketosis (48–72h) — growth hormone surges, ketones high.\n" +
                    "Extended (72h+) — stem cell activity rises; supervision recommended."),
                new("Use the glossary",
                    "Tap any stage in the Glossary section at the bottom of the Learn tab to open its detail card — " +
                    "what's happening biologically, how you might feel, and what to watch for."),
                new("Caveat",
                    "Stage timing is approximate. Your last meal, sleep, activity, and individual metabolism all shift the curve by a few hours. " +
                    "Don't chase a stage at the expense of how you actually feel."),
            }),

        new Article(
            "choosing-protocol",
            "Choosing your first protocol",
            "16:8, 18:6, OMAD, 5:2 — how to pick without drama.",
            "article_choosing_protocol.svg",
            4,
            new ArticleSection[]
            {
                new(null,
                    "The numbers describe the split between fasting and eating. 16:8 means 16 hours fasted, 8 hours eating. " +
                    "Start gentler than you think you should — building the habit matters more than the headline number."),
                new("16:8 — the on-ramp",
                    "Eat between noon and 8pm, fast the rest. Most people barely notice it after the first week. " +
                    "If you've never fasted before, start here."),
                new("18:6 — the workhorse",
                    "A 6-hour eating window. Good once 16:8 feels easy. Gets you reliably into ketosis on most days."),
                new("20:4 / OMAD — the deep end",
                    "Twenty hours fasted, or one-meal-a-day. Strong metabolic effects, but harder to hit your protein and micronutrient targets. " +
                    "Don't live here every day."),
                new("5:2 — the weekly pattern",
                    "Five normal eating days, two low-calorie (500–600 kcal) days. Useful if daily windows don't fit your schedule."),
                new("How to choose",
                    "Pick the longest fast you can comfortably finish three times a week without white-knuckling it. " +
                    "If your sleep, mood, or workouts degrade, dial it back. The best protocol is the one you can sustain for months, not the one that sounds most extreme."),
            }),

        new Article(
            "breaking-fast",
            "Breaking a fast safely",
            "What to eat first, what to skip, and why it matters.",
            "article_breaking_fast.svg",
            3,
            new ArticleSection[]
            {
                new(null,
                    "How you break a fast matters as much as the fast itself. After a long gap, your gut and insulin response are sensitive. " +
                    "A heavy or sugary first meal can spike blood sugar, leave you bloated, and undo the calm you built up."),
                new("Start small",
                    "Begin with something gentle — bone broth, a handful of nuts, plain Greek yogurt, or a small portion of cooked vegetables and protein. " +
                    "Wait 20–30 minutes, then eat a fuller meal if you're still hungry."),
                new("What to skip first",
                    "Skip ultra-processed snacks, fruit juice, sugary drinks, and very large carb-heavy meals as your first food. " +
                    "They're not forbidden — just not the right opener after a long fast."),
                new("Long fasts (48h+)",
                    "The longer the fast, the more carefully you should refeed. Plan the break-fast meal in advance. " +
                    "Avoid restaurants where portions are unpredictable. If anything feels off — dizziness, heart racing, severe nausea — stop and rest."),
                new("Hydration counts",
                    "Drink water with your first food. Many people mistake post-fast dehydration for a problem with the food itself."),
            }),

        new Article(
            "hunger-waves",
            "Hunger waves and ghrelin",
            "Hunger is a wave, not a hurricane.",
            "article_hunger_waves.svg",
            2,
            new ArticleSection[]
            {
                new(null,
                    "Hunger isn't a steadily-rising signal. It comes in waves driven by ghrelin, the hormone your stomach releases on a schedule built around your usual mealtimes. " +
                    "Each wave typically lasts 15–30 minutes and then fades."),
                new("Ride the wave",
                    "When a hunger pang hits, set a 20-minute timer instead of immediately reaching for food. " +
                    "Drink water, walk around, get back to whatever you were doing. Most waves are over before the timer."),
                new("Ghrelin learns",
                    "After a week or two of consistent fasting windows, ghrelin spikes start to migrate. " +
                    "If you used to feel ravenous at 8am and you've shifted breakfast to noon, the 8am pang fades within a couple of weeks."),
                new("When hunger isn't a wave",
                    "Steady, low-grade hunger that doesn't pass usually means you ate too little in your last window — protein and fibre in particular. " +
                    "Tighten the eating window's quality before you extend the fasting window's length."),
            }),

        new Article(
            "electrolytes",
            "Water, caffeine, and electrolytes",
            "What's actually fine to drink during a fast.",
            "article_electrolytes.svg",
            3,
            new ArticleSection[]
            {
                new(null,
                    "A fast is usually about food, not drink. Plain water, black coffee, and unsweetened tea don't break a fast in any meaningful metabolic sense. " +
                    "What you do need to watch is electrolytes."),
                new("Water — drink more than you think",
                    "Fasting flushes water and sodium faster than normal. Aim for slightly more water than your usual day, " +
                    "and don't wait until you're thirsty — thirst lags."),
                new("Sodium, potassium, magnesium",
                    "These are the three that matter. A pinch of salt in water, an electrolyte sachet, or a small bouillon cup can stop headaches, " +
                    "leg cramps, and the foggy 'fasting flu' people sometimes blame on the fast itself."),
                new("Caffeine — fine in moderation",
                    "Black coffee and tea are fine, in your usual amount. If you normally drink three coffees and a fast pushes you to five, " +
                    "expect jitters, poor sleep, and worse next-day mood. Don't out-caffeinate your hunger."),
                new("What does break a fast",
                    "Anything with calories or sweeteners that trigger an insulin response: milk in coffee, sugar, fruit juice, diet sodas. " +
                    "Save them for the eating window."),
            }),

        new Article(
            "when-to-stop",
            "When to stop or seek help",
            "Red flags that mean today isn't the day to be fasting.",
            "article_when_to_stop.svg",
            2,
            new ArticleSection[]
            {
                new(null,
                    "Stopping a fast early is not a failure. Fasting is a tool, and a tool you can't put down on a bad day is a bad tool. " +
                    "Listen to these signals and break your fast without guilt."),
                new("Stop today's fast if you have any of these",
                    "Persistent dizziness or near-fainting.\n" +
                    "Heart racing, palpitations, or chest discomfort.\n" +
                    "Severe headache that doesn't improve with water + electrolytes.\n" +
                    "Confusion, slurred speech, or trouble focusing your vision.\n" +
                    "Vomiting or persistent nausea.\n" +
                    "Cold sweats and shaking that don't pass."),
                new("Step back from IF entirely if",
                    "Your relationship with food is becoming anxious or rule-bound.\n" +
                    "You're tracking fasts as punishment for eating.\n" +
                    "Your sleep, mood, or energy have been worse for two weeks running.\n" +
                    "You're losing weight faster than ~0.5–1 kg per week without intending to."),
                new("Talk to a professional",
                    "If you're pregnant or breastfeeding, under 18, managing diabetes or another chronic condition, " +
                    "on medication that requires food, or have a history of disordered eating — please talk to a doctor or dietitian before continuing. " +
                    "Fast Track's Educational mode is there for a reason; don't fight it."),
            }),
    };

    public IReadOnlyList<Article> All() => Seed;

    public Article? GetById(string id) => Seed.FirstOrDefault(a => a.Id == id);
}
