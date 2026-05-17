using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class DataManagementViewModelTests
{
    private static (DataManagementViewModel Vm,
                    Mock<IDataExportService> Export,
                    Mock<IDataImportService> Import,
                    Mock<IDataResetService> Reset,
                    Mock<IFileShareService> Share,
                    Mock<IFilePickerService> Picker,
                    Mock<IDialogService> Dialogs,
                    Mock<INavigationService> Nav) Build()
    {
        var export = new Mock<IDataExportService>();
        var import = new Mock<IDataImportService>();
        var reset = new Mock<IDataResetService>();
        var share = new Mock<IFileShareService>();
        var picker = new Mock<IFilePickerService>();
        var dialogs = new Mock<IDialogService>();
        var nav = new Mock<INavigationService>();
        var vm = new DataManagementViewModel(export.Object, import.Object, reset.Object, share.Object, picker.Object, dialogs.Object, nav.Object);
        return (vm, export, import, reset, share, picker, dialogs, nav);
    }

    [Fact]
    public async Task ExportAsync_builds_json_and_shares_via_file()
    {
        var (vm, export, _, _, share, _, _, _) = Build();
        export.Setup(e => e.BuildJsonAsync()).ReturnsAsync("{\"schemaVersion\":1}");

        await vm.ExportCommand.ExecuteAsync(null);

        share.Verify(s => s.ShareTextFileAsync(
            It.Is<string>(name => name.StartsWith("fasttrack-export-") && name.EndsWith(".json")),
            "{\"schemaVersion\":1}",
            It.IsAny<string>()), Times.Once);
        vm.StatusMessage.Should().Contain("Built");
        vm.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task ExportAsync_reports_failure_via_status()
    {
        var (vm, export, _, _, _, _, _, _) = Build();
        export.Setup(e => e.BuildJsonAsync()).ThrowsAsync(new Exception("disk full"));
        await vm.ExportCommand.ExecuteAsync(null);
        vm.StatusMessage.Should().Contain("disk full");
    }

    [Fact]
    public async Task ImportAsync_cancelled_picker_is_noop()
    {
        var (vm, _, import, _, _, picker, dialogs, _) = Build();
        picker.Setup(p => p.PickJsonAsync()).ReturnsAsync((PickedFile?)null);
        await vm.ImportCommand.ExecuteAsync(null);
        dialogs.Verify(d => d.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        import.Verify(i => i.ApplyAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ImportAsync_user_cancels_confirmation_does_not_call_import()
    {
        var (vm, _, import, _, _, picker, dialogs, _) = Build();
        picker.Setup(p => p.PickJsonAsync()).ReturnsAsync(new PickedFile("backup.json", "{}"));
        dialogs.Setup(d => d.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
               .ReturnsAsync(false);
        await vm.ImportCommand.ExecuteAsync(null);
        import.Verify(i => i.ApplyAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ImportAsync_success_reports_counts()
    {
        var (vm, _, import, _, _, picker, dialogs, _) = Build();
        picker.Setup(p => p.PickJsonAsync()).ReturnsAsync(new PickedFile("backup.json", "{}"));
        dialogs.Setup(d => d.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
               .ReturnsAsync(true);
        import.Setup(i => i.ApplyAsync("{}")).ReturnsAsync(new ImportResult(true, "ok", 3, 2, 1, 4, 5, 6));

        await vm.ImportCommand.ExecuteAsync(null);

        vm.StatusMessage.Should().Contain("3 fasts");
        vm.StatusMessage.Should().Contain("2 weights");
        vm.StatusMessage.Should().Contain("1 moods");
    }

    [Fact]
    public async Task ImportAsync_failure_message_surfaces_in_status()
    {
        var (vm, _, import, _, _, picker, dialogs, _) = Build();
        picker.Setup(p => p.PickJsonAsync()).ReturnsAsync(new PickedFile("bad.json", "{}"));
        dialogs.Setup(d => d.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
               .ReturnsAsync(true);
        import.Setup(i => i.ApplyAsync(It.IsAny<string>())).ReturnsAsync(new ImportResult(false, "bad schema", 0, 0, 0, 0, 0, 0));

        await vm.ImportCommand.ExecuteAsync(null);
        vm.StatusMessage.Should().Be("bad schema");
    }

    [Fact]
    public async Task ResetAsync_requires_two_confirmations()
    {
        var (vm, _, _, reset, _, _, dialogs, nav) = Build();
        var calls = new Queue<bool>(new[] { true, false }); // first OK, second cancel
        dialogs.Setup(d => d.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
               .ReturnsAsync(() => calls.Dequeue());

        await vm.ResetCommand.ExecuteAsync(null);

        reset.Verify(r => r.ResetAllAsync(), Times.Never);
        nav.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResetAsync_both_confirmations_yes_runs_reset_and_navigates_to_onboarding()
    {
        var (vm, _, _, reset, _, _, dialogs, nav) = Build();
        dialogs.Setup(d => d.ConfirmAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
               .ReturnsAsync(true);

        await vm.ResetCommand.ExecuteAsync(null);

        reset.Verify(r => r.ResetAllAsync(), Times.Once);
        nav.Verify(n => n.GoToAsync("//OnboardingPage"), Times.Once);
        vm.StatusMessage.Should().Contain("reset");
    }

    [Fact]
    public async Task BackAsync_navigates_back()
    {
        var (vm, _, _, _, _, _, _, nav) = Build();
        await vm.BackCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoBackAsync(), Times.Once);
    }
}
