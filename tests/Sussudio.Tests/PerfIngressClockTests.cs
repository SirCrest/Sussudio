using Sussudio.Services.Performance;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of the QPC <-> 100 ns conversion and the ingress-clock classification
// (docs/architecture/performance-telemetry.md sections 2.6 and 5): overflow of ticks * 10_000_000 after 25.6 hours
// of uptime, non-integer frequency ratios, degenerate input, fewer than 120 usable samples, a single out-of-range
// offset, skipped non-positive sample times, a classification that must never flip, and reset on restart.
public sealed class PerfIngressClockTests
{
    private const long HundredNsPerMs = 10_000;
    private const long HundredNsPerSecond = 10_000_000;

    [Fact]
    public void TicksToHundredNsIsExactAtTenMegahertzEvenAfterTheNaiveProductWouldOverflow()
    {
        // 30 days of uptime at 10 MHz: ticks * 10_000_000 would be 2.6e19, past long.MaxValue (9.2e18).
        var ticks = 30L * 24 * 3600 * 10_000_000;
        Assert.True(ticks > long.MaxValue / HundredNsPerSecond, "the fixture must exceed the naive-multiply range");

        Assert.Equal(ticks, QpcMath.TicksToHundredNs(ticks, 10_000_000));
        Assert.Equal(ticks + 7, QpcMath.TicksToHundredNs(ticks + 7, 10_000_000));
    }

    [Theory]
    [InlineData(3_000_000L)]
    [InlineData(10_000_000L)]
    [InlineData(24_000_000L)]
    [InlineData(1_000_000_000L)]
    public void ConversionRoundTripsWithinOneHundredNsUnitAtAwkwardFrequencies(long frequency)
    {
        foreach (var uptimeSeconds in new long[] { 0, 1, 90, 3600, 25 * 3600 + 41, 400L * 24 * 3600 })
        {
            var ticks = uptimeSeconds * frequency + frequency / 7;
            var hundredNs = QpcMath.TicksToHundredNs(ticks, frequency);
            var expected = (long)((System.Int128)ticks * HundredNsPerSecond / frequency);
            Assert.Equal(expected, hundredNs);

            // 100 ns is the unit's resolution, so the round trip is exact to one unit: frequency / 10 MHz ticks.
            var tolerance = frequency / HundredNsPerSecond + 1;
            var back = QpcMath.HundredNsToTicks(hundredNs, frequency);
            Assert.InRange(back - ticks, -tolerance, tolerance);
        }
    }

    [Fact]
    public void HundredNsToTicksStaysExactForLongUptimeAtHighFrequency()
    {
        const long frequency = 1_000_000_000;
        var hundredNs = 400L * 24 * 3600 * HundredNsPerSecond;
        Assert.Equal(400L * 24 * 3600 * frequency, QpcMath.HundredNsToTicks(hundredNs, frequency));
    }

    [Fact]
    public void ConversionNeverThrowsOnZeroNegativeOrDegenerateInput()
    {
        Assert.Equal(0, QpcMath.TicksToHundredNs(0, 10_000_000));
        Assert.Equal(0, QpcMath.HundredNsToTicks(0, 10_000_000));
        Assert.Equal(0, QpcMath.TicksToHundredNs(123, 0));
        Assert.Equal(0, QpcMath.HundredNsToTicks(123, 0));
        Assert.Equal(0, QpcMath.TicksToHundredNs(123, -5));

        var negative = QpcMath.TicksToHundredNs(-10_000_000, 10_000_000);
        Assert.Equal(-HundredNsPerSecond, negative);

        // Huge input saturates instead of wrapping negative.
        Assert.True(QpcMath.TicksToHundredNs(long.MaxValue, 1) > 0);
        Assert.True(QpcMath.HundredNsToTicks(long.MaxValue, long.MaxValue) > 0);
    }

