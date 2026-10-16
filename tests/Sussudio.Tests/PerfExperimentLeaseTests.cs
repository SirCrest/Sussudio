using Sussudio.Services.Performance;
using Xunit;

namespace Sussudio.Tests;

// Failure modes of the perf experiment lease (docs/architecture/performance-telemetry.md section 2.12), written before
// the lease. A perf experiment changes process-wide tuning knobs from a pipe client that can die, stall or race
// another client, and the app is the only party that sees all of them, so the app owns who may change an
// experiment's knobs and puts them back when nobody else will:
//
//  1. Concurrent start: a second experiment (another client, another id) asks for the lease while one holds it. It
//     is refused with the holder's description, knobs and expiry; nothing changes. Two acquires racing on two
//     threads produce exactly one holder.
//  2. Lost acquire reply: the runner retries with the same id. That is the same lease: success, the expiry moves,
//     and the baseline stays the one captured the first time, not the experiment values now in place.
//  3. Knob changed by hand mid-run: a set or reset of a leased knob without the lease id, or with another id, is
//     refused, leaves the value alone, and says who holds it and how to end it. The holder's own changes pass, and
//     knobs outside the lease stay settable.
//  4. Crash without restore: the holder stops renewing. At expiry the app restores every leased knob to its
//     acquire-time value (by reset when that was the launch value, by value otherwise), records and logs what it
//     restored, and frees the lease for the next experiment. Knobs already at their baseline are not written.
//  5. Lease gone: a renew after expiry, after a forced release or with an id the app never saw (it restarted) is
//     refused and says which of those happened.
//  6. Ending: the holder's release frees the lease without touching knobs (the runner restores and reads back
//     itself) and is harmless twice; a release by anyone else is refused unless forced, and a forced release
//     restores the knobs exactly like an expiry, for an operator ending a wedged experiment.
//  7. Timer races: an expiry callback that fires after a renew moved the deadline must not end the lease. A callback
//     that runs late (a thread pool starved by the load being measured) must not let an overdue lease be renewed,
//     keep refusing another experiment or a hand change, or show as active; when it finally runs it must not end the
//     next holder's lease or restore a second time.
//  8. Malformed requests (unknown knob, empty or over-long or control-character id or holder, TTL out of range) are
//     rejected, never clamped, and take no lease.
public sealed class PerfExperimentLeaseTests
{
    private static readonly TuningKnobDefinition DepthKnob = new(
        "SUSSUDIO_TEST_DEPTH", TuningKnobType.Int, "3", 1, 60, null, TuningKnobApply.PreviewRestart, "Test.cs", "depth");

    private static readonly TuningKnobDefinition PollKnob = new(
        "SUSSUDIO_TEST_POLL", TuningKnobType.Int, "500", 100, 10000, null, TuningKnobApply.Live, "Test.cs", "poll");

    private static readonly TuningKnobDefinition OtherKnob = new(
        "SUSSUDIO_TEST_OTHER", TuningKnobType.Int, "8", 1, 64, null, TuningKnobApply.Live, "Test.cs", "other");

    private const string IdA = "lease-a";
    private const string IdB = "lease-b";

    private sealed class FakeEnvironment : ITuningKnobEnvironment
    {
        public Dictionary<string, string?> Values { get; } = new(StringComparer.Ordinal);

        public List<string> Writes { get; } = new();

        public string? Get(string name) => Values.TryGetValue(name, out var value) ? value : null;

        public void Set(string name, string? value)
        {
            Writes.Add(name);
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

    // Virtual time: the lease's expiry timer fires only when a test advances the clock past its due time, or when a
    // test fires it by hand to simulate a callback that was already queued when a renew moved the deadline.
    private sealed class ManualTime : TimeProvider
    {
        private readonly List<ManualTimer> _timers = new();

        public DateTimeOffset Now { get; private set; } = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state);
            timer.Change(dueTime, period);
            _timers.Add(timer);
            return timer;
        }

