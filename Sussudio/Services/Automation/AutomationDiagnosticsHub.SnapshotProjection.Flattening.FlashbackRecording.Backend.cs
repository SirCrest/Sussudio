namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackRecordingBackendFlattenedProjection BuildFlashbackRecordingBackendFlattenedProjection(
        FlashbackRecordingBackendProjection backend)
        => new()
        {
            SettingsStale = backend.SettingsStale,
            SettingsStaleReason = backend.SettingsStaleReason,
            ActiveFormat = backend.ActiveFormat,
            RequestedFormat = backend.RequestedFormat,
            ActivePreset = backend.ActivePreset,
            RequestedPreset = backend.RequestedPreset,
            ExportVerificationFormat = backend.ExportVerificationFormat,
            CodecDowngradeReason = backend.CodecDowngradeReason,
        };

    private readonly record struct FlashbackRecordingBackendFlattenedProjection
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
