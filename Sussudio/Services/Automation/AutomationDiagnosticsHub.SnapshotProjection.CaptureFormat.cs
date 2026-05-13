using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static CaptureFormatProjection BuildCaptureFormatProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            RequestedWidth = captureRuntime.RequestedWidth,
            RequestedHeight = captureRuntime.RequestedHeight,
            RequestedFrameRate = captureRuntime.RequestedFrameRate,
            RequestedFrameRateArg = captureRuntime.RequestedFrameRateArg,
            RequestedFrameRateNumerator = captureRuntime.RequestedFrameRateNumerator,
            RequestedFrameRateDenominator = captureRuntime.RequestedFrameRateDenominator,
            RequestedPixelFormat = captureRuntime.RequestedPixelFormat,
            RequestedFormat = captureRuntime.RequestedFormat,
            RequestedQuality = captureRuntime.RequestedQuality,
            RequestedHdrEnabled = captureRuntime.RequestedHdrEnabled,
            RequestedHdrMasteringMetadata = captureRuntime.RequestedHdrMasteringMetadata,
            RequestedAudioEnabled = captureRuntime.RequestedAudioEnabled,
            HdrActivationReason = captureRuntime.HdrActivationReason,
            HdrAutoDowngraded = captureRuntime.HdrAutoDowngraded,
            HdrAutoDowngradeReason = captureRuntime.HdrAutoDowngradeReason,
            HdrRequestedButSourceNot10Bit = captureRuntime.HdrRequestedButSourceNot10Bit,
            ActualWidth = captureRuntime.ActualWidth,
            ActualHeight = captureRuntime.ActualHeight,
            ActualFrameRate = captureRuntime.ActualFrameRate,
            ActualFrameRateArg = captureRuntime.ActualFrameRateArg,
            NegotiatedWidth = captureRuntime.NegotiatedWidth ?? captureRuntime.ActualWidth,
            NegotiatedHeight = captureRuntime.NegotiatedHeight ?? captureRuntime.ActualHeight,
            NegotiatedFrameRate = captureRuntime.NegotiatedFrameRate ?? captureRuntime.ActualFrameRate,
            NegotiatedFrameRateArg = captureRuntime.NegotiatedFrameRateArg ?? captureRuntime.ActualFrameRateArg,
            NegotiatedFrameRateNumerator = captureRuntime.NegotiatedFrameRateNumerator,
            NegotiatedFrameRateDenominator = captureRuntime.NegotiatedFrameRateDenominator,
            NegotiatedPixelFormat = captureRuntime.NegotiatedPixelFormat,
            RequestedReaderSubtype = captureRuntime.RequestedReaderSubtype,
            ReaderSourceStreamType = captureRuntime.ReaderSourceStreamType,
            ReaderSourceSubtype = captureRuntime.ReaderSourceSubtype,
            FirstObservedFramePixelFormat = captureRuntime.FirstObservedFramePixelFormat,
            LatestObservedFramePixelFormat = captureRuntime.LatestObservedFramePixelFormat,
            LatestObservedSurfaceFormat = captureRuntime.LatestObservedSurfaceFormat,
            ObservedP010FrameCount = captureRuntime.ObservedP010FrameCount,
            ObservedNv12FrameCount = captureRuntime.ObservedNv12FrameCount,
            ObservedOtherFrameCount = captureRuntime.ObservedOtherFrameCount,
            ObservedP010BitDepthSampleCount = captureRuntime.ObservedP010BitDepthSampleCount,
            ObservedP010Low2BitNonZeroPercent = captureRuntime.ObservedP010Low2BitNonZeroPercent,
            ObservedP010Likely8BitUpscaled = captureRuntime.ObservedP010Likely8BitUpscaled,
            EncoderInputPixelFormat = captureRuntime.EncoderInputPixelFormat,
            EncoderOutputPixelFormat = captureRuntime.EncoderOutputPixelFormat,
            EncoderVideoCodec = captureRuntime.EncoderVideoCodec,
            EncoderVideoProfile = captureRuntime.EncoderVideoProfile,
            EncoderTenBitPipelineConfirmed = captureRuntime.EncoderTenBitPipelineConfirmed,
            MfReadwriteDisableConverters = captureRuntime.MfReadwriteDisableConverters,
            NegotiatedMediaSubtypeToken = captureRuntime.NegotiatedMediaSubtypeToken
        };

    private readonly record struct CaptureFormatProjection
    {
        public uint? RequestedWidth { get; init; }
        public uint? RequestedHeight { get; init; }
        public double? RequestedFrameRate { get; init; }
        public string? RequestedFrameRateArg { get; init; }
        public uint? RequestedFrameRateNumerator { get; init; }
        public uint? RequestedFrameRateDenominator { get; init; }
        public string? RequestedPixelFormat { get; init; }
        public string? RequestedFormat { get; init; }
        public string? RequestedQuality { get; init; }
        public bool? RequestedHdrEnabled { get; init; }
        public bool? RequestedHdrMasteringMetadata { get; init; }
        public bool? RequestedAudioEnabled { get; init; }
        public string HdrActivationReason { get; init; }
        public bool HdrAutoDowngraded { get; init; }
        public string HdrAutoDowngradeReason { get; init; }
        public bool HdrRequestedButSourceNot10Bit { get; init; }
        public uint? ActualWidth { get; init; }
        public uint? ActualHeight { get; init; }
        public double? ActualFrameRate { get; init; }
        public string? ActualFrameRateArg { get; init; }
        public uint? NegotiatedWidth { get; init; }
        public uint? NegotiatedHeight { get; init; }
        public double? NegotiatedFrameRate { get; init; }
        public string? NegotiatedFrameRateArg { get; init; }
        public uint? NegotiatedFrameRateNumerator { get; init; }
        public uint? NegotiatedFrameRateDenominator { get; init; }
        public string? NegotiatedPixelFormat { get; init; }
        public string? RequestedReaderSubtype { get; init; }
        public string? ReaderSourceStreamType { get; init; }
        public string? ReaderSourceSubtype { get; init; }
        public string? FirstObservedFramePixelFormat { get; init; }
        public string? LatestObservedFramePixelFormat { get; init; }
        public string? LatestObservedSurfaceFormat { get; init; }
        public long ObservedP010FrameCount { get; init; }
        public long ObservedNv12FrameCount { get; init; }
        public long ObservedOtherFrameCount { get; init; }
        public long ObservedP010BitDepthSampleCount { get; init; }
        public double ObservedP010Low2BitNonZeroPercent { get; init; }
        public bool? ObservedP010Likely8BitUpscaled { get; init; }
        public string? EncoderInputPixelFormat { get; init; }
        public string? EncoderOutputPixelFormat { get; init; }
        public string? EncoderVideoCodec { get; init; }
        public string? EncoderVideoProfile { get; init; }
        public bool? EncoderTenBitPipelineConfirmed { get; init; }
        public bool? MfReadwriteDisableConverters { get; init; }
        public string? NegotiatedMediaSubtypeToken { get; init; }
    }
}
