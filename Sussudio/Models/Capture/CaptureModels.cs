using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading;

namespace Sussudio.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NvencPreset
{
    Auto,
    P1,
    P2,
    P3,
    P4,
    P5,
    P6,
    P7,
    Fast,
    Slow
}
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SplitEncodeMode
{
    Auto,
    Disabled,
    TwoWay,
    ThreeWay,
    ForcedOn
}

public enum RecordingFormat
{
    H264Mp4,
    HevcMp4,
    Av1Mp4
}

public enum VideoQuality
{
    Auto,       // Let the encoder decide based on resolution
    Low,        // ~8 Mbps for 1080p, scales with resolution
    Medium,     // ~15 Mbps for 1080p
    High,       // ~25 Mbps for 1080p
    SuperHigh,  // ~40 Mbps for 1080p
    Custom      // User-specified bitrate
}

public enum HdrOutputMode
{
    Off,
    Hdr10Pq
}

public enum PreviewMode
{
    GpuFast,
    TrueHdr
}

// Audio endpoint option displayed in the UI and persisted by settings.
public class AudioInputDevice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "Unknown Audio Device" : Name;

    public override string ToString() => DisplayName;
}

// Level-meter update payload from capture services to UI/view-model subscribers.
public sealed class AudioLevelEventArgs : EventArgs
{
    public AudioLevelEventArgs(double peak, double rms, bool clipped)
    {
        Peak = peak;
        Rms = rms;
        Clipped = clipped;
    }

    public double Peak { get; }
    public double Rms { get; }
    public bool Clipped { get; }
}

// Recording/monitoring audio topology reported in diagnostics.
public enum AudioPathMode
{
    PostMuxDefault
}

// Bounded audio-transition trace returned through automation for stutter/ramp
// investigations.
public sealed class AudioRampTraceSnapshot
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public int SampleIntervalMs { get; init; }
    public int Capacity { get; init; }
    public int EntryCount { get; init; }
    public bool IsSamplingActive { get; init; }
    public long ActiveSessionId { get; init; }
    public string ActiveReason { get; init; } = string.Empty;
    public AudioRampTraceEntry[] Entries { get; init; } = Array.Empty<AudioRampTraceEntry>();
}

