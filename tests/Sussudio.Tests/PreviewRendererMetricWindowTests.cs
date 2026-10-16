using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Sussudio.Tests;

// The renderer's pipeline-latency, render-CPU and frame-latency-wait getters run on every diagnostics-hub tick and on
// the stats overlay. They summarize a private copy of each sample ring. These tests drive the real getters on an
// uninitialized renderer whose rings are set directly. Failure modes covered (listed before the code):
//   1. A pooled scratch buffer is longer than the window and holds an earlier renter's values: only the window counts.
//   2. The four render-CPU rings share one read: each stage summarizes only its own ring.
//   3. A wrapped ring (full, write index mid-ring) summarizes every value; a partial ring ignores slots beyond its count.
//   4. Empty windows report zero summaries, and the frame-latency wait still reports its counters.
//   5. Concurrent readers of two renderers never see each other's windows.
//   6. Every summary equals a sort of a private copy: average over the sorted values, nearest-rank p95/p99, max.
//   7. A read does not allocate a window per ring.
public sealed class PreviewRendererMetricWindowTests
{
    private static readonly MethodInfo NearestRankIndex = SussudioAssembly.Load()
        .GetType("Sussudio.Services.Runtime.PercentileHelpers", throwOnError: true)!
        .GetMethod("NearestRankIndex", BindingFlags.Public | BindingFlags.Static)!;

    [Fact]
    public void StaleValuesFromAnEarlierLargerWindowAreIgnored()
    {
        // The pool hands this thread its own buffer back within a size class (1,024), so the 1000 ms tail an earlier
        // full window leaves behind must not reach the smaller window's percentiles or maximum.
        var fixture = new RendererFixture();
        fixture.SetPipelineRing(Repeat(1000.0, 1024), writes: 1024);
        fixture.SetWaitRing(Repeat(1000.0, 1024), writes: 1024);
        fixture.SetRenderCpuRings(Repeat(1000.0, 256), Repeat(1000.0, 256), Repeat(1000.0, 256), Repeat(1000.0, 256), writes: 256);
        _ = fixture.GetPipelineLatency();
        _ = fixture.GetFrameLatencyWait();
        _ = fixture.GetRenderCpuTiming();

        fixture.SetPipelineRing(Repeat(1.0, 1024), writes: 600);
        fixture.SetWaitRing(Repeat(1.0, 1024), writes: 600);
        fixture.SetRenderCpuRings(Repeat(1.0, 256), Repeat(2.0, 256), Repeat(3.0, 256), Repeat(4.0, 256), writes: 150);

        AssertSummary(Repeat(1.0, 600), fixture.GetPipelineLatency());
        AssertSummary(Repeat(1.0, 600), Timing(fixture.GetFrameLatencyWait()));
        var renderCpu = fixture.GetRenderCpuTiming();
        AssertSummary(Repeat(1.0, 150), Stage(renderCpu, "InputUpload"));
        AssertSummary(Repeat(2.0, 150), Stage(renderCpu, "RenderSubmit"));
        AssertSummary(Repeat(3.0, 150), Stage(renderCpu, "PresentCall"));
        AssertSummary(Repeat(4.0, 150), Stage(renderCpu, "TotalFrame"));
    }

    [Theory]
    [InlineData(1200, 1500)] // full, write index 300 slots past the start
    [InlineData(1200, 1200)] // exactly full, write index back at zero
    [InlineData(1200, 5)]    // partial: slots beyond the count hold garbage
    [InlineData(600, 2399)]
    [InlineData(1, 3)]
    public void EachRenderStageSummarizesOnlyItsOwnRing(int windowLength, int writes)
    {
        var random = new Random(windowLength * 31 + writes);
        var upload = Band(random, writes, 1.0);
        var submit = Band(random, writes, 3.0);
        var present = Band(random, writes, 5.0);
        var total = Band(random, writes, 10.0);
        var fixture = new RendererFixture();

        fixture.SetRenderCpuRings(
            WriteRing(windowLength, upload),
            WriteRing(windowLength, submit),
            WriteRing(windowLength, present),
            WriteRing(windowLength, total),
            writes);

        var renderCpu = fixture.GetRenderCpuTiming();
        AssertSummary(Newest(upload, windowLength), Stage(renderCpu, "InputUpload"));
        AssertSummary(Newest(submit, windowLength), Stage(renderCpu, "RenderSubmit"));
        AssertSummary(Newest(present, windowLength), Stage(renderCpu, "PresentCall"));
        AssertSummary(Newest(total, windowLength), Stage(renderCpu, "TotalFrame"));
    }

    [Theory]
    [InlineData(1200, 1500)]
    [InlineData(1200, 1200)]
    [InlineData(1200, 5)]
    [InlineData(2400, 2401)]
    [InlineData(1, 3)]
    public void WrappedAndPartialWindowsMatchASortOfAPrivateCopy(int windowLength, int writes)
    {
        var random = new Random(windowLength * 17 + writes);
        var latency = Band(random, writes, 20.0);
        var wait = Band(random, writes, 0.5);
        var fixture = new RendererFixture();

        fixture.SetPipelineRing(WriteRing(windowLength, latency), writes);
        fixture.SetWaitRing(WriteRing(windowLength, wait), writes);

        AssertSummary(Newest(latency, windowLength), fixture.GetPipelineLatency());
        AssertSummary(Newest(wait, windowLength), Timing(fixture.GetFrameLatencyWait()));
    }

