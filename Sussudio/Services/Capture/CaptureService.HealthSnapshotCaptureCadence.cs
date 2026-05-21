namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private readonly record struct CaptureCadenceHealthSnapshotFields(
        int SampleCount,
        double ObservedFps,
        double ExpectedIntervalMs,
        double AverageIntervalMs,
        double P95IntervalMs,
        double P99IntervalMs,
        double MaxIntervalMs,
        double OnePercentLowFps,
        double FivePercentLowFps,
        double SampleDurationMs,
        double[] RecentIntervalsMs,
        double JitterStdDevMs,
        long SevereGapCount,
        long EstimatedDroppedFrames,
        double EstimatedDropPercent);

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
