using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionMetrics
{
    internal static PlaybackCommandHealth BuildPlaybackCommandHealth(JsonElement snapshot, JsonElement baselineSnapshot)
    {
        var dropped = GetCounterDelta(snapshot, baselineSnapshot, "FlashbackPlaybackCommandsDropped");
        var skipped = GetCounterDelta(snapshot, baselineSnapshot, "FlashbackPlaybackCommandsSkippedNotReady");
        var submitFailures = GetCounterDelta(snapshot, baselineSnapshot, "FlashbackPlaybackSubmitFailures");
        var coalescedScrub = GetCounterDelta(snapshot, baselineSnapshot, "FlashbackPlaybackScrubUpdatesCoalesced");
        var coalescedSeek = GetCounterDelta(snapshot, baselineSnapshot, "FlashbackPlaybackSeekCommandsCoalesced");
        return new PlaybackCommandHealth(
            dropped,
            skipped,
            submitFailures,
            coalescedScrub,
            coalescedSeek,
            Math.Max(0, dropped - coalescedScrub));
    }
}
