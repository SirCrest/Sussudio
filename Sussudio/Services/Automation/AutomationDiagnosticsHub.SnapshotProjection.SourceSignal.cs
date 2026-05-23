using System;
using System.Collections.Generic;
using Sussudio.Models;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static SourceSignalProjection BuildSourceSignalProjection(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            DetectedFrameRate = viewModelSnapshot.DetectedSourceFrameRate ?? captureRuntime.DetectedSourceFrameRate,
            DetectedFrameRateArg = viewModelSnapshot.DetectedSourceFrameRateArg ?? captureRuntime.DetectedSourceFrameRateArg,
            FrameRateOrigin = ResolveSourceFrameRateOrigin(viewModelSnapshot.SourceFrameRateOrigin, captureRuntime.SourceFrameRateOrigin),
            Width = viewModelSnapshot.SourceWidth ?? captureRuntime.SourceWidth,
            Height = viewModelSnapshot.SourceHeight ?? captureRuntime.SourceHeight,
            IsHdr = viewModelSnapshot.SourceIsHdr ?? captureRuntime.SourceIsHdr,
            VideoFormat = captureRuntime.SourceVideoFormat,
            Colorimetry = captureRuntime.SourceColorimetry,
            Quantization = captureRuntime.SourceQuantization,
            HdrTransferFunction = captureRuntime.SourceHdrTransferFunction,
            HdrTransferCode = captureRuntime.SourceHdrTransferCode,
            Firmware = captureRuntime.SourceFirmware,
            AudioFormat = captureRuntime.SourceAudioFormat,
            AudioSampleRate = captureRuntime.SourceAudioSampleRate,
            InputSource = captureRuntime.SourceInputSource,
            UsbHostProtocol = captureRuntime.SourceUsbHostProtocol,
            HdcpMode = captureRuntime.SourceHdcpMode,
            HdcpVersion = captureRuntime.SourceHdcpVersion,
            RxTxHdcpVersion = captureRuntime.SourceRxTxHdcpVersion,
            RawTimingHex = captureRuntime.SourceRawTimingHex
        };

    private static string ResolveSourceFrameRateOrigin(string viewModelOrigin, string runtimeOrigin)
        => !string.IsNullOrWhiteSpace(viewModelOrigin) &&
           !string.Equals(viewModelOrigin, "Unknown", StringComparison.OrdinalIgnoreCase)
            ? viewModelOrigin
            : runtimeOrigin;

    private readonly record struct SourceSignalProjection
    {
        public double? DetectedFrameRate { get; init; }
        public string? DetectedFrameRateArg { get; init; }
        public string FrameRateOrigin { get; init; }
        public int? Width { get; init; }
        public int? Height { get; init; }
        public bool? IsHdr { get; init; }
        public string? VideoFormat { get; init; }
        public string? Colorimetry { get; init; }
        public string? Quantization { get; init; }
        public string? HdrTransferFunction { get; init; }
        public int? HdrTransferCode { get; init; }
        public string? Firmware { get; init; }
        public string? AudioFormat { get; init; }
        public string? AudioSampleRate { get; init; }
        public string? InputSource { get; init; }
        public string? UsbHostProtocol { get; init; }
        public string? HdcpMode { get; init; }
        public string? HdcpVersion { get; init; }
        public string? RxTxHdcpVersion { get; init; }
        public string? RawTimingHex { get; init; }
    }

    private static SourceTelemetryProjection BuildSourceTelemetryProjection(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        CaptureRuntimeSnapshot captureRuntime)
    {
        var telemetryTimestampUtc = viewModelSnapshot.SourceTelemetryTimestampUtc ?? captureRuntime.SourceTelemetryTimestampUtc;

        return new()
        {
            SourceTelemetryAvailability = PreferKnownTelemetryValue(
                viewModelSnapshot.SourceTelemetryAvailability,
                captureRuntime.SourceTelemetryAvailability),
            SourceTelemetryOriginDetail = PreferKnownTelemetryValue(
                viewModelSnapshot.SourceTelemetryOriginDetail,
                captureRuntime.SourceTelemetryOriginDetail),
            SourceTelemetryConfidence = PreferKnownTelemetryValue(
                viewModelSnapshot.SourceTelemetryConfidence,
                captureRuntime.SourceTelemetryConfidence),
            SourceTelemetryDiagnosticSummary = viewModelSnapshot.SourceTelemetryDiagnosticSummary ?? captureRuntime.SourceTelemetryDiagnosticSummary,
            SourceTelemetryDetails = captureRuntime.SourceTelemetryDetails,
            SourceTelemetryTimestampUtc = telemetryTimestampUtc,
            SourceTelemetryAgeSeconds = TelemetryAgeHelper.ComputeAgeSeconds(
                viewModelSnapshot.SourceTelemetryAgeSeconds,
                telemetryTimestampUtc,
                DateTimeOffset.UtcNow),
            SourceTelemetryBackend = captureRuntime.SourceTelemetryBackend,
            SourceTelemetrySuppressed = captureRuntime.SourceTelemetrySuppressed,
            SourceTelemetrySuppressedReason = captureRuntime.SourceTelemetrySuppressedReason,
            SourceTelemetryCircuitState = captureRuntime.SourceTelemetryCircuitState,
            SourceTelemetrySummaryText = viewModelSnapshot.SourceTelemetrySummaryText,
            SourceTargetSummaryText = viewModelSnapshot.SourceTargetSummaryText
        };
    }

    private static string PreferKnownTelemetryValue(string viewModelValue, string runtimeValue)
        => !string.IsNullOrWhiteSpace(viewModelValue) &&
           !string.Equals(viewModelValue, "Unknown", StringComparison.OrdinalIgnoreCase)
            ? viewModelValue
            : runtimeValue;

    private readonly record struct SourceTelemetryProjection
    {
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

    private static SourceFlattenedProjection BuildSourceFlattenedProjection(
        SourceSignalProjection sourceSignal,
        SourceTelemetryProjection sourceTelemetry)
        => new()
        {
            Signal = BuildSourceSignalFlattenedProjection(sourceSignal),
            Telemetry = BuildSourceTelemetryFlattenedProjection(sourceTelemetry)
        };

    private static SourceSignalFlattenedProjection BuildSourceSignalFlattenedProjection(
        SourceSignalProjection sourceSignal)
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
            SourceRawTimingHex = sourceSignal.RawTimingHex
        };

    private readonly record struct SourceSignalFlattenedProjection
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
    }

    private static SourceTelemetryFlattenedProjection BuildSourceTelemetryFlattenedProjection(
        SourceTelemetryProjection sourceTelemetry)
        => new()
        {
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

    private readonly record struct SourceTelemetryFlattenedProjection
    {
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

    private readonly record struct SourceFlattenedProjection
    {
        public SourceSignalFlattenedProjection Signal { get; init; }
        public SourceTelemetryFlattenedProjection Telemetry { get; init; }
    }
}
