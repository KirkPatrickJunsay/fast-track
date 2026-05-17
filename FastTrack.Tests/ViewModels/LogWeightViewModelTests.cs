using FastTrack.Models;
using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class LogWeightViewModelTests
{
    private static (LogWeightViewModel Vm, Mock<IWeightService> Weights, Mock<INavigationService> Nav) Build()
    {
        var weights = new Mock<IWeightService>();
        weights.Setup(w => w.LogAsync(It.IsAny<double>(), It.IsAny<DateTime?>(), It.IsAny<string?>()))
               .ReturnsAsync(new WeightEntry());
        var nav = new Mock<INavigationService>();
        return (new LogWeightViewModel(weights.Object, nav.Object), weights, nav);
    }

    [Fact]
    public void Defaults_are_kg_and_70()
    {
        var (vm, _, _) = Build();
        vm.UseLbs.Should().BeFalse();
        vm.UnitLabel.Should().Be("kg");
        vm.TextValue.Should().Be("70.0");
    }

    [Fact]
    public void PickLb_converts_value_and_flips_unit()
    {
        var (vm, _, _) = Build();
        vm.TextValue = "70";
        vm.PickLbCommand.Execute(null);
        vm.UseLbs.Should().BeTrue();
        double.Parse(vm.TextValue, System.Globalization.CultureInfo.InvariantCulture)
            .Should().BeApproximately(154.3, 0.5);
    }

    [Fact]
    public void PickKg_converts_back_and_flips_unit()
    {
        var (vm, _, _) = Build();
        vm.PickLbCommand.Execute(null);
        vm.PickKgCommand.Execute(null);
        vm.UseLbs.Should().BeFalse();
        double.Parse(vm.TextValue, System.Globalization.CultureInfo.InvariantCulture)
            .Should().BeApproximately(70.0, 0.5);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-3")]
    [InlineData("0")]
    public async Task Save_with_invalid_input_sets_error(string text)
    {
        var (vm, weights, nav) = Build();
        vm.TextValue = text;
        await vm.SaveCommand.ExecuteAsync(null);
        vm.HasError.Should().BeTrue();
        weights.Verify(w => w.LogAsync(It.IsAny<double>(), It.IsAny<DateTime?>(), It.IsAny<string?>()), Times.Never);
        nav.Verify(n => n.GoBackAsync(), Times.Never);
    }

    [Fact]
    public async Task Save_in_kg_passes_value_through()
    {
        var (vm, weights, nav) = Build();
        vm.TextValue = "72.5";
        await vm.SaveCommand.ExecuteAsync(null);
        weights.Verify(w => w.LogAsync(72.5, It.IsAny<DateTime?>(), null), Times.Once);
        nav.Verify(n => n.GoBackAsync(), Times.Once);
    }

    [Fact]
    public async Task Save_in_lbs_converts_to_kg()
    {
        var (vm, weights, _) = Build();
        vm.PickLbCommand.Execute(null);
        vm.TextValue = "154.0";
        await vm.SaveCommand.ExecuteAsync(null);
        weights.Verify(w => w.LogAsync(It.Is<double>(d => Math.Abs(d - 69.85) < 0.1), It.IsAny<DateTime?>(), null), Times.Once);
    }

    [Fact]
    public async Task Save_passes_trimmed_note_when_provided()
    {
        var (vm, weights, _) = Build();
        vm.TextValue = "70";
        vm.Note = "  morning  ";
        await vm.SaveCommand.ExecuteAsync(null);
        weights.Verify(w => w.LogAsync(70, It.IsAny<DateTime?>(), "  morning  "), Times.Once);
    }

    [Fact]
    public async Task Cancel_navigates_back_without_saving()
    {
        var (vm, weights, nav) = Build();
        await vm.CancelCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoBackAsync(), Times.Once);
        weights.Verify(w => w.LogAsync(It.IsAny<double>(), It.IsAny<DateTime?>(), It.IsAny<string?>()), Times.Never);
    }
}
