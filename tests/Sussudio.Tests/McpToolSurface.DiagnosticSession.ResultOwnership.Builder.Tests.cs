using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticSessionResultBuilder_OwnsSummaryConstruction()
    {
        AssertDiagnosticSessionResultBuilderCoreOwnership();
        AssertDiagnosticSessionResultBuilderPreviewSchedulerOwnership();
        AssertDiagnosticSessionResultBuilderOverviewAndCaptureProjectionOwnership();
        AssertDiagnosticSessionResultBuilderFlashbackProjectionOwnership();
        AssertDiagnosticSessionResultBuilderPreviewProjectionOwnership();
        AssertDiagnosticSessionResultBuilderAnalysisWarningsOwnership();
        AssertDiagnosticSessionResultBuilderSummaryArtifactHandoffOwnership();

        return Task.CompletedTask;
    }
}
