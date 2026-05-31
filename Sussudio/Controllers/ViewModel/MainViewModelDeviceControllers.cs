using System;
using System.Collections.Generic;
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
