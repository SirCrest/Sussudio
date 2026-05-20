using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task FrameFingerprintCadenceTracker_CurrentDuplicateRunLowersUniqueFps()
    {
        var trackerSource = ReadRepoFile("Sussudio/Services/Capture/FrameFingerprintCadenceTracker.cs").Replace("\r\n", "\n");
        var trackerMetricsSource = ReadRepoFile("Sussudio/Services/Capture/FrameFingerprintCadenceTracker.Metrics.cs").Replace("\r\n", "\n");
        var tracker = CreateInstance("Sussudio.Services.Capture.FrameFingerprintCadenceTracker");
        var trackerType = tracker.GetType();
        var recordFrame = trackerType.GetMethod("RecordFrame", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FrameFingerprintCadenceTracker.RecordFrame not found.");
        var getMetrics = trackerType.GetMethod("GetMetrics", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FrameFingerprintCadenceTracker.GetMetrics not found.");

        var intervalTicks = Math.Max(1, Stopwatch.Frequency / 120);
        var tick = Stopwatch.Frequency;
        for (ulong hash = 1; hash <= 120; hash++)
        {
            recordFrame.Invoke(tracker, new object?[] { hash, tick });
            tick += intervalTicks;
        }

        var repeatedHash = 120UL;
        for (var i = 0; i < 90; i++)
        {
            recordFrame.Invoke(tracker, new object?[] { repeatedHash, tick });
            tick += intervalTicks;
        }

        var metrics = getMetrics.Invoke(tracker, new object?[] { 180 })
            ?? throw new InvalidOperationException("FrameFingerprintCadenceTracker.GetMetrics returned null.");

        AssertEqual("DuplicateRun", GetStringProperty(metrics, "Pattern"), "packet hash pattern during trailing duplicate run");
        AssertEqual(true, GetBoolProperty(metrics, "LastFrameDuplicate"), "packet hash last-frame duplicate state");

        var duplicatePercent = GetDoubleProperty(metrics, "DuplicateFramePercent");
        if (duplicatePercent < 40)
        {
            throw new InvalidOperationException($"Duplicate percent did not reflect recent duplicate run: {duplicatePercent:0.00}%.");
        }

        var uniqueFps = GetDoubleProperty(metrics, "UniqueObservedFps");
        if (uniqueFps >= 80)
        {
            throw new InvalidOperationException($"Unique FPS stayed stale during duplicate run: {uniqueFps:0.00} fps.");
        }

        AssertContains(trackerSource, "internal sealed partial class FrameFingerprintCadenceTracker");
        AssertContains(trackerSource, "public void RecordFrame(ulong hash, long timestampTick = 0)");
        AssertContains(trackerSource, "public static ulong ComputeHash(ReadOnlySpan<byte> data)");
        AssertContains(trackerSource, "private static ulong HashBytes(ulong initialHash, ReadOnlySpan<byte> data)");
        AssertDoesNotContain(trackerSource, "public Metrics GetMetrics(");
        AssertDoesNotContain(trackerSource, "private static string ResolvePattern(");
        AssertDoesNotContain(trackerSource, "private static double[] BuildRecentUniqueIntervals(");

        AssertContains(trackerMetricsSource, "internal sealed partial class FrameFingerprintCadenceTracker");
        AssertContains(trackerMetricsSource, "public readonly record struct Metrics(");
        AssertContains(trackerMetricsSource, "public static Metrics Empty { get; }");
        AssertContains(trackerMetricsSource, "public Metrics GetMetrics(int maxRecentSamples = 180)");
        AssertContains(trackerMetricsSource, "private static double[] BuildRecentUniqueIntervals(");
        AssertContains(trackerMetricsSource, "private static string ResolvePattern(");

        return Task.CompletedTask;
    }

    internal static Task VisualCadenceTracker_UsesExactCropPixelsWithOnePassDiff()
    {
        var trackerSource = ReadRepoFile("Sussudio/Services/Capture/VisualCadenceTracker.cs").Replace("\r\n", "\n");
        var trackerSamplingSource = ReadRepoFile("Sussudio/Services/Capture/VisualCadenceTracker.Sampling.cs").Replace("\r\n", "\n");
        var trackerMetricsSource = ReadRepoFile("Sussudio/Services/Capture/VisualCadenceTracker.Metrics.cs").Replace("\r\n", "\n");
        var captureSource = ReadUnifiedVideoCaptureSource();

        AssertContains(trackerSource, "DefaultSampleColumns = 640");
        AssertContains(trackerSource, "DefaultSampleRows = 360");
        AssertContains(trackerSamplingSource, "sampleX = cropX + Math.Max(0, (cropWidth - sampleWidth) / 2)");
        AssertContains(trackerSamplingSource, "sampleY = cropY + Math.Max(0, (cropHeight - sampleHeight) / 2)");
        AssertContains(trackerSamplingSource, "var x = sampleX + col;");
        AssertContains(trackerSamplingSource, "var y = sampleY + row;");
        AssertContains(trackerSource, "SampleLumaAndCompare(");
        AssertContains(trackerSamplingSource, "destination[index] = luma;");
        AssertContains(trackerSamplingSource, "if (previous != null && previous[index] != luma)");
        AssertContains(trackerSource, "_lastSample = new byte[_sampleSize * 2]");
        AssertContains(trackerSamplingSource, "if (bytesPerLuma == 2)");
        AssertContains(trackerSamplingSource, "if (previous != null && previous[index] != secondLuma)");
        AssertContains(trackerSource, "sample.ChangedPixels");
        AssertContains(trackerSource, "PromoteCurrentSample(sampleLength, bytesPerLuma)");
        AssertContains(trackerSamplingSource, "_lastSample = _currentSample;");
        AssertContains(trackerSource, "AddValueSample(_deltaWindow, ref _deltaCount, ref _deltaIndex, delta)");
        AssertContains(trackerSource, "if (delta > 0)");
        AssertContains(trackerSamplingSource, "private readonly record struct LumaSample(int Length, double ChangedPixels)");
        AssertContains(trackerSamplingSource, "private static void AddTimingSample(double[] window, ref int count, ref int index, double value)");
        AssertContains(trackerSamplingSource, "private static void AddValueSample(double[] window, ref int count, ref int index, double value)");
        AssertContains(trackerMetricsSource, "public readonly record struct Metrics(");
        AssertContains(trackerMetricsSource, "public Metrics GetMetrics(int maxRecentIntervals = 180)");
        AssertContains(trackerMetricsSource, "var deltaStats = ComputeStats(deltas);");
        AssertContains(trackerMetricsSource, "ResolveMotionConfidence(_sampleCount, deltaStats.Average, repeatPercent, changeIntervals.Length)");
        AssertDoesNotContain(trackerSource, "public Metrics GetMetrics(");
        AssertDoesNotContain(trackerSource, "private static string ResolveMotionConfidence(");
        AssertDoesNotContain(trackerSource, "ChangeThreshold");
        AssertDoesNotContain(trackerSource, "ComputeAverageDelta");
        AssertDoesNotContain(trackerSource, "Array.Copy(_currentSample, _lastSample");
        AssertDoesNotContain(trackerSource, "ComputeChangedPixelCount");
        AssertDoesNotContain(trackerSource, "private LumaSample SampleLumaAndCompare(");
        AssertDoesNotContain(trackerSource, "private void PromoteCurrentSample(");
        AssertDoesNotContain(trackerSource, "private static void AddTimingSample(");

        AssertContains(captureSource, "previewFrameProbe: null");
        AssertContains(captureSource, "frame.ArrivalTick");
        AssertContains(captureSource, "cropLeft: 0.25");
        AssertContains(captureSource, "cropWidth: 0.5");
        AssertContains(captureSource, "sampleColumns: 320");
        AssertContains(captureSource, "cropLeft: 0.375");
        AssertContains(captureSource, "cropWidth: 0.25");

        return Task.CompletedTask;
    }
}
