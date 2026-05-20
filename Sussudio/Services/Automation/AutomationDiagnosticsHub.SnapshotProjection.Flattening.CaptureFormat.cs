namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static CaptureFormatFlattenedProjection BuildCaptureFormatFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            RequestedWidth = captureFormat.RequestedWidth,
            RequestedHeight = captureFormat.RequestedHeight,
            RequestedFrameRate = captureFormat.RequestedFrameRate,
            RequestedFrameRateArg = captureFormat.RequestedFrameRateArg,
            RequestedFrameRateNumerator = captureFormat.RequestedFrameRateNumerator,
            RequestedFrameRateDenominator = captureFormat.RequestedFrameRateDenominator,
            RequestedPixelFormat = captureFormat.RequestedPixelFormat,
            RequestedFormat = captureFormat.RequestedFormat,
            RequestedQuality = captureFormat.RequestedQuality,
            RequestedHdrEnabled = captureFormat.RequestedHdrEnabled,
            RequestedHdrMasteringMetadata = captureFormat.RequestedHdrMasteringMetadata,
            RequestedAudioEnabled = captureFormat.RequestedAudioEnabled,
            HdrActivationReason = captureFormat.HdrActivationReason,
            HdrAutoDowngraded = captureFormat.HdrAutoDowngraded,
            HdrAutoDowngradeReason = captureFormat.HdrAutoDowngradeReason,
            HdrRequestedButSourceNot10Bit = captureFormat.HdrRequestedButSourceNot10Bit,
            ActualWidth = captureFormat.ActualWidth,
            ActualHeight = captureFormat.ActualHeight,
            ActualFrameRate = captureFormat.ActualFrameRate,
            ActualFrameRateArg = captureFormat.ActualFrameRateArg,
            NegotiatedWidth = captureFormat.NegotiatedWidth,
            NegotiatedHeight = captureFormat.NegotiatedHeight,
            NegotiatedFrameRate = captureFormat.NegotiatedFrameRate,
            NegotiatedFrameRateArg = captureFormat.NegotiatedFrameRateArg,
            NegotiatedFrameRateNumerator = captureFormat.NegotiatedFrameRateNumerator,
            NegotiatedFrameRateDenominator = captureFormat.NegotiatedFrameRateDenominator,
            NegotiatedPixelFormat = captureFormat.NegotiatedPixelFormat,
            RequestedReaderSubtype = captureFormat.RequestedReaderSubtype,
            ReaderSourceStreamType = captureFormat.ReaderSourceStreamType,
            ReaderSourceSubtype = captureFormat.ReaderSourceSubtype,
            FirstObservedFramePixelFormat = captureFormat.FirstObservedFramePixelFormat,
            LatestObservedFramePixelFormat = captureFormat.LatestObservedFramePixelFormat,
            LatestObservedSurfaceFormat = captureFormat.LatestObservedSurfaceFormat,
            ObservedP010FrameCount = captureFormat.ObservedP010FrameCount,
            ObservedNv12FrameCount = captureFormat.ObservedNv12FrameCount,
            ObservedOtherFrameCount = captureFormat.ObservedOtherFrameCount,
            ObservedP010BitDepthSampleCount = captureFormat.ObservedP010BitDepthSampleCount,
            ObservedP010Low2BitNonZeroPercent = captureFormat.ObservedP010Low2BitNonZeroPercent,
            ObservedP010Likely8BitUpscaled = captureFormat.ObservedP010Likely8BitUpscaled,
            EncoderInputPixelFormat = captureFormat.EncoderInputPixelFormat,
            EncoderOutputPixelFormat = captureFormat.EncoderOutputPixelFormat,
            EncoderVideoCodec = captureFormat.EncoderVideoCodec,
            EncoderVideoProfile = captureFormat.EncoderVideoProfile,
            EncoderTenBitPipelineConfirmed = captureFormat.EncoderTenBitPipelineConfirmed,
            MfReadwriteDisableConverters = captureFormat.MfReadwriteDisableConverters,
            NegotiatedMediaSubtypeToken = captureFormat.NegotiatedMediaSubtypeToken
        };

    private readonly record struct CaptureFormatFlattenedProjection
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
