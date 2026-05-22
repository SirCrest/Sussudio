using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

internal readonly record struct DiagnosticEvaluation(
    string HealthStatus,
    string LikelyStage,
    string Summary,
    string Evidence,
    string SourceLane,
    string DecodeLane,
    string PreviewLane,
    string RenderLane,
    string PresentLane,
    string RecordingLane,
    string AudioLane);

public sealed partial class AutomationDiagnosticsHub
{
    private static DiagnosticEvaluation BuildDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        CaptureRuntimeSnapshot captureRuntime,
        PreviewRuntimeSnapshot previewRuntime,
        bool isPreviewing,
        bool isRecording,
        PerformanceEvaluation performance,
        MjpegRecentCounters recentMjpeg,
        long recentPreviewUnderflows,
        long recentPreviewDeadlineDrops,
        D3DRendererRecentCounters recentRenderer,
        long recentD3DMissedRefreshes,
        long recentD3DStatsFailures,
        FlashbackRecordingRecentCounters recentFlashbackRecording)
    {
        var nowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var playbackCommandQueueAgeMs =
            health.FlashbackPlaybackPendingCommands > 0 &&
            health.FlashbackPlaybackLastCommandQueuedUtcUnixMs > 0 &&
            health.FlashbackPlaybackLastCommandQueuedUtcUnixMs > health.FlashbackPlaybackLastCommandProcessedUtcUnixMs
                ? Math.Max(0, nowUnixMs - health.FlashbackPlaybackLastCommandQueuedUtcUnixMs)
                : 0;
        var playbackCommandFailureAgeMs = health.FlashbackPlaybackLastCommandFailureUtcUnixMs > 0
            ? Math.Max(0, nowUnixMs - health.FlashbackPlaybackLastCommandFailureUtcUnixMs)
            : 0;
        var playbackCommandFailure = string.IsNullOrWhiteSpace(health.FlashbackPlaybackLastCommandFailure)
            ? "None"
            : health.FlashbackPlaybackLastCommandFailure;
        var playbackCommandFailedRecently =
            playbackCommandFailureAgeMs > 0 &&
            playbackCommandFailureAgeMs <= FlashbackPlaybackCommandFailureRecentMs;
        var playbackTargetFps = ResolveFlashbackPlaybackTargetFps(
            health.FlashbackPlaybackTargetFps,
            health.ExpectedFrameRate);
        var lanes = BuildDiagnosticEvaluationLanes(
            health,
            captureRuntime,
            previewRuntime,
            recentMjpeg,
            recentPreviewUnderflows,
            recentPreviewDeadlineDrops,
            recentRenderer,
            recentD3DMissedRefreshes,
            recentD3DStatsFailures,
            playbackTargetFps,
            playbackCommandQueueAgeMs,
            playbackCommandFailureAgeMs,
            playbackCommandFailure);
        var sourceLane = lanes.Source;
        var decodeLane = lanes.Decode;
        var previewLane = lanes.Preview;
        var renderLane = lanes.Render;
        var presentLane = lanes.Present;
        var recordingLane = lanes.Recording;
        var audioLane = lanes.Audio;
        var flashbackDiagnostic = TryBuildFlashbackDiagnosticEvaluation(
            health,
            isRecording,
            recentFlashbackRecording,
            lanes,
            playbackTargetFps,
            playbackCommandQueueAgeMs,
            playbackCommandFailedRecently);
        if (flashbackDiagnostic.HasValue)
        {
            return flashbackDiagnostic.Value;
        }

        var realtimeDiagnostic = TryBuildRealtimeDiagnosticEvaluation(
            health,
            captureRuntime,
            previewRuntime,
            isPreviewing,
            isRecording,
            recentMjpeg,
            recentPreviewUnderflows,
            recentPreviewDeadlineDrops,
            lanes);
        if (realtimeDiagnostic.HasValue)
        {
            return realtimeDiagnostic.Value;
        }

        var summary = performance.PerfectionMet
            ? "No degraded frame lane detected."
            : performance.Summary;
        return new DiagnosticEvaluation(
            performance.PerfectionMet ? "Healthy" : "Warning",
            performance.PerfectionMet ? "none" : "mixed",
            summary,
            performance.PerfectionMet ? "All monitored frame lanes are within current thresholds." : performance.Summary,
            sourceLane,
            decodeLane,
            previewLane,
            renderLane,
            presentLane,
            recordingLane,
            audioLane);
    }
}
