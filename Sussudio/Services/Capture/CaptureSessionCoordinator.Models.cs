using System;
using Sussudio.Models;
using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

// Command names are the durable automation-facing lifecycle vocabulary. Keep
// additions explicit so snapshots, diagnostic sessions, and ssctl output can
// explain which transition was queued or failed.
public enum CaptureCommandKind
{
    Initialize,
    StartVideoPreview,
    StopVideoPreview,
    StartRecording,
    StopRecording,
    StartAudioPreview,
    StopAudioPreview,
    UpdateAudioMonitoring,
    UpdateAudioInput,
    UpdateMicrophoneMonitor,
    SetFlashbackEnabled,
    UpdateFlashbackSettings,
    Cleanup,
    RestartFlashback,
    UpdateFlashbackRecordingFormat,
    CycleFlashbackEncoderSettings
}

public enum CaptureCommandOutcome
{
    None,
    Completed,
    Failed,
    Canceled,
    Coalesced
}

// Lightweight queue receipt for a capture transition. CorrelationId is carried
// through diagnostics so automation can match a request to later state changes.
public readonly record struct CaptureCommand(
    CaptureCommandKind Kind,
    string CorrelationId,
    DateTimeOffset EnqueuedAtUtc);

// Thread-safe state projection of the serialized command worker. This snapshot
// intentionally reports queue health as well as CaptureService state, because
// pending/coalesced commands are often the real cause of "the UI is stuck".
public sealed class CaptureSessionSnapshot
{
    public DateTimeOffset LastTransitionUtc { get; init; }
    public CaptureCommandKind? LastCommand { get; init; }
    public string? LastCorrelationId { get; init; }
    public string? LastError { get; init; }
    public long CommandsEnqueued { get; init; }
    public long CommandsCompleted { get; init; }
    public long CommandsFailed { get; init; }
    public long CommandsCanceled { get; init; }
    public long CommandsCoalesced { get; init; }
    public int PendingCommands { get; init; }
    public int MaxPendingCommands { get; init; }
    public long OldestPendingCommandAgeMs { get; init; }
    public long LastCommandQueueLatencyMs { get; init; }
    public long MaxCommandQueueLatencyMs { get; init; }
    public CaptureCommandOutcome LastOutcome { get; init; }
    public CaptureSessionState SessionState { get; init; }
    public bool IsRecording { get; init; }
    public bool IsInitialized { get; init; }
    public bool IsVideoPreviewActive { get; init; }
    public bool IsAudioPreviewActive { get; init; }
}

internal readonly record struct FlashbackPlaybackSnapshot(
    bool IsActive,
    FlashbackPlaybackState State,
    TimeSpan PlaybackPosition,
    TimeSpan GapFromLive,
    TimeSpan? InPoint,
    TimeSpan? OutPoint,
    TimeSpan? InPointFilePts,
    TimeSpan? OutPointFilePts,
    bool ThreadAlive,
    int PendingCommands,
    string LastCommandFailure,
    long LastCommandFailureUtcUnixMs)
{
    public static FlashbackPlaybackSnapshot Disabled { get; } = new(
        false,
        FlashbackPlaybackState.Disabled,
        TimeSpan.Zero,
        TimeSpan.Zero,
        null,
        null,
        null,
        null,
        false,
        0,
        string.Empty,
        0);

    public static FlashbackPlaybackSnapshot Inactive(
        string lastCommandFailure,
        long lastCommandFailureUtcUnixMs) => new(
        false,
        FlashbackPlaybackState.Disabled,
        TimeSpan.Zero,
        TimeSpan.Zero,
        null,
        null,
        null,
        null,
        false,
        0,
        lastCommandFailure,
        lastCommandFailureUtcUnixMs);
}

internal readonly record struct FlashbackBufferStatus(
    bool IsActive,
    TimeSpan BufferDuration,
    TimeSpan FilledDuration,
    long DiskBytes,
    bool IsDiskWarningActive)
{
    public static FlashbackBufferStatus Inactive { get; } = new(
        false,
        TimeSpan.Zero,
        TimeSpan.Zero,
        0,
        false);
}
