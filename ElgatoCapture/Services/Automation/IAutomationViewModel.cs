using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;

namespace ElgatoCapture.Services;

/// <summary>
/// Abstraction over MainViewModel consumed by the automation layer.
/// Lives in ElgatoCapture.Services so the dependency arrow points
/// inward (Services defines the contract, ViewModels implements it).
/// </summary>
public interface IAutomationViewModel
{
    // ── Properties ──────────────────────────────────────────────────

    bool IsInitialized { get; }
    ObservableCollection<CaptureDevice> Devices { get; }

    // ── Snapshot / diagnostic queries ───────────────────────────────

    Task<CaptureRuntimeSnapshot> GetCaptureRuntimeSnapshotAsync(CancellationToken cancellationToken = default);
    Task<ViewModelRuntimeSnapshot> GetViewModelRuntimeSnapshotAsync(CancellationToken cancellationToken = default);
    Task<CaptureHealthSnapshot> GetCaptureHealthSnapshotAsync(CancellationToken cancellationToken = default);
    Task<RecordingStats> GetRecordingStatsSnapshotAsync(CancellationToken cancellationToken = default);
    Task<AutomationOptionsSnapshot> GetAutomationOptionsSnapshotAsync(CancellationToken cancellationToken = default);

    // ── Device selection ────────────────────────────────────────────

    Task RefreshDevicesForAutomationAsync(CancellationToken cancellationToken = default);
    Task SelectDeviceAsync(string? deviceId, string? deviceName, CancellationToken cancellationToken = default);
    Task SelectAudioInputDeviceAsync(string? deviceId, string? deviceName, CancellationToken cancellationToken = default);
    Task SetCustomAudioInputEnabledAsync(bool enabled, CancellationToken cancellationToken = default);

    // ── Capture settings ────────────────────────────────────────────

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

    // ── HDR ─────────────────────────────────────────────────────────

    Task SetHdrEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
    Task SetTrueHdrPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default);

    // ── Audio ───────────────────────────────────────────────────────

    Task SetAudioEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
    Task SetAudioPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
    Task SetDeviceAudioModeAsync(string mode, CancellationToken cancellationToken = default);
    Task SetAnalogAudioGainAsync(double gainPercent, CancellationToken cancellationToken = default);

    // ── Preview / recording / output ────────────────────────────────

    Task SetPreviewVolumeAsync(double previewVolumePercent, CancellationToken cancellationToken = default);
    Task SetPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
    Task SetRecordingEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
    Task SetOutputPathAsync(string outputPath, CancellationToken cancellationToken = default);

    // ── UI panels ───────────────────────────────────────────────────

    Task SetStatsVisibleAsync(bool visible, CancellationToken cancellationToken = default);
    Task SetStatsSectionVisibleAsync(string section, bool visible, CancellationToken cancellationToken = default);
    Task SetSettingsVisibleAsync(bool visible, CancellationToken cancellationToken = default);

    // ── Flashback ───────────────────────────────────────────────────

    bool FlashbackPlay();
    bool FlashbackPause();
    bool FlashbackGoLive();
    bool FlashbackBeginScrub(TimeSpan position);
    bool FlashbackEndScrub();
    Task<FinalizeResult> ExportFlashbackAutomationAsync(double seconds, string outputPath, CancellationToken ct);
    IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments();

    // ── Probes ──────────────────────────────────────────────────────

    VideoSourceProbeResult ProbeVideoSource();
    PreviewColorProbeResult ProbePreviewColor();
    Task<PreviewFrameCaptureResult> CapturePreviewFrameAsync(string outputPath, CancellationToken cancellationToken = default);
}
