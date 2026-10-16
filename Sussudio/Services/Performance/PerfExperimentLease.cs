using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Sussudio.Services.Performance;

public enum PerfExperimentLeaseFailure
{
    None,
    InvalidRequest,
    UnknownKnob,
    /// <summary>Another experiment holds the lease.</summary>
    Held,
    /// <summary>The caller does not hold the lease: it expired, was forced off, or belongs to someone else.</summary>
    NotHeld,
}

public enum PerfExperimentLeaseEndReason
{
    /// <summary>The holder released it; knobs were left as they were (the holder restores them itself).</summary>
    Released,
    /// <summary>The holder stopped renewing; the app restored the leased knobs.</summary>
    Expired,
    /// <summary>Someone else ended it; the app restored the leased knobs.</summary>
    Forced,
}

/// <summary>A leased knob's value and launch value when the lease began; <c>null</c> is unset.</summary>
public sealed record PerfExperimentLeaseKnob(string Name, string? Value, string? Launch);

/// <summary>The active lease as other clients may see it. The lease id is the holder's capability and is never shown.</summary>
public sealed record PerfExperimentLeaseView(
    string Holder,
    IReadOnlyList<string> Knobs,
    DateTimeOffset AcquiredUtc,
    DateTimeOffset ExpiresUtc,
    int TtlSeconds);

/// <summary>How the last lease ended, and what the app restored when it ended without its holder.</summary>
public sealed record PerfExperimentLeaseEnd(
    string Holder,
    PerfExperimentLeaseEndReason Reason,
    DateTimeOffset EndedUtc,
    IReadOnlyList<string> Restored,
    IReadOnlyList<string> Errors);

public sealed record PerfExperimentLeaseResult(
    bool Success,
    PerfExperimentLeaseFailure Failure,
    string? Error,
    PerfExperimentLeaseView? Active,
    IReadOnlyList<PerfExperimentLeaseKnob> Baseline,
    PerfExperimentLeaseEnd? LastEnded);

/// <summary>
/// Gives one perf experiment at a time exclusive control of the tuning knobs it varies
/// (docs/architecture/performance-telemetry.md section 2.12). Every automation knob change goes through
/// <see cref="ChangeKnob"/>, which refuses a leased knob to anyone without the lease id, so a second experiment or a
/// hand-typed change cannot fight a running one. The holder renews within its TTL; a holder that stops (crashed,
/// killed, stalled) loses the lease at expiry, and the app restores every leased knob to its value when the lease
/// began, because nobody else will. The deadline is checked on every operation as well as by the timer, so a timer
/// that runs late never leaves an overdue lease in force. Control-plane only: one lock, held while a knob is written,
/// so a check and the write it allows can never interleave with another client's acquire. Lock order is lease, then
/// store.
/// </summary>
public sealed class PerfExperimentLease
{
    public const int DefaultTtlSeconds = 60;
    public const int MinTtlSeconds = 10;
    public const int MaxTtlSeconds = 600;
    public const int MaxIdLength = 64;
    public const int MaxHolderLength = 200;

    private const string UnsetText = "<unset>";

    private sealed record ActiveLease(
        string Id,
        string Holder,
        IReadOnlyList<string> Knobs,
        IReadOnlyList<PerfExperimentLeaseKnob> Baseline,
        DateTimeOffset AcquiredUtc,
        DateTimeOffset ExpiresUtc,
        int TtlSeconds);

    private readonly object _gate = new();
    private readonly TuningKnobStore _knobs;
    private readonly TimeProvider _time;
    private readonly Action<string> _log;
    private readonly ITimer _expiryTimer;
    private ActiveLease? _active;
    private PerfExperimentLeaseEnd? _lastEnded;
    private string? _lastEndedId;

