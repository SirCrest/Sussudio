using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.ViewModels;

/// <summary>
/// Device and format discovery flow. It refreshes user-selectable options while
/// preserving the active selection whenever the underlying device list changes.
/// </summary>
public partial class MainViewModel
{
    public async Task RefreshDevicesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        StatusText = "Scanning for devices...";

        try
        {
            var discoveryStopwatch = Stopwatch.StartNew();
            var scanGeneration = Interlocked.Increment(ref _deviceScanGeneration);
            var previousAudioId = SelectedAudioInputDevice?.Id;
            var previousMicrophoneId = SelectedMicrophoneDevice?.Id;
            var previousDeviceId = SelectedDevice?.Id;
            var audioDevicesTask = MfDeviceEnumerator.EnumerateAudioCaptureEndpointsAsync();
            var devicesTask = _deviceService.EnumerateVideoCaptureDevicesAsync(waitForFormatProbes: false);
            await Task.WhenAll(audioDevicesTask, devicesTask).ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();
            var audioDevices = audioDevicesTask.Result.ToList();
            var devices = devicesTask.Result;
            cancellationToken.ThrowIfCancellationRequested();
            discoveryStopwatch.Stop();

            ApplyStartupAudioDeviceScan(
                audioDevices,
                devices,
                previousDeviceId,
                previousAudioId,
                previousMicrophoneId);

            ReplaceCollection(Devices, devices.ToList());
            foreach (var discoveredDevice in Devices)
            {
                _deviceService.BeginBackgroundFormatProbe(discoveredDevice, scanGeneration);
            }

            var discoverySummary = _deviceService.LastDiscoverySummary;
            Logger.Log($"Device discovery summary (ViewModel): {discoverySummary}");

            if (Devices.Count > 0)
            {
                StatusText = discoveryStopwatch.ElapsedMilliseconds <= 1500
                    ? $"Found {Devices.Count} device(s) in {discoveryStopwatch.ElapsedMilliseconds} ms"
                    : $"Found {Devices.Count} device(s) in {discoveryStopwatch.ElapsedMilliseconds} ms (slow scan: waiting on system device enumeration/probe startup)";

                var savedDeviceId = _pendingSavedDeviceId;
                _pendingSavedDeviceId = null;
                var nextSelectedDevice =
                    Devices.FirstOrDefault(d => d.Id == previousDeviceId)
                    ?? (!string.IsNullOrWhiteSpace(savedDeviceId) ? Devices.FirstOrDefault(d => d.Id == savedDeviceId) : null)
                    ?? Devices[0];
                if (!string.IsNullOrWhiteSpace(savedDeviceId) && nextSelectedDevice.Id != savedDeviceId)
                {
                    Logger.Log($"SETTINGS_RESTORE: saved device '{savedDeviceId}' not found, using fallback.");
                }
                SelectedDevice = nextSelectedDevice;
                Logger.Log($"Auto-selected device: {SelectedDevice?.Name}");

                // Auto-start preview (StartPreviewAsync will initialize device if needed)
                try
                {
                    await StartPreviewAsync(userInitiated: false, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Auto-start preview failed after device scan: {ex.Message}");
                    StatusText = $"Preview failed to start: {ex.Message}";
                }
            }
            else
            {
                SelectedDevice = null;
                StatusText = "No compatible video capture devices found (see log for discovery summary)";
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            StatusText = "Device scan canceled";
            throw;
        }
        catch (Exception ex)
        {
            StatusText = $"Error scanning devices: {ex.Message}";
        }
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IReadOnlyList<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

}
