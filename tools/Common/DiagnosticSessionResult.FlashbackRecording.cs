namespace Sussudio.Tools;

public sealed partial class DiagnosticSessionResult
{
    // Flashback recording summary.
    public bool FlashbackRecordingBackendObserved { get; init; }
    public bool FlashbackRecordingFileGrowthObserved { get; init; }
    public long FlashbackRecordingVideoFramesSubmittedDelta { get; init; }
    public long FlashbackRecordingVideoEncoderPacketsWrittenDelta { get; init; }
    public long FlashbackRecordingIntegritySequenceGapsAtEnd { get; init; }
    public long FlashbackRecordingIntegrityQueueDroppedFramesAtEnd { get; init; }
    public long FlashbackRecordingIntegritySequenceGapsDelta { get; init; }
    public long FlashbackRecordingIntegrityQueueDroppedFramesDelta { get; init; }
}
