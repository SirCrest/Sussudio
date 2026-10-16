using Sussudio.Services.Performance;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of demand-gated sensors (docs/architecture/performance-telemetry.md sections 2.8 and 5):
// knob text that is garbage, off/on beating demand in either direction, demand that flaps or repeats, a sensor
// that throws on start or stop, shutdown racing a late demand event, concurrent re-evaluation, a demand probe that
// throws on the render thread, and the Changed events that drive all of it (rejected writes, throwing handlers,
// handlers that re-enter the owner and would deadlock if raised under its lock).
public sealed class SensorDemandTests
{
    private sealed class FakeSensor : IDemandSensor
    {
        private int _starts;
        private int _stops;

        public FakeSensor(string name) => Name = name;

        public string Name { get; }

        public bool ThrowOnStart { get; set; }

        public bool ThrowOnStop { get; set; }

        public bool ThrowOnDescribe { get; set; }

        public int Starts => Volatile.Read(ref _starts);

        public int Stops => Volatile.Read(ref _stops);

        public bool Start()
        {
            Interlocked.Increment(ref _starts);
            if (ThrowOnStart)
            {
                throw new InvalidOperationException("start failed");
            }

            return true;
        }

        public void Stop()
        {
            Interlocked.Increment(ref _stops);
            if (ThrowOnStop)
            {
                throw new InvalidOperationException("stop failed");
            }
        }

        public string Describe() => ThrowOnDescribe ? throw new InvalidOperationException("describe failed") : Name + ": ok";
    }

    private sealed class DemandInputs
    {
        public TelemetrySensorMode Mode = TelemetrySensorMode.Auto;
        public int Windows;
        public bool Trace;
    }

    private static TelemetrySensorSupervisor Supervise(DemandInputs inputs, params IDemandSensor[] sensors) =>
        new(() => inputs.Mode, () => inputs.Windows, () => inputs.Trace, sensors);

    [Theory]
    [InlineData(null, TelemetrySensorMode.Auto)]
    [InlineData("", TelemetrySensorMode.Auto)]
    [InlineData("   ", TelemetrySensorMode.Auto)]
    [InlineData("garbage", TelemetrySensorMode.Auto)]
    [InlineData("maybe", TelemetrySensorMode.Auto)]
    [InlineData("auto", TelemetrySensorMode.Auto)]
    [InlineData("2", TelemetrySensorMode.Auto)]
    [InlineData("on", TelemetrySensorMode.On)]
    [InlineData("ON", TelemetrySensorMode.On)]
    [InlineData(" On ", TelemetrySensorMode.On)]
    [InlineData("1", TelemetrySensorMode.On)]
    [InlineData("true", TelemetrySensorMode.On)]
    [InlineData("TRUE", TelemetrySensorMode.On)]
    [InlineData("off", TelemetrySensorMode.Off)]
    [InlineData("OFF", TelemetrySensorMode.Off)]
    [InlineData("0", TelemetrySensorMode.Off)]
    [InlineData("false", TelemetrySensorMode.Off)]
    public void KnobTextNeverFailsAndAnythingUnrecognisedMeansAuto(string? text, TelemetrySensorMode expected)
    {
        Assert.Equal(expected, TelemetrySensorModes.Parse(text));
    }

    [Fact]
    public void WireNamesRoundTripThroughParse()
    {
        foreach (var mode in new[] { TelemetrySensorMode.Auto, TelemetrySensorMode.On, TelemetrySensorMode.Off })
        {
            Assert.Equal(mode, TelemetrySensorModes.Parse(TelemetrySensorModes.ToWire(mode)));
        }
    }

