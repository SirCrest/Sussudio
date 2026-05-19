using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

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
    private sealed partial class MainViewModelDeviceAudioRequestController
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
