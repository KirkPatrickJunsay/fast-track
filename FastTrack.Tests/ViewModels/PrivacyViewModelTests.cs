using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class PrivacyViewModelTests
{
    private static (PrivacyViewModel Vm, Mock<INavigationService> Nav) Build()
    {
        var nav = new Mock<INavigationService>();
        return (new PrivacyViewModel(nav.Object), nav);
    }

    [Fact]
    public void Copy_blocks_are_populated_and_non_empty()
    {
        var (vm, _) = Build();
        vm.Intro.Should().NotBeNullOrWhiteSpace();
        vm.LocalOnlyBody.Should().NotBeNullOrWhiteSpace();
        vm.WhatWeCollectBody.Should().NotBeNullOrWhiteSpace();
        vm.PermissionsBody.Should().NotBeNullOrWhiteSpace();
        vm.YourRightsBody.Should().NotBeNullOrWhiteSpace();
        vm.MedicalBody.Should().NotBeNullOrWhiteSpace();
        vm.TermsBody.Should().NotBeNullOrWhiteSpace();
        vm.LastUpdatedDisplay.Should().Contain("Last updated");
        vm.VersionText.Should().Contain("Fast Track");
    }

    [Fact]
    public void Medical_block_warns_about_at_risk_groups()
    {
        var (vm, _) = Build();
        // The medical disclaimer is a load-bearing legal block; verify it still
        // names the high-risk groups so a future copy edit can't silently drop them.
        vm.MedicalBody.Should().ContainAll("pregnant", "18", "diabetes", "disordered eating", "medication");
    }

    [Fact]
    public void LocalOnly_block_states_no_servers_no_accounts()
    {
        var (vm, _) = Build();
        vm.LocalOnlyBody.ToLowerInvariant().Should().Contain("phone");
        vm.LocalOnlyBody.ToLowerInvariant().Should().Contain("server");
        vm.WhatWeCollectBody.ToLowerInvariant().Should().Contain("nothing");
    }

    [Fact]
    public async Task BackAsync_calls_go_back()
    {
        var (vm, nav) = Build();
        await vm.BackCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoBackAsync(), Times.Once);
    }
}