        public void Advance(TimeSpan by)
        {
            Now += by;
            foreach (var timer in _timers.ToList())
            {
                timer.FireIfDue();
            }
        }

        // A starved thread pool: the clock passes a timer's due time but its callback has not run yet.
        public void AdvanceWithoutFiring(TimeSpan by) => Now += by;

        public void FireAllNow()
        {
            foreach (var timer in _timers.ToList())
            {
                timer.Fire();
            }
        }

        private sealed class ManualTimer : ITimer
        {
            private readonly ManualTime _time;
            private readonly TimerCallback _callback;
            private readonly object? _state;
            private DateTimeOffset? _dueAt;

            public ManualTimer(ManualTime time, TimerCallback callback, object? state)
            {
                _time = time;
                _callback = callback;
                _state = state;
            }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                _dueAt = dueTime == Timeout.InfiniteTimeSpan ? null : _time.Now + dueTime;
                return true;
            }

            public void FireIfDue()
            {
                if (_dueAt is { } due && _time.Now >= due)
                {
                    Fire();
                }
            }

            public void Fire()
            {
                _dueAt = null;
                _callback(_state);
            }

            public void Dispose() => _dueAt = null;

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }

    private sealed record Rig(PerfExperimentLease Lease, TuningKnobStore Store, FakeEnvironment Env, ManualTime Time, List<string> Log);

    private static Rig Create(Action<FakeEnvironment>? seed = null)
    {
        var env = new FakeEnvironment();
        seed?.Invoke(env);
        var log = new List<string>();
        var store = new TuningKnobStore(new[] { DepthKnob, PollKnob, OtherKnob }, env, log.Add);
        store.CaptureLaunchValues();
        var time = new ManualTime();
        return new Rig(new PerfExperimentLease(store, time, log.Add), store, env, time, log);
    }

    private static PerfExperimentLeaseResult AcquireA(Rig rig, params string[] knobs)
        => rig.Lease.Acquire(IdA, "ssctl pid 1: experiment 'a'", knobs, 60);

    // ---- 1. concurrent start -------------------------------------------------------------------------------------

    [Fact]
    public void ASecondExperimentIsRefusedWithTheHolderAndNothingChanges()
    {
        var rig = Create();
        Assert.True(AcquireA(rig, DepthKnob.Name).Success);
        Assert.True(rig.Lease.ChangeKnob(DepthKnob.Name, "5", reset: false, IdA).Success);
        rig.Env.Writes.Clear();

        var second = rig.Lease.Acquire(IdB, "McpServer pid 2: experiment 'b'", new[] { PollKnob.Name }, 60);

        Assert.False(second.Success);
        Assert.Equal(PerfExperimentLeaseFailure.Held, second.Failure);
        Assert.Contains("experiment 'a'", second.Error);
        Assert.Contains(DepthKnob.Name, second.Error);
        Assert.NotNull(second.Active);
        Assert.Equal("ssctl pid 1: experiment 'a'", second.Active!.Holder);
        Assert.Empty(rig.Env.Writes);
        Assert.Equal("5", rig.Env.Get(DepthKnob.Name));
        Assert.True(rig.Lease.ChangeKnob(PollKnob.Name, "250", reset: false, IdB).Success);
    }

