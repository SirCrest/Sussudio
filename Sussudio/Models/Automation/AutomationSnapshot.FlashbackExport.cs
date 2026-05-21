namespace Sussudio.Models;

public sealed partial class AutomationSnapshot
{
    public bool FlashbackExportActive { get; init; }
    public long FlashbackExportId { get; init; }
    public string FlashbackExportStatus { get; init; } = "NotStarted";
    public string FlashbackExportOutputPath { get; init; } = string.Empty;
    public long FlashbackExportStartedUtcUnixMs { get; init; }
    public long FlashbackExportLastProgressUtcUnixMs { get; init; }
    public long FlashbackExportCompletedUtcUnixMs { get; init; }
    public long FlashbackExportElapsedMs { get; init; }
    public long FlashbackExportLastProgressAgeMs { get; init; }
    public long FlashbackExportOutputBytes { get; init; }
    public double FlashbackExportThroughputBytesPerSec { get; init; }
    public int FlashbackExportSegmentsProcessed { get; init; }
    public int FlashbackExportTotalSegments { get; init; }
    public double FlashbackExportPercent { get; init; }
    public long FlashbackExportInPointMs { get; init; }
    public long FlashbackExportOutPointMs { get; init; }
    public string FlashbackExportMessage { get; init; } = string.Empty;
    public string FlashbackExportFailureKind { get; init; } = string.Empty;
    public long FlashbackExportForceRotateFallbacks { get; init; }
    public long FlashbackExportLastForceRotateFallbackUtcUnixMs { get; init; }
    public int FlashbackExportLastForceRotateFallbackSegments { get; init; }
    public long FlashbackExportLastForceRotateFallbackInPointMs { get; init; }
    public long FlashbackExportLastForceRotateFallbackOutPointMs { get; init; }
    public long LastExportId { get; init; }
    public string? LastExportPath { get; init; }
    public bool? LastExportSuccess { get; init; }
    public string? LastExportMessage { get; init; }
}
