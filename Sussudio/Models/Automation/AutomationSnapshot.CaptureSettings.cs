namespace Sussudio.Models;

public sealed partial class AutomationSnapshot
{
    public string? SelectedDeviceId { get; init; }
    public string? SelectedDeviceName { get; init; }
    public string? SelectedAudioInputDeviceId { get; init; }
    public string? SelectedAudioInputDeviceName { get; init; }

    public string? SelectedResolution { get; init; }
    public double SelectedFrameRate { get; init; }
    public double? SelectedFriendlyFrameRate { get; init; }
    public double? SelectedExactFrameRate { get; init; }
    public string? SelectedExactFrameRateArg { get; init; }
    public string? DisabledResolutionReason { get; init; }
    public string? DisabledFrameRateReason { get; init; }
    public string SelectedRecordingFormat { get; init; } = string.Empty;
    public string SelectedQuality { get; init; } = string.Empty;
    public string SelectedPreset { get; init; } = string.Empty;
    public string SelectedSplitEncodeMode { get; init; } = string.Empty;
    public string SelectedVideoFormat { get; init; } = string.Empty;
    public double CustomBitrateMbps { get; init; }
    public double PreviewVolumePercent { get; init; }
    public bool IsStatsVisible { get; init; }

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
    public string HdrActivationReason { get; init; } = "Unknown";
    public bool HdrAutoDowngraded { get; init; }
    public string HdrAutoDowngradeReason { get; init; } = string.Empty;
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

    public bool IsHdrAvailable { get; init; }
    public bool IsHdrEnabled { get; init; }
    public bool HdrOutputActive { get; init; }
    public string HdrRuntimeState { get; init; } = "Inactive";
    public string HdrReadinessReason { get; init; } = string.Empty;
    public string HdrWarmupState { get; init; } = "NotStarted";
    public int HdrWarmupRequiredP010Frames { get; init; }
    public int HdrWarmupAllowedNonP010Frames { get; init; }
    public int HdrWarmupObservedP010Frames { get; init; }
    public int HdrWarmupObservedNonP010Frames { get; init; }
    public string HdrDowngradeCode { get; init; } = string.Empty;
    public string RequestedPipelineMode { get; init; } = "SDR";
    public string ActivePipelineMode { get; init; } = "SDR";
    public bool PipelineModeMatched { get; init; } = true;
    public string PipelineModeStatus { get; init; } = "Ready";
    public string PipelineModeReason { get; init; } = string.Empty;
    public string TelemetryAlignmentStatus { get; init; } = "Unknown";
    public string TelemetryAlignmentReason { get; init; } = string.Empty;

    public HdrTruthVerdict? HdrTruthVerdict { get; init; }
}
