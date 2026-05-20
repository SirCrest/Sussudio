using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class PresentationPreviewMainViewModelInitialContractsTests
{
    public PresentationPreviewMainViewModelInitialContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task RecordingStartAndStopFailuresPropagateToCallers()
        => global::Program.MainViewModelCapture_RecordingFailuresPropagateToCallers();
}
