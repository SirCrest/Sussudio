using System;
using System.Numerics;
using Sussudio.Controllers;
using Sussudio.Services.Preview;
using Xunit;

namespace Sussudio.Tests;

/// <summary>
/// Executes the linked production <see cref="FrameTimeGraphGeometry"/> source directly.
/// The projection is pure arithmetic over sample metadata, so these are behavioral tests
/// of the real graph geometry — not source-text or reflection assertions.
/// </summary>
public sealed class FrameTimeGraphGeometryTests
{
    private const long Frequency = 10_000_000;
    private const float Width = 400;
    private const float Height = 200;

    private static PreviewFrameTimeSample Cadence(
        long sequence,
        long timestampQpc,
        double intervalMs,
        long epoch = 1,
        PreviewFrameTimeSampleFlags extraFlags = PreviewFrameTimeSampleFlags.None)
        => new(
            epoch,
            sequence,
            timestampQpc,
            intervalMs,
            PreviewFrameTimeSampleFlags.CountForPresentCadence | extraFlags);

    // ---- FrameTimeGraphScale -------------------------------------------------

    [Theory]
    [InlineData(60, 1000d / 60)]
    [InlineData(30, 1000d / 30)]
    [InlineData(120, 1000d / 120)]
    public void Scale_DerivesFrameBudgetFromExpectedFps(double expectedFps, double expectedBudgetMs)
    {
        var scale = FrameTimeGraphScale.FromExpectedFps(expectedFps);

        Assert.Equal(expectedBudgetMs, scale.FrameBudgetMs, 10);
        Assert.Equal(expectedBudgetMs * 3.25, scale.MaximumFrameTimeMs, 10);
        Assert.Equal(expectedFps * 1.25, scale.MaximumFps, 10);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Scale_FallsBackToSixtyForUnusableExpectedFps(double value)
    {
        // A device that has not reported a mode yet must not collapse the axis to
        // zero or infinity; the graph falls back to a 60 Hz scale.
        Assert.Equal(60, FrameTimeGraphScale.FromExpectedFps(value).ExpectedFps);
    }

    [Theory]
    [InlineData(0.5, 1)]
    [InlineData(5000, 1000)]
    public void Scale_ClampsExpectedFpsIntoTheSupportedRange(double value, double expected)
    {
        Assert.Equal(expected, FrameTimeGraphScale.FromExpectedFps(value).ExpectedFps);
    }

    // ---- ProjectX ------------------------------------------------------------

    [Fact]
    public void ProjectX_PlacesTheOriginAtTheRightEdge()
    {
        // The graph scrolls right-to-left: the newest sample sits at the origin,
        // which is the right edge of the plot.
        Assert.Equal(Width, FrameTimeGraphGeometry.ProjectX(1_000, 1_000, Frequency, Width), 3);
    }

    [Fact]
    public void ProjectX_PlacesAFullWindowInThePastAtTheLeftEdge()
    {
        var oneWindowAgo = 1_000 - (long)(PreviewFrameTimeHistory.WindowSeconds * Frequency);

        Assert.Equal(0, FrameTimeGraphGeometry.ProjectX(oneWindowAgo, 1_000, Frequency, Width), 3);
    }

    [Fact]
    public void ProjectX_IsMonotonicInTimestamp()
    {
        const long Origin = 5_000_000_000L;
        var previous = float.NegativeInfinity;

        for (var step = 100; step >= 0; step--)
        {
            var offsetTicks = (long)(PreviewFrameTimeHistory.WindowSeconds * Frequency * step / 100d);
            var x = FrameTimeGraphGeometry.ProjectX(Origin - offsetTicks, Origin, Frequency, Width);

            Assert.True(x >= previous, $"x went backwards at step {step}");
            previous = x;
        }
    }

    // ---- ProjectY ------------------------------------------------------------

    [Fact]
    public void ProjectY_PutsAZeroValueOnTheBaseline()
    {
        var scale = FrameTimeGraphScale.FromExpectedFps(60);

        var y = FrameTimeGraphGeometry.ProjectY(0, scale, FrameTimeGraphPanel.FrameTime, Height, out var outOfRange);

        Assert.Equal(Height, y, 3);
        Assert.False(outOfRange);
    }

    [Fact]
    public void ProjectY_PutsTheAxisMaximumAtTheTop()
    {
        var scale = FrameTimeGraphScale.FromExpectedFps(60);

        var y = FrameTimeGraphGeometry.ProjectY(
            scale.MaximumFrameTimeMs, scale, FrameTimeGraphPanel.FrameTime, Height, out var outOfRange);

        Assert.Equal(0, y, 3);
        Assert.False(outOfRange);
    }

    [Fact]
    public void ProjectY_FlagsAFrameTimeAboveTheAxisMaximumAndStillClampsIntoThePlot()
    {
        var scale = FrameTimeGraphScale.FromExpectedFps(60);

        var y = FrameTimeGraphGeometry.ProjectY(
            scale.MaximumFrameTimeMs * 4, scale, FrameTimeGraphPanel.FrameTime, Height, out var outOfRange);

        Assert.True(outOfRange);
        Assert.InRange(y, 0, Height);
    }

    [Fact]
    public void ProjectY_ConvertsIntervalToFpsOnTheFramesPerSecondPanel()
    {
        var scale = FrameTimeGraphScale.FromExpectedFps(60);

        // A 16.667 ms interval is 60 fps, plotted against the 75 fps axis maximum.
        var y = FrameTimeGraphGeometry.ProjectY(
            1000d / 60, scale, FrameTimeGraphPanel.FramesPerSecond, Height, out var outOfRange);

        Assert.False(outOfRange);
        Assert.Equal(Height * (1 - 60d / scale.MaximumFps), y, 2);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ProjectY_TreatsNonFiniteIntervalsAsOutOfRangeWithoutEmittingNaN(double intervalMs)
    {
        var scale = FrameTimeGraphScale.FromExpectedFps(60);

        var y = FrameTimeGraphGeometry.ProjectY(
            intervalMs, scale, FrameTimeGraphPanel.FrameTime, Height, out var outOfRange);

        Assert.True(outOfRange);
        Assert.True(float.IsFinite(y), "a non-finite interval must not leak NaN into the geometry");
        Assert.InRange(y, 0, Height);
    }

    [Fact]
    public void ProjectY_NeverLeavesThePlotAcrossASweepOfIntervals()
    {
        var scale = FrameTimeGraphScale.FromExpectedFps(60);

        foreach (var panel in new[] { FrameTimeGraphPanel.FrameTime, FrameTimeGraphPanel.FramesPerSecond })
        {
            for (var intervalMs = 0.05; intervalMs < 400; intervalMs += 0.05)
            {
                var y = FrameTimeGraphGeometry.ProjectY(intervalMs, scale, panel, Height, out _);

                Assert.True(float.IsFinite(y), $"non-finite y at {intervalMs} on {panel}");
                Assert.InRange(y, 0, Height);
            }
        }
    }

    // ---- TryProjectSegment ---------------------------------------------------

    [Theory]
    [InlineData(0f, 200f)]
    [InlineData(400f, 0f)]
    public void TryProjectSegment_RefusesADegeneratePlotRectangle(float width, float height)
    {
        var ok = FrameTimeGraphGeometry.TryProjectSegment(
            null, Cadence(1, 0, 16.7), 0, Frequency, width, height,
            FrameTimeGraphScale.FromExpectedFps(60), FrameTimeGraphPanel.FrameTime, out var segment);

        Assert.False(ok);
        Assert.Equal(default, segment);
    }

    [Fact]
    public void TryProjectSegment_RefusesAnUnusableQpcFrequency()
    {
        var ok = FrameTimeGraphGeometry.TryProjectSegment(
            null, Cadence(1, 0, 16.7), 0, 0, Width, Height,
            FrameTimeGraphScale.FromExpectedFps(60), FrameTimeGraphPanel.FrameTime, out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryProjectSegment_SkipsASampleExcludedFromCadenceMeasurement()
    {
        var nonCadence = new PreviewFrameTimeSample(1, 1, 0, 16.7, PreviewFrameTimeSampleFlags.None);

        var ok = FrameTimeGraphGeometry.TryProjectSegment(
            null, nonCadence, 0, Frequency, Width, Height,
            FrameTimeGraphScale.FromExpectedFps(60), FrameTimeGraphPanel.FrameTime, out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryProjectSegment_DrawsATickForAnIsolatedInRangeSample()
    {
        var sample = Cadence(1, 0, 16.7);

        var ok = FrameTimeGraphGeometry.TryProjectSegment(
            null, sample, 0, Frequency, Width, Height,
            FrameTimeGraphScale.FromExpectedFps(60), FrameTimeGraphPanel.FrameTime, out var segment);

        Assert.True(ok);
        Assert.False(segment.OutOfRange);
        // A 1.5px wide horizontal tick centred on the sample.
        Assert.Equal(segment.Start.Y, segment.End.Y, 4);
        Assert.Equal(1.5f, segment.End.X - segment.Start.X, 4);
    }

    [Fact]
    public void TryProjectSegment_DrawsADownwardSpikeForAnIsolatedOutOfRangeSample()
    {
        var scale = FrameTimeGraphScale.FromExpectedFps(60);
        var sample = Cadence(1, 0, scale.MaximumFrameTimeMs * 4);

        var ok = FrameTimeGraphGeometry.TryProjectSegment(
            null, sample, 0, Frequency, Width, Height, scale,
            FrameTimeGraphPanel.FrameTime, out var segment);

        Assert.True(ok);
        Assert.True(segment.OutOfRange);
        Assert.Equal(segment.Start.X, segment.End.X, 4);
        Assert.True(segment.End.Y > segment.Start.Y, "the overflow spike must extend downward");
        Assert.InRange(segment.End.Y, 0, Height);
    }

    [Fact]
    public void TryProjectSegment_JoinsTwoConsecutiveSamplesFromTheSameEpoch()
    {
        var previous = Cadence(1, 0, 16.7);
        var current = Cadence(2, Frequency / 60, 16.7);

        var ok = FrameTimeGraphGeometry.TryProjectSegment(
            previous, current, 0, Frequency, Width, Height,
            FrameTimeGraphScale.FromExpectedFps(60), FrameTimeGraphPanel.FrameTime, out var segment);

        Assert.True(ok);
        Assert.Equal(
            FrameTimeGraphGeometry.ProjectX(previous.TimestampQpc, 0, Frequency, Width), segment.Start.X, 3);
        Assert.Equal(
            FrameTimeGraphGeometry.ProjectX(current.TimestampQpc, 0, Frequency, Width), segment.End.X, 3);
    }

    [Fact]
    public void TryProjectSegment_NeverBridgesAnEpochReset()
    {
        // An epoch bump means history was reset; joining across it would draw a
        // line through time that never elapsed.
        var previous = Cadence(1, 0, 16.7, epoch: 1);
        var current = Cadence(2, Frequency / 60, 16.7, epoch: 2);

        FrameTimeGraphGeometry.TryProjectSegment(
            previous, current, 0, Frequency, Width, Height,
            FrameTimeGraphScale.FromExpectedFps(60), FrameTimeGraphPanel.FrameTime, out var segment);

        Assert.Equal(1.5f, segment.End.X - segment.Start.X, 4);
    }

    [Fact]
    public void TryProjectSegment_NeverBridgesASequenceGapFromOverwrittenHistory()
    {
        var previous = Cadence(1, 0, 16.7);
        var current = Cadence(9, Frequency / 60, 16.7);

        FrameTimeGraphGeometry.TryProjectSegment(
            previous, current, 0, Frequency, Width, Height,
            FrameTimeGraphScale.FromExpectedFps(60), FrameTimeGraphPanel.FrameTime, out var segment);

        Assert.Equal(1.5f, segment.End.X - segment.Start.X, 4);
    }

    [Fact]
    public void TryProjectSegment_NeverBridgesAcrossAnExplicitGapFlag()
    {
        var previous = Cadence(1, 0, 16.7);
        var current = Cadence(2, Frequency / 60, 16.7,
            extraFlags: PreviewFrameTimeSampleFlags.GapBefore);

        FrameTimeGraphGeometry.TryProjectSegment(
            previous, current, 0, Frequency, Width, Height,
            FrameTimeGraphScale.FromExpectedFps(60), FrameTimeGraphPanel.FrameTime, out var segment);

        Assert.Equal(1.5f, segment.End.X - segment.Start.X, 4);
    }

    [Fact]
    public void TryProjectSegment_NeverBridgesBackwardsInTime()
    {
        var previous = Cadence(1, Frequency, 16.7);
        var current = Cadence(2, 0, 16.7);

        FrameTimeGraphGeometry.TryProjectSegment(
            previous, current, 0, Frequency, Width, Height,
            FrameTimeGraphScale.FromExpectedFps(60), FrameTimeGraphPanel.FrameTime, out var segment);

        Assert.Equal(1.5f, segment.End.X - segment.Start.X, 4);
    }

    [Fact]
    public void TryProjectSegment_MarksAJoinedSegmentOutOfRangeWhenEitherEndOverflows()
    {
        var scale = FrameTimeGraphScale.FromExpectedFps(60);
        var previous = Cadence(1, 0, scale.MaximumFrameTimeMs * 4);
        var current = Cadence(2, Frequency / 60, 16.7);

        FrameTimeGraphGeometry.TryProjectSegment(
            previous, current, 0, Frequency, Width, Height, scale,
            FrameTimeGraphPanel.FrameTime, out var segment);

        Assert.True(segment.OutOfRange);
    }

    // ---- Cross-panel and hitch-height contracts ------------------------------

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

    private static PreviewFrameTimeSample Sample(long sequence, long timestamp, double interval)
        => new(1, sequence, timestamp, interval, PreviewFrameTimeSampleFlags.CountForPresentCadence);
}
