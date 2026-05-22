using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task FrameFingerprintCadenceTracker_CurrentDuplicateRunLowersUniqueFps()
    {
        var trackerSource = ReadRepoFile("Sussudio/Services/Capture/FrameFingerprintCadenceTracker.cs").Replace("\r\n", "\n");
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

        AssertContains(trackerSource, "internal sealed class FrameFingerprintCadenceTracker");
        AssertDoesNotContain(trackerSource, "partial class FrameFingerprintCadenceTracker");
        AssertContains(trackerSource, "public void RecordFrame(ulong hash, long timestampTick = 0)");
        AssertContains(trackerSource, "public static ulong ComputeHash(ReadOnlySpan<byte> data)");
        AssertContains(trackerSource, "private static ulong HashBytes(ulong initialHash, ReadOnlySpan<byte> data)");
        AssertContains(trackerSource, "public readonly record struct Metrics(");
        AssertContains(trackerSource, "public static Metrics Empty { get; }");
        AssertContains(trackerSource, "public Metrics GetMetrics(int maxRecentSamples = 180)");
        AssertContains(trackerSource, "private static double[] BuildRecentUniqueIntervals(");
        AssertContains(trackerSource, "private static string ResolvePattern(");

        return Task.CompletedTask;
    }

    internal static Task VisualCadenceTracker_UsesExactCropPixelsWithOnePassDiff()
    {
        var trackerSource = ReadRepoFile("Sussudio/Services/Capture/VisualCadenceTracker.cs").Replace("\r\n", "\n");
        var captureSource = ReadUnifiedVideoCaptureSource();

        AssertContains(trackerSource, "internal sealed class VisualCadenceTracker");
        AssertDoesNotContain(trackerSource, "partial class VisualCadenceTracker");
        AssertContains(trackerSource, "DefaultSampleColumns = 640");
        AssertContains(trackerSource, "DefaultSampleRows = 360");
        AssertContains(trackerSource, "sampleX = cropX + Math.Max(0, (cropWidth - sampleWidth) / 2)");
        AssertContains(trackerSource, "sampleY = cropY + Math.Max(0, (cropHeight - sampleHeight) / 2)");
        AssertContains(trackerSource, "var x = sampleX + col;");
        AssertContains(trackerSource, "var y = sampleY + row;");
        AssertContains(trackerSource, "SampleLumaAndCompare(");
        AssertContains(trackerSource, "destination[index] = luma;");
        AssertContains(trackerSource, "if (previous != null && previous[index] != luma)");
        AssertContains(trackerSource, "_lastSample = new byte[_sampleSize * 2]");
        AssertContains(trackerSource, "if (bytesPerLuma == 2)");
        AssertContains(trackerSource, "if (previous != null && previous[index] != secondLuma)");
        AssertContains(trackerSource, "sample.ChangedPixels");
        AssertContains(trackerSource, "PromoteCurrentSample(sampleLength, bytesPerLuma)");
        AssertContains(trackerSource, "_lastSample = _currentSample;");
        AssertContains(trackerSource, "AddValueSample(_deltaWindow, ref _deltaCount, ref _deltaIndex, delta)");
        AssertContains(trackerSource, "if (delta > 0)");
        AssertContains(trackerSource, "private readonly record struct LumaSample(int Length, double ChangedPixels)");
        AssertContains(trackerSource, "private static void AddTimingSample(double[] window, ref int count, ref int index, double value)");
        AssertContains(trackerSource, "private static void AddValueSample(double[] window, ref int count, ref int index, double value)");
        AssertContains(trackerSource, "public readonly record struct Metrics(");
        AssertContains(trackerSource, "public Metrics GetMetrics(int maxRecentIntervals = 180)");
        AssertContains(trackerSource, "var deltaStats = ComputeStats(deltas);");
        AssertContains(trackerSource, "ResolveMotionConfidence(_sampleCount, deltaStats.Average, repeatPercent, changeIntervals.Length)");
        AssertDoesNotContain(trackerSource, "ChangeThreshold");
        AssertDoesNotContain(trackerSource, "ComputeAverageDelta");
        AssertDoesNotContain(trackerSource, "Array.Copy(_currentSample, _lastSample");
        AssertDoesNotContain(trackerSource, "ComputeChangedPixelCount");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "VisualCadenceTracker.Sampling.cs")),
            "old visual cadence sampling partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "VisualCadenceTracker.Metrics.cs")),
            "old visual cadence metrics partial removed");

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
