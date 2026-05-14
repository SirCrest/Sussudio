using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticsSnapshotRefresh_IsSerializedForRecordingResponses()
    {
        var diagnostics = ReadAutomationDiagnosticsHubSourceFamily();
        var countersText = ReadAutomationDiagnosticsHubCountersSource();
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();

        AssertDiagnosticsRefreshCoreOwnership(diagnostics);
        AssertDiagnosticsAlertEventOwnership(diagnostics);
        AssertDiagnosticsSnapshotStatusProjectionOwnership(diagnostics);
        AssertDiagnosticsRefreshSnapshotProjectionOwnership(diagnostics);
        AssertDiagnosticsRefreshPipelineOwnership(diagnostics, dispatcherText);
        AssertDiagnosticsRefreshFlashbackAlertCoverage(diagnostics, countersText);
        AssertDiagnosticsRefreshFlashbackExportOwnership(dispatcherText);
        AssertDiagnosticsRefreshSourceReaderOwnership();

        var diagnosticSessionSources = ReadDiagnosticSessionSourceFamily();
        AssertDiagnosticSessionCoreOwnership(diagnosticSessionSources);
        AssertDiagnosticSessionPlaybackMetricsOwnership(diagnosticSessionSources.SourceFamilyText);
        AssertDiagnosticSessionPreviewMetricsOwnership(diagnosticSessionSources.SourceFamilyText, diagnostics);
        AssertDiagnosticSessionExportRecordingOwnership(diagnosticSessionSources);
        AssertDiagnosticSessionFlashbackScenarioOwnership(diagnosticSessionSources);
        AssertDiagnosticSessionToolSurfaceOwnership();

        return Task.CompletedTask;
    }

}
