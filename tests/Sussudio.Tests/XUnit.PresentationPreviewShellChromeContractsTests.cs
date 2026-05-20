using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewShellChromeContractsTests
{
    public PresentationPreviewShellChromeContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task SettingsShelfLifecycleLivesInController()
        => global::Program.SettingsShelfLifecycle_LivesInController();

    [Fact]
    public Task MainWindowTitlePresentationLivesInController()
        => global::Program.MainWindowTitlePresentation_LivesInController();

    [Fact]
    public Task WindowTitleControllerFormatsBuildStampAndRecordingSuffix()
        => global::Program.WindowTitleController_FormatsBuildStampAndRecordingSuffix();

    [Fact]
    public Task LiveSignalInfoPresentationLivesInController()
        => global::Program.LiveSignalInfoPresentation_LivesInController();

    [Fact]
    public Task StatusStripPresentationLivesInController()
        => global::Program.StatusStripPresentation_LivesInController();
}
