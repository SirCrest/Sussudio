using System.Diagnostics;
using Sussudio.Services.Performance;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of the PDH GPU engine sensor (docs/architecture/performance-telemetry.md sections 2.8 and 5):
// instance names of another process that share a pid prefix, malformed instance names, NaN/negative/infinite
// counter values, a first sample or an absent instance that must never read as 0 %, counters that become unavailable
// or throw mid-run, open failures with backoff, Stop during a backoff, restart, and a stale memory figure surviving
// a stop. The counters are a fake reader; the static PerfMetrics histograms are read as deltas.
public sealed class GpuEngineSensorTests
{
    private const int Pid = 4242;

    private static GpuCounterItem Engine(int pid, string engType, double value, int engine = 0) =>
        new($"pid_{pid}_luid_0x00000000_0x0000A1B2_phys_0_eng_{engine}_engtype_{engType}", value);

    private static GpuCounterItem Memory(int pid, double bytes) =>
        new($"pid_{pid}_luid_0x00000000_0x0000A1B2_phys_0", bytes);

    private sealed class FakeReader : IGpuCounterReader
    {
        private readonly Func<List<GpuCounterItem>, List<GpuCounterItem>, (GpuCounterReadResult Result, string? Detail)> _read;

        public FakeReader(
            Func<List<GpuCounterItem>, List<GpuCounterItem>, (GpuCounterReadResult Result, string? Detail)> read,
            string? openError = null)
        {
            _read = read;
            OpenError = openError;
        }

        public string? OpenError { get; }

        public bool Disposed { get; private set; }

        public string? Open(int processId) => OpenError;

        public GpuCounterReadResult Read(List<GpuCounterItem> engines, List<GpuCounterItem> memory, out string? detail)
        {
            engines.Clear();
            memory.Clear();
            var (result, text) = _read(engines, memory);
            detail = text;
            return result;
        }

        public void Dispose() => Disposed = true;
    }

    private static FakeReader ValidReader(params GpuCounterItem[] engines) => new(
        (e, m) =>
        {
            e.AddRange(engines);
            m.Add(Memory(Pid, 512 * 1024 * 1024));
            return (GpuCounterReadResult.Valid, null);
        });

    private readonly record struct EngineMark(long Count3d, double Sum3d, long CountOther)
    {
        public static EngineMark Now() => new(
            PerfMetrics.GpuEngine3d.Snapshot().Count,
            PerfMetrics.GpuEngine3d.Snapshot().Sum,
            PerfMetrics.GpuEngineOther.Snapshot().Count);
    }

    [Fact]
    public void OnlyInstancesOfTheExactPidAreCountedEvenWhenAnotherPidSharesThePrefix()
    {
        var engines = new[]
        {
            Engine(12, "3D", 10),
            Engine(123, "3D", 20),
            Engine(1234, "3D", 40),
            Engine(12, "Copy", 1),
        };

        var sample = GpuEngineAggregator.Aggregate(12, engines, Array.Empty<GpuCounterItem>());
        Assert.Equal(10, sample.Engine3D);
        Assert.Equal(1, sample.Copy);
        Assert.Equal(2, sample.Instances);
    }

    [Fact]
    public void InstancesOfOneTypeSumAcrossEnginesAndTypesStaySeparate()
    {
        var engines = new[]
        {
            Engine(Pid, "3D", 30, engine: 0),
            Engine(Pid, "3D", 25, engine: 1),
            Engine(Pid, "VideoEncode", 12),
            Engine(Pid, "VideoDecode", 7),
            Engine(Pid, "Compute_0", 3),
            Engine(Pid, "VideoProcessing", 2),
        };

        var sample = GpuEngineAggregator.Aggregate(Pid, engines, Array.Empty<GpuCounterItem>());
        Assert.Equal(55, sample.Engine3D);
        Assert.Equal(12, sample.VideoEncode);
        Assert.Equal(7, sample.VideoDecode);
        Assert.Equal(0, sample.Copy);
        Assert.Equal(5, sample.Other);
        Assert.Equal(6, sample.Instances);
    }

    [Theory]
    [InlineData("pid_4242_luid_0x0_0x1_phys_0_eng_0_engtype_3D", GpuEngineKind.Engine3D)]
    [InlineData("pid_4242_luid_0x0_0x1_phys_0_eng_0_ENGTYPE_3d", GpuEngineKind.Engine3D)]
    [InlineData("pid_4242_luid_0x0_0x1_phys_0_eng_3_engtype_VideoEncode", GpuEngineKind.VideoEncode)]
    [InlineData("pid_4242_luid_0x0_0x1_phys_0_eng_1_engtype_VideoDecode", GpuEngineKind.VideoDecode)]
    [InlineData("pid_4242_luid_0x0_0x1_phys_0_eng_2_engtype_Copy", GpuEngineKind.Copy)]
    [InlineData("pid_4242_luid_0x0_0x1_phys_0_eng_2_engtype_Security", GpuEngineKind.Other)]
    [InlineData("pid_4242_luid_0x0_0x1_phys_0_eng_2_engtype_", GpuEngineKind.Other)]
    [InlineData("pid_4242_no_engine_type_here", GpuEngineKind.Other)]
    [InlineData("", GpuEngineKind.Other)]
    public void EngineTypesAreClassifiedAndMalformedNamesFallToOther(string instance, GpuEngineKind expected)
    {
        Assert.Equal(expected, GpuEngineAggregator.Classify(instance));
    }

