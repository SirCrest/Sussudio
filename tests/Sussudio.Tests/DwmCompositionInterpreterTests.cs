using Sussudio.Services.Performance;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of the DWM/display truth fix (docs/architecture/performance-telemetry.md sections 2.8 and 5): a low
// DWM composed rate while the preview presents normally (independent flip or overlay bypass) must read as bypass, not
// as dropped frames; missing readings must read as unknown, not as zero; an idle preview must not be classified from
// unrelated screen activity; the boundaries between paths must be stable; and the renderer-to-report hook must
// survive a provider that throws, a second renderer replacing the first, and an old renderer unregistering late.
public sealed class DwmCompositionInterpreterTests
{
    [Fact]
    public void AFourKSixtyPreviewOnA120HzDisplayThatDwmBarelyComposesIsBypassNotDroppedFrames()
    {
        // The measured live case: 0.057 composed per second against 60 presents per second on a 120 Hz display.
        var verdict = DwmCompositionInterpreter.Classify(composedPerSec: 0.057, presentsPerSec: 60, refreshHz: 120);

        Assert.Equal("bypass", verdict.Path);
        Assert.Contains("not dropped frames", verdict.Interpretation);
        Assert.Contains("120", verdict.Interpretation);
        Assert.Contains("60", verdict.Interpretation);
    }

    [Theory]
    [InlineData(59.5, 60, "composed")]
    [InlineData(60, 60, "composed")]
    [InlineData(120, 60, "composed")]
    [InlineData(45, 60, "composed")]
    [InlineData(30, 60, "mixed")]
    [InlineData(16, 60, "mixed")]
    [InlineData(14.9, 60, "bypass")]
    [InlineData(0, 60, "bypass")]
    public void PathsFollowTheComposedToPresentedRatioWithStableBoundaries(double composed, double presents, string expected)
    {
        Assert.Equal(expected, DwmCompositionInterpreter.Classify(composed, presents, 120).Path);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.99)]
    public void APreviewThatPresentedNothingIsIdleWhateverDwmComposedForOtherWindows(double presents)
    {
        var verdict = DwmCompositionInterpreter.Classify(composedPerSec: 30, presentsPerSec: presents, refreshHz: 120);
        Assert.Equal("idle", verdict.Path);
    }

    [Fact]
    public void MissingReadingsAreUnknownNeverAssumedZero()
    {
        Assert.Equal("unknown", DwmCompositionInterpreter.Classify(null, 60, 120).Path);
        Assert.Equal("unknown", DwmCompositionInterpreter.Classify(null, null, null).Path);

        var noPresentRate = DwmCompositionInterpreter.Classify(composedPerSec: 0.057, presentsPerSec: null, refreshHz: 120);
        Assert.Equal("unknown", noPresentRate.Path);
        Assert.Contains("cannot be interpreted", noPresentRate.Interpretation);
    }

    [Fact]
    public void AnUnknownRefreshRateStillClassifiesAndLeavesOutTheDisplayClause()
    {
        var verdict = DwmCompositionInterpreter.Classify(composedPerSec: 0.1, presentsPerSec: 60, refreshHz: null);
        Assert.Equal("bypass", verdict.Path);
        Assert.DoesNotContain("Hz", verdict.Interpretation);

        Assert.Equal("bypass", DwmCompositionInterpreter.Classify(0.1, 60, 0).Path);
    }

    private static PerfDisplayCounters Counters(long generation) => new(generation, 10, 1000, 2000, 2000, 123456);

    [Fact]
    public void NothingRegisteredReadsAsNull()
    {
        Assert.Null(PerfDisplaySource.Read());
    }

    [Fact]
    public void AProviderThatThrowsIsAbsentDataNotAnException()
    {
        Func<PerfDisplayCounters?> broken = () => throw new InvalidOperationException("renderer is shutting down");
        PerfDisplaySource.Register(broken);
        try
        {
            Assert.Null(PerfDisplaySource.Read());
        }
        finally
        {
            PerfDisplaySource.Unregister(broken);
        }
    }

    [Fact]
    public void AnOldRendererUnregisteringLateDoesNotRemoveTheNewerRenderersProvider()
    {
        Func<PerfDisplayCounters?> first = () => Counters(1);
        Func<PerfDisplayCounters?> second = () => Counters(2);

        PerfDisplaySource.Register(first);
        PerfDisplaySource.Register(second);
        try
        {
            PerfDisplaySource.Unregister(first);
            Assert.Equal(2, PerfDisplaySource.Read()?.Generation);

            PerfDisplaySource.Unregister(second);
            Assert.Null(PerfDisplaySource.Read());
        }
        finally
        {
            PerfDisplaySource.Unregister(first);
            PerfDisplaySource.Unregister(second);
        }
    }
}
