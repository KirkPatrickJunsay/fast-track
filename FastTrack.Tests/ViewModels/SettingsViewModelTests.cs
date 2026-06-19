using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class SettingsViewModelTests
{
    private static (SettingsViewModel Vm,
                    Mock<IUserProfileRepository> Profiles,
                    Mock<IDialogService> Dialogs,
                    Mock<INavigationService> Nav,
                    UserProfile Profile) Build(UserProfile? profile = null)
    {
        profile ??= new UserProfile
        {
            Id = Guid.NewGuid(),
            DisplayName = "Kirk",
            Level = ExperienceLevel.Intermediate,
            IsEducationalMode = false,
            OnboardingCompleted = true,
        };
        var profiles = new Mock<IUserProfileRepository>();
        profiles.Setup(p => p.GetOrCreateAsync()).ReturnsAsync(profile);
        profiles.Setup(p => p.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);

        var dialogs = new Mock<IDialogService>();
        var nav = new Mock<INavigationService>();
        var vm = new SettingsViewModel(profiles.Object, dialogs.Object, nav.Object);
        return (vm, profiles, dialogs, nav, profile);
    }

    [Fact]
    public async Task LoadAsync_renders_profile_summary()
    {
        var (vm, _, _, _, _) = Build();
        await vm.LoadAsync();
        vm.ProfileSummary.Should().Be("Kirk · Intermediate · Full timer access");
    }

    [Fact]
    public async Task LoadAsync_marks_educational_mode_in_summary()
    {
        var (vm, _, _, _, _) = Build(new UserProfile
        {
            Id = Guid.NewGuid(),
            DisplayName = string.Empty,
            Level = ExperienceLevel.Beginner,
            IsEducationalMode = true,
        });
        await vm.LoadAsync();
        vm.ProfileSummary.Should().Contain("Educational mode");
        vm.ProfileSummary.Should().Contain("Faster");
    }

    [Fact]
    public async Task OpenNotifications_navigates_to_prefs_page()
    {
        var (vm, _, _, nav, _) = Build();
        await vm.OpenNotificationsCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoToAsync("NotificationPreferencesPage"), Times.Once);
    }

    [Fact]
    public async Task OpenTrophies_navigates_to_trophy_page()
    {
        var (vm, _, _, nav, _) = Build();
        await vm.OpenTrophiesCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoToAsync("TrophyCabinetPage"), Times.Once);
    }

    [Fact]
    public async Task OpenData_navigates_to_data_page()
    {
        var (vm, _, _, nav, _) = Build();
        await vm.OpenDataCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoToAsync("DataManagementPage"), Times.Once);
    }

    [Fact]
    public async Task OpenPrivacy_navigates_to_privacy_page()
    {
        var (vm, _, _, nav, _) = Build();
        await vm.OpenPrivacyCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoToAsync("PrivacyPage"), Times.Once);
    }

    [Fact]
    public async Task OpenCustomizeHome_navigates_to_customize_home_page()
    {
        var (vm, _, _, nav, _) = Build();
        await vm.OpenCustomizeHomeCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoToAsync("CustomizeHomePage"), Times.Once);
    }

    [Fact]
    public async Task RedoOnboarding_when_confirmed_flips_flag_and_navigates()
    {
        var (vm, profiles, dialogs, nav, profile) = Build();
        dialogs.Setup(d => d.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
               .ReturnsAsync(true);

        await vm.RedoOnboardingCommand.ExecuteAsync(null);

        profile.OnboardingCompleted.Should().BeFalse();
        profiles.Verify(p => p.UpdateAsync(profile), Times.Once);
        nav.Verify(n => n.GoToAsync("//OnboardingPage"), Times.Once);
    }

    [Fact]
    public async Task RedoOnboarding_when_cancelled_is_noop()
    {
        var (vm, profiles, dialogs, nav, profile) = Build();
        dialogs.Setup(d => d.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
               .ReturnsAsync(false);

        await vm.RedoOnboardingCommand.ExecuteAsync(null);

        profile.OnboardingCompleted.Should().BeTrue();
        profiles.Verify(p => p.UpdateAsync(It.IsAny<UserProfile>()), Times.Never);
        nav.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
    }
}
