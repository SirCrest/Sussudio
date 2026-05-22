using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackExportProjection BuildFlashbackExportProjection(CaptureHealthSnapshot health)
        => new()
        {
            Active = health.FlashbackExportActive,
            Id = health.FlashbackExportId,
            Status = health.FlashbackExportStatus,
            OutputPath = health.FlashbackExportOutputPath,
            StartedUtcUnixMs = health.FlashbackExportStartedUtcUnixMs,
            LastProgressUtcUnixMs = health.FlashbackExportLastProgressUtcUnixMs,
            CompletedUtcUnixMs = health.FlashbackExportCompletedUtcUnixMs,
            ElapsedMs = health.FlashbackExportElapsedMs,
            LastProgressAgeMs = health.FlashbackExportLastProgressAgeMs,
            OutputBytes = health.FlashbackExportOutputBytes,
            ThroughputBytesPerSec = health.FlashbackExportThroughputBytesPerSec,
            SegmentsProcessed = health.FlashbackExportSegmentsProcessed,
            TotalSegments = health.FlashbackExportTotalSegments,
            Percent = health.FlashbackExportPercent,
            InPointMs = health.FlashbackExportInPointMs,
            OutPointMs = health.FlashbackExportOutPointMs,
            Message = health.FlashbackExportMessage,
            FailureKind = health.FlashbackExportFailureKind,
            ForceRotateFallbacks = health.FlashbackExportForceRotateFallbacks,
            LastForceRotateFallbackUtcUnixMs = health.FlashbackExportLastForceRotateFallbackUtcUnixMs,
            LastForceRotateFallbackSegments = health.FlashbackExportLastForceRotateFallbackSegments,
            LastForceRotateFallbackInPointMs = health.FlashbackExportLastForceRotateFallbackInPointMs,
            LastForceRotateFallbackOutPointMs = health.FlashbackExportLastForceRotateFallbackOutPointMs
        };

    private static FlashbackExportLastResultProjection BuildFlashbackExportLastResultProjection(CaptureHealthSnapshot health)
        => new()
        {
            LastExportId = health.LastExportId,
            LastExportPath = health.LastExportPath,
            LastExportSuccess = health.LastExportSuccess,
            LastExportMessage = health.LastExportMessage
        };

    private readonly record struct FlashbackExportProjection
    {
        public bool Active { get; init; }
        public long Id { get; init; }
        public string Status { get; init; }
        public string OutputPath { get; init; }
        public long StartedUtcUnixMs { get; init; }
        public long LastProgressUtcUnixMs { get; init; }
        public long CompletedUtcUnixMs { get; init; }
        public long ElapsedMs { get; init; }
        public long LastProgressAgeMs { get; init; }
        public long OutputBytes { get; init; }
        public double ThroughputBytesPerSec { get; init; }
        public int SegmentsProcessed { get; init; }
        public int TotalSegments { get; init; }
        public double Percent { get; init; }
        public long InPointMs { get; init; }
        public long OutPointMs { get; init; }
        public string Message { get; init; }
        public string FailureKind { get; init; }
        public long ForceRotateFallbacks { get; init; }
        public long LastForceRotateFallbackUtcUnixMs { get; init; }
        public int LastForceRotateFallbackSegments { get; init; }
        public long LastForceRotateFallbackInPointMs { get; init; }
        public long LastForceRotateFallbackOutPointMs { get; init; }
    }

    private readonly record struct FlashbackExportLastResultProjection
    {
        public long LastExportId { get; init; }
        public string? LastExportPath { get; init; }
        public bool? LastExportSuccess { get; init; }
        public string? LastExportMessage { get; init; }
    }

    private static FlashbackExportFlattenedProjection BuildFlashbackExportFlattenedProjection(
        FlashbackExportProjection flashbackExport,
        FlashbackExportLastResultProjection lastResult)
        => new()
        {
            Active = flashbackExport.Active,
            Id = flashbackExport.Id,
            Status = flashbackExport.Status,
            OutputPath = flashbackExport.OutputPath,
            StartedUtcUnixMs = flashbackExport.StartedUtcUnixMs,
            LastProgressUtcUnixMs = flashbackExport.LastProgressUtcUnixMs,
            CompletedUtcUnixMs = flashbackExport.CompletedUtcUnixMs,
            ElapsedMs = flashbackExport.ElapsedMs,
            LastProgressAgeMs = flashbackExport.LastProgressAgeMs,
            OutputBytes = flashbackExport.OutputBytes,
            ThroughputBytesPerSec = flashbackExport.ThroughputBytesPerSec,
            SegmentsProcessed = flashbackExport.SegmentsProcessed,
            TotalSegments = flashbackExport.TotalSegments,
            Percent = flashbackExport.Percent,
            InPointMs = flashbackExport.InPointMs,
            OutPointMs = flashbackExport.OutPointMs,
            Message = flashbackExport.Message,
            FailureKind = flashbackExport.FailureKind,
            ForceRotateFallbacks = flashbackExport.ForceRotateFallbacks,
            LastForceRotateFallbackUtcUnixMs = flashbackExport.LastForceRotateFallbackUtcUnixMs,
            LastForceRotateFallbackSegments = flashbackExport.LastForceRotateFallbackSegments,
            LastForceRotateFallbackInPointMs = flashbackExport.LastForceRotateFallbackInPointMs,
            LastForceRotateFallbackOutPointMs = flashbackExport.LastForceRotateFallbackOutPointMs,
            LastExportId = lastResult.LastExportId,
            LastExportPath = lastResult.LastExportPath,
            LastExportSuccess = lastResult.LastExportSuccess,
            LastExportMessage = lastResult.LastExportMessage
        };

    private readonly record struct FlashbackExportFlattenedProjection
    {
        public bool Active { get; init; }
        public long Id { get; init; }
        public string Status { get; init; }
        public string OutputPath { get; init; }
        public long StartedUtcUnixMs { get; init; }
        public long LastProgressUtcUnixMs { get; init; }
        public long CompletedUtcUnixMs { get; init; }
        public long ElapsedMs { get; init; }
        public long LastProgressAgeMs { get; init; }
        public long OutputBytes { get; init; }
        public double ThroughputBytesPerSec { get; init; }
        public int SegmentsProcessed { get; init; }
        public int TotalSegments { get; init; }
        public double Percent { get; init; }
        public long InPointMs { get; init; }
        public long OutPointMs { get; init; }
        public string Message { get; init; }
        public string FailureKind { get; init; }
        public long ForceRotateFallbacks { get; init; }
        public long LastForceRotateFallbackUtcUnixMs { get; init; }
        public int LastForceRotateFallbackSegments { get; init; }
        public long LastForceRotateFallbackInPointMs { get; init; }
        public long LastForceRotateFallbackOutPointMs { get; init; }
        public long LastExportId { get; init; }
        public string? LastExportPath { get; init; }
        public bool? LastExportSuccess { get; init; }
        public string? LastExportMessage { get; init; }
    }
}