    [Fact]
    public void NonFiniteAndNegativeCounterValuesAreIgnoredAndMalformedNamesDoNotThrow()
    {
        var engines = new[]
        {
            Engine(Pid, "3D", double.NaN),
            Engine(Pid, "3D", double.PositiveInfinity),
            Engine(Pid, "3D", -5),
            Engine(Pid, "3D", 20),
            new GpuCounterItem("pid_4242_", 4),
            new GpuCounterItem(string.Empty, 4),
        };
        var memory = new[]
        {
            Memory(Pid, double.NaN),
            Memory(Pid, -1),
            Memory(Pid, 1000),
            Memory(99, 5000),
        };

        var sample = GpuEngineAggregator.Aggregate(Pid, engines, memory);
        Assert.Equal(20, sample.Engine3D);
        Assert.Equal(4, sample.Other);
        Assert.Equal(1000, sample.DedicatedBytes);
    }

    [Fact]
    public void ADedicatedFigureBeyondLongRangeSaturatesInsteadOfWrapping()
    {
        var sample = GpuEngineAggregator.Aggregate(
            Pid,
            new[] { Engine(Pid, "3D", 1) },
            new[] { Memory(Pid, 1e30) });
        Assert.Equal(long.MaxValue, sample.DedicatedBytes);
    }

    [Fact]
    public void AValidSampleIsRecordedInHundredthsOfAPercentAndKeepsTheDedicatedFigure()
    {
        var sensor = new GpuEngineSensor(() => ValidReader(), Pid);
        var reader = ValidReader(Engine(Pid, "3D", 37.5), Engine(Pid, "Security", 1.25));
        var before = EngineMark.Now();

        Assert.True(sensor.SampleOnce(reader));

        var after = EngineMark.Now();
        Assert.Equal(1, after.Count3d - before.Count3d);
        Assert.Equal(1, after.CountOther - before.CountOther);
        Assert.Equal(3750, after.Sum3d - before.Sum3d);
        var status = sensor.Status;
        Assert.Equal(1, status.Samples);
        Assert.Equal(512L * 1024 * 1024, status.DedicatedBytes);
        Assert.Null(status.LastError);
        Assert.Equal(0, status.ConsecutivePending);
    }

    [Fact]
    public void APendingReadAndAReadWithNoInstanceOfThisProcessRecordNothingSoMissingNeverReadsAsZero()
    {
        var sensor = new GpuEngineSensor(() => ValidReader(), Pid);
        var pending = new FakeReader((_, _) => (GpuCounterReadResult.Pending, null));
        var strangers = ValidReader(Engine(Pid * 10, "3D", 50), Engine(Pid * 10 + 1, "Copy", 5));
        var before = EngineMark.Now();

        Assert.True(sensor.SampleOnce(pending));
        Assert.True(sensor.SampleOnce(pending));
        Assert.True(sensor.SampleOnce(strangers));

        var after = EngineMark.Now();
        Assert.Equal(before, after);
        var status = sensor.Status;
        Assert.Equal(0, status.Samples);
        Assert.Equal(3, status.ConsecutivePending);
        Assert.Null(status.DedicatedBytes);

        // A real sample clears the pending run.
        Assert.True(sensor.SampleOnce(ValidReader(Engine(Pid, "3D", 0))));
        Assert.Equal(0, sensor.Status.ConsecutivePending);
        Assert.Equal(1, EngineMark.Now().Count3d - before.Count3d);
    }

    [Fact]
    public void AGenuineZeroPercentIsRecordedAsZero()
    {
        var sensor = new GpuEngineSensor(() => ValidReader(), Pid);
        var before = EngineMark.Now();

        Assert.True(sensor.SampleOnce(ValidReader(Engine(Pid, "3D", 0))));

        var after = EngineMark.Now();
        Assert.Equal(1, after.Count3d - before.Count3d);
        Assert.Equal(0, after.Sum3d - before.Sum3d);
    }

    [Fact]
    public void AnUnavailableReadReportsFailureKeepsTheReasonAndRecordsNothing()
    {
        var sensor = new GpuEngineSensor(() => ValidReader(), Pid);
        var gone = new FakeReader((_, _) => (GpuCounterReadResult.Unavailable, "PDH counter object missing"));
        var before = EngineMark.Now();

        Assert.False(sensor.SampleOnce(gone));

        Assert.Equal(before, EngineMark.Now());
        Assert.Equal("PDH counter object missing", sensor.Status.LastError);
        Assert.Equal(0, sensor.Status.Samples);
    }