// One 10ms-ish sample of control-side volume state plus render/capture evidence.
public sealed class AudioRampTraceEntry
{
    public long Sequence { get; init; }
    public long SessionId { get; init; }
    public string Kind { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string Note { get; init; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public double ElapsedMs { get; init; }
    public double PreviewVolumePercent { get; init; }
    public double TargetVolumePercent { get; init; }
    public double PlaybackTargetVolumePercent { get; init; }
    public double PlaybackCurrentVolumePercent { get; init; }
    public double PlaybackOutputPeak { get; init; }
    public double PlaybackOutputRms { get; init; }
    public long PlaybackOutputAgeMs { get; init; }
    public long PlaybackRenderCallbackCount { get; init; }
    public int PlaybackQueueDepth { get; init; }
    public bool IsAudioEnabled { get; init; }
    public bool IsAudioPreviewEnabled { get; init; }
    public bool IsAudioPreviewActive { get; init; }
    public bool AudioReaderActive { get; init; }
    public double CaptureAudioPeak { get; init; }
    public long AudioFramesArrived { get; init; }
}

public class CaptureSettings
{
    public uint Width { get; set; } = 1920;
    public uint Height { get; set; } = 1080;
    public double FrameRate { get; set; } = 60;
    public string? RequestedFrameRateArg { get; set; }
    public uint? RequestedFrameRateNumerator { get; set; }
    public uint? RequestedFrameRateDenominator { get; set; }
    public string? RequestedPixelFormat { get; set; }
    public RecordingFormat Format { get; set; } = RecordingFormat.H264Mp4;
    public VideoQuality Quality { get; set; } = VideoQuality.High;
    public NvencPreset NvencPreset { get; set; } = NvencPreset.Auto;
    public SplitEncodeMode SplitEncodeMode { get; set; } = SplitEncodeMode.Auto;
    public double CustomBitrateMbps { get; set; } = 50; // Used when Quality is Custom
    public bool HdrEnabled { get; set; }
    public HdrOutputMode HdrOutputMode { get; set; } = HdrOutputMode.Hdr10Pq;
    public int HdrNominalPeakNits { get; set; } = 1000;
    // Optional HDR10 static metadata (only emitted when explicitly configured).
    public int HdrMaxCll { get; set; }
    public int HdrMaxFall { get; set; }
    public string HdrMasterDisplayMetadata { get; set; } = string.Empty;
    public PreviewMode PreviewMode { get; set; } = PreviewMode.GpuFast;
    public string OutputPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
    public bool AudioEnabled { get; set; } = true;
    public bool UseCustomAudioInput { get; set; }
    public string? AudioDeviceId { get; set; }
    public string? AudioDeviceName { get; set; }
    public bool MicrophoneEnabled { get; set; }
    public string? MicrophoneDeviceId { get; set; }
    public string? MicrophoneDeviceName { get; set; }
    public AudioPathMode AudioPathMode { get; set; } = AudioPathMode.PostMuxDefault;
    public RecordingPipelineOptions PipelineOptions { get; set; } = new();
    public bool ForceMjpegDecode { get; set; }
    public bool FlashbackGpuDecode { get; set; } = true;
    public int FlashbackBufferMinutes { get; set; } = 5;
    public int MjpegDecoderCount { get; set; } = 6;

    public bool UseMjpegHighFrameRateMode =>
        IsMjpegHighFrameRateMode(RequestedPixelFormat, Width, Height, FrameRate, HdrEnabled, ForceMjpegDecode);

    public static bool IsMjpegHighFrameRateMode(
        string? requestedPixelFormat,
        uint width,
        uint height,
        double frameRate,
        bool hdrEnabled,
        bool force = false)
    {
        if (hdrEnabled)
        {
            return false;
        }

        if (!string.Equals(requestedPixelFormat, "MJPG", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return force || (width >= 3840 && height >= 2160 && frameRate >= 100);
    }

    /// <summary>
    /// Calculates the target video bitrate based on quality setting, resolution, and frame rate.
    /// Returns bitrate in bits per second.
    /// </summary>
    public uint GetTargetBitrate()
    {
        // For Custom quality, use the user-specified bitrate directly
        if (Quality == VideoQuality.Custom)
        {
            var customMbps = Math.Clamp(CustomBitrateMbps, 1, 300);
            return (uint)(customMbps * 1_000_000);
        }

        // Base bitrates for 1080p30 (in Mbps)
        double baseMbps = Quality switch
        {
            VideoQuality.Low => 8,
            VideoQuality.Medium => 15,
            VideoQuality.High => 25,
            VideoQuality.SuperHigh => 40,
            VideoQuality.Auto => 20, // Default for Auto
            _ => 20
        };

        // Scale by resolution (relative to 1080p = 2,073,600 pixels)
        double pixelCount = Width * Height;
        double resolutionScale = pixelCount / 2_073_600.0;

        // Scale by frame rate (relative to 30fps)
        double frameRateScale = FrameRate / 30.0;

        // Codec efficiency factors (lower = more efficient)
        double codecFactor = Format switch
        {
            RecordingFormat.HevcMp4 => 0.6,
            RecordingFormat.Av1Mp4 => 0.5,
            _ => 1.0
        };

        // Calculate final bitrate
        double finalMbps = baseMbps * resolutionScale * frameRateScale * codecFactor;

        // Clamp to reasonable range (1 Mbps to 200 Mbps)
        finalMbps = Math.Clamp(finalMbps, 1, 200);

        // Convert to bits per second
        return (uint)(finalMbps * 1_000_000);
    }

    public string GetOutputFileName() => GetOutputFileNameForFormat(Format);

    public string GetOutputFileNameForFormat(RecordingFormat format)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        const string extension = "mp4";
        var formatSuffix = format switch
        {
            RecordingFormat.H264Mp4 => "H264",
            RecordingFormat.HevcMp4 => "HEVC",
            RecordingFormat.Av1Mp4 => "AV1",
            _ => "VIDEO"
        };
        return $"Capture_{timestamp}_{formatSuffix}.{extension}";
    }

    public string GetFullOutputPath() => Path.Combine(OutputPath, GetOutputFileName());
}

public static class NvencPresetParser
{
    public static NvencPreset Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return NvencPreset.Auto;
        return Enum.TryParse<NvencPreset>(value, ignoreCase: true, out var result) ? result : NvencPreset.Auto;
    }
}

public static class SplitEncodeModeParser
{
    public static SplitEncodeMode Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return SplitEncodeMode.Auto;
        // Handle hyphenated wire values from UI/automation
        if (string.Equals(value, "2-way", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "2", StringComparison.OrdinalIgnoreCase))
            return SplitEncodeMode.TwoWay;
        if (string.Equals(value, "3-way", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "3", StringComparison.OrdinalIgnoreCase))
            return SplitEncodeMode.ThreeWay;
        return Enum.TryParse<SplitEncodeMode>(value, ignoreCase: true, out var result) ? result : SplitEncodeMode.Auto;
    }

    /// <summary>Returns the wire/UI string for a SplitEncodeMode value.</summary>
    public static string ToWireString(SplitEncodeMode mode) => mode switch
    {
        SplitEncodeMode.TwoWay => "2-way",
        SplitEncodeMode.ThreeWay => "3-way",
        _ => mode.ToString()
    };
}

public sealed record SplitEncodeSupport(bool Supports2Way, bool Supports3Way)
{
    public static SplitEncodeSupport NvencUnavailable { get; } = new(false, false);
}

// Capture device option returned by Media Foundation enumeration.
public class CaptureDevice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NativeXuInterfacePath { get; set; }
    public string? AudioDeviceId { get; set; }
    public string? AudioDeviceName { get; set; }
    public bool IsHdrCapable { get; set; }
    public ObservableCollection<MediaFormat> SupportedFormats { get; set; } = new();

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? "Unknown Device" : Name;

    public override string ToString() => DisplayName;
}

// Resolution choice shown in the settings shelf.
public sealed class ResolutionOption
{
    public required string Value { get; init; }
    public required uint Width { get; init; }
    public required uint Height { get; init; }
    public bool IsEnabled { get; init; }
    public string DisableReason { get; init; } = string.Empty;
    public string? DisplayTextOverride { get; init; }
    public string DisplayText => string.IsNullOrWhiteSpace(DisplayTextOverride) ? Value : DisplayTextOverride;
}

// Frame-rate choice shown in the settings shelf. FriendlyValue is the rounded
// UI bucket, while Value/Rational carry the exact capture timing.
public sealed class FrameRateOption
{
    public required double FriendlyValue { get; init; }
    public required double Value { get; init; }
    public string Rational { get; init; } = string.Empty;
    public uint? Numerator { get; init; }
    public uint? Denominator { get; init; }
    public bool IsEnabled { get; init; }
    public string DisableReason { get; init; } = string.Empty;
    public string? DisplayTextOverride { get; init; }
    public string DisplayText => string.IsNullOrWhiteSpace(DisplayTextOverride)
        ? $"{Math.Round(FriendlyValue):0}"
        : DisplayTextOverride;
}

// Coarse capture lifecycle state surfaced to UI and automation snapshots.
public enum CaptureSessionState
{
    Uninitialized,
    Initializing,
    Ready,
    Previewing,
    Recording,
    CleaningUp,
    Faulted,
    Disposed
}

