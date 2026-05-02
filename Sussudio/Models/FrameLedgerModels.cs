using System;

namespace Sussudio.Models;

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

public readonly record struct FrameIdentity(
    long SourceSequence,
    long CaptureArrivalQpc,
    long? DeviceTimestamp100ns,
    string InputFormat,
    int Width,
    int Height,
    double FrameRateNominal,
    int CompressedByteLength);

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