    [Fact]
    public void ARunningSensorReopensCountersThatBecomeUnavailableOrThrow()
    {
        var created = new List<FakeReader>();
        var reads = 0;
        var sensor = new GpuEngineSensor(
            () =>
            {
                var generation = created.Count;
                var reader = new FakeReader((e, m) =>
                {
                    var n = Interlocked.Increment(ref reads);
                    if (generation == 0 && n > 2)
                    {
                        return (GpuCounterReadResult.Unavailable, "driver reset");
                    }

                    if (generation == 1 && n > 4)
                    {
                        throw new InvalidOperationException("PDH blew up");
                    }

                    e.Add(Engine(Pid, "3D", 10));
                    return (GpuCounterReadResult.Valid, null);
                });
                lock (created)
                {
                    created.Add(reader);
                }

                return reader;
            },
            Pid,
            periodMs: 5);

        Assert.True(sensor.Start());
        try
        {
            SpinWait.SpinUntil(() => sensor.Status.Samples >= 6 && created.Count >= 3, TimeSpan.FromSeconds(15));
            Assert.True(sensor.Status.Samples >= 6, "sampling did not continue across reopen");
            Assert.True(created.Count >= 3, "the sensor did not reopen after Unavailable and after a throwing read");
        }
        finally
        {
            sensor.Stop();
        }

        // Every reader the sensor ever created was closed, including the ones that failed.
        lock (created)
        {
            Assert.All(created, reader => Assert.True(reader.Disposed));
        }
    }

    [Fact]
    public void OpenFailuresAreRetriedWithBackoffAndRecoverWhenTheCountersAppear()
    {
        var attempts = 0;
        var sensor = new GpuEngineSensor(
            () =>
            {
                var attempt = Interlocked.Increment(ref attempts);
                return attempt < 3
                    ? new FakeReader((_, _) => (GpuCounterReadResult.Valid, null), openError: "no GPU Engine counters yet")
                    : ValidReader(Engine(Pid, "3D", 5));
            },
            Pid,
            periodMs: 5);

        Assert.True(sensor.Start());
        try
        {
            SpinWait.SpinUntil(() => sensor.Status.Samples >= 2, TimeSpan.FromSeconds(15));
            var status = sensor.Status;
            Assert.True(status.Samples >= 2);
            Assert.Equal(GpuEngineSensorState.Running, status.State);
            Assert.Equal(2, status.OpenFailures);
        }
        finally
        {
            sensor.Stop();
        }
    }

    [Fact]
    public void AFactoryThatThrowsIsAStateNotAnException()
    {
        var sensor = new GpuEngineSensor(() => throw new DllNotFoundException("pdh.dll"), Pid, periodMs: 5);

        Assert.True(sensor.Start());
        try
        {
            SpinWait.SpinUntil(() => sensor.Status.OpenFailures >= 2, TimeSpan.FromSeconds(15));
            var status = sensor.Status;
            Assert.True(status.OpenFailures >= 2);
            Assert.Equal(GpuEngineSensorState.Opening, status.State);
            Assert.Contains("DllNotFoundException", status.LastError);
            Assert.Contains("retrying", sensor.Describe());
        }
        finally
        {
            sensor.Stop();
        }
    }

    [Fact]
    public void StopDuringALongOpenBackoffReturnsPromptly()
    {
        var sensor = new GpuEngineSensor(
            () => new FakeReader((_, _) => (GpuCounterReadResult.Valid, null), openError: "unavailable"),
            Pid,
            periodMs: 60_000);

        Assert.True(sensor.Start());
        SpinWait.SpinUntil(() => sensor.Status.OpenFailures >= 1, TimeSpan.FromSeconds(15));

        var stopwatch = Stopwatch.StartNew();
        sensor.Stop();
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(3), "Stop waited out the retry backoff");
        Assert.Equal(GpuEngineSensorState.Stopped, sensor.Status.State);
    }

    [Fact]
    public void StartIsIdempotentStopIsHarmlessAndAStoppedSensorForgetsItsDedicatedFigure()
    {
        var sensor = new GpuEngineSensor(() => ValidReader(Engine(Pid, "3D", 5)), Pid, periodMs: 5);
        sensor.Stop();
        Assert.Equal("gpu_engines: stopped", sensor.Describe());

        Assert.True(sensor.Start());
        Assert.False(sensor.Start());
        SpinWait.SpinUntil(() => sensor.Status.DedicatedBytes is not null, TimeSpan.FromSeconds(15));
        Assert.NotNull(sensor.Status.DedicatedBytes);
        Assert.StartsWith("gpu_engines: running", sensor.Describe());

        sensor.Stop();
        sensor.Stop();
        var stopped = sensor.Status;
        Assert.Equal(GpuEngineSensorState.Stopped, stopped.State);
        Assert.Null(stopped.DedicatedBytes);

        // And it restarts cleanly.
        Assert.True(sensor.Start());
        SpinWait.SpinUntil(() => sensor.Status.DedicatedBytes is not null, TimeSpan.FromSeconds(15));
        Assert.NotNull(sensor.Status.DedicatedBytes);
        sensor.Stop();
    }
}
