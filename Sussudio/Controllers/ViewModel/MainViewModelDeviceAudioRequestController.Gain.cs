using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Telemetry;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed partial class MainViewModelDeviceAudioRequestController
    {
        public void HandleAnalogAudioGainPercentChanged(double value)
        {
            if (_viewModel._isLoadingSettings || _viewModel._isRefreshingDeviceAudioControls || !_viewModel.IsDeviceAudioControlSupported)
            {
                return;
            }

            if (_viewModel.IsRecording)
            {
                Logger.Log("Analog audio gain change ignored while recording");
                return;
            }

            if (!string.Equals(_viewModel.SelectedDeviceAudioMode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase))
            {
                _viewModel.SaveSettings();
                return;
            }

            // Debounce the XU write to avoid flooding the hardware with commands
            // while the user drags the slider (same hazard class as AT SET bricking).
            var targetDevice = _viewModel.SelectedDevice;
            if (targetDevice == null)
            {
                _viewModel.SaveSettings();
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
                    var enqueued = _viewModel.EnqueueUiOperation(async () =>
                    {
                        try
                        {
                            if (Volatile.Read(ref _viewModel._disposeState) == 0)
                            {
                                await _viewModel.ApplyAnalogAudioGainAsync("analog audio gain change", targetDevice: targetDevice, cancellationToken: token).ConfigureAwait(false);
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
            _viewModel.SaveSettings();
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
                    if (!token.IsCancellationRequested && _viewModel.IsCurrentSelectedDevice(device))
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
}