    [Fact]
    public void EmptyWindowsReportZeroSummariesAndTheWaitCounters()
    {
        var fixture = new RendererFixture();
        fixture.SetPipelineRing(Repeat(9999.0, 1200), writes: 0);
        fixture.SetWaitRing(Repeat(9999.0, 1200), writes: 0);
        fixture.SetRenderCpuRings(Repeat(9999.0, 1200), Repeat(9999.0, 1200), Repeat(9999.0, 1200), Repeat(9999.0, 1200), writes: 0);
        fixture.Set("_frameLatencyWaitCallCount", 7L);
        fixture.Set("_frameLatencyWaitSignaledCount", 5L);
        fixture.Set("_frameLatencyWaitTimeoutCount", 2L);

        AssertSummary(Array.Empty<double>(), fixture.GetPipelineLatency());
        var renderCpu = fixture.GetRenderCpuTiming();
        foreach (var stage in new[] { "InputUpload", "RenderSubmit", "PresentCall", "TotalFrame" })
        {
            AssertSummary(Array.Empty<double>(), Stage(renderCpu, stage));
        }

        var wait = fixture.GetFrameLatencyWait();
        AssertSummary(Array.Empty<double>(), Timing(wait));
        Assert.Equal(7L, Get<long>(wait, "CallCount"));
        Assert.Equal(5L, Get<long>(wait, "SignaledCount"));
        Assert.Equal(2L, Get<long>(wait, "TimeoutCount"));
    }

    [Fact]
    public async Task ConcurrentReadersOfTwoRenderersNeverSeeEachOthersWindows()
    {
        // The hub reads on a pool thread while the stats overlay reads on the UI thread. Each renderer's values sit in
        // their own band, so a scratch shared between calls would produce a summary neither window can.
        var random = new Random(20261015);
        var low = Band(random, 1500, 10.0);
        var high = Band(random, 1500, 100.0);
        var lowRenderer = new RendererFixture();
        var highRenderer = new RendererFixture();
        foreach (var (fixture, values) in new[] { (lowRenderer, low), (highRenderer, high) })
        {
            fixture.SetPipelineRing(WriteRing(1200, values), values.Length);
            fixture.SetRenderCpuRings(
                WriteRing(1200, values), WriteRing(1200, values), WriteRing(1200, values), WriteRing(1200, values), values.Length);
        }

        var failures = new System.Collections.Concurrent.ConcurrentBag<string>();
        var workers = Enumerable.Range(0, 8).Select(thread => Task.Run(() =>
        {
            for (var iteration = 0; iteration < 200; iteration++)
            {
                var (fixture, values) = (thread + iteration) % 2 == 0 ? (lowRenderer, low) : (highRenderer, high);
                try
                {
                    AssertSummary(Newest(values, 1200), fixture.GetPipelineLatency());
                    AssertSummary(Newest(values, 1200), Stage(fixture.GetRenderCpuTiming(), "TotalFrame"));
                }
                catch (Exception ex)
                {
                    failures.Add($"thread {thread} iteration {iteration}: {ex.Message}");
                    return;
                }
            }
        })).ToArray();

        await Task.WhenAll(workers);
        Assert.Empty(failures);
    }