    [Theory]
    [InlineData(TelemetrySensorMode.Auto, 0, false, false)]
    [InlineData(TelemetrySensorMode.Auto, 1, false, true)]
    [InlineData(TelemetrySensorMode.Auto, 0, true, true)]
    [InlineData(TelemetrySensorMode.Auto, 3, true, true)]
    [InlineData(TelemetrySensorMode.On, 0, false, true)]
    [InlineData(TelemetrySensorMode.Off, 2, true, false)]
    [InlineData(TelemetrySensorMode.Off, 0, false, false)]
    public void OffBeatsDemandOnBeatsIdleAndAutoFollowsWindowsOrTrace(
        TelemetrySensorMode mode, int windows, bool trace, bool expected)
    {
        Assert.Equal(expected, SensorDemandPolicy.IsActive(mode, windows, trace));
    }

    [Theory]
    [InlineData(TelemetrySensorMode.On, TelemetrySensorMode.Off, 0, false, true)]
    [InlineData(TelemetrySensorMode.Off, TelemetrySensorMode.On, 2, true, false)]
    [InlineData(TelemetrySensorMode.Auto, TelemetrySensorMode.Auto, 1, false, true)]
    [InlineData(TelemetrySensorMode.Auto, TelemetrySensorMode.Auto, 0, true, true)]
    [InlineData(TelemetrySensorMode.Auto, TelemetrySensorMode.Auto, 0, false, false)]
    [InlineData(TelemetrySensorMode.Auto, TelemetrySensorMode.Off, 1, true, false)]
    [InlineData(TelemetrySensorMode.Auto, TelemetrySensorMode.On, 0, false, true)]
    public void GpuTimingKnobBeatsTheSensorsKnobAndAutoFollowsIt(
        TelemetrySensorMode gpuMode, TelemetrySensorMode sensorsMode, int windows, bool trace, bool expected)
    {
        Assert.Equal(expected, SensorDemandPolicy.IsGpuTimingActive(gpuMode, sensorsMode, windows, trace));
    }

    [Fact]
    public void ReportReasonNamesTheKnobTheWindowsOrTheTraceInThatPrecedence()
    {
        var none = Array.Empty<string>();
        var windows = new[] { "w1", "w2" };

        Assert.Equal(new SensorDemandDecision(false, "knob:off"), SensorDemandPolicy.Decide(TelemetrySensorMode.Off, windows, true));
        Assert.Equal(new SensorDemandDecision(true, "knob:on"), SensorDemandPolicy.Decide(TelemetrySensorMode.On, none, false));
        Assert.Equal(new SensorDemandDecision(true, "window:w1,w2"), SensorDemandPolicy.Decide(TelemetrySensorMode.Auto, windows, true));
        Assert.Equal(new SensorDemandDecision(true, "trace"), SensorDemandPolicy.Decide(TelemetrySensorMode.Auto, none, true));
        Assert.Equal(new SensorDemandDecision(false, "idle"), SensorDemandPolicy.Decide(TelemetrySensorMode.Auto, none, false));
    }

    [Fact]
    public void CachedDemandEvaluatesOncePerIntervalAndHoldsTheAnswerBetween()
    {
        var calls = 0;
        var answer = false;
        var demand = new CachedDemand(() => { calls++; return answer; }, intervalTicks: 100);

        Assert.False(demand.IsActive(0));
        Assert.Equal(1, calls);

        answer = true;
        Assert.False(demand.IsActive(50));
        Assert.False(demand.IsActive(99));
        Assert.Equal(1, calls);

        Assert.True(demand.IsActive(100));
        Assert.Equal(2, calls);
        Assert.True(demand.IsActive(150));
        Assert.Equal(2, calls);
    }

    [Fact]
    public void ADemandProbeThatThrowsKeepsTheLastAnswerAndIsNotRetriedEveryCall()
    {
        var calls = 0;
        var fail = false;
        var demand = new CachedDemand(
            () =>
            {
                calls++;
                return fail ? throw new InvalidOperationException("probe failed") : true;
            },
            intervalTicks: 100);

        Assert.True(demand.IsActive(0));
        fail = true;

        // The render thread must survive the throw and keep the previous answer; the failure is retried next interval.
        Assert.True(demand.IsActive(100));
        Assert.True(demand.IsActive(120));
        Assert.True(demand.IsActive(199));
        Assert.Equal(2, calls);
    }

