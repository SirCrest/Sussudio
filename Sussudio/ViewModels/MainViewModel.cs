using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Sussudio.Controllers;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Automation;
using Sussudio.Services.Capture;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Runtime;

namespace Sussudio.ViewModels;

internal readonly record struct MainViewModelSettingsLoadInput(
    IReadOnlyCollection<string> AvailableRecordingFormats,
    IReadOnlyCollection<string> AvailableQualities,
    IReadOnlyCollection<string> AvailablePresets,
    IReadOnlyCollection<string> AvailableSplitEncodeModes,
    IReadOnlyCollection<string> AvailableDeviceAudioModes,
    Func<string, bool> OutputDirectoryExists);

internal readonly record struct MainViewModelSettingsLoadPlan(
    string? OutputPath,
    string? SelectedRecordingFormat,
    string? UnavailableRecordingFormat,
    string? SelectedQuality,
    string? SelectedPreset,
    string? SelectedSplitEncodeMode,
    double? CustomBitrateMbps,
    bool? IsHdrEnabled,
    bool? IsAudioEnabled,
    bool? IsAudioPreviewEnabled,
    bool? IsCustomAudioInputEnabled,
    bool? IsMicrophoneEnabled,
    double? MicrophoneVolume,
    string? PendingMicrophoneVolumeDeviceId,
    double? PreviewVolume,
    bool? IsStatsVisible,
    string? SelectedDeviceAudioMode,
    double? AnalogAudioGainPercent,
    bool? FlashbackGpuDecode,
    int? FlashbackBufferMinutes,
    string? PendingDeviceId,
    string? PendingAudioDeviceId,
    string? PendingMicrophoneDeviceId,
    string? PendingDeviceAudioMode,
    double? PendingAnalogAudioGainPercent);

internal readonly record struct MainViewModelSettingsSaveInput(
    string? SelectedDeviceId,
    string OutputPath,
    string SelectedRecordingFormat,
    string SelectedQuality,
    string SelectedPreset,
    string SelectedSplitEncodeMode,
    double CustomBitrateMbps,
    bool IsHdrEnabled,
    bool IsAudioEnabled,
    bool IsAudioPreviewEnabled,
    bool IsCustomAudioInputEnabled,
    string? SelectedAudioInputDeviceId,
    bool IsMicrophoneEnabled,
    string? SelectedMicrophoneDeviceId,
    double MicrophoneVolume,
    double PreviewVolume,
    bool IsStatsVisible,
    string SelectedDeviceAudioMode,
    double AnalogAudioGainPercent,
    bool FlashbackGpuDecode,
    int FlashbackBufferMinutes);

/// <summary>
/// Pure settings projection between persisted settings and MainViewModel load state.
/// </summary>
internal static class MainViewModelSettingsPersistenceProjection
{
    internal static MainViewModelSettingsLoadPlan BuildLoadPlan(
        UserSettings settings,
        MainViewModelSettingsLoadInput input)
    {
        var outputPath = !string.IsNullOrWhiteSpace(settings.OutputPath) &&
            input.OutputDirectoryExists(settings.OutputPath)
                ? settings.OutputPath
                : null;

        var selectedRecordingFormat = ResolveAvailableValue(
            settings.SelectedRecordingFormat,
            input.AvailableRecordingFormats,
            StringComparer.Ordinal);
        var unavailableRecordingFormat = selectedRecordingFormat is null &&
            !string.IsNullOrWhiteSpace(settings.SelectedRecordingFormat)
                ? settings.SelectedRecordingFormat
                : null;

        var microphoneVolume = settings.MicrophoneVolume.HasValue
            ? Math.Clamp(settings.MicrophoneVolume.Value, 0.0, 100.0)
            : (double?)null;

        return new MainViewModelSettingsLoadPlan(
            OutputPath: outputPath,
            SelectedRecordingFormat: selectedRecordingFormat,
            UnavailableRecordingFormat: unavailableRecordingFormat,
            SelectedQuality: ResolveAvailableValue(settings.SelectedQuality, input.AvailableQualities, StringComparer.Ordinal),
            SelectedPreset: ResolveAvailableValue(settings.SelectedPreset, input.AvailablePresets, StringComparer.Ordinal),
            SelectedSplitEncodeMode: ResolveAvailableValue(settings.SelectedSplitEncodeMode, input.AvailableSplitEncodeModes, StringComparer.Ordinal),
            CustomBitrateMbps: settings.CustomBitrateMbps,
            IsHdrEnabled: settings.IsHdrEnabled,
            IsAudioEnabled: settings.IsAudioEnabled,
            IsAudioPreviewEnabled: settings.IsAudioPreviewEnabled,
            IsCustomAudioInputEnabled: settings.IsCustomAudioInputEnabled,
            IsMicrophoneEnabled: settings.IsMicrophoneEnabled,
            MicrophoneVolume: microphoneVolume,
            PendingMicrophoneVolumeDeviceId: microphoneVolume.HasValue ? settings.SelectedMicrophoneDeviceId : null,
            PreviewVolume: settings.PreviewVolume.HasValue ? Math.Clamp(settings.PreviewVolume.Value, 0.0, 1.0) : null,
            IsStatsVisible: settings.IsStatsVisible,
            SelectedDeviceAudioMode: ResolveAvailableValue(
                settings.SelectedDeviceAudioMode,
                input.AvailableDeviceAudioModes,
                StringComparer.OrdinalIgnoreCase),
            AnalogAudioGainPercent: settings.AnalogAudioGainPercent.HasValue
                ? Math.Clamp(settings.AnalogAudioGainPercent.Value, 0.0, 100.0)
                : null,
            FlashbackGpuDecode: settings.FlashbackGpuDecode,
            FlashbackBufferMinutes: settings.FlashbackBufferMinutes.HasValue
                ? Math.Clamp(settings.FlashbackBufferMinutes.Value, 1, 30)
                : null,
            PendingDeviceId: settings.SelectedDeviceId,
            PendingAudioDeviceId: settings.SelectedAudioInputDeviceId,
            PendingMicrophoneDeviceId: settings.SelectedMicrophoneDeviceId,
            PendingDeviceAudioMode: settings.SelectedDeviceAudioMode,
            PendingAnalogAudioGainPercent: settings.AnalogAudioGainPercent);
    }

    internal static UserSettings BuildSaveSettings(MainViewModelSettingsSaveInput input)
    {
        return new UserSettings
        {
            SelectedDeviceId = input.SelectedDeviceId,
            OutputPath = input.OutputPath,
            SelectedRecordingFormat = input.SelectedRecordingFormat,
            SelectedQuality = input.SelectedQuality,
            SelectedPreset = input.SelectedPreset,
            SelectedSplitEncodeMode = input.SelectedSplitEncodeMode,
            CustomBitrateMbps = input.CustomBitrateMbps,
            IsHdrEnabled = input.IsHdrEnabled,
            IsAudioEnabled = input.IsAudioEnabled,
            IsAudioPreviewEnabled = input.IsAudioPreviewEnabled,
            IsCustomAudioInputEnabled = input.IsCustomAudioInputEnabled,
            SelectedAudioInputDeviceId = input.SelectedAudioInputDeviceId,
            IsMicrophoneEnabled = input.IsMicrophoneEnabled,
            SelectedMicrophoneDeviceId = input.SelectedMicrophoneDeviceId,
            MicrophoneVolume = input.MicrophoneVolume,
            PreviewVolume = input.PreviewVolume,
            IsStatsVisible = input.IsStatsVisible,
            SelectedDeviceAudioMode = input.SelectedDeviceAudioMode,
            AnalogAudioGainPercent = input.AnalogAudioGainPercent,
            FlashbackGpuDecode = input.FlashbackGpuDecode,
            FlashbackBufferMinutes = input.FlashbackBufferMinutes,
        };
    }

    private static string? ResolveAvailableValue(
        string? savedValue,
        IEnumerable<string> availableValues,
        StringComparer comparer)
    {
        return !string.IsNullOrWhiteSpace(savedValue) &&
            availableValues.Contains(savedValue, comparer)
                ? savedValue
                : null;
    }
}

