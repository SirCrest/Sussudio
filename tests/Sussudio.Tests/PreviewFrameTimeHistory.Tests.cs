using System;
using Sussudio.Controllers;
using Sussudio.Services.Preview;
using Xunit;
using Xunit.Abstractions;

namespace Sussudio.Tests;

public sealed class PreviewFrameTimeHistoryTests
{
    private readonly ITestOutputHelper _output;

    public PreviewFrameTimeHistoryTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void IncrementalReads_ReturnOnlyUnreadSamplesAndReportOverrun()
    {
        var history = new PreviewFrameTimeHistory(capacity: 3, frequency: 1000);
        history.Append(100, 16);
        history.Append(116, 16);
        Span<PreviewFrameTimeSample> sample = stackalloc PreviewFrameTimeSample[1];
        var first = history.CopyAfter(default, sample);
        Assert.Equal(1, first.Count);
        Assert.Equal(100, sample[0].TimestampQpc);
        Assert.True(first.GapBeforeFirst);
        var second = history.CopyAfter(first.Cursor, sample);
        Assert.Equal(116, sample[0].TimestampQpc);
        Assert.False(second.GapBeforeFirst);
        Assert.Equal(0, history.CopyAfter(second.Cursor, sample).Count);

        history.Append(132, 16);
        history.Append(148, 16);
        history.Append(164, 16);
        history.Append(180, 16);
        var overwritten = history.CopyAfter(second.Cursor, sample);
        Assert.True(overwritten.GapBeforeFirst);
        Assert.Equal(148, sample[0].TimestampQpc);
        Assert.Equal(3, history.Count);
    }

    [Fact]
    public void TenSecondRetention_UsesTimestampsRatherThanSampleCount()
    {
        var history = new PreviewFrameTimeHistory(capacity: 20, frequency: 1000);
        history.Append(100, 16);
        history.Append(1000, 900);
        history.Append(10_100, 9100);
        Assert.Equal(3, history.Count);
        history.Append(10_101, 1);
        Span<PreviewFrameTimeSample> samples = stackalloc PreviewFrameTimeSample[20];
        var read = history.CopyAfter(default, samples);
        Assert.Equal(3, read.Count);
        Assert.Equal(1000, samples[0].TimestampQpc);
        Assert.Equal(10_101, samples[2].TimestampQpc);
    }

    [Fact]
    public void ResetAndRendererReplacement_InvalidateOldCursors()
    {
        var original = new PreviewFrameTimeHistory(frequency: 1000);
        Span<PreviewFrameTimeSample> sample = stackalloc PreviewFrameTimeSample[1];
        original.Append(100, 16);
        var first = original.CopyAfter(default, sample);
        original.Reset();
        var reset = original.CopyAfter(first.Cursor, sample);
        Assert.Equal(0, reset.Count);
        Assert.True(reset.GapBeforeFirst);
        Assert.NotEqual(first.Cursor.Epoch, reset.Cursor.Epoch);

        var replacement = new PreviewFrameTimeHistory(frequency: 1000);
        replacement.Append(200, 16);
        var read = replacement.CopyAfter(new(first.Cursor.Epoch, 10_000), sample);
        Assert.Equal(1, read.Count);
        Assert.True(read.GapBeforeFirst);
        Assert.NotEqual(first.Cursor.Epoch, read.Cursor.Epoch);
    }

    [Fact]
    public void ExcludedFramesAndInvalidIntervals_RemainExplicitGaps()
    {
        var history = new PreviewFrameTimeHistory(frequency: 1000);
        history.Append(100, 16);
        history.Append(116, 0, PreviewFrameTimeSampleFlags.GapBefore);
        history.Append(132, double.NaN);
        history.Append(148, 16);
        Span<PreviewFrameTimeSample> samples = stackalloc PreviewFrameTimeSample[4];
        Assert.Equal(4, history.CopyAfter(default, samples).Count);
        Assert.True(samples[0].IsCadenceSample);
        Assert.False(samples[1].IsCadenceSample);
        Assert.False(samples[2].IsCadenceSample);
        Assert.True((samples[2].Flags & PreviewFrameTimeSampleFlags.GapBefore) != 0);
        Assert.True(samples[3].IsCadenceSample);
    }

    [Fact]
    public void BackwardClock_StartsANewEpoch()
    {
        var history = new PreviewFrameTimeHistory(frequency: 1000);
        var before = history.Append(200, 16);
        var after = history.Append(100, 16);
        Assert.NotEqual(before.Epoch, after.Epoch);
        Assert.True((after.Flags & PreviewFrameTimeSampleFlags.GapBefore) != 0);
        Assert.Equal(1, history.Count);
    }

