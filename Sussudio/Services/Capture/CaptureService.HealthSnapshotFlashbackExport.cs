using Sussudio.Services.Contracts;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private FlashbackExportHealthSnapshotFields CaptureFlashbackExportHealthSnapshotFields()
    {
        lock (_flashbackExportDiagnosticsLock)
        {
            return new FlashbackExportHealthSnapshotFields(
                _flashbackExportActive,
                _flashbackExportId,
                _flashbackExportStatus,
                _flashbackExportOutputPath,
                _flashbackExportStartedUtcUnixMs,
                _flashbackExportLastProgressUtcUnixMs,
                _flashbackExportCompletedUtcUnixMs,
                _flashbackExportSegmentsProcessed,
                _flashbackExportTotalSegments,
                _flashbackExportPercent,
                _flashbackExportInPointMs,
                _flashbackExportOutPointMs,
                _flashbackExportMessage,
                _flashbackExportFailureKind,
                _flashbackExportForceRotateFallbacks,
                _flashbackExportLastForceRotateFallbackUtcUnixMs,
                _flashbackExportLastForceRotateFallbackSegments,
                _flashbackExportLastForceRotateFallbackInPointMs,
                _flashbackExportLastForceRotateFallbackOutPointMs,
                _lastFlashbackExportResultId,
                _lastExportResult);
        }
    }

    private readonly record struct FlashbackExportHealthSnapshotFields(
        bool Active,
        long Id,
        string Status,
        string OutputPath,
        long StartedUtcUnixMs,
        long LastProgressUtcUnixMs,
        long CompletedUtcUnixMs,
        int SegmentsProcessed,
        int TotalSegments,
        double Percent,
        long InPointMs,
        long OutPointMs,
        string Message,
        string FailureKind,
        long ForceRotateFallbacks,
        long LastForceRotateFallbackUtcUnixMs,
        int LastForceRotateFallbackSegments,
        long LastForceRotateFallbackInPointMs,
        long LastForceRotateFallbackOutPointMs,
        long LastResultId,
        FinalizeResult? LastResult);
}
