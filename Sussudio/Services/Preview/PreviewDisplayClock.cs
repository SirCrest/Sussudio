namespace Sussudio.Services.Preview;

internal readonly record struct PreviewDisplayClockSnapshot(
    long LastPresentTick,
    long FrameIntervalTicks,
    double ExpectedFrameIntervalMs,
    int SampleCount);

internal interface IPreviewDisplayClock
{
    bool TryGetDisplayClock(out PreviewDisplayClockSnapshot snapshot);
}

internal interface IPreviewFrameQueueControl
{
    int DropPendingFrames(string reason);
}
