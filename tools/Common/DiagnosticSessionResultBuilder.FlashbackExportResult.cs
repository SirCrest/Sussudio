namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    private readonly record struct DiagnosticSessionFlashbackExportResultProjection(
        bool FlashbackExportObserved,
        bool FlashbackExportActiveAtEnd,
        string FlashbackExportStatusAtEnd,
        string FlashbackExportMessageAtEnd,
        string FlashbackExportFailureKindAtEnd,
        string FlashbackExportOutputPathAtEnd,
        long FlashbackExportForceRotateFallbacksAtEnd,
        long FlashbackExportForceRotateFallbacksDelta,
        int FlashbackExportLastForceRotateFallbackSegmentsAtEnd,
        long LastExportIdAtEnd,
        string LastExportSuccessAtEnd,
        string LastExportMessageAtEnd,
        long FlashbackExportMaxElapsedMsObserved,
        long FlashbackExportMaxLastProgressAgeMsObserved,
        long FlashbackExportMaxOutputBytesObserved,
        double FlashbackExportMaxThroughputBytesPerSecObserved);

    private static DiagnosticSessionFlashbackExportResultProjection BuildFlashbackExportResultProjection(
        DiagnosticSessionResultAnalysis analysis)
    {
        var exportMetrics = analysis.ExportMetrics;

        return new DiagnosticSessionFlashbackExportResultProjection(
            FlashbackExportObserved: exportMetrics.Observed,
            FlashbackExportActiveAtEnd: exportMetrics.ActiveAtEnd,
            FlashbackExportStatusAtEnd: exportMetrics.StatusAtEnd,
            FlashbackExportMessageAtEnd: exportMetrics.MessageAtEnd,
            FlashbackExportFailureKindAtEnd: exportMetrics.FailureKindAtEnd,
            FlashbackExportOutputPathAtEnd: exportMetrics.OutputPathAtEnd,
            FlashbackExportForceRotateFallbacksAtEnd: exportMetrics.ForceRotateFallbacksAtEnd,
            FlashbackExportForceRotateFallbacksDelta: exportMetrics.ForceRotateFallbacksDelta,
            FlashbackExportLastForceRotateFallbackSegmentsAtEnd: exportMetrics.LastForceRotateFallbackSegmentsAtEnd,
            LastExportIdAtEnd: exportMetrics.LastExportIdAtEnd,
            LastExportSuccessAtEnd: exportMetrics.LastSuccessAtEnd,
            LastExportMessageAtEnd: exportMetrics.LastMessageAtEnd,
            FlashbackExportMaxElapsedMsObserved: exportMetrics.MaxElapsedMsObserved,
            FlashbackExportMaxLastProgressAgeMsObserved: exportMetrics.MaxLastProgressAgeMsObserved,
            FlashbackExportMaxOutputBytesObserved: exportMetrics.MaxOutputBytesObserved,
            FlashbackExportMaxThroughputBytesPerSecObserved: exportMetrics.MaxThroughputBytesPerSecObserved);
    }
}
