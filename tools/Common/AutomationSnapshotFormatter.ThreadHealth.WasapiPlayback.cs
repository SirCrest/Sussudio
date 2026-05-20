using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendWasapiPlaybackThreadHealthLine(StringBuilder builder, JsonElement snapshot)
    {
        var wasapiPlaybackLastRenderAgeMs = ComputeTickAgeMs(GetLong(snapshot, "WasapiPlaybackLastRenderTickMs"));
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
