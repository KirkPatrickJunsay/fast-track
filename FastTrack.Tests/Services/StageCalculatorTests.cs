using FastTrack.Services.Implementations;
using FluentAssertions;

namespace FastTrack.Tests.Services;

public class StageCalculatorTests
{
    private readonly StageCalculator _sut = new();

    [Fact]
    public void Exposes_seven_stages_in_order()
    {
        _sut.Stages.Should().HaveCount(7);
        _sut.Stages.Select(s => s.Key).Should().ContainInOrder(
            "anabolic", "catabolic", "fat-burning", "ketosis", "autophagy", "deep-ketosis", "extended");
    }

    [Theory]
    [InlineData(0.0,   "anabolic")]
    [InlineData(2.0,   "anabolic")]
    [InlineData(3.999, "anabolic")]
    [InlineData(4.0,   "catabolic")]
    [InlineData(11.99, "catabolic")]
    [InlineData(12.0,  "fat-burning")]
    [InlineData(17.99, "fat-burning")]
    [InlineData(18.0,  "ketosis")]
    [InlineData(23.99, "ketosis")]
    [InlineData(24.0,  "autophagy")]
    [InlineData(47.99, "autophagy")]
    [InlineData(48.0,  "deep-ketosis")]
    [InlineData(71.99, "deep-ketosis")]
    [InlineData(72.0,  "extended")]
    [InlineData(168.0, "extended")]
    public void GetStage_returns_correct_band(double hours, string expectedKey)
    {
        _sut.GetStage(hours).Key.Should().Be(expectedKey);
    }

    [Fact]
    public void Negative_hours_clamp_to_anabolic()
    {
        _sut.GetStage(-5).Key.Should().Be("anabolic");
    }

    [Fact]
    public void Every_stage_has_icon_long_description_and_feelings()
    {
        foreach (var s in _sut.Stages)
        {
            s.IconAsset.Should().NotBeNullOrWhiteSpace();
            s.LongDescription.Should().NotBeNullOrWhiteSpace();
            s.Feelings.Should().NotBeEmpty();
        }
    }

    [Fact]
    public void Bands_are_contiguous_with_no_gaps()
    {
        var ordered = _sut.Stages.OrderBy(s => s.StartHour).ToList();
        for (var i = 1; i < ordered.Count; i++)
        {
            ordered[i].StartHour.Should().Be(ordered[i - 1].EndHour!.Value);
        }
        ordered[^1].EndHour.Should().BeNull(); // extended is open-ended
    }
}
