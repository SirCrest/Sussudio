using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Sussudio.Tests;

public sealed class StatsUiSamplerTests
{
    [Fact]
    public void FullscreenDockVisibilityControlsSamplingWithoutChangingThePreference()
    {
        var readSource = typeof(global::Program).GetMethod("ReadRepoFile", BindingFlags.NonPublic | BindingFlags.Static)!;
        var source = (string)readSource.Invoke(null, new object[] { "Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs" })!;
        var visibility = global::Program.ExtractDeclaredMemberCode(source, "public void ApplyStatsVisibility(bool visible, bool immediate = false)");
        var show = global::Program.ExtractDeclaredMemberCode(source, "public void ShowDockPanel()");
        var hide = global::Program.ExtractDeclaredMemberCode(source, "public void HideDockPanel(bool immediate = false)");
        var demand = global::Program.ExtractDeclaredMemberCode(source, "private void RefreshActivity()");
        var apply = global::Program.ExtractDeclaredMemberCode(source, "private void ApplySample(StatsUiSample sample)");

        Assert.Contains("_dockVisible = visible;", visibility);
        Assert.Contains("RefreshActivity();", visibility);
        Assert.Contains("ApplyStatsVisibility(true)", show);
        Assert.Contains("ApplyStatsVisibility(false, immediate)", hide);
        Assert.DoesNotContain("StatsToggle.IsChecked", visibility);
        Assert.Contains("_dockVisible || IsFrameTimeOverlayVisible", demand);
        Assert.DoesNotContain("StatsToggle.IsChecked", demand);
        Assert.Contains("if (_dockVisible)", apply);
        Assert.DoesNotContain("StatsToggle.IsChecked", apply);
    }

    [Fact]
    public void ThreeConsumersShareEachSampleAndHealthRunsAtHalfTheLabelRate()
    {
        using var fixture = new SamplerFixture();
        var dock = new List<object>();
        var graph = new List<object>();
        var detached = new List<object>();
        using var first = fixture.Subscribe(dock.Add);
        using var second = fixture.Subscribe(graph.Add);
        using var third = fixture.Subscribe(detached.Add);

        Assert.Equal(1, fixture.HealthCalls);
        Assert.Equal(1, fixture.SnapshotCalls);
        Assert.Same(dock[0], graph[0]);
        Assert.Same(dock[0], detached[0]);

        fixture.AdvanceTo(250);
        Assert.Equal(1, fixture.HealthCalls);
        Assert.Equal(2, fixture.SnapshotCalls);
        Assert.False(Get<bool>(dock[1], "HealthUpdated"));
        Assert.Equal(0L, Get<long>(dock[1], "HealthCollectedTick"));
        Assert.Same(dock[1], graph[1]);
        Assert.Same(dock[1], detached[1]);

        fixture.AdvanceTo(500);
        Assert.Equal(2, fixture.HealthCalls);
        Assert.Equal(3, fixture.SnapshotCalls);
        Assert.True(Get<bool>(dock[2], "HealthUpdated"));
        Assert.Equal(new[] { true, false, true }, fixture.RefreshDetails);
        Assert.Same(dock[2], detached[2]);
        Assert.Equal(new[] { true }, fixture.DemandChanges);
    }

    [Fact]
    public void AnimationFrequencyTicksDoNotIncreaseCollectionRate()
    {
        using var fixture = new SamplerFixture();
        using var consumer = fixture.Subscribe(_ => { });
        for (var timestamp = 1; timestamp < 250; timestamp++)
        {
            fixture.AdvanceTo(timestamp);
        }
        Assert.Equal(1, fixture.HealthCalls);
        Assert.Equal(1, fixture.SnapshotCalls);
    }

    [Fact]
    public void HiddenConsumersStopCollectionAndReopeningRefreshesImmediately()
    {
        using var fixture = new SamplerFixture();
        fixture.AdvanceTo(500);
        Assert.Equal(0, fixture.HealthCalls);
        var first = fixture.Subscribe(_ => { });
        var second = fixture.Subscribe(_ => { });
        first.Dispose();
        fixture.AdvanceTo(750);
        Assert.Equal(2, fixture.SnapshotCalls);

        second.Dispose();
        fixture.AdvanceTo(1500);
        Assert.Equal(2, fixture.SnapshotCalls);
        Assert.Equal(new[] { true, false }, fixture.DemandChanges);

        using var reopened = fixture.Subscribe(_ => { });
        Assert.Equal(3, fixture.SnapshotCalls);
        Assert.True(fixture.RefreshDetails[^1]);
    }

