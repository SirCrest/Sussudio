using System.Text;
using static Sussudio.Tools.DiagnosticSessionOptionalTextFormatter;

namespace Sussudio.Tools;

public static partial class DiagnosticSessionResultFormatter
{
    private static void AppendFlashbackSections(StringBuilder builder, DiagnosticSessionResult result)
    {
        AppendFlashbackPlaybackCommands(builder, result);
        AppendFlashbackPlaybackPerformance(builder, result);
        AppendFlashbackPlaybackDecode(builder, result);
        AppendFlashbackPlaybackStages(builder, result);
        AppendFlashbackRecording(builder, result);
        AppendFlashbackExport(builder, result);
    }

    private static void AppendFlashbackPlaybackCommands(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Flashback Playback Commands: " +
            $"pendingEnd={result.FlashbackPlaybackPendingCommandsAtEnd} " +
            $"maxPending={result.FlashbackPlaybackMaxPendingCommandsObserved} " +
            $"maxLatencyMs={result.FlashbackPlaybackMaxCommandQueueLatencyMsObserved} " +
            $"maxLatencyCommand={FormatOptional(result.FlashbackPlaybackMaxCommandQueueLatencyCommandObserved)} " +
            $"droppedEnd={result.FlashbackPlaybackCommandsDroppedAtEnd} " +
            $"skippedEnd={result.FlashbackPlaybackCommandsSkippedNotReadyAtEnd} " +
            $"coalescedScrubEnd={result.FlashbackPlaybackScrubUpdatesCoalescedAtEnd} " +
            $"coalescedSeekEnd={result.FlashbackPlaybackSeekCommandsCoalescedAtEnd} " +
            $"failureEnd={FormatOptional(result.FlashbackPlaybackLastCommandFailureAtEnd)} " +
            $"failureUtcEnd={result.FlashbackPlaybackLastCommandFailureUtcUnixMsAtEnd}");
    }

    private static void AppendFlashbackPlaybackStages(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Flashback Playback Stages: " +
            $"switchesEnd={result.FlashbackPlaybackSegmentSwitchesAtEnd} " +
            $"fmp4ReopensEnd={result.FlashbackPlaybackFmp4ReopensAtEnd} " +
            $"writeHeadWaitsEnd={result.FlashbackPlaybackWriteHeadWaitsAtEnd} " +
            $"nearLiveSnapsEnd={result.FlashbackPlaybackNearLiveSnapsAtEnd} " +
            $"decodeErrorSnapsEnd={result.FlashbackPlaybackDecodeErrorSnapsAtEnd} " +
            $"seekCapHitsEnd={result.FlashbackPlaybackSeekForwardDecodeCapHitsAtEnd} " +
            $"seekCapHitsDelta={result.FlashbackPlaybackSeekForwardDecodeCapHitsDelta} " +
            $"lastSeekCapEnd={result.FlashbackPlaybackLastSeekHitForwardDecodeCapAtEnd} " +
            $"lastWriteHeadGapMsEnd={result.FlashbackPlaybackLastWriteHeadWaitGapMsAtEnd}");
    }

}
