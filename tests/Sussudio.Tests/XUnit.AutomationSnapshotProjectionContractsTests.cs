using System.Threading.Tasks;
using Xunit;

namespace Sussudio.Tests;

public sealed class AutomationSnapshotProjectionContractsTests
{
    public AutomationSnapshotProjectionContractsTests()
    {
        global::Program.EnsureTargetAssemblyLoadedForXUnit();
    }

    [Fact]
    public Task AutomationDiagnosticsSnapshotStatusProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsSnapshotStatusProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsSnapshotEvaluationProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsSnapshotEvaluationProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsAudioProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsSnapshotAudioProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsCaptureCommandProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsCaptureCommandProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsUserSettingsProjectionLivesWithSnapshotProjection()
        => global::Program.AutomationDiagnosticsUserSettingsProjection_LivesWithSnapshotProjection();

    [Fact]
    public Task AutomationDiagnosticsCaptureFormatProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsCaptureFormatProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsCaptureTransportProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsCaptureTransportProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsHdrPipelineProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsHdrPipelineProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsCaptureCadenceProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsCaptureCadenceProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsVisualCadenceProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsVisualCadenceProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsMjpegProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsMjpegProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsSourceSignalProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsSourceSignalProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsSourceTelemetryProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsSourceTelemetryProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsRecordingPipelineProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsRecordingPipelineProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsRecordingBackendProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsRecordingBackendProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsRecordingOutputProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsRecordingOutputProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsProcessResourceProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsProcessResourceProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsAvSyncProjectionLivesWithProjectionRoot()
        => global::Program.AutomationDiagnosticsAvSyncProjection_LivesWithProjectionRoot();

    [Fact]
    public Task AutomationDiagnosticsPreviewRuntimeProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsPreviewRuntimeProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsPreviewD3DProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsPreviewD3DProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsFlashbackExportProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsFlashbackExportProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsFlashbackRecordingProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsFlashbackRecordingProjection_LivesInFocusedPartial();

    [Fact]
    public Task AutomationDiagnosticsFlashbackPlaybackProjectionLivesInFocusedPartial()
        => global::Program.AutomationDiagnosticsFlashbackPlaybackProjection_LivesInFocusedPartial();
}