    [Fact]
    public void FewerThanTheClassificationSampleCountStaysUnknown()
    {
        var classifier = new IngressClockClassifier();
        for (var i = 0; i < IngressClockClassifier.ClassificationSampleCount - 1; i++)
        {
            Assert.False(classifier.Observe(5 * HundredNsPerMs));
        }

        Assert.Equal(PerfIngressClockKind.Unknown, classifier.Kind);
        Assert.False(classifier.IsQpcDomain);
    }

    [Fact]
    public void AllOffsetsInRangeClassifyAsQpcOnTheSampleThatCompletesTheWindow()
    {
        var classifier = new IngressClockClassifier();
        var completed = false;
        for (var i = 0; i < IngressClockClassifier.ClassificationSampleCount; i++)
        {
            // Offsets 3.0..4.19 ms, minimum 3.0 ms.
            completed = classifier.Observe((30 + i) * HundredNsPerMs / 10);
            Assert.Equal(i == IngressClockClassifier.ClassificationSampleCount - 1, completed);
        }

        Assert.Equal(PerfIngressClockKind.Qpc, classifier.Kind);
        Assert.True(classifier.IsQpcDomain);
        Assert.Equal(3 * HundredNsPerMs, classifier.MinOffset100Ns);
    }

    [Fact]
    public void OneOutOfRangeOffsetClassifiesRelativeAtOnceAndPermanently()
    {
        var classifier = new IngressClockClassifier();
        for (var i = 0; i < 10; i++)
        {
            Assert.False(classifier.Observe(2 * HundredNsPerMs));
        }

        // Exactly 2 s is outside [0, 2 s).
        Assert.True(classifier.Observe(2 * HundredNsPerSecond));
        Assert.Equal(PerfIngressClockKind.Relative, classifier.Kind);
        Assert.Equal(2 * HundredNsPerMs, classifier.MinOffset100Ns);

        for (var i = 0; i < IngressClockClassifier.ClassificationSampleCount * 2; i++)
        {
            Assert.False(classifier.Observe(HundredNsPerMs));
        }

        Assert.Equal(PerfIngressClockKind.Relative, classifier.Kind);
    }

    [Fact]
    public void NegativeOffsetIsOutOfRange()
    {
        var classifier = new IngressClockClassifier();
        Assert.True(classifier.Observe(-1));
        Assert.Equal(PerfIngressClockKind.Relative, classifier.Kind);
        Assert.Equal(-1, classifier.MinOffset100Ns);
    }

    [Fact]
    public void QpcClassificationNeverFlipsOnLaterOutliers()
    {
        var classifier = new IngressClockClassifier();
        for (var i = 0; i < IngressClockClassifier.ClassificationSampleCount; i++)
        {
            classifier.Observe(HundredNsPerMs);
        }

        Assert.Equal(PerfIngressClockKind.Qpc, classifier.Kind);
        Assert.False(classifier.Observe(10 * HundredNsPerSecond));
        Assert.False(classifier.Observe(-HundredNsPerSecond));
        Assert.Equal(PerfIngressClockKind.Qpc, classifier.Kind);
        Assert.Equal(HundredNsPerMs, classifier.MinOffset100Ns);
    }

    [Fact]
    public void ResetReturnsToUnknownSoARestartedCaptureIsClassifiedAfresh()
    {
        var classifier = new IngressClockClassifier();
        classifier.Observe(5 * HundredNsPerSecond);
        Assert.Equal(PerfIngressClockKind.Relative, classifier.Kind);

        classifier.Reset();
        Assert.Equal(PerfIngressClockKind.Unknown, classifier.Kind);
        Assert.Equal(long.MinValue, classifier.MinOffset100Ns);

        for (var i = 0; i < IngressClockClassifier.ClassificationSampleCount; i++)
        {
            classifier.Observe(HundredNsPerMs);
        }

        Assert.Equal(PerfIngressClockKind.Qpc, classifier.Kind);
    }
}
