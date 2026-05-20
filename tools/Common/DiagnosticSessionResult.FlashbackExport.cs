namespace Sussudio.Tools;

public sealed partial class DiagnosticSessionResult
{
    // Flashback export summary.
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
