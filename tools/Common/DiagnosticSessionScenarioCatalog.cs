namespace Sussudio.Tools;

internal static class DiagnosticSessionScenarioCatalog
{
    internal static IReadOnlyList<DiagnosticSessionScenarioCatalogEntry> Entries { get; } =
    [
        new(DiagnosticSessionScenarios.Observe),
        new(
            DiagnosticSessionScenarios.PreviewOnly,
            RequiresPreview: true),
        new(
            DiagnosticSessionScenarios.RecordingOnly,
            RequiresRecording: true),
        new(
            DiagnosticSessionScenarios.Flashback,
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-stress-export.mp4"),
        new(
            DiagnosticSessionScenarios.FlashbackPlayback,
            DiagnosticSessionScenarioPlan.Create(runFlashbackPlayback: true),
            RequiresPreview: true,
            RequiresFlashback: true),
        new(
            DiagnosticSessionScenarios.FlashbackStress,
            DiagnosticSessionScenarioPlan.Create(runFlashbackStress: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-stress-export.mp4"),
        new(
            DiagnosticSessionScenarios.FlashbackScrubStress,
            DiagnosticSessionScenarioPlan.Create(runFlashbackScrubStress: true),
            RequiresPreview: true,
            RequiresFlashback: true),
        new(
            DiagnosticSessionScenarios.FlashbackRestartCycle,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRestartCycle: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-restart-cycle-export.mp4"),
        new(
            DiagnosticSessionScenarios.FlashbackEncoderCycle,
            DiagnosticSessionScenarioPlan.Create(runFlashbackEncoderCycle: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-encoder-cycle-export.mp4"),
        new(
            DiagnosticSessionScenarios.FlashbackExportPlayback,
            DiagnosticSessionScenarioPlan.Create(runFlashbackExportPlayback: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-export-playback.mp4"),
        new(
            DiagnosticSessionScenarios.FlashbackSegmentPlayback,
            DiagnosticSessionScenarioPlan.Create(runFlashbackSegmentPlayback: true),
            RequiresPreview: true,
            RequiresFlashback: true),
        new(
            DiagnosticSessionScenarios.FlashbackRangeExport,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRangeExport: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-range-export.mp4"),
        new(
            DiagnosticSessionScenarios.FlashbackRangeExportAudioSwitch,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRangeExportAudioSwitch: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-range-export-audio-switch.mp4"),
        new(
            DiagnosticSessionScenarios.FlashbackLifecycle,
            DiagnosticSessionScenarioPlan.Create(runFlashbackLifecycle: true),
            RequiresPreview: true,
            RequiresFlashback: true),
        new(
            DiagnosticSessionScenarios.FlashbackExportConcurrent,
            DiagnosticSessionScenarioPlan.Create(runFlashbackExportConcurrent: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-concurrent-a.mp4"),
        new(
            DiagnosticSessionScenarios.FlashbackDisableDuringExport,
            DiagnosticSessionScenarioPlan.Create(runFlashbackDisableDuringExport: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-disable-during-export.mp4"),
        new(
            DiagnosticSessionScenarios.FlashbackRotatedExport,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRotatedExport: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-rotated-export.mp4"),
        new(
            DiagnosticSessionScenarios.FlashbackPreviewCycle,
            DiagnosticSessionScenarioPlan.Create(runFlashbackPreviewCycle: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-preview-off-export.mp4"),
        new(
            DiagnosticSessionScenarios.FlashbackPlaybackPreviewCycle,
            DiagnosticSessionScenarioPlan.Create(runFlashbackPlaybackPreviewCycle: true),
            RequiresPreview: true,
            RequiresFlashback: true,
            FlashbackExportVerificationFileName: "flashback-playback-preview-cycle.mp4"),
        new(
            DiagnosticSessionScenarios.FlashbackRecording,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRecording: true),
            RequiresPreview: true,
            RequiresRecording: true,
            RequiresFlashback: true),
        new(
            DiagnosticSessionScenarios.FlashbackRecordingPreviewCycle,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRecordingPreviewCycle: true),
            RequiresPreview: true,
            RequiresRecording: true,
            RequiresFlashback: true),
        new(
            DiagnosticSessionScenarios.FlashbackRecordingSettingsDeferred,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRecordingSettingsDeferred: true),
            RequiresPreview: true,
            RequiresRecording: true,
            RequiresFlashback: true),
        new(
            DiagnosticSessionScenarios.FlashbackRecordingExportRejected,
            DiagnosticSessionScenarioPlan.Create(runFlashbackRecordingExportRejected: true),
            RequiresPreview: true,
            RequiresRecording: true,
            RequiresFlashback: true),
        new(
            DiagnosticSessionScenarios.FlashbackExportRejected,
            DiagnosticSessionScenarioPlan.Create(runFlashbackExportRejected: true)),
        new(
            DiagnosticSessionScenarios.Combined,
            DiagnosticSessionScenarioPlan.Create(runCombined: true),
            RequiresPreview: true,
            RequiresRecording: true,
            RequiresFlashback: true)
    ];

    internal static IReadOnlyList<string> Names { get; } = Entries.Select(static entry => entry.Name).ToArray();

    internal static string HelpList { get; } = string.Join("|", Names);

    internal static bool TryGetEntry(string scenario, out DiagnosticSessionScenarioCatalogEntry entry)
    {
        foreach (var candidate in Entries)
        {
            if (string.Equals(candidate.Name, scenario, StringComparison.Ordinal))
            {
                entry = candidate;
                return true;
            }
        }

        entry = default;
        return false;
    }

}

internal readonly record struct DiagnosticSessionScenarioCatalogEntry(
    string Name,
    DiagnosticSessionScenarioPlan Plan = default,
    bool RequiresPreview = false,
    bool RequiresRecording = false,
    bool RequiresFlashback = false,
    string? FlashbackExportVerificationFileName = null);
