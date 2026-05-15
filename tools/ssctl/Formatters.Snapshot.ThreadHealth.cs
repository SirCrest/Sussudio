using System;
using System.Text;
using System.Text.Json;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class Formatters
{
    private static void AppendSnapshotThreadHealthSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine();
        builder.AppendLine("== Thread Health ==");
        var sourceReaderLastFrameAgeMs = AutomationSnapshotFormatter.ComputeTickAgeMs(AutomationSnapshotFormatter.GetLong(snapshot, "SourceReaderLastFrameTickMs"));
        var wasapiCaptureLastCallbackAgeMs = AutomationSnapshotFormatter.ComputeTickAgeMs(AutomationSnapshotFormatter.GetLong(snapshot, "WasapiCaptureLastCallbackTickMs"));
        var wasapiPlaybackLastRenderAgeMs = AutomationSnapshotFormatter.ComputeTickAgeMs(AutomationSnapshotFormatter.GetLong(snapshot, "WasapiPlaybackLastRenderTickMs"));
        var sourceReaderOutstanding = AutomationSnapshotFormatter.Get(snapshot, "SourceReaderReadOutstanding");
        var sourceReaderOutstandingSuffix = string.Equals(sourceReaderOutstanding, "true", StringComparison.OrdinalIgnoreCase)
            ? $" outstandingFor={AutomationSnapshotFormatter.Get(snapshot, "SourceReaderReadOutstandingMs")}ms"
            : string.Empty;
        builder.AppendLine(
            $"Source Reader: outstanding={sourceReaderOutstanding}{sourceReaderOutstandingSuffix} " +
            $"lastFrame={sourceReaderLastFrameAgeMs}ms ago channelDepth={AutomationSnapshotFormatter.Get(snapshot, "SourceReaderFrameChannelDepth")}");
        builder.AppendLine(
            $"WASAPI Capture: callbacks={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureCallbackCount")} " +
            $"interval={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureCallbackAvgIntervalMs")}ms/avg {AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureCallbackMaxIntervalMs")}ms/max " +
            $"silence={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureCallbackSilenceCount")} " +
            $"lastCallback={wasapiCaptureLastCallbackAgeMs}ms ago " +
            $"levelEvents={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureAudioLevelEventsFired")} " +
            $"glitches={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureAudioGlitchCount")} " +
            $"disc={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureAudioDiscontinuityCount")} " +
            $"tsErr={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureAudioTimestampErrorCount")} " +
            $"severeGaps={AutomationSnapshotFormatter.Get(snapshot, "WasapiCaptureCallbackSevereGapCount")}");
        builder.AppendLine(
            $"WASAPI Playback: callbacks={AutomationSnapshotFormatter.Get(snapshot, "WasapiPlaybackRenderCallbackCount")} " +
            $"silence={AutomationSnapshotFormatter.Get(snapshot, "WasapiPlaybackRenderSilenceCount")} " +
            $"queueDepth={AutomationSnapshotFormatter.Get(snapshot, "WasapiPlaybackQueueDepth")} " +
            $"drops={AutomationSnapshotFormatter.Get(snapshot, "WasapiPlaybackQueueDropCount")} " +
            $"lastCallback={wasapiPlaybackLastRenderAgeMs}ms ago");
    }
}
