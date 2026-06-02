using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;
using Sussudio.ViewModels;

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

    public async Task RefreshDevicesAsync(
        CancellationToken cancellationToken = default,
        bool throwOnScanFailure = false)
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
            if (throwOnScanFailure)
            {
                throw;
            }
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

/// <summary>
/// Graph-built ports consumed by the late device-format probe controller.
/// </summary>
internal sealed class MainViewModelDeviceFormatProbeControllerContext
{
    public required Func<Action, bool> TryEnqueueOnUiThread { get; init; }
    public required Func<long> ReadDeviceScanGeneration { get; init; }
    public required Func<string, CaptureDevice?> FindDeviceById { get; init; }
    public required Action<bool> SetPendingSdrAutoSelectionForDeviceChange { get; init; }
    public required Action<int?> SetPendingSdrAutoFriendlyFrameRateBucket { get; init; }
    public required Func<CaptureDevice?> GetSelectedDevice { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Func<bool> IsInitialized { get; init; }
    public required Func<bool> IsRecording { get; init; }
    public required Func<string?> GetSelectedResolution { get; init; }
    public required Func<double> GetSelectedFrameRate { get; init; }
    public required Func<MediaFormat?> GetSelectedFormat { get; init; }
    public required Action<bool> SetSuppressFormatChangeReinitialize { get; init; }
    public required Action<CaptureDevice, bool> RebuildSelectedDeviceCapabilities { get; init; }
    public required Func<MainViewModelDeviceFormatProbeRetargetApplier> CreateRetargetApplier { get; init; }
}

/// <summary>
/// Owns late device-format probe reconciliation for the compatibility ViewModel facade.
/// </summary>
internal sealed class MainViewModelDeviceFormatProbeController
{
    private readonly MainViewModelDeviceFormatProbeControllerContext _context;
    private readonly MainViewModelDeviceFormatProbeRetargetApplier _retargetApplier;

    public MainViewModelDeviceFormatProbeController(MainViewModelDeviceFormatProbeControllerContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _retargetApplier = _context.CreateRetargetApplier();
    }

