using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static DiagnosticEvaluation? TryBuildRealtimeDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        CaptureRuntimeSnapshot captureRuntime,
        PreviewRuntimeSnapshot previewRuntime,
        bool isPreviewing,
        bool isRecording,
        MjpegRecentCounters recentMjpeg,
        long recentPreviewUnderflows,
        long recentPreviewDeadlineDrops,
        DiagnosticEvaluationLanes lanes)
    {
        var visualCadenceHealthy =
            IsVisualCadenceHealthy(
                health.ExpectedFrameRate,
                health.VisualCadenceSampleCount,
                health.VisualCadenceChangeObservedFps,
                health.VisualCadenceRepeatFramePercent,
                health.VisualCadenceLongestRepeatRun);

        return TryBuildRealtimeStateDiagnosticEvaluation(health, isPreviewing, isRecording, lanes) ??
               TryBuildRealtimeRecordingDiagnosticEvaluation(captureRuntime, health, isRecording, lanes) ??
               TryBuildRealtimeSourceDiagnosticEvaluation(health, isPreviewing, visualCadenceHealthy, lanes) ??
               TryBuildRealtimeMjpegDiagnosticEvaluation(health, recentMjpeg, lanes) ??
               TryBuildRealtimePreviewDiagnosticEvaluation(
                   health,
                   previewRuntime,
                   visualCadenceHealthy,
                   recentPreviewUnderflows,
                   recentPreviewDeadlineDrops,
                   lanes);
    }

    private static DiagnosticEvaluation? TryBuildRealtimeStateDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        bool isPreviewing,
        bool isRecording,
        DiagnosticEvaluationLanes lanes)
    {
        if (!isPreviewing && !isRecording)
        {
            return new DiagnosticEvaluation(
                "Idle",
                "diagnostic_unavailable",
                "Preview and recording are idle.",
                "Start preview or recording to collect live frame-lane diagnostics.",
                lanes.Source,
                lanes.Decode,
                lanes.Preview,
                lanes.Render,
                lanes.Present,
                lanes.Recording,
                lanes.Audio);
        }

        if (health.CaptureCadenceSampleCount >= 30)
        {
            return null;
        }

        return new DiagnosticEvaluation(
            "WarmingUp",
            "diagnostic_unavailable",
            "Waiting for enough capture cadence samples.",
            lanes.Source,
            lanes.Source,
            lanes.Decode,
            lanes.Preview,
            lanes.Render,
            lanes.Present,
            lanes.Recording,
            lanes.Audio);
    }
}