/// <summary>
/// Pure lifecycle rules for capture session state transitions. Resource
/// acquisition and release still belong to CaptureService; this type only
/// defines which high-level states may be entered.
/// </summary>
public static class CaptureSessionTransitionPolicy
{
    public static bool CanEnterTransition(
        CaptureSessionState currentState,
        CaptureSessionState transitionState)
    {
        if (currentState == CaptureSessionState.Disposed)
        {
            return false;
        }

        if (currentState == transitionState)
        {
            return true;
        }

        if (currentState == CaptureSessionState.CleaningUp)
        {
            return transitionState == CaptureSessionState.CleaningUp;
        }

        return transitionState switch
        {
            CaptureSessionState.Initializing => true,
            CaptureSessionState.Ready => true,
            CaptureSessionState.Previewing => true,
            CaptureSessionState.Recording => currentState is CaptureSessionState.Ready or CaptureSessionState.Previewing or CaptureSessionState.Recording,
            CaptureSessionState.CleaningUp => true,
            CaptureSessionState.Uninitialized => false,
            CaptureSessionState.Faulted => false,
            CaptureSessionState.Disposed => false,
            _ => false
        };
    }

    public static void ThrowIfDisallowed(
        CaptureSessionState currentState,
        CaptureSessionState transitionState)
    {
        if (CanEnterTransition(currentState, transitionState))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Capture session transition is not allowed: {currentState} -> {transitionState}.");
    }

    public static CaptureSessionState ResolveSteadyState(
        bool isDisposed,
        bool isRecording,
        bool isVideoPreviewActive,
        bool isAudioPreviewActive,
        bool isInitialized)
    {
        if (isDisposed) return CaptureSessionState.Disposed;
        if (isRecording) return CaptureSessionState.Recording;
        if (isVideoPreviewActive || isAudioPreviewActive) return CaptureSessionState.Previewing;
        return isInitialized ? CaptureSessionState.Ready : CaptureSessionState.Uninitialized;
    }
}

internal readonly record struct CaptureSessionSteadyStateInputs(
    bool IsDisposed,
    bool IsRecording,
    bool IsVideoPreviewActive,
    bool IsAudioPreviewActive,
    bool IsInitialized);

internal sealed class CaptureSessionStateMachine
{
    private CaptureSessionState _state = CaptureSessionState.Uninitialized;
    private long _generation;

    public CaptureSessionState State => _state;

    public long Generation => Interlocked.Read(ref _generation);

    public void EnterTransition(CaptureSessionState transitionState)
    {
        CaptureSessionTransitionPolicy.ThrowIfDisallowed(_state, transitionState);
        Interlocked.Increment(ref _generation);
        _state = transitionState;
    }

    public void ResolveSteadyState(CaptureSessionSteadyStateInputs inputs)
        => _state = CaptureSessionTransitionPolicy.ResolveSteadyState(
            inputs.IsDisposed,
            inputs.IsRecording,
            inputs.IsVideoPreviewActive,
            inputs.IsAudioPreviewActive,
            inputs.IsInitialized);

    public void EnterCleanup()
        => _state = CaptureSessionState.CleaningUp;

    public void EnterFaulted()
        => _state = CaptureSessionState.Faulted;

    public void EnterDisposed()
        => _state = CaptureSessionState.Disposed;

    public void ResetAfterCleanup(bool isDisposed)
        => _state = isDisposed ? CaptureSessionState.Disposed : CaptureSessionState.Uninitialized;
}

// Frame-ledger stages name the major handoff points a source frame can cross.
public enum FrameLedgerStage
{
    CaptureArrived,
    CompressedQueued,
    DecodeStarted,
    DecodeFinished,
    StrictOrderReleased,
    RecordingEnqueued,
    EncoderSubmitted,
    EncoderAccepted,
    EncoderPacketWritten,
    FlashbackEnqueued,
    PreviewEnqueued,
    PreviewSelected,
    GpuUploadStarted,
    GpuUploadFinished,
    RenderSubmitted,
    PresentCalled,
    PresentMonPresentSeen,
    PresentMonDisplayedOrSuperseded
}

// Immutable identity carried with a frame through capture/preview/recording.
public readonly record struct FrameIdentity(
    long SourceSequence,
    long CaptureArrivalQpc,
    long? DeviceTimestamp100ns,
    string InputFormat,
    int Width,
    int Height,
    double FrameRateNominal,
    int CompressedByteLength);

// Public snapshot DTO for one retained ledger event.
public sealed record FrameLedgerEventSnapshot(
    long SourceSequence,
    FrameLedgerStage Stage,
    long QpcTimestamp,
    string Subsystem,
    int? QueueDepth,
    long? ByteDepth,
    bool? Accepted,
    string? Reason,
    FrameIdentity? Identity);

// Bounded ledger summary returned in diagnostics snapshots.
public sealed record FrameLedgerSummary(
    int Capacity,
    long TotalEventsRecorded,
    long EventsDroppedByRetention,
    int RecentEventCount,
    long? OldestSourceSequence,
    long? NewestSourceSequence,
    FrameLedgerEventSnapshot[] RecentEvents)
{
    public static FrameLedgerSummary Empty { get; } = new(
        Capacity: 0,
        TotalEventsRecorded: 0,
        EventsDroppedByRetention: 0,
        RecentEventCount: 0,
        OldestSourceSequence: null,
        NewestSourceSequence: null,
        RecentEvents: Array.Empty<FrameLedgerEventSnapshot>());
}

