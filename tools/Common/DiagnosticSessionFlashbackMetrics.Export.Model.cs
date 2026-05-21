namespace Sussudio.Tools;

internal sealed class FlashbackExportSessionMetrics
{
    public bool Observed { get; set; }
    public bool ActiveAtEnd { get; set; }
    public string StatusAtEnd { get; set; } = "NotStarted";
    public string MessageAtEnd { get; set; } = string.Empty;
    public string FailureKindAtEnd { get; set; } = string.Empty;
    public string OutputPathAtEnd { get; set; } = string.Empty;
    public long ForceRotateFallbacksAtEnd { get; set; }
    public long ForceRotateFallbacksDelta { get; set; }
    public int LastForceRotateFallbackSegmentsAtEnd { get; set; }
    public long LastExportIdAtEnd { get; set; }
    public string LastSuccessAtEnd { get; set; } = string.Empty;
    public string LastMessageAtEnd { get; set; } = string.Empty;
    public long MaxElapsedMsObserved { get; set; }
    public long MaxLastProgressAgeMsObserved { get; set; }
    public long MaxOutputBytesObserved { get; set; }
    public double MaxThroughputBytesPerSecObserved { get; set; }
}
