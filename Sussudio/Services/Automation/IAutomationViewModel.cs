using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio.Services.Automation;

/// <summary>
/// Automation readiness state needed before device-bound commands can run.
/// </summary>
public interface IAutomationReadinessPort
{
    bool IsInitialized { get; }
    ObservableCollection<CaptureDevice> Devices { get; }
}

/// <summary>
/// Read-only automation snapshots and diagnostics probes.
/// </summary>
public interface IAutomationSnapshotQueryPort
{
    Task<CaptureRuntimeSnapshot> GetCaptureRuntimeSnapshotAsync(CancellationToken cancellationToken = default);
    Task<ViewModelRuntimeSnapshot> GetViewModelRuntimeSnapshotAsync(CancellationToken cancellationToken = default);
    Task<CaptureHealthSnapshot> GetCaptureHealthSnapshotAsync(CancellationToken cancellationToken = default);
    Task<RecordingStats> GetRecordingStatsSnapshotAsync(CancellationToken cancellationToken = default);
    Task<AutomationOptionsSnapshot> GetAutomationOptionsSnapshotAsync(CancellationToken cancellationToken = default);
    Task<AudioRampTraceSnapshot> GetAudioRampTraceSnapshotAsync(int maxEntries = 512, CancellationToken cancellationToken = default);
}

/// <summary>
/// Automation commands for capture-device and audio-input selection.
/// </summary>
public interface IAutomationDeviceSelectionPort
{
    Task RefreshDevicesForAutomationAsync(CancellationToken cancellationToken = default);
    Task SelectDeviceAsync(string? deviceId, string? deviceName, CancellationToken cancellationToken = default);
    Task SelectAudioInputDeviceAsync(string? deviceId, string? deviceName, CancellationToken cancellationToken = default);
    Task SetCustomAudioInputEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
}

/// <summary>
/// Automation commands for capture, recording, and HDR settings.
/// </summary>
public interface IAutomationCaptureSettingsPort
{
    Task SetResolutionAsync(string resolution, CancellationToken cancellationToken = default);
    Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default);
    Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default);
    Task SetPresetAsync(string preset, CancellationToken cancellationToken = default);
    Task SetSplitEncodeModeAsync(string splitEncodeMode, CancellationToken cancellationToken = default);
    Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default);
    Task SetShowAllCaptureOptionsAsync(bool enabled, CancellationToken cancellationToken = default);
    Task SetRecordingFormatAsync(string format, CancellationToken cancellationToken = default);
    Task SetQualityAsync(string quality, CancellationToken cancellationToken = default);
    Task SetCustomBitrateAsync(double bitrateMbps, CancellationToken cancellationToken = default);
    Task SetHdrEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
    Task SetTrueHdrPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
}

/// <summary>
/// Automation commands for audio and microphone controls.
/// </summary>
public interface IAutomationAudioPort
{
    Task SetAudioEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
    Task SetAudioPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
    Task SetDeviceAudioModeAsync(string mode, CancellationToken cancellationToken = default);
    Task SetAnalogAudioGainAsync(double gainPercent, CancellationToken cancellationToken = default);
    Task SetMicrophoneEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
}

/// <summary>
/// Automation commands for preview, recording, and output state.
/// </summary>
public interface IAutomationPreviewRecordingPort
{
    Task SetPreviewVolumeAsync(double previewVolumePercent, CancellationToken cancellationToken = default);
    Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
    Task SetRecordingEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
    Task SetOutputPathAsync(string outputPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Automation commands for UI-only panel and overlay state.
/// </summary>
public interface IAutomationUiPort
{
    Task SetStatsVisibleAsync(bool visible, CancellationToken cancellationToken = default);
    Task SetStatsSectionVisibleAsync(string section, bool visible, CancellationToken cancellationToken = default);
    Task SetSettingsVisibleAsync(bool visible, CancellationToken cancellationToken = default);
    Task SetFrameTimeOverlayVisibleAsync(bool visible, CancellationToken cancellationToken = default);
    Task SetFlashbackTimelineVisibleAsync(bool visible, CancellationToken cancellationToken = default);
}

/// <summary>
/// Automation commands and queries for Flashback.
/// </summary>
public interface IAutomationFlashbackPort
{
    Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
    Task RestartFlashbackAsync(CancellationToken cancellationToken = default);
    Task<bool> ExecuteFlashbackActionAsync(
        AutomationFlashbackAction action,
        TimeSpan? position = null,
        CancellationToken cancellationToken = default);
    Task<FinalizeResult> ExportFlashbackAutomationAsync(
        double seconds,
        string outputPath,
        bool useSelectionRange,
        bool force,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FlashbackSegmentInfo>> GetFlashbackSegmentsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Automation probes that inspect preview or source output.
/// </summary>
public interface IAutomationProbePort
{
    Task<VideoSourceProbeResult> ProbeVideoSourceAsync(CancellationToken cancellationToken = default);
    Task<PreviewColorProbeResult> ProbePreviewColorAsync(CancellationToken cancellationToken = default);
    Task<PreviewFrameCaptureResult> CapturePreviewFrameAsync(string outputPath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Feature-shaped automation ports composed from the aggregate ViewModel surface.
/// </summary>
internal readonly record struct AutomationViewModelPorts(
    IAutomationReadinessPort Readiness,
    IAutomationSnapshotQueryPort SnapshotQuery,
    IAutomationDeviceSelectionPort DeviceSelection,
    IAutomationCaptureSettingsPort CaptureSettings,
    IAutomationAudioPort Audio,
    IAutomationPreviewRecordingPort PreviewRecording,
    IAutomationUiPort Ui,
    IAutomationFlashbackPort Flashback,
    IAutomationProbePort Probe)
{
    public static AutomationViewModelPorts From(IAutomationViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        return new AutomationViewModelPorts(
            viewModel,
            viewModel,
            viewModel,
            viewModel,
            viewModel,
            viewModel,
            viewModel,
            viewModel,
            viewModel);
    }
}

/// <summary>
/// Aggregate automation ViewModel contract consumed by the current automation host.
/// Narrower ports above let command owners depend on feature-shaped interfaces as
/// the dispatcher continues to shed the root compatibility surface.
/// </summary>
public interface IAutomationViewModel :
    IAutomationReadinessPort,
    IAutomationSnapshotQueryPort,
    IAutomationDeviceSelectionPort,
    IAutomationCaptureSettingsPort,
    IAutomationAudioPort,
    IAutomationPreviewRecordingPort,
    IAutomationUiPort,
    IAutomationFlashbackPort,
    IAutomationProbePort
{
}
