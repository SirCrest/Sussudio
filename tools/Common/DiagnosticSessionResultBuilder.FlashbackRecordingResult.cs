namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    private readonly record struct DiagnosticSessionFlashbackRecordingResultProjection(
        bool FlashbackRecordingBackendObserved,
        bool FlashbackRecordingFileGrowthObserved,
        long FlashbackRecordingVideoFramesSubmittedDelta,
        long FlashbackRecordingVideoEncoderPacketsWrittenDelta,
        long FlashbackRecordingIntegritySequenceGapsAtEnd,
        long FlashbackRecordingIntegrityQueueDroppedFramesAtEnd,
        long FlashbackRecordingIntegritySequenceGapsDelta,
        long FlashbackRecordingIntegrityQueueDroppedFramesDelta);

    private static DiagnosticSessionFlashbackRecordingResultProjection BuildFlashbackRecordingResultProjection(
        DiagnosticSessionResultAnalysis analysis)
    {
        var recordingMetrics = analysis.RecordingMetrics;

        return new DiagnosticSessionFlashbackRecordingResultProjection(
            FlashbackRecordingBackendObserved: recordingMetrics.BackendObserved,
            FlashbackRecordingFileGrowthObserved: recordingMetrics.FileGrowthObserved,
            FlashbackRecordingVideoFramesSubmittedDelta: recordingMetrics.VideoFramesSubmittedDelta,
            FlashbackRecordingVideoEncoderPacketsWrittenDelta: recordingMetrics.VideoEncoderPacketsWrittenDelta,
            FlashbackRecordingIntegritySequenceGapsAtEnd: recordingMetrics.IntegritySequenceGapsAtEnd,
            FlashbackRecordingIntegrityQueueDroppedFramesAtEnd: recordingMetrics.IntegrityQueueDroppedFramesAtEnd,
            FlashbackRecordingIntegritySequenceGapsDelta: recordingMetrics.IntegritySequenceGapsDelta,
            FlashbackRecordingIntegrityQueueDroppedFramesDelta: recordingMetrics.IntegrityQueueDroppedFramesDelta);
    }
}
