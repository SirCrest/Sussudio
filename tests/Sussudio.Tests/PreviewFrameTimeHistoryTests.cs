using Sussudio.Controllers;
using Sussudio.Services.Preview;
using Xunit;
using Xunit.Abstractions;

namespace Sussudio.Tests;

[CollectionDefinition(nameof(PreviewPerformanceCollection), DisableParallelization = true)]
public sealed class PreviewPerformanceCollection { }

[Collection(nameof(PreviewPerformanceCollection))]
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
        // Exercise retention eviction and ring wrap before measuring warm updates.
        for (var i = 0; i < history.Capacity + fps * 12; i++)
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
