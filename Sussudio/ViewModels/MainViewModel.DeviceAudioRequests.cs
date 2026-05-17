using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Telemetry;

namespace Sussudio.ViewModels;

/// <summary>
/// Device-native audio request lifetime: selected-device refresh, mode-change
/// application, analog-gain debounce, and cancellation cleanup.
/// </summary>
public partial class MainViewModel
{
    private CancellationTokenSource? _gainFlashDebounceCts;
    private CancellationTokenSource? _gainXuDebounceCts;
    private CancellationTokenSource? _deviceAudioModeCts;
    private CancellationTokenSource? _deviceAudioRefreshCts;

    private void RequestDeviceAudioControlsRefresh(CaptureDevice? targetDevice)
    {
        var refreshCts = new CancellationTokenSource();
        var refreshToken = refreshCts.Token;
        _deviceAudioRefreshCts = refreshCts;
        var enqueued = EnqueueUiOperation(async () =>
        {
            try
            {
                if (Volatile.Read(ref _disposeState) == 0)
                {
                    await RefreshDeviceAudioControlsAsync(targetDevice, applySavedState: true, refreshToken).ConfigureAwait(false);
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
        }, "device audio controls refresh", allowDuringDispose: true);
        if (!enqueued)
        {
            if (ReferenceEquals(_deviceAudioRefreshCts, refreshCts))
            {
                _deviceAudioRefreshCts = null;
            }

            refreshCts.Dispose();
        }
    }

    partial void OnSelectedDeviceAudioModeChanged(string value)
    {
        if (_isLoadingSettings || _isRefreshingDeviceAudioControls || !IsDeviceAudioControlSupported)
        {
            return;
        }

        if (IsRecording)
        {
            Logger.Log("Device audio mode change ignored while recording");
            return;
        }
        var oldCts = _deviceAudioModeCts;
        oldCts?.Cancel();
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var targetDevice = SelectedDevice;
        _deviceAudioModeCts = cts;
        var enqueued = EnqueueUiOperation(async () =>
        {
            try
            {
                if (Volatile.Read(ref _disposeState) == 0)
                {
                    await ApplyDeviceAudioModeAsync("device audio mode change", targetDevice: targetDevice, cancellationToken: token).ConfigureAwait(false);
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
        }, "device audio mode change", allowDuringDispose: true);
        if (!enqueued)
        {
            if (ReferenceEquals(_deviceAudioModeCts, cts))
            {
                _deviceAudioModeCts = null;
            }

            cts.Dispose();
        }
        SaveSettings();
    }

    partial void OnAnalogAudioGainPercentChanged(double value)
    {
        if (_isLoadingSettings || _isRefreshingDeviceAudioControls || !IsDeviceAudioControlSupported)
        {
            return;
        }

        if (IsRecording)
        {
            Logger.Log("Analog audio gain change ignored while recording");
            return;
        }

        if (!string.Equals(SelectedDeviceAudioMode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase))
        {
            SaveSettings();
            return;
        }

        // Debounce the XU write to avoid flooding the hardware with commands
        // while the user drags the slider (same hazard class as AT SET bricking).
        var targetDevice = SelectedDevice;
        if (targetDevice == null)
        {
            SaveSettings();
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
                var enqueued = EnqueueUiOperation(async () =>
                {
                    try
                    {
                        if (Volatile.Read(ref _disposeState) == 0)
                        {
                            await ApplyAnalogAudioGainAsync("analog audio gain change", targetDevice: targetDevice, cancellationToken: token).ConfigureAwait(false);
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
                }, "analog audio gain change", allowDuringDispose: true);
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
        SaveSettings();
    }

    private void RequestAnalogGainFlashPersist(CaptureDevice device, byte gainByte)
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
                if (!token.IsCancellationRequested && IsCurrentSelectedDevice(device))
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

    private void CancelPendingAudioControlWork()
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
