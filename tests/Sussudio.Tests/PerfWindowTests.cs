using Sussudio.Services.Performance;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of the registry and measurement windows (docs/architecture/performance-telemetry.md
// sections 2.2, 2.3 and 5): restart of an existing name, the 17th window, unknown names, end removing the
// window, baselines that never leak earlier samples, and metrics that appear after a baseline.
public sealed class PerfWindowTests
{
    private static (PerfRegistry Registry, PerfHistogram Histogram, PerfCounter Counter, PerfWindowSet<int> Windows, Func<int> Bump)
        CreateFixture()
    {
        var registry = new PerfRegistry();
        var histogram = registry.Histogram("test.latency", PerfUnit.Microseconds, "test histogram");
        var counter = registry.Counter("test.events", "test counter");
        var system = 0;
        var windows = new PerfWindowSet<int>(registry, () => system);
        return (registry, histogram, counter, windows, () => ++system);
    }

    [Fact]
    public void RegistryReturnsTheSameInstanceForTheSameNameAndRejectsAKindMismatch()
    {
        var registry = new PerfRegistry();
        var first = registry.Histogram("a.b", PerfUnit.Microseconds, "d");
        Assert.Same(first, registry.Histogram("a.b", PerfUnit.Microseconds, "d"));
        Assert.Throws<InvalidOperationException>(() => registry.Counter("a.b", "d"));

        var counter = registry.Counter("a.c", "d");
        Assert.Same(counter, registry.GetOrAddCounter("a.c", "d"));
        Assert.Throws<InvalidOperationException>(() => registry.Histogram("a.c", PerfUnit.Microseconds, "d"));
    }

    [Fact]
    public void CatalogMetricsHaveUniqueStableDottedNames()
    {
        var names = PerfMetrics.Registry.Catalog.Select(info => info.Name).ToList();
        Assert.True(names.Count > 40, "the catalog should declare every spec 2.2 metric");
        Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
        foreach (var info in PerfMetrics.Registry.Catalog)
        {
            // Dynamic names (preview.drop.<reason>) may contain hyphens.
            Assert.Matches("^[a-z0-9_]+(\\.[a-z0-9_-]+)+$", info.Name);
            Assert.False(string.IsNullOrWhiteSpace(info.Description), info.Name);
        }
    }

    [Fact]
    public void DynamicDropCountersAreCreatedOnceAndShowUpInTheCatalog()
    {
        var first = PerfMetrics.PreviewDrop("unit-test-reason");
        first.Increment();
        Assert.Same(first, PerfMetrics.PreviewDrop("unit-test-reason"));
        Assert.Contains(PerfMetrics.Registry.Catalog, info => info.Name == "preview.drop.unit-test-reason");
    }

    [Fact]
    public void BeginThenReadReportsOnlySamplesRecordedAfterTheBaseline()
    {
        var (_, histogram, counter, windows, _) = CreateFixture();
        histogram.Record(100);
        counter.Add(7);

        var begin = windows.Begin("w");
        Assert.Equal(PerfWindowBeginOutcome.Started, begin.Outcome);

        histogram.Record(200);
        histogram.Record(300);
        counter.Add(3);

        var reading = windows.Read("w");
        Assert.NotNull(reading);
        Assert.Equal(2, reading!.Delta.Find("test.latency")!.Histogram!.Count);
        Assert.Equal(500, reading.Delta.Find("test.latency")!.Histogram!.Sum);
        Assert.Equal(3, reading.Delta.Find("test.events")!.Value);
        Assert.True(reading.Delta.IsDelta);
        Assert.True(reading.ElapsedTicks >= 0);
    }

    [Fact]
    public void ReadDoesNotCloseTheWindowButEndDoes()
    {
        var (_, histogram, _, windows, _) = CreateFixture();
        windows.Begin("w");
        histogram.Record(1);

        Assert.NotNull(windows.Read("w"));
        Assert.NotNull(windows.Read("w"));
        Assert.Single(windows.List());

        var ended = windows.End("w");
        Assert.NotNull(ended);
        Assert.Equal(1, ended!.Delta.Find("test.latency")!.Histogram!.Count);
        Assert.Null(windows.Read("w"));
        Assert.Null(windows.End("w"));
        Assert.Empty(windows.List());
    }

    [Fact]
    public void BeginningAnExistingNameRestartsItAndSaysSo()
    {
        var (_, histogram, _, windows, _) = CreateFixture();
        windows.Begin("w");
        histogram.Record(10);
        histogram.Record(10);

        var restart = windows.Begin("w");
        Assert.Equal(PerfWindowBeginOutcome.Restarted, restart.Outcome);
        Assert.Equal(1, restart.ActiveCount);

        histogram.Record(10);
        Assert.Equal(1, windows.Read("w")!.Delta.Find("test.latency")!.Histogram!.Count);
    }

    [Fact]
    public void TheSeventeenthWindowIsRejectedAndTheFirstSixteenSurvive()
    {
        var (_, histogram, _, windows, _) = CreateFixture();
        for (var i = 0; i < PerfWindowSet<int>.MaxWindows; i++)
        {
            Assert.Equal(PerfWindowBeginOutcome.Started, windows.Begin($"w{i}").Outcome);
        }

        var rejected = windows.Begin("one-too-many");
        Assert.Equal(PerfWindowBeginOutcome.LimitReached, rejected.Outcome);
        Assert.Equal(PerfWindowSet<int>.MaxWindows, windows.List().Count);
        Assert.Null(windows.Read("one-too-many"));

        // Restarting a name at the limit replaces it in place instead of needing a free slot.
        Assert.Equal(PerfWindowBeginOutcome.Restarted, windows.Begin("w3").Outcome);

        // Closing one frees a slot.
        Assert.NotNull(windows.End("w0"));
        Assert.Equal(PerfWindowBeginOutcome.Started, windows.Begin("one-too-many").Outcome);
        histogram.Record(1);
        Assert.NotNull(windows.Read("w1"));
    }

    [Fact]
    public void UnknownNamesAreRejectedByReadEndAndCancel()
    {
        var (_, _, _, windows, _) = CreateFixture();
        Assert.Null(windows.Read("nope"));
        Assert.Null(windows.End("nope"));
        Assert.False(windows.Cancel("nope"));
        Assert.Null(windows.Read(null!));
        Assert.Null(windows.End(string.Empty));
    }

    [Fact]
    public void CancelDiscardsTheWindowWithoutAReport()
    {
        var (_, _, _, windows, _) = CreateFixture();
        windows.Begin("w");
        Assert.True(windows.Cancel("w"));
        Assert.False(windows.Cancel("w"));
        Assert.Null(windows.Read("w"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bad\nname")]
    public void InvalidNamesAreRejected(string name)
    {
        var (_, _, _, windows, _) = CreateFixture();
        Assert.Equal(PerfWindowBeginOutcome.InvalidName, windows.Begin(name).Outcome);
        Assert.Equal(PerfWindowBeginOutcome.InvalidName, windows.Begin(new string('x', PerfWindowSet<int>.MaxNameLength + 1)).Outcome);
        Assert.Empty(windows.List());
    }

    [Fact]
    public void MetricsRegisteredAfterTheBaselineStartFromZero()
    {
        var (registry, _, _, windows, _) = CreateFixture();
        windows.Begin("w");

        var late = registry.Histogram("test.late", PerfUnit.Microseconds, "registered after begin");
        var lateCounter = registry.Counter("test.late_events", "registered after begin");
        late.Record(5);
        lateCounter.Add(2);

        var reading = windows.Read("w")!;
        Assert.Equal(1, reading.Delta.Find("test.late")!.Histogram!.Count);
        Assert.Equal(2, reading.Delta.Find("test.late_events")!.Value);
    }

    [Fact]
    public void SystemSamplesAreCapturedAtBeginAndAtRead()
    {
        var (_, _, _, windows, bump) = CreateFixture();
        bump();
        windows.Begin("w");
        bump();
        bump();

        var reading = windows.Read("w")!;
        Assert.Equal(1, reading.Baseline);
        Assert.Equal(3, reading.Current);
    }

    [Fact]
    public void ConcurrentBeginsNeverExceedTheLimit()
    {
        var (_, _, _, windows, _) = CreateFixture();
        Parallel.For(0, 200, i => windows.Begin($"w{i}"));
        Assert.Equal(PerfWindowSet<int>.MaxWindows, windows.List().Count);
    }

    [Fact]
    public void CounterDeltasNeverGoNegativeWhenABaselineIsNewer()
    {
        var registry = new PerfRegistry();
        var counter = registry.Counter("test.events", "d");
        var older = registry.Snapshot();
        counter.Add(5);
        var newer = registry.Snapshot();

        var reversed = older.Subtract(newer);
        Assert.Equal(0, reversed.Find("test.events")!.Value);
        Assert.Equal(5, newer.Subtract(older).Find("test.events")!.Value);
    }
}
