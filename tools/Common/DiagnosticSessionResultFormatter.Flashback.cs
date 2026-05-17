using System.Text;
using static Sussudio.Tools.AutomationSnapshotFormatter;
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

    private static void AppendFlashbackRecording(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Flashback Recording: " +
            $"backendObserved={result.FlashbackRecordingBackendObserved} " +
            $"fileGrowthObserved={result.FlashbackRecordingFileGrowthObserved} " +
            $"submittedDelta={result.FlashbackRecordingVideoFramesSubmittedDelta} " +
            $"packetsDelta={result.FlashbackRecordingVideoEncoderPacketsWrittenDelta} " +
            $"seqGapsEnd={result.FlashbackRecordingIntegritySequenceGapsAtEnd} " +
            $"seqGapsDelta={result.FlashbackRecordingIntegritySequenceGapsDelta} " +
            $"queueDropsEnd={result.FlashbackRecordingIntegrityQueueDroppedFramesAtEnd} " +
            $"queueDropsDelta={result.FlashbackRecordingIntegrityQueueDroppedFramesDelta}");
    }

    private static void AppendFlashbackExport(StringBuilder builder, DiagnosticSessionResult result)
    {
        builder.AppendLine(
            "Flashback Export: " +
            $"observed={result.FlashbackExportObserved} " +
            $"activeEnd={result.FlashbackExportActiveAtEnd} " +
            $"statusEnd={FormatOptional(result.FlashbackExportStatusAtEnd)} " +
            $"failureKindEnd={FormatOptional(result.FlashbackExportFailureKindAtEnd)} " +
            $"messageEnd={FormatOptional(result.FlashbackExportMessageAtEnd)} " +
            $"forceRotateFallbacksEnd={result.FlashbackExportForceRotateFallbacksAtEnd} " +
            $"forceRotateFallbacksDelta={result.FlashbackExportForceRotateFallbacksDelta} " +
            $"lastForceRotateFallbackSegmentsEnd={result.FlashbackExportLastForceRotateFallbackSegmentsAtEnd} " +
            $"lastResultIdEnd={result.LastExportIdAtEnd} " +
            $"lastSuccessEnd={FormatOptional(result.LastExportSuccessAtEnd)} " +
            $"lastMessageEnd={FormatOptional(result.LastExportMessageAtEnd)} " +
            $"pathEnd={FormatOptional(result.FlashbackExportOutputPathAtEnd)} " +
            $"maxElapsedMs={result.FlashbackExportMaxElapsedMsObserved} " +
            $"maxProgressAgeMs={result.FlashbackExportMaxLastProgressAgeMsObserved} " +
            $"maxBytes={FormatBytes(result.FlashbackExportMaxOutputBytesObserved)} " +
            $"maxThroughput={FormatBytes((long)result.FlashbackExportMaxThroughputBytesPerSecObserved)}/s");
    }
}
