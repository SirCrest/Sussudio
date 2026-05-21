namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    private readonly record struct DiagnosticSessionResultProjectionSet(
        DiagnosticSessionOverviewResultProjection Overview,
        DiagnosticSessionCaptureResultProjection Capture,
        DiagnosticSessionFlashbackPlaybackResultProjection FlashbackPlayback,
        DiagnosticSessionFlashbackRecordingResultProjection FlashbackRecording,
        DiagnosticSessionFlashbackExportResultProjection FlashbackExport,
        DiagnosticSessionPreviewResultProjection Preview,
        DiagnosticSessionPreviewSchedulerResultProjection PreviewScheduler,
        DiagnosticSessionPreviewD3DResultProjection PreviewD3D,
        DiagnosticSessionPreviewVisualCadenceResultProjection PreviewVisualCadence);

    private static DiagnosticSessionResultProjectionSet BuildResultProjectionSet(
        DiagnosticSessionResultBuildRequest request,
        DiagnosticSessionRunState runState,
        DiagnosticSessionResultAnalysis analysis)
    {
        return new DiagnosticSessionResultProjectionSet(
            Overview: BuildOverviewResultProjection(request, runState, analysis),
            Capture: BuildCaptureResultProjection(analysis),
            FlashbackPlayback: BuildFlashbackPlaybackResultProjection(analysis),
            FlashbackRecording: BuildFlashbackRecordingResultProjection(analysis),
            FlashbackExport: BuildFlashbackExportResultProjection(analysis),
            Preview: BuildPreviewResultProjection(analysis),
            PreviewScheduler: BuildPreviewSchedulerResultProjection(analysis),
            PreviewD3D: BuildPreviewD3DResultProjection(analysis),
            PreviewVisualCadence: BuildPreviewVisualCadenceResultProjection(analysis));
    }
}
