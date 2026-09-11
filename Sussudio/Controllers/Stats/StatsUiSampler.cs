using System;
using System.Diagnostics;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed record StatsUiSample(
    StatsSnapshot Snapshot,
    long CaptureSessionEpoch,
    long CollectedTick,
    long HealthCollectedTick,
    bool HealthUpdated);

/// <summary>
/// Shares one UI sample across visible consumers. The owner supplies the timer;
/// automation continues to collect its own fresh snapshots.
/// </summary>
internal sealed class StatsUiSampler : IDisposable
{
    internal const int LabelIntervalMs = 250;
    internal const int HealthIntervalMs = 500;

    private readonly Func<long> _getSessionEpoch;
    private readonly Func<CaptureHealthSnapshot> _collectHealth;
    private readonly Func<CaptureHealthSnapshot, bool, StatsSnapshot> _collectSnapshot;
    private readonly Action<string> _log;
    private readonly Func<long> _getTick;
    private readonly long _frequency;
    private Subscription[] _subscriptions = Array.Empty<Subscription>();
    private CaptureHealthSnapshot? _health;
    private StatsUiSample? _current;
    private bool _collecting;
    private bool _disposed;

    public StatsUiSampler(
        Func<long> getSessionEpoch,
        Func<CaptureHealthSnapshot> collectHealth,
        Func<CaptureHealthSnapshot, bool, StatsSnapshot> collectSnapshot,
        Action<string> log,
        Func<long>? getTick = null,
        long frequency = 0)
    {
        _getSessionEpoch = getSessionEpoch;
        _collectHealth = collectHealth;
        _collectSnapshot = collectSnapshot;
        _log = log;
        _getTick = getTick ?? Stopwatch.GetTimestamp;
        _frequency = frequency > 0 ? frequency : Stopwatch.Frequency;
    }

    public event Action<bool>? DemandChanged;
    public bool HasSubscribers => _subscriptions.Length != 0;

    public IDisposable Subscribe(Action<StatsUiSample> receiveSample)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(receiveSample);
        var subscription = new Subscription(this, receiveSample);
        var previousCount = _subscriptions.Length;
        Array.Resize(ref _subscriptions, previousCount + 1);
        _subscriptions[previousCount] = subscription;
        if (previousCount == 0)
        {
            DemandChanged?.Invoke(true);
        }

        var alreadyCollecting = _collecting;
        var previousSample = _current;
        Tick();
        if (!alreadyCollecting && _current != null && ReferenceEquals(previousSample, _current))
        {
            subscription.Publish(_current);
        }
        return subscription;
    }

    public void Tick()
    {
        if (!_disposed && HasSubscribers)
        {
            CollectAndPublishIfDue();
        }
    }

    /// <summary>
    /// Returns the shared UI snapshot, collecting and publishing a new sample when due.
    /// </summary>
    public StatsSnapshot RefreshIfDueAndGetSnapshot()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        CollectAndPublishIfDue();
        return _current?.Snapshot
            ?? throw new InvalidOperationException("The capture session changed while collecting UI stats; retry on the next sample.");
    }

    private bool IsCurrent(long now, long epoch)
        => _current != null && _current.CaptureSessionEpoch == epoch &&
           ElapsedMs(_current.CollectedTick, now) < LabelIntervalMs;

    private void CollectAndPublishIfDue()
    {
        if (_collecting)
        {
            return;
        }

        _collecting = true;
        try
        {
            var now = _getTick();
            var epoch = _getSessionEpoch();
            if (IsCurrent(now, epoch))
            {
                return;
            }

            var refreshHealth = _health == null || _current == null ||
                _current.CaptureSessionEpoch != epoch ||
                ElapsedMs(_current.HealthCollectedTick, now) >= HealthIntervalMs;
            var health = refreshHealth ? _collectHealth() : _health!;
            if (health.CaptureSessionEpoch != epoch || _getSessionEpoch() != epoch)
            {
                Invalidate();
                return;
            }

            var snapshot = _collectSnapshot(health, refreshHealth);
            if (_getSessionEpoch() != epoch)
            {
                Invalidate();
                return;
            }

            var sample = new StatsUiSample(
                snapshot,
                epoch,
                now,
                refreshHealth ? now : _current!.HealthCollectedTick,
                refreshHealth);
            _health = health;
            _current = sample;

            // Subscribe/unsubscribe allocate only when visibility changes. Retain the
            // current array during callbacks so a closing consumer cannot skip another.
            var subscriptions = _subscriptions;
            foreach (var subscription in subscriptions)
            {
                subscription.Publish(sample);
            }
        }
        catch (Exception ex)
        {
            Invalidate();
            _log($"STATS_SAMPLE_FAIL type={ex.GetType().Name} msg={ex.Message}");
        }
        finally
        {
            _collecting = false;
        }
    }

    private double ElapsedMs(long from, long to)
        => Math.Max(0, to - from) * 1000.0 / _frequency;

    private void Invalidate()
    {
        _health = null;
        _current = null;
    }

    private void Remove(Subscription subscription)
    {
        var index = Array.IndexOf(_subscriptions, subscription);
        if (index < 0)
        {
            return;
        }

        var remaining = new Subscription[_subscriptions.Length - 1];
        Array.Copy(_subscriptions, 0, remaining, 0, index);
        Array.Copy(_subscriptions, index + 1, remaining, index, remaining.Length - index);
        _subscriptions = remaining;
        if (remaining.Length == 0)
        {
            Invalidate();
            DemandChanged?.Invoke(false);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        var subscriptions = _subscriptions;
        foreach (var subscription in subscriptions)
        {
            subscription.Dispose();
        }
        Invalidate();
        DemandChanged = null;
    }

    private sealed class Subscription : IDisposable
    {
        private StatsUiSampler? _owner;
        private readonly Action<StatsUiSample> _receiveSample;

        public Subscription(StatsUiSampler owner, Action<StatsUiSample> receiveSample)
        {
            _owner = owner;
            _receiveSample = receiveSample;
        }

        public void Publish(StatsUiSample sample)
        {
            var owner = _owner;
            if (owner == null)
            {
                return;
            }
            try
            {
                _receiveSample(sample);
            }
            catch (Exception ex)
            {
                owner._log($"STATS_CONSUMER_FAIL type={ex.GetType().Name} msg={ex.Message}");
            }
        }

        public void Dispose()
        {
            var owner = _owner;
            _owner = null;
            owner?.Remove(this);
        }
    }
}
