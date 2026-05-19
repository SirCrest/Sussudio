using System;
using Sussudio.Models;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

// Read-only source telemetry projection for runtime snapshots. Availability,
// age, circuit state, and request alignment are evidence for callers only.
public partial class CaptureService
{
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