/// <summary>
/// Core capture diagnostics shared by both the lightweight diagnostics snapshot
/// and the full health snapshot.  CaptureHealthSnapshot extends this with
/// flashback playback/encoder detail, source signal metadata, and AV-sync data.
/// </summary>
public class CaptureDiagnosticsSnapshot
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public long CaptureSessionEpoch { get; init; }
    public long SourceTelemetryEpoch { get; init; }
    public CaptureSessionState SessionState { get; init; }
    public bool IsRecording { get; init; }
    public string RecordingBackend { get; init; } = "None";
    public string AudioPathMode { get; init; } = "None";
    public string MuxResult { get; init; } = "NotAttempted";
    public long RecordingElapsedMs { get; init; }
    public long LastFrameArrivalMs { get; init; }
    public long EstimatedPipelineLatencyMs { get; init; }
    public double ExpectedFrameRate { get; init; }
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
    public long ObservedP010FrameCount { get; init; }
    public long ObservedNv12FrameCount { get; init; }
    public long ObservedOtherFrameCount { get; init; }
    public bool HdrAutoDowngraded { get; init; }
    public string HdrAutoDowngradeReason { get; init; } = string.Empty;

    // Source telemetry diagnostics
    public SourceTelemetryAvailability SourceTelemetryAvailability { get; init; } = SourceTelemetryAvailability.Unknown;
    public SourceTelemetryOrigin SourceTelemetryOrigin { get; init; } = SourceTelemetryOrigin.Unknown;
    public SourceTelemetryConfidence SourceTelemetryConfidence { get; init; } = SourceTelemetryConfidence.Unknown;
    public string SourceTelemetryOriginDetail { get; init; } = "Unknown";
    public string? SourceTelemetryDiagnosticSummary { get; init; }
    public DateTimeOffset? SourceTelemetryTimestampUtc { get; init; }
    public string SourceTelemetryBackend { get; init; } = "Unknown";
    public bool SourceTelemetrySuppressed { get; init; }
    public string? SourceTelemetrySuppressedReason { get; init; }
    public string SourceTelemetryCircuitState { get; init; } = "Closed";
    public int? SourceWidth { get; init; }
    public int? SourceHeight { get; init; }
    public double? SourceFrameRateExact { get; init; }
    public string? SourceFrameRateArg { get; init; }
    public bool? SourceIsHdr { get; init; }

    // Capture cadence diagnostics
    public int CaptureCadenceSampleCount { get; init; }
    public double CaptureCadenceObservedFps { get; init; }
    public double CaptureCadenceExpectedIntervalMs { get; init; }
    public double CaptureCadenceAverageIntervalMs { get; init; }
    public double CaptureCadenceP95IntervalMs { get; init; }
    public double CaptureCadenceP99IntervalMs { get; init; }
    public double CaptureCadenceMaxIntervalMs { get; init; }
    public double CaptureCadenceOnePercentLowFps { get; init; }
    public double CaptureCadenceFivePercentLowFps { get; init; }
    public double CaptureCadenceSampleDurationMs { get; init; }
    public double[] CaptureCadenceRecentIntervalsMs { get; init; } = Array.Empty<double>();
    public double CaptureCadenceJitterStdDevMs { get; init; }
    public long CaptureCadenceSevereGapCount { get; init; }
    public long CaptureCadenceEstimatedDroppedFrames { get; init; }
    public double CaptureCadenceEstimatedDropPercent { get; init; }

    // Recording diagnostics
    public int ConversionQueueDepth { get; init; }
    public int FfmpegVideoQueueDepth { get; init; }
    public int FfmpegAudioQueueDepth { get; init; }
    public long VideoFramesArrived { get; init; }
    public long VideoFramesQueued { get; init; }
    public long VideoFramesDropped { get; init; }
    public long VideoFramesDroppedBacklog { get; init; }
    public long VideoFramesConverted { get; init; }
    public long VideoFramesEnqueued { get; init; }
    public long VideoDropsQueueSaturated { get; init; }
    public long VideoDropsBacklogEviction { get; init; }
    public bool RecordingEncodingFailed { get; init; }
    public string? RecordingEncodingFailureType { get; init; }
    public string? RecordingEncodingFailureMessage { get; init; }
    public int RecordingVideoQueueCapacity { get; init; }
    public int RecordingVideoQueueMaxDepth { get; init; }
    public long RecordingVideoFramesSubmittedToEncoder { get; init; }
    public long RecordingVideoEncoderPts { get; init; }
    public long RecordingVideoEncoderPacketsWritten { get; init; }
    public long RecordingVideoEncoderDroppedFrames { get; init; }
    public long RecordingVideoSequenceGaps { get; init; }
    public long RecordingVideoQueueOldestFrameAgeMs { get; init; }
    public long RecordingVideoQueueLastLatencyMs { get; init; }
    public int RecordingVideoQueueLatencySampleCount { get; init; }
    public double RecordingVideoQueueLatencyAvgMs { get; init; }
    public double RecordingVideoQueueLatencyP95Ms { get; init; }
    public double RecordingVideoQueueLatencyP99Ms { get; init; }
    public double RecordingVideoQueueLatencyMaxMs { get; init; }
    public long RecordingVideoBackpressureWaitMs { get; init; }
    public long RecordingVideoBackpressureEvents { get; init; }
    public long RecordingVideoBackpressureLastWaitMs { get; init; }
    public long RecordingVideoBackpressureMaxWaitMs { get; init; }
    public int RecordingGpuQueueDepth { get; init; }
    public int RecordingGpuQueueCapacity { get; init; }
    public int RecordingGpuQueueMaxDepth { get; init; }
    public long RecordingGpuFramesEnqueued { get; init; }
    public long RecordingGpuFramesDropped { get; init; }
    public int RecordingCudaQueueDepth { get; init; }
    public int RecordingCudaQueueCapacity { get; init; }
    public int RecordingCudaQueueMaxDepth { get; init; }
    public long RecordingCudaFramesEnqueued { get; init; }
    public long RecordingCudaFramesDropped { get; init; }
    public long AudioDropsQueueSaturated { get; init; }
    public long AudioDropsBacklogEviction { get; init; }
    public long AudioChunksDropped { get; init; }

    // Flashback diagnostics
    public bool FlashbackActive { get; init; }
    public long FlashbackBufferedDurationMs { get; init; }
    public int FlashbackSegmentCount { get; init; }
    public long FlashbackDiskBytes { get; init; }
    public long FlashbackTotalBytesWritten { get; init; }
    public long FlashbackTempDriveFreeBytes { get; init; }
    public long FlashbackStartupCacheBudgetBytes { get; init; }
    public long FlashbackStartupCacheBytes { get; init; }
    public int FlashbackStartupCacheSessionCount { get; init; }
    public int FlashbackStartupCacheDeletedSessionCount { get; init; }
    public long FlashbackStartupCacheFreedBytes { get; init; }
    public bool FlashbackStartupCacheOverBudget { get; init; }
    public bool FlashbackEncodingFailed { get; init; }
    public string? FlashbackEncodingFailureType { get; init; }
    public string? FlashbackEncodingFailureMessage { get; init; }
    public bool FatalCleanupInProgress { get; init; }
    public bool FlashbackCleanupInProgress { get; init; }
    public bool FlashbackForceRotateActive { get; init; }
    public bool FlashbackForceRotateRequested { get; init; }
    public bool FlashbackForceRotateDraining { get; init; }
    public int FlashbackVideoQueueCapacity { get; init; }
    public int FlashbackVideoQueueMaxDepth { get; init; }
    public long FlashbackVideoFramesSubmittedToEncoder { get; init; }
    public long FlashbackVideoEncoderPts { get; init; }
    public long FlashbackVideoEncoderPacketsWritten { get; init; }
    public long FlashbackVideoEncoderDroppedFrames { get; init; }
    public long FlashbackVideoSequenceGaps { get; init; }
    public long FlashbackVideoQueueRejectedFrames { get; init; }
    public string FlashbackVideoQueueLastRejectReason { get; init; } = string.Empty;
    public long FlashbackVideoQueueOldestFrameAgeMs { get; init; }
    public long FlashbackVideoQueueLastLatencyMs { get; init; }
    public int FlashbackVideoQueueLatencySampleCount { get; init; }
    public double FlashbackVideoQueueLatencyAvgMs { get; init; }
    public double FlashbackVideoQueueLatencyP95Ms { get; init; }
    public double FlashbackVideoQueueLatencyP99Ms { get; init; }
    public double FlashbackVideoQueueLatencyMaxMs { get; init; }
    public long FlashbackVideoBackpressureWaitMs { get; init; }
    public long FlashbackVideoBackpressureEvents { get; init; }
    public long FlashbackVideoBackpressureLastWaitMs { get; init; }
    public long FlashbackVideoBackpressureMaxWaitMs { get; init; }
    public int FlashbackGpuQueueDepth { get; init; }
    public int FlashbackGpuQueueCapacity { get; init; }
    public int FlashbackGpuQueueMaxDepth { get; init; }
    public long FlashbackGpuFramesEnqueued { get; init; }
    public long FlashbackGpuFramesDropped { get; init; }
    public long FlashbackGpuQueueRejectedFrames { get; init; }
    public string FlashbackGpuQueueLastRejectReason { get; init; } = string.Empty;

    // MJPEG and visual cadence diagnostics
    public int MjpegDecodeSampleCount { get; init; }
    public double MjpegDecodeAvgMs { get; init; }
    public double MjpegDecodeP95Ms { get; init; }
    public double MjpegDecodeMaxMs { get; init; }
    public int MjpegInteropCopySampleCount { get; init; }
    public double MjpegInteropCopyAvgMs { get; init; }
    public double MjpegInteropCopyP95Ms { get; init; }
    public double MjpegInteropCopyMaxMs { get; init; }
    public int MjpegCallbackSampleCount { get; init; }
    public double MjpegCallbackAvgMs { get; init; }
    public double MjpegCallbackP95Ms { get; init; }
    public double MjpegCallbackMaxMs { get; init; }
    public int MjpegDecoderCount { get; init; }
    public int MjpegReorderSampleCount { get; init; }
    public double MjpegReorderAvgMs { get; init; }
    public double MjpegReorderP95Ms { get; init; }
    public double MjpegReorderMaxMs { get; init; }
    public int MjpegPipelineSampleCount { get; init; }
    public double MjpegPipelineAvgMs { get; init; }
    public double MjpegPipelineP95Ms { get; init; }
    public double MjpegPipelineMaxMs { get; init; }
    public long MjpegTotalDecoded { get; init; }
    public long MjpegTotalEmitted { get; init; }
    public long MjpegTotalDropped { get; init; }
    public long MjpegCompressedFramesQueued { get; init; }
    public long MjpegCompressedFramesDequeued { get; init; }
    public long MjpegCompressedDropsQueueFull { get; init; }
    public long MjpegCompressedDropsByteBudget { get; init; }
    public long MjpegCompressedDropsDisposed { get; init; }
    public long MjpegDecodeFailures { get; init; }
    public long MjpegReorderCollisions { get; init; }
    public long MjpegEmitFailures { get; init; }
    public int MjpegCompressedQueueDepth { get; init; }
    public long MjpegCompressedQueueBytes { get; init; }
    public long MjpegCompressedQueueByteBudget { get; init; }
    public long MjpegReorderSkips { get; init; }
    public int MjpegReorderBufferDepth { get; init; }
    public bool MjpegPreviewJitterEnabled { get; init; }
    public int MjpegPreviewJitterTargetDepth { get; init; }
    public int MjpegPreviewJitterMaxDepth { get; init; }
    public int MjpegPreviewJitterQueueDepth { get; init; }
    public long MjpegPreviewJitterTotalQueued { get; init; }
    public long MjpegPreviewJitterTotalSubmitted { get; init; }
    public long MjpegPreviewJitterTotalDropped { get; init; }
    public long MjpegPreviewJitterUnderflowCount { get; init; }
    public long MjpegPreviewJitterResumeReprimeCount { get; init; }
    public int MjpegPreviewJitterInputSampleCount { get; init; }
    public double MjpegPreviewJitterInputAvgMs { get; init; }
    public double MjpegPreviewJitterInputP95Ms { get; init; }
    public double MjpegPreviewJitterInputMaxMs { get; init; }
    public int MjpegPreviewJitterOutputSampleCount { get; init; }
    public double MjpegPreviewJitterOutputAvgMs { get; init; }
    public double MjpegPreviewJitterOutputP95Ms { get; init; }
    public double MjpegPreviewJitterOutputMaxMs { get; init; }
    public int MjpegPreviewJitterLatencySampleCount { get; init; }
    public double MjpegPreviewJitterLatencyAvgMs { get; init; }
    public double MjpegPreviewJitterLatencyP95Ms { get; init; }
    public double MjpegPreviewJitterLatencyMaxMs { get; init; }
    public long MjpegPreviewJitterDeadlineDropCount { get; init; }
    public long MjpegPreviewJitterClearedDropCount { get; init; }
    public long MjpegPreviewJitterTargetIncreaseCount { get; init; }
    public long MjpegPreviewJitterTargetDecreaseCount { get; init; }
    public long MjpegPreviewJitterLastSelectedPreviewPresentId { get; init; }
    public long MjpegPreviewJitterLastSelectedSourceSequenceNumber { get; init; }
    public long MjpegPreviewJitterLastSelectedQpc { get; init; }
    public double MjpegPreviewJitterLastSelectedSourceLatencyMs { get; init; }
    public long MjpegPreviewJitterLastDroppedSourceSequenceNumber { get; init; }
    public long MjpegPreviewJitterLastDropQpc { get; init; }
    public string MjpegPreviewJitterLastDropReason { get; init; } = string.Empty;
    public long MjpegPreviewJitterLastUnderflowQpc { get; init; }
    public string MjpegPreviewJitterLastUnderflowReason { get; init; } = string.Empty;
    public int MjpegPreviewJitterLastUnderflowQueueDepth { get; init; }
    public double MjpegPreviewJitterLastUnderflowInputAgeMs { get; init; }
    public double MjpegPreviewJitterLastUnderflowOutputAgeMs { get; init; }
    public double MjpegPreviewJitterLastScheduleLateMs { get; init; }
    public double MjpegPreviewJitterMaxScheduleLateMs { get; init; }
    public long MjpegPreviewJitterScheduleLateCount { get; init; }
    public int MjpegPacketHashSampleCount { get; init; }
    public long MjpegPacketHashUniqueFrameCount { get; init; }
    public long MjpegPacketHashDuplicateFrameCount { get; init; }
    public long MjpegPacketHashLongestDuplicateRun { get; init; }
    public double MjpegPacketHashInputObservedFps { get; init; }
    public double MjpegPacketHashUniqueObservedFps { get; init; }
    public double MjpegPacketHashDuplicateFramePercent { get; init; }
    public string MjpegPacketHashLastHash { get; init; } = string.Empty;
    public bool MjpegPacketHashLastFrameDuplicate { get; init; }
    public string MjpegPacketHashPattern { get; init; } = "NoSamples";
    public double[] MjpegPacketHashRecentInputIntervalsMs { get; init; } = Array.Empty<double>();
    public double[] MjpegPacketHashRecentUniqueIntervalsMs { get; init; } = Array.Empty<double>();
    public int[] MjpegPacketHashRecentDuplicateFlags { get; init; } = Array.Empty<int>();
    public int VisualCadenceSampleCount { get; init; }
    public long VisualCadenceChangedFrameCount { get; init; }
    public long VisualCadenceRepeatFrameCount { get; init; }
    public long VisualCadenceLongestRepeatRun { get; init; }
    public double VisualCadenceOutputObservedFps { get; init; }
    public double VisualCadenceChangeObservedFps { get; init; }
    public double VisualCadenceRepeatFramePercent { get; init; }
    public double VisualCadenceLastDelta { get; init; }
    public double VisualCadenceAverageDelta { get; init; }
    public double VisualCadenceP95Delta { get; init; }
    public double VisualCadenceMotionScore { get; init; }
    public string VisualCadenceMotionConfidence { get; init; } = "NoSamples";
    public double[] VisualCadenceRecentOutputIntervalsMs { get; init; } = Array.Empty<double>();
    public double[] VisualCadenceRecentChangeIntervalsMs { get; init; } = Array.Empty<double>();
    public int VisualCenterCadenceSampleCount { get; init; }
    public long VisualCenterCadenceChangedFrameCount { get; init; }
    public long VisualCenterCadenceRepeatFrameCount { get; init; }
    public long VisualCenterCadenceLongestRepeatRun { get; init; }
    public double VisualCenterCadenceOutputObservedFps { get; init; }
    public double VisualCenterCadenceChangeObservedFps { get; init; }
    public double VisualCenterCadenceRepeatFramePercent { get; init; }
    public double VisualCenterCadenceLastDelta { get; init; }
    public double VisualCenterCadenceAverageDelta { get; init; }
    public double VisualCenterCadenceP95Delta { get; init; }
    public double VisualCenterCadenceMotionScore { get; init; }
    public string VisualCenterCadenceMotionConfidence { get; init; } = "NoSamples";
    public double[] VisualCenterCadenceRecentOutputIntervalsMs { get; init; } = Array.Empty<double>();
    public double[] VisualCenterCadenceRecentChangeIntervalsMs { get; init; } = Array.Empty<double>();
    public MjpegDecoderHealthSnapshot[] MjpegPerDecoder { get; init; } = Array.Empty<MjpegDecoderHealthSnapshot>();
}

