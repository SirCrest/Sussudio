using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Sussudio.Tools.Performance;

// Owns the app's lease capability and its renewal clock. The session interprets the acquired baseline and owns every
// mutation and restore action; this client only manages lease lifetime.
internal sealed class PerfExperimentLeaseClient
{
    private const string LeaseCommand = "PerfExperimentLease";
    private const int MaxHolderLength = 200;

    private readonly PerfAppLink _app;
    private readonly string _experiment;
    private readonly TimeSpan _cleanupTimeout;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly Stopwatch _sinceRenewClock = new();
    private TimeSpan _sinceRenewWaited;

    public PerfExperimentLeaseClient(
        PerfAppLink app,
        string experiment,
        TimeSpan ttl,
        TimeSpan cleanupTimeout,
        Func<TimeSpan, CancellationToken, Task> delay)
    {
        _app = app;
        _experiment = experiment;
        TtlSeconds = (int)Math.Ceiling(ttl.TotalSeconds);
        RenewInterval = TimeSpan.FromSeconds(Math.Max(1, TtlSeconds / 4.0));
        _cleanupTimeout = cleanupTimeout;
        _delay = delay;
        Id = Guid.NewGuid().ToString("N");
    }

    public string Id { get; }

    public int TtlSeconds { get; }

    private TimeSpan RenewInterval { get; }

    public bool IsHeld { get; private set; }

    public string? LostReason { get; private set; }

    public async Task<PerfReply> AcquireAsync(IReadOnlyList<string> knobNames, CancellationToken cancellationToken)
    {
        var reply = await _app.CallAsync(
            LeaseCommand,
            new Dictionary<string, object?>
            {
                ["action"] = "acquire",
                ["lease"] = Id,
                ["holder"] = BuildHolder(_experiment),
                ["knobs"] = knobNames.ToArray(),
                ["ttlSeconds"] = TtlSeconds,
            },
            cancellationToken,
            "PerfExperimentLease acquire").ConfigureAwait(false);

        if (reply.Success)
        {
            IsHeld = true;
            MarkRenewed();
        }

        return reply;
    }

    /// <summary>
    /// The acquire's reply never arrived, so the app may hold a lease under this runner's id with nothing to restore.
    /// Release it so it does not block the next experiment until it expires. Best effort; never throws.
    /// </summary>
    public async Task ReleaseUnconfirmedAsync()
    {
        using var cleanup = new CancellationTokenSource(_cleanupTimeout);
        try
        {
            await _app.CallAsync(
                LeaseCommand,
                new Dictionary<string, object?> { ["action"] = "release", ["lease"] = Id },
                cleanup.Token,
                "PerfExperimentLease release").ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Intentionally swallowed: the lease, if the app holds one, expires by itself and changed nothing.
        }
    }

    public async Task RenewAsync(CancellationToken cancellationToken)
    {
        if (!IsHeld)
        {
            return;
        }

        var reply = await _app.CallAsync(
            LeaseCommand,
            new Dictionary<string, object?> { ["action"] = "renew", ["lease"] = Id, ["ttlSeconds"] = TtlSeconds },
            cancellationToken,
            "PerfExperimentLease renew").ConfigureAwait(false);
        if (!reply.Success)
        {
            IsHeld = false;
            LostReason = reply.Message;
            throw new PerfExperimentCommandException(
                $"The experiment lost its tuning-knob lease: {reply.Message} The experiment stopped, and the run in progress is not used.");
        }

        MarkRenewed();
    }

    public Task RenewIfDueAsync(CancellationToken cancellationToken)
        => IsHeld && (_sinceRenewWaited >= RenewInterval || _sinceRenewClock.Elapsed >= RenewInterval)
            ? RenewAsync(cancellationToken)
            : Task.CompletedTask;

    // Used by readiness polling, which checks state after a wait and renews immediately before the next wait if due.
    public async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        await _delay(delay, cancellationToken).ConfigureAwait(false);
        _sinceRenewWaited += delay;
    }

    // Waits through injected delay in bounded chunks, renewing between chunks so warmup and measurement cannot outlive
    // the lease. Virtual time in tests counts as waited.
    public async Task WaitAsync(TimeSpan duration, CancellationToken cancellationToken)
    {
        var remaining = duration;
        while (remaining > TimeSpan.Zero)
        {
            var step = IsHeld && remaining > RenewInterval ? RenewInterval : remaining;
            await DelayAsync(step, cancellationToken).ConfigureAwait(false);
            remaining -= step;
            if (remaining > TimeSpan.Zero)
            {
                await RenewIfDueAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task<PerfReply> ReleaseAsync(CancellationToken cancellationToken)
    {
        var reply = await _app.CallAsync(
            LeaseCommand,
            new Dictionary<string, object?> { ["action"] = "release", ["lease"] = Id },
            cancellationToken,
            "PerfExperimentLease release").ConfigureAwait(false);
        if (reply.Success)
        {
            IsHeld = false;
        }

        return reply;
    }

    private void MarkRenewed()
    {
        _sinceRenewWaited = TimeSpan.Zero;
        _sinceRenewClock.Restart();
    }

    // Who runs the experiment, as other clients see it in the lease: the tool process and the experiment name.
    private static string BuildHolder(string experiment)
    {
        var text = new StringBuilder();
        text.Append(Process.GetCurrentProcess().ProcessName)
            .Append(" pid ")
            .Append(Environment.ProcessId.ToString(CultureInfo.InvariantCulture))
            .Append(": experiment '");
        foreach (var ch in experiment)
        {
            text.Append(char.IsControl(ch) ? ' ' : ch);
        }

        text.Append('\'');
        return text.Length <= MaxHolderLength ? text.ToString() : text.ToString(0, MaxHolderLength - 4) + "...'";
    }
}
