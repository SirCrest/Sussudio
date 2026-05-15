using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendPreviewSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine();
        builder.AppendLine("== Preview ==");
        var rendererMode = Get(snapshot, "PreviewRendererMode");
        builder.AppendLine($"Renderer: {rendererMode} | Startup: {Get(snapshot, "PreviewStartupState")} | First Visual: {Get(snapshot, "PreviewFirstVisualConfirmed")}");
        if (rendererMode == "GpuMediaSource")
        {
            builder.AppendLine($"GPU Playback: {Get(snapshot, "PreviewGpuPlaybackState")} | Video: {Get(snapshot, "PreviewGpuNaturalVideoWidth")}x{Get(snapshot, "PreviewGpuNaturalVideoHeight")} | Position: {Get(snapshot, "PreviewGpuPositionMs")}ms | Events: {Get(snapshot, "PreviewGpuPositionEventCount")}");
            return;
        }

        if (rendererMode == "D3D11VideoProcessor" ||
            rendererMode == "Nv12Shader" ||
            rendererMode == "HdrShader" ||
            rendererMode == "HdrPassthrough")
        {
            builder.AppendLine($"D3D Swap Chain: {Get(snapshot, "PreviewD3DSwapChainAddress", "N/A")}");
            builder.AppendLine($"D3D Frames: {Get(snapshot, "PreviewD3DFramesSubmitted")} submitted, {Get(snapshot, "PreviewD3DFramesRendered")} rendered, {Get(snapshot, "PreviewD3DFramesDropped")} dropped, pending={Get(snapshot, "PreviewD3DPendingFrameCount")}");
            var renderThreadFailures = Get(snapshot, "PreviewD3DRenderThreadFailureCount", "0");
            if (renderThreadFailures != "0")
            {
                builder.AppendLine($"D3D Render Thread Failures: {renderThreadFailures} last={Get(snapshot, "PreviewD3DLastRenderThreadFailureType")} hr={Get(snapshot, "PreviewD3DLastRenderThreadFailureHResult")} msg={Get(snapshot, "PreviewD3DLastRenderThreadFailureMessage")}");
            }
            builder.AppendLine($"Color: input={Get(snapshot, "PreviewD3DInputColorSpace")} output={Get(snapshot, "PreviewD3DOutputColorSpace")}");
            builder.AppendLine($"Frame Time: target={FormatIntervalMs(snapshot, "PreviewCadenceExpectedIntervalMs")} avg={Get(snapshot, "PreviewCadenceAverageIntervalMs")}ms P95={Get(snapshot, "PreviewCadenceP95IntervalMs")}ms max={Get(snapshot, "PreviewCadenceMaxIntervalMs")}ms");
            builder.AppendLine($"Average Rate: {Get(snapshot, "PreviewCadenceObservedFps")} fps | 5% Low: {Get(snapshot, "PreviewCadenceFivePercentLowFps")} fps | 1% Low: {Get(snapshot, "PreviewCadenceOnePercentLowFps")} fps | Samples: {Get(snapshot, "PreviewCadenceSampleCount")} over {Get(snapshot, "PreviewCadenceSampleDurationMs")}ms");
            builder.AppendLine($"Pacing Classifier: stage={Get(snapshot, "PreviewPacingLikelySlowStage")} confidence={Get(snapshot, "PreviewPacingSlowStageConfidence")} evidence={Get(snapshot, "PreviewPacingSlowStageEvidence", "")}");
            builder.AppendLine($"D3D CPU timing: input/upload avg={Get(snapshot, "PreviewD3DInputUploadCpuAvgMs")}ms P95={Get(snapshot, "PreviewD3DInputUploadCpuP95Ms")}ms P99={Get(snapshot, "PreviewD3DInputUploadCpuP99Ms")}ms max={Get(snapshot, "PreviewD3DInputUploadCpuMaxMs")}ms | render-submit avg={Get(snapshot, "PreviewD3DRenderSubmitCpuAvgMs")}ms P95={Get(snapshot, "PreviewD3DRenderSubmitCpuP95Ms")}ms P99={Get(snapshot, "PreviewD3DRenderSubmitCpuP99Ms")}ms max={Get(snapshot, "PreviewD3DRenderSubmitCpuMaxMs")}ms | present-call avg={Get(snapshot, "PreviewD3DPresentCallAvgMs")}ms P95={Get(snapshot, "PreviewD3DPresentCallP95Ms")}ms P99={Get(snapshot, "PreviewD3DPresentCallP99Ms")}ms max={Get(snapshot, "PreviewD3DPresentCallMaxMs")}ms | total-frame avg={Get(snapshot, "PreviewD3DTotalFrameCpuAvgMs")}ms P95={Get(snapshot, "PreviewD3DTotalFrameCpuP95Ms")}ms P99={Get(snapshot, "PreviewD3DTotalFrameCpuP99Ms")}ms max={Get(snapshot, "PreviewD3DTotalFrameCpuMaxMs")}ms samples={Get(snapshot, "PreviewD3DCpuTimingSampleCount")}");
            builder.AppendLine($"D3D pipeline latency: avg={Get(snapshot, "PreviewD3DPipelineLatencyAvgMs")}ms P95={Get(snapshot, "PreviewD3DPipelineLatencyP95Ms")}ms P99={Get(snapshot, "PreviewD3DPipelineLatencyP99Ms")}ms max={Get(snapshot, "PreviewD3DPipelineLatencyMaxMs")}ms last={Get(snapshot, "PreviewD3DLastRenderedPipelineLatencyMs")}ms samples={Get(snapshot, "PreviewD3DPipelineLatencySampleCount")}");
            builder.AppendLine($"D3D frame-latency wait: enabled={Get(snapshot, "PreviewD3DFrameLatencyWaitEnabled")} handle={Get(snapshot, "PreviewD3DFrameLatencyWaitHandleActive")} calls={Get(snapshot, "PreviewD3DFrameLatencyWaitCallCount")} signaled={Get(snapshot, "PreviewD3DFrameLatencyWaitSignaledCount")} timeouts={Get(snapshot, "PreviewD3DFrameLatencyWaitTimeoutCount")} unexpected={Get(snapshot, "PreviewD3DFrameLatencyWaitUnexpectedResultCount")} lastResult={Get(snapshot, "PreviewD3DFrameLatencyWaitLastResult")} last={Get(snapshot, "PreviewD3DFrameLatencyWaitLastMs")}ms avg={Get(snapshot, "PreviewD3DFrameLatencyWaitAvgMs")}ms P95={Get(snapshot, "PreviewD3DFrameLatencyWaitP95Ms")}ms max={Get(snapshot, "PreviewD3DFrameLatencyWaitMaxMs")}ms samples={Get(snapshot, "PreviewD3DFrameLatencyWaitSampleCount")}");
            builder.AppendLine($"D3D DXGI stats: ok={Get(snapshot, "PreviewD3DFrameStatsSuccessCount")}/{Get(snapshot, "PreviewD3DFrameStatsSampleCount")} failures={Get(snapshot, "PreviewD3DFrameStatsFailureCount")} recentFailures={Get(snapshot, "PreviewD3DFrameStatsRecentFailureCount")} missedRefresh={Get(snapshot, "PreviewD3DFrameStatsMissedRefreshCount")} recentMissed={Get(snapshot, "PreviewD3DFrameStatsRecentMissedRefreshCount")} lastError={Get(snapshot, "PreviewD3DFrameStatsLastError", "")}");
            builder.AppendLine($"D3D Ownership: submitted present={Get(snapshot, "PreviewD3DLastSubmittedPreviewPresentId")} sourceSeq={Get(snapshot, "PreviewD3DLastSubmittedSourceSequenceNumber")} pts={Get(snapshot, "PreviewD3DLastSubmittedSourcePtsTicks")} | rendered present={Get(snapshot, "PreviewD3DLastRenderedPreviewPresentId")} sourceSeq={Get(snapshot, "PreviewD3DLastRenderedSourceSequenceNumber")} pts={Get(snapshot, "PreviewD3DLastRenderedSourcePtsTicks")} schedulerToPresent={Get(snapshot, "PreviewD3DLastRenderedSchedulerToPresentMs")}ms pipeline={Get(snapshot, "PreviewD3DLastRenderedPipelineLatencyMs")}ms | lastDrop={Get(snapshot, "PreviewD3DLastDropReason")} dropPts={Get(snapshot, "PreviewD3DLastDroppedSourcePtsTicks")}");
            AppendPreviewSlowFrameDiagnostics(builder, snapshot);
            return;
        }

        builder.AppendLine($"Frames: {Get(snapshot, "PreviewFramesArrived")} arrived, {Get(snapshot, "PreviewFramesDisplayed")} displayed, {Get(snapshot, "PreviewFramesDropped")} dropped");
        builder.AppendLine($"Average Rate: {Get(snapshot, "PreviewCadenceObservedFps")} fps | 5% Low: {Get(snapshot, "PreviewCadenceFivePercentLowFps")} fps | 1% Low: {Get(snapshot, "PreviewCadenceOnePercentLowFps")} fps | Samples: {Get(snapshot, "PreviewCadenceSampleCount")} over {Get(snapshot, "PreviewCadenceSampleDurationMs")}ms");
        builder.AppendLine($"Pacing Classifier: stage={Get(snapshot, "PreviewPacingLikelySlowStage")} confidence={Get(snapshot, "PreviewPacingSlowStageConfidence")} evidence={Get(snapshot, "PreviewPacingSlowStageEvidence", "")}");
    }

    internal static void AppendPreviewSlowFrameDiagnostics(StringBuilder builder, JsonElement snapshot)
    {
        if (!snapshot.TryGetProperty("PreviewD3DRecentSlowFrames", out var slowFrames) ||
            slowFrames.ValueKind != JsonValueKind.Array ||
            slowFrames.GetArrayLength() <= 0)
        {
            return;
        }

        var lines = new List<string>();
        foreach (var frame in slowFrames.EnumerateArray())
        {
            if (frame.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            lines.Add(
                $"present={Get(frame, "PreviewPresentId")} srcSeq={Get(frame, "SourceSequenceNumber")} " +
                $"reason={Get(frame, "SlowReason")} target={FormatDiagnosticMs(frame, "ExpectedIntervalMs")} " +
                $"over={FormatDiagnosticMs(frame, "WorstOverBudgetMs")} interval={FormatDiagnosticMs(frame, "PresentIntervalMs")} total={FormatDiagnosticMs(frame, "TotalFrameCpuMs")} " +
                $"upload={FormatDiagnosticMs(frame, "InputUploadCpuMs")} render={FormatDiagnosticMs(frame, "RenderSubmitCpuMs")} " +
                $"presentCall={FormatDiagnosticMs(frame, "PresentCallMs")} sched={FormatDiagnosticMs(frame, "SchedulerToPresentMs")} pipeline={FormatDiagnosticMs(frame, "PipelineLatencyMs")} " +
                $"pending={Get(frame, "PendingFrameCount")} dxgiDelta={Get(frame, "DxgiPresentDelta")}/{Get(frame, "DxgiPresentRefreshDelta")}/{Get(frame, "DxgiSyncRefreshDelta")}");
            if (lines.Count >= 3)
            {
                break;
            }
        }

        if (lines.Count > 0)
        {
            builder.AppendLine($"D3D Slow Frames: {string.Join(" | ", lines)}");
        }
    }

    private static string FormatDiagnosticMs(JsonElement element, string propertyName)
    {
        var value = GetDouble(element, propertyName, double.NaN);
        return double.IsFinite(value) ? $"{FormatNumber(value, "0.00")}ms" : "N/A";
    }
}
