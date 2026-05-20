namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private static CaptureCadenceHealthSnapshotFields BuildCaptureCadenceHealthSnapshotFields(
        UnifiedVideoCapture? unifiedVideoCapture)
    {
        var sourceCadence = unifiedVideoCapture?.GetSourceCadenceMetrics()
            ?? default(MfSourceReaderVideoCapture.SourceCadenceMetrics);

        return new CaptureCadenceHealthSnapshotFields(
            sourceCadence.SampleCount,
            sourceCadence.ObservedFps,
            sourceCadence.ExpectedIntervalMs,
            sourceCadence.AverageIntervalMs,
            sourceCadence.P95IntervalMs,
            sourceCadence.P99IntervalMs,
            sourceCadence.MaxIntervalMs,
            sourceCadence.OnePercentLowFps,
            sourceCadence.FivePercentLowFps,
            sourceCadence.SampleDurationMs,
            sourceCadence.RecentIntervalsMs,
            sourceCadence.JitterStdDevMs,
            sourceCadence.SevereGapCount,
            sourceCadence.EstimatedDroppedFrames,
            sourceCadence.EstimatedDropPercent);
    }
}
