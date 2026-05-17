using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Implementations;
using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class FastDetailViewModelTests
{
    private static FastDetailViewModel BuildSut(
        Fast? fast = null,
        FastingProtocol? protocol = null,
        IReadOnlyList<MoodEntry>? moods = null,
        Mock<INavigationService>? nav = null)
    {
        var fastsRepo = new Mock<IFastRepository>();
        if (fast is not null) fastsRepo.Setup(r => r.GetByIdAsync(fast.Id)).ReturnsAsync(fast);

        var protocolsRepo = new Mock<IFastingProtocolRepository>();
        protocolsRepo.Setup(p => p.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(protocol);

        var moodsSvc = new Mock<IMoodService>();
        moodsSvc.Setup(m => m.GetForFastAsync(It.IsAny<Guid>())).ReturnsAsync(moods ?? Array.Empty<MoodEntry>());

        nav ??= new Mock<INavigationService>();

        return new FastDetailViewModel(
            fastsRepo.Object, protocolsRepo.Object, moodsSvc.Object,
            new StageCalculator(), nav.Object);
    }

    [Fact]
    public async Task Load_with_invalid_id_sets_error()
    {
        var vm = BuildSut();
        vm.FastId = "not-a-guid";
        await vm.LoadAsync();
        vm.HasError.Should().BeTrue();
        vm.IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task Load_with_missing_fast_sets_error()
    {
        var vm = BuildSut();
        vm.FastId = Guid.NewGuid().ToString();
        await vm.LoadAsync();
        vm.ErrorMessage.Should().Be("Fast not found.");
    }

    [Fact]
    public async Task Load_completed_goal_met_renders_full_state()
    {
        var start = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);
        var fast = new Fast
        {
            Id = Guid.NewGuid(),
            ProtocolId = Guid.NewGuid(),
            StartUtc = start,
            EndUtc = start.AddHours(20),
            GoalHours = 16,
            EndReason = FastEndReason.Completed,
        };
        var vm = BuildSut(fast, new FastingProtocol { Id = fast.ProtocolId, Name = "16:8" });
        vm.FastId = fast.Id.ToString();

        await vm.LoadAsync();

        vm.IsLoaded.Should().BeTrue();
        vm.ProtocolDisplay.Should().Be("16:8");
        vm.DurationDisplay.Should().Be("20h 0m");
        vm.GoalDisplay.Should().Be("Goal 16h");
        vm.GoalMet.Should().BeTrue();
        vm.StageReachedDisplay.Should().Be("Ketosis"); // 20h falls in 18–24
        vm.EndReasonDisplay.Should().Be("Completed");
        vm.HasMoods.Should().BeFalse();
    }

    [Fact]
    public async Task Load_with_mood_entries_builds_timeline()
    {
        var start = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc);
        var fast = new Fast { Id = Guid.NewGuid(), ProtocolId = Guid.NewGuid(),
            StartUtc = start, EndUtc = start.AddHours(16), GoalHours = 16, EndReason = FastEndReason.Completed };
        var moods = new[]
        {
            new MoodEntry { Id = 1, FastId = fast.Id, MoodLevel = 3, TimestampUtc = start.AddHours(1), Note = "good start" },
            new MoodEntry { Id = 2, FastId = fast.Id, MoodLevel = 4, TimestampUtc = start.AddHours(8.5) },
        };
        var vm = BuildSut(fast, new FastingProtocol { Id = fast.ProtocolId, Name = "16:8" }, moods);
        vm.FastId = fast.Id.ToString();

        await vm.LoadAsync();

        vm.HasMoods.Should().BeTrue();
        vm.MoodTimeline.Should().HaveCount(2);
        vm.MoodTimeline[0].Label.Should().Be("1h in");
        vm.MoodTimeline[0].Emoji.Should().Be("🙂");
        vm.MoodTimeline[1].Label.Should().Be("8.5h in");
        vm.MoodTimeline[1].Emoji.Should().Be("😊");
    }

    [Theory]
    [InlineData(FastEndReason.Hungry, "Ended early — hungry")]
    [InlineData(FastEndReason.SocialEvent, "Ended early — social event")]
    [InlineData(FastEndReason.Illness, "Ended early — feeling unwell")]
    [InlineData(FastEndReason.Other, "Ended early")]
    public async Task Load_formats_end_reason(FastEndReason reason, string expected)
    {
        var start = DateTime.UtcNow.AddHours(-12);
        var fast = new Fast { Id = Guid.NewGuid(), ProtocolId = Guid.NewGuid(),
            StartUtc = start, EndUtc = DateTime.UtcNow, GoalHours = 16, EndReason = reason };
        var vm = BuildSut(fast);
        vm.FastId = fast.Id.ToString();
        await vm.LoadAsync();
        vm.EndReasonDisplay.Should().Be(expected);
    }

    [Fact]
    public async Task EditAsync_navigates_to_edit_page_with_id()
    {
        var nav = new Mock<INavigationService>();
        var fast = new Fast { Id = Guid.NewGuid(), ProtocolId = Guid.NewGuid(),
            StartUtc = DateTime.UtcNow.AddHours(-16), EndUtc = DateTime.UtcNow, GoalHours = 16 };
        var vm = BuildSut(fast, nav: nav);
        vm.FastId = fast.Id.ToString();
        await vm.LoadAsync();
        await vm.EditCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoToAsync($"EditFastPage?fastId={fast.Id}"), Times.Once);
    }

    [Fact]
    public async Task BackAsync_navigates_back()
    {
        var nav = new Mock<INavigationService>();
        var vm = BuildSut(nav: nav);
        await vm.BackCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoBackAsync(), Times.Once);
    }

    [Fact]
    public async Task Load_in_progress_fast_renders_in_progress_label()
    {
        var fast = new Fast { Id = Guid.NewGuid(), ProtocolId = Guid.NewGuid(),
            StartUtc = DateTime.UtcNow.AddHours(-2), EndUtc = null, GoalHours = 16 };
        var vm = BuildSut(fast);
        vm.FastId = fast.Id.ToString();
        await vm.LoadAsync();
        vm.EndedLocal.Should().Be("In progress");
        vm.EndReasonDisplay.Should().Be("In progress");
    }
}
