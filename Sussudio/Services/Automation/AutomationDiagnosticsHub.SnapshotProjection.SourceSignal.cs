using System;
using Sussudio.Models;

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
}
