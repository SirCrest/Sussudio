using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackRecordingBackendProjection BuildFlashbackRecordingBackendProjection(
        CaptureRuntimeSnapshot captureRuntime,
        CaptureHealthSnapshot health)
        => new()
        {
            SettingsStale = health.FlashbackBackendSettingsStale,
            SettingsStaleReason = health.FlashbackBackendSettingsStaleReason,
            ActiveFormat = health.FlashbackBackendActiveFormat,
            RequestedFormat = health.FlashbackBackendRequestedFormat,
            ActivePreset = health.FlashbackBackendActivePreset,
            RequestedPreset = health.FlashbackBackendRequestedPreset,
            ExportVerificationFormat = captureRuntime.FlashbackExportVerificationFormat ?? health.FlashbackExportVerificationFormat,
            CodecDowngradeReason = captureRuntime.FlashbackCodecDowngradeReason ?? health.FlashbackCodecDowngradeReason
        };

    private readonly record struct FlashbackRecordingBackendProjection
    {
        public bool SettingsStale { get; init; }
        public string SettingsStaleReason { get; init; }
        public string ActiveFormat { get; init; }
        public string RequestedFormat { get; init; }
        public string ActivePreset { get; init; }
        public string RequestedPreset { get; init; }
        public string? ExportVerificationFormat { get; init; }
        public string? CodecDowngradeReason { get; init; }
    }
}
