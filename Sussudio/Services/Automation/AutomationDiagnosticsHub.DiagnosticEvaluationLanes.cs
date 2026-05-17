using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static DiagnosticEvaluationLanes BuildDiagnosticEvaluationLanes(
        CaptureHealthSnapshot health,
        CaptureRuntimeSnapshot captureRuntime,
        PreviewRuntimeSnapshot previewRuntime,
        MjpegRecentCounters recentMjpeg,
        long recentPreviewUnderflows,
        long recentPreviewDeadlineDrops,
        D3DRendererRecentCounters recentRenderer,
        long recentD3DMissedRefreshes,
        long recentD3DStatsFailures,
        double playbackTargetFps,
        long playbackCommandQueueAgeMs,
        long playbackCommandFailureAgeMs,
        string playbackCommandFailure)
    {
        var sourceLane = BuildSourceLane(health);
        var decodeLane = BuildDecodeLane(health, recentMjpeg);
        var previewLane = BuildPreviewLane(
            health,
            recentPreviewUnderflows,
            recentPreviewDeadlineDrops);
        var renderLane = BuildRenderLane(previewRuntime, recentRenderer);
        var presentLane = BuildPresentLane(
            previewRuntime,
            recentD3DMissedRefreshes,
            recentD3DStatsFailures);
        var visualLane = BuildVisualLane(health);
        var mjpegDuplicateLane = FormatMjpegDuplicateCadenceDetail(health);
        var sourceSignalLane = BuildSourceSignalLane(health, sourceLane);
        var recordingLane = BuildRecordingLane(captureRuntime);
        var audioLane = BuildAudioLane(captureRuntime);
        var flashbackRecordingLane = BuildFlashbackRecordingLane(health);
        var exportLane = BuildFlashbackExportLane(health);
        var tempCacheLane = BuildFlashbackTempCacheLane(health);
        var playbackCommandLane = BuildFlashbackPlaybackCommandLane(
            health,
            playbackCommandQueueAgeMs,
            playbackCommandFailureAgeMs,
            playbackCommandFailure);
        var playbackPerfLane = BuildFlashbackPlaybackPerformanceLane(
            health,
            previewRuntime,
            playbackTargetFps);

        return new DiagnosticEvaluationLanes(
            sourceLane,
            decodeLane,
            previewLane,
            renderLane.Text,
            presentLane,
            visualLane,
            mjpegDuplicateLane,
            sourceSignalLane,
            recordingLane,
            audioLane,
            flashbackRecordingLane,
            exportLane,
            tempCacheLane,
            playbackCommandLane,
            playbackPerfLane,
            renderLane.RecentSubmitted,
            renderLane.RecentDropPercent);
    }

    private readonly record struct DiagnosticEvaluationLanes(
        string Source,
        string Decode,
        string Preview,
        string Render,
        string Present,
        string Visual,
        string MjpegDuplicate,
        string SourceSignal,
        string Recording,
        string Audio,
        string FlashbackRecording,
        string Export,
        string TempCache,
        string PlaybackCommand,
        string PlaybackPerf,
        long RecentRendererSubmitted,
        double RecentRendererDropPercent);

    private readonly record struct DiagnosticEvaluationRenderLane(
        string Text,
        long RecentSubmitted,
        double RecentDropPercent);
}
