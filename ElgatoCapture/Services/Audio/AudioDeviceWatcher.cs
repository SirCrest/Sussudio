using System;
using System.Threading;

namespace ElgatoCapture.Services;

internal sealed class AudioDeviceWatcher : IMMNotificationClient, IDisposable
{
    private IMMDeviceEnumerator? _enumerator;
    private Timer? _debounceTimer;
    private int _disposed;
    private const int DebounceMs = 500;

    public event Action? DevicesChanged;

    public AudioDeviceWatcher()
    {
        try
        {
            _enumerator = WasapiComInterop.CreateDeviceEnumerator();
            WasapiComInterop.ThrowIfFailed(
                _enumerator.RegisterEndpointNotificationCallback(this),
                "IMMDeviceEnumerator.RegisterEndpointNotificationCallback");
            Logger.Log("AudioDeviceWatcher: registered for endpoint notifications.");
        }
        catch (Exception ex)
        {
            Logger.Log($"AudioDeviceWatcher: failed to register — {ex.Message}");
            WasapiComInterop.ReleaseComObject(ref _enumerator);
        }
    }

    private void ScheduleNotification()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        var newTimer = new Timer(_ =>
        {
            if (Volatile.Read(ref _disposed) != 0) return;
            try
            {
                DevicesChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Log($"AudioDeviceWatcher: callback error — {ex.Message}");
            }
        }, null, DebounceMs, Timeout.Infinite);
        Interlocked.Exchange(ref _debounceTimer, newTimer)?.Dispose();
    }

    int IMMNotificationClient.OnDeviceStateChanged(string deviceId, uint newState)
    {
        Logger.Log($"AudioDeviceWatcher: device state changed — {deviceId} → 0x{newState:X}");
        ScheduleNotification();
        return 0;
    }

    int IMMNotificationClient.OnDeviceAdded(string deviceId)
    {
        Logger.Log($"AudioDeviceWatcher: device added — {deviceId}");
        ScheduleNotification();
        return 0;
    }

    int IMMNotificationClient.OnDeviceRemoved(string deviceId)
    {
        Logger.Log($"AudioDeviceWatcher: device removed — {deviceId}");
        ScheduleNotification();
        return 0;
    }

    int IMMNotificationClient.OnDefaultDeviceChanged(EDataFlow flow, ERole role, string? defaultDeviceId)
    {
        return 0; // Not relevant for our use case
    }

    int IMMNotificationClient.OnPropertyValueChanged(string deviceId, PROPERTYKEY key)
    {
        return 0; // Not relevant for our use case
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _debounceTimer?.Dispose();
        _debounceTimer = null;

        if (_enumerator != null)
        {
            try
            {
                _enumerator.UnregisterEndpointNotificationCallback(this);
            }
            catch (Exception ex)
            {
                Logger.Log($"AudioDeviceWatcher: unregister warning — {ex.Message}");
            }

            WasapiComInterop.ReleaseComObject(ref _enumerator);
        }

        Logger.Log("AudioDeviceWatcher: disposed.");
    }
}
