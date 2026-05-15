using System.Text;

namespace Sussudio.Tools;

public static partial class DiagnosticSessionResultFormatter
{
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
}
