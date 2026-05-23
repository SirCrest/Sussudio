using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;

namespace Sussudio.Controllers;

/// <summary>
/// Graph-built ports consumed by the device refresh controller.
/// </summary>
internal sealed class MainViewModelDeviceRefreshControllerContext
{
    public required Action<string> SetStatusText { get; init; }
    public required Func<long> IncrementDeviceScanGeneration { get; init; }
    public required Func<string?> GetSelectedAudioInputDeviceId { get; init; }
    public required Func<string?> GetSelectedMicrophoneDeviceId { get; init; }
    public required Func<string?> GetSelectedDeviceId { get; init; }
    public required Func<Task<DeviceService.DeviceDiscoveryResult>> EnumerateCaptureDeviceDiscoveryAsync { get; init; }
    public required Action<List<AudioInputDevice>, IReadOnlyList<CaptureDevice>, string?, string?, string?> ApplyStartupAudioDeviceScan { get; init; }
    public required Action<IReadOnlyList<CaptureDevice>> ReplaceDevices { get; init; }
    public required Func<IList<CaptureDevice>> GetDevices { get; init; }
    public required Action<CaptureDevice, long> BeginBackgroundFormatProbe { get; init; }
    public required Func<string> GetLastDiscoverySummary { get; init; }
    public required Action<CaptureDevice?> SetSelectedDevice { get; init; }
    public required Func<CaptureDevice?> GetSelectedDevice { get; init; }
    public required Func<string?> GetPendingSavedDeviceId { get; init; }
    public required Action<string?> SetPendingSavedDeviceId { get; init; }
}

/// <summary>
/// Owns capture-device refresh orchestration behind the MainViewModel compatibility facade.
/// </summary>
internal sealed class MainViewModelDeviceRefreshController
{
    private readonly MainViewModelDeviceRefreshControllerContext _context;
    private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;

    public MainViewModelDeviceRefreshController(
        MainViewModelDeviceRefreshControllerContext context,
        MainViewModelPreviewLifecycleController previewLifecycleController)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _previewLifecycleController = previewLifecycleController ?? throw new ArgumentNullException(nameof(previewLifecycleController));
    }

    public async Task RefreshDevicesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _context.SetStatusText("Scanning for devices...");

        try
        {
            var discoveryStopwatch = Stopwatch.StartNew();
            var scanGeneration = _context.IncrementDeviceScanGeneration();
            var previousAudioId = _context.GetSelectedAudioInputDeviceId();
            var previousMicrophoneId = _context.GetSelectedMicrophoneDeviceId();
            var previousDeviceId = _context.GetSelectedDeviceId();
            var discovery = await _context.EnumerateCaptureDeviceDiscoveryAsync()
                .ConfigureAwait(true);
            cancellationToken.ThrowIfCancellationRequested();
            var audioDevices = discovery.AudioInputDevices.ToList();
            var devices = discovery.CaptureDevices;
            cancellationToken.ThrowIfCancellationRequested();
            discoveryStopwatch.Stop();

            _context.ApplyStartupAudioDeviceScan(
                audioDevices,
                devices,
                previousDeviceId,
                previousAudioId,
                previousMicrophoneId);

            _context.ReplaceDevices(devices.ToList());
            foreach (var discoveredDevice in _context.GetDevices())
            {
                _context.BeginBackgroundFormatProbe(discoveredDevice, scanGeneration);
            }

            var discoverySummary = _context.GetLastDiscoverySummary();
            Logger.Log($"Device discovery summary (ViewModel): {discoverySummary}");

            if (_context.GetDevices().Count > 0)
            {
                await ApplySuccessfulDeviceScanAsync(
                    discoveryStopwatch.ElapsedMilliseconds,
                    previousDeviceId,
                    cancellationToken);
            }
            else
            {
                _context.SetSelectedDevice(null);
                _context.SetStatusText("No compatible video capture devices found (see log for discovery summary)");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _context.SetStatusText("Device scan canceled");
            throw;
        }
        catch (Exception ex)
        {
            _context.SetStatusText($"Error scanning devices: {ex.Message}");
        }
    }

    private async Task ApplySuccessfulDeviceScanAsync(
        long discoveryElapsedMs,
        string? previousDeviceId,
        CancellationToken cancellationToken)
    {
        var devices = _context.GetDevices();
        _context.SetStatusText(discoveryElapsedMs <= 1500
            ? $"Found {devices.Count} device(s) in {discoveryElapsedMs} ms"
            : $"Found {devices.Count} device(s) in {discoveryElapsedMs} ms (slow scan: waiting on system device enumeration/probe startup)");

        var savedDeviceId = _context.GetPendingSavedDeviceId();
        _context.SetPendingSavedDeviceId(null);
        var nextSelectedDevice =
            devices.FirstOrDefault(d => d.Id == previousDeviceId)
            ?? (!string.IsNullOrWhiteSpace(savedDeviceId) ? devices.FirstOrDefault(d => d.Id == savedDeviceId) : null)
            ?? devices[0];
        if (!string.IsNullOrWhiteSpace(savedDeviceId) && nextSelectedDevice.Id != savedDeviceId)
        {
            Logger.Log($"SETTINGS_RESTORE: saved device '{savedDeviceId}' not found, using fallback.");
        }

        _context.SetSelectedDevice(nextSelectedDevice);
        Logger.Log($"Auto-selected device: {_context.GetSelectedDevice()?.Name}");

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
            _context.SetStatusText($"Preview failed to start: {ex.Message}");
        }
    }
}
