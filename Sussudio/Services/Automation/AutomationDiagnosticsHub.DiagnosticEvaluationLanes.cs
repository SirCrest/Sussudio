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

    private static string BuildDecodeLane(
        CaptureHealthSnapshot health,
        MjpegRecentCounters recentMjpeg)
    {
        return $"decode p95={health.MjpegDecodeP95Ms:0.##}ms callbackP95={health.MjpegCallbackP95Ms:0.##}ms dropped={health.MjpegTotalDropped} failures={health.MjpegDecodeFailures + health.MjpegEmitFailures} recentDropped={recentMjpeg.TotalDropped} recentFailures={recentMjpeg.Failures}";
    }

    private static string BuildRecordingLane(CaptureRuntimeSnapshot captureRuntime)
    {
        return $"recording integrity={captureRuntime.RecordingIntegrityStatus} complete={captureRuntime.RecordingIntegrityComplete} seqGaps={captureRuntime.RecordingIntegritySequenceGaps} queueDrops={captureRuntime.RecordingIntegrityQueueDroppedFrames}";
    }

    private static string BuildAudioLane(CaptureRuntimeSnapshot captureRuntime)
    {
        return $"audio integrity={captureRuntime.RecordingIntegrityAudioStatus} drops={captureRuntime.RecordingIntegrityAudioDropEvents} disc={captureRuntime.RecordingIntegrityAudioDiscontinuities} gaps={captureRuntime.RecordingIntegrityAudioCallbackGaps}";
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

}
