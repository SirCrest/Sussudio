using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Telemetry;

namespace Sussudio.Controllers;

internal sealed class MainViewModelDeviceAudioRequestControllerContext
{
    public required Func<Func<Task>, string, bool, bool> EnqueueUiOperation { get; init; }
    public required Func<bool> IsDisposing { get; init; }
    public required Func<bool> IsLoadingSettings { get; init; }
    public required Func<bool> IsRefreshingDeviceAudioControls { get; init; }
    public required Func<bool> IsDeviceAudioControlSupported { get; init; }
    public required Func<bool> IsRecording { get; init; }
    public required Func<string> GetSelectedDeviceAudioMode { get; init; }
    public required Func<CaptureDevice?> GetSelectedDevice { get; init; }
    public required Action SaveSettings { get; init; }
    public required Func<CaptureDevice?, bool, CancellationToken, Task> RefreshDeviceAudioControlsAsync { get; init; }
    public required Func<string, CaptureDevice?, CancellationToken, Task<bool>> ApplyDeviceAudioModeAsync { get; init; }
    public required Func<string, CaptureDevice?, CancellationToken, Task<bool>> ApplyAnalogAudioGainAsync { get; init; }
    public required Func<CaptureDevice, bool> IsCurrentSelectedDevice { get; init; }
}

/// <summary>
/// Owns device-native audio request scheduling, debounce lifetimes, and
/// cancellation cleanup for the compatibility ViewModel facade.
/// </summary>
internal sealed class MainViewModelDeviceAudioRequestController
{
    private readonly MainViewModelDeviceAudioRequestControllerContext _context;
    private CancellationTokenSource? _gainFlashDebounceCts;
    private CancellationTokenSource? _gainXuDebounceCts;
    private CancellationTokenSource? _deviceAudioModeCts;
    private CancellationTokenSource? _deviceAudioRefreshCts;

    public MainViewModelDeviceAudioRequestController(MainViewModelDeviceAudioRequestControllerContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void RequestDeviceAudioControlsRefresh(CaptureDevice? targetDevice)
    {
        var refreshCts = new CancellationTokenSource();
        var refreshToken = refreshCts.Token;
        _deviceAudioRefreshCts = refreshCts;
        var enqueued = _context.EnqueueUiOperation(async () =>
        {
            try
            {
                if (!_context.IsDisposing())
                {
                    await _context.RefreshDeviceAudioControlsAsync(targetDevice, true, refreshToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Log("Device audio controls refresh canceled because selected device changed");
            }
            finally
            {
                if (ReferenceEquals(_deviceAudioRefreshCts, refreshCts))
                {
                    _deviceAudioRefreshCts = null;
                }

                refreshCts.Dispose();
            }
        }, "device audio controls refresh", true);
        if (!enqueued)
        {
            if (ReferenceEquals(_deviceAudioRefreshCts, refreshCts))
            {
                _deviceAudioRefreshCts = null;
            }

            refreshCts.Dispose();
        }
    }

    public void HandleSelectedDeviceAudioModeChanged(string value)
    {
        if (_context.IsLoadingSettings() || _context.IsRefreshingDeviceAudioControls() || !_context.IsDeviceAudioControlSupported())
        {
            return;
        }

        if (_context.IsRecording())
        {
            Logger.Log("Device audio mode change ignored while recording");
            return;
        }

        var oldCts = _deviceAudioModeCts;
        oldCts?.Cancel();
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var targetDevice = _context.GetSelectedDevice();
        _deviceAudioModeCts = cts;
        var enqueued = _context.EnqueueUiOperation(async () =>
        {
            try
            {
                if (!_context.IsDisposing())
                {
                    await _context.ApplyDeviceAudioModeAsync("device audio mode change", targetDevice, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Log("Device audio mode change canceled because selected device changed");
            }
            finally
            {
                if (ReferenceEquals(_deviceAudioModeCts, cts))
                {
                    _deviceAudioModeCts = null;
                }

                cts.Dispose();
            }
        }, "device audio mode change", true);
        if (!enqueued)
        {
            if (ReferenceEquals(_deviceAudioModeCts, cts))
            {
                _deviceAudioModeCts = null;
            }

            cts.Dispose();
        }

        _context.SaveSettings();
    }

    public void HandleAnalogAudioGainPercentChanged(double value)
    {
        if (_context.IsLoadingSettings() || _context.IsRefreshingDeviceAudioControls() || !_context.IsDeviceAudioControlSupported())
        {
            return;
        }

        if (_context.IsRecording())
        {
            Logger.Log("Analog audio gain change ignored while recording");
            return;
        }

        if (!string.Equals(_context.GetSelectedDeviceAudioMode(), DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase))
        {
            _context.SaveSettings();
            return;
        }

        // Debounce the XU write to avoid flooding the hardware with commands
        // while the user drags the slider (same hazard class as AT SET bricking).
        var targetDevice = _context.GetSelectedDevice();
        if (targetDevice == null)
        {
            _context.SaveSettings();
            return;
        }

        var oldCts = _gainXuDebounceCts;
        oldCts?.Cancel();
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        _gainXuDebounceCts = cts;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(200, token).ConfigureAwait(false);
                var enqueued = _context.EnqueueUiOperation(async () =>
                {
                    try
                    {
                        if (!_context.IsDisposing())
                        {
                            await _context.ApplyAnalogAudioGainAsync("analog audio gain change", targetDevice, token).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.Log("Analog audio gain change canceled because selected device changed");
                    }
                    finally
                    {
                        if (ReferenceEquals(_gainXuDebounceCts, cts))
                        {
                            _gainXuDebounceCts = null;
                        }

                        cts.Dispose();
                    }
                }, "analog audio gain change", true);
                if (!enqueued)
                {
                    if (ReferenceEquals(_gainXuDebounceCts, cts))
                    {
                        _gainXuDebounceCts = null;
                    }

                    cts.Dispose();
                }
            }
            catch (OperationCanceledException)
            {
                if (ReferenceEquals(_gainXuDebounceCts, cts))
                {
                    _gainXuDebounceCts = null;
                }

                cts.Dispose();
            }
        });
        _context.SaveSettings();
    }

    public void ScheduleAnalogGainFlashPersist(CaptureDevice device, byte gainByte)
    {
        var oldCts = _gainFlashDebounceCts;
        oldCts?.Cancel();
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        _gainFlashDebounceCts = cts;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token).ConfigureAwait(false);
                if (!token.IsCancellationRequested && _context.IsCurrentSelectedDevice(device))
                {
                    await NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: true, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                /* Superseded by a newer gain change - expected */
            }
            finally
            {
                if (ReferenceEquals(_gainFlashDebounceCts, cts))
                {
                    _gainFlashDebounceCts = null;
                }

                cts.Dispose();
            }
        });
    }

    public void CancelPendingAudioControlWork()
    {
        var flashCts = _gainFlashDebounceCts;
        _gainFlashDebounceCts = null;
        flashCts?.Cancel();

        var xuCts = _gainXuDebounceCts;
        _gainXuDebounceCts = null;
        xuCts?.Cancel();

        var modeCts = _deviceAudioModeCts;
        _deviceAudioModeCts = null;
        modeCts?.Cancel();

        var refreshCts = _deviceAudioRefreshCts;
        _deviceAudioRefreshCts = null;
        refreshCts?.Cancel();
    }
}
