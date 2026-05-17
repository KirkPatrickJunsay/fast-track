using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class ProtocolsViewModelTests
{
    private static (ProtocolsViewModel Vm,
                    Mock<IFastingProtocolRepository> Protocols,
                    Mock<IUserProfileRepository> Profiles,
                    Mock<INavigationService> Nav,
                    UserProfile Profile)
        Build(IReadOnlyList<FastingProtocol>? protocols = null, Guid? defaultId = null)
    {
        protocols ??= new List<FastingProtocol>
        {
            new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000001"), Name = "16:8", FastHours = 16, EatHours = 8, Difficulty = Difficulty.Beginner, IsPreset = true },
            new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000002"), Name = "18:6", FastHours = 18, EatHours = 6, Difficulty = Difficulty.Intermediate, IsPreset = true },
        };
        var protocolsRepo = new Mock<IFastingProtocolRepository>();
        protocolsRepo.Setup(p => p.GetAllAsync()).ReturnsAsync(protocols);

        var profile = new UserProfile { Id = Guid.NewGuid(), LastUsedProtocolId = defaultId };
        var profilesRepo = new Mock<IUserProfileRepository>();
        profilesRepo.Setup(p => p.GetOrCreateAsync()).ReturnsAsync(profile);
        profilesRepo.Setup(p => p.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);

        var nav = new Mock<INavigationService>();
        var vm = new ProtocolsViewModel(protocolsRepo.Object, profilesRepo.Object, nav.Object);
        return (vm, protocolsRepo, profilesRepo, nav, profile);
    }

    [Fact]
    public async Task LoadAsync_maps_protocols_to_items_and_marks_default()
    {
        var defaultId = Guid.Parse("a1000000-0000-0000-0000-000000000002");
        var (vm, _, _, _, _) = Build(defaultId: defaultId);
        await vm.LoadAsync();

        vm.Items.Should().HaveCount(2);
        vm.Items.Single(i => i.IsDefault).Name.Should().Be("18:6");
        vm.Items.First().Subtitle.Should().Be("16h fast · 8h eat");
    }

    [Fact]
    public async Task LoadAsync_with_no_default_marks_none()
    {
        var (vm, _, _, _, _) = Build();
        await vm.LoadAsync();
        vm.Items.Should().OnlyContain(i => !i.IsDefault);
    }

    [Fact]
    public async Task SetDefault_writes_profile_and_updates_chips()
    {
        var (vm, _, profiles, _, profile) = Build();
        await vm.LoadAsync();

        var target = vm.Items.Last();
        await vm.SetDefaultCommand.ExecuteAsync(target);

        profile.LastUsedProtocolId.Should().Be(target.Id);
        profiles.Verify(p => p.UpdateAsync(profile), Times.Once);
        vm.Items.Single(i => i.IsDefault).Id.Should().Be(target.Id);
    }

    [Fact]
    public async Task NewCustom_navigates_to_create_page()
    {
        var (vm, _, _, nav, _) = Build();
        await vm.NewCustomCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoToAsync("CustomProtocolPage"), Times.Once);
    }
}
