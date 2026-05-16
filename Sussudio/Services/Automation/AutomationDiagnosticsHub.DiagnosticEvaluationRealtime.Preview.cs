using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static DiagnosticEvaluation? TryBuildRealtimePreviewDiagnosticEvaluation(
        CaptureHealthSnapshot health,
        PreviewRuntimeSnapshot previewRuntime,
        bool visualCadenceHealthy,
        long recentPreviewUnderflows,
        long recentPreviewDeadlineDrops,
        DiagnosticEvaluationLanes lanes)
    {
        var sourceLane = lanes.Source;
        var decodeLane = lanes.Decode;
        var previewLane = lanes.Preview;
        var renderLane = lanes.Render;
        var presentLane = lanes.Present;
        var visualLane = lanes.Visual;
        var recordingLane = lanes.Recording;
        var audioLane = lanes.Audio;
        var recentRendererSubmitted = lanes.RecentRendererSubmitted;
        var recentRendererDropPercent = lanes.RecentRendererDropPercent;
        var previewOnePercentLowDegraded =
            IsPreviewOnePercentLowDegraded(
                previewRuntime.DisplayCadenceExpectedIntervalMs,
                previewRuntime.DisplayCadenceSampleCount,
                previewRuntime.DisplayCadenceOnePercentLowFps);

        var previewSubmitFailed = string.Equals(
            health.MjpegPreviewJitterLastDropReason,
            "submit-failed",
            StringComparison.OrdinalIgnoreCase);
        if (previewSubmitFailed ||
            (recentPreviewDeadlineDrops > 0 && !visualCadenceHealthy) ||
            recentPreviewUnderflows > 3)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "preview_scheduler",
                previewSubmitFailed
                    ? "Preview scheduler failed to submit frames."
                    : "Preview scheduler is skipping stale or missing frames.",
                previewLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (recentRendererSubmitted >= DiagnosticThresholds.RendererDropWarningMinSamples &&
            recentRendererDropPercent > DiagnosticThresholds.RendererDropWarningPercent)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "renderer",
                "Renderer pacing is the likely preview bottleneck.",
                renderLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        var presentCadenceOverBudget =
            previewRuntime.DisplayCadenceExpectedIntervalMs > 0 &&
            previewRuntime.DisplayCadenceP95IntervalMs > previewRuntime.DisplayCadenceExpectedIntervalMs * 1.5;
        var unsyncedPresentCallSlow =
            previewRuntime.D3DPresentSyncInterval == 0 &&
            previewRuntime.D3DPresentCallP95Ms > 4.0;
        if (presentCadenceOverBudget ||
            unsyncedPresentCallSlow)
        {
            return new DiagnosticEvaluation(
                "Warning",
                "present_display",
                "Present/display cadence is the likely preview bottleneck.",
                presentLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        if (previewOnePercentLowDegraded)
        {
            if (visualCadenceHealthy)
            {
                return new DiagnosticEvaluation(
                    "Healthy",
                    "none",
                    "Present/display 1% low is below target, but sampled visual cadence confirms source-rate output.",
                    $"{presentLane} | {visualLane}",
                    sourceLane,
                    decodeLane,
                    previewLane,
                    renderLane,
                    presentLane,
                    recordingLane,
                    audioLane);
            }

            return new DiagnosticEvaluation(
                "Warning",
                "present_display",
                "Present/display 1% low is below target.",
                presentLane,
                sourceLane,
                decodeLane,
                previewLane,
                renderLane,
                presentLane,
                recordingLane,
                audioLane);
        }

        return null;
    }
}
