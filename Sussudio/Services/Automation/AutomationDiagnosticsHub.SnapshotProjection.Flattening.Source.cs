using System;
using System.Collections.Generic;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static SourceFlattenedProjection BuildSourceFlattenedProjection(
        SourceSignalProjection sourceSignal,
        SourceTelemetryProjection sourceTelemetry)
        => new()
        {
            DetectedSourceFrameRate = sourceSignal.DetectedFrameRate,
            DetectedSourceFrameRateArg = sourceSignal.DetectedFrameRateArg,
            SourceFrameRateOrigin = sourceSignal.FrameRateOrigin,
            SourceWidth = sourceSignal.Width,
            SourceHeight = sourceSignal.Height,
            SourceIsHdr = sourceSignal.IsHdr,
            SourceVideoFormat = sourceSignal.VideoFormat,
            SourceColorimetry = sourceSignal.Colorimetry,
            SourceQuantization = sourceSignal.Quantization,
            SourceHdrTransferFunction = sourceSignal.HdrTransferFunction,
            SourceHdrTransferCode = sourceSignal.HdrTransferCode,
            SourceFirmware = sourceSignal.Firmware,
            SourceAudioFormat = sourceSignal.AudioFormat,
            SourceAudioSampleRate = sourceSignal.AudioSampleRate,
            SourceInputSource = sourceSignal.InputSource,
            SourceUsbHostProtocol = sourceSignal.UsbHostProtocol,
            SourceHdcpMode = sourceSignal.HdcpMode,
            SourceHdcpVersion = sourceSignal.HdcpVersion,
            SourceRxTxHdcpVersion = sourceSignal.RxTxHdcpVersion,
            SourceRawTimingHex = sourceSignal.RawTimingHex,
            SourceTelemetryAvailability = sourceTelemetry.SourceTelemetryAvailability,
            SourceTelemetryOriginDetail = sourceTelemetry.SourceTelemetryOriginDetail,
            SourceTelemetryConfidence = sourceTelemetry.SourceTelemetryConfidence,
            SourceTelemetryDiagnosticSummary = sourceTelemetry.SourceTelemetryDiagnosticSummary,
            SourceTelemetryDetails = sourceTelemetry.SourceTelemetryDetails,
            SourceTelemetryTimestampUtc = sourceTelemetry.SourceTelemetryTimestampUtc,
            SourceTelemetryAgeSeconds = sourceTelemetry.SourceTelemetryAgeSeconds,
            SourceTelemetryBackend = sourceTelemetry.SourceTelemetryBackend,
            SourceTelemetrySuppressed = sourceTelemetry.SourceTelemetrySuppressed,
            SourceTelemetrySuppressedReason = sourceTelemetry.SourceTelemetrySuppressedReason,
            SourceTelemetryCircuitState = sourceTelemetry.SourceTelemetryCircuitState,
            SourceTelemetrySummaryText = sourceTelemetry.SourceTelemetrySummaryText,
            SourceTargetSummaryText = sourceTelemetry.SourceTargetSummaryText
        };

    private readonly record struct SourceFlattenedProjection
    {
        public double? DetectedSourceFrameRate { get; init; }
        public string? DetectedSourceFrameRateArg { get; init; }
        public string SourceFrameRateOrigin { get; init; }
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
        public string SourceTelemetryAvailability { get; init; }
        public string SourceTelemetryOriginDetail { get; init; }
        public string SourceTelemetryConfidence { get; init; }
        public string? SourceTelemetryDiagnosticSummary { get; init; }
        public IReadOnlyList<SourceTelemetryDetailEntry> SourceTelemetryDetails { get; init; }
        public DateTimeOffset? SourceTelemetryTimestampUtc { get; init; }
        public int? SourceTelemetryAgeSeconds { get; init; }
        public string SourceTelemetryBackend { get; init; }
        public bool SourceTelemetrySuppressed { get; init; }
        public string? SourceTelemetrySuppressedReason { get; init; }
        public string SourceTelemetryCircuitState { get; init; }
        public string SourceTelemetrySummaryText { get; init; }
        public string SourceTargetSummaryText { get; init; }
    }
}
