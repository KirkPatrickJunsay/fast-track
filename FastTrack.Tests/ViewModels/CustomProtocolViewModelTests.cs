using FastTrack.Data;
using FastTrack.Models;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class CustomProtocolViewModelTests
{
    private static CustomProtocolViewModel BuildSut(Mock<IFastingProtocolRepository>? repo = null)
    {
        repo ??= new Mock<IFastingProtocolRepository>();
        return new CustomProtocolViewModel(repo.Object);
    }

    [Fact]
    public void Defaults_should_be_16_8_my_protocol()
    {
        var vm = BuildSut();
        vm.Name.Should().Be("My protocol");
        vm.FastHours.Should().Be(16);
        vm.EatHours.Should().Be(8);
        vm.SummaryText.Should().Be("16h fast · 8h eat");
        vm.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_persists_protocol_when_valid()
    {
        var repo = new Mock<IFastingProtocolRepository>();
        var vm = BuildSut(repo);
        var saved = false;
        vm.OnSaved = () => { saved = true; return Task.CompletedTask; };

        vm.Name = "OMAD-ish";
        vm.FastHours = 22;
        vm.EatHours = 2;

        await vm.SaveCommand.ExecuteAsync(null);

        vm.HasError.Should().BeFalse();
        saved.Should().BeTrue();
        repo.Verify(r => r.UpsertAsync(It.Is<FastingProtocol>(p =>
            p.Name == "OMAD-ish" && p.FastHours == 22 && p.EatHours == 2 && p.IsCustom && !p.IsPreset
            && p.Difficulty == Difficulty.Advanced)), Times.Once);
    }

    [Theory]
    [InlineData("", 16, 8, "Please give your protocol a name.")]
    [InlineData("   ", 16, 8, "Please give your protocol a name.")]
    [InlineData("ok", 0, 8, "Fast duration must be at least 1 hour.")]
    [InlineData("ok", 200, 8, "Fast duration cannot exceed 168 hours.")]
    [InlineData("ok", 12, 25, "Eating window must be between 0 and 24 hours.")]
    public async Task SaveAsync_surfaces_validation_errors(string name, int fast, int eat, string expectedError)
    {
        var repo = new Mock<IFastingProtocolRepository>();
        var vm = BuildSut(repo);
        vm.Name = name;
        vm.FastHours = fast;
        vm.EatHours = eat;

        await vm.SaveCommand.ExecuteAsync(null);

        vm.HasError.Should().BeTrue();
        vm.ErrorMessage.Should().Be(expectedError);
        repo.Verify(r => r.UpsertAsync(It.IsAny<FastingProtocol>()), Times.Never);
    }

    [Theory]
    [InlineData(16, Difficulty.Beginner)]
    [InlineData(18, Difficulty.Intermediate)]
    [InlineData(20, Difficulty.Intermediate)]
    [InlineData(21, Difficulty.Advanced)]
    [InlineData(48, Difficulty.Advanced)]
    public async Task SaveAsync_classifies_difficulty_by_fast_hours(int fastHours, Difficulty expected)
    {
        var repo = new Mock<IFastingProtocolRepository>();
        var vm = BuildSut(repo);
        vm.FastHours = fastHours;
        vm.EatHours = 4;

        await vm.SaveCommand.ExecuteAsync(null);

        repo.Verify(r => r.UpsertAsync(It.Is<FastingProtocol>(p => p.Difficulty == expected)), Times.Once);
    }
}
