using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastTrack.Data;
using FastTrack.Models;

namespace FastTrack.ViewModels;

public partial class OnboardingViewModel : ObservableObject
{
    public const int TotalSteps = 6;

    private readonly IUserProfileRepository _profiles;

    // Step indices kept as constants for readability.
    private const int StepWelcome = 0;
    private const int StepGoals = 1;
    private const int StepExperience = 2;
    private const int StepMedical = 3;
    private const int StepNotifications = 4;
    private const int StepDone = 5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWelcomeStep))]
    [NotifyPropertyChangedFor(nameof(IsGoalsStep))]
    [NotifyPropertyChangedFor(nameof(IsExperienceStep))]
    [NotifyPropertyChangedFor(nameof(IsMedicalStep))]
    [NotifyPropertyChangedFor(nameof(IsNotificationsStep))]
    [NotifyPropertyChangedFor(nameof(IsDoneStep))]
    [NotifyPropertyChangedFor(nameof(StepLabel))]
    [NotifyPropertyChangedFor(nameof(ProgressFraction))]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    private int currentStep = StepWelcome;

    public bool IsWelcomeStep => CurrentStep == StepWelcome;
    public bool IsGoalsStep => CurrentStep == StepGoals;
    public bool IsExperienceStep => CurrentStep == StepExperience;
    public bool IsMedicalStep => CurrentStep == StepMedical;
    public bool IsNotificationsStep => CurrentStep == StepNotifications;
    public bool IsDoneStep => CurrentStep == StepDone;

    public string StepLabel => $"Step {CurrentStep + 1} of {TotalSteps}";
    public double ProgressFraction => (double)(CurrentStep + 1) / TotalSteps;
    public bool CanGoBack => CurrentStep > StepWelcome && CurrentStep < StepDone;

    // GOALS
    public ObservableCollection<GoalChipViewModel> Goals { get; } = new();

    // EXPERIENCE
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBeginnerSelected))]
    [NotifyPropertyChangedFor(nameof(IsIntermediateSelected))]
    [NotifyPropertyChangedFor(nameof(IsAdvancedSelected))]
    private ExperienceLevel experience = ExperienceLevel.Beginner;

    public bool IsBeginnerSelected => Experience == ExperienceLevel.Beginner;
    public bool IsIntermediateSelected => Experience == ExperienceLevel.Intermediate;
    public bool IsAdvancedSelected => Experience == ExperienceLevel.Advanced;

    // MEDICAL
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AnyMedicalFlag))]
    [NotifyPropertyChangedFor(nameof(MedicalStatusText))]
    private bool medPregnantOrBreastfeeding;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AnyMedicalFlag))]
    [NotifyPropertyChangedFor(nameof(MedicalStatusText))]
    private bool medUnder18;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AnyMedicalFlag))]
    [NotifyPropertyChangedFor(nameof(MedicalStatusText))]
    private bool medEatingDisorderHistory;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AnyMedicalFlag))]
    [NotifyPropertyChangedFor(nameof(MedicalStatusText))]
    private bool medInsulinDependentDiabetes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AnyMedicalFlag))]
    [NotifyPropertyChangedFor(nameof(MedicalStatusText))]
    private bool medFoodDependentMeds;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AnyMedicalFlag))]
    [NotifyPropertyChangedFor(nameof(MedicalStatusText))]
    private bool medOtherChronic;

    public bool AnyMedicalFlag =>
        MedPregnantOrBreastfeeding || MedUnder18 || MedEatingDisorderHistory ||
        MedInsulinDependentDiabetes || MedFoodDependentMeds || MedOtherChronic;

    public string MedicalStatusText => AnyMedicalFlag
        ? "Based on your responses, the app will run in educational mode. You can browse content and learn, but the timer will be disabled. Please consult your doctor."
        : "No contraindications flagged. You'll have full access to the timer.";

    [ObservableProperty] private string displayName = "Faster";

    [ObservableProperty] private string suggestedProtocolText = string.Empty;

    // NOTIFICATION PREFERENCES (US-06.6)
    [ObservableProperty] private bool notifFastLifecycle = true;
    [ObservableProperty] private bool notifStageTransitions = true;
    [ObservableProperty] private bool notifEatingWindow = true;
    [ObservableProperty] private bool notifStreakProtection = true;
    [ObservableProperty] private bool notifQuestReminders = true;
    [ObservableProperty] private bool quietHoursEnabled = true;
    [ObservableProperty] private TimeSpan quietHoursStart = new(22, 0, 0);
    [ObservableProperty] private TimeSpan quietHoursEnd = new(7, 0, 0);

    public OnboardingViewModel(IUserProfileRepository profiles)
    {
        _profiles = profiles;
        foreach (var g in Enum.GetValues<FastingGoal>())
        {
            Goals.Add(new GoalChipViewModel { Goal = g, Label = g.DisplayName() });
        }
    }

    [RelayCommand]
    private void ToggleGoal(GoalChipViewModel? item)
    {
        if (item is null) return;
        item.IsSelected = !item.IsSelected;
    }

    [RelayCommand]
    private void PickBeginner() => Experience = ExperienceLevel.Beginner;

    [RelayCommand]
    private void PickIntermediate() => Experience = ExperienceLevel.Intermediate;

    [RelayCommand]
    private void PickAdvanced() => Experience = ExperienceLevel.Advanced;

    [RelayCommand]
    private void Next()
    {
        if (CurrentStep >= StepDone) return;
        if (CurrentStep == StepNotifications)
        {
            // Compute suggested protocol blurb based on experience.
            SuggestedProtocolText = Experience switch
            {
                ExperienceLevel.Beginner => "We'll start you with 16:8 — gentle and well-tolerated.",
                ExperienceLevel.Intermediate => "18:6 is a good fit for your experience level.",
                ExperienceLevel.Advanced => "Full protocol library unlocked — pick what suits your week.",
                _ => "",
            };
        }
        CurrentStep++;
    }

    [RelayCommand]
    private void Back()
    {
        if (CurrentStep <= StepWelcome) return;
        CurrentStep--;
    }

    /// <summary>Set by the page so it can hand control back to the main shell on completion.</summary>
    public Func<Task>? OnFinished { get; set; }

    [RelayCommand]
    private async Task FinishAsync()
    {
        var profile = await _profiles.GetOrCreateAsync();

        var screening = new MedicalScreeningResponses
        {
            PregnantOrBreastfeeding = MedPregnantOrBreastfeeding,
            Under18 = MedUnder18,
            EatingDisorderHistory = MedEatingDisorderHistory,
            InsulinDependentDiabetes = MedInsulinDependentDiabetes,
            TakesFoodDependentMedications = MedFoodDependentMeds,
            OtherChronicCondition = MedOtherChronic,
        };

        var selectedGoals = Goals.Where(g => g.IsSelected).Select(g => g.Goal).ToList();

        var notif = new NotificationPreferences
        {
            FastLifecycleEnabled = NotifFastLifecycle,
            StageTransitionEnabled = NotifStageTransitions,
            EatingWindowEnabled = NotifEatingWindow,
            StreakProtectionEnabled = NotifStreakProtection,
            QuestRemindersEnabled = NotifQuestReminders,
            QuietHoursEnabled = QuietHoursEnabled,
            QuietHoursStart = QuietHoursStart,
            QuietHoursEnd = QuietHoursEnd,
        };

        profile.DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? "Faster" : DisplayName.Trim();
        profile.Level = Experience;
        profile.GoalsJson = JsonSerializer.Serialize(selectedGoals);
        profile.MedicalScreeningJson = JsonSerializer.Serialize(screening);
        profile.NotificationPrefsJson = JsonSerializer.Serialize(notif);
        profile.IsEducationalMode = screening.AnyContraindicated;
        profile.OnboardingCompleted = true;

        await _profiles.UpdateAsync(profile);

        if (OnFinished is not null) await OnFinished();
    }
}
