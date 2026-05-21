namespace Sussudio.Models;

public sealed partial class CaptureHealthSnapshot
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
    /// <summary>
    /// The actual codec/container the next flashback export will produce. This
    /// should match the user-requested <c>SelectedRecordingFormat</c>; mismatches
    /// are reserved for future explicit, user-visible substitutions.
    /// </summary>
    public string? FlashbackExportVerificationFormat { get; init; }
    /// <summary>
    /// Legacy compatibility field for recording settings substitutions. It should
    /// remain null while Flashback honors the selected codec and preset directly.
    /// </summary>
    public string? FlashbackCodecDowngradeReason { get; init; }
    public long LastExportId { get; init; }
    public string? LastExportPath { get; init; }
    public bool? LastExportSuccess { get; init; }
    public string? LastExportMessage { get; init; }
}
