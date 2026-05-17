using System.Text.Json;
using FastTrack.Data;
using FastTrack.Models;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class OnboardingViewModelTests
{
    private static (OnboardingViewModel Vm, Mock<IUserProfileRepository> Profiles, UserProfile Profile) Build()
    {
        var profile = new UserProfile { Id = Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow };
        var profiles = new Mock<IUserProfileRepository>();
        profiles.Setup(p => p.GetOrCreateAsync()).ReturnsAsync(profile);
        profiles.Setup(p => p.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);
        return (new OnboardingViewModel(profiles.Object), profiles, profile);
    }

    [Fact]
    public void Starts_on_welcome_step()
    {
        var (vm, _, _) = Build();
        vm.CurrentStep.Should().Be(0);
        vm.IsWelcomeStep.Should().BeTrue();
        vm.StepLabel.Should().Be("Step 1 of 6");
        vm.CanGoBack.Should().BeFalse();
        vm.ProgressFraction.Should().BeApproximately(1.0 / 6.0, 0.001);
    }

    [Fact]
    public void Goals_collection_has_one_per_enum_value()
    {
        var (vm, _, _) = Build();
        vm.Goals.Should().HaveCount(Enum.GetValues<FastingGoal>().Length);
    }

    [Fact]
    public void Next_walks_through_all_steps_and_stops_at_done()
    {
        var (vm, _, _) = Build();

        vm.NextCommand.Execute(null);
        vm.IsGoalsStep.Should().BeTrue();
        vm.CanGoBack.Should().BeTrue();

        vm.NextCommand.Execute(null);
        vm.IsExperienceStep.Should().BeTrue();

        vm.NextCommand.Execute(null);
        vm.IsMedicalStep.Should().BeTrue();

        vm.NextCommand.Execute(null);
        vm.IsNotificationsStep.Should().BeTrue();

        vm.NextCommand.Execute(null);
        vm.IsDoneStep.Should().BeTrue();
        vm.SuggestedProtocolText.Should().Contain("16:8"); // Beginner default

        // Going past Done is a no-op.
        vm.NextCommand.Execute(null);
        vm.CurrentStep.Should().Be(5);
    }

    [Fact]
    public void Back_stops_at_welcome()
    {
        var (vm, _, _) = Build();
        vm.NextCommand.Execute(null);
        vm.NextCommand.Execute(null);
        vm.BackCommand.Execute(null);
        vm.IsGoalsStep.Should().BeTrue();

        // Back from Goals brings us to Welcome; further back is a no-op.
        vm.BackCommand.Execute(null);
        vm.IsWelcomeStep.Should().BeTrue();
        vm.BackCommand.Execute(null);
        vm.CurrentStep.Should().Be(0);
    }

    [Fact]
    public void ToggleGoal_flips_selection()
    {
        var (vm, _, _) = Build();
        var first = vm.Goals.First();
        first.IsSelected.Should().BeFalse();
        vm.ToggleGoalCommand.Execute(first);
        first.IsSelected.Should().BeTrue();
        vm.ToggleGoalCommand.Execute(first);
        first.IsSelected.Should().BeFalse();
    }

    [Theory]
    [InlineData(ExperienceLevel.Beginner, "16:8")]
    [InlineData(ExperienceLevel.Intermediate, "18:6")]
    [InlineData(ExperienceLevel.Advanced, "Full protocol library")]
    public void Next_into_done_sets_suggestion_per_experience(ExperienceLevel level, string expectedFragment)
    {
        var (vm, _, _) = Build();
        vm.Experience = level;
        for (var i = 0; i < OnboardingViewModel.TotalSteps - 1; i++) vm.NextCommand.Execute(null);
        vm.SuggestedProtocolText.Should().Contain(expectedFragment);
    }

    [Fact]
    public void Experience_picker_commands_select_correct_level()
    {
        var (vm, _, _) = Build();
        vm.PickAdvancedCommand.Execute(null);
        vm.IsAdvancedSelected.Should().BeTrue();
        vm.PickIntermediateCommand.Execute(null);
        vm.IsIntermediateSelected.Should().BeTrue();
        vm.PickBeginnerCommand.Execute(null);
        vm.IsBeginnerSelected.Should().BeTrue();
    }

    [Fact]
    public void AnyMedicalFlag_reflects_any_checkbox()
    {
        var (vm, _, _) = Build();
        vm.AnyMedicalFlag.Should().BeFalse();

        vm.MedUnder18 = true;
        vm.AnyMedicalFlag.Should().BeTrue();
        vm.MedicalStatusText.Should().Contain("educational mode");

        vm.MedUnder18 = false;
        vm.MedicalStatusText.Should().Contain("No contraindications");
    }

    [Fact]
    public async Task FinishAsync_persists_profile_with_educational_mode_when_flagged()
    {
        var (vm, profiles, profile) = Build();
        vm.Goals.First(g => g.Goal == FastingGoal.MentalClarity).IsSelected = true;
        vm.Goals.First(g => g.Goal == FastingGoal.Longevity).IsSelected = true;
        vm.Experience = ExperienceLevel.Intermediate;
        vm.MedEatingDisorderHistory = true;
        vm.DisplayName = "Kirk";

        var finished = false;
        vm.OnFinished = () => { finished = true; return Task.CompletedTask; };

        await vm.FinishCommand.ExecuteAsync(null);

        finished.Should().BeTrue();
        profile.DisplayName.Should().Be("Kirk");
        profile.Level.Should().Be(ExperienceLevel.Intermediate);
        profile.OnboardingCompleted.Should().BeTrue();
        profile.IsEducationalMode.Should().BeTrue();

        var goals = JsonSerializer.Deserialize<List<FastingGoal>>(profile.GoalsJson!)!;
        goals.Should().Contain(new[] { FastingGoal.MentalClarity, FastingGoal.Longevity });

        var screening = JsonSerializer.Deserialize<MedicalScreeningResponses>(profile.MedicalScreeningJson!)!;
        screening.EatingDisorderHistory.Should().BeTrue();
        screening.AnyContraindicated.Should().BeTrue();

        profiles.Verify(p => p.UpdateAsync(profile), Times.Once);
    }

    [Fact]
    public async Task FinishAsync_defaults_display_name_to_Faster_when_blank()
    {
        var (vm, _, profile) = Build();
        vm.DisplayName = "   ";
        await vm.FinishCommand.ExecuteAsync(null);
        profile.DisplayName.Should().Be("Faster");
    }
}