    public void OnDeviceFormatProbeCompleted(object? sender, DeviceService.DeviceFormatProbeCompletedEventArgs e)
    {
        if (!_context.TryEnqueueOnUiThread(() =>
        {
            if (e.RequestId != _context.ReadDeviceScanGeneration())
            {
                return;
            }

            var target = _context.FindDeviceById(e.DeviceId);
            if (target == null)
            {
                return;
            }

            if (!e.Succeeded)
            {
                _context.SetPendingSdrAutoSelectionForDeviceChange(false);
                _context.SetPendingSdrAutoFriendlyFrameRateBucket(null);
                Logger.Log($"Format probe failed for {e.DeviceName}: {e.Error}");
                return;
            }

            target.SupportedFormats.Clear();
            foreach (var format in e.Formats)
            {
                target.SupportedFormats.Add(new MediaFormat
                {
                    Width = format.Width,
                    Height = format.Height,
                    FrameRate = format.FrameRate,
                    FrameRateNumerator = format.FrameRateNumerator,
                    FrameRateDenominator = format.FrameRateDenominator,
                    PixelFormat = format.PixelFormat,
                    IsHdr = format.IsHdr
                });
            }

            target.IsHdrCapable = e.IsHdrCapable;

            var selectedDevice = _context.GetSelectedDevice();
            if (selectedDevice == null ||
                !string.Equals(selectedDevice.Id, target.Id, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var isPreviewing = _context.IsPreviewing();
            var isRecording = _context.IsRecording();
            var preserveActiveSelection = isPreviewing || isRecording;
            var allowProbeDrivenRetarget = isPreviewing && _context.IsInitialized() && !isRecording;
            var previousResolution = _context.GetSelectedResolution();
            var previousFrameRate = _context.GetSelectedFrameRate();
            Logger.Log($"Format probe completed for {e.DeviceName}: formats={e.Formats.Count} preserveActive={preserveActiveSelection} allowRetarget={allowProbeDrivenRetarget} prevRes={previousResolution} prevFps={previousFrameRate:0.###}");

            if (preserveActiveSelection)
            {
                Logger.Log($"Refreshing selected-device capabilities during active capture for {e.DeviceName} (preserveSelection={!allowProbeDrivenRetarget}).");
            }

            _context.SetSuppressFormatChangeReinitialize(preserveActiveSelection);
            try
            {
                _context.RebuildSelectedDeviceCapabilities(selectedDevice, false);
            }
            finally
            {
                _context.SetSuppressFormatChangeReinitialize(false);
            }

            var selectedResolution = _context.GetSelectedResolution();
            var selectedFrameRate = _context.GetSelectedFrameRate();
            var selectedFormat = _context.GetSelectedFormat();
            Logger.Log($"Format probe rebuild done: SelectedRes={selectedResolution} SelectedFormat={selectedFormat?.Width}x{selectedFormat?.Height}@{selectedFormat?.FrameRate:0.###} modeChanged={!string.Equals(previousResolution, selectedResolution, StringComparison.OrdinalIgnoreCase) || !FrameRateTimingPolicy.IsFrameRateMatch(previousFrameRate, selectedFrameRate)}");

            var modeChanged = !string.Equals(previousResolution, selectedResolution, StringComparison.OrdinalIgnoreCase) ||
                              !FrameRateTimingPolicy.IsFrameRateMatch(previousFrameRate, selectedFrameRate);

            if (_retargetApplier.TryApplyDeviceFormatProbeRetarget(
                target,
                preserveActiveSelection,
                allowProbeDrivenRetarget,
                previousResolution,
                previousFrameRate,
                modeChanged))
            {
                return;
            }
        }))
        {
            Logger.Log($"FORMAT_PROBE_UI_ENQUEUE_FAILED deviceId='{e.DeviceId}' requestId={e.RequestId}");
        }
    }
}

/// <summary>
/// Graph-built ports consumed by late device-format probe retarget application.
/// </summary>
internal sealed class MainViewModelDeviceFormatProbeRetargetApplierContext
{
    public required Func<bool> IsHdrEnabled { get; init; }
    public required Func<string?> GetSelectedResolution { get; init; }
    public required Action<string?> SetSelectedResolution { get; init; }
    public required Func<double> GetSelectedFrameRate { get; init; }
    public required Action<double> SetSelectedFrameRate { get; init; }
    public required Func<MediaFormat?> GetSelectedFormat { get; init; }
    public required Func<string, bool> AvailableResolutionsContains { get; init; }
    public required Action<bool> SetIsRebuildingModeOptions { get; init; }
    public required Action<bool> SetIsApplyingAutomaticResolutionSelection { get; init; }
    public required Action<bool> SetSuppressFormatChangeReinitialize { get; init; }
    public required Action RebuildFrameRateOptions { get; init; }
    public required Func<string, Task> ReinitializeDeviceAsync { get; init; }
    public required Func<Func<Task>, string, bool> EnqueueUiOperation { get; init; }
    public required Func<CaptureRuntimeSnapshot> GetCaptureRuntimeSnapshot { get; init; }
    public required Action UpdateSelectedFormat { get; init; }
    public required Action UpdateTargetSummary { get; init; }
}

/// <summary>
/// Applies late device-format probe retarget decisions to the compatibility ViewModel facade.
/// </summary>
internal sealed class MainViewModelDeviceFormatProbeRetargetApplier
{
    private readonly MainViewModelDeviceFormatProbeRetargetApplierContext _context;

    public MainViewModelDeviceFormatProbeRetargetApplier(MainViewModelDeviceFormatProbeRetargetApplierContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public bool TryApplyDeviceFormatProbeRetarget(
        CaptureDevice target,
        bool preserveActiveSelection,
        bool allowProbeDrivenRetarget,
        string? previousResolution,
        double previousFrameRate,
        bool modeChanged)
    {
        var retargetDecision = DecideDeviceFormatProbeRetarget(
            target,
            preserveActiveSelection,
            allowProbeDrivenRetarget,
            previousResolution,
            previousFrameRate,
            modeChanged,
            includeSessionMismatchCheck: false,
            sessionActualWidth: null,
            sessionActualHeight: null);

        if (retargetDecision.Kind == DeviceFormatProbeRetargetDecisionKind.HdrRetarget)
        {
            Logger.Log($"Format probe updated HDR mode set; applying new mode {_context.GetSelectedResolution()}@{_context.GetSelectedFrameRate():0.###} via device renegotiation.");
            _context.EnqueueUiOperation(
                () => _context.ReinitializeDeviceAsync(retargetDecision.ReinitializeReason!),
                retargetDecision.UiOperationName!);
            return true;
        }

        if (retargetDecision.Kind == DeviceFormatProbeRetargetDecisionKind.PreserveMjpegHighFrameRate)
        {
            Logger.Log(
                $"Format probe preserved special MJPG HFR mode at {_context.GetSelectedResolution()}@{_context.GetSelectedFrameRate():0.###}; " +
                "skipping SDR NV12 retarget.");
            return true;
        }

        if (retargetDecision.Kind == DeviceFormatProbeRetargetDecisionKind.SdrNv12Retarget)
        {
            Logger.Log(
                $"Format probe detected MJPG-only mode at {_context.GetSelectedResolution()}@{_context.GetSelectedFrameRate():0.###}; " +
                $"retargeting SDR to NV12-capable mode {retargetDecision.TargetResolution}@{retargetDecision.TargetFrameRate:0.###}.");

            _context.SetIsRebuildingModeOptions(true);
            _context.SetIsApplyingAutomaticResolutionSelection(true);
            try
            {
                _context.SetSelectedResolution(retargetDecision.TargetResolution);
            }
            finally
            {
                _context.SetIsApplyingAutomaticResolutionSelection(false);
                _context.SetIsRebuildingModeOptions(false);
            }

            _context.SetSuppressFormatChangeReinitialize(true);
            try
            {
                _context.RebuildFrameRateOptions();
            }
            finally
            {
                _context.SetSuppressFormatChangeReinitialize(false);
            }

            _context.EnqueueUiOperation(
                () => _context.ReinitializeDeviceAsync(retargetDecision.ReinitializeReason!),
                retargetDecision.UiOperationName!);
            return true;
        }

        if (allowProbeDrivenRetarget && _context.GetSelectedFormat() != null)
        {
            var runtime = _context.GetCaptureRuntimeSnapshot();
            var selectedFormat = _context.GetSelectedFormat();
            Logger.Log($"Format probe session check: actual={runtime.ActualWidth}x{runtime.ActualHeight} selected={selectedFormat!.Width}x{selectedFormat.Height}");
            retargetDecision = DecideDeviceFormatProbeRetarget(
                target,
                preserveActiveSelection,
                allowProbeDrivenRetarget,
                previousResolution,
                previousFrameRate,
                modeChanged,
                includeSessionMismatchCheck: true,
                sessionActualWidth: runtime.ActualWidth,
                sessionActualHeight: runtime.ActualHeight);

            if (retargetDecision.Kind == DeviceFormatProbeRetargetDecisionKind.SessionRuntimeUnavailable)
            {
                Logger.Log("Format probe session mismatch check skipped: runtime width/height not yet available.");
            }
            else if (retargetDecision.Kind == DeviceFormatProbeRetargetDecisionKind.SessionMismatch)
            {
                Logger.Log(
                    $"Format probe detected session/format mismatch: " +
                    $"session={runtime.ActualWidth}x{runtime.ActualHeight} " +
                    $"selected={selectedFormat.Width}x{selectedFormat.Height}; reinitializing.");
                _context.EnqueueUiOperation(
                    () => _context.ReinitializeDeviceAsync(retargetDecision.ReinitializeReason!),
                    retargetDecision.UiOperationName!);
                return true;
            }
        }

        if (retargetDecision.Kind == DeviceFormatProbeRetargetDecisionKind.RestoreActiveSelection)
        {
            _context.SetIsRebuildingModeOptions(true);
            _context.SetIsApplyingAutomaticResolutionSelection(true);
            try
            {
                _context.SetSelectedResolution(previousResolution);
                _context.SetSelectedFrameRate(previousFrameRate);
                _context.UpdateSelectedFormat();
                _context.UpdateTargetSummary();
            }
            finally
            {
                _context.SetIsApplyingAutomaticResolutionSelection(false);
                _context.SetIsRebuildingModeOptions(false);
            }
        }

        return false;
    }

    private DeviceFormatProbeRetargetDecision DecideDeviceFormatProbeRetarget(
        CaptureDevice target,
        bool preserveActiveSelection,
        bool allowProbeDrivenRetarget,
        string? previousResolution,
        double previousFrameRate,
        bool modeChanged,
        bool includeSessionMismatchCheck,
        uint? sessionActualWidth,
        uint? sessionActualHeight)
        => DeviceFormatProbeRetargetPolicy.Decide(new DeviceFormatProbeRetargetRequest(
            preserveActiveSelection,
            allowProbeDrivenRetarget,
            _context.IsHdrEnabled(),
            modeChanged,
            previousResolution,
            previousFrameRate,
            _context.GetSelectedResolution(),
            _context.GetSelectedFrameRate(),
            _context.GetSelectedFormat(),
            target.SupportedFormats,
            !string.IsNullOrWhiteSpace(previousResolution) &&
                _context.AvailableResolutionsContains(previousResolution),
            includeSessionMismatchCheck,
            sessionActualWidth,
            sessionActualHeight));
}

/// <summary>
/// Graph-built ports consumed by the recording capability refresh controller.
/// </summary>

/// <summary>
/// Graph-built ports consumed by the capture-mode option rebuild controller.
/// </summary>
internal sealed class MainViewModelCaptureModeOptionRebuildControllerContext
{
    public required ObservableCollection<MediaFormat> AvailableFormats { get; init; }
    public required ObservableCollection<FrameRateOption> AvailableFrameRates { get; init; }
    public required ObservableCollection<ResolutionOption> AvailableResolutions { get; init; }
    public required ObservableCollection<string> AvailableVideoFormats { get; init; }
    public required string AutoResolutionValue { get; init; }
    public required double AutoFrameRateValue { get; init; }
    public required Func<IReadOnlyDictionary<string, List<MediaFormat>>> GetResolutionToFormats { get; init; }
    public required Func<SourceSignalTelemetrySnapshot> GetLatestSourceTelemetry { get; init; }
    public required TryGetEffectiveResolutionSelectionDelegate TryGetEffectiveResolutionSelection { get; init; }
    public required TryResolveResolutionKeyDelegate TryResolveResolutionKey { get; init; }
    public required Func<string?, string?> GetEffectiveResolutionKey { get; init; }
    public required Action<FrameRateOption?, double> ApplyResolvedFrameRateSelection { get; init; }
    public required Func<string> GetSelectedResolutionDisplayText { get; init; }
    public required Func<string?, string> BuildHdrSupportHintForResolution { get; init; }
    public required Action UpdateTargetSummary { get; init; }
    public required Action NotifySelectedResolutionChanged { get; init; }
    public required Func<CaptureDevice?> GetSelectedDevice { get; init; }
    public required Func<string?> GetSelectedResolution { get; init; }
    public required Action<string?> SetSelectedResolution { get; init; }
    public required Func<double> GetSelectedFrameRate { get; init; }
    public required Func<string> GetSelectedVideoFormat { get; init; }
    public required Action<string> SetSelectedVideoFormat { get; init; }
    public required Action<MediaFormat?> SetSelectedFormat { get; init; }
    public required Func<bool> IsHdrEnabled { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Func<bool> IsAutoFrameRateSelected { get; init; }
    public required Action<bool> SetIsAutoFrameRateSelected { get; init; }
    public required Func<bool> HasUserOverriddenResolutionForCurrentMode { get; init; }
    public required Func<bool> HasUserOverriddenFrameRateForCurrentMode { get; init; }
    public required Func<bool> IsPendingSdrAutoSelectionForDeviceChange { get; init; }
    public required Action<bool> SetPendingSdrAutoSelectionForDeviceChange { get; init; }
    public required Func<int?> GetPendingSdrAutoFriendlyFrameRateBucket { get; init; }
    public required Action<int?> SetPendingSdrAutoFriendlyFrameRateBucket { get; init; }
    public required Func<bool> IsForceSourceAutoRetarget { get; init; }
    public required Action<bool> SetForceSourceAutoRetarget { get; init; }
    public required Func<string?> GetLastKnownResolutionKey { get; init; }
    public required Action<string?> SetLastKnownResolutionKey { get; init; }
    public required Action<bool> SetIsRebuildingModeOptions { get; init; }
    public required Action<bool> SetIsApplyingAutomaticResolutionSelection { get; init; }
    public required Action<bool> SetIsApplyingAutomaticFrameRateSelection { get; init; }
    public required Func<bool> IsSuppressFormatChangeReinitialize { get; init; }
    public required Action<bool> SetSuppressFormatChangeReinitialize { get; init; }
    public required Action<double?> SetDetectedSourceFrameRate { get; init; }
    public required Action<string?> SetDetectedSourceFrameRateArg { get; init; }
    public required Action<string> SetSourceFrameRateOrigin { get; init; }
    public required Action<uint?> SetAutoResolvedWidth { get; init; }
    public required Action<uint?> SetAutoResolvedHeight { get; init; }
    public required Action<double?> SetAutoResolvedFrameRate { get; init; }
    public required Action<string> SetHdrResolutionSupportHint { get; init; }
    public required Action<string> SetDisabledResolutionReason { get; init; }
    public required Action<string> SetStatusText { get; init; }

    public delegate bool TryGetEffectiveResolutionSelectionDelegate(out string resolutionKey, out uint width, out uint height);
    public delegate bool TryResolveResolutionKeyDelegate(string? resolutionValue, out string resolutionKey);
}

/// <summary>
/// Owns capture-mode option rebuild transactions for the MainViewModel compatibility facade.
/// </summary>
internal sealed class MainViewModelCaptureModeOptionRebuildController
{
    private readonly MainViewModelCaptureModeOptionRebuildControllerContext _context;
    private readonly MainViewModelFrameRateTimingResolver _frameRateTimingResolver;

    public MainViewModelCaptureModeOptionRebuildController(
        MainViewModelCaptureModeOptionRebuildControllerContext context,
        MainViewModelFrameRateTimingResolver frameRateTimingResolver)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _frameRateTimingResolver = frameRateTimingResolver ?? throw new ArgumentNullException(nameof(frameRateTimingResolver));
    }

    public void UpdateSelectedFormat()
    {
        if (!_context.TryGetEffectiveResolutionSelection(out var resolutionKey, out var width, out var height))
        {
            _context.SetSelectedFormat(null);
            return;
        }

        _context.SetSelectedFormat(CaptureFormatSelectionPolicy.Select(
            BuildCaptureFormatSelectionRequest(resolutionKey, width, height)));
    }

    public void RebuildVideoFormatOptions()
    {
        // Source-reader pixel formats are not global device capabilities. A card can expose
        // MJPG at 4K120 SDR while exposing only P010 at the HDR retarget mode, so keep this
        // list scoped to the currently selected resolution+fps tuple.
        var formats = GetFormatsForSelectedModeTuple();
        var nextFormats = CaptureModeOptionsBuilder.BuildVideoFormatOptions(formats);

        _context.AvailableVideoFormats.Clear();
        foreach (var format in nextFormats)
        {
            _context.AvailableVideoFormats.Add(format);
        }

        if (!_context.AvailableVideoFormats.Any(format => string.Equals(format, _context.GetSelectedVideoFormat(), StringComparison.OrdinalIgnoreCase)))
        {
            var previousSuppress = _context.IsSuppressFormatChangeReinitialize();
            _context.SetSuppressFormatChangeReinitialize(true);
            try
            {
                _context.SetSelectedVideoFormat("Auto");
            }
            finally
            {
                _context.SetSuppressFormatChangeReinitialize(previousSuppress);
            }
        }
    }

    private List<MediaFormat> GetFormatsForSelectedModeTuple()
    {
        if (!_context.TryGetEffectiveResolutionSelection(out var resolutionKey, out var width, out var height))
        {
            return new List<MediaFormat>();
        }

        return CaptureFormatSelectionPolicy
            .SelectModeTupleFormats(BuildCaptureFormatSelectionRequest(resolutionKey, width, height))
            .ToList();
    }

    private CaptureFormatSelectionRequest BuildCaptureFormatSelectionRequest(
        string resolutionKey,
        uint width,
        uint height)
        => new(
            _context.AvailableFormats,
            _context.AvailableFrameRates,
            width,
            height,
            _context.GetSelectedFrameRate(),
            _context.GetSelectedVideoFormat(),
            _context.IsHdrEnabled(),
            _frameRateTimingResolver.ResolvePreferredTimingFamily(resolutionKey, _context.GetSelectedFrameRate()));

    public void RebuildFrameRateOptions()
    {
        var previousRate = _context.GetSelectedFrameRate();
        var options = new List<FrameRateOption>();
        var selectedResolutionKey = _context.GetEffectiveResolutionKey(_context.GetSelectedResolution());
        var timingFamily = _frameRateTimingResolver.ResolvePreferredTimingFamily(selectedResolutionKey, previousRate);
        var sourceTelemetry = _context.GetLatestSourceTelemetry();
        if (sourceTelemetry.HasFrameRate &&
            FrameRateTimingPolicy.TryInferFrameRateTimingFamily(sourceTelemetry.FrameRateArg, sourceTelemetry.FrameRateExact, out var sourceFamilyHint))
        {
            timingFamily = sourceFamilyHint;
        }

        if (!string.IsNullOrWhiteSpace(selectedResolutionKey) &&
            _context.GetResolutionToFormats().TryGetValue(selectedResolutionKey, out var formats))
        {
            options = formats
                .GroupBy(format => FrameRateTimingPolicy.GetFriendlyFrameRateBucket(format.FrameRateExact))
                .Select(group =>
                {
                    var allFormats = group.ToList();
                    var hdrFormats = allFormats.Where(CaptureModeOptionsBuilder.IsHdrModeCandidate).ToList();
                    var sdrFormats = allFormats.Where(f => !CaptureModeOptionsBuilder.IsHdrModeCandidate(f)).ToList();
                    // In HDR mode, only enable rates with HDR-capable formats.
                    // In SDR mode, enable if 8-bit formats exist. Also enable if only
                    // 10-bit formats exist for this rate (e.g., 4K HFR paths that only
                    // advertise P010) - UpdateSelectedFormat handles the fallback.
                    var enabled = _context.IsHdrEnabled() ? hdrFormats.Count > 0 : allFormats.Count > 0;
                    List<MediaFormat> selectionPool;
                    if (_context.IsHdrEnabled() && hdrFormats.Count > 0)
                        selectionPool = hdrFormats;
                    else if (!_context.IsHdrEnabled() && sdrFormats.Count > 0)
                        selectionPool = sdrFormats;
                    else
                        selectionPool = allFormats;
                    var preferred = FrameRateTimingPolicy.SelectPreferredFrameRateFormat(selectionPool, group.Key, timingFamily);
                    var numerator = preferred.FrameRateNumerator > 0 ? preferred.FrameRateNumerator : (uint?)null;
                    var denominator = preferred.FrameRateDenominator > 0 ? preferred.FrameRateDenominator : (uint?)null;
                    return new FrameRateOption
                    {
                        FriendlyValue = group.Key,
                        Value = preferred.FrameRateExact,
                        Rational = preferred.FrameRateRational,
                        Numerator = numerator,
                        Denominator = denominator,
                        IsEnabled = enabled,
                        DisableReason = enabled
                            ? string.Empty
                            : "HDR mode is not supported at this frame rate."
                    };
                })
                .OrderByDescending(option => option.FriendlyValue)
                .ToList();
        }

        var sourceRate = _frameRateTimingResolver.ResolveDetectedSourceFrameRate(selectedResolutionKey, options, previousRate);
        var sourceFilter = FrameRateSourceFilterPolicy.Apply(
            options,
            sourceRate.Rate,
            sourceRate.Arg,
            _frameRateTimingResolver.BuildFrameRateTimingVariants(selectedResolutionKey),
            true);
        var sourceTimingFamilyKnown = sourceFilter.SourceTimingFamilyKnown;
        var sourceTimingFamily = sourceFilter.SourceTimingFamily;
        options = sourceFilter.Options.ToList();
        var autoFrameRateOption = options.Count > 0
            ? new FrameRateOption
            {
                FriendlyValue = _context.AutoFrameRateValue,
                Value = _context.AutoFrameRateValue,
                IsEnabled = true,
                DisplayTextOverride = "Source"
            }
            : null;
        var availableOptions = autoFrameRateOption == null
            ? options
            : new[] { autoFrameRateOption }.Concat(options).ToList();
        _context.SetDetectedSourceFrameRate(sourceRate.Rate);
        _context.SetDetectedSourceFrameRateArg(sourceRate.Arg);
        _context.SetSourceFrameRateOrigin(sourceRate.Origin);

        _context.SetIsRebuildingModeOptions(true);
        try
        {
            _context.AvailableFrameRates.Clear();
            foreach (var option in availableOptions)
            {
                _context.AvailableFrameRates.Add(option);
            }

            var selection = FrameRateAutoSelectionPolicy.Select(new FrameRateAutoSelectionRequest(
                options,
                AutoFrameRateOptionAvailable: autoFrameRateOption != null,
                ForceAutoSelection: false,
                IsAutoFrameRateSelected: _context.IsAutoFrameRateSelected(),
                HasUserOverriddenFrameRateForCurrentMode: _context.HasUserOverriddenFrameRateForCurrentMode(),
                IsHdrEnabled: _context.IsHdrEnabled(),
                PendingSdrAutoSelectionForDeviceChange: _context.IsPendingSdrAutoSelectionForDeviceChange(),
                PendingSdrAutoFriendlyFrameRateBucket: _context.GetPendingSdrAutoFriendlyFrameRateBucket(),
                Source: new FrameRateAutoSelectionSource(sourceRate.Rate, sourceTimingFamilyKnown, sourceTimingFamily),
                PreviousRate: previousRate));

            if (autoFrameRateOption != null)
            {
                _context.SetIsAutoFrameRateSelected(selection.SelectAutoOption);
            }

            var fallbackRate = previousRate > 0
                ? previousRate
                : 60;
            _context.ApplyResolvedFrameRateSelection(selection.Selected, fallbackRate);
            if (_context.IsHdrEnabled() && selection.Selected is { IsEnabled: false })
            {
                _context.SetStatusText($"No HDR-capable frame rate is available for {_context.GetSelectedResolutionDisplayText()}.");
            }

            if (!_context.IsHdrEnabled() && _context.IsPendingSdrAutoSelectionForDeviceChange() && selection.Selected != null)
            {
                _context.SetPendingSdrAutoSelectionForDeviceChange(false);
                _context.SetPendingSdrAutoFriendlyFrameRateBucket(null);
            }
        }
        finally
        {
            _context.SetIsApplyingAutomaticFrameRateSelection(false);
            _context.SetIsRebuildingModeOptions(false);
        }

        RebuildVideoFormatOptions();
        UpdateSelectedFormat();
        _context.UpdateTargetSummary();
        _context.SetForceSourceAutoRetarget(false);
    }

    public void RebuildResolutionOptions()
    {
        var previousSelection = _context.GetSelectedResolution();
        var previousRate = _context.GetSelectedFrameRate();
        var desiredSelection = !string.IsNullOrWhiteSpace(previousSelection)
            ? previousSelection
            : _context.GetLastKnownResolutionKey();
        var options = CaptureModeOptionsBuilder.BuildResolutionOptions(
                _context.GetResolutionToFormats(),
                _context.IsHdrEnabled(),
                true,
                _context.GetLatestSourceTelemetry())
            .ToList();

        var autoSelection = ResolveAutoCaptureSelection(options);
        var autoOption = options.Count > 0
            ? CreateAutoResolutionOption()
            : null;

        if (options.Count == 0)
        {
            if (_context.GetSelectedDevice() != null && _context.IsPreviewing() && _context.AvailableResolutions.Count > 0)
            {
                var retainedSelection = _context.AvailableResolutions.FirstOrDefault(option =>
                        string.Equals(option.Value, _context.GetSelectedResolution(), StringComparison.OrdinalIgnoreCase))
                    ?? _context.AvailableResolutions.FirstOrDefault(option => option.IsEnabled)
                    ?? _context.AvailableResolutions.FirstOrDefault();
                if (retainedSelection != null)
                {
                    _context.SetIsRebuildingModeOptions(true);
                    _context.SetIsApplyingAutomaticResolutionSelection(true);
                    try
                    {
                        var previousSelectedResolution = _context.GetSelectedResolution();
                        _context.SetSelectedResolution(retainedSelection.Value);
                        if (string.Equals(previousSelectedResolution, retainedSelection.Value, StringComparison.OrdinalIgnoreCase))
                        {
                            _context.NotifySelectedResolutionChanged();
                        }

                        if (_context.TryResolveResolutionKey(retainedSelection.Value, out var retainedResolutionKey))
                        {
                            _context.SetLastKnownResolutionKey(retainedResolutionKey);
                        }
                    }
                    finally
                    {
                        _context.SetIsApplyingAutomaticResolutionSelection(false);
                        _context.SetIsRebuildingModeOptions(false);
                    }
                }

                RebuildDependentOptions();
                _context.UpdateTargetSummary();
                return;
            }

            _context.SetIsRebuildingModeOptions(true);
            try
            {
                _context.AvailableResolutions.Clear();
                _context.SetIsApplyingAutomaticResolutionSelection(true);
                _context.SetSelectedResolution(null);
                _context.SetIsApplyingAutomaticResolutionSelection(false);
                ClearAutoResolutionState();
                _context.SetHdrResolutionSupportHint(string.Empty);
                _context.SetDisabledResolutionReason(string.Empty);
            }
            finally
            {
                _context.SetIsApplyingAutomaticResolutionSelection(false);
                _context.SetIsRebuildingModeOptions(false);
            }

            RebuildDependentOptions();
            _context.UpdateTargetSummary();
            return;
        }

        var allowSourceAutoSelect = _context.IsHdrEnabled() && (_context.IsForceSourceAutoRetarget() || !_context.HasUserOverriddenResolutionForCurrentMode());
        var selection = CaptureResolutionSelectionPolicy.Select(new CaptureResolutionSelectionRequest(
            options,
            _context.GetResolutionToFormats(),
            _context.GetLatestSourceTelemetry(),
            desiredSelection,
            previousRate,
            _context.IsHdrEnabled(),
            allowSourceAutoSelect,
            _context.IsPendingSdrAutoSelectionForDeviceChange()));
        var selected = selection.Selected;
        var hdrHint = selection.HdrHint;
        if (!_context.IsHdrEnabled() && selection.SdrAutoFriendlyFrameRateBucket.HasValue)
        {
            _context.SetPendingSdrAutoFriendlyFrameRateBucket(selection.SdrAutoFriendlyFrameRateBucket.Value);
        }

        var selectAutoOption = autoOption != null && ShouldSelectAutoResolutionOption(previousSelection);
        var selectedDropdownOption = selectAutoOption
            ? autoOption
            : selected;
        var availableOptions = autoOption == null
            ? options
            : new[] { autoOption }.Concat(options).ToList();

        _context.SetIsRebuildingModeOptions(true);
        try
        {
            UpdateAutoResolutionState(autoSelection);
            _context.AvailableResolutions.Clear();
            foreach (var option in availableOptions)
            {
                _context.AvailableResolutions.Add(option);
            }

            _context.SetIsApplyingAutomaticResolutionSelection(true);
            if (selectedDropdownOption != null)
            {
                var previousSelectedResolution = _context.GetSelectedResolution();
                _context.SetSelectedResolution(selectedDropdownOption.Value);
                if (string.Equals(previousSelectedResolution, selectedDropdownOption.Value, StringComparison.OrdinalIgnoreCase))
                {
                    _context.NotifySelectedResolutionChanged();
                }
            }

            _context.SetIsApplyingAutomaticResolutionSelection(false);
            if (selected != null)
            {
                _context.SetLastKnownResolutionKey(selected.Value);
            }

            if (_context.IsHdrEnabled())
            {
                _context.SetHdrResolutionSupportHint(hdrHint ?? _context.BuildHdrSupportHintForResolution(selected?.Value));
            }
            else
            {
                _context.SetHdrResolutionSupportHint(string.Empty);
            }

            if (_context.IsHdrEnabled() && selected is { IsEnabled: false })
            {
                _context.SetStatusText("No HDR-capable resolution is available for this device.");
            }

            _context.SetDisabledResolutionReason(selected is { IsEnabled: false }
                ? selected.DisableReason
                : string.Empty);
        }
        finally
        {
            _context.SetIsApplyingAutomaticResolutionSelection(false);
            _context.SetIsRebuildingModeOptions(false);
        }

        RebuildDependentOptions();
    }

    private void RebuildDependentOptions()
        => RebuildFrameRateOptions();

    private AutoCaptureSelection? ResolveAutoCaptureSelection(IReadOnlyList<ResolutionOption> options)
        => AutoCaptureSelectionPolicy.Select(new AutoCaptureSelectionRequest(
            options,
            _context.GetResolutionToFormats(),
            _context.GetLatestSourceTelemetry(),
            _context.IsHdrEnabled()));

    private bool ShouldSelectAutoResolutionOption(string? previousSelection)
        => string.Equals(previousSelection, _context.AutoResolutionValue, StringComparison.OrdinalIgnoreCase) ||
           string.IsNullOrWhiteSpace(previousSelection) ||
           !_context.HasUserOverriddenResolutionForCurrentMode();

    private ResolutionOption CreateAutoResolutionOption()
        => new()
        {
            Value = _context.AutoResolutionValue,
            Width = 0,
            Height = 0,
            IsEnabled = true,
            DisplayTextOverride = BuildAutoResolutionDisplayText()
        };

    private string BuildAutoResolutionDisplayText()
        => _context.AutoResolutionValue;

    private void UpdateAutoResolutionState(AutoCaptureSelection? selection)
    {
        _context.SetAutoResolvedWidth(selection?.Resolution.Width);
        _context.SetAutoResolvedHeight(selection?.Resolution.Height);
        _context.SetAutoResolvedFrameRate(selection?.ExactFrameRate);
    }

    private void ClearAutoResolutionState()
    {
        _context.SetAutoResolvedWidth(null);
        _context.SetAutoResolvedHeight(null);
        _context.SetAutoResolvedFrameRate(null);
    }
}
/// <summary>
/// Graph-built ports consumed by the frame-rate timing resolver.
/// </summary>
internal sealed class MainViewModelFrameRateTimingResolverContext
{
    public required Func<IReadOnlyDictionary<string, List<MediaFormat>>> GetResolutionToFormats { get; init; }
    public required Func<CaptureRuntimeSnapshot> GetRuntimeSnapshot { get; init; }
    public required Func<SourceSignalTelemetrySnapshot> GetLatestSourceTelemetry { get; init; }
    public required Func<MediaFormat?> GetSelectedFormat { get; init; }
    public required ObservableCollection<FrameRateOption> AvailableFrameRates { get; init; }
}

/// <summary>
/// Resolves stateful frame-rate timing preferences and detected source rates
/// for the MainViewModel compatibility facade.
/// </summary>
internal sealed class MainViewModelFrameRateTimingResolver
{
    private readonly MainViewModelFrameRateTimingResolverContext _context;

    public MainViewModelFrameRateTimingResolver(MainViewModelFrameRateTimingResolverContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IReadOnlyList<FrameRateTimingVariant> BuildFrameRateTimingVariants(string? resolutionKey)
    {
        if (string.IsNullOrWhiteSpace(resolutionKey) ||
            !_context.GetResolutionToFormats().TryGetValue(resolutionKey, out var formats))
        {
            return Array.Empty<FrameRateTimingVariant>();
        }

        return FrameRateTimingPolicy.BuildTimingVariants(formats);
    }

    public FrameRateTimingFamily ResolvePreferredTimingFamily(string? resolutionKey, double previousRate)
    {
        var runtime = _context.GetRuntimeSnapshot();
        if (CaptureResolutionSelectionPolicy.TryParseResolutionKey(resolutionKey, out var selectedWidth, out var selectedHeight))
        {
            if (runtime.ActualWidth == selectedWidth &&
                runtime.ActualHeight == selectedHeight &&
                FrameRateTimingPolicy.TryInferFrameRateTimingFamily(
                    runtime.ActualFrameRateArg ?? runtime.NegotiatedFrameRateArg,
                    runtime.ActualFrameRate ?? runtime.NegotiatedFrameRate,
                    out var runtimeFamily))
            {
                return runtimeFamily;
            }

            if (runtime.NegotiatedWidth == selectedWidth &&
                runtime.NegotiatedHeight == selectedHeight &&
                FrameRateTimingPolicy.TryInferFrameRateTimingFamily(
                    runtime.NegotiatedFrameRateArg,
                    runtime.NegotiatedFrameRate,
                    out var negotiatedFamily))
            {
                return negotiatedFamily;
            }
        }

        var selectedFormat = _context.GetSelectedFormat();
        if (FrameRateTimingPolicy.TryInferFrameRateTimingFamily(selectedFormat?.FrameRateRational, selectedFormat?.FrameRateExact, out var selectedFamily))
        {
            return selectedFamily;
        }

        var selectedOption = _context.AvailableFrameRates.FirstOrDefault(option => FrameRateTimingPolicy.IsFrameRateMatch(option.Value, previousRate));
        if (selectedOption != null &&
            FrameRateTimingPolicy.TryInferFrameRateTimingFamily(selectedOption.Rational, selectedOption.Value, out var optionFamily))
        {
            return optionFamily;
        }

        if (FrameRateTimingPolicy.TryInferFrameRateTimingFamily(null, previousRate, out var previousFamily))
        {
            return previousFamily;
        }

        return FrameRateTimingFamily.Unknown;
    }

    public (double? Rate, string? Arg, string Origin) ResolveDetectedSourceFrameRate(
        string? resolutionKey,
        IReadOnlyList<FrameRateOption> options,
        double previousRate)
    {
        var latestSourceTelemetry = _context.GetLatestSourceTelemetry();
        if (latestSourceTelemetry.HasFrameRate)
        {
            return (
                latestSourceTelemetry.FrameRateExact,
                latestSourceTelemetry.FrameRateArg,
                latestSourceTelemetry.Origin != SourceTelemetryOrigin.Unknown
                    ? latestSourceTelemetry.Origin.ToString()
                    : "SourceTelemetry");
        }

        var runtime = _context.GetRuntimeSnapshot();
        if (CaptureResolutionSelectionPolicy.TryParseResolutionKey(resolutionKey, out var selectedWidth, out var selectedHeight))
        {
            if (runtime.ActualFrameRate.HasValue &&
                runtime.ActualWidth == selectedWidth &&
                runtime.ActualHeight == selectedHeight)
            {
                return (
                    runtime.ActualFrameRate,
                    runtime.ActualFrameRateArg ??
                    runtime.NegotiatedFrameRateArg,
                    "NegotiatedDeviceFormat");
            }

            if (runtime.NegotiatedFrameRate.HasValue &&
                runtime.NegotiatedWidth == selectedWidth &&
                runtime.NegotiatedHeight == selectedHeight)
            {
                return (
                    runtime.NegotiatedFrameRate,
                    runtime.NegotiatedFrameRateArg,
                    "NegotiatedDeviceFormat");
            }
        }

        var selectedFormat = _context.GetSelectedFormat();
        if (selectedFormat != null &&
            options.Any(option => FrameRateTimingPolicy.IsFriendlyFrameRateMatch(option.FriendlyValue, selectedFormat.FrameRateExact)))
        {
            return (
                selectedFormat.FrameRateExact,
                string.IsNullOrWhiteSpace(selectedFormat.FrameRateRational)
                    ? null
                    : selectedFormat.FrameRateRational,
                "SelectedMode");
        }

        if (previousRate > 0 &&
            options.Any(option => FrameRateTimingPolicy.IsFriendlyFrameRateMatch(option.FriendlyValue, previousRate)))
        {
            return (previousRate, null, "SelectedMode");
        }

        return (null, null, "Unknown");
    }
}

internal sealed class MainViewModelRecordingCapabilityControllerContext
{
    public required string DefaultRecordingFormat { get; init; }
    public required string HevcRecordingFormat { get; init; }
    public required string Av1RecordingFormat { get; init; }
    public required Func<IReadOnlyCollection<string>> GetAvailableRecordingFormats { get; init; }
    public required Action<IReadOnlyList<string>> ReplaceAvailableRecordingFormats { get; init; }
    public required Func<string> GetSelectedRecordingFormat { get; init; }
    public required Action<string> SetSelectedRecordingFormat { get; init; }
    public required Action NotifySelectedRecordingFormatChanged { get; init; }
    public required Func<bool> IsHdrEnabled { get; init; }
    public required Action<string> SetStatusText { get; init; }
    public required Func<bool> IsFfmpegMissing { get; init; }
    public required Action<bool> SetIsFfmpegMissing { get; init; }
    public required Func<bool> HasUiThreadAccess { get; init; }
    public required Func<Action, bool> TryEnqueueOnUiThread { get; init; }
    public required Func<IReadOnlyCollection<string>> GetAvailableSplitEncodeModes { get; init; }
    public required Action<IReadOnlyList<string>> ReplaceAvailableSplitEncodeModes { get; init; }
    public required Func<string> GetSelectedSplitEncodeMode { get; init; }
    public required Action<string> SetSelectedSplitEncodeMode { get; init; }
    public required Func<string, bool> AvailableSplitEncodeModesContains { get; init; }
}

/// <summary>
/// Owns startup encoder/split-encode probing and observable option repair for
/// the MainViewModel compatibility facade.
/// </summary>
internal sealed class MainViewModelRecordingCapabilityController
{
    private readonly MainViewModelRecordingCapabilityControllerContext _context;
    private List<string> _detectedRecordingFormats = new();

    public MainViewModelRecordingCapabilityController(MainViewModelRecordingCapabilityControllerContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Start()
    {
        TrackStartupRefreshTask(RefreshRecordingFormatCapabilitiesAsync(), "recording formats");
        TrackStartupRefreshTask(RefreshSplitEncodeCapabilitiesAsync(), "split encode modes");
    }

    public void RebuildRecordingFormatOptions()
    {
        var selection = RecordingSettingsSelectionPolicy.Select(
            _detectedRecordingFormats,
            _context.GetAvailableRecordingFormats(),
            _context.GetSelectedRecordingFormat(),
            _context.IsHdrEnabled(),
            _context.DefaultRecordingFormat,
            _context.HevcRecordingFormat,
            _context.Av1RecordingFormat);

        _context.ReplaceAvailableRecordingFormats(selection.AvailableFormats);

        var previousSelection = _context.GetSelectedRecordingFormat();
        _context.SetSelectedRecordingFormat(selection.SelectedFormat);
        if (string.Equals(previousSelection, selection.SelectedFormat, StringComparison.Ordinal))
        {
            _context.NotifySelectedRecordingFormatChanged();
        }

        if (_context.IsHdrEnabled() &&
            !RecordingSettingsSelectionPolicy.IsHdrCompatible(_context.GetSelectedRecordingFormat()))
        {
            _context.SetStatusText("HDR recording requires HEVC or AV1 (10-bit).");
        }

        Logger.Log($"Selected recording format: {_context.GetSelectedRecordingFormat()}");
    }

    private static void TrackStartupRefreshTask(Task task, string description)
    {
        _ = task.ContinueWith(
            t => Logger.Log($"Startup {description} refresh failed: {t.Exception!.InnerException?.Message}"),
            TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task RefreshRecordingFormatCapabilitiesAsync()
    {
        var support = await FfmpegRuntimeLocator.GetEncoderSupportAsync();
        var formats = new List<string>();

        if (support.HasH264Nvenc)
        {
            formats.Add("H.264");
        }

        if (support.HasHevcNvenc)
        {
            formats.Add("HEVC");
        }

        if (support.HasAv1Nvenc)
        {
            formats.Add("AV1");
        }

        void ApplyFormats()
        {
            _detectedRecordingFormats = formats
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            _context.SetIsFfmpegMissing(_detectedRecordingFormats.Count == 0);
            if (_context.IsFfmpegMissing())
            {
                Logger.Log("FFMPEG_MISSING: encoder probe returned zero codecs. Recording unavailable.");
            }

            RebuildRecordingFormatOptions();
            Logger.Log($"Recording formats refreshed: {string.Join(", ", _detectedRecordingFormats)}");
        }

        if (_context.HasUiThreadAccess())
        {
            ApplyFormats();
        }
        else
        {
            if (!_context.TryEnqueueOnUiThread(ApplyFormats))
            {
                Logger.Log($"RECORDING_FORMATS_UI_ENQUEUE_FAILED formats={formats.Count}");
            }
        }
    }

    private async Task RefreshSplitEncodeCapabilitiesAsync()
    {
        var modes = new List<string> { "Auto", "Disabled", "2-way", "3-way" };
        var support = await FfmpegRuntimeLocator.GetSplitEncodeSupportAsync();
        if (!support.Supports2Way)
        {
            modes.Remove("2-way");
        }

        if (!support.Supports3Way)
        {
            modes.Remove("3-way");
        }

        void ApplyModes()
        {
            _context.ReplaceAvailableSplitEncodeModes(modes);

            if (!_context.AvailableSplitEncodeModesContains(_context.GetSelectedSplitEncodeMode()))
            {
                _context.SetSelectedSplitEncodeMode("Auto");
            }

            Logger.Log($"Split encode modes refreshed: {string.Join(", ", _context.GetAvailableSplitEncodeModes())}");
        }

        if (_context.HasUiThreadAccess())
        {
            ApplyModes();
        }
        else
        {
            if (!_context.TryEnqueueOnUiThread(ApplyModes))
            {
                Logger.Log($"SPLIT_ENCODE_MODES_UI_ENQUEUE_FAILED modes={modes.Count}");
            }
        }
    }
}

internal sealed class MainViewModelSourceTelemetryControllerContext
{
    public required Func<Action, bool> TryEnqueueOnUiThread { get; init; }
    public required Func<SourceSignalTelemetrySnapshot> GetLatestSourceTelemetry { get; init; }
    public required Action<SourceSignalTelemetrySnapshot> SetLatestSourceTelemetry { get; init; }
    public required Func<SourceSignalTelemetrySnapshot, DateTimeOffset, string> BuildSourceTelemetrySummary { get; init; }
    public required Action<int?> SetSourceWidth { get; init; }
    public required Action<int?> SetSourceHeight { get; init; }
    public required Action<bool?> SetSourceIsHdr { get; init; }
    public required Func<bool> IsRecording { get; init; }
    public required Func<bool> IsHdrEnabled { get; init; }
    public required Action<bool> SetIsHdrEnabled { get; init; }
    public required Action<string> SetSourceTelemetryAvailability { get; init; }
    public required Action<string> SetSourceTelemetryOriginDetail { get; init; }
    public required Action<string> SetSourceTelemetryConfidence { get; init; }
    public required Action<string?> SetSourceTelemetryDiagnosticSummary { get; init; }
    public required Func<DateTimeOffset?> GetSourceTelemetryTimestampUtc { get; init; }
    public required Action<DateTimeOffset?> SetSourceTelemetryTimestampUtc { get; init; }
    public required Action<double?> SetDetectedSourceFrameRate { get; init; }
    public required Action<string?> SetDetectedSourceFrameRateArg { get; init; }
    public required Action<string> SetSourceFrameRateOrigin { get; init; }
    public required Func<string> GetSourceTelemetrySummaryText { get; init; }
    public required Action<string> SetSourceTelemetrySummaryText { get; init; }
    public required Func<string?> GetLastSourceModeKey { get; init; }
    public required Action<string?> SetLastSourceModeKey { get; init; }
    public required Func<string?> GetSelectedResolution { get; init; }
    public required Func<string?, bool> IsAutoResolutionValue { get; init; }
    public required Func<bool> HasUserOverriddenResolutionForCurrentMode { get; init; }
    public required Action<bool> SetHasUserOverriddenResolutionForCurrentMode { get; init; }
    public required Func<bool> IsAutoFrameRateSelected { get; init; }
    public required Func<bool> HasUserOverriddenFrameRateForCurrentMode { get; init; }
    public required Action<bool> SetHasUserOverriddenFrameRateForCurrentMode { get; init; }
    public required Func<bool> ForceSourceAutoRetarget { get; init; }
    public required Action<bool> SetForceSourceAutoRetarget { get; init; }
    public required Func<int> AvailableResolutionCount { get; init; }
    public required Action<bool> SetPendingModeOptionsRefresh { get; init; }
    public required Action RebuildResolutionOptions { get; init; }
    public required Action UpdateTargetSummary { get; init; }
}

/// <summary>
/// Owns source telemetry ingress, UI projection, and source-aware retargeting
/// for the compatibility ViewModel facade.
/// </summary>
internal sealed class MainViewModelSourceTelemetryController
{
    private readonly MainViewModelSourceTelemetryControllerContext _context;

    private SourceTelemetryAvailability _lastAppliedTelemetryAvailability = SourceTelemetryAvailability.Unknown;
    private SourceTelemetryConfidence _lastAppliedTelemetryConfidence = SourceTelemetryConfidence.Unknown;
    private SourceTelemetryOrigin _lastAppliedFrameRateOrigin = SourceTelemetryOrigin.Unknown;
    private bool _hasAppliedTelemetryEnums;
    private int? _lastTelemetryAgeBucket;

    public MainViewModelSourceTelemetryController(MainViewModelSourceTelemetryControllerContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void RefreshSourceTelemetrySummaryAge()
    {
        var ageSeconds = TelemetryAgeHelper.ComputeAgeSeconds(_context.GetSourceTelemetryTimestampUtc(), DateTimeOffset.UtcNow);
        var ageBucket = ageSeconds.HasValue ? ageSeconds.Value / 5 : (int?)null;
        if (_lastTelemetryAgeBucket.HasValue &&
            ageBucket.HasValue &&
            _lastTelemetryAgeBucket.Value == ageBucket.GetValueOrDefault())
        {
            return;
        }

        var refreshedSummary = _context.BuildSourceTelemetrySummary(_context.GetLatestSourceTelemetry(), DateTimeOffset.UtcNow);
        if (!string.Equals(_context.GetSourceTelemetrySummaryText(), refreshedSummary, StringComparison.Ordinal))
        {
            _context.SetSourceTelemetrySummaryText(refreshedSummary);
        }

        _lastTelemetryAgeBucket = ageBucket;
    }

    public void OnSourceTelemetryUpdated(object? sender, SourceSignalTelemetrySnapshot snapshot)
    {
        if (!_context.TryEnqueueOnUiThread(() =>
        {
            ApplySourceTelemetrySnapshot(snapshot, allowAutoRetarget: true);
        }))
        {
            Logger.Log(
                $"SOURCE_TELEMETRY_UI_ENQUEUE_FAILED availability={snapshot.Availability} " +
                $"origin={snapshot.Origin} mode='{snapshot.GetModeKey()}'");
        }
    }

    public void ApplySourceTelemetrySnapshot(SourceSignalTelemetrySnapshot snapshot, bool allowAutoRetarget)
    {
        _context.SetLatestSourceTelemetry(snapshot);
        _context.SetSourceWidth(snapshot.Width);
        _context.SetSourceHeight(snapshot.Height);
        _context.SetSourceIsHdr(snapshot.IsHdr);
        if (!_context.IsRecording() && _context.IsHdrEnabled() && snapshot.IsHdr == false)
        {
            _context.SetIsHdrEnabled(false);
        }

        if (!_hasAppliedTelemetryEnums || _lastAppliedTelemetryAvailability != snapshot.Availability)
        {
            _context.SetSourceTelemetryAvailability(snapshot.Availability.ToString());
            _lastAppliedTelemetryAvailability = snapshot.Availability;
        }

        _context.SetSourceTelemetryOriginDetail(snapshot.OriginDetail);
        if (!_hasAppliedTelemetryEnums || _lastAppliedTelemetryConfidence != snapshot.Confidence)
        {
            _context.SetSourceTelemetryConfidence(snapshot.Confidence.ToString());
            _lastAppliedTelemetryConfidence = snapshot.Confidence;
        }

        _context.SetSourceTelemetryDiagnosticSummary(snapshot.DiagnosticSummary);
        _context.SetSourceTelemetryTimestampUtc(snapshot.TimestampUtc);
        _context.SetDetectedSourceFrameRate(snapshot.FrameRateExact);
        _context.SetDetectedSourceFrameRateArg(snapshot.FrameRateArg);
        if (!_hasAppliedTelemetryEnums || _lastAppliedFrameRateOrigin != snapshot.Origin)
        {
            _context.SetSourceFrameRateOrigin(snapshot.Origin != SourceTelemetryOrigin.Unknown
                ? snapshot.Origin.ToString()
                : "Unknown");
            _lastAppliedFrameRateOrigin = snapshot.Origin;
        }

        _hasAppliedTelemetryEnums = true;
        _lastTelemetryAgeBucket = null;
        _context.SetSourceTelemetrySummaryText(_context.BuildSourceTelemetrySummary(snapshot, DateTimeOffset.UtcNow));

        var modeKey = snapshot.GetModeKey();
        if (!string.IsNullOrWhiteSpace(modeKey) &&
            !string.Equals(modeKey, _context.GetLastSourceModeKey(), StringComparison.Ordinal))
        {
            if (allowAutoRetarget)
            {
                var shouldAutoRetargetResolution =
                    _context.IsAutoResolutionValue(_context.GetSelectedResolution()) ||
                    !_context.HasUserOverriddenResolutionForCurrentMode();
                var shouldAutoRetargetFrameRate =
                    _context.IsAutoFrameRateSelected() ||
                    !_context.HasUserOverriddenFrameRateForCurrentMode();
                _context.SetLastSourceModeKey(modeKey);
                _context.SetForceSourceAutoRetarget(shouldAutoRetargetResolution || shouldAutoRetargetFrameRate);
                if (shouldAutoRetargetResolution)
                {
                    _context.SetHasUserOverriddenResolutionForCurrentMode(false);
                }

                if (shouldAutoRetargetFrameRate)
                {
                    _context.SetHasUserOverriddenFrameRateForCurrentMode(false);
                }
            }
        }

        var shouldRebuildModeOptions = allowAutoRetarget &&
                                       (_context.ForceSourceAutoRetarget() ||
                                        (snapshot.HasSignalData && _context.AvailableResolutionCount() == 0));
        if (shouldRebuildModeOptions)
        {
            if (_context.IsRecording())
            {
                _context.SetPendingModeOptionsRefresh(true);
            }
            else
            {
                _context.RebuildResolutionOptions();
            }
        }
        else
        {
            _context.UpdateTargetSummary();
        }
    }
}
