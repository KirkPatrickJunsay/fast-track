using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class ProfileViewModelTests
{
    private static (ProfileViewModel Vm,
                    Mock<IUserProfileRepository> Profiles,
                    Mock<IDialogService> Dialogs,
                    Mock<INavigationService> Nav,
                    UserProfile Profile)
        Build(UserProfile? profile = null)
    {
        profile ??= new UserProfile
        {
            Id = Guid.NewGuid(),
            DisplayName = "Kirk",
            Level = ExperienceLevel.Beginner,
        };
        var profiles = new Mock<IUserProfileRepository>();
        profiles.Setup(p => p.GetOrCreateAsync()).ReturnsAsync(profile);
        profiles.Setup(p => p.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);
        var dialogs = new Mock<IDialogService>();
        var nav = new Mock<INavigationService>();
        return (new ProfileViewModel(profiles.Object, dialogs.Object, nav.Object),
                profiles, dialogs, nav, profile);
    }

    [Fact]
    public async Task LoadAsync_hydrates_from_persisted_profile()
    {
        var (vm, _, _, _, _) = Build(new UserProfile
        {
            Id = Guid.NewGuid(),
            DisplayName = "Test User",
            Level = ExperienceLevel.Intermediate,
        });
        await vm.LoadAsync();
        vm.DisplayName.Should().Be("Test User");
        vm.Experience.Should().Be(ExperienceLevel.Intermediate);
        vm.IsIntermediateSelected.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_with_null_displayname_yields_empty_string()
    {
        // Empty display name must be a writable string so the Entry doesn't NRE
        // when the user starts typing.
        var (vm, _, _, _, _) = Build(new UserProfile { Id = Guid.NewGuid(), DisplayName = null, Level = ExperienceLevel.Beginner });
        await vm.LoadAsync();
        vm.DisplayName.Should().Be(string.Empty);
    }

    [Theory]
    [InlineData(ExperienceLevel.Beginner, true, false, false)]
    [InlineData(ExperienceLevel.Intermediate, false, true, false)]
    [InlineData(ExperienceLevel.Advanced, false, false, true)]
    public void IsXxxSelected_flags_reflect_current_experience(ExperienceLevel level, bool beg, bool inter, bool adv)
    {
        var (vm, _, _, _, _) = Build();
        vm.Experience = level;
        vm.IsBeginnerSelected.Should().Be(beg);
        vm.IsIntermediateSelected.Should().Be(inter);
        vm.IsAdvancedSelected.Should().Be(adv);
    }

    [Fact]
    public void PickCommands_set_the_experience_level()
    {
        var (vm, _, _, _, _) = Build();
        vm.PickAdvancedCommand.Execute(null);
        vm.Experience.Should().Be(ExperienceLevel.Advanced);
        vm.PickBeginnerCommand.Execute(null);
        vm.Experience.Should().Be(ExperienceLevel.Beginner);
    }

    [Fact]
    public async Task SaveAsync_persists_trimmed_name_and_level_then_navigates()
    {
        var (vm, profiles, dialogs, nav, profile) = Build();
        await vm.LoadAsync();

        vm.DisplayName = "  Skywalker  ";
        vm.PickAdvancedCommand.Execute(null);
        await vm.SaveCommand.ExecuteAsync(null);

        profile.DisplayName.Should().Be("Skywalker");
        profile.Level.Should().Be(ExperienceLevel.Advanced);
        profiles.Verify(p => p.UpdateAsync(profile), Times.Once);
        dialogs.Verify(d => d.ShowAlertAsync("Saved", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        nav.Verify(n => n.GoBackAsync(), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_writes_null_for_blank_displayname()
    {
        // Greeting code falls back to "Faster" on null/blank, so blank means
        // "use the default" not "store empty string".
        var (vm, _, _, _, profile) = Build();
        await vm.LoadAsync();
        vm.DisplayName = "   ";
        await vm.SaveCommand.ExecuteAsync(null);
        profile.DisplayName.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_surfaces_repository_errors()
    {
        var (vm, profiles, _, nav, _) = Build();
        profiles.Setup(p => p.UpdateAsync(It.IsAny<UserProfile>())).ThrowsAsync(new InvalidOperationException("db locked"));
        await vm.LoadAsync();

        await vm.SaveCommand.ExecuteAsync(null);

        vm.HasError.Should().BeTrue();
        vm.ErrorMessage.Should().Be("db locked");
        nav.Verify(n => n.GoBackAsync(), Times.Never);
    }

    [Fact]
    public async Task CancelAsync_navigates_back_without_saving()
    {
        var (vm, profiles, _, nav, _) = Build();
        await vm.CancelCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoBackAsync(), Times.Once);
        profiles.Verify(p => p.UpdateAsync(It.IsAny<UserProfile>()), Times.Never);
    }
}
