using System;

namespace Sussudio.Models;

public partial class CaptureDiagnosticsSnapshot
{
    public SourceTelemetryAvailability SourceTelemetryAvailability { get; init; } = SourceTelemetryAvailability.Unknown;
    public SourceTelemetryOrigin SourceTelemetryOrigin { get; init; } = SourceTelemetryOrigin.Unknown;
    public SourceTelemetryConfidence SourceTelemetryConfidence { get; init; } = SourceTelemetryConfidence.Unknown;
    public string SourceTelemetryOriginDetail { get; init; } = "Unknown";
    public string? SourceTelemetryDiagnosticSummary { get; init; }
    public DateTimeOffset? SourceTelemetryTimestampUtc { get; init; }
    public string SourceTelemetryBackend { get; init; } = "Unknown";
    public bool SourceTelemetrySuppressed { get; init; }
    public string? SourceTelemetrySuppressedReason { get; init; }
    public string SourceTelemetryCircuitState { get; init; } = "Closed";
    public int? SourceWidth { get; init; }
    public int? SourceHeight { get; init; }
    public double? SourceFrameRateExact { get; init; }
    public string? SourceFrameRateArg { get; init; }
    public bool? SourceIsHdr { get; init; }
}
