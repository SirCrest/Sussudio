using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Telemetry;

namespace Sussudio.Controllers;

internal sealed partial class MainViewModelDeviceAudioRequestController
{
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
}
