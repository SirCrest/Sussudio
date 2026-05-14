using System;
using System.Collections.Generic;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private SourceTelemetryHealthSnapshotFields CaptureSourceTelemetryHealthSnapshotFields(
        SourceSignalTelemetrySnapshot telemetry)
    {
        var suppressedReason = ResolveSourceTelemetrySuppressedReason(telemetry) ?? string.Empty;
        var suppressed = !string.IsNullOrWhiteSpace(suppressedReason);

        return new SourceTelemetryHealthSnapshotFields(
            telemetry.Availability,
            telemetry.Origin,
            telemetry.Confidence,
            telemetry.OriginDetail,
            telemetry.DiagnosticSummary,
            telemetry.TimestampUtc,
            telemetry.Width,
            telemetry.Height,
            telemetry.FrameRateExact,
            telemetry.FrameRateArg,
            telemetry.IsHdr,
            telemetry.VideoFormat,
            telemetry.Colorimetry,
            telemetry.Quantization,
            telemetry.HdrTransferFunction,
            telemetry.HdrTransferCode,
            telemetry.Firmware,
            telemetry.AudioFormat,
            telemetry.AudioSampleRate,
            telemetry.InputSource,
            telemetry.UsbHostProtocol,
            telemetry.HdcpMode,
            telemetry.HdcpVersion,
            telemetry.RxTxHdcpVersion,
            telemetry.RawTimingHex,
            telemetry.DetailEntries,
            ResolveSourceTelemetryBackend(telemetry),
            suppressedReason,
            suppressed,
            ResolveSourceTelemetryCircuitState(telemetry.Availability, suppressed));
    }

    private readonly record struct SourceTelemetryHealthSnapshotFields(
        SourceTelemetryAvailability Availability,
        SourceTelemetryOrigin Origin,
        SourceTelemetryConfidence Confidence,
        string OriginDetail,
        string? DiagnosticSummary,
        DateTimeOffset TimestampUtc,
        int? Width,
        int? Height,
        double? FrameRateExact,
        string? FrameRateArg,
        bool? IsHdr,
        string? VideoFormat,
        string? Colorimetry,
        string? Quantization,
        string? HdrTransferFunction,
        int? HdrTransferCode,
        string? Firmware,
        string? AudioFormat,
        string? AudioSampleRate,
        string? InputSource,
        string? UsbHostProtocol,
        string? HdcpMode,
        string? HdcpVersion,
        string? RxTxHdcpVersion,
        string? RawTimingHex,
        IReadOnlyList<SourceTelemetryDetailEntry> Details,
        string Backend,
        string SuppressedReason,
        bool Suppressed,
        string CircuitState);
}