    [Fact]
    public void SensorsStartOnceWhenDemandAppearsAndStopOnceWhenItEnds()
    {
        var inputs = new DemandInputs();
        var a = new FakeSensor("a");
        var b = new FakeSensor("b");
        var supervisor = Supervise(inputs, a, b);

        supervisor.Reevaluate();
        Assert.False(supervisor.IsActive);
        Assert.Equal(0, a.Starts);

        inputs.Windows = 1;
        supervisor.Reevaluate();
        supervisor.Reevaluate();
        inputs.Windows = 2;
        inputs.Trace = true;
        supervisor.Reevaluate();
        Assert.True(supervisor.IsActive);
        Assert.Equal(1, a.Starts);
        Assert.Equal(1, b.Starts);
        Assert.Equal(0, a.Stops);

        inputs.Windows = 0;
        supervisor.Reevaluate();
        Assert.True(supervisor.IsActive);
        Assert.Equal(0, a.Stops);

        inputs.Trace = false;
        supervisor.Reevaluate();
        supervisor.Reevaluate();
        Assert.False(supervisor.IsActive);
        Assert.Equal(1, a.Stops);
        Assert.Equal(1, b.Stops);
    }

    [Fact]
    public void AKnobFlippedWhileAWindowIsOpenStopsAndRestartsTheSensors()
    {
        var inputs = new DemandInputs { Windows = 1 };
        var sensor = new FakeSensor("a");
        var supervisor = Supervise(inputs, sensor);
        supervisor.Reevaluate();
        Assert.Equal(1, sensor.Starts);

        inputs.Mode = TelemetrySensorMode.Off;
        supervisor.Reevaluate();
        Assert.False(supervisor.IsActive);
        Assert.Equal(1, sensor.Stops);

        inputs.Mode = TelemetrySensorMode.Auto;
        supervisor.Reevaluate();
        Assert.True(supervisor.IsActive);
        Assert.Equal(2, sensor.Starts);

        inputs.Windows = 0;
        inputs.Mode = TelemetrySensorMode.On;
        supervisor.Reevaluate();
        Assert.True(supervisor.IsActive);
        Assert.Equal(2, sensor.Starts);
    }

    [Fact]
    public void ASensorThatThrowsOnStartOrStopIsRecordedAndDoesNotAffectTheOthers()
    {
        var inputs = new DemandInputs { Windows = 1 };
        var broken = new FakeSensor("broken") { ThrowOnStart = true, ThrowOnStop = true };
        var healthy = new FakeSensor("healthy");
        var supervisor = Supervise(inputs, broken, healthy);

        supervisor.Reevaluate();
        Assert.True(supervisor.IsActive);
        Assert.Equal(1, healthy.Starts);
        Assert.Contains("broken", supervisor.LastError);
        Assert.Contains("start", supervisor.LastError);

        inputs.Windows = 0;
        supervisor.Reevaluate();
        Assert.Equal(1, healthy.Stops);
        Assert.Contains("stop", supervisor.LastError);
    }

    [Fact]
    public void ShutdownStopsEverythingAndALaterDemandEventStartsNothing()
    {
        var inputs = new DemandInputs { Windows = 1 };
        var sensor = new FakeSensor("a");
        var supervisor = Supervise(inputs, sensor);
        supervisor.Reevaluate();

        supervisor.Shutdown();
        supervisor.Shutdown();
        Assert.False(supervisor.IsActive);
        Assert.Equal(1, sensor.Stops);

        // An event that races shutdown (a window began a moment before the app closed) must not resurrect a sensor.
        inputs.Windows = 2;
        supervisor.Reevaluate();
        Assert.Equal(1, sensor.Starts);
        Assert.False(supervisor.IsActive);
    }

