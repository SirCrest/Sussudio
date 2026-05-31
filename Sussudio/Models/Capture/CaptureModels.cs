using System;
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