public sealed record MjpegDecoderHealthSnapshot(
    int WorkerIndex,
    int SampleCount,
    double AvgMs,
    double P95Ms,
    double MaxMs);

/// <summary>
/// Full health snapshot extending <see cref="CaptureDiagnosticsSnapshot"/> with
/// flashback playback/encoder detail, source signal metadata, and AV-sync data.
/// </summary>
public sealed class CaptureHealthSnapshot : CaptureDiagnosticsSnapshot
{
    // Source signal extended metadata
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
    public IReadOnlyList<SourceTelemetryDetailEntry> SourceTelemetryDetails { get; init; } = Array.Empty<SourceTelemetryDetailEntry>();

    // Queue age / enqueue tracking
    public long LastVideoEnqueueAgeMs { get; init; }
    public long LastVideoWriteAgeMs { get; init; }

    // AV sync diagnostics
    public double? AvSyncCaptureDriftMs { get; init; }
    public double? AvSyncCaptureDriftRateMsPerSec { get; init; }
    public double? AvSyncEncoderDriftMs { get; init; }
    public long? AvSyncEncoderCorrectionSamples { get; init; }

    // Flashback backend diagnostics
    public long FlashbackOutputBytes { get; init; }
    public string? FlashbackFilePath { get; init; }
    public long FlashbackEncodedFrames { get; init; }
    public long FlashbackDroppedFrames { get; init; }
    public bool FlashbackGpuEncoding { get; init; }
    public bool FlashbackBackendSettingsStale { get; init; }
    public string FlashbackBackendSettingsStaleReason { get; init; } = string.Empty;
    public string FlashbackBackendActiveFormat { get; init; } = string.Empty;
    public string FlashbackBackendRequestedFormat { get; init; } = string.Empty;
    public string FlashbackBackendActivePreset { get; init; } = string.Empty;
    public string FlashbackBackendRequestedPreset { get; init; } = string.Empty;
    public string? EncoderCodecName { get; init; }
    public uint EncoderTargetBitRate { get; init; }
    public int EncoderWidth { get; init; }
    public int EncoderHeight { get; init; }
    public double EncoderFrameRate { get; init; }
    public int? EncoderFrameRateNumerator { get; init; }
    public int? EncoderFrameRateDenominator { get; init; }
    public int FlashbackVideoQueueDepth { get; init; }
    public int FlashbackAudioQueueDepth { get; init; }
    public int FlashbackAudioQueueCapacity { get; init; }

