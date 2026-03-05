using System.Text;
using System.Text.Json;

namespace McpServer;

public static class ResponseFormatter
{
    public static string FormatSnapshot(JsonElement snapshotResponse)
    {
        if (snapshotResponse.ValueKind != JsonValueKind.Object)
        {
            return "Snapshot response was not a JSON object.";
        }

        if (!snapshotResponse.TryGetProperty("Snapshot", out var snapshot) ||
            snapshot.ValueKind != JsonValueKind.Object)
        {
            var message = Get(snapshotResponse, "Message", "Snapshot data not available.");
            return message;
        }

        var selectedFriendlyFrameRate = Get(snapshot, "SelectedFriendlyFrameRate", string.Empty);
        var selectedFrameRate = Get(snapshot, "SelectedFrameRate");
        var frameRateSummary = string.IsNullOrWhiteSpace(selectedFriendlyFrameRate)
            ? $"{selectedFrameRate} fps"
            : $"{selectedFriendlyFrameRate} fps";

        var builder = new StringBuilder();
        builder.AppendLine("== ElgatoCapture State ==");
        builder.AppendLine($"Status: {Get(snapshot, "SessionState")} | {Get(snapshot, "StatusText")}");
        builder.AppendLine($"Device: {Get(snapshot, "SelectedDeviceName")} ({Get(snapshot, "SelectedDeviceId")})");
        builder.AppendLine($"Initialized: {Get(snapshot, "IsInitialized")} | Previewing: {Get(snapshot, "IsPreviewing")} | Recording: {Get(snapshot, "IsRecording")}");
        builder.AppendLine();
        builder.AppendLine("== Capture Settings ==");
        builder.AppendLine($"Resolution: {Get(snapshot, "SelectedResolution")} | Frame Rate: {frameRateSummary}");
        builder.AppendLine($"Format: {Get(snapshot, "SelectedRecordingFormat")} | Quality: {Get(snapshot, "SelectedQuality")}");
        builder.AppendLine($"HDR: {Get(snapshot, "IsHdrEnabled")} (Available: {Get(snapshot, "IsHdrAvailable")}, Active: {Get(snapshot, "HdrOutputActive")}, State: {Get(snapshot, "HdrRuntimeState")})");
        builder.AppendLine($"Pipeline: Requested={Get(snapshot, "RequestedPipelineMode")} Active={Get(snapshot, "ActivePipelineMode")} Matched={Get(snapshot, "PipelineModeMatched")}");
        builder.AppendLine();
        builder.AppendLine("== Audio ==");
        builder.AppendLine($"Enabled: {Get(snapshot, "IsAudioEnabled")} | Preview: {Get(snapshot, "IsAudioPreviewEnabled")} | Custom Input: {Get(snapshot, "IsCustomAudioInputEnabled")}");
        builder.AppendLine($"Peak: {Get(snapshot, "AudioPeak")} | Clipping: {Get(snapshot, "AudioClipping")} | Signal: {Get(snapshot, "AudioSignalPresent")}");
        builder.AppendLine($"Reader: {Get(snapshot, "AudioReaderActive")} | Frames: {Get(snapshot, "AudioFramesArrived")} arrived, {Get(snapshot, "AudioFramesWrittenToSink")} to sink");
        builder.AppendLine();
        builder.AppendLine("== Video Pipeline ==");
        builder.AppendLine($"Reader: {Get(snapshot, "VideoReaderActive")} | Ingest: {Get(snapshot, "IngestVideoFramesArrived")} arrived, {Get(snapshot, "IngestVideoFramesWrittenToSink")} to sink");
        builder.AppendLine($"Encoder: {Get(snapshot, "EncoderVideoFramesEnqueued")} enqueued, {Get(snapshot, "EncoderVideoFramesEncoded")} encoded | Queue: {Get(snapshot, "FfmpegVideoQueueDepth")} depth, {Get(snapshot, "VideoDropsQueueSaturated")} drops");
        builder.AppendLine($"Freshness: reader {Get(snapshot, "IngestLastVideoFrameAgeMs")}ms | enqueue {Get(snapshot, "EncoderLastEnqueueAgeMs")}ms | write {Get(snapshot, "EncoderLastWriteAgeMs")}ms");
        builder.AppendLine($"Diagnostics: MemPref={Get(snapshot, "MemoryPreference")} ReqSubtype={Get(snapshot, "VideoRequestedSubtype")} NegSubtype={Get(snapshot, "VideoNegotiatedSubtype")} Errors={Get(snapshot, "VideoIngestErrorCount")}");
        builder.AppendLine();
        builder.AppendLine("== Recording ==");
        builder.AppendLine($"Recording: {Get(snapshot, "IsRecording")} | Output: {Get(snapshot, "OutputPath")}");
        builder.AppendLine($"Time: {Get(snapshot, "RecordingTime")} | Size: {Get(snapshot, "RecordingSizeInfo")} | Bitrate: {Get(snapshot, "RecordingBitrateInfo")}");
        builder.AppendLine($"Backend: {Get(snapshot, "RecordingBackend")} | Audio Path: {Get(snapshot, "AudioPathMode")} | Mux: {Get(snapshot, "MuxResult")}");
        builder.AppendLine($"Last Output: {Get(snapshot, "LastOutputPath")} ({Get(snapshot, "LastOutputSizeBytes")} bytes) Finalize: {Get(snapshot, "LastFinalizeStatus")}");
        builder.AppendLine();
        builder.AppendLine("== Performance ==");
        builder.AppendLine($"Score: {Get(snapshot, "PerformanceScore")} | Perfection: {Get(snapshot, "PerformancePerfectionMet")}");
        builder.AppendLine($"Summary: {Get(snapshot, "PerformanceSummary")}");
        builder.AppendLine();
        builder.AppendLine("== Preview ==");
        var rendererMode = Get(snapshot, "PreviewRendererMode");
        builder.AppendLine($"Renderer: {rendererMode} | Startup: {Get(snapshot, "PreviewStartupState")} | First Visual: {Get(snapshot, "PreviewFirstVisualConfirmed")}");
        if (rendererMode == "GpuMediaSource")
        {
            builder.AppendLine($"GPU Playback: {Get(snapshot, "PreviewGpuPlaybackState")} | Video: {Get(snapshot, "PreviewGpuNaturalVideoWidth")}x{Get(snapshot, "PreviewGpuNaturalVideoHeight")} | Position: {Get(snapshot, "PreviewGpuPositionMs")}ms | Events: {Get(snapshot, "PreviewGpuPositionEventCount")}");
        }
        else if (rendererMode == "D3D11VideoProcessor")
        {
            builder.AppendLine($"D3D Frames: {Get(snapshot, "PreviewD3DFramesSubmitted")} submitted, {Get(snapshot, "PreviewD3DFramesRendered")} rendered, {Get(snapshot, "PreviewD3DFramesDropped")} dropped");
            builder.AppendLine($"Color: input={Get(snapshot, "PreviewD3DInputColorSpace")} output={Get(snapshot, "PreviewD3DOutputColorSpace")}");
            builder.AppendLine($"Cadence: {Get(snapshot, "PreviewCadenceObservedFps")} fps");
        }
        else
        {
            builder.AppendLine($"Frames: {Get(snapshot, "PreviewFramesArrived")} arrived, {Get(snapshot, "PreviewFramesDisplayed")} displayed, {Get(snapshot, "PreviewFramesDropped")} dropped");
            builder.AppendLine($"Cadence: {Get(snapshot, "PreviewCadenceObservedFps")} fps");
        }
        builder.AppendLine();
        builder.AppendLine("== Source ==");
        builder.AppendLine($"Source: {Get(snapshot, "SourceWidth")} x {Get(snapshot, "SourceHeight")} HDR={Get(snapshot, "SourceIsHdr")}");
        builder.AppendLine($"Telemetry: {Get(snapshot, "SourceTelemetryAvailability")} ({Get(snapshot, "SourceTelemetryConfidence")})");

        return builder.ToString().TrimEnd();
    }

    public static string Get(JsonElement el, string prop, string fallback = "N/A")
    {
        if (el.ValueKind != JsonValueKind.Object || !el.TryGetProperty(prop, out var value))
        {
            return fallback;
        }

        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return fallback;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? fallback,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.Array => value.GetArrayLength() == 0 ? fallback : value.ToString(),
            JsonValueKind.Object => value.ToString(),
            _ => fallback
        };
    }
}