/// <summary>
/// UI-facing state coordinator. MainViewModel translates user settings and
/// automation requests into serialized CaptureService operations while keeping
/// WinUI properties, saved settings, and diagnostics summaries coherent.
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable, IAsyncDisposable, IAutomationViewModel
{
    private IntPtr _windowHandle;
    private const string LiveInfoUnavailable = "\u2014";
    private const int BitrateWindowMs = 10000;
    private const string DefaultRecordingFormat = "H.264";
    private const string HevcRecordingFormat = "HEVC";
    private const string Av1RecordingFormat = "AV1";
    private const int PreviewReinitializeDebounceMs = 250;
    private const string AutoResolutionValue = "Source";
    private const double AutoFrameRateValue = 0;
    private const string HdrToggleBlockedWhileRecordingMessage = "Stop recording before switching between HDR and SDR pipelines.";

    private readonly DeviceService _deviceService;
    private readonly CaptureService _captureService;
    private readonly CaptureSessionCoordinator _sessionCoordinator;
    private readonly Stopwatch _recordingStopwatch = new();
    private readonly BitrateSampleWindow _recordingBitrateSamples = new(BitrateWindowMs);
    private readonly AudioRampTraceRecorder _audioRampTraceRecorder;
    private readonly PreviewAudioVolumeTransitionController _previewAudioVolumeTransitionController;
    private readonly NativeXuAudioControlService _deviceAudioControlService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly AudioDeviceWatcher _audioDeviceWatcher;
    private readonly MainViewModelUiDispatchController _uiDispatchController;
    private readonly MainViewModelDeviceFormatProbeController _deviceFormatProbeController;
    private readonly MainViewModelSourceTelemetryController _sourceTelemetryController;
    private readonly MainViewModelDeviceRefreshController _deviceRefreshController;
    private readonly MainViewModelRuntimeLifecycleController _runtimeLifecycleController;
    private readonly MainViewModelRecordingTransitionController _recordingTransitionController;
    private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;
    private readonly MainViewModelDeviceAudioRequestController _deviceAudioRequestController;
    private readonly MainViewModelRecordingCapabilityController _recordingCapabilityController;
    private readonly MainViewModelCaptureSettingsAutomationController _captureSettingsAutomationController;
    private readonly MainViewModelRecordingSettingsAutomationController _recordingSettingsAutomationController;
    private readonly MainViewModelFrameRateTimingResolver _frameRateTimingResolver;
    private readonly MainViewModelCaptureModeOptionRebuildController _captureModeOptionRebuildController;
    private readonly MainViewModelDisposalController _disposalController;

    public MainViewModel()
        : this(MainViewModelDependencies.CreateDefault())
    {
    }

    internal MainViewModel(MainViewModelDependencies dependencies)
    {
        _deviceService = dependencies.DeviceService;
        _captureService = dependencies.CaptureService;
        _sessionCoordinator = dependencies.SessionCoordinator;
        _audioRampTraceRecorder = CreateAudioRampTraceRecorder();
        _previewAudioVolumeTransitionController = CreatePreviewAudioVolumeTransitionController();
        _deviceAudioControlService = dependencies.DeviceAudioControlService;
        _dispatcherQueue = dependencies.DispatcherQueue;
        _audioDeviceWatcher = dependencies.AudioDeviceWatcher;
        _frameRateTimingResolver = MainViewModelControllerGraph.CreateFrameRateTimingResolver(this);

        var controllerGraph = MainViewModelControllerGraph.Create(this);
        _uiDispatchController = controllerGraph.UiDispatchController;
        _recordingTransitionController = controllerGraph.RecordingTransitionController;
        _previewLifecycleController = controllerGraph.PreviewLifecycleController;
        _deviceAudioRequestController = controllerGraph.DeviceAudioRequestController;
        _recordingCapabilityController = controllerGraph.RecordingCapabilityController;
        _captureSettingsAutomationController = controllerGraph.CaptureSettingsAutomationController;
        _recordingSettingsAutomationController = controllerGraph.RecordingSettingsAutomationController;
        _captureModeOptionRebuildController = controllerGraph.CaptureModeOptionRebuildController;
        _deviceFormatProbeController = controllerGraph.DeviceFormatProbeController;
        _sourceTelemetryController = controllerGraph.SourceTelemetryController;
        _deviceRefreshController = controllerGraph.DeviceRefreshController;
        _runtimeLifecycleController = controllerGraph.RuntimeLifecycleController;
        _disposalController = controllerGraph.DisposalController;

        _runtimeLifecycleController.Start();
        _runtimeLifecycleController.InitializePresentation();
    }

    public Task InitializeAsync()
    {
        LoadSettings();
        StartRecordingCapabilityRefresh();
        return Task.CompletedTask;
    }

    partial void OnOutputPathChanged(string value)
    {
        SaveSettings();
    }

    partial void OnIsStatsVisibleChanged(bool value)
    {
        SaveSettings();
    }

    private void LoadSettings()
    {
        _isLoadingSettings = true;
        try
        {
            var settings = SettingsService.Load();
            var loadPlan = MainViewModelSettingsPersistenceProjection.BuildLoadPlan(
                settings,
                new MainViewModelSettingsLoadInput(
                    AvailableRecordingFormats,
                    AvailableQualities,
                    AvailablePresets,
                    AvailableSplitEncodeModes,
                    AvailableDeviceAudioModes,
                    Directory.Exists));

            if (!string.IsNullOrWhiteSpace(loadPlan.UnavailableRecordingFormat))
            {
                Logger.Log($"SETTINGS_LOAD: saved format '{loadPlan.UnavailableRecordingFormat}' not available, using default.");
            }

            ApplySettingsLoadPlan(loadPlan);
        }
        catch (Exception ex)
        {
            Logger.Log($"SETTINGS_LOAD: unexpected error: {ex.Message}");
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    private bool SaveSettings()
    {
        if (_isLoadingSettings)
        {
            return true;
        }

        try
        {
            var settings = MainViewModelSettingsPersistenceProjection.BuildSaveSettings(
                new MainViewModelSettingsSaveInput(
                    SelectedDevice?.Id,
                    OutputPath,
                    SelectedRecordingFormat,
                    SelectedQuality,
                    SelectedPreset,
                    SelectedSplitEncodeMode,
                    CustomBitrateMbps,
                    IsHdrEnabled,
                    IsAudioEnabled,
                    IsAudioPreviewEnabled,
                    IsCustomAudioInputEnabled,
                    SelectedAudioInputDevice?.Id,
                    IsMicrophoneEnabled,
                    SelectedMicrophoneDevice?.Id,
                    MicrophoneVolume,
                    VolumeSaveOverride ?? PreviewVolume,
                    IsStatsVisible,
                    SelectedDeviceAudioMode,
                    AnalogAudioGainPercent,
                    FlashbackGpuDecode,
                    FlashbackBufferMinutes));

            if (SettingsService.Save(settings, out var settingsSaveFailure))
            {
                return true;
            }

            StatusText = $"Settings save failed: {settingsSaveFailure}. Changes may revert after restart.";
            return false;
        }
        catch (Exception ex)
        {
            var failure = $"{ex.GetType().Name}: {ex.Message}";
            Logger.Log($"SETTINGS_SAVE: unexpected error: {failure}");
            StatusText = $"Settings save failed: {failure}. Changes may revert after restart.";
            return false;
        }
    }

    private void SaveSettingsOrThrow()
    {
        if (SaveSettings())
        {
            return;
        }

        throw new InvalidOperationException(StatusText);
    }

    private void ApplySettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)
    {
        ApplyRecordingSettingsLoadPlan(loadPlan);
        ApplyAudioSettingsLoadPlan(loadPlan);
        ApplyUiSettingsLoadPlan(loadPlan);
        ApplyDeviceAudioSettingsLoadPlan(loadPlan);
        ApplyFlashbackSettingsLoadPlan(loadPlan);
        StageDeferredDeviceSettingsLoadPlan(loadPlan);
    }

    private void ApplyRecordingSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)
    {
        if (loadPlan.OutputPath is not null)
        {
            OutputPath = loadPlan.OutputPath;
        }

        if (loadPlan.SelectedRecordingFormat is not null)
        {
            SelectedRecordingFormat = loadPlan.SelectedRecordingFormat;
        }

        if (loadPlan.SelectedQuality is not null)
        {
            SelectedQuality = loadPlan.SelectedQuality;
        }

        if (loadPlan.SelectedPreset is not null)
        {
            SelectedPreset = loadPlan.SelectedPreset;
        }

        if (loadPlan.SelectedSplitEncodeMode is not null)
        {
            SelectedSplitEncodeMode = loadPlan.SelectedSplitEncodeMode;
        }

        if (loadPlan.CustomBitrateMbps.HasValue)
        {
            CustomBitrateMbps = loadPlan.CustomBitrateMbps.Value;
        }

        if (loadPlan.IsHdrEnabled.HasValue)
        {
            IsHdrEnabled = loadPlan.IsHdrEnabled.Value;
        }
    }

    private void ApplyAudioSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)
    {
        if (loadPlan.IsAudioEnabled.HasValue)
        {
            IsAudioEnabled = loadPlan.IsAudioEnabled.Value;
        }

        if (loadPlan.IsAudioPreviewEnabled.HasValue)
        {
            IsAudioPreviewEnabled = loadPlan.IsAudioPreviewEnabled.Value;
        }

        if (loadPlan.IsCustomAudioInputEnabled.HasValue)
        {
            IsCustomAudioInputEnabled = loadPlan.IsCustomAudioInputEnabled.Value;
        }

        if (loadPlan.IsMicrophoneEnabled.HasValue)
        {
            IsMicrophoneEnabled = loadPlan.IsMicrophoneEnabled.Value;
        }

        if (loadPlan.MicrophoneVolume.HasValue)
        {
            MicrophoneVolume = loadPlan.MicrophoneVolume.Value;
            _pendingSavedMicrophoneVolume = loadPlan.MicrophoneVolume.Value;
            _pendingSavedMicrophoneVolumeDeviceId = loadPlan.PendingMicrophoneVolumeDeviceId;
        }

        if (loadPlan.PreviewVolume.HasValue)
        {
            PreviewVolume = loadPlan.PreviewVolume.Value;
        }
    }

    private void ApplyUiSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)
    {
        if (loadPlan.IsStatsVisible.HasValue)
        {
            IsStatsVisible = loadPlan.IsStatsVisible.Value;
        }
    }

    private void ApplyDeviceAudioSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)
    {
        if (loadPlan.SelectedDeviceAudioMode is not null)
        {
            SelectedDeviceAudioMode = loadPlan.SelectedDeviceAudioMode;
        }

        if (loadPlan.AnalogAudioGainPercent.HasValue)
        {
            AnalogAudioGainPercent = loadPlan.AnalogAudioGainPercent.Value;
        }
    }

    private void ApplyFlashbackSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)
    {
        if (loadPlan.FlashbackGpuDecode.HasValue)
        {
            FlashbackGpuDecode = loadPlan.FlashbackGpuDecode.Value;
        }

        if (loadPlan.FlashbackBufferMinutes.HasValue)
        {
            FlashbackBufferMinutes = loadPlan.FlashbackBufferMinutes.Value;
        }
    }

    private void StageDeferredDeviceSettingsLoadPlan(MainViewModelSettingsLoadPlan loadPlan)
    {
        // Defer device selection until RefreshDevicesAsync populates the device list.
        _pendingSavedDeviceId = loadPlan.PendingDeviceId;
        _pendingSavedAudioDeviceId = loadPlan.PendingAudioDeviceId;
        _pendingSavedMicrophoneDeviceId = loadPlan.PendingMicrophoneDeviceId;
        _pendingSavedDeviceAudioMode = loadPlan.PendingDeviceAudioMode;
        _pendingSavedAnalogAudioGainPercent = loadPlan.PendingAnalogAudioGainPercent;
    }

    public Task RefreshDevicesAsync(CancellationToken cancellationToken = default)
        => _deviceRefreshController.RefreshDevicesAsync(cancellationToken);

    internal Task RefreshDevicesForStartupAsync(CancellationToken cancellationToken = default)
        => _deviceRefreshController.RefreshDevicesAsync(cancellationToken, throwOnScanFailure: true);

    internal void SetPreviewFrameSink(IPreviewFrameSink? sink)
    {
        _captureService.SetPreviewFrameSink(sink);
    }

    internal void CancelPendingPreviewRestart()
        => _previewLifecycleController.CancelPendingPreviewRestart();

    private Task InitializeDeviceAsync(CancellationToken cancellationToken = default)
        => _previewLifecycleController.InitializeDeviceAsync(cancellationToken);

    public Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)
        => _previewLifecycleController.StartPreviewAsync(userInitiated, cancellationToken);

    public Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => _previewLifecycleController.SetPreviewEnabledAsync(enabled, cancellationToken);

    public Task StopPreviewAsync()
        => StopPreviewAsync(userInitiated: true, teardownPipeline: false, CancellationToken.None);

    public Task StopPreviewAsync(bool userInitiated)
        => StopPreviewAsync(userInitiated, teardownPipeline: false, CancellationToken.None);

    public Task StopPreviewAsync(bool userInitiated, bool teardownPipeline)
        => StopPreviewAsync(userInitiated, teardownPipeline, CancellationToken.None);

    public Task ApplySelectedDeviceAsync(CaptureDevice device, CancellationToken cancellationToken = default)
        => _previewLifecycleController.ApplySelectedDeviceAsync(device, cancellationToken);

    private Task<bool> ApplySelectedDeviceWithResultAsync(CaptureDevice device, CancellationToken cancellationToken = default)
        => _previewLifecycleController.ApplySelectedDeviceWithResultAsync(device, cancellationToken);

    private Task ReinitializeDeviceAsync(string reason)
        => _previewLifecycleController.ReinitializeDeviceAsync(reason);

    private Task<bool> ReinitializeDeviceWithResultAsync(string reason)
        => _previewLifecycleController.ReinitializeDeviceWithResultAsync(reason);

    public Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)
        => _previewLifecycleController.StopPreviewAsync(userInitiated, teardownPipeline, cancellationToken);

    public Action<string, bool>? StatsSectionVisibilityHandler { get; set; }
    public Action<bool>? FrameTimeOverlayVisibilityHandler { get; set; }

    public void SetWindowHandle(IntPtr handle)
    {
        _windowHandle = handle;
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IReadOnlyList<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    [ObservableProperty]
    public partial bool IsStatsVisible { get; set; }

    [ObservableProperty]
    public partial bool IsSettingsVisible { get; set; }

    [ObservableProperty]
    public partial bool IsPreviewing { get; set; }

    [ObservableProperty]
    public partial bool IsPreviewReinitializing { get; set; }

    [ObservableProperty]
    public partial bool IsInitialized { get; set; }

    private readonly SemaphoreSlim _previewReinitializeGate = new(1, 1);
    private int _previewReinitializeGeneration;
    private bool _cancelPreviewRestartAfterReinitialize;

    // Resolution capability matrix keyed by "{width}x{height}".
    private readonly Dictionary<string, List<MediaFormat>> _resolutionToFormats =
        new(StringComparer.OrdinalIgnoreCase);
    private bool _isRebuildingModeOptions;
    private bool _isApplyingAutomaticFrameRateSelection;
    private bool _isApplyingAutomaticResolutionSelection;
    private bool _isAutoFrameRateSelected = true;
    private bool _hasUserOverriddenFrameRateForCurrentMode;
    private bool _hasUserOverriddenResolutionForCurrentMode;
    private bool _forceSourceAutoRetarget;
    private string? _lastSourceModeKey;
    private string? _lastKnownResolutionKey;
    private bool _pendingSdrAutoSelectionForDeviceChange;
    private int? _pendingSdrAutoFriendlyFrameRateBucket;
    private long _deviceScanGeneration;

    // Flag to prevent reinitialization during initial device setup.
    private bool _isChangingDevice;
    private bool _isLoadingSettings;
    private string? _pendingSavedDeviceId;
    private SourceSignalTelemetrySnapshot _latestSourceTelemetry = SourceSignalTelemetrySnapshot.CreateUnavailable("telemetry-not-started");
    private bool _pendingModeOptionsRefresh;
    private bool _suppressFormatChangeReinitialize;
    private bool _suppressHdrToggleReinitialize;
    private bool _isRevertingHdrToggle;

    /// <summary>
    /// Capture-device, resolution, and frame-rate selection reactions.
    /// Capture-mode transactions that coordinate option rebuilds, HDR/SDR changes,
    /// and active-preview reinitialization without duplicate property-change cascades.
    /// </summary>
    private void RebuildResolutionOptions()
        => _captureModeOptionRebuildController.RebuildResolutionOptions();

    private void RebuildFrameRateOptions()
        => _captureModeOptionRebuildController.RebuildFrameRateOptions();

    private void UpdateSelectedFormat()
        => _captureModeOptionRebuildController.UpdateSelectedFormat();

    private void RebuildVideoFormatOptions()
        => _captureModeOptionRebuildController.RebuildVideoFormatOptions();

    private MainViewModelCaptureSelectionSnapshot CaptureSelectionSnapshot()
        => new(
            SelectedDevice,
            AvailableFormats.ToArray(),
            _resolutionToFormats
                .Select(pair => new KeyValuePair<string, MediaFormat[]>(pair.Key, pair.Value.ToArray()))
                .ToArray(),
            AvailableResolutions.ToArray(),
            AvailableFrameRates.ToArray(),
            AvailableVideoFormats.ToArray(),
            SelectedResolution,
            AutoResolvedWidth,
            AutoResolvedHeight,
            SelectedFrameRate,
            AutoResolvedFrameRate,
            SelectedVideoFormat,
            MjpegDecoderCount,
            SelectedFormat,
            IsHdrAvailable,
            IsHdrEnabled,
            IsAutoFrameRateSelected,
            SelectedFriendlyFrameRate,
            SelectedExactFrameRate,
            SelectedExactFrameRateArg,
            DisabledResolutionReason,
            DisabledFrameRateReason,
            HdrResolutionSupportHint,
            AvailableRecordingFormats.ToArray(),
            SelectedRecordingFormat,
            _latestSourceTelemetry,
            SourceWidth,
            SourceHeight,
            SourceIsHdr,
            SourceTelemetryAvailability,
            SourceTelemetryOriginDetail,
            SourceTelemetryConfidence,
            SourceTelemetryDiagnosticSummary,
            SourceTelemetryTimestampUtc,
            DetectedSourceFrameRate,
            DetectedSourceFrameRateArg,
            SourceFrameRateOrigin,
            SourceTelemetrySummaryText,
            SourceTargetSummaryText,
            _hasUserOverriddenResolutionForCurrentMode,
            _hasUserOverriddenFrameRateForCurrentMode,
            _pendingSdrAutoSelectionForDeviceChange,
            _pendingSdrAutoFriendlyFrameRateBucket,
            _forceSourceAutoRetarget,
            _lastKnownResolutionKey,
            _lastSourceModeKey,
            _pendingModeOptionsRefresh);

    private bool RestoreCaptureSelectionSnapshotIfUnchanged(
        MainViewModelCaptureSelectionSnapshot snapshot,
        MainViewModelCaptureSelectionSnapshot expectedCurrent)
    {
        if (!CaptureSelectionSnapshot().MatchesSelectionState(expectedCurrent))
        {
            return false;
        }

        RestoreCaptureSelectionSnapshot(snapshot);
        return true;
    }

    private void RestoreCaptureSelectionSnapshot(MainViewModelCaptureSelectionSnapshot snapshot)
    {
        var previousSuppressFormatChangeReinitialize = _suppressFormatChangeReinitialize;
        var previousSuppressHdrToggleReinitialize = _suppressHdrToggleReinitialize;
        var previousRevertingHdrToggle = _isRevertingHdrToggle;
        var previousChangingDevice = _isChangingDevice;
        var previousRebuildingModeOptions = _isRebuildingModeOptions;
        var previousApplyingAutomaticResolutionSelection = _isApplyingAutomaticResolutionSelection;
        var previousApplyingAutomaticFrameRateSelection = _isApplyingAutomaticFrameRateSelection;
        var previousSuppressFlashbackFormatCycle = _suppressFlashbackFormatCycle;
        _suppressFormatChangeReinitialize = true;
        _suppressHdrToggleReinitialize = true;
        _isRevertingHdrToggle = true;
        _isChangingDevice = true;
        _isRebuildingModeOptions = true;
        _isApplyingAutomaticResolutionSelection = true;
        _isApplyingAutomaticFrameRateSelection = true;
        _suppressFlashbackFormatCycle = true;
        try
        {
            if (!ReferenceEquals(SelectedDevice, snapshot.SelectedDevice))
            {
                SelectedDevice = snapshot.SelectedDevice;
            }

            _isChangingDevice = true;
            RestoreCollection(AvailableFormats, snapshot.AvailableFormats);
            _resolutionToFormats.Clear();
            foreach (var pair in snapshot.ResolutionToFormats)
            {
                _resolutionToFormats[pair.Key] = pair.Value.ToList();
            }

            RestoreCollection(AvailableResolutions, snapshot.AvailableResolutions);
            RestoreCollection(AvailableFrameRates, snapshot.AvailableFrameRates);
            RestoreCollection(AvailableVideoFormats, snapshot.AvailableVideoFormats);
            RestoreCollection(AvailableRecordingFormats, snapshot.AvailableRecordingFormats);

            IsHdrAvailable = snapshot.IsHdrAvailable;
            IsHdrEnabled = snapshot.IsHdrEnabled;
            SelectedResolution = snapshot.SelectedResolution;
            AutoResolvedWidth = snapshot.AutoResolvedWidth;
            AutoResolvedHeight = snapshot.AutoResolvedHeight;
            SelectedFrameRate = snapshot.SelectedFrameRate;
            AutoResolvedFrameRate = snapshot.AutoResolvedFrameRate;
            SelectedVideoFormat = snapshot.SelectedVideoFormat;
            MjpegDecoderCount = snapshot.MjpegDecoderCount;
            SelectedFormat = snapshot.SelectedFormat;
            IsAutoFrameRateSelected = snapshot.IsAutoFrameRateSelected;
            SelectedFriendlyFrameRate = snapshot.SelectedFriendlyFrameRate;
            SelectedExactFrameRate = snapshot.SelectedExactFrameRate;
            SelectedExactFrameRateArg = snapshot.SelectedExactFrameRateArg;
            DisabledResolutionReason = snapshot.DisabledResolutionReason;
            DisabledFrameRateReason = snapshot.DisabledFrameRateReason;
            HdrResolutionSupportHint = snapshot.HdrResolutionSupportHint;
            SelectedRecordingFormat = snapshot.SelectedRecordingFormat;
            _latestSourceTelemetry = snapshot.LatestSourceTelemetry;
            SourceWidth = snapshot.SourceWidth;
            SourceHeight = snapshot.SourceHeight;
            SourceIsHdr = snapshot.SourceIsHdr;
            SourceTelemetryAvailability = snapshot.SourceTelemetryAvailability;
            SourceTelemetryOriginDetail = snapshot.SourceTelemetryOriginDetail;
            SourceTelemetryConfidence = snapshot.SourceTelemetryConfidence;
            SourceTelemetryDiagnosticSummary = snapshot.SourceTelemetryDiagnosticSummary;
            SourceTelemetryTimestampUtc = snapshot.SourceTelemetryTimestampUtc;
            DetectedSourceFrameRate = snapshot.DetectedSourceFrameRate;
            DetectedSourceFrameRateArg = snapshot.DetectedSourceFrameRateArg;
            SourceFrameRateOrigin = snapshot.SourceFrameRateOrigin;
            SourceTelemetrySummaryText = snapshot.SourceTelemetrySummaryText;
            SourceTargetSummaryText = snapshot.SourceTargetSummaryText;
            _hasUserOverriddenResolutionForCurrentMode = snapshot.HasUserOverriddenResolutionForCurrentMode;
            _hasUserOverriddenFrameRateForCurrentMode = snapshot.HasUserOverriddenFrameRateForCurrentMode;
            _pendingSdrAutoSelectionForDeviceChange = snapshot.PendingSdrAutoSelectionForDeviceChange;
            _pendingSdrAutoFriendlyFrameRateBucket = snapshot.PendingSdrAutoFriendlyFrameRateBucket;
            _forceSourceAutoRetarget = snapshot.ForceSourceAutoRetarget;
            _lastKnownResolutionKey = snapshot.LastKnownResolutionKey;
            _lastSourceModeKey = snapshot.LastSourceModeKey;
            _pendingModeOptionsRefresh = snapshot.PendingModeOptionsRefresh;
            UpdateTargetSummary();
            SaveSettings();
        }
        finally
        {
            _suppressFlashbackFormatCycle = previousSuppressFlashbackFormatCycle;
            _isApplyingAutomaticFrameRateSelection = previousApplyingAutomaticFrameRateSelection;
            _isApplyingAutomaticResolutionSelection = previousApplyingAutomaticResolutionSelection;
            _isRebuildingModeOptions = previousRebuildingModeOptions;
            _isChangingDevice = previousChangingDevice;
            _isRevertingHdrToggle = previousRevertingHdrToggle;
            _suppressHdrToggleReinitialize = previousSuppressHdrToggleReinitialize;
            _suppressFormatChangeReinitialize = previousSuppressFormatChangeReinitialize;
        }
    }

    private static void RestoreCollection<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values)
        {
            target.Add(value);
        }
    }

    partial void OnSelectedDeviceChanged(CaptureDevice? value)
    {
        CancelPendingAudioControlWork();
        RebuildSelectedDeviceCapabilities(value, resetTelemetryState: true);
        RequestDeviceAudioControlsRefresh(value);
        SaveSettings();
    }

    private void RebuildSelectedDeviceCapabilities(CaptureDevice? device, bool resetTelemetryState)
    {
        _isChangingDevice = true;
        var preserveSourceTelemetryForActiveSourceSelection =
            resetTelemetryState &&
            device != null &&
            IsPreviewing &&
            IsAutoResolutionValue(SelectedResolution) &&
            _latestSourceTelemetry.HasDimensions;
        try
        {
            ResetFrameRateSelectionState();
            HdrResolutionSupportHint = string.Empty;

            AvailableFormats.Clear();
            AvailableFrameRates.Clear();
            _resolutionToFormats.Clear();
            if (resetTelemetryState)
            {
                _pendingSdrAutoSelectionForDeviceChange = device != null && !IsHdrEnabled;
                _pendingSdrAutoFriendlyFrameRateBucket = null;
                if (!preserveSourceTelemetryForActiveSourceSelection)
                {
                    _sourceTelemetryController.ApplySourceTelemetrySnapshot(
                        SourceSignalTelemetrySnapshot.CreateUnavailable("awaiting-source-telemetry"),
                        allowAutoRetarget: false);
                }
            }

            if (device != null)
            {
                var unsupportedFormatReasons = new HashSet<string>(StringComparer.Ordinal);
                foreach (var format in device.SupportedFormats)
                {
                    if (!DeviceModeSupportPolicy.IsSupported(device, format))
                    {
                        unsupportedFormatReasons.Add(DeviceModeSupportPolicy.DescribeUnsupported(device, format));
                        continue;
                    }

                    AvailableFormats.Add(format);

                    var resolutionKey = GetResolutionKey(format.Width, format.Height);
                    if (!_resolutionToFormats.TryGetValue(resolutionKey, out var formats))
                    {
                        formats = new List<MediaFormat>();
                        _resolutionToFormats[resolutionKey] = formats;
                    }

                    formats.Add(format);
                }

                IsHdrAvailable = device.IsHdrCapable &&
                    AvailableFormats.Any(CaptureModeOptionsBuilder.IsHdrModeCandidate);
                if (!IsHdrAvailable)
                {
                    IsHdrEnabled = false;
                }
                else if (unsupportedFormatReasons.Count > 0)
                {
                    HdrResolutionSupportHint = unsupportedFormatReasons.First();
                }
            }

            if (IsRecording)
            {
                _pendingModeOptionsRefresh = true;
            }
            else
            {
                RebuildResolutionOptions();
            }
        }
        finally
        {
            _isChangingDevice = false;
        }
    }

    private static bool IsAutoResolutionValue(string? resolutionValue)
        => string.Equals(resolutionValue, AutoResolutionValue, StringComparison.OrdinalIgnoreCase);

    private bool TryResolveResolutionKey(string? resolutionValue, out string resolutionKey)
    {
        resolutionKey = string.Empty;
        if (string.IsNullOrWhiteSpace(resolutionValue))
        {
            return false;
        }

        if (IsAutoResolutionValue(resolutionValue))
        {
            if (AutoResolvedWidth.HasValue &&
                AutoResolvedHeight.HasValue &&
                AutoResolvedWidth.Value > 0 &&
                AutoResolvedHeight.Value > 0)
            {
                resolutionKey = GetResolutionKey(AutoResolvedWidth.Value, AutoResolvedHeight.Value);
                return true;
            }

            return false;
        }

        if (!TryParseResolutionKey(resolutionValue, out var width, out var height))
        {
            return false;
        }

        resolutionKey = GetResolutionKey(width, height);
        return true;
    }

    private void RefreshAutoResolvedResolutionFromLatestSource()
    {
        if (!_latestSourceTelemetry.HasDimensions)
        {
            return;
        }

        var width = (uint)_latestSourceTelemetry.Width!.Value;
        var height = (uint)_latestSourceTelemetry.Height!.Value;
        var resolutionKey = GetResolutionKey(width, height);
        if (!_resolutionToFormats.ContainsKey(resolutionKey))
        {
            return;
        }

        AutoResolvedWidth = width;
        AutoResolvedHeight = height;
        if (_latestSourceTelemetry.HasFrameRate)
        {
            AutoResolvedFrameRate = _latestSourceTelemetry.FrameRateExact;
        }
    }

    private string? GetEffectiveResolutionKey(string? resolutionValue)
        => TryResolveResolutionKey(resolutionValue, out var resolutionKey)
            ? resolutionKey
            : null;

    private bool TryGetEffectiveResolutionSelection(out string resolutionKey, out uint width, out uint height)
    {
        resolutionKey = string.Empty;
        width = 0;
        height = 0;

        if (!TryResolveResolutionKey(SelectedResolution, out resolutionKey) ||
            !TryParseResolutionKey(resolutionKey, out width, out height))
        {
            resolutionKey = string.Empty;
            width = 0;
            height = 0;
            return false;
        }

        return true;
    }

    private static bool TryParseResolutionKey(string? resolutionKey, out uint width, out uint height)
        => CaptureResolutionSelectionPolicy.TryParseResolutionKey(resolutionKey, out width, out height);

    private static string GetResolutionKey(uint width, uint height)
        => $"{width}x{height}";

    private bool ResolutionSupportsFrameRate(string resolutionKey, double frameRate, bool hdrOnly)
        => CaptureResolutionSelectionPolicy.ResolutionSupportsFrameRate(
            _resolutionToFormats,
            resolutionKey,
            frameRate,
            hdrOnly);

    private bool ResolutionSupportsFriendlyFrameRate(
        string resolutionKey,
        int friendlyBucket,
        bool hdrOnly,
        bool sdrOnly)
        => CaptureResolutionSelectionPolicy.ResolutionSupportsFriendlyFrameRate(
            _resolutionToFormats,
            resolutionKey,
            friendlyBucket,
            hdrOnly,
            sdrOnly);

    private string BuildHdrSupportHintForResolution(string? resolutionKey)
        => CaptureResolutionSelectionPolicy.BuildHdrSupportHint(new HdrSupportHintRequest(
            _resolutionToFormats,
            resolutionKey,
            IsHdrEnabled,
            SelectedFrameRate));

    partial void OnSelectedResolutionChanged(string? value)
    {
        if (IsAutoResolutionValue(value))
        {
            RefreshAutoResolvedResolutionFromLatestSource();
        }

        if (!IsAutoResolutionValue(value) &&
            TryResolveResolutionKey(value, out var resolvedResolutionKey))
        {
            _lastKnownResolutionKey = resolvedResolutionKey;
        }

        if (!_isRebuildingModeOptions && !_isApplyingAutomaticResolutionSelection)
        {
            _hasUserOverriddenResolutionForCurrentMode = !IsAutoResolutionValue(value);
            _pendingSdrAutoSelectionForDeviceChange = false;
            _pendingSdrAutoFriendlyFrameRateBucket = null;
        }

        if (_isRebuildingModeOptions)
        {
            return;
        }

        _forceSourceAutoRetarget = false;
        ResetFrameRateSelectionState();
        RebuildFrameRateOptions();
        UpdateTargetSummary();
    }

    partial void OnSelectedFormatChanged(MediaFormat? value)
    {
        if (value != null && !_isChangingDevice && !_suppressFormatChangeReinitialize && IsPreviewing && IsInitialized)
        {
            Logger.Log($"=== Format changed to {value.Width}x{value.Height}@{value.FrameRate}fps - reinitializing device ===");
            EnqueueUiOperation(() => ReinitializeDeviceAsync("format change"), "format change reinitialize");
        }
    }

    partial void OnSelectedVideoFormatChanged(string value)
    {
        if (!_isRebuildingModeOptions)
        {
            var previousSuppress = _suppressFormatChangeReinitialize;
            _suppressFormatChangeReinitialize = true;
            try
            {
                UpdateSelectedFormat();
            }
            finally
            {
                _suppressFormatChangeReinitialize = previousSuppress;
            }
        }

        if (!_isChangingDevice && !_suppressFormatChangeReinitialize && IsPreviewing && IsInitialized)
        {
            Logger.Log($"=== Video format override changed to {value} - reinitializing device ===");
            EnqueueUiOperation(() => ReinitializeDeviceAsync("video format override"), "video format override reinitialize");
        }
    }

    partial void OnSelectedFrameRateChanged(double value)
    {
        if (FrameRateTimingPolicy.IsAutoFrameRateValue(value))
        {
            SelectAutoFrameRate(rebuildOptions: !IsRecording && !_isRebuildingModeOptions && !_isApplyingAutomaticFrameRateSelection);
            return;
        }

        if (!_isRebuildingModeOptions && !_isApplyingAutomaticFrameRateSelection)
        {
            IsAutoFrameRateSelected = false;
            _hasUserOverriddenFrameRateForCurrentMode = true;
            _pendingSdrAutoSelectionForDeviceChange = false;
            _pendingSdrAutoFriendlyFrameRateBucket = null;
        }

        var selected = AvailableFrameRates
            .FirstOrDefault(option => FrameRateTimingPolicy.IsFrameRateMatch(option.Value, value))
            ?? AvailableFrameRates.FirstOrDefault(option => FrameRateTimingPolicy.IsFriendlyFrameRateMatch(option.FriendlyValue, value));
        SelectedFriendlyFrameRate = selected?.FriendlyValue ?? Math.Round(value, MidpointRounding.AwayFromZero);
        SelectedExactFrameRate = selected?.Value ?? value;
        SelectedExactFrameRateArg = selected?.Rational;
        if (IsAutoResolutionValue(SelectedResolution))
        {
            AutoResolvedFrameRate = selected?.Value ?? value;
        }

        RebuildVideoFormatOptions();
        UpdateSelectedFormat();
        UpdateTargetSummary();
    }

    public void SelectAutoFrameRate()
        => SelectAutoFrameRate(rebuildOptions: !IsRecording && !_isRebuildingModeOptions && !_isApplyingAutomaticFrameRateSelection);

    private void SelectAutoFrameRate(bool rebuildOptions)
    {
        IsAutoFrameRateSelected = true;
        _hasUserOverriddenFrameRateForCurrentMode = false;
        _pendingSdrAutoSelectionForDeviceChange = false;
        _pendingSdrAutoFriendlyFrameRateBucket = null;

        if (rebuildOptions)
        {
            RebuildFrameRateOptions();
            return;
        }

        var currentOptions = AvailableFrameRates
            .Where(option => !FrameRateTimingPolicy.IsAutoFrameRateValue(option.FriendlyValue))
            .ToList();
        var selectedResolutionKey = GetEffectiveResolutionKey(SelectedResolution);
        var sourceRate = _frameRateTimingResolver.ResolveDetectedSourceFrameRate(selectedResolutionKey, currentOptions, SelectedFrameRate);
        var sourceTimingFamilyKnown = FrameRateTimingPolicy.TryInferFrameRateTimingFamily(sourceRate.Arg, sourceRate.Rate, out var sourceTimingFamily);
        var selection = FrameRateAutoSelectionPolicy.Select(new FrameRateAutoSelectionRequest(
            currentOptions,
            AutoFrameRateOptionAvailable: false,
            ForceAutoSelection: true,
            IsAutoFrameRateSelected: IsAutoFrameRateSelected,
            HasUserOverriddenFrameRateForCurrentMode: _hasUserOverriddenFrameRateForCurrentMode,
            IsHdrEnabled: IsHdrEnabled,
            PendingSdrAutoSelectionForDeviceChange: _pendingSdrAutoSelectionForDeviceChange,
            PendingSdrAutoFriendlyFrameRateBucket: _pendingSdrAutoFriendlyFrameRateBucket,
            Source: new FrameRateAutoSelectionSource(sourceRate.Rate, sourceTimingFamilyKnown, sourceTimingFamily),
            PreviousRate: SelectedFrameRate));

        ApplyResolvedFrameRateSelection(selection.Selected, SelectedFrameRate > 0 ? SelectedFrameRate : 60);
        UpdateSelectedFormat();
        UpdateTargetSummary();
    }

    partial void OnMjpegDecoderCountChanged(int value)
    {
        var clamped = Math.Clamp(value, 1, 8);
        if (clamped != value)
        {
            MjpegDecoderCount = clamped;
            return;
        }

        if (!_isChangingDevice &&
            IsPreviewing &&
            IsInitialized &&
            BuildCaptureSettings().UseMjpegHighFrameRateMode)
        {
            Logger.Log($"=== MJPEG decoder count changed to {value} - reinitializing device ===");
            EnqueueUiOperation(() => ReinitializeDeviceAsync("mjpeg decoder count"), "mjpeg decoder count reinitialize");
        }
    }

    public Task SetHdrEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(async () =>
        {
            if (IsRecording)
            {
                throw new InvalidOperationException(HdrToggleBlockedWhileRecordingMessage);
            }

            if (enabled && !IsHdrAvailable)
            {
                throw new InvalidOperationException("HDR is not available on the selected device.");
            }

            if (IsHdrEnabled == enabled)
            {
                return;
            }

            var rollback = CaptureSelectionSnapshot();
            var shouldReinitialize = IsInitialized && SelectedDevice != null && SelectedFormat != null;
            _suppressHdrToggleReinitialize = true;
            try
            {
                IsHdrEnabled = enabled;
            }
            finally
            {
                _suppressHdrToggleReinitialize = false;
            }

            var attempted = CaptureSelectionSnapshot();
            if (shouldReinitialize && SelectedFormat != null)
            {
                var reinitialized = await ReinitializeDeviceWithResultAsync("automation HDR toggle").ConfigureAwait(true);
                if (!reinitialized)
                {
                    var restored = RestoreCaptureSelectionSnapshotIfUnchanged(rollback, attempted);
                    var rollbackStatus = restored
                        ? "restored previous capture selection"
                        : "a newer capture selection superseded this request";
                    throw new InvalidOperationException($"Failed to apply automation HDR toggle; {rollbackStatus}.");
                }
            }

            SaveSettingsOrThrow();
        }, cancellationToken);
    }

    public Task SetTrueHdrPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            if (IsRecording)
            {
                throw new InvalidOperationException("True HDR preview cannot be changed while recording.");
            }

            IsTrueHdrPreviewEnabled = enabled;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    partial void OnIsHdrEnabledChanged(bool value)
    {
        if (_isRevertingHdrToggle)
        {
            return;
        }

        if (value)
        {
            _pendingSdrAutoSelectionForDeviceChange = false;
            _pendingSdrAutoFriendlyFrameRateBucket = null;
        }

        if (IsRecording)
        {
            _isRevertingHdrToggle = true;
            try
            {
                IsHdrEnabled = !value;
            }
            finally
            {
                _isRevertingHdrToggle = false;
            }

            StatusText = HdrToggleBlockedWhileRecordingMessage;
            return;
        }

        if (!_isChangingDevice)
        {
            _suppressFormatChangeReinitialize = true;
            try
            {
                ResetModeSelectionState();
                RebuildResolutionOptions();
                RebuildRecordingFormatOptions();
            }
            finally
            {
                _suppressFormatChangeReinitialize = false;
            }

            if (!_suppressHdrToggleReinitialize && IsInitialized && !IsRecording && SelectedDevice != null && SelectedFormat != null)
            {
                Logger.Log($"HDR toggle changed to {(value ? "On" : "Off")} - forcing immediate device renegotiation");
                EnqueueUiOperation(() => ReinitializeDeviceAsync("HDR toggle"), "hdr toggle reinitialize");
            }
        }

        SaveSettings();
    }

    public Task ToggleRecordingAsync()
        => _recordingTransitionController.ToggleRecordingAsync();

    public Task SetRecordingEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => SetRecordingDesiredStateAsync(enabled, cancellationToken);

    internal Task SetRecordingDesiredStateAsync(bool enabled, CancellationToken cancellationToken = default)
        => _recordingTransitionController.SetRecordingDesiredStateAsync(enabled, cancellationToken);

    /// <summary>
    /// Graceful-stop entry point for callers that must NOT short-circuit on the
    /// toggle CAS gate (e.g. the window-close handler). If a toggle is in flight,
    /// await it; afterwards, if still recording, initiate a fresh stop.
    /// </summary>
    public Task StopRecordingAndWaitAsync(CancellationToken cancellationToken = default)
        => _recordingTransitionController.StopRecordingAndWaitAsync(cancellationToken);

    internal void MarkRecordingFinalizationUnresolved(string statusMessage)
        => _captureService.MarkRecordingFinalizationUnresolved(statusMessage);

    internal Task StopRecordingForEmergencyAsync(CancellationToken cancellationToken = default)
        => _sessionCoordinator.StopRecordingForEmergencyAsync(cancellationToken);

    public event EventHandler? PreviewStartRequested;
    public event EventHandler? PreviewStopRequested;
    public event Func<string, Task>? PreviewReinitRequested;
    public event Func<Task>? PreviewRendererStopRequested;

    public Task SetSettingsVisibleAsync(bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            IsSettingsVisible = visible;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetStatsVisibleAsync(bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            IsStatsVisible = visible;
            SaveSettingsOrThrow();
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetStatsSectionVisibleAsync(string section, bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            StatsSectionVisibilityHandler?.Invoke(section, visible);
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetFrameTimeOverlayVisibleAsync(bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            FrameTimeOverlayVisibilityHandler?.Invoke(visible);
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetFlashbackTimelineVisibleAsync(bool visible, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            IsFlashbackTimelineVisible = visible;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public CaptureRuntimeSnapshot GetCaptureRuntimeSnapshot() => _captureService.GetRuntimeSnapshot();
    public CaptureHealthSnapshot GetCaptureHealthSnapshot() => _captureService.GetHealthSnapshot();
    public CaptureDiagnosticsSnapshot GetCaptureDiagnosticsSnapshot() => _captureService.GetDiagnosticsSnapshot();
    public RecordingStats GetRecordingStatsSnapshot() => _captureService.GetRecordingStats();
    internal ParallelMjpegDecodePipeline.PipelineTimingMetrics? GetMjpegPipelineTimingDetails()
        => _captureService.GetMjpegPipelineTimingDetails();
    public VideoSourceProbeResult ProbeVideoSource() => _captureService.ProbeVideoSource();
    public PreviewColorProbeResult ProbePreviewColor() => _captureService.ProbePreviewColor();
    public Task<long> GetCaptureSnapshotProducerEpochAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(() => _captureService.SessionGeneration, cancellationToken);
    public Task<CaptureRuntimeSnapshot> GetCaptureRuntimeSnapshotAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(_captureService.GetRuntimeSnapshot, cancellationToken);
    public Task<CaptureHealthSnapshot> GetCaptureHealthSnapshotAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(_captureService.GetHealthSnapshot, cancellationToken);
    public Task<RecordingStats> GetRecordingStatsSnapshotAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(_captureService.GetRecordingStats, cancellationToken);
    public Task<VideoSourceProbeResult> ProbeVideoSourceAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(ProbeVideoSource, cancellationToken);
    public Task<PreviewColorProbeResult> ProbePreviewColorAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(ProbePreviewColor, cancellationToken);
    public Task<PreviewFrameCaptureResult> CapturePreviewFrameAsync(string outputPath, CancellationToken cancellationToken = default)
        => _captureService.CapturePreviewFrameAsync(outputPath, cancellationToken);

    public Task<ViewModelRuntimeSnapshot> GetViewModelRuntimeSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var sessionSnapshot = _sessionCoordinator.Snapshot;
        return InvokeOnUiThreadAsync(() =>
        {
            var input = new ViewModelRuntimeSnapshotInput
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                SessionSnapshot = sessionSnapshot,
                IsInitialized = IsInitialized,
                IsPreviewing = IsPreviewing,
                IsRecording = IsRecording,
                IsAudioEnabled = IsAudioEnabled,
                IsAudioPreviewEnabled = IsAudioPreviewEnabled,
                IsCustomAudioInputEnabled = IsCustomAudioInputEnabled,
                StatusText = StatusText,
                SelectedDeviceId = SelectedDevice?.Id,
                SelectedDeviceName = SelectedDevice?.Name,
                SelectedAudioInputDeviceId = SelectedAudioInputDevice?.Id,
                SelectedAudioInputDeviceName = SelectedAudioInputDevice?.Name,
                SelectedResolution = SelectedResolution,
                SelectedFrameRate = SelectedFrameRate,
                SelectedFriendlyFrameRate = SelectedFriendlyFrameRate,
                SelectedExactFrameRate = SelectedExactFrameRate,
                SelectedExactFrameRateArg = SelectedExactFrameRateArg,
                DisabledResolutionReason = DisabledResolutionReason,
                DisabledFrameRateReason = DisabledFrameRateReason,
                HdrResolutionSupportHint = HdrResolutionSupportHint,
                DetectedSourceFrameRate = DetectedSourceFrameRate,
                DetectedSourceFrameRateArg = DetectedSourceFrameRateArg,
                SourceFrameRateOrigin = SourceFrameRateOrigin,
                SourceWidth = SourceWidth,
                SourceHeight = SourceHeight,
                SourceIsHdr = SourceIsHdr,
                SourceTelemetryAvailability = SourceTelemetryAvailability,
                SourceTelemetryOriginDetail = SourceTelemetryOriginDetail,
                SourceTelemetryConfidence = SourceTelemetryConfidence,
                SourceTelemetryDiagnosticSummary = SourceTelemetryDiagnosticSummary,
                SourceTelemetryTimestampUtc = SourceTelemetryTimestampUtc,
                SourceTelemetryEpoch = _latestSourceTelemetry.TelemetryEpoch,
                SourceTelemetrySummaryText = SourceTelemetrySummaryText,
                SourceTargetSummaryText = SourceTargetSummaryText,
                SelectedRecordingFormat = SelectedRecordingFormat,
                SelectedQuality = SelectedQuality,
                SelectedPreset = SelectedPreset,
                SelectedSplitEncodeMode = SelectedSplitEncodeMode,
                SelectedVideoFormat = SelectedVideoFormat,
                CustomBitrateMbps = CustomBitrateMbps,
                PreviewVolume = PreviewVolume,
                IsStatsVisible = IsStatsVisible,
                IsHdrAvailable = IsHdrAvailable,
                IsHdrEnabled = IsHdrEnabled,
                HdrRuntimeState = HdrRuntimeState,
                HdrReadinessReason = HdrReadinessReason,
                LiveResolution = LiveResolution,
                LiveFrameRate = LiveFrameRate,
                LivePixelFormat = LivePixelFormat,
                OutputPath = OutputPath,
                RecordingTime = RecordingTime,
                RecordingSizeInfo = RecordingSizeInfo,
                RecordingBitrateInfo = RecordingBitrateInfo,
                AudioPeak = AudioPeak,
                AudioClipping = AudioClipping
            };

            return ViewModelRuntimeSnapshotBuilder.Build(input);
        }, cancellationToken);
    }

    public Task<AutomationOptionsSnapshot> GetAutomationOptionsSnapshotAsync(CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var selectedFrameRate = SelectedFrameRate;
            var input = new AutomationOptionsSnapshotInput
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Devices = Devices
                    .Select(device => new AutomationOptionsDeviceInput
                    {
                        Id = device.Id,
                        Name = device.Name
                    })
                    .ToArray(),
                AudioInputDevices = AudioInputDevices
                    .Select(device => new AutomationOptionsDeviceInput
                    {
                        Id = device.Id,
                        Name = device.Name
                    })
                    .ToArray(),
                MicrophoneDevices = MicrophoneDevices
                    .Select(device => new AutomationOptionsDeviceInput
                    {
                        Id = device.Id,
                        Name = device.Name
                    })
                    .ToArray(),
                Resolutions = AvailableResolutions
                    .Select(option => new AutomationOptionsResolutionInput
                    {
                        Value = option.Value,
                        Width = option.Width,
                        Height = option.Height,
                        IsEnabled = option.IsEnabled,
                        DisableReason = option.DisableReason
                    })
                    .ToArray(),
                FrameRates = AvailableFrameRates
                    .Select(option => new AutomationOptionsFrameRateInput
                    {
                        Value = option.Value,
                        FriendlyValue = option.FriendlyValue,
                        ExactValueArg = option.Rational,
                        IsEnabled = option.IsEnabled,
                        DisableReason = option.DisableReason,
                        IsSelected = FrameRateTimingPolicy.IsFrameRateMatch(option.Value, selectedFrameRate)
                    })
                    .ToArray(),
                RecordingFormats = AvailableRecordingFormats.ToArray(),
                Qualities = AvailableQualities.ToArray(),
                Presets = AvailablePresets.ToArray(),
                SplitEncodeModes = AvailableSplitEncodeModes.ToArray(),
                VideoFormats = AvailableVideoFormats.ToArray(),
                FlashbackBufferMinuteOptions = SupportedFlashbackBufferMinutes,
                SelectedDeviceId = SelectedDevice?.Id,
                SelectedAudioInputDeviceId = SelectedAudioInputDevice?.Id,
                SelectedMicrophoneDeviceId = SelectedMicrophoneDevice?.Id,
                SelectedResolution = SelectedResolution,
                SelectedFrameRate = selectedFrameRate,
                SelectedRecordingFormat = SelectedRecordingFormat,
                SelectedQuality = SelectedQuality,
                SelectedPreset = SelectedPreset,
                SelectedSplitEncodeMode = SelectedSplitEncodeMode,
                SelectedVideoFormat = SelectedVideoFormat,
                MjpegDecoderCount = MjpegDecoderCount,
                PreviewVolume = PreviewVolume,
                IsMicrophoneEnabled = IsMicrophoneEnabled,
                MicrophoneVolume = MicrophoneVolume,
                FlashbackBufferMinutes = FlashbackBufferMinutes,
                FlashbackGpuDecode = FlashbackGpuDecode,
                IsFlashbackEnabled = IsFlashbackEnabled,
                IsStatsVisible = IsStatsVisible
            };

            return AutomationOptionsSnapshotBuilder.Build(input);
        }, cancellationToken);
    }

    private static Task<T> FromSynchronousSnapshot<T>(Func<T> snapshotFactory, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<T>(cancellationToken);
        }

        return Task.FromResult(snapshotFactory());
    }

    public CaptureSettings BuildCurrentSettings() => BuildCaptureSettings();

    public Task RefreshDevicesForAutomationAsync(CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(() => RefreshDevicesAsync(cancellationToken), cancellationToken);

    public Task SelectDeviceAsync(string? deviceId, string? deviceName, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(async () =>
        {
            var target = ResolveDevice(deviceId, deviceName);
            if (target == null)
            {
                throw new InvalidOperationException($"Capture device not found. Id='{deviceId ?? "(null)"}', Name='{deviceName ?? "(null)"}'.");
            }

            var applied = await ApplySelectedDeviceWithResultAsync(target, cancellationToken).ConfigureAwait(true);
            if (!applied)
            {
                throw new InvalidOperationException("Capture device selection did not initialize; rollback was skipped if a newer selection superseded this request.");
            }

            SaveSettingsOrThrow();
        }, cancellationToken);
    }

    public Task SelectAudioInputDeviceAsync(string? deviceId, string? deviceName, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var target = ResolveAudioDevice(deviceId, deviceName);
            if (target == null)
            {
                throw new InvalidOperationException($"Audio input device not found. Id='{deviceId ?? "(null)"}', Name='{deviceName ?? "(null)"}'.");
            }

            SelectedAudioInputDevice = target;
            SaveSettingsOrThrow();
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public async Task SelectMicrophoneDeviceAsync(string? deviceId, string? deviceName, CancellationToken cancellationToken = default)
    {
        var request = await InvokeOnUiThreadAsync(
            () =>
            {
                var target = ResolveMicrophoneDevice(deviceId, deviceName);
                if (target == null)
                {
                    throw new InvalidOperationException($"Microphone device not found. Id='{deviceId ?? "(null)"}', Name='{deviceName ?? "(null)"}'.");
                }

                return (
                    IsRecording,
                    IsMicrophoneEnabled,
                    CurrentDeviceId: SelectedMicrophoneDevice?.Id,
                    Target: target);
            },
            cancellationToken).ConfigureAwait(false);

        if (request.IsRecording &&
            !string.Equals(request.CurrentDeviceId, request.Target.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cannot change microphone device while recording. Stop the recording first.");
        }

        if (!request.IsRecording)
        {
            await _sessionCoordinator.UpdateMicrophoneMonitorAsync(
                request.IsMicrophoneEnabled,
                request.Target.Id,
                request.Target.Name,
                cancellationToken).ConfigureAwait(false);
        }

        await InvokeOnUiThreadAsync(
            () =>
            {
                _suppressMicrophoneMonitorUpdate = true;
                try
                {
                    SelectedMicrophoneDevice = request.Target;
                }
                finally
                {
                    _suppressMicrophoneMonitorUpdate = false;
                }

                SaveSettingsOrThrow();
                return Task.CompletedTask;
            },
            cancellationToken).ConfigureAwait(false);
    }

    public Task SetCustomAudioInputEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            if (IsRecording)
            {
                throw new InvalidOperationException("Custom audio input cannot be changed while recording.");
            }

            IsCustomAudioInputEnabled = enabled;
            SaveSettingsOrThrow();
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetAudioEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            IsAudioEnabled = enabled;
            SaveSettingsOrThrow();
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetAudioPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            IsAudioPreviewEnabled = enabled;
            SaveSettingsOrThrow();
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetPreviewVolumeAsync(double previewVolumePercent, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            PreviewVolume = Math.Clamp(previewVolumePercent / 100.0, 0.0, 1.0);
            SaveSettingsOrThrow();
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetDeviceAudioModeAsync(string mode, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(async () =>
        {
            var normalizedMode = NormalizeDeviceAudioMode(mode);
            WithAudioControlRefreshSuppressed(() => SelectedDeviceAudioMode = normalizedMode);
            var applied = await ApplyDeviceAudioModeAsync(
                "automation device audio mode",
                normalizedMode,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!applied)
            {
                throw new InvalidOperationException($"Device audio mode change failed ({normalizedMode}).");
            }

            SaveSettingsOrThrow();
        }, cancellationToken);
    }

    public Task SetAnalogAudioGainAsync(double gainPercent, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(async () =>
        {
            var clampedGain = Math.Clamp(gainPercent, 0.0, 100.0);
            WithAudioControlRefreshSuppressed(() => AnalogAudioGainPercent = clampedGain);
            var applied = await ApplyAnalogAudioGainAsync(
                "automation analog audio gain",
                clampedGain,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!applied)
            {
                throw new InvalidOperationException($"Analog audio gain change failed ({clampedGain:0}%).");
            }

            SaveSettingsOrThrow();
        }, cancellationToken);
    }

    public Task SetMicrophoneEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return SetMicrophoneEnabledAutomationAsync(enabled, cancellationToken);
    }

    public Task SetMicrophoneVolumeAsync(double microphoneVolumePercent, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            MicrophoneVolume = Math.Clamp(microphoneVolumePercent, 0.0, 100.0);
            SaveSettingsOrThrow();
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetResolutionAsync(string resolution, CancellationToken cancellationToken = default)
        => _captureSettingsAutomationController.SetResolutionAsync(resolution, cancellationToken);

    public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)
        => _captureSettingsAutomationController.SetFrameRateAsync(frameRate, cancellationToken);

    public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)
        => _captureSettingsAutomationController.SetVideoFormatAsync(videoFormat, cancellationToken);

    public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)
        => _captureSettingsAutomationController.SetMjpegDecoderCountAsync(decoderCount, cancellationToken);

    public Task SetRecordingFormatAsync(string format, CancellationToken cancellationToken = default)
        => RunPersistedSettingsAutomationAsync(
            _recordingSettingsAutomationController.SetRecordingFormatAsync(format, cancellationToken),
            cancellationToken);

    public Task SetQualityAsync(string quality, CancellationToken cancellationToken = default)
        => RunPersistedSettingsAutomationAsync(
            _recordingSettingsAutomationController.SetQualityAsync(quality, cancellationToken),
            cancellationToken);

    public Task SetSplitEncodeModeAsync(string splitEncodeMode, CancellationToken cancellationToken = default)
        => RunPersistedSettingsAutomationAsync(
            _recordingSettingsAutomationController.SetSplitEncodeModeAsync(splitEncodeMode, cancellationToken),
            cancellationToken);

    public Task SetCustomBitrateAsync(double bitrateMbps, CancellationToken cancellationToken = default)
        => RunPersistedSettingsAutomationAsync(
            _recordingSettingsAutomationController.SetCustomBitrateAsync(bitrateMbps, cancellationToken),
            cancellationToken);

    public Task SetPresetAsync(string preset, CancellationToken cancellationToken = default)
        => RunPersistedSettingsAutomationAsync(
            _recordingSettingsAutomationController.SetPresetAsync(preset, cancellationToken),
            cancellationToken);

    public Task SetOutputPathAsync(string outputPath, CancellationToken cancellationToken = default)
        => RunPersistedSettingsAutomationAsync(
            _recordingSettingsAutomationController.SetOutputPathAsync(outputPath, cancellationToken),
            cancellationToken);

    private async Task RunPersistedSettingsAutomationAsync(Task operation, CancellationToken cancellationToken)
    {
        await operation.ConfigureAwait(false);
        await InvokeOnUiThreadAsync(
            () =>
            {
                SaveSettingsOrThrow();
                return Task.CompletedTask;
            },
            cancellationToken).ConfigureAwait(false);
    }

    private CaptureDevice? ResolveDevice(string? deviceId, string? deviceName)
    {
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            var byId = Devices.FirstOrDefault(d => string.Equals(d.Id, deviceId, StringComparison.OrdinalIgnoreCase));
            if (byId != null)
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(deviceName))
        {
            return ResolveByName(Devices, deviceName, d => d.Name);
        }

        return null;
    }

    private AudioInputDevice? ResolveAudioDevice(string? deviceId, string? deviceName)
    {
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            var byId = AudioInputDevices.FirstOrDefault(d => string.Equals(d.Id, deviceId, StringComparison.OrdinalIgnoreCase));
            if (byId != null)
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(deviceName))
        {
            return ResolveByName(AudioInputDevices, deviceName, d => d.Name);
        }

        return null;
    }

    private AudioInputDevice? ResolveMicrophoneDevice(string? deviceId, string? deviceName)
    {
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            var byId = MicrophoneDevices.FirstOrDefault(d => string.Equals(d.Id, deviceId, StringComparison.OrdinalIgnoreCase));
            if (byId != null)
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(deviceName))
        {
            return ResolveByName(MicrophoneDevices, deviceName, d => d.Name);
        }

        return null;
    }

    private static T? ResolveByName<T>(
        IEnumerable<T> devices,
        string deviceName,
        Func<T, string?> getName)
        where T : class
    {
        var exact = devices.FirstOrDefault(d => string.Equals(getName(d), deviceName, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
        {
            return exact;
        }

        var partialMatches = devices
            .Where(d => (getName(d) ?? string.Empty).Contains(deviceName, StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToArray();
        return partialMatches.Length == 1 ? partialMatches[0] : null;
    }

    private async Task SetMicrophoneEnabledAutomationAsync(bool enabled, CancellationToken cancellationToken)
    {
        var request = await InvokeOnUiThreadAsync(
            () => (
                IsRecording,
                CurrentMicEnabled: IsMicrophoneEnabled,
                DeviceId: SelectedMicrophoneDevice?.Id,
                DeviceName: SelectedMicrophoneDevice?.Name),
            cancellationToken).ConfigureAwait(false);

        if (request.IsRecording)
        {
            if (enabled == request.CurrentMicEnabled)
            {
                // Idempotent reassertion during recording: automation clients often
                // re-issue desired state. The mic wiring is already where the caller
                // wants it, so succeed as a no-op rather than throwing.
                Logger.Log($"MIC_TOGGLE_NOOP reason=recording_active_idempotent requested={enabled}");
                return;
            }

            // Real state transition while recording: refuse. UpdateMicrophoneMonitorAsync
            // cannot rewire the device mid-recording, so setting IsMicrophoneEnabled
            // here would leave UI state lying about the actual device wiring.
            Logger.Log($"MIC_TOGGLE_REFUSED reason=recording_active requested={enabled} current={request.CurrentMicEnabled}");
            throw new InvalidOperationException(
                "Cannot change microphone enable state while recording. Stop the recording first.");
        }

        await _sessionCoordinator.UpdateMicrophoneMonitorAsync(
            enabled,
            request.DeviceId,
            request.DeviceName,
            cancellationToken).ConfigureAwait(false);

        await InvokeOnUiThreadAsync(
            () =>
            {
                _suppressMicrophoneMonitorUpdate = true;
                try
                {
                    IsMicrophoneEnabled = enabled;
                }
                finally
                {
                    _suppressMicrophoneMonitorUpdate = false;
                }

                SaveSettingsOrThrow();
                return true;
            },
            cancellationToken).ConfigureAwait(false);
    }

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Ready";

    [ObservableProperty]
    public partial string LiveResolution { get; set; } = LiveInfoUnavailable;

    [ObservableProperty]
    public partial string LiveFrameRate { get; set; } = LiveInfoUnavailable;

    [ObservableProperty]
    public partial string LivePixelFormat { get; set; } = LiveInfoUnavailable;

    [ObservableProperty]
    public partial string DiskSpaceInfo { get; set; } = "";

    [ObservableProperty]
    public partial ObservableCollection<CaptureDevice> Devices { get; set; } = new();

    [ObservableProperty]
    public partial CaptureDevice? SelectedDevice { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<MediaFormat> AvailableFormats { get; set; } = new();

    [ObservableProperty]
    public partial MediaFormat? SelectedFormat { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<ResolutionOption> AvailableResolutions { get; set; } = new();

    [ObservableProperty]
    public partial string? SelectedResolution { get; set; }

    [ObservableProperty]
    public partial uint? AutoResolvedWidth { get; set; }

    [ObservableProperty]
    public partial uint? AutoResolvedHeight { get; set; }

    [ObservableProperty]
    public partial double? AutoResolvedFrameRate { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<FrameRateOption> AvailableFrameRates { get; set; } = new();

    [ObservableProperty]
    public partial double SelectedFrameRate { get; set; } = 60;

    public bool IsAutoFrameRateSelected
    {
        get => _isAutoFrameRateSelected;
        private set => SetProperty(ref _isAutoFrameRateSelected, value);
    }

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableVideoFormats { get; set; } = new()
    {
        "Auto", "MJPG", "NV12", "P010"
    };

    [ObservableProperty]
    public partial string SelectedVideoFormat { get; set; } = "Auto";

    [ObservableProperty]
    public partial int MjpegDecoderCount { get; set; } = 6;

    [ObservableProperty]
    public partial double? SelectedFriendlyFrameRate { get; set; }

    [ObservableProperty]
    public partial double? SelectedExactFrameRate { get; set; }

    [ObservableProperty]
    public partial string? SelectedExactFrameRateArg { get; set; }

    [ObservableProperty]
    public partial string DisabledResolutionReason { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DisabledFrameRateReason { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsHdrEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsHdrAvailable { get; set; }

    [ObservableProperty]
    public partial bool IsTrueHdrPreviewEnabled { get; set; }

    [ObservableProperty]
    public partial string HdrResolutionSupportHint { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HdrRuntimeState { get; set; } = "Inactive";

    [ObservableProperty]
    public partial string HdrReadinessReason { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double? DetectedSourceFrameRate { get; set; }

    [ObservableProperty]
    public partial string? DetectedSourceFrameRateArg { get; set; }

    [ObservableProperty]
    public partial string SourceFrameRateOrigin { get; set; } = "Unknown";

    [ObservableProperty]
    public partial int? SourceWidth { get; set; }

    [ObservableProperty]
    public partial int? SourceHeight { get; set; }

    [ObservableProperty]
    public partial bool? SourceIsHdr { get; set; }

    [ObservableProperty]
    public partial string SourceTelemetryAvailability { get; set; } = "Unknown";

    [ObservableProperty]
    public partial string SourceTelemetryOriginDetail { get; set; } = "Unknown";

    [ObservableProperty]
    public partial string SourceTelemetryConfidence { get; set; } = "Unknown";

    [ObservableProperty]
    public partial string? SourceTelemetryDiagnosticSummary { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? SourceTelemetryTimestampUtc { get; set; }

    [ObservableProperty]
    public partial string SourceTelemetrySummaryText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SourceTargetSummaryText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsRecordingTransitioning { get; set; }

    [ObservableProperty]
    public partial bool IsFfmpegMissing { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableRecordingFormats { get; set; } =
        new() { DefaultRecordingFormat, HevcRecordingFormat, Av1RecordingFormat };

    [ObservableProperty]
    public partial string SelectedRecordingFormat { get; set; } = DefaultRecordingFormat;

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableQualities { get; set; } = new() { "Auto", "Low", "Medium", "High", "Super High", "Custom" };

    [ObservableProperty]
    public partial string SelectedQuality { get; set; } = "Medium";

    [ObservableProperty]
    public partial ObservableCollection<string> AvailablePresets { get; set; } = new()
    {
        "Auto", "P1", "P2", "P3", "P4", "P5", "P6", "P7"
    };

    [ObservableProperty]
    public partial string SelectedPreset { get; set; } = "Auto";

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableSplitEncodeModes { get; set; } = new()
    {
        "Auto", "Disabled", "2-way", "3-way"
    };

    [ObservableProperty]
    public partial string SelectedSplitEncodeMode { get; set; } = "Auto";

    [ObservableProperty]
    public partial double CustomBitrateMbps { get; set; } = 50;

    [ObservableProperty]
    public partial bool IsCustomBitrateVisible { get; set; }

    [ObservableProperty]
    public partial string OutputPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

    [ObservableProperty]
    public partial string RecordingTime { get; set; } = "00:00:00";

    [ObservableProperty]
    public partial string RecordingSizeInfo { get; set; } = "--";

    [ObservableProperty]
    public partial string RecordingBitrateInfo { get; set; } = "--";

    [ObservableProperty]
    public partial bool IsRecording { get; set; }

    private int _disposeState;

    // Capture presentation adapters that apply runtime/source state to ViewModel labels.
    private void UpdateLiveCaptureInfo(CaptureRuntimeSnapshot? runtimeSnapshot = null)
    {
        var runtime = runtimeSnapshot ?? _captureService.GetRuntimeSnapshot();
        IsAudioPreviewActive = runtime.IsAudioPreviewActive;

        var liveSignalText = LiveSignalTextPresentationBuilder.Build(
            runtime,
            _captureService.EncoderCodecName,
            LiveInfoUnavailable);
        LiveResolution = liveSignalText.Resolution;
        LiveFrameRate = liveSignalText.FrameRate;
        LivePixelFormat = liveSignalText.PixelFormat;
    }

    private void ResetLiveCaptureInfo()
    {
        IsAudioPreviewActive = false;
        LiveResolution = LiveInfoUnavailable;
        LiveFrameRate = LiveInfoUnavailable;
        LivePixelFormat = LiveInfoUnavailable;
    }

    partial void OnIsPreviewingChanged(bool value)
    {
        if (!value && !IsRecording)
        {
            ResetLiveCaptureInfo();
        }
    }

    private void UpdateHdrRuntimeStatusFromCapture(CaptureRuntimeSnapshot? runtimeSnapshot = null)
    {
        var runtime = runtimeSnapshot ?? _captureService.GetRuntimeSnapshot();
        HdrRuntimeState = runtime.HdrRuntimeState;
        HdrReadinessReason = runtime.HdrReadinessReason;
        UpdateTargetSummary();
    }

    private void UpdateTargetSummary()
    {
        SourceTargetSummaryText = SourceTelemetryPresentationBuilder.BuildTargetSummary(
            GetSelectedResolutionDisplayText(),
            SelectedFrameRate,
            SelectedFriendlyFrameRate,
            SelectedExactFrameRate,
            SelectedExactFrameRateArg,
            HdrRuntimeState);
    }

    private string GetSelectedResolutionDisplayText()
    {
        if (string.IsNullOrWhiteSpace(SelectedResolution))
        {
            return "?";
        }

        if (!IsAutoResolutionValue(SelectedResolution))
        {
            return SelectedResolution;
        }

        var friendlyRate = SelectedFriendlyFrameRate
            ?? (AutoResolvedFrameRate.HasValue
                ? Math.Round(AutoResolvedFrameRate.Value, MidpointRounding.AwayFromZero)
                : (double?)null);
        if (AutoResolvedWidth.HasValue &&
            AutoResolvedHeight.HasValue &&
            friendlyRate.HasValue)
        {
            return $"{AutoResolutionValue} ({GetResolutionKey(AutoResolvedWidth.Value, AutoResolvedHeight.Value)} @ {friendlyRate.Value:0} fps)";
        }

        return AutoResolutionValue;
    }

    private void ResetFrameRateSelectionState()
    {
        _hasUserOverriddenFrameRateForCurrentMode = false;
        IsAutoFrameRateSelected = true;
    }

    private void ApplyResolvedFrameRateSelection(FrameRateOption? selected, double fallbackRate)
    {
        _isApplyingAutomaticFrameRateSelection = true;
        try
        {
            SelectedFrameRate = selected?.Value ?? fallbackRate;
        }
        finally
        {
            _isApplyingAutomaticFrameRateSelection = false;
        }

        SelectedFriendlyFrameRate = selected?.FriendlyValue ?? Math.Round(SelectedFrameRate);
        SelectedExactFrameRate = selected?.Value ?? SelectedFrameRate;
        SelectedExactFrameRateArg = selected?.Rational;
        if (IsAutoResolutionValue(SelectedResolution))
        {
            AutoResolvedFrameRate = selected?.Value ?? SelectedFrameRate;
        }

        DisabledFrameRateReason = selected is { IsEnabled: false }
            ? selected.DisableReason
            : string.Empty;
    }

    private void ResetModeSelectionState()
    {
        ResetFrameRateSelectionState();
        _hasUserOverriddenResolutionForCurrentMode = false;
        _forceSourceAutoRetarget = false;
        _lastSourceModeKey = null;
        _pendingSdrAutoSelectionForDeviceChange = false;
        _pendingSdrAutoFriendlyFrameRateBucket = null;
    }

    private CaptureSettings BuildCaptureSettings()
    {
        var effectiveResolutionKnown = TryGetEffectiveResolutionSelection(out _, out var effectiveWidth, out var effectiveHeight);
        var runtime = _captureService.GetRuntimeSnapshot();
        var sourceTelemetry = _captureService.GetLatestSourceTelemetrySnapshot();
        return CaptureSettingsProjectionBuilder.Build(new CaptureSettingsProjectionInput
        {
            EffectiveResolutionKnown = effectiveResolutionKnown,
            EffectiveWidth = effectiveWidth,
            EffectiveHeight = effectiveHeight,
            SelectedResolution = SelectedResolution,
            SelectedFrameRate = SelectedFrameRate,
            AutoResolvedFrameRate = AutoResolvedFrameRate,
            IsAutoResolutionSelected = IsAutoResolutionValue(SelectedResolution),
            SelectedFormat = SelectedFormat,
            AvailableFrameRates = AvailableFrameRates.ToArray(),
            Runtime = runtime,
            SourceTelemetry = sourceTelemetry,
            SelectedVideoFormat = SelectedVideoFormat,
            IsHdrEnabled = IsHdrEnabled,
            IsTrueHdrPreviewEnabled = IsTrueHdrPreviewEnabled,
            MjpegDecoderCount = MjpegDecoderCount,
            SelectedRecordingFormat = SelectedRecordingFormat,
            SelectedQuality = SelectedQuality,
            SelectedPreset = SelectedPreset,
            SelectedSplitEncodeMode = SelectedSplitEncodeMode,
            CustomBitrateMbps = CustomBitrateMbps,
            OutputPath = OutputPath,
            FlashbackGpuDecode = FlashbackGpuDecode,
            FlashbackBufferMinutes = FlashbackBufferMinutes,
            IsAudioEnabled = IsAudioEnabled,
            IsCustomAudioInputEnabled = IsCustomAudioInputEnabled,
            SelectedAudioInputDeviceId = SelectedAudioInputDevice?.Id,
            SelectedAudioInputDeviceName = SelectedAudioInputDevice?.Name,
            IsMicrophoneEnabled = IsMicrophoneEnabled,
            SelectedMicrophoneDeviceId = SelectedMicrophoneDevice?.Id,
            SelectedMicrophoneDeviceName = SelectedMicrophoneDevice?.Name
        });
    }

    private void StartRecordingCapabilityRefresh()
        => _recordingCapabilityController.Start();

    private void RebuildRecordingFormatOptions()
        => _recordingCapabilityController.RebuildRecordingFormatOptions();

    partial void OnIsRecordingChanged(bool value)
    {
        if (!value)
        {
            ResetAudioMeter();
            RecordingSizeInfo = "--";
            RecordingBitrateInfo = "--";
            _recordingBitrateSamples.Clear();

            if (_pendingModeOptionsRefresh)
            {
                _pendingModeOptionsRefresh = false;
                RebuildResolutionOptions();
            }
        }
    }

    private void UpdateRecordingStats()
    {
        var stats = _captureService.GetRecordingStats();
        var totalBytes = stats.TotalBytes;
        RecordingSizeInfo = DisplayFormatters.FormatBytes(totalBytes, "0");

        var now = Environment.TickCount64;
        var smoothed = _recordingBitrateSamples.AddSampleAndCompute(now, totalBytes);
        RecordingBitrateInfo = smoothed.HasValue ? DisplayFormatters.FormatBitrate(smoothed.Value) : "--";
    }

    private void UpdateDiskSpace()
    {
        DiskSpaceInfo = OutputDriveSpacePresentationBuilder.Build(OutputPath);
    }

    private bool EnqueueUiOperation(Func<Task> operation, string operationName, bool allowDuringDispose = false)
        => _uiDispatchController.Enqueue(operation, operationName, allowDuringDispose);

    private Task ExecuteUiOperationAsync(Func<Task> operation, string operationName)
        => _uiDispatchController.ExecuteAsync(operation, operationName);

    private async Task NotifyPreviewReinitRequestedAsync(string reason)
    {
        var handlers = PreviewReinitRequested;
        if (handlers == null)
        {
            return;
        }

        foreach (Func<string, Task> handler in handlers.GetInvocationList())
        {
            await handler(reason);
        }
    }

    private async Task NotifyRendererStopAsync()
    {
        var handlers = PreviewRendererStopRequested;
        if (handlers == null)
        {
            return;
        }

        foreach (Func<Task> handler in handlers.GetInvocationList())
        {
            await handler();
        }
    }

    private Task InvokeOnUiThreadAsync(Func<Task> operation, CancellationToken cancellationToken = default)
        => _uiDispatchController.InvokeAsync(operation, cancellationToken);

    private Task<T> InvokeOnUiThreadAsync<T>(Func<T> operation, CancellationToken cancellationToken = default)
        => _uiDispatchController.InvokeAsync(operation, cancellationToken);

    private static async Task AwaitWithTimeoutAsync(Task task, int timeoutMs, string operationName)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeoutMs)).ConfigureAwait(false);
        if (completed != task)
        {
            throw new TimeoutException($"{operationName} timed out after {timeoutMs} ms.");
        }

        await task.ConfigureAwait(false);
    }

    private void CancelActiveFlashbackExportForDispose()
    {
        Interlocked.Increment(ref _flashbackExportOperationId);
        var exportCts = Interlocked.Exchange(ref _exportCts, null);
        CancelFlashbackExportCts(exportCts);
        if (exportCts != null)
        {
            DisposeFlashbackExportCtsBestEffort(exportCts, "viewmodel_dispose");
        }
    }

    // REVIEWED 2026-04-07: IDisposable fallback only. MainWindow.Closed calls
    // await ViewModel.DisposeAsync(); this sync path is for GC finalizer safety.
    public void Dispose()
        => _disposalController.Dispose();

    public async ValueTask DisposeAsync()
        => await _disposalController.DisposeAsync().ConfigureAwait(false);

    private sealed class MainViewModelControllerGraph
    {
        private MainViewModelControllerGraph(
            MainViewModelUiDispatchController uiDispatchController,
            MainViewModelRecordingTransitionController recordingTransitionController,
            MainViewModelPreviewLifecycleController previewLifecycleController,
            MainViewModelDeviceAudioRequestController deviceAudioRequestController,
            MainViewModelRecordingCapabilityController recordingCapabilityController,
            MainViewModelCaptureSettingsAutomationController captureSettingsAutomationController,
            MainViewModelRecordingSettingsAutomationController recordingSettingsAutomationController,
            MainViewModelCaptureModeOptionRebuildController captureModeOptionRebuildController,
            MainViewModelDeviceFormatProbeController deviceFormatProbeController,
            MainViewModelSourceTelemetryController sourceTelemetryController,
            MainViewModelDeviceRefreshController deviceRefreshController,
            MainViewModelRuntimeLifecycleController runtimeLifecycleController,
            MainViewModelDisposalController disposalController)
        {
            UiDispatchController = uiDispatchController;
            RecordingTransitionController = recordingTransitionController;
            PreviewLifecycleController = previewLifecycleController;
            DeviceAudioRequestController = deviceAudioRequestController;
            RecordingCapabilityController = recordingCapabilityController;
            CaptureSettingsAutomationController = captureSettingsAutomationController;
            RecordingSettingsAutomationController = recordingSettingsAutomationController;
            CaptureModeOptionRebuildController = captureModeOptionRebuildController;
            DeviceFormatProbeController = deviceFormatProbeController;
            SourceTelemetryController = sourceTelemetryController;
            DeviceRefreshController = deviceRefreshController;
            RuntimeLifecycleController = runtimeLifecycleController;
            DisposalController = disposalController;
        }

        public MainViewModelUiDispatchController UiDispatchController { get; }
        public MainViewModelRecordingTransitionController RecordingTransitionController { get; }
        public MainViewModelPreviewLifecycleController PreviewLifecycleController { get; }
        public MainViewModelDeviceAudioRequestController DeviceAudioRequestController { get; }
        public MainViewModelRecordingCapabilityController RecordingCapabilityController { get; }
        public MainViewModelCaptureSettingsAutomationController CaptureSettingsAutomationController { get; }
        public MainViewModelRecordingSettingsAutomationController RecordingSettingsAutomationController { get; }
        public MainViewModelCaptureModeOptionRebuildController CaptureModeOptionRebuildController { get; }
        public MainViewModelDeviceFormatProbeController DeviceFormatProbeController { get; }
        public MainViewModelSourceTelemetryController SourceTelemetryController { get; }
        public MainViewModelDeviceRefreshController DeviceRefreshController { get; }
        public MainViewModelRuntimeLifecycleController RuntimeLifecycleController { get; }
        public MainViewModelDisposalController DisposalController { get; }

        public static MainViewModelControllerGraph Create(MainViewModel viewModel)
        {
            ArgumentNullException.ThrowIfNull(viewModel);

            var uiDispatchController = CreateUiDispatchController(viewModel);
            var previewLifecycleController = CreatePreviewLifecycleController(viewModel);
            var recordingTransitionController = CreateRecordingTransitionController(viewModel, previewLifecycleController);
            var deviceAudioRequestController = CreateDeviceAudioRequestController(viewModel);
            var recordingCapabilityController = CreateRecordingCapabilityController(viewModel);
            var captureSettingsAutomationController = CreateCaptureSettingsAutomationController(viewModel);
            var recordingSettingsAutomationController = CreateRecordingSettingsAutomationController(viewModel);
            var captureModeOptionRebuildController = CreateCaptureModeOptionRebuildController(viewModel);
            var deviceFormatProbeController = CreateDeviceFormatProbeController(viewModel);
            var sourceTelemetryController = CreateSourceTelemetryController(viewModel);
            var deviceRefreshController = CreateDeviceRefreshController(viewModel, previewLifecycleController);
            var runtimeLifecycleController = CreateRuntimeLifecycleController(
                viewModel,
                previewLifecycleController,
                deviceFormatProbeController,
                sourceTelemetryController);
            var disposalController = CreateDisposalController(viewModel, deviceAudioRequestController, runtimeLifecycleController);

            return new MainViewModelControllerGraph(
                uiDispatchController,
                recordingTransitionController,
                previewLifecycleController,
                deviceAudioRequestController,
                recordingCapabilityController,
                captureSettingsAutomationController,
                recordingSettingsAutomationController,
                captureModeOptionRebuildController,
                deviceFormatProbeController,
                sourceTelemetryController,
                deviceRefreshController,
                runtimeLifecycleController,
                disposalController);
        }

        private static MainViewModelUiDispatchController CreateUiDispatchController(MainViewModel viewModel)
        {
            return new MainViewModelUiDispatchController(
                new MainViewModelUiDispatchControllerContext
                {
                    DispatcherQueue = viewModel._dispatcherQueue,
                    IsDisposing = () => Volatile.Read(ref viewModel._disposeState) != 0,
                    Log = message => Logger.Log(message),
                    LogException = exception => Logger.LogException(exception),
                    SetStatusText = value => viewModel.StatusText = value,
                });
        }

        private static MainViewModelDeviceAudioRequestController CreateDeviceAudioRequestController(MainViewModel viewModel)
        {
            return new MainViewModelDeviceAudioRequestController(
                new MainViewModelDeviceAudioRequestControllerContext
                {
                    EnqueueUiOperation = (operation, operationName, allowDuringDispose) =>
                        viewModel.EnqueueUiOperation(operation, operationName, allowDuringDispose),
                    IsDisposing = () => Volatile.Read(ref viewModel._disposeState) != 0,
                    IsLoadingSettings = () => viewModel._isLoadingSettings,
                    IsRefreshingDeviceAudioControls = () => viewModel._isRefreshingDeviceAudioControls,
                    IsDeviceAudioControlSupported = () => viewModel.IsDeviceAudioControlSupported,
                    IsRecording = () => viewModel.IsRecording,
                    GetSelectedDeviceAudioMode = () => viewModel.SelectedDeviceAudioMode,
                    GetSelectedDevice = () => viewModel.SelectedDevice,
                    SaveSettings = () => { _ = viewModel.SaveSettings(); },
                    RefreshDeviceAudioControlsAsync = viewModel.RefreshDeviceAudioControlsAsync,
                    ApplyDeviceAudioModeAsync = (reason, targetDevice, cancellationToken) =>
                        viewModel.ApplyDeviceAudioModeAsync(reason, targetDevice: targetDevice, cancellationToken: cancellationToken),
                    ApplyAnalogAudioGainAsync = (reason, targetDevice, cancellationToken) =>
                        viewModel.ApplyAnalogAudioGainAsync(reason, targetDevice: targetDevice, cancellationToken: cancellationToken),
                    IsCurrentSelectedDevice = viewModel.IsCurrentSelectedDevice,
                });
        }

        private static MainViewModelDeviceRefreshController CreateDeviceRefreshController(
            MainViewModel viewModel,
            MainViewModelPreviewLifecycleController previewLifecycleController)
        {
            return new MainViewModelDeviceRefreshController(
                new MainViewModelDeviceRefreshControllerContext
                {
                    SetStatusText = value => viewModel.StatusText = value,
                    IncrementDeviceScanGeneration = () => Interlocked.Increment(ref viewModel._deviceScanGeneration),
                    GetSelectedAudioInputDeviceId = () => viewModel.SelectedAudioInputDevice?.Id,
                    GetSelectedMicrophoneDeviceId = () => viewModel.SelectedMicrophoneDevice?.Id,
                    GetSelectedDeviceId = () => viewModel.SelectedDevice?.Id,
                    EnumerateCaptureDeviceDiscoveryAsync = () =>
                        viewModel._deviceService.EnumerateCaptureDeviceDiscoveryAsync(waitForFormatProbes: false),
                    ApplyStartupAudioDeviceScan = viewModel.ApplyStartupAudioDeviceScan,
                    ReplaceDevices = devices => ReplaceCollection(viewModel.Devices, devices),
                    GetDevices = () => viewModel.Devices,
                    BeginBackgroundFormatProbe = (device, scanGeneration) =>
                        viewModel._deviceService.BeginBackgroundFormatProbe(device, scanGeneration),
                    GetLastDiscoverySummary = () => viewModel._deviceService.LastDiscoverySummary,
                    SetSelectedDevice = device => viewModel.SelectedDevice = device,
                    GetSelectedDevice = () => viewModel.SelectedDevice,
                    GetPendingSavedDeviceId = () => viewModel._pendingSavedDeviceId,
                    SetPendingSavedDeviceId = value => viewModel._pendingSavedDeviceId = value,
                },
                previewLifecycleController);
        }

        private static MainViewModelCaptureSettingsAutomationController CreateCaptureSettingsAutomationController(MainViewModel viewModel)
        {
            return new MainViewModelCaptureSettingsAutomationController(
                new MainViewModelCaptureSettingsAutomationControllerContext
                {
                    InvokeBooleanOnUiThreadAsync = (operation, cancellationToken) =>
                        viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    InvokeOnUiThreadAsync = (operation, cancellationToken) =>
                        viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    GetAvailableResolutions = () => viewModel.AvailableResolutions,
                    GetAvailableFrameRates = () => viewModel.AvailableFrameRates,
                    GetAvailableVideoFormats = () => viewModel.AvailableVideoFormats,
                    GetSelectedResolution = () => viewModel.SelectedResolution,
                    SetSelectedResolution = value => viewModel.SelectedResolution = value,
                    SetSelectedFrameRate = value => viewModel.SelectedFrameRate = value,
                    SetSelectedVideoFormat = value => viewModel.SelectedVideoFormat = value,
                    SetMjpegDecoderCount = value => viewModel.MjpegDecoderCount = value,
                    SelectAutoFrameRate = viewModel.SelectAutoFrameRate,
                    IsPreviewing = () => viewModel.IsPreviewing,
                    IsInitialized = () => viewModel.IsInitialized,
                    GetSelectedDevice = () => viewModel.SelectedDevice,
                    GetSelectedFormat = () => viewModel.SelectedFormat,
                    CaptureSelectionSnapshot = viewModel.CaptureSelectionSnapshot,
                    RestoreCaptureSelectionSnapshotIfUnchanged = viewModel.RestoreCaptureSelectionSnapshotIfUnchanged,
                    SetSuppressFormatChangeReinitialize = value => viewModel._suppressFormatChangeReinitialize = value,
                    ReinitializeDeviceWithResultAsync = viewModel.ReinitializeDeviceWithResultAsync,
                });
        }

        private static MainViewModelSourceTelemetryController CreateSourceTelemetryController(MainViewModel viewModel)
        {
            return new MainViewModelSourceTelemetryController(
                new MainViewModelSourceTelemetryControllerContext
                {
                    TryEnqueueOnUiThread = operation => viewModel._dispatcherQueue.TryEnqueue(() => operation()),
                    GetLatestSourceTelemetry = () => viewModel._latestSourceTelemetry,
                    SetLatestSourceTelemetry = snapshot => viewModel._latestSourceTelemetry = snapshot,
                    BuildSourceTelemetrySummary = SourceTelemetryPresentationBuilder.BuildSourceSummary,
                    SetSourceWidth = value => viewModel.SourceWidth = value,
                    SetSourceHeight = value => viewModel.SourceHeight = value,
                    SetSourceIsHdr = value => viewModel.SourceIsHdr = value,
                    IsRecording = () => viewModel.IsRecording,
                    IsHdrEnabled = () => viewModel.IsHdrEnabled,
                    SetIsHdrEnabled = value => viewModel.IsHdrEnabled = value,
                    SetSourceTelemetryAvailability = value => viewModel.SourceTelemetryAvailability = value,
                    SetSourceTelemetryOriginDetail = value => viewModel.SourceTelemetryOriginDetail = value,
                    SetSourceTelemetryConfidence = value => viewModel.SourceTelemetryConfidence = value,
                    SetSourceTelemetryDiagnosticSummary = value => viewModel.SourceTelemetryDiagnosticSummary = value,
                    GetSourceTelemetryTimestampUtc = () => viewModel.SourceTelemetryTimestampUtc,
                    SetSourceTelemetryTimestampUtc = value => viewModel.SourceTelemetryTimestampUtc = value,
                    SetDetectedSourceFrameRate = value => viewModel.DetectedSourceFrameRate = value,
                    SetDetectedSourceFrameRateArg = value => viewModel.DetectedSourceFrameRateArg = value,
                    SetSourceFrameRateOrigin = value => viewModel.SourceFrameRateOrigin = value,
                    GetSourceTelemetrySummaryText = () => viewModel.SourceTelemetrySummaryText,
                    SetSourceTelemetrySummaryText = value => viewModel.SourceTelemetrySummaryText = value,
                    GetLastSourceModeKey = () => viewModel._lastSourceModeKey,
                    SetLastSourceModeKey = value => viewModel._lastSourceModeKey = value,
                    GetSelectedResolution = () => viewModel.SelectedResolution,
                    IsAutoResolutionValue = MainViewModel.IsAutoResolutionValue,
                    HasUserOverriddenResolutionForCurrentMode = () => viewModel._hasUserOverriddenResolutionForCurrentMode,
                    SetHasUserOverriddenResolutionForCurrentMode = value => viewModel._hasUserOverriddenResolutionForCurrentMode = value,
                    IsAutoFrameRateSelected = () => viewModel.IsAutoFrameRateSelected,
                    HasUserOverriddenFrameRateForCurrentMode = () => viewModel._hasUserOverriddenFrameRateForCurrentMode,
                    SetHasUserOverriddenFrameRateForCurrentMode = value => viewModel._hasUserOverriddenFrameRateForCurrentMode = value,
                    ForceSourceAutoRetarget = () => viewModel._forceSourceAutoRetarget,
                    SetForceSourceAutoRetarget = value => viewModel._forceSourceAutoRetarget = value,
                    AvailableResolutionCount = () => viewModel.AvailableResolutions.Count,
                    SetPendingModeOptionsRefresh = value => viewModel._pendingModeOptionsRefresh = value,
                    RebuildResolutionOptions = viewModel.RebuildResolutionOptions,
                    UpdateTargetSummary = viewModel.UpdateTargetSummary,
                });
        }

        private static MainViewModelRecordingTransitionController CreateRecordingTransitionController(
            MainViewModel viewModel,
            MainViewModelPreviewLifecycleController previewLifecycleController)
        {
            return new MainViewModelRecordingTransitionController(
                new MainViewModelRecordingTransitionControllerContext
                {
                    InvokeOnUiThreadAsync = (operation, cancellationToken) => viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    IsRecording = () => viewModel.IsRecording,
                    SetIsRecording = value => viewModel.IsRecording = value,
                    IsInitialized = () => viewModel.IsInitialized,
                    HasSelectedDevice = () => viewModel.SelectedDevice != null,
                    GetStatusText = () => viewModel.StatusText,
                    SetStatusText = value => viewModel.StatusText = value,
                    SetIsRecordingTransitioning = value => viewModel.IsRecordingTransitioning = value,
                    BuildCaptureSettings = viewModel.BuildCaptureSettings,
                    StartRecordingAsync = (settings, cancellationToken) =>
                        viewModel._sessionCoordinator.StartRecordingAsync(settings, cancellationToken),
                    StopRecordingAsync = cancellationToken =>
                        viewModel._sessionCoordinator.StopRecordingAsync(cancellationToken),
                    GetSessionIsRecording = () => viewModel._sessionCoordinator.Snapshot.IsRecording,
                    RestartRecordingStopwatch = viewModel._recordingStopwatch.Restart,
                    StopRecordingStopwatch = viewModel._recordingStopwatch.Stop,
                    ClearRecordingBitrateSamples = viewModel._recordingBitrateSamples.Clear,
                    SetRecordingSizeInfo = value => viewModel.RecordingSizeInfo = value,
                    SetRecordingBitrateInfo = value => viewModel.RecordingBitrateInfo = value,
                    GetRecordingTime = () => viewModel.RecordingTime,
                },
                previewLifecycleController);
        }

        private static MainViewModelRecordingCapabilityController CreateRecordingCapabilityController(MainViewModel viewModel)
        {
            return new MainViewModelRecordingCapabilityController(
                new MainViewModelRecordingCapabilityControllerContext
                {
                    DefaultRecordingFormat = DefaultRecordingFormat,
                    HevcRecordingFormat = HevcRecordingFormat,
                    Av1RecordingFormat = Av1RecordingFormat,
                    GetAvailableRecordingFormats = () => viewModel.AvailableRecordingFormats,
                    ReplaceAvailableRecordingFormats = formats =>
                    {
                        viewModel.AvailableRecordingFormats.Clear();
                        foreach (var format in formats)
                        {
                            viewModel.AvailableRecordingFormats.Add(format);
                        }
                    },
                    GetSelectedRecordingFormat = () => viewModel.SelectedRecordingFormat,
                    SetSelectedRecordingFormat = value => viewModel.SelectedRecordingFormat = value,
                    NotifySelectedRecordingFormatChanged = () => viewModel.OnPropertyChanged(nameof(SelectedRecordingFormat)),
                    IsHdrEnabled = () => viewModel.IsHdrEnabled,
                    SetStatusText = value => viewModel.StatusText = value,
                    IsFfmpegMissing = () => viewModel.IsFfmpegMissing,
                    SetIsFfmpegMissing = value => viewModel.IsFfmpegMissing = value,
                    HasUiThreadAccess = () => viewModel._dispatcherQueue.HasThreadAccess,
                    TryEnqueueOnUiThread = operation => viewModel._dispatcherQueue.TryEnqueue(() => operation()),
                    GetAvailableSplitEncodeModes = () => viewModel.AvailableSplitEncodeModes,
                    ReplaceAvailableSplitEncodeModes = modes =>
                    {
                        viewModel.AvailableSplitEncodeModes.Clear();
                        foreach (var mode in modes)
                        {
                            viewModel.AvailableSplitEncodeModes.Add(mode);
                        }
                    },
                    GetSelectedSplitEncodeMode = () => viewModel.SelectedSplitEncodeMode,
                    SetSelectedSplitEncodeMode = value => viewModel.SelectedSplitEncodeMode = value,
                    AvailableSplitEncodeModesContains = value => viewModel.AvailableSplitEncodeModes.Contains(value),
                });
        }

        private static MainViewModelRecordingSettingsAutomationController CreateRecordingSettingsAutomationController(MainViewModel viewModel)
        {
            return new MainViewModelRecordingSettingsAutomationController(
                new MainViewModelRecordingSettingsAutomationControllerContext
                {
                    InvokeRecordingFormatOnUiThreadAsync = (operation, cancellationToken) =>
                        viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    InvokeEncoderSettingsOnUiThreadAsync = (operation, cancellationToken) =>
                        viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    InvokeOnUiThreadAsync = (operation, cancellationToken) =>
                        viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    GetAvailableRecordingFormats = () => viewModel.AvailableRecordingFormats,
                    GetAvailableQualities = () => viewModel.AvailableQualities,
                    GetAvailableSplitEncodeModes = () => viewModel.AvailableSplitEncodeModes,
                    GetAvailablePresets = () => viewModel.AvailablePresets,
                    IsHdrEnabled = () => viewModel.IsHdrEnabled,
                    SetSuppressFlashbackFormatCycle = value => viewModel._suppressFlashbackFormatCycle = value,
                    SetSuppressFlashbackEncoderSettingsCycle = value => viewModel._suppressFlashbackEncoderSettingsCycle = value,
                    SetSelectedRecordingFormat = value => viewModel.SelectedRecordingFormat = value,
                    GetSelectedQuality = () => viewModel.SelectedQuality,
                    SetSelectedQuality = value => viewModel.SelectedQuality = value,
                    GetSelectedSplitEncodeMode = () => viewModel.SelectedSplitEncodeMode,
                    SetSelectedSplitEncodeMode = value => viewModel.SelectedSplitEncodeMode = value,
                    GetSelectedPreset = () => viewModel.SelectedPreset,
                    SetSelectedPreset = value => viewModel.SelectedPreset = value,
                    GetCustomBitrateMbps = () => viewModel.CustomBitrateMbps,
                    SetCustomBitrateMbps = value => viewModel.CustomBitrateMbps = value,
                    SetOutputPath = value => viewModel.OutputPath = value,
                    UpdateRecordingFormatAsync = (format, cancellationToken) =>
                        viewModel._sessionCoordinator.UpdateRecordingFormatAsync(format, cancellationToken),
                    CycleFlashbackEncoderSettingsAsync = (quality, customBitrateMbps, nvencPreset, splitEncodeMode, cancellationToken) =>
                        viewModel._sessionCoordinator.CycleFlashbackEncoderSettingsAsync(
                            quality,
                            customBitrateMbps,
                            nvencPreset,
                            splitEncodeMode,
                            cancellationToken),
                });
        }

        private static MainViewModelRuntimeEventIngressController CreateRuntimeEventIngressController(
            MainViewModel viewModel,
            MainViewModelPreviewLifecycleController previewLifecycleController,
            MainViewModelDeviceFormatProbeController deviceFormatProbeController,
            MainViewModelSourceTelemetryController sourceTelemetryController)
        {
            return new MainViewModelRuntimeEventIngressController(
                new MainViewModelRuntimeEventIngressControllerContext
                {
                    AttachFormatProbeCompleted = handler => viewModel._deviceService.FormatProbeCompleted += handler,
                    DetachFormatProbeCompleted = handler => viewModel._deviceService.FormatProbeCompleted -= handler,
                    OnDeviceFormatProbeCompleted = deviceFormatProbeController.OnDeviceFormatProbeCompleted,
                    AttachCaptureStatusChanged = handler => viewModel._captureService.StatusChanged += handler,
                    DetachCaptureStatusChanged = handler => viewModel._captureService.StatusChanged -= handler,
                    AttachCaptureErrorOccurred = handler => viewModel._captureService.ErrorOccurred += handler,
                    DetachCaptureErrorOccurred = handler => viewModel._captureService.ErrorOccurred -= handler,
                    AttachCapturePreCleanupRequested = handler => viewModel._captureService.PreCleanupRequested += handler,
                    DetachCapturePreCleanupRequested = handler => viewModel._captureService.PreCleanupRequested -= handler,
                    AttachFrameCaptured = handler => viewModel._captureService.FrameCaptured += handler,
                    DetachFrameCaptured = handler => viewModel._captureService.FrameCaptured -= handler,
                    AttachAudioLevelUpdated = handler => viewModel._captureService.AudioLevelUpdated += handler,
                    DetachAudioLevelUpdated = handler => viewModel._captureService.AudioLevelUpdated -= handler,
                    OnAudioLevelUpdated = viewModel.OnAudioLevelUpdated,
                    AttachMicrophoneAudioLevelUpdated = handler => viewModel._captureService.MicrophoneAudioLevelUpdated += handler,
                    DetachMicrophoneAudioLevelUpdated = handler => viewModel._captureService.MicrophoneAudioLevelUpdated -= handler,
                    OnMicrophoneAudioLevelUpdated = viewModel.OnMicrophoneAudioLevelUpdated,
                    AttachSourceTelemetryUpdated = handler => viewModel._captureService.SourceTelemetryUpdated += handler,
                    DetachSourceTelemetryUpdated = handler => viewModel._captureService.SourceTelemetryUpdated -= handler,
                    OnSourceTelemetryUpdated = sourceTelemetryController.OnSourceTelemetryUpdated,
                    AttachAudioDevicesChanged = handler => viewModel._audioDeviceWatcher.DevicesChanged += handler,
                    DetachAudioDevicesChanged = handler => viewModel._audioDeviceWatcher.DevicesChanged -= handler,
                    OnAudioDevicesChanged = viewModel.OnAudioDevicesChanged,
                    TryEnqueueOnUiThread = operation => viewModel._dispatcherQueue.TryEnqueue(() => operation()),
                    GetRuntimeSnapshot = viewModel._captureService.GetRuntimeSnapshot,
                    SetStatusText = value => viewModel.StatusText = value,
                    UpdateLiveCaptureInfo = snapshot => viewModel.UpdateLiveCaptureInfo(snapshot),
                    UpdateHdrRuntimeStatusFromCapture = snapshot => viewModel.UpdateHdrRuntimeStatusFromCapture(snapshot),
                    SetIsInitialized = value => viewModel.IsInitialized = value,
                    IsCaptureInitialized = () => viewModel._captureService.IsInitialized,
                    IsInitialized = () => viewModel.IsInitialized,
                    SetIsPreviewing = value => viewModel.IsPreviewing = value,
                    IsVideoPreviewActive = () => viewModel._captureService.IsVideoPreviewActive,
                    IsPreviewing = () => viewModel.IsPreviewing,
                    SetIsRecording = value => viewModel.IsRecording = value,
                    IsCaptureRecording = () => viewModel._captureService.IsRecording,
                    IsRecording = () => viewModel.IsRecording,
                    ResetAudioMeter = viewModel.ResetAudioMeter,
                    GetPreviewRendererStopHandlers = () =>
                    {
                        var handlers = viewModel.PreviewRendererStopRequested;
                        return handlers != null
                            ? Array.ConvertAll(handlers.GetInvocationList(), handler => (Func<Task>)handler)
                            : Array.Empty<Func<Task>>();
                    },
                    ReinitializeDeviceAsync = previewLifecycleController.ReinitializeDeviceAsync,
                    EnqueueUiOperation = (operation, operationName) => viewModel.EnqueueUiOperation(operation, operationName),
                });
        }

        internal static MainViewModelFrameRateTimingResolver CreateFrameRateTimingResolver(MainViewModel viewModel)
        {
            return new MainViewModelFrameRateTimingResolver(
                new MainViewModelFrameRateTimingResolverContext
                {
                    GetResolutionToFormats = () => viewModel._resolutionToFormats,
                    GetRuntimeSnapshot = () => viewModel._captureService.GetRuntimeSnapshot(),
                    GetLatestSourceTelemetry = () => viewModel._latestSourceTelemetry,
                    GetSelectedFormat = () => viewModel.SelectedFormat,
                    AvailableFrameRates = viewModel.AvailableFrameRates,
                });
        }

        private static MainViewModelCaptureModeOptionRebuildController CreateCaptureModeOptionRebuildController(MainViewModel viewModel)
        {
            return new MainViewModelCaptureModeOptionRebuildController(
                new MainViewModelCaptureModeOptionRebuildControllerContext
                {
                    AvailableFormats = viewModel.AvailableFormats,
                    AvailableFrameRates = viewModel.AvailableFrameRates,
                    AvailableResolutions = viewModel.AvailableResolutions,
                    AvailableVideoFormats = viewModel.AvailableVideoFormats,
                    AutoResolutionValue = AutoResolutionValue,
                    AutoFrameRateValue = AutoFrameRateValue,
                    GetResolutionToFormats = () => viewModel._resolutionToFormats,
                    GetLatestSourceTelemetry = () => viewModel._latestSourceTelemetry,
                    TryGetEffectiveResolutionSelection = viewModel.TryGetEffectiveResolutionSelection,
                    TryResolveResolutionKey = viewModel.TryResolveResolutionKey,
                    GetEffectiveResolutionKey = viewModel.GetEffectiveResolutionKey,
                    ApplyResolvedFrameRateSelection = viewModel.ApplyResolvedFrameRateSelection,
                    GetSelectedResolutionDisplayText = viewModel.GetSelectedResolutionDisplayText,
                    BuildHdrSupportHintForResolution = viewModel.BuildHdrSupportHintForResolution,
                    UpdateTargetSummary = viewModel.UpdateTargetSummary,
                    NotifySelectedResolutionChanged = () => viewModel.OnPropertyChanged(nameof(SelectedResolution)),
                    GetSelectedDevice = () => viewModel.SelectedDevice,
                    GetSelectedResolution = () => viewModel.SelectedResolution,
                    SetSelectedResolution = value => viewModel.SelectedResolution = value,
                    GetSelectedFrameRate = () => viewModel.SelectedFrameRate,
                    GetSelectedVideoFormat = () => viewModel.SelectedVideoFormat,
                    SetSelectedVideoFormat = value => viewModel.SelectedVideoFormat = value,
                    SetSelectedFormat = value => viewModel.SelectedFormat = value,
                    IsHdrEnabled = () => viewModel.IsHdrEnabled,
                    IsPreviewing = () => viewModel.IsPreviewing,
                    IsAutoFrameRateSelected = () => viewModel.IsAutoFrameRateSelected,
                    SetIsAutoFrameRateSelected = value => viewModel.IsAutoFrameRateSelected = value,
                    HasUserOverriddenResolutionForCurrentMode = () => viewModel._hasUserOverriddenResolutionForCurrentMode,
                    HasUserOverriddenFrameRateForCurrentMode = () => viewModel._hasUserOverriddenFrameRateForCurrentMode,
                    IsPendingSdrAutoSelectionForDeviceChange = () => viewModel._pendingSdrAutoSelectionForDeviceChange,
                    SetPendingSdrAutoSelectionForDeviceChange = value => viewModel._pendingSdrAutoSelectionForDeviceChange = value,
                    GetPendingSdrAutoFriendlyFrameRateBucket = () => viewModel._pendingSdrAutoFriendlyFrameRateBucket,
                    SetPendingSdrAutoFriendlyFrameRateBucket = value => viewModel._pendingSdrAutoFriendlyFrameRateBucket = value,
                    IsForceSourceAutoRetarget = () => viewModel._forceSourceAutoRetarget,
                    SetForceSourceAutoRetarget = value => viewModel._forceSourceAutoRetarget = value,
                    GetLastKnownResolutionKey = () => viewModel._lastKnownResolutionKey,
                    SetLastKnownResolutionKey = value => viewModel._lastKnownResolutionKey = value,
                    SetIsRebuildingModeOptions = value => viewModel._isRebuildingModeOptions = value,
                    SetIsApplyingAutomaticResolutionSelection = value => viewModel._isApplyingAutomaticResolutionSelection = value,
                    SetIsApplyingAutomaticFrameRateSelection = value => viewModel._isApplyingAutomaticFrameRateSelection = value,
                    IsSuppressFormatChangeReinitialize = () => viewModel._suppressFormatChangeReinitialize,
                    SetSuppressFormatChangeReinitialize = value => viewModel._suppressFormatChangeReinitialize = value,
                    SetDetectedSourceFrameRate = value => viewModel.DetectedSourceFrameRate = value,
                    SetDetectedSourceFrameRateArg = value => viewModel.DetectedSourceFrameRateArg = value,
                    SetSourceFrameRateOrigin = value => viewModel.SourceFrameRateOrigin = value,
                    SetAutoResolvedWidth = value => viewModel.AutoResolvedWidth = value,
                    SetAutoResolvedHeight = value => viewModel.AutoResolvedHeight = value,
                    SetAutoResolvedFrameRate = value => viewModel.AutoResolvedFrameRate = value,
                    SetHdrResolutionSupportHint = value => viewModel.HdrResolutionSupportHint = value,
                    SetDisabledResolutionReason = value => viewModel.DisabledResolutionReason = value,
                    SetStatusText = value => viewModel.StatusText = value,
                },
                viewModel._frameRateTimingResolver);
        }

        private static MainViewModelDeviceFormatProbeController CreateDeviceFormatProbeController(MainViewModel viewModel)
        {
            return new MainViewModelDeviceFormatProbeController(
                new MainViewModelDeviceFormatProbeControllerContext
                {
                    TryEnqueueOnUiThread = operation => viewModel._dispatcherQueue.TryEnqueue(() => operation()),
                    ReadDeviceScanGeneration = () => Interlocked.Read(ref viewModel._deviceScanGeneration),
                    FindDeviceById = deviceId => viewModel.Devices.FirstOrDefault(
                        device => string.Equals(device.Id, deviceId, StringComparison.OrdinalIgnoreCase)),
                    SetPendingSdrAutoSelectionForDeviceChange = value => viewModel._pendingSdrAutoSelectionForDeviceChange = value,
                    SetPendingSdrAutoFriendlyFrameRateBucket = value => viewModel._pendingSdrAutoFriendlyFrameRateBucket = value,
                    GetSelectedDevice = () => viewModel.SelectedDevice,
                    IsPreviewing = () => viewModel.IsPreviewing,
                    IsInitialized = () => viewModel.IsInitialized,
                    IsRecording = () => viewModel.IsRecording,
                    GetSelectedResolution = () => viewModel.SelectedResolution,
                    GetSelectedFrameRate = () => viewModel.SelectedFrameRate,
                    GetSelectedFormat = () => viewModel.SelectedFormat,
                    SetSuppressFormatChangeReinitialize = value => viewModel._suppressFormatChangeReinitialize = value,
                    RebuildSelectedDeviceCapabilities = (device, resetTelemetryState) =>
                        viewModel.RebuildSelectedDeviceCapabilities(device, resetTelemetryState),
                    CreateRetargetApplier = () => new MainViewModelDeviceFormatProbeRetargetApplier(
                        new MainViewModelDeviceFormatProbeRetargetApplierContext
                        {
                            IsHdrEnabled = () => viewModel.IsHdrEnabled,
                            GetSelectedResolution = () => viewModel.SelectedResolution,
                            SetSelectedResolution = value => viewModel.SelectedResolution = value,
                            GetSelectedFrameRate = () => viewModel.SelectedFrameRate,
                            SetSelectedFrameRate = value => viewModel.SelectedFrameRate = value,
                            GetSelectedFormat = () => viewModel.SelectedFormat,
                            AvailableResolutionsContains = value => viewModel.AvailableResolutions.Any(
                                option => string.Equals(option.Value, value, StringComparison.OrdinalIgnoreCase)),
                            SetIsRebuildingModeOptions = value => viewModel._isRebuildingModeOptions = value,
                            SetIsApplyingAutomaticResolutionSelection = value => viewModel._isApplyingAutomaticResolutionSelection = value,
                            SetSuppressFormatChangeReinitialize = value => viewModel._suppressFormatChangeReinitialize = value,
                            RebuildFrameRateOptions = viewModel.RebuildFrameRateOptions,
                            ReinitializeDeviceAsync = viewModel.ReinitializeDeviceAsync,
                            EnqueueUiOperation = (operation, operationName) => viewModel.EnqueueUiOperation(operation, operationName),
                            GetCaptureRuntimeSnapshot = viewModel.GetCaptureRuntimeSnapshot,
                            UpdateSelectedFormat = viewModel.UpdateSelectedFormat,
                            UpdateTargetSummary = viewModel.UpdateTargetSummary,
                        }),
                });
        }

        private static MainViewModelPreviewLifecycleController CreatePreviewLifecycleController(MainViewModel viewModel)
        {
            return new MainViewModelPreviewLifecycleController(
                new MainViewModelPreviewLifecycleControllerContext
                {
                    SessionCoordinator = viewModel._sessionCoordinator,
                    BuildCaptureSettings = viewModel.BuildCaptureSettings,
                    InvokeOnUiThreadAsync = (operation, cancellationToken) => viewModel.InvokeOnUiThreadAsync(operation, cancellationToken),
                    RampPreviewVolumeDownForStopAsync = viewModel.RampPreviewVolumeDownForStopAsync,
                    CreateReinitializeController = controller => new MainViewModelPreviewReinitializeController(
                        new MainViewModelPreviewReinitializeControllerContext
                        {
                            SelectedDevice = () => viewModel.SelectedDevice,
                            SelectedFormat = () => viewModel.SelectedFormat,
                            IsRecording = () => viewModel.IsRecording,
                            IsInitialized = () => viewModel.IsInitialized,
                            SetIsInitialized = value => viewModel.IsInitialized = value,
                            IsPreviewing = () => viewModel.IsPreviewing,
                            SetIsPreviewing = value => viewModel.IsPreviewing = value,
                            IsPreviewReinitializing = () => viewModel.IsPreviewReinitializing,
                            SetIsPreviewReinitializing = value => viewModel.IsPreviewReinitializing = value,
                            SetStatusText = value => viewModel.StatusText = value,
                            CancelPreviewRestartAfterReinitialize = () => viewModel._cancelPreviewRestartAfterReinitialize,
                            SetCancelPreviewRestartAfterReinitialize = value => viewModel._cancelPreviewRestartAfterReinitialize = value,
                            IncrementReinitializeGeneration = () => Interlocked.Increment(ref viewModel._previewReinitializeGeneration),
                            ReadReinitializeGeneration = () => Volatile.Read(ref viewModel._previewReinitializeGeneration),
                            PreviewReinitializeDebounceMs = PreviewReinitializeDebounceMs,
                            PendingFlashbackCycleTask = () => viewModel._pendingFlashbackCycleTask,
                            FlashbackCycleBeforeReinitializeTimeoutMs = FlashbackCycleBeforeReinitializeTimeoutMs,
                            AwaitWithTimeoutAsync = AwaitWithTimeoutAsync,
                            ClearPendingFlashbackCycleIfSameAndCompleted = task =>
                            {
                                if (ReferenceEquals(viewModel._pendingFlashbackCycleTask, task) && task.IsCompleted)
                                {
                                    viewModel._pendingFlashbackCycleTask = null;
                                }
                            },
                            WaitReinitializeGateAsync = viewModel._previewReinitializeGate.WaitAsync,
                            ReleaseReinitializeGate = () => viewModel._previewReinitializeGate.Release(),
                            NotifyPreviewReinitRequestedAsync = viewModel.NotifyPreviewReinitRequestedAsync,
                            NotifyRendererStopAsync = viewModel.NotifyRendererStopAsync,
                        },
                        controller),
                    SelectedDevice = () => viewModel.SelectedDevice,
                    SetSelectedDevice = device => viewModel.SelectedDevice = device,
                    CaptureSelectionSnapshot = viewModel.CaptureSelectionSnapshot,
                    RestoreCaptureSelectionSnapshotIfUnchanged = viewModel.RestoreCaptureSelectionSnapshotIfUnchanged,
                    IsInitialized = () => viewModel.IsInitialized,
                    SetIsInitialized = value => viewModel.IsInitialized = value,
                    IsPreviewing = () => viewModel.IsPreviewing,
                    SetIsPreviewing = value => viewModel.IsPreviewing = value,
                    IsPreviewReinitializing = () => viewModel.IsPreviewReinitializing,
                    IsRecording = () => viewModel.IsRecording,
                    ShouldStartAudioPreview = () => viewModel.IsAudioPreviewEnabled && viewModel.IsAudioEnabled,
                    IsAudioPreviewActive = () => viewModel._captureService.IsAudioPreviewActive,
                    SetStatusText = value => viewModel.StatusText = value,
                    RaisePreviewStartRequested = () => viewModel.PreviewStartRequested?.Invoke(viewModel, EventArgs.Empty),
                    RaisePreviewStopRequested = () => viewModel.PreviewStopRequested?.Invoke(viewModel, EventArgs.Empty),
                    ApplyLatestSourceTelemetryForPreviewStart = () =>
                        viewModel._sourceTelemetryController.ApplySourceTelemetrySnapshot(
                            viewModel._captureService.GetLatestSourceTelemetrySnapshot(),
                            allowAutoRetarget: true),
                });
        }

        private static MainViewModelRuntimeLifecycleController CreateRuntimeLifecycleController(
            MainViewModel viewModel,
            MainViewModelPreviewLifecycleController previewLifecycleController,
            MainViewModelDeviceFormatProbeController deviceFormatProbeController,
            MainViewModelSourceTelemetryController sourceTelemetryController)
        {
            return new MainViewModelRuntimeLifecycleController(
                new MainViewModelRuntimeLifecycleControllerContext
                {
                    CreateEventIngressController = () => CreateRuntimeEventIngressController(
                        viewModel,
                        previewLifecycleController,
                        deviceFormatProbeController,
                        sourceTelemetryController),
                    CreateTimer = viewModel._dispatcherQueue.CreateTimer,
                    GetRuntimeSnapshot = viewModel._captureService.GetRuntimeSnapshot,
                    GetLatestSourceTelemetrySnapshot = viewModel._captureService.GetLatestSourceTelemetrySnapshot,
                    SetLatestSourceTelemetrySnapshot = snapshot => viewModel._latestSourceTelemetry = snapshot,
                    ApplySourceTelemetrySnapshot = sourceTelemetryController.ApplySourceTelemetrySnapshot,
                    UpdateHdrRuntimeStatusFromCaptureWithoutSnapshot = () => viewModel.UpdateHdrRuntimeStatusFromCapture(),
                    UpdateHdrRuntimeStatusFromCaptureWithSnapshot = snapshot => viewModel.UpdateHdrRuntimeStatusFromCapture(snapshot),
                    UpdateLiveCaptureInfoWithoutSnapshot = () => viewModel.UpdateLiveCaptureInfo(),
                    UpdateLiveCaptureInfoWithSnapshot = snapshot => viewModel.UpdateLiveCaptureInfo(snapshot),
                    ResetLiveCaptureInfo = viewModel.ResetLiveCaptureInfo,
                    UpdateDiskSpace = viewModel.UpdateDiskSpace,
                    RefreshSourceTelemetrySummaryAge = sourceTelemetryController.RefreshSourceTelemetrySummaryAge,
                    IsRecording = () => viewModel.IsRecording,
                    IsPreviewing = () => viewModel.IsPreviewing,
                    IsFlashbackActive = () => viewModel._captureService.IsFlashbackActive,
                    GetRecordingElapsed = () => viewModel._recordingStopwatch.Elapsed,
                    SetRecordingTime = value => viewModel.RecordingTime = value,
                    UpdateRecordingStats = viewModel.UpdateRecordingStats,
                    UpdateFlashbackBitrate = viewModel.UpdateFlashbackBitrate,
                    DisposeAudioDeviceWatcher = viewModel._audioDeviceWatcher.Dispose,
                });
        }

        private static MainViewModelDisposalController CreateDisposalController(
            MainViewModel viewModel,
            MainViewModelDeviceAudioRequestController deviceAudioRequestController,
            MainViewModelRuntimeLifecycleController runtimeLifecycleController)
        {
            return new MainViewModelDisposalController(
                new MainViewModelDisposalControllerContext
                {
                    TryBeginDispose = () => Interlocked.Exchange(ref viewModel._disposeState, 1) == 0,
                    CancelActiveFlashbackExport = viewModel.CancelActiveFlashbackExportForDispose,
                    CancelPendingAudioControlWork = deviceAudioRequestController.CancelPendingAudioControlWork,
                    StopRuntimeForDispose = runtimeLifecycleController.StopForDispose,
                    CleanupSessionCoordinatorAsync = () => viewModel._sessionCoordinator.CleanupAsync(),
                    DisposeSessionCoordinatorAsync = () => viewModel._sessionCoordinator.DisposeAsync().AsTask(),
                    DisposeCaptureServiceAsync = () => viewModel._captureService.DisposeAsync().AsTask(),
                    DisposeCaptureService = viewModel._captureService.Dispose,
                    AwaitWithTimeoutAsync = AwaitWithTimeoutAsync,
                });
        }
    }
}

/// <summary>
/// Owns bounded byte-sample smoothing for recording and Flashback bitrate labels.
/// </summary>
internal sealed class BitrateSampleWindow
{
    private readonly long _windowMs;
    private readonly Queue<(long Tick, long Bytes)> _samples = new();

    public BitrateSampleWindow(long windowMs)
    {
        if (windowMs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowMs), "Bitrate sample window must be positive.");
        }

        _windowMs = windowMs;
    }

    public void Clear()
    {
        _samples.Clear();
    }

    public double? AddSampleAndCompute(long tick, long bytes)
    {
        _samples.Enqueue((tick, bytes));
        while (_samples.Count > 0 && tick - _samples.Peek().Tick > _windowMs)
        {
            _samples.Dequeue();
        }

        return ComputeAverageBitrate(_samples);
    }

    private static double? ComputeAverageBitrate(Queue<(long Tick, long Bytes)> samples)
    {
        if (samples.Count < 2)
        {
            return null;
        }

        var first = samples.Peek();
        var last = samples.Last();
        var deltaMs = last.Tick - first.Tick;
        if (deltaMs <= 0)
        {
            return null;
        }

        var deltaBytes = Math.Max(0, last.Bytes - first.Bytes);
        return (deltaBytes * 8.0) / (deltaMs / 1000.0);
    }
}

// Construction seam for the root compatibility view model. MainViewModel keeps
// the XAML/automation-facing property surface, while this type owns the default
// service graph until a fuller composition root can inject feature view models.
internal sealed class MainViewModelDependencies
{
    private MainViewModelDependencies(
        DeviceService deviceService,
        CaptureService captureService,
        CaptureSessionCoordinator sessionCoordinator,
        NativeXuAudioControlService deviceAudioControlService,
        DispatcherQueue dispatcherQueue,
        AudioDeviceWatcher audioDeviceWatcher)
    {
        DeviceService = deviceService ?? throw new ArgumentNullException(nameof(deviceService));
        CaptureService = captureService ?? throw new ArgumentNullException(nameof(captureService));
        SessionCoordinator = sessionCoordinator ?? throw new ArgumentNullException(nameof(sessionCoordinator));
        DeviceAudioControlService = deviceAudioControlService ?? throw new ArgumentNullException(nameof(deviceAudioControlService));
        DispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
        AudioDeviceWatcher = audioDeviceWatcher ?? throw new ArgumentNullException(nameof(audioDeviceWatcher));
    }

    public DeviceService DeviceService { get; }
    public CaptureService CaptureService { get; }
    public CaptureSessionCoordinator SessionCoordinator { get; }
    public NativeXuAudioControlService DeviceAudioControlService { get; }
    public DispatcherQueue DispatcherQueue { get; }
    public AudioDeviceWatcher AudioDeviceWatcher { get; }

    public static MainViewModelDependencies CreateDefault()
    {
        var captureService = new CaptureService();
        return new MainViewModelDependencies(
            new DeviceService(),
            captureService,
            new CaptureSessionCoordinator(captureService),
            new NativeXuAudioControlService(),
            DispatcherQueue.GetForCurrentThread(),
            new AudioDeviceWatcher());
    }
}