    // Flashback export diagnostics
    public bool FlashbackExportActive { get; init; }
    public long FlashbackExportId { get; init; }
    public string FlashbackExportStatus { get; init; } = "NotStarted";
    public string FlashbackExportOutputPath { get; init; } = string.Empty;
    public long FlashbackExportStartedUtcUnixMs { get; init; }
    public long FlashbackExportLastProgressUtcUnixMs { get; init; }
    public long FlashbackExportCompletedUtcUnixMs { get; init; }
    public long FlashbackExportElapsedMs { get; init; }
    public long FlashbackExportLastProgressAgeMs { get; init; }
    public long FlashbackExportOutputBytes { get; init; }
    public double FlashbackExportThroughputBytesPerSec { get; init; }
    public int FlashbackExportSegmentsProcessed { get; init; }
    public int FlashbackExportTotalSegments { get; init; }
    public double FlashbackExportPercent { get; init; }
    public long FlashbackExportInPointMs { get; init; }
    public long FlashbackExportOutPointMs { get; init; }
    public string FlashbackExportMessage { get; init; } = string.Empty;
    public string FlashbackExportFailureKind { get; init; } = string.Empty;
    public long FlashbackExportForceRotateFallbacks { get; init; }
    public long FlashbackExportLastForceRotateFallbackUtcUnixMs { get; init; }
    public int FlashbackExportLastForceRotateFallbackSegments { get; init; }
    public long FlashbackExportLastForceRotateFallbackInPointMs { get; init; }
    public long FlashbackExportLastForceRotateFallbackOutPointMs { get; init; }

