using System;

namespace Sussudio.Models;

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