    [Fact]
    public async Task TwoAcquiresRacingProduceExactlyOneHolder()
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var rig = Create();
            using var start = new ManualResetEventSlim(false);
            var a = Task.Run(() => { start.Wait(); return rig.Lease.Acquire(IdA, "a", new[] { DepthKnob.Name }, 60); });
            var b = Task.Run(() => { start.Wait(); return rig.Lease.Acquire(IdB, "b", new[] { DepthKnob.Name }, 60); });
            start.Set();
            var results = await Task.WhenAll(a, b);
            Assert.Equal(1, results.Count(result => result.Success));
            Assert.Equal(1, results.Count(result => result.Failure == PerfExperimentLeaseFailure.Held));
        }
    }

    [Fact]
    public void ALeaseWithNoKnobsStillExcludesOtherExperiments()
    {
        var rig = Create();
        Assert.True(rig.Lease.Acquire(IdA, "a", Array.Empty<string>(), 60).Success);
        Assert.Equal(PerfExperimentLeaseFailure.Held, rig.Lease.Acquire(IdB, "b", Array.Empty<string>(), 60).Failure);
    }

    // ---- 2. lost acquire reply ------------------------------------------------------------------------------------

    [Fact]
    public void ReacquiringWithTheSameIdKeepsTheFirstBaselineAndMovesTheExpiry()
    {
        var rig = Create(env => env.Values[DepthKnob.Name] = "4");
        var first = AcquireA(rig, DepthKnob.Name);
        Assert.Equal("4", Assert.Single(first.Baseline).Value);
        Assert.True(rig.Lease.ChangeKnob(DepthKnob.Name, "9", reset: false, IdA).Success);
        rig.Time.Advance(TimeSpan.FromSeconds(50));

        var again = AcquireA(rig, DepthKnob.Name);

        Assert.True(again.Success);
        Assert.Equal("4", Assert.Single(again.Baseline).Value);
        Assert.Equal(rig.Time.Now.AddSeconds(60), again.Active!.ExpiresUtc);
        rig.Time.Advance(TimeSpan.FromSeconds(30));
        Assert.NotNull(rig.Lease.Status().Active);
        Assert.Equal("9", rig.Env.Get(DepthKnob.Name));
    }

    [Fact]
    public void TheBaselineReportsValueAndLaunchForEachLeasedKnob()
    {
        var rig = Create(env => env.Values[PollKnob.Name] = "700");
        Assert.True(rig.Store.Set(DepthKnob.Name, "7").Success);

        var result = rig.Lease.Acquire(IdA, "a", new[] { DepthKnob.Name.ToLowerInvariant(), PollKnob.Name }, 60);

        Assert.True(result.Success);
        Assert.Equal(new[] { DepthKnob.Name, PollKnob.Name }, result.Active!.Knobs);
        var depth = result.Baseline.Single(knob => knob.Name == DepthKnob.Name);
        Assert.Equal("7", depth.Value);
        Assert.Null(depth.Launch);
        var poll = result.Baseline.Single(knob => knob.Name == PollKnob.Name);
        Assert.Equal("700", poll.Value);
        Assert.Equal("700", poll.Launch);
    }

    // ---- 3. knob changed by hand mid-run --------------------------------------------------------------------------

    [Theory]
    [InlineData(null, false)]
    [InlineData(null, true)]
    [InlineData(IdB, false)]
    [InlineData(IdB, true)]
    public void AHandChangeToALeasedKnobIsRefusedAndNamesTheHolder(string? leaseId, bool reset)
    {
        var rig = Create();
        Assert.True(AcquireA(rig, DepthKnob.Name).Success);
        Assert.True(rig.Lease.ChangeKnob(DepthKnob.Name, "5", reset: false, IdA).Success);
        rig.Env.Writes.Clear();

        var result = rig.Lease.ChangeKnob(DepthKnob.Name.ToLowerInvariant(), reset ? null : "2", reset, leaseId);

        Assert.False(result.Success);
        Assert.Equal(TuningKnobFailure.Leased, result.Failure);
        Assert.Equal(DepthKnob.Name, result.Name);
        Assert.Contains("experiment 'a'", result.Error);
        Assert.Contains("release --force", result.Error);
        Assert.Empty(rig.Env.Writes);
        Assert.Equal("5", rig.Env.Get(DepthKnob.Name));
        Assert.Contains(rig.Log, line => line.StartsWith("TUNING_KNOB_REFUSED", StringComparison.Ordinal) && line.Contains(DepthKnob.Name));
    }

    [Fact]
    public void TheHolderAndKnobsOutsideTheLeaseStaySettable()
    {
        var rig = Create();
        Assert.True(AcquireA(rig, DepthKnob.Name).Success);

        Assert.True(rig.Lease.ChangeKnob(DepthKnob.Name, "5", reset: false, IdA).Success);
        Assert.True(rig.Lease.ChangeKnob(DepthKnob.Name, null, reset: true, IdA).Success);
        Assert.True(rig.Lease.ChangeKnob(PollKnob.Name, "250", reset: false, null).Success);
        Assert.Equal("250", rig.Env.Get(PollKnob.Name));
    }

    [Fact]
    public void WithNoLeaseAnyIdIsIgnoredAndValidationStillApplies()
    {
        var rig = Create();
        Assert.True(rig.Lease.ChangeKnob(DepthKnob.Name, "5", reset: false, "stale-id").Success);
        var outOfRange = rig.Lease.ChangeKnob(DepthKnob.Name, "500", reset: false, null);
        Assert.Equal(TuningKnobFailure.OutOfRange, outOfRange.Failure);
        Assert.Equal(TuningKnobFailure.MissingValue, rig.Lease.ChangeKnob(DepthKnob.Name, null, reset: false, null).Failure);
        Assert.Equal(TuningKnobFailure.UnknownKnob, rig.Lease.ChangeKnob("SUSSUDIO_NOPE", "1", reset: false, null).Failure);
    }

    // ---- 4. crash without restore ---------------------------------------------------------------------------------

    [Fact]
    public void AtExpiryTheAppRestoresEveryLeasedKnobAndFreesTheLease()
    {
        var rig = Create(env => env.Values[PollKnob.Name] = "700");
        Assert.True(rig.Store.Set(OtherKnob.Name, "16").Success);
        Assert.True(AcquireA(rig, DepthKnob.Name, PollKnob.Name, OtherKnob.Name).Success);
        Assert.True(rig.Lease.ChangeKnob(DepthKnob.Name, "9", reset: false, IdA).Success);
        Assert.True(rig.Lease.ChangeKnob(PollKnob.Name, "250", reset: false, IdA).Success);
        Assert.True(rig.Lease.ChangeKnob(OtherKnob.Name, "32", reset: false, IdA).Success);
        rig.Env.Writes.Clear();

        rig.Time.Advance(TimeSpan.FromSeconds(59));
        Assert.NotNull(rig.Lease.Status().Active);
        rig.Time.Advance(TimeSpan.FromSeconds(1));

        var status = rig.Lease.Status();
        Assert.Null(status.Active);
        Assert.Null(rig.Env.Get(DepthKnob.Name));
        Assert.Equal("700", rig.Env.Get(PollKnob.Name));
        Assert.Equal("16", rig.Env.Get(OtherKnob.Name));
        Assert.NotNull(status.LastEnded);
        Assert.Equal(PerfExperimentLeaseEndReason.Expired, status.LastEnded!.Reason);
        Assert.Equal(3, status.LastEnded.Restored.Count);
        Assert.Empty(status.LastEnded.Errors);
        Assert.Contains(rig.Log, line => line.StartsWith("PERF_EXPERIMENT_LEASE_EXPIRED", StringComparison.Ordinal));
        Assert.True(rig.Lease.Acquire(IdB, "b", new[] { DepthKnob.Name }, 60).Success);
    }

    [Fact]
    public void AtExpiryKnobsAlreadyAtTheirBaselineAreNotWritten()
    {
        var rig = Create();
        Assert.True(AcquireA(rig, DepthKnob.Name, PollKnob.Name).Success);
        Assert.True(rig.Lease.ChangeKnob(DepthKnob.Name, "9", reset: false, IdA).Success);
        Assert.True(rig.Lease.ChangeKnob(DepthKnob.Name, null, reset: true, IdA).Success);
        rig.Env.Writes.Clear();

        rig.Time.Advance(TimeSpan.FromSeconds(61));

        Assert.Empty(rig.Env.Writes);
        Assert.Empty(rig.Lease.Status().LastEnded!.Restored);
    }

    [Fact]
    public void RenewingKeepsTheLeasePastItsFirstDeadline()
    {
        var rig = Create();
        Assert.True(AcquireA(rig, DepthKnob.Name).Success);
        for (var i = 0; i < 10; i++)
        {
            rig.Time.Advance(TimeSpan.FromSeconds(40));
            Assert.True(rig.Lease.Renew(IdA, null).Success);
        }

        Assert.NotNull(rig.Lease.Status().Active);
    }

    // ---- 5. lease gone --------------------------------------------------------------------------------------------

    [Fact]
    public void ARenewAfterExpirySaysTheLeaseExpiredAndWasRestored()
    {
        var rig = Create();
        Assert.True(AcquireA(rig, DepthKnob.Name).Success);
        rig.Time.Advance(TimeSpan.FromSeconds(61));

        var renew = rig.Lease.Renew(IdA, null);

        Assert.False(renew.Success);
        Assert.Equal(PerfExperimentLeaseFailure.NotHeld, renew.Failure);
        Assert.Contains("expired", renew.Error);
    }

    [Fact]
    public void ARenewAfterAForcedReleaseSaysItWasForced()
    {
        var rig = Create();
        Assert.True(AcquireA(rig, DepthKnob.Name).Success);
        Assert.True(rig.Lease.Release(null, force: true).Success);

        var renew = rig.Lease.Renew(IdA, null);

        Assert.Equal(PerfExperimentLeaseFailure.NotHeld, renew.Failure);
        Assert.Contains("force", renew.Error);
    }

    [Fact]
    public void ARenewWithAnUnknownIdSaysTheAppNeverHeldIt()
    {
        var rig = Create();
        var renew = rig.Lease.Renew(IdA, null);
        Assert.Equal(PerfExperimentLeaseFailure.NotHeld, renew.Failure);
        Assert.Contains("restarted", renew.Error);

        Assert.True(AcquireA(rig).Success);
        var other = rig.Lease.Renew(IdB, null);
        Assert.Equal(PerfExperimentLeaseFailure.NotHeld, other.Failure);
        Assert.Contains("'a'", other.Error);
    }

    // ---- 6. ending --------------------------------------------------------------------------------------------------

    [Fact]
    public void TheHoldersReleaseFreesTheLeaseWithoutTouchingKnobsAndIsHarmlessTwice()
    {
        var rig = Create();
        Assert.True(AcquireA(rig, DepthKnob.Name).Success);
        Assert.True(rig.Lease.ChangeKnob(DepthKnob.Name, "9", reset: false, IdA).Success);
        rig.Env.Writes.Clear();

        Assert.True(rig.Lease.Release(IdA, force: false).Success);
        Assert.True(rig.Lease.Release(IdA, force: false).Success);

        Assert.Empty(rig.Env.Writes);
        Assert.Equal("9", rig.Env.Get(DepthKnob.Name));
        var status = rig.Lease.Status();
        Assert.Null(status.Active);
        Assert.Equal(PerfExperimentLeaseEndReason.Released, status.LastEnded!.Reason);
        rig.Time.Advance(TimeSpan.FromSeconds(120));
        Assert.Equal("9", rig.Env.Get(DepthKnob.Name));
    }

    [Fact]
    public void AReleaseByAnotherClientIsRefusedUnlessForcedAndForcingRestoresTheKnobs()
    {
        var rig = Create();
        Assert.True(AcquireA(rig, DepthKnob.Name).Success);
        Assert.True(rig.Lease.ChangeKnob(DepthKnob.Name, "9", reset: false, IdA).Success);

        var refused = rig.Lease.Release(IdB, force: false);
        Assert.False(refused.Success);
        Assert.Equal(PerfExperimentLeaseFailure.NotHeld, refused.Failure);
        Assert.Equal(PerfExperimentLeaseFailure.InvalidRequest, rig.Lease.Release(null, force: false).Failure);
        Assert.NotNull(rig.Lease.Status().Active);

        var forced = rig.Lease.Release(IdB, force: true);

        Assert.True(forced.Success);
        Assert.Null(rig.Env.Get(DepthKnob.Name));
        Assert.Equal(PerfExperimentLeaseEndReason.Forced, rig.Lease.Status().LastEnded!.Reason);
        Assert.Contains(rig.Log, line => line.StartsWith("PERF_EXPERIMENT_LEASE_FORCED", StringComparison.Ordinal));
    }

    [Fact]
    public void AForcedReleaseWithNoLeaseIsAHarmlessNoOp()
    {
        var rig = Create();
        var result = rig.Lease.Release(null, force: true);
        Assert.True(result.Success);
        Assert.Null(result.Active);
        Assert.Empty(rig.Env.Writes);
    }

    // ---- 7. timer races ---------------------------------------------------------------------------------------------

    [Fact]
    public void AnExpiryCallbackThatFiresAfterARenewDoesNotEndTheLease()
    {
        var rig = Create();
        Assert.True(AcquireA(rig, DepthKnob.Name).Success);
        Assert.True(rig.Lease.ChangeKnob(DepthKnob.Name, "9", reset: false, IdA).Success);
        rig.Time.Advance(TimeSpan.FromSeconds(55));
        Assert.True(rig.Lease.Renew(IdA, null).Success);

        rig.Time.FireAllNow();

        Assert.NotNull(rig.Lease.Status().Active);
        Assert.Equal("9", rig.Env.Get(DepthKnob.Name));
        rig.Time.Advance(TimeSpan.FromSeconds(61));
        Assert.Null(rig.Lease.Status().Active);
        Assert.Null(rig.Env.Get(DepthKnob.Name));
    }

    [Fact]
    public void AnOverdueLeaseCannotBeRenewedBeforeItsLateTimerRuns()
    {
        var rig = Create();
        Assert.True(AcquireA(rig, DepthKnob.Name).Success);
        Assert.True(rig.Lease.ChangeKnob(DepthKnob.Name, "9", reset: false, IdA).Success);
        rig.Time.AdvanceWithoutFiring(TimeSpan.FromSeconds(61));

        var renew = rig.Lease.Renew(IdA, null);

        Assert.False(renew.Success);
        Assert.Equal(PerfExperimentLeaseFailure.NotHeld, renew.Failure);
        Assert.Contains("expired", renew.Error);
        Assert.Null(rig.Env.Get(DepthKnob.Name));
    }

    [Fact]
    public void AnOverdueLeaseShowsAsExpiredBeforeItsLateTimerRuns()
    {
        var rig = Create();
        Assert.True(AcquireA(rig, DepthKnob.Name).Success);
        Assert.True(rig.Lease.ChangeKnob(DepthKnob.Name, "9", reset: false, IdA).Success);
        rig.Time.AdvanceWithoutFiring(TimeSpan.FromSeconds(60));

        var status = rig.Lease.Status();

        Assert.Null(status.Active);
        Assert.Equal(PerfExperimentLeaseEndReason.Expired, status.LastEnded!.Reason);
        Assert.Null(rig.Env.Get(DepthKnob.Name));
    }

    [Fact]
    public void AnOverdueLeaseLetsTheNextExperimentInAndTheLateTimerLeavesItsLeaseAlone()
    {
        var rig = Create();
        Assert.True(AcquireA(rig, DepthKnob.Name).Success);
        Assert.True(rig.Lease.ChangeKnob(DepthKnob.Name, "9", reset: false, IdA).Success);
        rig.Time.AdvanceWithoutFiring(TimeSpan.FromSeconds(61));

        var next = rig.Lease.Acquire(IdB, "b", new[] { DepthKnob.Name }, 60);

        Assert.True(next.Success, next.Error);
        Assert.Null(Assert.Single(next.Baseline).Value);
        Assert.True(rig.Lease.ChangeKnob(DepthKnob.Name, "5", reset: false, IdB).Success);
        rig.Time.FireAllNow();
        Assert.NotNull(rig.Lease.Status().Active);
        Assert.Equal("5", rig.Env.Get(DepthKnob.Name));
    }

    [Fact]
    public void AHandChangeAfterTheDeadlineGoesThroughAndTheLateRestoreDoesNotUndoIt()
    {
        var rig = Create();
        Assert.True(AcquireA(rig, DepthKnob.Name).Success);
        Assert.True(rig.Lease.ChangeKnob(DepthKnob.Name, "9", reset: false, IdA).Success);
        rig.Time.AdvanceWithoutFiring(TimeSpan.FromSeconds(61));

        var change = rig.Lease.ChangeKnob(DepthKnob.Name, "7", reset: false, leaseId: null);

        Assert.True(change.Success, change.Error);
        rig.Time.FireAllNow();
        Assert.Equal("7", rig.Env.Get(DepthKnob.Name));
        Assert.Single(rig.Log, line => line.StartsWith("PERF_EXPERIMENT_LEASE_EXPIRED", StringComparison.Ordinal));
    }

    // ---- 8. malformed requests --------------------------------------------------------------------------------------

    [Theory]
    [InlineData(null, "holder", 60)]
    [InlineData("", "holder", 60)]
    [InlineData("id\u0001", "holder", 60)]
    [InlineData("id", null, 60)]
    [InlineData("id", "  ", 60)]
    [InlineData("id", "holder\n", 60)]
    [InlineData("id", "holder", 9)]
    [InlineData("id", "holder", 601)]
    public void AMalformedAcquireIsRejectedAndTakesNoLease(string? id, string? holder, int ttl)
    {
        var rig = Create();
        var result = rig.Lease.Acquire(id, holder, new[] { DepthKnob.Name }, ttl);
        Assert.False(result.Success);
        Assert.Equal(PerfExperimentLeaseFailure.InvalidRequest, result.Failure);
        Assert.Null(rig.Lease.Status().Active);
    }

    [Fact]
    public void OverLongIdsAndHoldersAreRejectedNotTruncated()
    {
        var rig = Create();
        Assert.Equal(
            PerfExperimentLeaseFailure.InvalidRequest,
            rig.Lease.Acquire(new string('x', PerfExperimentLease.MaxIdLength + 1), "h", Array.Empty<string>(), 60).Failure);
        Assert.Equal(
            PerfExperimentLeaseFailure.InvalidRequest,
            rig.Lease.Acquire("id", new string('h', PerfExperimentLease.MaxHolderLength + 1), Array.Empty<string>(), 60).Failure);
        Assert.Null(rig.Lease.Status().Active);
    }

    [Fact]
    public void AnUnknownKnobIsRejectedAndTakesNoLease()
    {
        var rig = Create();
        var result = rig.Lease.Acquire(IdA, "a", new[] { DepthKnob.Name, "SUSSUDIO_NOPE" }, 60);
        Assert.False(result.Success);
        Assert.Equal(PerfExperimentLeaseFailure.UnknownKnob, result.Failure);
        Assert.Contains("SUSSUDIO_NOPE", result.Error);
        Assert.Null(rig.Lease.Status().Active);
    }

    [Fact]
    public void ARenewCanChangeTheTtlWithinRangeOnly()
    {
        var rig = Create();
        Assert.True(AcquireA(rig).Success);
        Assert.Equal(PerfExperimentLeaseFailure.InvalidRequest, rig.Lease.Renew(IdA, 5).Failure);
        var renewed = rig.Lease.Renew(IdA, 300);
        Assert.True(renewed.Success);
        Assert.Equal(300, renewed.Active!.TtlSeconds);
        Assert.Equal(rig.Time.Now.AddSeconds(300), renewed.Active.ExpiresUtc);
    }
}