    [Theory]
    [InlineData(60)]
    [InlineData(120)]
    [InlineData(240)]
    public void HistoryAndGeometry_WarmUpdatesAllocateNothing(int fps)
    {
        const long frequency = 12_000;
        var history = new PreviewFrameTimeHistory(frequency: frequency);
        var step = frequency / fps;
        var scale = FrameTimeGraphScale.FromExpectedFps(fps);
        Span<PreviewFrameTimeSample> copied = stackalloc PreviewFrameTimeSample[1];
        PreviewFrameTimeCursor cursor = default;
        PreviewFrameTimeSample? previous = null;
        var tick = 1L;
        for (var i = 0; i < 100; i++)
            Update(history, ref cursor, ref previous, copied, tick += step, frequency, scale);

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < fps * 12; i++)
            Update(history, ref cursor, ref previous, copied, tick += step, frequency, scale);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(0, allocated);
        Assert.InRange(history.Count, fps * 10, fps * 10 + 1);
        _output.WriteLine($"{fps} fps: {history.Count} retained samples; at most {history.Count * 2} live line segments across two panels; warmed history/geometry allocations={allocated} B.");
    }

    private static void Update(PreviewFrameTimeHistory history, ref PreviewFrameTimeCursor cursor,
        ref PreviewFrameTimeSample? previous, Span<PreviewFrameTimeSample> copied,
        long tick, long frequency, FrameTimeGraphScale scale)
    {
        history.Append(tick, scale.FrameBudgetMs);
        cursor = history.CopyAfter(cursor, copied).Cursor;
        var sample = copied[0];
        FrameTimeGraphGeometry.TryProjectSegment(previous, sample, tick - frequency, frequency,
            500, 92, scale, FrameTimeGraphPanel.FrameTime, out _);
        FrameTimeGraphGeometry.TryProjectSegment(previous, sample, tick - frequency, frequency,
            500, 68, scale, FrameTimeGraphPanel.FramesPerSecond, out _);
        previous = sample;
    }
}

public sealed class FrameTimeGraphGeometryTests
{
    [Fact]
    public void TimeAxis_IsIndependentOfIncomingFrameRate()
    {
        Assert.Equal(0, FrameTimeGraphGeometry.ProjectX(1000, 11_000, 1000, 500));
        Assert.Equal(250, FrameTimeGraphGeometry.ProjectX(6000, 11_000, 1000, 500));
        Assert.Equal(500, FrameTimeGraphGeometry.ProjectX(11_000, 11_000, 1000, 500));
    }

    [Fact]
    public void OneTwoAndThreeFrameHitches_HaveDistinctHeights()
    {
        var scale = FrameTimeGraphScale.FromExpectedFps(60);
        var one = FrameTimeGraphGeometry.ProjectY(1000.0 / 60, scale, FrameTimeGraphPanel.FrameTime, 100, out var oneOverflow);
        var two = FrameTimeGraphGeometry.ProjectY(1000.0 / 30, scale, FrameTimeGraphPanel.FrameTime, 100, out var twoOverflow);
        var three = FrameTimeGraphGeometry.ProjectY(50, scale, FrameTimeGraphPanel.FrameTime, 100, out var threeOverflow);
        Assert.True(one > two && two > three && three > 0);
        Assert.False(oneOverflow || twoOverflow || threeOverflow);
        Assert.Equal(0, FrameTimeGraphGeometry.ProjectY(100, scale, FrameTimeGraphPanel.FrameTime, 100, out var overflow));
        Assert.True(overflow);
    }

    [Fact]
    public void Panels_UseTheSameTimestampAndReciprocalFrameRate()
    {
        var scale = FrameTimeGraphScale.FromExpectedFps(60);
        var sample = Sample(2, 3000, 50);
        Assert.True(FrameTimeGraphGeometry.TryProjectSegment(Sample(1, 2950, 16), sample, 5000, 1000,
            500, 100, scale, FrameTimeGraphPanel.FrameTime, out var time));
        Assert.True(FrameTimeGraphGeometry.TryProjectSegment(Sample(1, 2950, 16), sample, 5000, 1000,
            500, 100, scale, FrameTimeGraphPanel.FramesPerSecond, out var fps));
        Assert.Equal(time.End.X, fps.End.X);
        Assert.Equal(100 * (1 - 20.0 / 75), fps.End.Y, precision: 4);
    }

    [Fact]
    public void GapsAndEpochChanges_AreNeverConnected()
    {
        var prior = Sample(1, 1000, 16);
        var scale = FrameTimeGraphScale.FromExpectedFps(60);
        foreach (var sample in new[]
        {
            Sample(2, 1100, 16) with { Flags = PreviewFrameTimeSampleFlags.GapBefore | PreviewFrameTimeSampleFlags.CountForPresentCadence },
            Sample(3, 1100, 16),
            Sample(2, 1100, 16) with { Epoch = 2 }
        })
        {
            Assert.True(FrameTimeGraphGeometry.TryProjectSegment(prior, sample, 2000, 1000,
                500, 100, scale, FrameTimeGraphPanel.FrameTime, out var geometry));
            Assert.InRange(geometry.End.X - geometry.Start.X, 0, 1.5f);
        }
        Assert.False(FrameTimeGraphGeometry.TryProjectSegment(prior,
            Sample(2, 1100, 0) with { Flags = PreviewFrameTimeSampleFlags.GapBefore },
            2000, 1000, 500, 100, scale, FrameTimeGraphPanel.FrameTime, out _));
    }

    [Fact]
    public void IsolatedOutlier_HasAnExplicitEdgeMarker()
    {
        Assert.True(FrameTimeGraphGeometry.TryProjectSegment(null, Sample(1, 1000, 100), 2000, 1000,
            500, 100, FrameTimeGraphScale.FromExpectedFps(60), FrameTimeGraphPanel.FrameTime, out var geometry));
        Assert.True(geometry.OutOfRange);
        Assert.Equal(geometry.Start.X, geometry.End.X);
        Assert.True(geometry.End.Y > geometry.Start.Y);
    }

    private static PreviewFrameTimeSample Sample(long sequence, long timestamp, double interval)
        => new(1, sequence, timestamp, interval, PreviewFrameTimeSampleFlags.CountForPresentCadence);
}
