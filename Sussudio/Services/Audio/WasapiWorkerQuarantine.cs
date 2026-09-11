using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.Services.Audio;

internal enum WasapiWorkerRole
{
    Capture,
    Playback
}

internal sealed class WasapiWorkerQuarantinedEventArgs : EventArgs
{
    internal WasapiWorkerQuarantinedEventArgs(WasapiWorkerRole role, string timeoutEvent)
    {
        Role = role;
        TimeoutEvent = timeoutEvent;
    }

    internal WasapiWorkerRole Role { get; }

    internal string TimeoutEvent { get; }
}

// A worker that did not reach its terminal finally still owns its event and COM
// graph. Keep that exact instance rooted and refuse a replacement for the same
// role until the old worker proves that cleanup completed.
internal static class WasapiWorkerQuarantine
{
    private static readonly ConcurrentDictionary<object, Registration> Registrations = new();

    internal static event EventHandler<WasapiWorkerQuarantinedEventArgs>? EmergencyCloseRequested;

    internal static void ThrowIfBlocked(WasapiWorkerRole role)
    {
        foreach (var registration in Registrations.Values)
        {
            if (registration.Role == role && !registration.Completion.IsCompleted)
            {
                throw new InvalidOperationException(
                    $"WASAPI {role.ToString().ToLowerInvariant()} is temporarily unavailable because its previous worker has not exited.");
            }
        }
    }

    internal static void Register(
        WasapiWorkerRole role,
        object owner,
        Task completion,
        string timeoutEvent)
    {
        if (completion.IsCompleted)
        {
            return;
        }

        var registration = new Registration(role, completion);
        if (!Registrations.TryAdd(owner, registration))
        {
            return;
        }

        var handler = EmergencyCloseRequested;
        Logger.Log(
            $"{timeoutEvent} role={role.ToString().ToLowerInvariant()} audio_restart_blocked=true emergency_close_requested={handler is not null}");

        try
        {
            handler?.Invoke(
                null,
                new WasapiWorkerQuarantinedEventArgs(role, timeoutEvent));
        }
        catch (Exception ex)
        {
            Logger.Log($"WASAPI_WORKER_QUARANTINE_NOTIFICATION_FAILED error='{ex.Message}'");
        }

        _ = completion.ContinueWith(
            static (_, state) =>
            {
                var quarantinedOwner = state!;
                if (Registrations.TryRemove(quarantinedOwner, out var removed))
                {
                    Logger.Log(
                        $"WASAPI_WORKER_QUARANTINE_CLEARED role={removed.Role.ToString().ToLowerInvariant()}");
                }
            },
            owner,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private sealed record Registration(WasapiWorkerRole Role, Task Completion);
}
