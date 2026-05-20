namespace Sussudio.Tools;

public sealed partial class DiagnosticSessionResult
{
    // Flashback recording/export summary.
    public bool FlashbackRecordingBackendObserved { get; init; }
    public bool FlashbackRecordingFileGrowthObserved { get; init; }
    public long FlashbackRecordingVideoFramesSubmittedDelta { get; init; }
    public long FlashbackRecordingVideoEncoderPacketsWrittenDelta { get; init; }
    public long FlashbackRecordingIntegritySequenceGapsAtEnd { get; init; }
    public long FlashbackRecordingIntegrityQueueDroppedFramesAtEnd { get; init; }
    public long FlashbackRecordingIntegritySequenceGapsDelta { get; init; }
    public long FlashbackRecordingIntegrityQueueDroppedFramesDelta { get; init; }
    public bool FlashbackExportObserved { get; init; }
    public bool FlashbackExportActiveAtEnd { get; init; }
    public string FlashbackExportStatusAtEnd { get; init; } = string.Empty;
    public string FlashbackExportMessageAtEnd { get; init; } = string.Empty;
    public string FlashbackExportFailureKindAtEnd { get; init; } = string.Empty;
    public string FlashbackExportOutputPathAtEnd { get; init; } = string.Empty;
    public long FlashbackExportForceRotateFallbacksAtEnd { get; init; }
    public long FlashbackExportForceRotateFallbacksDelta { get; init; }
    public int FlashbackExportLastForceRotateFallbackSegmentsAtEnd { get; init; }
    public long LastExportIdAtEnd { get; init; }
    public string LastExportSuccessAtEnd { get; init; } = string.Empty;
    public string LastExportMessageAtEnd { get; init; } = string.Empty;
    public long FlashbackExportMaxElapsedMsObserved { get; init; }
    public long FlashbackExportMaxLastProgressAgeMsObserved { get; init; }
    public long FlashbackExportMaxOutputBytesObserved { get; init; }
    public double FlashbackExportMaxThroughputBytesPerSecObserved { get; init; }
}
