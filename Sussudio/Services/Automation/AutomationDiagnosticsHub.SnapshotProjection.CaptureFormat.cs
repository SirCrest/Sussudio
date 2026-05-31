using System;
using System.Collections.Generic;
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

    private static bool IsHdrSubtype(string? subtype)
        => MediaFormat.IsHdrPixelFormat(subtype);

    private static PreviewHdrState BuildPreviewHdrState(
        CaptureRuntimeSnapshot captureRuntime,
        ViewModelRuntimeSnapshot viewModelSnapshot,
        PreviewRuntimeSnapshot previewRuntime)
    {
        var inputDetected =
            IsHdrSubtype(captureRuntime.NegotiatedPixelFormat) ||
            (captureRuntime.RequestedHdrEnabled ?? false) ||
            viewModelSnapshot.IsHdrEnabled;
        var toneMapMode = !inputDetected
            ? "None"
            : previewRuntime.GpuActive
                ? "Auto"
                : "Unavailable";

        return new PreviewHdrState(inputDetected, toneMapMode);
    }

    private static HdrTruthVerdict BuildHdrTruthVerdict(
        CaptureRuntimeSnapshot captureRuntime,
        bool hdrEnabledInUi,
        RecordingVerificationResult? lastVerification)
    {
        static string NormalizeFormatToken(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "unknown";
            }

            var value = text.Trim();
            if (value.Contains("P010", StringComparison.OrdinalIgnoreCase))
            {
                return "P010";
            }

            if (value.Contains("NV12", StringComparison.OrdinalIgnoreCase))
            {
                return "NV12";
            }

            return value.ToUpperInvariant();
        }

        var evidence = new List<string>(capacity: 8);
        var observedFormatToken = NormalizeFormatToken(
            captureRuntime.LatestObservedFramePixelFormat ??
            captureRuntime.FirstObservedFramePixelFormat ??
            captureRuntime.NegotiatedPixelFormat);
        var hasP010 = captureRuntime.ObservedP010FrameCount > 0 || string.Equals(observedFormatToken, "P010", StringComparison.OrdinalIgnoreCase);
        var hasNv12 = captureRuntime.ObservedNv12FrameCount > 0 || string.Equals(observedFormatToken, "NV12", StringComparison.OrdinalIgnoreCase);
        var pipelineFormat = hasP010
            ? "P010"
            : hasNv12
                ? "NV12"
                : observedFormatToken;

        if (hasP010)
        {
            evidence.Add($"observed-p010-frames={captureRuntime.ObservedP010FrameCount}");
        }
        if (hasNv12)
        {
            evidence.Add($"observed-nv12-frames={captureRuntime.ObservedNv12FrameCount}");
        }

        string effectiveBitDepth;
        if (string.Equals(pipelineFormat, "NV12", StringComparison.OrdinalIgnoreCase))
        {
            effectiveBitDepth = "8bit-like";
        }
        else if (string.Equals(pipelineFormat, "P010", StringComparison.OrdinalIgnoreCase))
        {
            if (captureRuntime.ObservedP010Likely8BitUpscaled == true)
            {
                effectiveBitDepth = "8bit-like";
                evidence.Add("p010-samples-look-upscaled-8bit=true");
            }
            else if (captureRuntime.ObservedP010BitDepthSampleCount > 0)
            {
                effectiveBitDepth = captureRuntime.ObservedP010Low2BitNonZeroPercent >= 0.50
                    ? "10bit"
                    : "8bit-like";
                evidence.Add(
                    $"p010-low2-nonzero-pct={captureRuntime.ObservedP010Low2BitNonZeroPercent:0.###} (samples={captureRuntime.ObservedP010BitDepthSampleCount})");
            }
            else
            {
                effectiveBitDepth = "unknown";
                evidence.Add("p010-bitdepth-samples=0");
            }
        }
        else
        {
            effectiveBitDepth = "unknown";
        }

        string metadataState;
        if (lastVerification is null)
        {
            metadataState = "unknown";
            evidence.Add("metadata=verification-not-run");
        }
        else if (lastVerification.HdrColorimetryValid == false)
        {
            metadataState = "invalid";
            evidence.Add("metadata=colorimetry-invalid");
        }
        else if (lastVerification.HdrMetadataPresent == true)
        {
            metadataState = "present-valid";
            evidence.Add("metadata=present-valid");
        }
        else if (lastVerification.HdrMetadataPresent == false)
        {
            metadataState = "missing";
            evidence.Add("metadata=missing");
        }
        else
        {
            metadataState = "unknown";
            evidence.Add("metadata=unknown");
        }

        var captureHdrLike =
            string.Equals(pipelineFormat, "P010", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(effectiveBitDepth, "10bit", StringComparison.OrdinalIgnoreCase);
        var sourceHdr = captureRuntime.SourceIsHdr;
        string sourceVsCaptureParity;
        if (!sourceHdr.HasValue)
        {
            sourceVsCaptureParity = "unknown";
        }
        else if (sourceHdr.Value == captureHdrLike)
        {
            sourceVsCaptureParity = "match";
        }
        else if (sourceHdr.Value && !captureHdrLike && !hdrEnabledInUi)
        {
            sourceVsCaptureParity = "expected-sdr-capture";
            evidence.Add("source-hdr=true, capture-hdr-like=false, hdr-requested=false");
        }
        else
        {
            sourceVsCaptureParity = "mismatch";
            evidence.Add($"source-hdr={sourceHdr.Value}, capture-hdr-like={captureHdrLike}");
        }

        var finalClassification = pipelineFormat switch
        {
            "NV12" => "sdr-8bit",
            "P010" when string.Equals(effectiveBitDepth, "10bit", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(metadataState, "present-valid", StringComparison.OrdinalIgnoreCase)
                => "true-hdr10",
            "P010" => "p010-sdr",
            _ => "inconclusive"
        };

        if (hdrEnabledInUi && string.Equals(finalClassification, "sdr-8bit", StringComparison.OrdinalIgnoreCase))
        {
            evidence.Add("hdr-enabled-ui-while-effective-path-is-sdr-8bit");
        }

        return new HdrTruthVerdict
        {
            PipelineFormat = pipelineFormat,
            EffectiveBitDepth = effectiveBitDepth,
            HdrMetadataState = metadataState,
            SourceVsCaptureParity = sourceVsCaptureParity,
            FinalClassification = finalClassification,
            Evidence = evidence
        };
    }

    private static HdrPipelineProjection BuildHdrPipelineProjection(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        CaptureRuntimeSnapshot captureRuntime,
        HdrTruthVerdict truthVerdict)
        => new()
        {
            IsHdrAvailable = viewModelSnapshot.IsHdrAvailable,
            IsHdrEnabled = viewModelSnapshot.IsHdrEnabled,
            HdrOutputActive = captureRuntime.HdrOutputActive,
            HdrRuntimeState = PreferViewModelHdrText(viewModelSnapshot.HdrRuntimeState, captureRuntime.HdrRuntimeState),
            HdrReadinessReason = PreferViewModelHdrText(viewModelSnapshot.HdrReadinessReason, captureRuntime.HdrReadinessReason),
            HdrWarmupState = captureRuntime.HdrWarmupState,
            HdrWarmupRequiredP010Frames = captureRuntime.HdrWarmupRequiredP010Frames,
            HdrWarmupAllowedNonP010Frames = captureRuntime.HdrWarmupAllowedNonP010Frames,
            HdrWarmupObservedP010Frames = captureRuntime.HdrWarmupObservedP010Frames,
            HdrWarmupObservedNonP010Frames = captureRuntime.HdrWarmupObservedNonP010Frames,
            HdrDowngradeCode = captureRuntime.HdrDowngradeCode,
            RequestedPipelineMode = captureRuntime.RequestedPipelineMode,
            ActivePipelineMode = captureRuntime.ActivePipelineMode,
            PipelineModeMatched = captureRuntime.PipelineModeMatched,
            PipelineModeStatus = captureRuntime.PipelineModeStatus,
            PipelineModeReason = captureRuntime.PipelineModeReason,
            TelemetryAlignmentStatus = captureRuntime.TelemetryAlignmentStatus,
            TelemetryAlignmentReason = captureRuntime.TelemetryAlignmentReason,
            TruthVerdict = truthVerdict
        };

    private static string PreferViewModelHdrText(string viewModelValue, string runtimeValue)
        => !string.IsNullOrWhiteSpace(viewModelValue) ? viewModelValue : runtimeValue;

    private readonly record struct HdrPipelineProjection
    {
        public bool IsHdrAvailable { get; init; }
        public bool IsHdrEnabled { get; init; }
        public bool HdrOutputActive { get; init; }
        public string HdrRuntimeState { get; init; }
        public string HdrReadinessReason { get; init; }
        public string HdrWarmupState { get; init; }
        public int HdrWarmupRequiredP010Frames { get; init; }
        public int HdrWarmupAllowedNonP010Frames { get; init; }
        public int HdrWarmupObservedP010Frames { get; init; }
        public int HdrWarmupObservedNonP010Frames { get; init; }
        public string HdrDowngradeCode { get; init; }
        public string RequestedPipelineMode { get; init; }
        public string ActivePipelineMode { get; init; }
        public bool PipelineModeMatched { get; init; }
        public string PipelineModeStatus { get; init; }
        public string PipelineModeReason { get; init; }
        public string TelemetryAlignmentStatus { get; init; }
        public string TelemetryAlignmentReason { get; init; }
        public HdrTruthVerdict TruthVerdict { get; init; }
    }

    private static HdrPipelineFlattenedProjection BuildHdrPipelineFlattenedProjection(
        HdrPipelineProjection hdrPipeline)
        => new()
        {
            IsHdrAvailable = hdrPipeline.IsHdrAvailable,
            IsHdrEnabled = hdrPipeline.IsHdrEnabled,
            HdrOutputActive = hdrPipeline.HdrOutputActive,
            HdrRuntimeState = hdrPipeline.HdrRuntimeState,
            HdrReadinessReason = hdrPipeline.HdrReadinessReason,
            HdrWarmupState = hdrPipeline.HdrWarmupState,
            HdrWarmupRequiredP010Frames = hdrPipeline.HdrWarmupRequiredP010Frames,
            HdrWarmupAllowedNonP010Frames = hdrPipeline.HdrWarmupAllowedNonP010Frames,
            HdrWarmupObservedP010Frames = hdrPipeline.HdrWarmupObservedP010Frames,
            HdrWarmupObservedNonP010Frames = hdrPipeline.HdrWarmupObservedNonP010Frames,
            HdrDowngradeCode = hdrPipeline.HdrDowngradeCode,
            RequestedPipelineMode = hdrPipeline.RequestedPipelineMode,
            ActivePipelineMode = hdrPipeline.ActivePipelineMode,
            PipelineModeMatched = hdrPipeline.PipelineModeMatched,
            PipelineModeStatus = hdrPipeline.PipelineModeStatus,
            PipelineModeReason = hdrPipeline.PipelineModeReason,
            TelemetryAlignmentStatus = hdrPipeline.TelemetryAlignmentStatus,
            TelemetryAlignmentReason = hdrPipeline.TelemetryAlignmentReason,
            TruthVerdict = hdrPipeline.TruthVerdict
        };

    private readonly record struct HdrPipelineFlattenedProjection
    {
        public bool IsHdrAvailable { get; init; }
        public bool IsHdrEnabled { get; init; }
        public bool HdrOutputActive { get; init; }
        public string HdrRuntimeState { get; init; }
        public string HdrReadinessReason { get; init; }
        public string HdrWarmupState { get; init; }
        public int HdrWarmupRequiredP010Frames { get; init; }
        public int HdrWarmupAllowedNonP010Frames { get; init; }
        public int HdrWarmupObservedP010Frames { get; init; }
        public int HdrWarmupObservedNonP010Frames { get; init; }
        public string HdrDowngradeCode { get; init; }
        public string RequestedPipelineMode { get; init; }
        public string ActivePipelineMode { get; init; }
        public bool PipelineModeMatched { get; init; }
        public string PipelineModeStatus { get; init; }
        public string PipelineModeReason { get; init; }
        public string TelemetryAlignmentStatus { get; init; }
        public string TelemetryAlignmentReason { get; init; }
        public HdrTruthVerdict TruthVerdict { get; init; }
    }

    private readonly record struct PreviewHdrState(bool InputDetected, string ToneMapMode);

    private static MjpegProjection BuildMjpegProjection(CaptureHealthSnapshot health)
    {
        var timing = BuildMjpegTimingProjection(health);
        var previewJitter = BuildMjpegPreviewJitterProjection(health);
        var packetHash = BuildMjpegPacketHashProjection(health);

        return new()
        {
            Timing = timing,
            TotalDecoded = health.MjpegTotalDecoded,
            TotalEmitted = health.MjpegTotalEmitted,
            TotalDropped = health.MjpegTotalDropped,
            CompressedFramesQueued = health.MjpegCompressedFramesQueued,
            CompressedFramesDequeued = health.MjpegCompressedFramesDequeued,
            CompressedDropsQueueFull = health.MjpegCompressedDropsQueueFull,
            CompressedDropsByteBudget = health.MjpegCompressedDropsByteBudget,
            CompressedDropsDisposed = health.MjpegCompressedDropsDisposed,
            DecodeFailures = health.MjpegDecodeFailures,
            ReorderCollisions = health.MjpegReorderCollisions,
            EmitFailures = health.MjpegEmitFailures,
            CompressedQueueDepth = health.MjpegCompressedQueueDepth,
            CompressedQueueBytes = health.MjpegCompressedQueueBytes,
            CompressedQueueByteBudget = health.MjpegCompressedQueueByteBudget,
            ReorderSkips = health.MjpegReorderSkips,
            ReorderBufferDepth = health.MjpegReorderBufferDepth,
            PreviewJitter = previewJitter,
            PacketHash = packetHash,
        };
    }

    private readonly record struct MjpegProjection
    {
        public MjpegTimingProjection Timing { get; init; }
        public long TotalDecoded { get; init; }
        public long TotalEmitted { get; init; }
        public long TotalDropped { get; init; }
        public long CompressedFramesQueued { get; init; }
        public long CompressedFramesDequeued { get; init; }
        public long CompressedDropsQueueFull { get; init; }
        public long CompressedDropsByteBudget { get; init; }
        public long CompressedDropsDisposed { get; init; }
        public long DecodeFailures { get; init; }
        public long ReorderCollisions { get; init; }
        public long EmitFailures { get; init; }
        public int CompressedQueueDepth { get; init; }
        public long CompressedQueueBytes { get; init; }
        public long CompressedQueueByteBudget { get; init; }
        public long ReorderSkips { get; init; }
        public int ReorderBufferDepth { get; init; }
        public MjpegPreviewJitterProjection PreviewJitter { get; init; }
        public MjpegPacketHashProjection PacketHash { get; init; }
    }

    private static MjpegTimingProjection BuildMjpegTimingProjection(CaptureHealthSnapshot health)
        => new()
        {
            DecodeSampleCount = health.MjpegDecodeSampleCount,
            DecodeAvgMs = health.MjpegDecodeAvgMs,
            DecodeP95Ms = health.MjpegDecodeP95Ms,
            DecodeMaxMs = health.MjpegDecodeMaxMs,
            InteropCopySampleCount = health.MjpegInteropCopySampleCount,
            InteropCopyAvgMs = health.MjpegInteropCopyAvgMs,
            InteropCopyP95Ms = health.MjpegInteropCopyP95Ms,
            InteropCopyMaxMs = health.MjpegInteropCopyMaxMs,
            CallbackSampleCount = health.MjpegCallbackSampleCount,
            CallbackAvgMs = health.MjpegCallbackAvgMs,
            CallbackP95Ms = health.MjpegCallbackP95Ms,
            CallbackMaxMs = health.MjpegCallbackMaxMs,
            DecoderCount = health.MjpegDecoderCount,
            ReorderSampleCount = health.MjpegReorderSampleCount,
            ReorderAvgMs = health.MjpegReorderAvgMs,
            ReorderP95Ms = health.MjpegReorderP95Ms,
            ReorderMaxMs = health.MjpegReorderMaxMs,
            PipelineSampleCount = health.MjpegPipelineSampleCount,
            PipelineAvgMs = health.MjpegPipelineAvgMs,
            PipelineP95Ms = health.MjpegPipelineP95Ms,
            PipelineMaxMs = health.MjpegPipelineMaxMs,
            PerDecoder = health.MjpegPerDecoder is { Length: > 0 } perDecoder
                ? Array.ConvertAll(
                    perDecoder,
                    worker => new MjpegDecoderAutomationSnapshot(
                        worker.WorkerIndex,
                        worker.SampleCount,
                        worker.AvgMs,
                        worker.P95Ms,
                        worker.MaxMs))
                : Array.Empty<MjpegDecoderAutomationSnapshot>()
        };

    private static MjpegTimingFlattenedProjection BuildMjpegTimingFlattenedProjection(
        MjpegTimingProjection timing)
        => new()
        {
            DecodeSampleCount = timing.DecodeSampleCount,
            DecodeAvgMs = timing.DecodeAvgMs,
            DecodeP95Ms = timing.DecodeP95Ms,
            DecodeMaxMs = timing.DecodeMaxMs,
            InteropCopySampleCount = timing.InteropCopySampleCount,
            InteropCopyAvgMs = timing.InteropCopyAvgMs,
            InteropCopyP95Ms = timing.InteropCopyP95Ms,
            InteropCopyMaxMs = timing.InteropCopyMaxMs,
            CallbackSampleCount = timing.CallbackSampleCount,
            CallbackAvgMs = timing.CallbackAvgMs,
            CallbackP95Ms = timing.CallbackP95Ms,
            CallbackMaxMs = timing.CallbackMaxMs,
            DecoderCount = timing.DecoderCount,
            ReorderSampleCount = timing.ReorderSampleCount,
            ReorderAvgMs = timing.ReorderAvgMs,
            ReorderP95Ms = timing.ReorderP95Ms,
            ReorderMaxMs = timing.ReorderMaxMs,
            PipelineSampleCount = timing.PipelineSampleCount,
            PipelineAvgMs = timing.PipelineAvgMs,
            PipelineP95Ms = timing.PipelineP95Ms,
            PipelineMaxMs = timing.PipelineMaxMs,
            PerDecoder = timing.PerDecoder
        };

    private readonly record struct MjpegTimingProjection
    {
        public int DecodeSampleCount { get; init; }
        public double DecodeAvgMs { get; init; }
        public double DecodeP95Ms { get; init; }
        public double DecodeMaxMs { get; init; }
        public int InteropCopySampleCount { get; init; }
        public double InteropCopyAvgMs { get; init; }
        public double InteropCopyP95Ms { get; init; }
        public double InteropCopyMaxMs { get; init; }
        public int CallbackSampleCount { get; init; }
        public double CallbackAvgMs { get; init; }
        public double CallbackP95Ms { get; init; }
        public double CallbackMaxMs { get; init; }
        public int DecoderCount { get; init; }
        public int ReorderSampleCount { get; init; }
        public double ReorderAvgMs { get; init; }
        public double ReorderP95Ms { get; init; }
        public double ReorderMaxMs { get; init; }
        public int PipelineSampleCount { get; init; }
        public double PipelineAvgMs { get; init; }
        public double PipelineP95Ms { get; init; }
        public double PipelineMaxMs { get; init; }
        public MjpegDecoderAutomationSnapshot[] PerDecoder { get; init; }
    }

    private readonly record struct MjpegTimingFlattenedProjection
    {
        public int DecodeSampleCount { get; init; }
        public double DecodeAvgMs { get; init; }
        public double DecodeP95Ms { get; init; }
        public double DecodeMaxMs { get; init; }
        public int InteropCopySampleCount { get; init; }
        public double InteropCopyAvgMs { get; init; }
        public double InteropCopyP95Ms { get; init; }
        public double InteropCopyMaxMs { get; init; }
        public int CallbackSampleCount { get; init; }
        public double CallbackAvgMs { get; init; }
        public double CallbackP95Ms { get; init; }
        public double CallbackMaxMs { get; init; }
        public int DecoderCount { get; init; }
        public int ReorderSampleCount { get; init; }
        public double ReorderAvgMs { get; init; }
        public double ReorderP95Ms { get; init; }
        public double ReorderMaxMs { get; init; }
        public int PipelineSampleCount { get; init; }
        public double PipelineAvgMs { get; init; }
        public double PipelineP95Ms { get; init; }
        public double PipelineMaxMs { get; init; }
        public MjpegDecoderAutomationSnapshot[] PerDecoder { get; init; }
    }

    private static MjpegPreviewJitterProjection BuildMjpegPreviewJitterProjection(CaptureHealthSnapshot health)
        => new()
        {
            Queue = BuildMjpegPreviewJitterQueueProjection(health),
            Timing = BuildMjpegPreviewJitterTimingProjection(health),
            Adaptive = BuildMjpegPreviewJitterAdaptiveProjection(health),
            Events = BuildMjpegPreviewJitterEventProjection(health)
        };

    private readonly record struct MjpegPreviewJitterProjection
    {
        public MjpegPreviewJitterQueueProjection Queue { get; init; }
        public MjpegPreviewJitterTimingProjection Timing { get; init; }
        public MjpegPreviewJitterAdaptiveProjection Adaptive { get; init; }
        public MjpegPreviewJitterEventProjection Events { get; init; }
    }

    private static MjpegPreviewJitterQueueProjection BuildMjpegPreviewJitterQueueProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            Enabled = health.MjpegPreviewJitterEnabled,
            TargetDepth = health.MjpegPreviewJitterTargetDepth,
            MaxDepth = health.MjpegPreviewJitterMaxDepth,
            QueueDepth = health.MjpegPreviewJitterQueueDepth,
            TotalQueued = health.MjpegPreviewJitterTotalQueued,
            TotalSubmitted = health.MjpegPreviewJitterTotalSubmitted,
            TotalDropped = health.MjpegPreviewJitterTotalDropped,
            UnderflowCount = health.MjpegPreviewJitterUnderflowCount,
            ResumeReprimeCount = health.MjpegPreviewJitterResumeReprimeCount
        };

    private readonly record struct MjpegPreviewJitterQueueProjection
    {
        public bool Enabled { get; init; }
        public int TargetDepth { get; init; }
        public int MaxDepth { get; init; }
        public int QueueDepth { get; init; }
        public long TotalQueued { get; init; }
        public long TotalSubmitted { get; init; }
        public long TotalDropped { get; init; }
        public long UnderflowCount { get; init; }
        public long ResumeReprimeCount { get; init; }
    }

    private static MjpegPreviewJitterQueueFlattenedProjection BuildMjpegPreviewJitterQueueFlattenedProjection(
        MjpegPreviewJitterQueueProjection queue)
        => new()
        {
            Enabled = queue.Enabled,
            TargetDepth = queue.TargetDepth,
            MaxDepth = queue.MaxDepth,
            QueueDepth = queue.QueueDepth,
            TotalQueued = queue.TotalQueued,
            TotalSubmitted = queue.TotalSubmitted,
            TotalDropped = queue.TotalDropped,
            UnderflowCount = queue.UnderflowCount,
            ResumeReprimeCount = queue.ResumeReprimeCount
        };

    private readonly record struct MjpegPreviewJitterQueueFlattenedProjection
    {
        public bool Enabled { get; init; }
        public int TargetDepth { get; init; }
        public int MaxDepth { get; init; }
        public int QueueDepth { get; init; }
        public long TotalQueued { get; init; }
        public long TotalSubmitted { get; init; }
        public long TotalDropped { get; init; }
        public long UnderflowCount { get; init; }
        public long ResumeReprimeCount { get; init; }
    }

    private static MjpegPreviewJitterTimingProjection BuildMjpegPreviewJitterTimingProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            InputSampleCount = health.MjpegPreviewJitterInputSampleCount,
            InputAvgMs = health.MjpegPreviewJitterInputAvgMs,
            InputP95Ms = health.MjpegPreviewJitterInputP95Ms,
            InputMaxMs = health.MjpegPreviewJitterInputMaxMs,
            OutputSampleCount = health.MjpegPreviewJitterOutputSampleCount,
            OutputAvgMs = health.MjpegPreviewJitterOutputAvgMs,
            OutputP95Ms = health.MjpegPreviewJitterOutputP95Ms,
            OutputMaxMs = health.MjpegPreviewJitterOutputMaxMs,
            LatencySampleCount = health.MjpegPreviewJitterLatencySampleCount,
            LatencyAvgMs = health.MjpegPreviewJitterLatencyAvgMs,
            LatencyP95Ms = health.MjpegPreviewJitterLatencyP95Ms,
            LatencyMaxMs = health.MjpegPreviewJitterLatencyMaxMs
        };

    private readonly record struct MjpegPreviewJitterTimingProjection
    {
        public int InputSampleCount { get; init; }
        public double InputAvgMs { get; init; }
        public double InputP95Ms { get; init; }
        public double InputMaxMs { get; init; }
        public int OutputSampleCount { get; init; }
        public double OutputAvgMs { get; init; }
        public double OutputP95Ms { get; init; }
        public double OutputMaxMs { get; init; }
        public int LatencySampleCount { get; init; }
        public double LatencyAvgMs { get; init; }
        public double LatencyP95Ms { get; init; }
        public double LatencyMaxMs { get; init; }
    }

    private static MjpegPreviewJitterTimingFlattenedProjection BuildMjpegPreviewJitterTimingFlattenedProjection(
        MjpegPreviewJitterTimingProjection timing)
        => new()
        {
            InputSampleCount = timing.InputSampleCount,
            InputAvgMs = timing.InputAvgMs,
            InputP95Ms = timing.InputP95Ms,
            InputMaxMs = timing.InputMaxMs,
            OutputSampleCount = timing.OutputSampleCount,
            OutputAvgMs = timing.OutputAvgMs,
            OutputP95Ms = timing.OutputP95Ms,
            OutputMaxMs = timing.OutputMaxMs,
            LatencySampleCount = timing.LatencySampleCount,
            LatencyAvgMs = timing.LatencyAvgMs,
            LatencyP95Ms = timing.LatencyP95Ms,
            LatencyMaxMs = timing.LatencyMaxMs
        };

    private readonly record struct MjpegPreviewJitterTimingFlattenedProjection
    {
        public int InputSampleCount { get; init; }
        public double InputAvgMs { get; init; }
        public double InputP95Ms { get; init; }
        public double InputMaxMs { get; init; }
        public int OutputSampleCount { get; init; }
        public double OutputAvgMs { get; init; }
        public double OutputP95Ms { get; init; }
        public double OutputMaxMs { get; init; }
        public int LatencySampleCount { get; init; }
        public double LatencyAvgMs { get; init; }
        public double LatencyP95Ms { get; init; }
        public double LatencyMaxMs { get; init; }
    }

    private static MjpegPreviewJitterAdaptiveProjection BuildMjpegPreviewJitterAdaptiveProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            DeadlineDropCount = health.MjpegPreviewJitterDeadlineDropCount,
            ClearedDropCount = health.MjpegPreviewJitterClearedDropCount,
            TargetIncreaseCount = health.MjpegPreviewJitterTargetIncreaseCount,
            TargetDecreaseCount = health.MjpegPreviewJitterTargetDecreaseCount
        };

    private readonly record struct MjpegPreviewJitterAdaptiveProjection
    {
        public long DeadlineDropCount { get; init; }
        public long ClearedDropCount { get; init; }
        public long TargetIncreaseCount { get; init; }
        public long TargetDecreaseCount { get; init; }
    }

    private static MjpegPreviewJitterAdaptiveFlattenedProjection BuildMjpegPreviewJitterAdaptiveFlattenedProjection(
        MjpegPreviewJitterAdaptiveProjection adaptive)
        => new()
        {
            DeadlineDropCount = adaptive.DeadlineDropCount,
            ClearedDropCount = adaptive.ClearedDropCount,
            TargetIncreaseCount = adaptive.TargetIncreaseCount,
            TargetDecreaseCount = adaptive.TargetDecreaseCount
        };

    private readonly record struct MjpegPreviewJitterAdaptiveFlattenedProjection
    {
        public long DeadlineDropCount { get; init; }
        public long ClearedDropCount { get; init; }
        public long TargetIncreaseCount { get; init; }
        public long TargetDecreaseCount { get; init; }
    }

    private static MjpegPreviewJitterEventProjection BuildMjpegPreviewJitterEventProjection(
        CaptureHealthSnapshot health)
        => new()
        {
            LastSelectedPreviewPresentId = health.MjpegPreviewJitterLastSelectedPreviewPresentId,
            LastSelectedSourceSequenceNumber = health.MjpegPreviewJitterLastSelectedSourceSequenceNumber,
            LastSelectedQpc = health.MjpegPreviewJitterLastSelectedQpc,
            LastSelectedSourceLatencyMs = health.MjpegPreviewJitterLastSelectedSourceLatencyMs,
            LastDroppedSourceSequenceNumber = health.MjpegPreviewJitterLastDroppedSourceSequenceNumber,
            LastDropQpc = health.MjpegPreviewJitterLastDropQpc,
            LastDropReason = health.MjpegPreviewJitterLastDropReason,
            LastUnderflowQpc = health.MjpegPreviewJitterLastUnderflowQpc,
            LastUnderflowReason = health.MjpegPreviewJitterLastUnderflowReason,
            LastUnderflowQueueDepth = health.MjpegPreviewJitterLastUnderflowQueueDepth,
            LastUnderflowInputAgeMs = health.MjpegPreviewJitterLastUnderflowInputAgeMs,
            LastUnderflowOutputAgeMs = health.MjpegPreviewJitterLastUnderflowOutputAgeMs,
            LastScheduleLateMs = health.MjpegPreviewJitterLastScheduleLateMs,
            MaxScheduleLateMs = health.MjpegPreviewJitterMaxScheduleLateMs,
            ScheduleLateCount = health.MjpegPreviewJitterScheduleLateCount
        };

    private readonly record struct MjpegPreviewJitterEventProjection
    {
        public long LastSelectedPreviewPresentId { get; init; }
        public long LastSelectedSourceSequenceNumber { get; init; }
        public long LastSelectedQpc { get; init; }
        public double LastSelectedSourceLatencyMs { get; init; }
        public long LastDroppedSourceSequenceNumber { get; init; }
        public long LastDropQpc { get; init; }
        public string LastDropReason { get; init; }
        public long LastUnderflowQpc { get; init; }
        public string LastUnderflowReason { get; init; }
        public int LastUnderflowQueueDepth { get; init; }
        public double LastUnderflowInputAgeMs { get; init; }
        public double LastUnderflowOutputAgeMs { get; init; }
        public double LastScheduleLateMs { get; init; }
        public double MaxScheduleLateMs { get; init; }
        public long ScheduleLateCount { get; init; }
    }

    private static MjpegPreviewJitterEventFlattenedProjection BuildMjpegPreviewJitterEventFlattenedProjection(
        MjpegPreviewJitterEventProjection events)
        => new()
        {
            LastSelectedPreviewPresentId = events.LastSelectedPreviewPresentId,
            LastSelectedSourceSequenceNumber = events.LastSelectedSourceSequenceNumber,
            LastSelectedQpc = events.LastSelectedQpc,
            LastSelectedSourceLatencyMs = events.LastSelectedSourceLatencyMs,
            LastDroppedSourceSequenceNumber = events.LastDroppedSourceSequenceNumber,
            LastDropQpc = events.LastDropQpc,
            LastDropReason = events.LastDropReason,
            LastUnderflowQpc = events.LastUnderflowQpc,
            LastUnderflowReason = events.LastUnderflowReason,
            LastUnderflowQueueDepth = events.LastUnderflowQueueDepth,
            LastUnderflowInputAgeMs = events.LastUnderflowInputAgeMs,
            LastUnderflowOutputAgeMs = events.LastUnderflowOutputAgeMs,
            LastScheduleLateMs = events.LastScheduleLateMs,
            MaxScheduleLateMs = events.MaxScheduleLateMs,
            ScheduleLateCount = events.ScheduleLateCount
        };

    private readonly record struct MjpegPreviewJitterEventFlattenedProjection
    {
        public long LastSelectedPreviewPresentId { get; init; }
        public long LastSelectedSourceSequenceNumber { get; init; }
        public long LastSelectedQpc { get; init; }
        public double LastSelectedSourceLatencyMs { get; init; }
        public long LastDroppedSourceSequenceNumber { get; init; }
        public long LastDropQpc { get; init; }
        public string LastDropReason { get; init; }
        public long LastUnderflowQpc { get; init; }
        public string LastUnderflowReason { get; init; }
        public int LastUnderflowQueueDepth { get; init; }
        public double LastUnderflowInputAgeMs { get; init; }
        public double LastUnderflowOutputAgeMs { get; init; }
        public double LastScheduleLateMs { get; init; }
        public double MaxScheduleLateMs { get; init; }
        public long ScheduleLateCount { get; init; }
    }

    private static MjpegPreviewJitterFlattenedProjection BuildMjpegPreviewJitterFlattenedProjection(
        MjpegPreviewJitterProjection previewJitter)
        => new()
        {
            Queue = BuildMjpegPreviewJitterQueueFlattenedProjection(previewJitter.Queue),
            Timing = BuildMjpegPreviewJitterTimingFlattenedProjection(previewJitter.Timing),
            Adaptive = BuildMjpegPreviewJitterAdaptiveFlattenedProjection(previewJitter.Adaptive),
            Events = BuildMjpegPreviewJitterEventFlattenedProjection(previewJitter.Events)
        };

    private readonly record struct MjpegPreviewJitterFlattenedProjection
    {
        public MjpegPreviewJitterQueueFlattenedProjection Queue { get; init; }
        public MjpegPreviewJitterTimingFlattenedProjection Timing { get; init; }
        public MjpegPreviewJitterAdaptiveFlattenedProjection Adaptive { get; init; }
        public MjpegPreviewJitterEventFlattenedProjection Events { get; init; }
    }

    private static MjpegPacketHashProjection BuildMjpegPacketHashProjection(CaptureHealthSnapshot health)
        => new()
        {
            SampleCount = health.MjpegPacketHashSampleCount,
            UniqueFrameCount = health.MjpegPacketHashUniqueFrameCount,
            DuplicateFrameCount = health.MjpegPacketHashDuplicateFrameCount,
            LongestDuplicateRun = health.MjpegPacketHashLongestDuplicateRun,
            InputObservedFps = health.MjpegPacketHashInputObservedFps,
            UniqueObservedFps = health.MjpegPacketHashUniqueObservedFps,
            DuplicateFramePercent = health.MjpegPacketHashDuplicateFramePercent,
            LastHash = health.MjpegPacketHashLastHash,
            LastFrameDuplicate = health.MjpegPacketHashLastFrameDuplicate,
            Pattern = health.MjpegPacketHashPattern,
            RecentInputIntervalsMs = health.MjpegPacketHashRecentInputIntervalsMs,
            RecentUniqueIntervalsMs = health.MjpegPacketHashRecentUniqueIntervalsMs,
            RecentDuplicateFlags = health.MjpegPacketHashRecentDuplicateFlags
        };

    private static MjpegPacketHashFlattenedProjection BuildMjpegPacketHashFlattenedProjection(
        MjpegPacketHashProjection packetHash)
        => new()
        {
            SampleCount = packetHash.SampleCount,
            UniqueFrameCount = packetHash.UniqueFrameCount,
            DuplicateFrameCount = packetHash.DuplicateFrameCount,
            LongestDuplicateRun = packetHash.LongestDuplicateRun,
            InputObservedFps = packetHash.InputObservedFps,
            UniqueObservedFps = packetHash.UniqueObservedFps,
            DuplicateFramePercent = packetHash.DuplicateFramePercent,
            LastHash = packetHash.LastHash,
            LastFrameDuplicate = packetHash.LastFrameDuplicate,
            Pattern = packetHash.Pattern,
            RecentInputIntervalsMs = packetHash.RecentInputIntervalsMs,
            RecentUniqueIntervalsMs = packetHash.RecentUniqueIntervalsMs,
            RecentDuplicateFlags = packetHash.RecentDuplicateFlags
        };

    private readonly record struct MjpegPacketHashProjection
    {
        public int SampleCount { get; init; }
        public long UniqueFrameCount { get; init; }
        public long DuplicateFrameCount { get; init; }
        public long LongestDuplicateRun { get; init; }
        public double InputObservedFps { get; init; }
        public double UniqueObservedFps { get; init; }
        public double DuplicateFramePercent { get; init; }
        public string LastHash { get; init; }
        public bool LastFrameDuplicate { get; init; }
        public string Pattern { get; init; }
        public double[] RecentInputIntervalsMs { get; init; }
        public double[] RecentUniqueIntervalsMs { get; init; }
        public int[] RecentDuplicateFlags { get; init; }
    }

    private readonly record struct MjpegPacketHashFlattenedProjection
    {
        public int SampleCount { get; init; }
        public long UniqueFrameCount { get; init; }
        public long DuplicateFrameCount { get; init; }
        public long LongestDuplicateRun { get; init; }
        public double InputObservedFps { get; init; }
        public double UniqueObservedFps { get; init; }
        public double DuplicateFramePercent { get; init; }
        public string LastHash { get; init; }
        public bool LastFrameDuplicate { get; init; }
        public string Pattern { get; init; }
        public double[] RecentInputIntervalsMs { get; init; }
        public double[] RecentUniqueIntervalsMs { get; init; }
        public int[] RecentDuplicateFlags { get; init; }
    }

    private static MjpegFlattenedProjection BuildMjpegFlattenedProjection(MjpegProjection mjpeg)
    {
        return new()
        {
            TotalDecoded = mjpeg.TotalDecoded,
            TotalEmitted = mjpeg.TotalEmitted,
            TotalDropped = mjpeg.TotalDropped,
            CompressedFramesQueued = mjpeg.CompressedFramesQueued,
            CompressedFramesDequeued = mjpeg.CompressedFramesDequeued,
            CompressedDropsQueueFull = mjpeg.CompressedDropsQueueFull,
            CompressedDropsByteBudget = mjpeg.CompressedDropsByteBudget,
            CompressedDropsDisposed = mjpeg.CompressedDropsDisposed,
            DecodeFailures = mjpeg.DecodeFailures,
            ReorderCollisions = mjpeg.ReorderCollisions,
            EmitFailures = mjpeg.EmitFailures,
            CompressedQueueDepth = mjpeg.CompressedQueueDepth,
            CompressedQueueBytes = mjpeg.CompressedQueueBytes,
            CompressedQueueByteBudget = mjpeg.CompressedQueueByteBudget,
            ReorderSkips = mjpeg.ReorderSkips,
            ReorderBufferDepth = mjpeg.ReorderBufferDepth,
        };
    }

    private readonly record struct MjpegFlattenedProjection
    {
        public long TotalDecoded { get; init; }
        public long TotalEmitted { get; init; }
        public long TotalDropped { get; init; }
        public long CompressedFramesQueued { get; init; }
        public long CompressedFramesDequeued { get; init; }
        public long CompressedDropsQueueFull { get; init; }
        public long CompressedDropsByteBudget { get; init; }
        public long CompressedDropsDisposed { get; init; }
        public long DecodeFailures { get; init; }
        public long ReorderCollisions { get; init; }
        public long EmitFailures { get; init; }
        public int CompressedQueueDepth { get; init; }
        public long CompressedQueueBytes { get; init; }
        public long CompressedQueueByteBudget { get; init; }
        public long ReorderSkips { get; init; }
        public int ReorderBufferDepth { get; init; }
    }
}
