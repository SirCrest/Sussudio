using System;
using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendThreadHealthSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine();
        builder.AppendLine("== Thread Health ==");
        var sourceReaderLastFrameAgeMs = ComputeTickAgeMs(GetLong(snapshot, "SourceReaderLastFrameTickMs"));
        var wasapiCaptureLastCallbackAgeMs = ComputeTickAgeMs(GetLong(snapshot, "WasapiCaptureLastCallbackTickMs"));
        var wasapiPlaybackLastRenderAgeMs = ComputeTickAgeMs(GetLong(snapshot, "WasapiPlaybackLastRenderTickMs"));
        var sourceReaderOutstanding = Get(snapshot, "SourceReaderReadOutstanding");
        var sourceReaderOutstandingSuffix = string.Equals(sourceReaderOutstanding, "true", StringComparison.OrdinalIgnoreCase)
            ? $" outstandingFor={Get(snapshot, "SourceReaderReadOutstandingMs")}ms"
            : string.Empty;
        builder.AppendLine(
            $"Source Reader: outstanding={sourceReaderOutstanding}{sourceReaderOutstandingSuffix} " +
            $"lastFrame={sourceReaderLastFrameAgeMs}ms ago channelDepth={Get(snapshot, "SourceReaderFrameChannelDepth")}");
        builder.AppendLine(
            $"WASAPI Capture: callbacks={Get(snapshot, "WasapiCaptureCallbackCount")} " +
            $"interval={Get(snapshot, "WasapiCaptureCallbackAvgIntervalMs")}ms/avg {Get(snapshot, "WasapiCaptureCallbackMaxIntervalMs")}ms/max " +
            $"silence={Get(snapshot, "WasapiCaptureCallbackSilenceCount")} " +
            $"lastCallback={wasapiCaptureLastCallbackAgeMs}ms ago " +
            $"levelEvents={Get(snapshot, "WasapiCaptureAudioLevelEventsFired")} " +
            $"glitches={Get(snapshot, "WasapiCaptureAudioGlitchCount")} " +
            $"disc={Get(snapshot, "WasapiCaptureAudioDiscontinuityCount")} " +
            $"tsErr={Get(snapshot, "WasapiCaptureAudioTimestampErrorCount")} " +
            $"severeGaps={Get(snapshot, "WasapiCaptureCallbackSevereGapCount")}");
        builder.AppendLine(
            $"WASAPI Playback: callbacks={Get(snapshot, "WasapiPlaybackRenderCallbackCount")} " +
            $"silence={Get(snapshot, "WasapiPlaybackRenderSilenceCount")} " +
            $"queueDepth={Get(snapshot, "WasapiPlaybackQueueDepth")} " +
            $"queueMs={Get(snapshot, "WasapiPlaybackQueueDurationMs")} " +
            $"activeMs={Get(snapshot, "WasapiPlaybackActiveChunkDurationMs")} " +
            $"endpointMs={Get(snapshot, "WasapiPlaybackEndpointQueuedDurationMs")} " +
            $"bufferedMs={Get(snapshot, "WasapiPlaybackBufferedDurationMs")} " +
            $"streamLatencyMs={Get(snapshot, "WasapiPlaybackStreamLatencyMs")} " +
            $"drops={Get(snapshot, "WasapiPlaybackQueueDropCount")} " +
            $"lastCallback={wasapiPlaybackLastRenderAgeMs}ms ago");
    }
}
