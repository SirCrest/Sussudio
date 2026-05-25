using System;
using System.Collections.ObjectModel;
using System.Threading;

namespace Sussudio.Models;

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
