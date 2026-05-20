namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static FlashbackRecordingBackendFlattenedProjection BuildFlashbackRecordingBackendFlattenedProjection(
        FlashbackRecordingProjection flashbackRecording)
        => new()
        {
            SettingsStale = flashbackRecording.BackendSettingsStale,
            SettingsStaleReason = flashbackRecording.BackendSettingsStaleReason,
            ActiveFormat = flashbackRecording.BackendActiveFormat,
            RequestedFormat = flashbackRecording.BackendRequestedFormat,
            ActivePreset = flashbackRecording.BackendActivePreset,
            RequestedPreset = flashbackRecording.BackendRequestedPreset,
            ExportVerificationFormat = flashbackRecording.ExportVerificationFormat,
            CodecDowngradeReason = flashbackRecording.CodecDowngradeReason,
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
