using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static CaptureFormatProjection BuildCaptureFormatProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            Requested = BuildCaptureFormatRequestedProjection(captureRuntime),
            HdrRequest = BuildCaptureFormatHdrRequestProjection(captureRuntime),
            Actual = BuildCaptureFormatActualProjection(captureRuntime),
            Negotiated = BuildCaptureFormatNegotiatedProjection(captureRuntime),
            ReaderObservation = BuildCaptureFormatReaderObservationProjection(captureRuntime),
            Encoder = BuildCaptureFormatEncoderProjection(captureRuntime)
        };

    private static CaptureFormatFlattenedProjection BuildCaptureFormatFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            Requested = BuildCaptureFormatRequestedFlattenedProjection(captureFormat),
            HdrRequest = BuildCaptureFormatHdrRequestFlattenedProjection(captureFormat),
            Actual = BuildCaptureFormatActualFlattenedProjection(captureFormat),
            Negotiated = BuildCaptureFormatNegotiatedFlattenedProjection(captureFormat),
            ReaderObservation = BuildCaptureFormatReaderObservationFlattenedProjection(captureFormat),
            Encoder = BuildCaptureFormatEncoderFlattenedProjection(captureFormat)
        };

    private readonly record struct CaptureFormatProjection
    {
        public CaptureFormatRequestedProjection Requested { get; init; }
        public CaptureFormatHdrRequestProjection HdrRequest { get; init; }
        public CaptureFormatActualProjection Actual { get; init; }
        public CaptureFormatNegotiatedProjection Negotiated { get; init; }
        public CaptureFormatReaderObservationProjection ReaderObservation { get; init; }
        public CaptureFormatEncoderProjection Encoder { get; init; }
    }

    private static CaptureFormatRequestedProjection BuildCaptureFormatRequestedProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            Width = captureRuntime.RequestedWidth,
            Height = captureRuntime.RequestedHeight,
            FrameRate = captureRuntime.RequestedFrameRate,
            FrameRateArg = captureRuntime.RequestedFrameRateArg,
            FrameRateNumerator = captureRuntime.RequestedFrameRateNumerator,
            FrameRateDenominator = captureRuntime.RequestedFrameRateDenominator,
            PixelFormat = captureRuntime.RequestedPixelFormat,
            Format = captureRuntime.RequestedFormat,
            Quality = captureRuntime.RequestedQuality,
            HdrEnabled = captureRuntime.RequestedHdrEnabled,
            HdrMasteringMetadata = captureRuntime.RequestedHdrMasteringMetadata,
            AudioEnabled = captureRuntime.RequestedAudioEnabled
        };

    private static CaptureFormatRequestedFlattenedProjection BuildCaptureFormatRequestedFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            Width = captureFormat.Requested.Width,
            Height = captureFormat.Requested.Height,
            FrameRate = captureFormat.Requested.FrameRate,
            FrameRateArg = captureFormat.Requested.FrameRateArg,
            FrameRateNumerator = captureFormat.Requested.FrameRateNumerator,
            FrameRateDenominator = captureFormat.Requested.FrameRateDenominator,
            PixelFormat = captureFormat.Requested.PixelFormat,
            Format = captureFormat.Requested.Format,
            Quality = captureFormat.Requested.Quality,
            HdrEnabled = captureFormat.Requested.HdrEnabled,
            HdrMasteringMetadata = captureFormat.Requested.HdrMasteringMetadata,
            AudioEnabled = captureFormat.Requested.AudioEnabled
        };

    private readonly record struct CaptureFormatRequestedProjection
    {
        public uint? Width { get; init; }
        public uint? Height { get; init; }
        public double? FrameRate { get; init; }
        public string? FrameRateArg { get; init; }
        public uint? FrameRateNumerator { get; init; }
        public uint? FrameRateDenominator { get; init; }
        public string? PixelFormat { get; init; }
        public string? Format { get; init; }
        public string? Quality { get; init; }
        public bool? HdrEnabled { get; init; }
        public bool? HdrMasteringMetadata { get; init; }
        public bool? AudioEnabled { get; init; }
    }

    private readonly record struct CaptureFormatRequestedFlattenedProjection
    {
        public uint? Width { get; init; }
        public uint? Height { get; init; }
        public double? FrameRate { get; init; }
        public string? FrameRateArg { get; init; }
        public uint? FrameRateNumerator { get; init; }
        public uint? FrameRateDenominator { get; init; }
        public string? PixelFormat { get; init; }
        public string? Format { get; init; }
        public string? Quality { get; init; }
        public bool? HdrEnabled { get; init; }
        public bool? HdrMasteringMetadata { get; init; }
        public bool? AudioEnabled { get; init; }
    }

    private static CaptureFormatHdrRequestProjection BuildCaptureFormatHdrRequestProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            ActivationReason = captureRuntime.HdrActivationReason,
            AutoDowngraded = captureRuntime.HdrAutoDowngraded,
            AutoDowngradeReason = captureRuntime.HdrAutoDowngradeReason,
            RequestedButSourceNot10Bit = captureRuntime.HdrRequestedButSourceNot10Bit
        };

    private static CaptureFormatHdrRequestFlattenedProjection BuildCaptureFormatHdrRequestFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            ActivationReason = captureFormat.HdrRequest.ActivationReason,
            AutoDowngraded = captureFormat.HdrRequest.AutoDowngraded,
            AutoDowngradeReason = captureFormat.HdrRequest.AutoDowngradeReason,
            RequestedButSourceNot10Bit = captureFormat.HdrRequest.RequestedButSourceNot10Bit
        };

    private readonly record struct CaptureFormatHdrRequestProjection
    {
        public string ActivationReason { get; init; }
        public bool AutoDowngraded { get; init; }
        public string AutoDowngradeReason { get; init; }
        public bool RequestedButSourceNot10Bit { get; init; }
    }

    private readonly record struct CaptureFormatHdrRequestFlattenedProjection
    {
        public string ActivationReason { get; init; }
        public bool AutoDowngraded { get; init; }
        public string AutoDowngradeReason { get; init; }
        public bool RequestedButSourceNot10Bit { get; init; }
    }

    private static CaptureFormatActualProjection BuildCaptureFormatActualProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            Width = captureRuntime.ActualWidth,
            Height = captureRuntime.ActualHeight,
            FrameRate = captureRuntime.ActualFrameRate,
            FrameRateArg = captureRuntime.ActualFrameRateArg
        };

    private static CaptureFormatActualFlattenedProjection BuildCaptureFormatActualFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            Width = captureFormat.Actual.Width,
            Height = captureFormat.Actual.Height,
            FrameRate = captureFormat.Actual.FrameRate,
            FrameRateArg = captureFormat.Actual.FrameRateArg
        };

    private readonly record struct CaptureFormatActualProjection
    {
        public uint? Width { get; init; }
        public uint? Height { get; init; }
        public double? FrameRate { get; init; }
        public string? FrameRateArg { get; init; }
    }

    private readonly record struct CaptureFormatActualFlattenedProjection
    {
        public uint? Width { get; init; }
        public uint? Height { get; init; }
        public double? FrameRate { get; init; }
        public string? FrameRateArg { get; init; }
    }

    private static CaptureFormatNegotiatedProjection BuildCaptureFormatNegotiatedProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            Width = captureRuntime.NegotiatedWidth ?? captureRuntime.ActualWidth,
            Height = captureRuntime.NegotiatedHeight ?? captureRuntime.ActualHeight,
            FrameRate = captureRuntime.NegotiatedFrameRate ?? captureRuntime.ActualFrameRate,
            FrameRateArg = captureRuntime.NegotiatedFrameRateArg ?? captureRuntime.ActualFrameRateArg,
            FrameRateNumerator = captureRuntime.NegotiatedFrameRateNumerator,
            FrameRateDenominator = captureRuntime.NegotiatedFrameRateDenominator,
            PixelFormat = captureRuntime.NegotiatedPixelFormat,
            MediaSubtypeToken = captureRuntime.NegotiatedMediaSubtypeToken
        };

    private static CaptureFormatNegotiatedFlattenedProjection BuildCaptureFormatNegotiatedFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            Width = captureFormat.Negotiated.Width,
            Height = captureFormat.Negotiated.Height,
            FrameRate = captureFormat.Negotiated.FrameRate,
            FrameRateArg = captureFormat.Negotiated.FrameRateArg,
            FrameRateNumerator = captureFormat.Negotiated.FrameRateNumerator,
            FrameRateDenominator = captureFormat.Negotiated.FrameRateDenominator,
            PixelFormat = captureFormat.Negotiated.PixelFormat,
            MediaSubtypeToken = captureFormat.Negotiated.MediaSubtypeToken
        };

    private readonly record struct CaptureFormatNegotiatedProjection
    {
        public uint? Width { get; init; }
        public uint? Height { get; init; }
        public double? FrameRate { get; init; }
        public string? FrameRateArg { get; init; }
        public uint? FrameRateNumerator { get; init; }
        public uint? FrameRateDenominator { get; init; }
        public string? PixelFormat { get; init; }
        public string? MediaSubtypeToken { get; init; }
    }

    private readonly record struct CaptureFormatNegotiatedFlattenedProjection
    {
        public uint? Width { get; init; }
        public uint? Height { get; init; }
        public double? FrameRate { get; init; }
        public string? FrameRateArg { get; init; }
        public uint? FrameRateNumerator { get; init; }
        public uint? FrameRateDenominator { get; init; }
        public string? PixelFormat { get; init; }
        public string? MediaSubtypeToken { get; init; }
    }

    private static CaptureFormatReaderObservationProjection BuildCaptureFormatReaderObservationProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
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
            MfReadwriteDisableConverters = captureRuntime.MfReadwriteDisableConverters
        };

    private static CaptureFormatReaderObservationFlattenedProjection BuildCaptureFormatReaderObservationFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            RequestedReaderSubtype = captureFormat.ReaderObservation.RequestedReaderSubtype,
            ReaderSourceStreamType = captureFormat.ReaderObservation.ReaderSourceStreamType,
            ReaderSourceSubtype = captureFormat.ReaderObservation.ReaderSourceSubtype,
            FirstObservedFramePixelFormat = captureFormat.ReaderObservation.FirstObservedFramePixelFormat,
            LatestObservedFramePixelFormat = captureFormat.ReaderObservation.LatestObservedFramePixelFormat,
            LatestObservedSurfaceFormat = captureFormat.ReaderObservation.LatestObservedSurfaceFormat,
            ObservedP010FrameCount = captureFormat.ReaderObservation.ObservedP010FrameCount,
            ObservedNv12FrameCount = captureFormat.ReaderObservation.ObservedNv12FrameCount,
            ObservedOtherFrameCount = captureFormat.ReaderObservation.ObservedOtherFrameCount,
            ObservedP010BitDepthSampleCount = captureFormat.ReaderObservation.ObservedP010BitDepthSampleCount,
            ObservedP010Low2BitNonZeroPercent = captureFormat.ReaderObservation.ObservedP010Low2BitNonZeroPercent,
            ObservedP010Likely8BitUpscaled = captureFormat.ReaderObservation.ObservedP010Likely8BitUpscaled,
            MfReadwriteDisableConverters = captureFormat.ReaderObservation.MfReadwriteDisableConverters
        };

    private readonly record struct CaptureFormatReaderObservationProjection
    {
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
        public bool? MfReadwriteDisableConverters { get; init; }
    }

    private readonly record struct CaptureFormatReaderObservationFlattenedProjection
    {
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
        public bool? MfReadwriteDisableConverters { get; init; }
    }

    private static CaptureFormatEncoderProjection BuildCaptureFormatEncoderProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            InputPixelFormat = captureRuntime.EncoderInputPixelFormat,
            OutputPixelFormat = captureRuntime.EncoderOutputPixelFormat,
            VideoCodec = captureRuntime.EncoderVideoCodec,
            VideoProfile = captureRuntime.EncoderVideoProfile,
            TenBitPipelineConfirmed = captureRuntime.EncoderTenBitPipelineConfirmed
        };

    private static CaptureFormatEncoderFlattenedProjection BuildCaptureFormatEncoderFlattenedProjection(
        CaptureFormatProjection captureFormat)
        => new()
        {
            InputPixelFormat = captureFormat.Encoder.InputPixelFormat,
            OutputPixelFormat = captureFormat.Encoder.OutputPixelFormat,
            VideoCodec = captureFormat.Encoder.VideoCodec,
            VideoProfile = captureFormat.Encoder.VideoProfile,
            TenBitPipelineConfirmed = captureFormat.Encoder.TenBitPipelineConfirmed
        };

    private readonly record struct CaptureFormatEncoderProjection
    {
        public string? InputPixelFormat { get; init; }
        public string? OutputPixelFormat { get; init; }
        public string? VideoCodec { get; init; }
        public string? VideoProfile { get; init; }
        public bool? TenBitPipelineConfirmed { get; init; }
    }

    private readonly record struct CaptureFormatEncoderFlattenedProjection
    {
        public string? InputPixelFormat { get; init; }
        public string? OutputPixelFormat { get; init; }
        public string? VideoCodec { get; init; }
        public string? VideoProfile { get; init; }
        public bool? TenBitPipelineConfirmed { get; init; }
    }

    private static CaptureTransportProjection BuildCaptureTransportProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            MemoryPreference = captureRuntime.MemoryPreference,
            VideoRequestedSubtype = captureRuntime.VideoRequestedSubtype,
            VideoNegotiatedSubtype = captureRuntime.VideoNegotiatedSubtype,
            FrameLedgerCapacity = captureRuntime.FrameLedgerCapacity,
            FrameLedgerEventCount = captureRuntime.FrameLedgerEventCount,
            FrameLedgerDroppedEventCount = captureRuntime.FrameLedgerDroppedEventCount,
            FrameLedgerRecentEvents = captureRuntime.FrameLedgerRecentEvents
        };

    private readonly record struct CaptureTransportProjection
    {
        public string MemoryPreference { get; init; }
        public string VideoRequestedSubtype { get; init; }
        public string VideoNegotiatedSubtype { get; init; }
        public int FrameLedgerCapacity { get; init; }
        public long FrameLedgerEventCount { get; init; }
        public long FrameLedgerDroppedEventCount { get; init; }
        public FrameLedgerEventSnapshot[] FrameLedgerRecentEvents { get; init; }
    }

    private static CaptureTransportFlattenedProjection BuildCaptureTransportFlattenedProjection(
        CaptureTransportProjection captureTransport)
        => new()
        {
            MemoryPreference = captureTransport.MemoryPreference,
            VideoRequestedSubtype = captureTransport.VideoRequestedSubtype,
            VideoNegotiatedSubtype = captureTransport.VideoNegotiatedSubtype,
            FrameLedgerCapacity = captureTransport.FrameLedgerCapacity,
            FrameLedgerEventCount = captureTransport.FrameLedgerEventCount,
            FrameLedgerDroppedEventCount = captureTransport.FrameLedgerDroppedEventCount,
            FrameLedgerRecentEvents = captureTransport.FrameLedgerRecentEvents
        };

    private readonly record struct CaptureTransportFlattenedProjection
    {
        public string MemoryPreference { get; init; }
        public string VideoRequestedSubtype { get; init; }
        public string VideoNegotiatedSubtype { get; init; }
        public int FrameLedgerCapacity { get; init; }
        public long FrameLedgerEventCount { get; init; }
        public long FrameLedgerDroppedEventCount { get; init; }
        public FrameLedgerEventSnapshot[] FrameLedgerRecentEvents { get; init; }
    }

    private readonly record struct CaptureFormatFlattenedProjection
    {
        public CaptureFormatRequestedFlattenedProjection Requested { get; init; }
        public CaptureFormatHdrRequestFlattenedProjection HdrRequest { get; init; }
        public CaptureFormatActualFlattenedProjection Actual { get; init; }
        public CaptureFormatNegotiatedFlattenedProjection Negotiated { get; init; }
        public CaptureFormatReaderObservationFlattenedProjection ReaderObservation { get; init; }
        public CaptureFormatEncoderFlattenedProjection Encoder { get; init; }
    }
}
