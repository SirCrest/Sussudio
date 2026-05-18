using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Telemetry;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    partial void OnSelectedDeviceAudioModeChanged(string value)
        => _deviceAudioRequestController.HandleSelectedDeviceAudioModeChanged(value);

    partial void OnAnalogAudioGainPercentChanged(double value)
        => _deviceAudioRequestController.HandleAnalogAudioGainPercentChanged(value);

    private void RequestDeviceAudioControlsRefresh(CaptureDevice? targetDevice)
        => _deviceAudioRequestController.RequestDeviceAudioControlsRefresh(targetDevice);

    private void RequestAnalogGainFlashPersist(CaptureDevice device, byte gainByte)
        => _deviceAudioRequestController.ScheduleAnalogGainFlashPersist(device, gainByte);

    private void CancelPendingAudioControlWork()
        => _deviceAudioRequestController.CancelPendingAudioControlWork();

    /// <summary>
    /// Owns device-native audio request scheduling, debounce lifetimes, and
    /// cancellation cleanup for the compatibility ViewModel facade.
    /// </summary>
    private sealed class MainViewModelDeviceAudioRequestController
    {
        private readonly MainViewModel _viewModel;
        private CancellationTokenSource? _gainFlashDebounceCts;
        private CancellationTokenSource? _gainXuDebounceCts;
        private CancellationTokenSource? _deviceAudioModeCts;
        private CancellationTokenSource? _deviceAudioRefreshCts;

        public MainViewModelDeviceAudioRequestController(MainViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public void RequestDeviceAudioControlsRefresh(CaptureDevice? targetDevice)
        {
            var refreshCts = new CancellationTokenSource();
            var refreshToken = refreshCts.Token;
            _deviceAudioRefreshCts = refreshCts;
            var enqueued = _viewModel.EnqueueUiOperation(async () =>
            {
                try
                {
                    if (Volatile.Read(ref _viewModel._disposeState) == 0)
                    {
                        await _viewModel.RefreshDeviceAudioControlsAsync(targetDevice, applySavedState: true, refreshToken).ConfigureAwait(false);
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

        public void HandleSelectedDeviceAudioModeChanged(string value)
        {
            if (_viewModel._isLoadingSettings || _viewModel._isRefreshingDeviceAudioControls || !_viewModel.IsDeviceAudioControlSupported)
            {
                return;
            }

            if (_viewModel.IsRecording)
            {
                Logger.Log("Device audio mode change ignored while recording");
                return;
            }

            var oldCts = _deviceAudioModeCts;
            oldCts?.Cancel();
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var targetDevice = _viewModel.SelectedDevice;
            _deviceAudioModeCts = cts;
            var enqueued = _viewModel.EnqueueUiOperation(async () =>
            {
                try
                {
                    if (Volatile.Read(ref _viewModel._disposeState) == 0)
                    {
                        await _viewModel.ApplyDeviceAudioModeAsync("device audio mode change", targetDevice: targetDevice, cancellationToken: token).ConfigureAwait(false);
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

            _viewModel.SaveSettings();
        }

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
}