    [Fact]
    public void EpochChangeInvalidatesCachedHealthBeforeTheNextConsumerReceivesIt()
    {
        using var fixture = new SamplerFixture();
        var samples = new List<object>();
        using var first = fixture.Subscribe(samples.Add);
        fixture.Epoch++;
        var newConsumerSamples = new List<object>();
        using var second = fixture.Subscribe(newConsumerSamples.Add);

        Assert.Equal(2, fixture.HealthCalls);
        Assert.Single(newConsumerSamples);
        Assert.Equal(fixture.Epoch, Get<long>(newConsumerSamples[0], "CaptureSessionEpoch"));
        Assert.Same(samples[1], newConsumerSamples[0]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CollectionCrossingAnEpochBoundaryIsDiscarded(bool changeDuringHealth)
    {
        using var fixture = new SamplerFixture();
        var samples = new List<object>();
        if (changeDuringHealth)
        {
            fixture.DuringHealth = () => fixture.Epoch++;
        }
        else
        {
            fixture.DuringSnapshot = () => fixture.Epoch++;
        }
        using var consumer = fixture.Subscribe(samples.Add);
        Assert.Empty(samples);

        fixture.DuringHealth = null;
        fixture.DuringSnapshot = null;
        fixture.AdvanceTo(500);
        Assert.Single(samples);
        Assert.Equal(fixture.Epoch, Get<long>(samples[0], "CaptureSessionEpoch"));
        Assert.True(Get<bool>(samples[0], "HealthUpdated"));
    }

    [Fact]
    public void ProducerReadFailureDoesNotEscapeTheTimerAndTheNextTickRecovers()
    {
        using var fixture = new SamplerFixture { FailEpochRead = true };
        var received = new List<object>();
        using var consumer = fixture.Subscribe(received.Add);
        Assert.Empty(received);
        Assert.Contains(fixture.Logs, message => message.Contains("STATS_SAMPLE_FAIL", StringComparison.Ordinal));

        fixture.FailEpochRead = false;
        fixture.AdvanceTo(250);
        Assert.Single(received);
    }

    [Fact]
    public void ClosingOrFailingConsumerDoesNotSkipTheOtherConsumer()
    {
        using var fixture = new SamplerFixture();
        IDisposable? first = null;
        var closeFirst = false;
        first = fixture.Subscribe(_ =>
        {
            if (closeFirst)
            {
                first!.Dispose();
            }
        });
        using var failing = fixture.Subscribe(_ => throw new InvalidOperationException("Presentation failed."));
        var received = new List<object>();
        using var remaining = fixture.Subscribe(received.Add);
        closeFirst = true;

        fixture.AdvanceTo(250);

        Assert.Equal(2, received.Count);
        Assert.Equal(2, fixture.SnapshotCalls);
        Assert.Contains(fixture.Logs, message => message.Contains("STATS_CONSUMER_FAIL", StringComparison.Ordinal));
    }

    private static T Get<T>(object instance, string name)
        => (T)instance.GetType().GetProperty(name)!.GetValue(instance)!;

    private sealed class SamplerFixture : IDisposable
    {
        private readonly object _sampler;
        private readonly Type _samplerType;

        public SamplerFixture()
        {
            var assembly = SussudioAssembly.Load();
            _samplerType = assembly.GetType("Sussudio.Controllers.StatsUiSampler", throwOnError: true)!;
            var healthType = assembly.GetType("Sussudio.Models.CaptureHealthSnapshot", throwOnError: true)!;
            var snapshotType = assembly.GetType("Sussudio.ViewModels.StatsSnapshot", throwOnError: true)!;
            var constructor = _samplerType.GetConstructors().Single();
            var parameters = constructor.GetParameters();
            _sampler = constructor.Invoke(new object[]
            {
                new Func<long>(() => FailEpochRead ? throw new InvalidOperationException("Producer unavailable.") : Epoch),
                MakeDelegate(parameters[1].ParameterType, _ =>
                {
                    HealthCalls++;
                    var epoch = Epoch;
                    DuringHealth?.Invoke();
                    var health = Activator.CreateInstance(healthType)!;
                    healthType.GetProperty("CaptureSessionEpoch")!.SetValue(health, epoch);
                    return health;
                }),
                MakeDelegate(parameters[2].ParameterType, args =>
                {
                    SnapshotCalls++;
                    RefreshDetails.Add((bool)args[1]!);
                    DuringSnapshot?.Invoke();
                    return RuntimeHelpers.GetUninitializedObject(snapshotType);
                }),
                new Action<string>(Logs.Add),
                new Func<long>(() => Tick),
                1000L
            });
            _samplerType.GetEvent("DemandChanged")!.AddEventHandler(_sampler, new Action<bool>(DemandChanges.Add));
        }

        public long Epoch { get; set; } = 1;
        public bool FailEpochRead { get; set; }
        public long Tick { get; private set; }
        public int HealthCalls { get; private set; }
        public int SnapshotCalls { get; private set; }
        public Action? DuringHealth { get; set; }
        public Action? DuringSnapshot { get; set; }
        public List<bool> RefreshDetails { get; } = new();
        public List<bool> DemandChanges { get; } = new();
        public List<string> Logs { get; } = new();

        public IDisposable Subscribe(Action<object> receiveSample)
        {
            var subscribe = _samplerType.GetMethod("Subscribe")!;
            var callback = MakeDelegate(subscribe.GetParameters()[0].ParameterType, args =>
            {
                receiveSample(args[0]!);
                return null;
            });
            return (IDisposable)subscribe.Invoke(_sampler, new object[] { callback })!;
        }

        public void AdvanceTo(long tick)
        {
            Tick = tick;
            _samplerType.GetMethod("Tick")!.Invoke(_sampler, null);
        }

        public void Dispose() => ((IDisposable)_sampler).Dispose();
    }

    private static Delegate MakeDelegate(Type delegateType, Func<object?[], object?> body)
    {
        var invoke = delegateType.GetMethod("Invoke")!;
        var parameters = invoke.GetParameters()
            .Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name))
            .ToArray();
        var boxedArguments = Expression.NewArrayInit(typeof(object), parameters.Select(parameter => Expression.Convert(parameter, typeof(object))));
        var call = Expression.Invoke(Expression.Constant(body), boxedArguments);
        Expression result = invoke.ReturnType == typeof(void)
            ? Expression.Block(call, Expression.Empty())
            : Expression.Convert(call, invoke.ReturnType);
        return Expression.Lambda(delegateType, result, parameters).Compile();
    }
}
