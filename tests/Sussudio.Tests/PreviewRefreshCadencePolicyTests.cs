using Sussudio.Services.Preview;
using Xunit;

namespace Sussudio.Tests;

/// <summary>
/// Executes the linked production <see cref="PreviewRefreshCadencePolicy"/> source directly. The live rig is a single
/// display and source pair (120 Hz, 60 fps), so the other cadences the policy must handle (144 Hz, 59.94 fps, a
/// source faster than the display, an unmeasured refresh) are only reachable here.
/// </summary>
public sealed class PreviewRefreshCadencePolicyTests
{
    private const long TicksPerSecond = 10_000_000;

    [Theory]
    // 60 fps on 120 Hz, paced perfectly: every frame spans two refreshes, none was due a new frame.
    [InlineData(1, 2, 0)]
    [InlineData(2, 4, 0)]
    // One frame held an extra refresh while its successor was due.
    [InlineData(1, 3, 1)]
    [InlineData(2, 5, 1)]
    [InlineData(2, 7, 3)]
    public void SixtyFpsOnOneHundredTwentyHzCountsOnlyRefreshesBeyondTheCadence(
        long presentDelta,
        long presentRefreshDelta,
        long expectedMisses)
    {
        var ratio = PreviewRefreshCadencePolicy.RefreshesPerSourceFrame(60, TicksPerSecond / 120, TicksPerSecond);

        Assert.Equal(expectedMisses, PreviewRefreshCadencePolicy.CountMissedRefreshes(presentDelta, presentRefreshDelta, ratio));
    }

    [Theory]
    [InlineData(2, 3)]
    [InlineData(2, 5)]
    [InlineData(1, 4)]
    public void MatchedRatesKeepTheRawCount(long presentDelta, long presentRefreshDelta)
    {
        var ratio = PreviewRefreshCadencePolicy.RefreshesPerSourceFrame(60, TicksPerSecond / 60, TicksPerSecond);

        Assert.Equal(
            presentRefreshDelta - presentDelta,
            PreviewRefreshCadencePolicy.CountMissedRefreshes(presentDelta, presentRefreshDelta, ratio));
    }

    [Theory]
    [InlineData(2, 3)]
    [InlineData(1, 3)]
    [InlineData(4, 6)]
    public void SourceFasterThanTheDisplayKeepsTheRawCount(long presentDelta, long presentRefreshDelta)
    {
        // 120 fps on 60 Hz: a ratio below one must not credit a frame with less than one refresh.
        var ratio = PreviewRefreshCadencePolicy.RefreshesPerSourceFrame(120, TicksPerSecond / 60, TicksPerSecond);

        Assert.Equal(0.5, ratio, 4);
        Assert.Equal(
            presentRefreshDelta - presentDelta,
            PreviewRefreshCadencePolicy.CountMissedRefreshes(presentDelta, presentRefreshDelta, ratio));
    }

    [Theory]
    // 60 fps on 144 Hz (2.4 refreshes per frame): on time, a span is the floor or ceiling of presents x 2.4.
    [InlineData(1, 2, 0)]
    [InlineData(1, 3, 0)]
    [InlineData(1, 4, 1)]
    [InlineData(2, 4, 0)]
    [InlineData(2, 5, 0)]
    [InlineData(2, 6, 1)]
    [InlineData(3, 7, 0)]
    [InlineData(3, 8, 0)]
    [InlineData(3, 9, 1)]
    public void FractionalRatioAcceptsBothOnTimeSpans(long presentDelta, long presentRefreshDelta, long expectedMisses)
    {
        var ratio = PreviewRefreshCadencePolicy.RefreshesPerSourceFrame(60, TicksPerSecond / 144, TicksPerSecond);

        Assert.Equal(expectedMisses, PreviewRefreshCadencePolicy.CountMissedRefreshes(presentDelta, presentRefreshDelta, ratio));
    }