    [Fact]
    public void ConcurrentReevaluationStartsEachSensorExactlyOnce()
    {
        var inputs = new DemandInputs { Windows = 1 };
        var sensor = new FakeSensor("a");
        var supervisor = Supervise(inputs, sensor);

        Parallel.For(0, 256, new ParallelOptions { MaxDegreeOfParallelism = 16 }, _ => supervisor.Reevaluate());
        Assert.Equal(1, sensor.Starts);

        inputs.Windows = 0;
        Parallel.For(0, 256, new ParallelOptions { MaxDegreeOfParallelism = 16 }, _ => supervisor.Reevaluate());
        Assert.Equal(1, sensor.Stops);
    }

    [Fact]
    public void DescribeSurvivesASensorThatThrows()
    {
        var supervisor = Supervise(
            new DemandInputs(),
            new FakeSensor("good"),
            new FakeSensor("bad") { ThrowOnDescribe = true });

        var lines = supervisor.Describe();
        Assert.Equal(2, lines.Count);
        Assert.Equal("good: ok", lines[0]);
        Assert.StartsWith("bad: describe failed", lines[1]);
    }

    [Fact]
    public void WindowSetRaisesChangedOnlyWhenTheOpenCountChanges()
    {
        var windows = new PerfWindowSet<int>(new PerfRegistry(), () => 0);
        var raised = 0;
        windows.Changed += () => raised++;

        windows.Begin("w");
        Assert.Equal(1, raised);
        Assert.Equal(1, windows.OpenCount);

        // A restart keeps the count, so it must not look like new demand.
        Assert.Equal(PerfWindowBeginOutcome.Restarted, windows.Begin("w").Outcome);
        Assert.Equal(1, raised);

        Assert.Equal(PerfWindowBeginOutcome.InvalidName, windows.Begin("").Outcome);
        Assert.Equal(1, raised);

        windows.Begin("x");
        Assert.Equal(2, raised);
        Assert.Equal(2, windows.OpenCount);

        Assert.Null(windows.End("unknown"));
        Assert.False(windows.Cancel("unknown"));
        Assert.Equal(2, raised);

        Assert.NotNull(windows.End("w"));
        Assert.Equal(3, raised);
        Assert.True(windows.Cancel("x"));
        Assert.Equal(4, raised);
        Assert.Equal(0, windows.OpenCount);
    }

    [Fact]
    public void WindowSetLimitRejectionDoesNotRaiseChanged()
    {
        var windows = new PerfWindowSet<int>(new PerfRegistry(), () => 0);
        for (var i = 0; i < PerfWindowSet<int>.MaxWindows; i++)
        {
            windows.Begin("w" + i);
        }

        var raised = 0;
        windows.Changed += () => raised++;
        Assert.Equal(PerfWindowBeginOutcome.LimitReached, windows.Begin("one-too-many").Outcome);
        Assert.Equal(0, raised);
        Assert.Equal(PerfWindowSet<int>.MaxWindows, windows.OpenCount);
    }

    [Fact]
    public void AThrowingChangedHandlerDoesNotBreakTheWindowSet()
    {
        var windows = new PerfWindowSet<int>(new PerfRegistry(), () => 0);
        windows.Changed += () => throw new InvalidOperationException("handler failed");

        Assert.Equal(PerfWindowBeginOutcome.Started, windows.Begin("w").Outcome);
        Assert.NotNull(windows.End("w"));
        Assert.Equal(0, windows.OpenCount);
    }