    public PerfExperimentLease(TuningKnobStore knobs, TimeProvider time, Action<string> log)
    {
        _knobs = knobs;
        _time = time;
        _log = log;
        _expiryTimer = time.CreateTimer(_ => OnExpiryTimer(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public PerfExperimentLeaseResult Status()
    {
        lock (_gate)
        {
            ExpireIfDueLocked();
            return ResultLocked();
        }
    }

    /// <summary>
    /// Takes the lease, or renews it when <paramref name="id"/> already holds it (a retried acquire whose reply was
    /// lost): the expiry moves and the baseline stays the one captured the first time.
    /// </summary>
    public PerfExperimentLeaseResult Acquire(string? id, string? holder, IReadOnlyList<string>? knobs, int? ttlSeconds)
    {
        if (ValidateId(id) is { } idError)
        {
            return Refuse(PerfExperimentLeaseFailure.InvalidRequest, idError);
        }

        if (string.IsNullOrWhiteSpace(holder) || holder.Length > MaxHolderLength || holder.Any(char.IsControl))
        {
            return Refuse(
                PerfExperimentLeaseFailure.InvalidRequest,
                $"holder must be 1 to {MaxHolderLength} characters with no control characters; it says who runs the experiment.");
        }

        if (ValidateTtl(ttlSeconds, out var ttl) is { } ttlError)
        {
            return Refuse(PerfExperimentLeaseFailure.InvalidRequest, ttlError);
        }

        var names = new List<string>();
        foreach (var requested in knobs ?? Array.Empty<string>())
        {
            var state = _knobs.Find(requested);
            if (state is null)
            {
                return Refuse(PerfExperimentLeaseFailure.UnknownKnob, $"Unknown tuning knob '{requested}'. Use the knob listing for valid names.");
            }

            if (!names.Contains(state.Definition.Name, StringComparer.Ordinal))
            {
                names.Add(state.Definition.Name);
            }
        }

        lock (_gate)
        {
            ExpireIfDueLocked();
            var now = _time.GetUtcNow();
            if (_active is { } active && active.Id != id)
            {
                return Refuse(
                    PerfExperimentLeaseFailure.Held,
                    $"Another perf experiment holds the tuning-knob lease: {Describe(active, now)}. Wait for it to finish, "
                    + "or end it with 'ssctl perf lease release --force', which restores its knobs.");
            }

            if (_active is { } same)
            {
                _active = same with { ExpiresUtc = now.AddSeconds(ttl), TtlSeconds = ttl };
            }
            else
            {
                // The baseline is read here, under the lease, so a change made between the client's own read and this
                // acquire is part of what the experiment restores to.
                var baseline = names
                    .Select(name => _knobs.Find(name)!)
                    .Select(state => new PerfExperimentLeaseKnob(state.Definition.Name, state.Value, state.Launch))
                    .ToList();
                _active = new ActiveLease(id!, holder.Trim(), names, baseline, now, now.AddSeconds(ttl), ttl);
                _log(string.Create(
                    CultureInfo.InvariantCulture,
                    $"PERF_EXPERIMENT_LEASE_ACQUIRED holder='{_active.Holder}' knobs={JoinOrNone(names)} ttl={ttl}s"));
            }

            _expiryTimer.Change(TimeSpan.FromSeconds(ttl), Timeout.InfiniteTimeSpan);
            return ResultLocked();
        }
    }

    public PerfExperimentLeaseResult Renew(string? id, int? ttlSeconds)
    {
        if (ValidateId(id) is { } idError)
        {
            return Refuse(PerfExperimentLeaseFailure.InvalidRequest, idError);
        }

        lock (_gate)
        {
            ExpireIfDueLocked();
            if (_active is not { } active || active.Id != id)
            {
                return Refuse(PerfExperimentLeaseFailure.NotHeld, NotHeldReasonLocked(id!));
            }

            if (ValidateTtl(ttlSeconds ?? active.TtlSeconds, out var ttl) is { } ttlError)
            {
                return Refuse(PerfExperimentLeaseFailure.InvalidRequest, ttlError);
            }

            _active = active with { ExpiresUtc = _time.GetUtcNow().AddSeconds(ttl), TtlSeconds = ttl };
            _expiryTimer.Change(TimeSpan.FromSeconds(ttl), Timeout.InfiniteTimeSpan);
            return ResultLocked();
        }
    }

    /// <summary>
    /// The holder's release frees the lease and leaves the knobs alone (the runner restores and reads back itself);
    /// releasing a lease that already ended succeeds. <paramref name="force"/> ends anyone's lease and restores its
    /// knobs, for an operator ending a wedged experiment.
    /// </summary>
    public PerfExperimentLeaseResult Release(string? id, bool force)
    {
        if (!force && ValidateId(id) is { } idError)
        {
            return Refuse(PerfExperimentLeaseFailure.InvalidRequest, idError + " Use force to end another client's lease.");
        }

        lock (_gate)
        {
            ExpireIfDueLocked();
            if (_active is not { } active)
            {
                return ResultLocked();
            }

            if (force)
            {
                EndLocked(PerfExperimentLeaseEndReason.Forced);
                return ResultLocked();
            }

            if (active.Id != id)
            {
                return Refuse(
                    PerfExperimentLeaseFailure.NotHeld,
                    $"This client does not hold the lease: {Describe(active, _time.GetUtcNow())}. Use force to end it anyway; its knobs are restored.");
            }

            EndLocked(PerfExperimentLeaseEndReason.Released);
            return ResultLocked();
        }
    }

    /// <summary>
    /// Sets or resets a knob for an automation client. A knob held by an active lease is refused unless
    /// <paramref name="leaseId"/> is that lease's id; any other knob, or any knob while no lease is active, is passed to
    /// the store whatever the id says.
    /// </summary>
    public TuningKnobChangeResult ChangeKnob(string? name, string? value, bool reset, string? leaseId)
    {
        lock (_gate)
        {
            ExpireIfDueLocked();
            var state = _knobs.Find(name);
            if (state is not null && _active is { } active && active.Id != leaseId
                && active.Knobs.Contains(state.Definition.Name, StringComparer.Ordinal))
            {
                var now = _time.GetUtcNow();
                _log($"TUNING_KNOB_REFUSED name={state.Definition.Name} reason=leased holder='{active.Holder}'");
                return new TuningKnobChangeResult(
                    false,
                    TuningKnobFailure.Leased,
                    $"{state.Definition.Name} is held by a running perf experiment: {Describe(active, now)}. Wait for it to "
                    + "finish, or end it with 'ssctl perf lease release --force', which restores its knobs.",
                    state.Definition.Name,
                    null,
                    null,
                    null,
                    null);
            }

            return reset ? _knobs.Reset(name) : _knobs.Set(name, value);
        }
    }

    private void OnExpiryTimer()
    {
        lock (_gate)
        {
            if (_active is not { } active)
            {
                return;
            }

            // A renew can move the deadline after this callback was already queued; only a lease that is really past
            // its expiry ends here.
            var now = _time.GetUtcNow();
            if (now < active.ExpiresUtc)
            {
                _expiryTimer.Change(active.ExpiresUtc - now, Timeout.InfiniteTimeSpan);
                return;
            }

            EndLocked(PerfExperimentLeaseEndReason.Expired);
        }
    }

    // The timer's callback needs a thread-pool thread, and the pool can be starved by exactly the load an experiment
    // measures. So every operation first ends a lease that is past its deadline, as the timer would have: an overdue
    // lease is never renewed, never refuses another client and never shows as held. The late callback then finds
    // nothing due and re-arms for whoever holds the lease next.
    private void ExpireIfDueLocked()
    {
        if (_active is { } active && _time.GetUtcNow() >= active.ExpiresUtc)
        {
            EndLocked(PerfExperimentLeaseEndReason.Expired);
        }
    }

    private void EndLocked(PerfExperimentLeaseEndReason reason)
    {
        var active = _active!;
        _active = null;
        _expiryTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        var restored = new List<string>();
        var errors = new List<string>();
        if (reason != PerfExperimentLeaseEndReason.Released)
        {
            RestoreLocked(active, restored, errors);
        }

        _lastEnded = new PerfExperimentLeaseEnd(active.Holder, reason, _time.GetUtcNow(), restored, errors);
        _lastEndedId = active.Id;
        var tag = reason switch
        {
            PerfExperimentLeaseEndReason.Expired => "PERF_EXPERIMENT_LEASE_EXPIRED",
            PerfExperimentLeaseEndReason.Forced => "PERF_EXPERIMENT_LEASE_FORCED",
            _ => "PERF_EXPERIMENT_LEASE_RELEASED",
        };
        _log($"{tag} holder='{active.Holder}' restored={JoinOrNone(restored)}{(errors.Count > 0 ? " errors=" + string.Join(" | ", errors) : string.Empty)}");
    }

    // The holder is gone, so the app puts each leased knob back to its value when the lease began: by reset when that
    // was the launch value (which also covers "unset"), by value otherwise. A knob already there is not written.
    private void RestoreLocked(ActiveLease lease, List<string> restored, List<string> errors)
    {
        foreach (var baseline in lease.Baseline)
        {
            var current = _knobs.Find(baseline.Name);
            if (current is null || string.Equals(current.Value, baseline.Value, StringComparison.Ordinal))
            {
                continue;
            }

            var result = string.Equals(baseline.Value, current.Launch, StringComparison.Ordinal)
                ? _knobs.Reset(baseline.Name)
                : _knobs.Set(baseline.Name, baseline.Value);
            if (result.Success)
            {
                restored.Add($"{baseline.Name}: {current.Value ?? UnsetText} -> {baseline.Value ?? UnsetText} ({result.Note})");
            }
            else
            {
                errors.Add($"{baseline.Name}: {result.Error}");
            }
        }
    }

    private string NotHeldReasonLocked(string id)
    {
        if (_lastEnded is { } ended && _lastEndedId == id)
        {
            var when = ended.EndedUtc.ToString("O", CultureInfo.InvariantCulture);
            return ended.Reason switch
            {
                PerfExperimentLeaseEndReason.Expired =>
                    $"The lease expired at {when} without a renewal, and the app restored its knobs ({JoinOrNone(ended.Restored)}).",
                PerfExperimentLeaseEndReason.Forced =>
                    $"The lease was ended by force at {when}, and the app restored its knobs ({JoinOrNone(ended.Restored)}).",
                _ => $"The lease was released at {when}.",
            };
        }

        return _active is { } active
            ? $"This client does not hold the lease: {Describe(active, _time.GetUtcNow())}."
            : "The app holds no such lease; it may have restarted since the lease was taken, which also reset every knob to its launch value.";
    }

    private PerfExperimentLeaseResult ResultLocked()
    {
        if (_active is not { } active)
        {
            return new PerfExperimentLeaseResult(true, PerfExperimentLeaseFailure.None, null, null, Array.Empty<PerfExperimentLeaseKnob>(), _lastEnded);
        }

        return new PerfExperimentLeaseResult(
            true,
            PerfExperimentLeaseFailure.None,
            null,
            new PerfExperimentLeaseView(active.Holder, active.Knobs, active.AcquiredUtc, active.ExpiresUtc, active.TtlSeconds),
            active.Baseline,
            _lastEnded);
    }

    private PerfExperimentLeaseResult Refuse(PerfExperimentLeaseFailure failure, string error)
    {
        lock (_gate)
        {
            ExpireIfDueLocked();
            var current = ResultLocked();
            return current with { Success = false, Failure = failure, Error = error, Baseline = Array.Empty<PerfExperimentLeaseKnob>() };
        }
    }

    private static string Describe(ActiveLease lease, DateTimeOffset now)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"{lease.Holder}, knobs {JoinOrNone(lease.Knobs)}, held since {lease.AcquiredUtc:O}, expires in {Math.Max(0, (lease.ExpiresUtc - now).TotalSeconds):0} s unless renewed");

    private static string JoinOrNone(IReadOnlyCollection<string> items) => items.Count == 0 ? "none" : string.Join(", ", items);

    private static string? ValidateId(string? id)
        => string.IsNullOrEmpty(id) || id.Length > MaxIdLength || id.Any(char.IsControl)
            ? $"lease must be the client's lease id: 1 to {MaxIdLength} characters with no control characters."
            : null;

    private static string? ValidateTtl(int? requested, out int ttl)
    {
        ttl = requested ?? DefaultTtlSeconds;
        return ttl is < MinTtlSeconds or > MaxTtlSeconds
            ? string.Create(CultureInfo.InvariantCulture, $"ttlSeconds must be {MinTtlSeconds} to {MaxTtlSeconds}; got {ttl}.")
            : null;
    }
}
