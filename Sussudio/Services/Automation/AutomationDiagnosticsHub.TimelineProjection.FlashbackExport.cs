using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static PerformanceTimelineFlashbackExportProjection BuildPerformanceTimelineFlashbackExportProjection(
        AutomationSnapshot snapshot)
        => new(
            Active: snapshot.FlashbackExportActive,
            Status: snapshot.FlashbackExportStatus,
            FailureKind: snapshot.FlashbackExportFailureKind,
            ElapsedMs: snapshot.FlashbackExportElapsedMs,
            LastProgressAgeMs: snapshot.FlashbackExportLastProgressAgeMs,
            OutputBytes: snapshot.FlashbackExportOutputBytes,
            ThroughputBytesPerSec: snapshot.FlashbackExportThroughputBytesPerSec,
            SegmentsProcessed: snapshot.FlashbackExportSegmentsProcessed,
            TotalSegments: snapshot.FlashbackExportTotalSegments,
            Percent: snapshot.FlashbackExportPercent,
            InPointMs: snapshot.FlashbackExportInPointMs,
            OutPointMs: snapshot.FlashbackExportOutPointMs,
            Message: snapshot.FlashbackExportMessage,
            ForceRotateFallbacks: snapshot.FlashbackExportForceRotateFallbacks,
            LastForceRotateFallbackUtcUnixMs: snapshot.FlashbackExportLastForceRotateFallbackUtcUnixMs,
            LastForceRotateFallbackSegments: snapshot.FlashbackExportLastForceRotateFallbackSegments,
            LastForceRotateFallbackInPointMs: snapshot.FlashbackExportLastForceRotateFallbackInPointMs,
            LastForceRotateFallbackOutPointMs: snapshot.FlashbackExportLastForceRotateFallbackOutPointMs);

    private readonly record struct PerformanceTimelineFlashbackExportProjection(
        bool Active,
        string Status,
        string FailureKind,
        long ElapsedMs,
        long LastProgressAgeMs,
        long OutputBytes,
        double ThroughputBytesPerSec,
        int SegmentsProcessed,
        int TotalSegments,
        double Percent,
        long InPointMs,
        long OutPointMs,
        string Message,
        long ForceRotateFallbacks,
        long LastForceRotateFallbackUtcUnixMs,
        int LastForceRotateFallbackSegments,
        long LastForceRotateFallbackInPointMs,
        long LastForceRotateFallbackOutPointMs);
}
