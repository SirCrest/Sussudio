using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    /// <summary>
    /// Owns capture-device refresh orchestration behind the MainViewModel compatibility facade.
    /// </summary>
    private sealed class MainViewModelDeviceRefreshController
    {
        private readonly MainViewModel _viewModel;
        private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;

        public MainViewModelDeviceRefreshController(
            MainViewModel viewModel,
            MainViewModelPreviewLifecycleController previewLifecycleController)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _previewLifecycleController = previewLifecycleController ?? throw new ArgumentNullException(nameof(previewLifecycleController));
        }

        public async Task RefreshDevicesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _viewModel.StatusText = "Scanning for devices...";

            try
            {
                var discoveryStopwatch = Stopwatch.StartNew();
                var scanGeneration = Interlocked.Increment(ref _viewModel._deviceScanGeneration);
                var previousAudioId = _viewModel.SelectedAudioInputDevice?.Id;
                var previousMicrophoneId = _viewModel.SelectedMicrophoneDevice?.Id;
                var previousDeviceId = _viewModel.SelectedDevice?.Id;
                var discovery = await _viewModel._deviceService
                    .EnumerateCaptureDeviceDiscoveryAsync(waitForFormatProbes: false)
                    .ConfigureAwait(true);
                cancellationToken.ThrowIfCancellationRequested();
                var audioDevices = discovery.AudioInputDevices.ToList();
                var devices = discovery.CaptureDevices;
                cancellationToken.ThrowIfCancellationRequested();
                discoveryStopwatch.Stop();

                _viewModel.ApplyStartupAudioDeviceScan(
                    audioDevices,
                    devices,
                    previousDeviceId,
                    previousAudioId,
                    previousMicrophoneId);

                ReplaceCollection(_viewModel.Devices, devices.ToList());
                foreach (var discoveredDevice in _viewModel.Devices)
                {
                    _viewModel._deviceService.BeginBackgroundFormatProbe(discoveredDevice, scanGeneration);
                }

                var discoverySummary = _viewModel._deviceService.LastDiscoverySummary;
                Logger.Log($"Device discovery summary (ViewModel): {discoverySummary}");

                if (_viewModel.Devices.Count > 0)
                {
                    await ApplySuccessfulDeviceScanAsync(
                        discoveryStopwatch.ElapsedMilliseconds,
                        previousDeviceId,
                        cancellationToken);
                }
                else
                {
                    _viewModel.SelectedDevice = null;
                    _viewModel.StatusText = "No compatible video capture devices found (see log for discovery summary)";
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _viewModel.StatusText = "Device scan canceled";
                throw;
            }
            catch (Exception ex)
            {
                _viewModel.StatusText = $"Error scanning devices: {ex.Message}";
            }
        }

        private async Task ApplySuccessfulDeviceScanAsync(
            long discoveryElapsedMs,
            string? previousDeviceId,
            CancellationToken cancellationToken)
        {
            _viewModel.StatusText = discoveryElapsedMs <= 1500
                ? $"Found {_viewModel.Devices.Count} device(s) in {discoveryElapsedMs} ms"
                : $"Found {_viewModel.Devices.Count} device(s) in {discoveryElapsedMs} ms (slow scan: waiting on system device enumeration/probe startup)";

            var savedDeviceId = _viewModel._pendingSavedDeviceId;
            _viewModel._pendingSavedDeviceId = null;
            var nextSelectedDevice =
                _viewModel.Devices.FirstOrDefault(d => d.Id == previousDeviceId)
                ?? (!string.IsNullOrWhiteSpace(savedDeviceId) ? _viewModel.Devices.FirstOrDefault(d => d.Id == savedDeviceId) : null)
                ?? _viewModel.Devices[0];
            if (!string.IsNullOrWhiteSpace(savedDeviceId) && nextSelectedDevice.Id != savedDeviceId)
            {
                Logger.Log($"SETTINGS_RESTORE: saved device '{savedDeviceId}' not found, using fallback.");
            }

            _viewModel.SelectedDevice = nextSelectedDevice;
            Logger.Log($"Auto-selected device: {_viewModel.SelectedDevice?.Name}");

            try
            {
                await _previewLifecycleController.StartPreviewAsync(userInitiated: false, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Log($"Auto-start preview failed after device scan: {ex.Message}");
                _viewModel.StatusText = $"Preview failed to start: {ex.Message}";
            }
        }
    }
}