    [Theory]
    [InlineData(119.98)]
    [InlineData(120.02)]
    public void MeasuredRefreshAHairOffNominalStillCountsASingleMiss(double measuredHz)
    {
        // Without snapping, 2.0003 refreshes per frame makes ceil(2 x ratio) = 5 the on-time span and hides this miss.
        var ratio = PreviewRefreshCadencePolicy.RefreshesPerSourceFrame(
            60,
            (long)Math.Round(TicksPerSecond / measuredHz),
            TicksPerSecond);

        Assert.NotEqual(2.0, ratio);
        Assert.Equal(0, PreviewRefreshCadencePolicy.CountMissedRefreshes(2, 4, ratio));
        Assert.Equal(1, PreviewRefreshCadencePolicy.CountMissedRefreshes(2, 5, ratio));
    }

    [Fact]
    public void FiftyNineNinetyFourOnOneHundredTwentyHzSnapsToTwoRefreshesPerFrame()
    {
        // Accepted: the natural extra repeat 59.94 fps needs about every 8 s on 120 Hz is counted as a miss.
        var ratio = PreviewRefreshCadencePolicy.RefreshesPerSourceFrame(60000.0 / 1001.0, TicksPerSecond / 120, TicksPerSecond);

        Assert.Equal(0, PreviewRefreshCadencePolicy.CountMissedRefreshes(2, 4, ratio));
        Assert.Equal(1, PreviewRefreshCadencePolicy.CountMissedRefreshes(2, 5, ratio));
    }

    [Theory]
    [InlineData(0.0, 83_333L, TicksPerSecond)]
    [InlineData(-60.0, 83_333L, TicksPerSecond)]
    [InlineData(double.NaN, 83_333L, TicksPerSecond)]
    [InlineData(double.PositiveInfinity, 83_333L, TicksPerSecond)]
    [InlineData(60.0, 0L, TicksPerSecond)]
    [InlineData(60.0, -1L, TicksPerSecond)]
    [InlineData(60.0, 83_333L, 0L)]
    public void UnknownRatesFallBackToOneRefreshPerFrame(double sourceFps, long refreshIntervalTicks, long ticksPerSecond)
    {
        Assert.Equal(1.0, PreviewRefreshCadencePolicy.RefreshesPerSourceFrame(sourceFps, refreshIntervalTicks, ticksPerSecond));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-2.0)]
    [InlineData(0.0)]
    public void UnusableRatioKeepsTheRawCount(double ratio)
    {
        Assert.Equal(3, PreviewRefreshCadencePolicy.CountMissedRefreshes(2, 5, ratio));
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(-1, 4)]
    [InlineData(2, 2)]
    [InlineData(4, 2)]
    [InlineData(2, -3)]
    public void NoPresentsOrNoExtraRefreshesIsNeverAMiss(long presentDelta, long presentRefreshDelta)
    {
        Assert.Equal(0, PreviewRefreshCadencePolicy.CountMissedRefreshes(presentDelta, presentRefreshDelta, 2.0));
    }

    [Fact]
    public void CadenceCountNeverExceedsTheRawCount()
    {
        var ratios = new[] { double.NaN, -1.0, 0.0, 0.5, 0.99, 1.0, 1.005, 1.5, 1.99967, 2.0, 2.002, 2.0003, 2.4, 2.5, 4.8, 5.0 };
        foreach (var ratio in ratios)
        {
            for (long presentDelta = -1; presentDelta <= 8; presentDelta++)
            {
                for (long presentRefreshDelta = -1; presentRefreshDelta <= 40; presentRefreshDelta++)
                {
                    var raw = presentDelta > 0 && presentRefreshDelta > presentDelta ? presentRefreshDelta - presentDelta : 0;
                    var cadence = PreviewRefreshCadencePolicy.CountMissedRefreshes(presentDelta, presentRefreshDelta, ratio);

                    Assert.InRange(cadence, 0, raw);
                }
            }
        }
    }
}
