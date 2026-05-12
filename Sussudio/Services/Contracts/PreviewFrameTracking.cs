namespace Sussudio.Services.Contracts;

// Bundles the per-frame tracking metadata that every IPreviewFrameSink.Submit*
// overload accepts. Collapses the prior 6-parameter trailing block (which had
// drifted into different orderings between SubmitRawFrame and SubmitTexture)
// into a single value with a stable field order. SourceSequenceNumber=-1 and
// CountForPresentCadence=true match the old default-argument behavior; use
// PreviewFrameTracking.Default as the starting point.
public readonly record struct PreviewFrameTracking(
    long ArrivalTick,
    long SourceSequenceNumber,
    long PreviewPresentId,
    long SchedulerSubmitTick,
    long SourcePtsTicks,
    bool CountForPresentCadence)
{
    public static PreviewFrameTracking Default { get; } = new(
        ArrivalTick: 0,
        SourceSequenceNumber: -1,
        PreviewPresentId: 0,
        SchedulerSubmitTick: 0,
        SourcePtsTicks: 0,
        CountForPresentCadence: true);

    public PreviewFrameTracking WithArrivalTick(long arrivalTick)
        => this with { ArrivalTick = arrivalTick };
}