    [Fact]
    public void ReadingTheWindowsDoesNotAllocateAWindowPerRing()
    {
        // A copy of a 1,200-sample window is 9.6 KB, so the old reads allocated 9.6 KB (pipeline), 38.4 KB (render CPU)
        // and 9.6 KB (wait) a call. What remains is reflection and the boxed result. Averaged over 32 calls, so a pool
        // refill on this thread cannot decide the outcome.
        const int Calls = 32;
        const long BudgetPerCall = 2048;
        var random = new Random(7);
        var fixture = new RendererFixture();
        fixture.SetPipelineRing(WriteRing(1200, Band(random, 1500, 20.0)), 1500);
        fixture.SetWaitRing(WriteRing(1200, Band(random, 1500, 0.5)), 1500);
        fixture.SetRenderCpuRings(
            WriteRing(1200, Band(random, 1500, 1.0)),
            WriteRing(1200, Band(random, 1500, 3.0)),
            WriteRing(1200, Band(random, 1500, 5.0)),
            WriteRing(1200, Band(random, 1500, 10.0)),
            1500);

        foreach (var (name, read) in new (string, Func<object>)[]
        {
            ("GetPipelineLatencyMetrics", fixture.GetPipelineLatency),
            ("GetRenderCpuTimingMetrics", fixture.GetRenderCpuTiming),
            ("GetFrameLatencyWaitMetrics", fixture.GetFrameLatencyWait),
        })
        {
            _ = read();
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var call = 0; call < Calls; call++)
            {
                _ = read();
            }

            var perCall = (GC.GetAllocatedBytesForCurrentThread() - before) / Calls;
            Assert.True(perCall < BudgetPerCall, $"{name} allocated {perCall} bytes a call (budget {BudgetPerCall}).");
        }
    }

    private static void AssertSummary(double[] window, object summary)
    {
        var sorted = (double[])window.Clone();
        Array.Sort(sorted);
        var sum = 0.0;
        foreach (var value in sorted)
        {
            sum += value;
        }

        Assert.Equal(sorted.Length, Get<int>(summary, "SampleCount"));
        Assert.Equal(sorted.Length == 0 ? 0 : sum / sorted.Length, Get<double>(summary, "AverageMs"));
        Assert.Equal(Percentile(sorted, 0.95), Get<double>(summary, "P95Ms"));
        Assert.Equal(Percentile(sorted, 0.99), Get<double>(summary, "P99Ms"));
        Assert.Equal(sorted.Length == 0 ? 0 : sorted[^1], Get<double>(summary, "MaxMs"));
    }

    private static double Percentile(double[] sorted, double percentile)
        => sorted.Length == 0 ? 0 : sorted[(int)NearestRankIndex.Invoke(null, new object[] { sorted.Length, percentile })!];

    private static object Stage(object renderCpuTiming, string stage) => Get<object>(renderCpuTiming, stage);

    private static object Timing(object frameLatencyWait) => Get<object>(frameLatencyWait, "Timing");

    private static T Get<T>(object value, string propertyName)
        => (T)value.GetType().GetProperty(propertyName)!.GetValue(value)!;

    private static double[] Repeat(double value, int count) => Enumerable.Repeat(value, count).ToArray();

    // Values in [floor, floor + 1), rounded so duplicates occur and ties in the sort are exercised.
    private static double[] Band(Random random, int count, double floor)
        => Enumerable.Range(0, count).Select(_ => floor + Math.Round(random.NextDouble(), 2)).ToArray();

    private static double[] Newest(double[] written, int windowLength)
        => written.Skip(Math.Max(0, written.Length - windowLength)).ToArray();

    // Writes the values into a ring of the given length the way the renderer's trackers do. Slots the writes never
    // reach hold garbage, which a partial window must not read.
    private static double[] WriteRing(int windowLength, double[] values)
    {
        var window = Repeat(9999.0, windowLength);
        for (var i = 0; i < values.Length; i++)
        {
            window[i % windowLength] = values[i];
        }

        return window;
    }

    // Holds an uninitialized renderer: no panel, no dispatcher, no device. Only the locks and rings the getters read
    // are populated.
    private sealed class RendererFixture
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly Type _rendererType = SussudioAssembly.Load().GetType(
            "Sussudio.Services.Preview.D3D11PreviewRenderer", throwOnError: true)!;
        private readonly object _renderer;

        public RendererFixture()
        {
            _renderer = RuntimeHelpers.GetUninitializedObject(_rendererType);
            Set("_pipelineLatencyLock", new object());
            Set("_renderCpuTimingLock", new object());
            Set("_frameLatencyWaitTimingLock", new object());
        }

        public object GetPipelineLatency() => Invoke("GetPipelineLatencyMetrics");

        public object GetRenderCpuTiming() => Invoke("GetRenderCpuTimingMetrics");

        public object GetFrameLatencyWait() => Invoke("GetFrameLatencyWaitMetrics");

        public void SetPipelineRing(double[] window, int writes)
            => SetRing(new[] { ("_pipelineLatencyWindowMs", window) }, "_pipelineLatencyCount", "_pipelineLatencyIndex", writes);

        public void SetWaitRing(double[] window, int writes)
            => SetRing(
                new[] { ("_frameLatencyWaitTimingWindowMs", window) },
                "_frameLatencyWaitTimingCount",
                "_frameLatencyWaitTimingIndex",
                writes);

        public void SetRenderCpuRings(double[] upload, double[] submit, double[] present, double[] total, int writes)
            => SetRing(
                new[]
                {
                    ("_inputUploadCpuTimingWindowMs", upload),
                    ("_renderSubmitCpuTimingWindowMs", submit),
                    ("_presentCallTimingWindowMs", present),
                    ("_renderTotalCpuTimingWindowMs", total),
                },
                "_renderCpuTimingCount",
                "_renderCpuTimingIndex",
                writes);

        public void Set(string name, object? value) => Field(name).SetValue(_renderer, value);

        private void SetRing((string Name, double[] Window)[] windows, string countField, string indexField, int writes)
        {
            var length = windows[0].Window.Length;
            foreach (var (name, window) in windows)
            {
                Set(name, window);
            }

            Set(countField, Math.Min(writes, length));
            Set(indexField, writes % length);
        }

        private object Invoke(string name)
            => (_rendererType.GetMethod(name, InstanceFlags, Type.EmptyTypes)
                ?? throw new MissingMethodException(_rendererType.FullName, name))
                .Invoke(_renderer, BindingFlags.DoNotWrapExceptions, null, null, null)!;

        private FieldInfo Field(string name)
            => _rendererType.GetField(name, InstanceFlags) ?? throw new MissingFieldException(_rendererType.FullName, name);
    }
}