    /// <summary>
    /// The actual codec/container the next flashback export will produce. This
    /// should match the user-requested <c>SelectedRecordingFormat</c>; mismatches
    /// are reserved for future explicit, user-visible substitutions.
    /// </summary>
    public string? FlashbackExportVerificationFormat { get; init; }

    /// <summary>
    /// Legacy compatibility field for recording settings substitutions. It should
    /// remain null while Flashback honors the selected codec and preset directly.
    /// </summary>
    public string? FlashbackCodecDowngradeReason { get; init; }

    public long LastExportId { get; init; }
    public string? LastExportPath { get; init; }
    public bool? LastExportSuccess { get; init; }
    public string? LastExportMessage { get; init; }

    // Flashback playback diagnostics
    public string FlashbackPlaybackState { get; init; } = "N/A";
    public long FlashbackPlaybackPositionMs { get; init; }
    public string FlashbackDecoderHwAccel { get; init; } = "N/A";
    public long FlashbackPlaybackFrameCount { get; init; }
    public long FlashbackPlaybackLateFrames { get; init; }
    public long FlashbackPlaybackDroppedFrames { get; init; }
    public long FlashbackPlaybackAudioMasterDelayDoubles { get; init; }
    public long FlashbackPlaybackAudioMasterDelayShrinks { get; init; }
    public long FlashbackPlaybackAudioMasterFallbacks { get; init; }
    public long FlashbackPlaybackAudioMasterUnavailableFallbacks { get; init; }
    public long FlashbackPlaybackAudioMasterStaleFallbacks { get; init; }
    public long FlashbackPlaybackAudioMasterDriftOutlierFallbacks { get; init; }
    public string FlashbackPlaybackAudioMasterLastFallbackReason { get; init; } = string.Empty;
    public double FlashbackPlaybackAudioMasterLastFallbackDriftMs { get; init; }
    public double FlashbackPlaybackAudioMasterLastFallbackClockAgeMs { get; init; }
    public long FlashbackPlaybackSegmentSwitches { get; init; }
    public long FlashbackPlaybackFmp4Reopens { get; init; }
    public long FlashbackPlaybackWriteHeadWaits { get; init; }
    public long FlashbackPlaybackNearLiveSnaps { get; init; }
    public long FlashbackPlaybackDecodeErrorSnaps { get; init; }
    public long FlashbackPlaybackSubmitFailures { get; init; }
    public long FlashbackPlaybackLastDropUtcUnixMs { get; init; }
    public string FlashbackPlaybackLastDropReason { get; init; } = string.Empty;
    public long FlashbackPlaybackLastSubmitFailureUtcUnixMs { get; init; }
    public string FlashbackPlaybackLastSubmitFailure { get; init; } = string.Empty;
    public long FlashbackPlaybackLastSegmentSwitchUtcUnixMs { get; init; }
    public long FlashbackPlaybackLastFmp4ReopenUtcUnixMs { get; init; }
    public long FlashbackPlaybackLastWriteHeadWaitGapMs { get; init; }
    public double FlashbackPlaybackTargetFps { get; init; }
    public double FlashbackPlaybackObservedFps { get; init; }
    public double FlashbackPlaybackAvgFrameMs { get; init; }
    public int FlashbackPlaybackCadenceSampleCount { get; init; }
    public double FlashbackPlaybackP95FrameMs { get; init; }
    public double FlashbackPlaybackP99FrameMs { get; init; }
    public double FlashbackPlaybackMaxFrameMs { get; init; }
    public long FlashbackPlaybackSlowFrames { get; init; }
    public double FlashbackPlaybackSlowFramePercent { get; init; }
    public double FlashbackPlaybackOnePercentLowFps { get; init; }
    public double FlashbackPlaybackFivePercentLowFps { get; init; }
    public double FlashbackPlaybackSampleDurationMs { get; init; }
    public double[] FlashbackPlaybackRecentFrameIntervalsMs { get; init; } = Array.Empty<double>();
    public long FlashbackPlaybackPtsCadenceMismatchCount { get; init; }
    public long FlashbackPlaybackLastPtsCadenceMismatchUtcUnixMs { get; init; }
    public double FlashbackPlaybackLastPtsCadenceDeltaMs { get; init; }
    public double FlashbackPlaybackLastPtsCadenceExpectedMs { get; init; }
    public long FlashbackPlaybackSeekForwardDecodeCapHits { get; init; }
    public bool FlashbackPlaybackLastSeekHitForwardDecodeCap { get; init; }
    public int FlashbackPlaybackDecodeSampleCount { get; init; }
    public double FlashbackPlaybackDecodeAvgMs { get; init; }
    public double FlashbackPlaybackDecodeP95Ms { get; init; }
    public double FlashbackPlaybackDecodeP99Ms { get; init; }
    public double FlashbackPlaybackDecodeMaxMs { get; init; }
    public string FlashbackPlaybackMaxDecodePhase { get; init; } = string.Empty;
    public double FlashbackPlaybackMaxDecodeReceiveMs { get; init; }
    public double FlashbackPlaybackMaxDecodeFeedMs { get; init; }
    public double FlashbackPlaybackMaxDecodeReadMs { get; init; }
    public double FlashbackPlaybackMaxDecodeSendMs { get; init; }
    public double FlashbackPlaybackMaxDecodeAudioMs { get; init; }
    public double FlashbackPlaybackMaxDecodeConvertMs { get; init; }
    public long FlashbackPlaybackMaxDecodeUtcUnixMs { get; init; }
    public long FlashbackPlaybackMaxDecodePositionMs { get; init; }
    public double FlashbackAvDriftMs { get; init; }
    public bool FlashbackPlaybackThreadAlive { get; init; }
    public long FlashbackPlaybackCommandsEnqueued { get; init; }
    public long FlashbackPlaybackCommandsProcessed { get; init; }
    public long FlashbackPlaybackCommandsDropped { get; init; }
    public long FlashbackPlaybackCommandsSkippedNotReady { get; init; }
    public long FlashbackPlaybackScrubUpdatesCoalesced { get; init; }
    public long FlashbackPlaybackSeekCommandsCoalesced { get; init; }
    public int FlashbackPlaybackCommandQueueCapacity { get; init; }
    public int FlashbackPlaybackPendingCommands { get; init; }
    public int FlashbackPlaybackMaxPendingCommands { get; init; }
    public long FlashbackPlaybackLastCommandQueueLatencyMs { get; init; }
    public long FlashbackPlaybackMaxCommandQueueLatencyMs { get; init; }
    public string FlashbackPlaybackMaxCommandQueueLatencyCommand { get; init; } = "None";
    public string FlashbackPlaybackLastCommandQueued { get; init; } = "None";
    public string FlashbackPlaybackLastCommandProcessed { get; init; } = "None";
    public long FlashbackPlaybackLastCommandQueuedUtcUnixMs { get; init; }
    public long FlashbackPlaybackLastCommandProcessedUtcUnixMs { get; init; }
    public long FlashbackPlaybackLastCommandFailureUtcUnixMs { get; init; }
    public string FlashbackPlaybackLastCommandFailure { get; init; } = string.Empty;
}
