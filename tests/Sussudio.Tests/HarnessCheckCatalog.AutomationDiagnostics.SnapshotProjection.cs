using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddAutomationDiagnosticsSnapshotProjectionChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Automation diagnostics snapshot status projection lives in focused partial",
            AutomationDiagnosticsSnapshotStatusProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics snapshot evaluation projection lives in focused partial",
            AutomationDiagnosticsSnapshotEvaluationProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics audio projection lives in focused partial",
            AutomationDiagnosticsSnapshotAudioProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics capture command projection lives in focused partial",
            AutomationDiagnosticsCaptureCommandProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics user settings projection lives in focused partial",
            AutomationDiagnosticsUserSettingsProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics capture format projection lives in focused partial",
            AutomationDiagnosticsCaptureFormatProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics capture transport projection lives in focused partial",
            AutomationDiagnosticsCaptureTransportProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics HDR pipeline projection lives in focused partial",
            AutomationDiagnosticsHdrPipelineProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics capture cadence projection lives in focused partial",
            AutomationDiagnosticsCaptureCadenceProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics visual cadence projection lives in focused partial",
            AutomationDiagnosticsVisualCadenceProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics MJPEG projection lives in focused partial",
            AutomationDiagnosticsMjpegProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics source signal projection lives in focused partial",
            AutomationDiagnosticsSourceSignalProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics source telemetry projection lives in focused partial",
            AutomationDiagnosticsSourceTelemetryProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics recording pipeline projection lives in focused partial",
            AutomationDiagnosticsRecordingPipelineProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics recording backend projection lives in focused partial",
            AutomationDiagnosticsRecordingBackendProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics recording output projection lives in focused partial",
            AutomationDiagnosticsRecordingOutputProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics process resource projection lives in focused partial",
            AutomationDiagnosticsProcessResourceProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics AV sync projection lives in focused partial",
            AutomationDiagnosticsAvSyncProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics preview runtime projection lives in focused partial",
            AutomationDiagnosticsPreviewRuntimeProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics preview D3D projection lives in focused partial",
            AutomationDiagnosticsPreviewD3DProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics Flashback export projection lives in focused partial",
            AutomationDiagnosticsFlashbackExportProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics Flashback recording projection lives in focused partial",
            AutomationDiagnosticsFlashbackRecordingProjection_LivesInFocusedPartial);
        await AddCheckAsync(results,
            "Automation diagnostics Flashback playback projection lives in focused partial",
            AutomationDiagnosticsFlashbackPlaybackProjection_LivesInFocusedPartial);
    }
}
