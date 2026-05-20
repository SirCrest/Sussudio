using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendFlashbackPlaybackStatusSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"Playback: {Get(snapshot, "FlashbackPlaybackState")} | Pos: {Get(snapshot, "FlashbackPlaybackPositionMs")}ms | Decoder: {Get(snapshot, "FlashbackDecoderHwAccel")}");
        builder.AppendLine($"Playback Commands: pending={Get(snapshot, "FlashbackPlaybackPendingCommands")}/{Get(snapshot, "FlashbackPlaybackCommandQueueCapacity")} maxPending={Get(snapshot, "FlashbackPlaybackMaxPendingCommands")} lastLatency={Get(snapshot, "FlashbackPlaybackLastCommandQueueLatencyMs")}ms maxLatency={Get(snapshot, "FlashbackPlaybackMaxCommandQueueLatencyMs")}ms maxLatencyCommand={Get(snapshot, "FlashbackPlaybackMaxCommandQueueLatencyCommand")} enq={Get(snapshot, "FlashbackPlaybackCommandsEnqueued")} proc={Get(snapshot, "FlashbackPlaybackCommandsProcessed")} drop={Get(snapshot, "FlashbackPlaybackCommandsDropped")} skip={Get(snapshot, "FlashbackPlaybackCommandsSkippedNotReady")} coalescedScrub={Get(snapshot, "FlashbackPlaybackScrubUpdatesCoalesced")} coalescedSeek={Get(snapshot, "FlashbackPlaybackSeekCommandsCoalesced")} threadAlive={Get(snapshot, "FlashbackPlaybackThreadAlive")} lastQueued={Get(snapshot, "FlashbackPlaybackLastCommandQueued")} lastProcessed={Get(snapshot, "FlashbackPlaybackLastCommandProcessed")} failure={Get(snapshot, "FlashbackPlaybackLastCommandFailure", "")} failureUtc={Get(snapshot, "FlashbackPlaybackLastCommandFailureUtcUnixMs")}");
    }
}