    [Fact]
    public async Task AChangedHandlerMayCallBackIntoTheWindowSetWithoutDeadlocking()
    {
        var windows = new PerfWindowSet<int>(new PerfRegistry(), () => 0);
        var seen = new List<int>();
        windows.Changed += () =>
        {
            // Raised outside the gate: listing and counting here must not wait on the lock Begin or End holds.
            seen.Add(windows.List().Count);
            seen.Add(windows.OpenCount);
        };

        var work = Task.Run(() =>
        {
            windows.Begin("w");
            windows.End("w");
        });
        var finished = await Task.WhenAny(work, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.True(ReferenceEquals(work, finished), "Begin or End deadlocked against a re-entrant Changed handler");
        await work;
        Assert.Equal(new[] { 1, 1, 0, 0 }, seen);
    }

    [Fact]
    public void FrameTraceRecorderRaisesChangedOnStartAndOnAStopOfARunningTraceOnly()
    {
        var recorder = new FrameTraceRecorder(new PerfMarkerLog(), new PerfThreadNames());
        var raised = 0;
        recorder.Changed += () => raised++;

        recorder.Stop();
        Assert.Equal(0, raised);

        Assert.True(recorder.Start(0).Success);
        Assert.Equal(1, raised);

        Assert.False(recorder.Start(0).Success);
        Assert.Equal(1, raised);

        recorder.Stop();
        Assert.Equal(2, raised);
        recorder.Stop();
        Assert.Equal(2, raised);
    }

    [Fact]
    public void AThrowingChangedHandlerDoesNotBreakTheTraceRecorder()
    {
        var recorder = new FrameTraceRecorder(new PerfMarkerLog(), new PerfThreadNames());
        recorder.Changed += () => throw new InvalidOperationException("handler failed");

        Assert.True(recorder.Start(0).Success);
        Assert.True(recorder.IsEnabled);
        recorder.Stop();
        Assert.False(recorder.IsEnabled);
    }

    private sealed class DictionaryEnvironment : ITuningKnobEnvironment
    {
        public Dictionary<string, string?> Values { get; } = new(StringComparer.Ordinal);

        public string? Get(string name) => Values.TryGetValue(name, out var value) ? value : null;

        public void Set(string name, string? value)
        {
            if (value is null)
            {
                Values.Remove(name);
            }
            else
            {
                Values[name] = value;
            }
        }
    }

    [Fact]
    public void KnobStoreRaisesChangedAfterASuccessfulSetOrResetButNotAfterARejectedOne()
    {
        var env = new DictionaryEnvironment();
        var store = new TuningKnobStore(TuningKnobCatalog.Knobs, env, _ => { });
        var raised = 0;
        store.Changed += () => raised++;

        Assert.False(store.Set(TelemetrySensorModes.SensorsKnob, "maybe").Success);
        Assert.False(store.Set("SUSSUDIO_NOT_A_KNOB", "on").Success);
        Assert.Equal(0, raised);

        Assert.True(store.Set(TelemetrySensorModes.SensorsKnob, "off").Success);
        Assert.Equal(1, raised);
        Assert.Equal("off", env.Get(TelemetrySensorModes.SensorsKnob));

        Assert.True(store.Reset(TelemetrySensorModes.SensorsKnob).Success);
        Assert.Equal(2, raised);
        Assert.Null(env.Get(TelemetrySensorModes.SensorsKnob));
    }

    [Theory]
    [InlineData(TelemetrySensorModes.SensorsKnob)]
    [InlineData(TelemetrySensorModes.GpuTimingKnob)]
    public void TheAutoOnOffKnobsAreLiveAcceptEveryChoiceAndNormaliseItsCasing(string knob)
    {
        var env = new DictionaryEnvironment();
        var store = new TuningKnobStore(TuningKnobCatalog.Knobs, env, _ => { });

        foreach (var (input, expected) in new[] { ("ON", "on"), ("Off", "off"), ("AUTO", "auto") })
        {
            var result = store.Set(knob, input);
            Assert.True(result.Success, result.Error);
            Assert.Equal("live", result.Apply);
            Assert.Equal(expected, env.Get(knob));
            Assert.Equal(TelemetrySensorModes.Parse(expected), TelemetrySensorModes.Parse(env.Get(knob)));
        }

        Assert.Equal(TuningKnobFailure.WrongType, store.Set(knob, "maybe").Failure);
    }

    [Fact]
    public void AThrowingKnobChangedHandlerDoesNotFailTheSet()
    {
        var env = new DictionaryEnvironment();
        var store = new TuningKnobStore(TuningKnobCatalog.Knobs, env, _ => { });
        store.Changed += () => throw new InvalidOperationException("handler failed");

        Assert.True(store.Set(TelemetrySensorModes.GpuTimingKnob, "on").Success);
        Assert.Equal("on", env.Get(TelemetrySensorModes.GpuTimingKnob));
    }
}
