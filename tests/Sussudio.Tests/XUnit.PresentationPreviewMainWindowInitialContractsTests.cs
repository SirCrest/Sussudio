using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewMainWindowInitialContractsTests
{
    public PresentationPreviewMainWindowInitialContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task WindowCloseCancelsUntilRecordingStopCompletes()
        => global::Program.MainWindowClose_CancelsCloseUntilRecordingStopCompletes();

    [Fact]
    public Task WindowScreenshotCaptureCompletesOnDispatcherFailureAndCancellation()
        => global::Program.MainWindowScreenshot_CompletesOnDispatcherFailureAndCancellation();

    [Fact]
    public Task WindowScreenshotNativeCaptureLivesWithController()
        => global::Program.WindowScreenshotNativeCapture_LivesWithWindowScreenshotController();

    [Fact]
    public Task WindowScreenshotImageEncodingLivesInFocusedHelper()
        => global::Program.WindowScreenshotImageEncoding_LivesInFocusedHelper();

    [Fact]
    public Task PropertyChangedRoutingDelegatesToFocusedControllers()
        => global::Program.MainWindowPropertyChangedRouting_DelegatesToFocusedControllers();
}
