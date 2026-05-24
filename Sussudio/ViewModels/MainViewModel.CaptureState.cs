using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Sussudio.Models;

namespace Sussudio.ViewModels;

// Capture-selection state and option collections.
public partial class MainViewModel
{
    private const int PreviewReinitializeDebounceMs = 250;
    private const string AutoResolutionValue = "Source";
    private const double AutoFrameRateValue = 0;
    private const string HdrToggleBlockedWhileRecordingMessage = "Stop recording before switching between HDR and SDR pipelines.";

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

    private SourceSignalTelemetrySnapshot _latestSourceTelemetry = SourceSignalTelemetrySnapshot.CreateUnavailable("telemetry-not-started");

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
}
