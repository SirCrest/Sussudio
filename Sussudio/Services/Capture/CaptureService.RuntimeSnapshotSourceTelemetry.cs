using System;
using System.Collections.Generic;
using Sussudio.Models;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

// Read-only source telemetry projection for runtime snapshots. Availability,
// age, circuit state, and request alignment are evidence for callers only.
public partial class CaptureService
{
    private sealed class RuntimeSourceTelemetrySnapshotFields
    {
        public double? DetectedSourceFrameRate { get; init; }
        public string? DetectedSourceFrameRateArg { get; init; }
        public string SourceFrameRateOrigin { get; init; } = "Unknown";
        public int? SourceWidth { get; init; }
        public int? SourceHeight { get; init; }
        public bool? SourceIsHdr { get; init; }
        public string? SourceVideoFormat { get; init; }
        public string? SourceColorimetry { get; init; }
        public string? SourceQuantization { get; init; }
        public string? SourceHdrTransferFunction { get; init; }
        public int? SourceHdrTransferCode { get; init; }
        public string? SourceFirmware { get; init; }
        public string? SourceAudioFormat { get; init; }
        public string? SourceAudioSampleRate { get; init; }
        public string? SourceInputSource { get; init; }
        public string? SourceUsbHostProtocol { get; init; }
        public string? SourceHdcpMode { get; init; }
        public string? SourceHdcpVersion { get; init; }
        public string? SourceRxTxHdcpVersion { get; init; }
        public string? SourceRawTimingHex { get; init; }
        public string Availability { get; init; } = "Unknown";
        public string OriginDetail { get; init; } = "Unknown";
        public string Confidence { get; init; } = "Unknown";
        public string? DiagnosticSummary { get; init; }
        public IReadOnlyList<SourceTelemetryDetailEntry> Details { get; init; } = Array.Empty<SourceTelemetryDetailEntry>();
        public DateTimeOffset? TimestampUtc { get; init; }
        public int? AgeSeconds { get; init; }
        public string Backend { get; init; } = "Unknown";
        public bool Suppressed { get; init; }
        public string? SuppressedReason { get; init; }
        public string CircuitState { get; init; } = "Closed";
        public string AlignmentStatus { get; init; } = "Unknown";
        public string AlignmentReason { get; init; } = string.Empty;
    }

    private static RuntimeSourceTelemetrySnapshotFields CaptureRuntimeSourceTelemetrySnapshotFields(
        CaptureSettings? requestedSettings,
        SourceSignalTelemetrySnapshot telemetry,
        uint? actualWidth,
        uint? actualHeight,
        double? actualFrameRate,
        bool hdrRequested)
    {
        var telemetryTimestampUtc = telemetry.TimestampUtc;
        var telemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(telemetryTimestampUtc, DateTimeOffset.UtcNow);
        var suppressedReason = ResolveSourceTelemetrySuppressedReason(telemetry);
        var suppressed = !string.IsNullOrWhiteSpace(suppressedReason);
        var (alignmentStatus, alignmentReason) = ResolveTelemetryAlignment(
            requestedSettings,
            telemetry,
            actualWidth,
            actualHeight,
            actualFrameRate,
            hdrRequested);

        return new RuntimeSourceTelemetrySnapshotFields
        {
            DetectedSourceFrameRate = telemetry.FrameRateExact,
            DetectedSourceFrameRateArg = telemetry.FrameRateArg,
            SourceFrameRateOrigin = ResolveSourceFrameRateOrigin(telemetry),
            SourceWidth = telemetry.Width,
            SourceHeight = telemetry.Height,
            SourceIsHdr = telemetry.IsHdr,
            SourceVideoFormat = telemetry.VideoFormat,
            SourceColorimetry = telemetry.Colorimetry,
            SourceQuantization = telemetry.Quantization,
            SourceHdrTransferFunction = telemetry.HdrTransferFunction,
            SourceHdrTransferCode = telemetry.HdrTransferCode,
            SourceFirmware = telemetry.Firmware,
            SourceAudioFormat = telemetry.AudioFormat,
            SourceAudioSampleRate = telemetry.AudioSampleRate,
            SourceInputSource = telemetry.InputSource,
            SourceUsbHostProtocol = telemetry.UsbHostProtocol,
            SourceHdcpMode = telemetry.HdcpMode,
            SourceHdcpVersion = telemetry.HdcpVersion,
            SourceRxTxHdcpVersion = telemetry.RxTxHdcpVersion,
            SourceRawTimingHex = telemetry.RawTimingHex,
            Availability = telemetry.Availability.ToString(),
            OriginDetail = telemetry.OriginDetail,
            Confidence = telemetry.Confidence.ToString(),
            DiagnosticSummary = telemetry.DiagnosticSummary,
            Details = telemetry.DetailEntries,
            TimestampUtc = telemetryTimestampUtc,
            AgeSeconds = telemetryAgeSeconds,
            Backend = ResolveSourceTelemetryBackend(telemetry),
            Suppressed = suppressed,
            SuppressedReason = suppressedReason,
            CircuitState = ResolveSourceTelemetryCircuitState(telemetry.Availability, suppressed),
            AlignmentStatus = alignmentStatus,
            AlignmentReason = alignmentReason
        };
    }
}
