namespace Sussudio.Tools;

internal static class DiagnosticSessionScenarios
{
    internal const string Observe = "observe";
    internal const string PreviewOnly = "preview-only";
    internal const string RecordingOnly = "recording-only";
    internal const string Flashback = "flashback";
    internal const string FlashbackPlayback = "flashback-playback";
    internal const string FlashbackStress = "flashback-stress";
    internal const string FlashbackScrubStress = "flashback-scrub-stress";
    internal const string FlashbackRestartCycle = "flashback-restart-cycle";
    internal const string FlashbackEncoderCycle = "flashback-encoder-cycle";
    internal const string FlashbackExportPlayback = "flashback-export-playback";
    internal const string FlashbackSegmentPlayback = "flashback-segment-playback";
    internal const string FlashbackRangeExport = "flashback-range-export";
    internal const string FlashbackRangeExportAudioSwitch = "flashback-range-export-audio-switch";
    internal const string FlashbackLifecycle = "flashback-lifecycle";
    internal const string FlashbackExportConcurrent = "flashback-export-concurrent";
    internal const string FlashbackDisableDuringExport = "flashback-disable-during-export";
    internal const string FlashbackRotatedExport = "flashback-rotated-export";
    internal const string FlashbackPreviewCycle = "flashback-preview-cycle";
    internal const string FlashbackPlaybackPreviewCycle = "flashback-playback-preview-cycle";
    internal const string FlashbackRecording = "flashback-recording";
    internal const string FlashbackRecordingPreviewCycle = "flashback-recording-preview-cycle";
    internal const string FlashbackRecordingSettingsDeferred = "flashback-recording-settings-deferred";
    internal const string FlashbackRecordingExportRejected = "flashback-recording-export-rejected";
    internal const string FlashbackExportRejected = "flashback-export-rejected";
    internal const string Combined = "combined";

    internal static IReadOnlyList<string> All { get; } =
    [
        Observe,
        PreviewOnly,
        RecordingOnly,
        Flashback,
        FlashbackPlayback,
        FlashbackStress,
        FlashbackScrubStress,
        FlashbackRestartCycle,
        FlashbackEncoderCycle,
        FlashbackExportPlayback,
        FlashbackSegmentPlayback,
        FlashbackRangeExport,
        FlashbackRangeExportAudioSwitch,
        FlashbackLifecycle,
        FlashbackExportConcurrent,
        FlashbackDisableDuringExport,
        FlashbackRotatedExport,
        FlashbackPreviewCycle,
        FlashbackPlaybackPreviewCycle,
        FlashbackRecording,
        FlashbackRecordingPreviewCycle,
        FlashbackRecordingSettingsDeferred,
        FlashbackRecordingExportRejected,
        FlashbackExportRejected,
        Combined
    ];

    internal static string HelpList { get; } = string.Join("|", All);

    internal static string Normalize(string? scenario)
    {
        var normalized = string.IsNullOrWhiteSpace(scenario)
            ? Observe
            : scenario.Trim().ToLowerInvariant();

        if (All.Contains(normalized, StringComparer.Ordinal))
        {
            return normalized;
        }

        throw new ArgumentException($"Unknown diagnostic session scenario '{scenario}'.", nameof(scenario));
    }

    internal static bool NeedsPreview(string scenario)
        => scenario is PreviewOnly or Flashback or FlashbackPlayback or FlashbackStress or FlashbackScrubStress or
            FlashbackRestartCycle or FlashbackEncoderCycle or FlashbackExportPlayback or FlashbackSegmentPlayback or
            FlashbackRangeExport or FlashbackRangeExportAudioSwitch or FlashbackLifecycle or FlashbackExportConcurrent or
            FlashbackDisableDuringExport or FlashbackRotatedExport or FlashbackPreviewCycle or FlashbackPlaybackPreviewCycle or
            FlashbackRecording or FlashbackRecordingPreviewCycle or FlashbackRecordingSettingsDeferred or
            FlashbackRecordingExportRejected or Combined;

    internal static bool NeedsRecording(string scenario)
        => scenario is RecordingOnly or FlashbackRecording or FlashbackRecordingPreviewCycle or
            FlashbackRecordingSettingsDeferred or FlashbackRecordingExportRejected or Combined;

    internal static bool NeedsFlashback(string scenario)
        => scenario is Flashback or FlashbackPlayback or FlashbackStress or FlashbackScrubStress or
            FlashbackRestartCycle or FlashbackEncoderCycle or FlashbackExportPlayback or FlashbackSegmentPlayback or
            FlashbackRangeExport or FlashbackRangeExportAudioSwitch or FlashbackLifecycle or FlashbackExportConcurrent or
            FlashbackDisableDuringExport or FlashbackRotatedExport or FlashbackPreviewCycle or FlashbackPlaybackPreviewCycle or
            FlashbackRecording or FlashbackRecordingPreviewCycle or FlashbackRecordingSettingsDeferred or
            FlashbackRecordingExportRejected or Combined;

    internal static bool TryGetFlashbackExportVerificationPath(
        string scenario,
        string outputDirectory,
        out string exportPath)
    {
        exportPath = scenario switch
        {
            Flashback or FlashbackStress => Path.Combine(outputDirectory, "flashback-stress-export.mp4"),
            FlashbackRestartCycle => Path.Combine(outputDirectory, "flashback-restart-cycle-export.mp4"),
            FlashbackEncoderCycle => Path.Combine(outputDirectory, "flashback-encoder-cycle-export.mp4"),
            FlashbackExportPlayback => Path.Combine(outputDirectory, "flashback-export-playback.mp4"),
            FlashbackRangeExport => Path.Combine(outputDirectory, "flashback-range-export.mp4"),
            FlashbackRangeExportAudioSwitch => Path.Combine(outputDirectory, "flashback-range-export-audio-switch.mp4"),
            FlashbackExportConcurrent => Path.Combine(outputDirectory, "flashback-concurrent-a.mp4"),
            FlashbackDisableDuringExport => Path.Combine(outputDirectory, "flashback-disable-during-export.mp4"),
            FlashbackRotatedExport => Path.Combine(outputDirectory, "flashback-rotated-export.mp4"),
            FlashbackPreviewCycle => Path.Combine(outputDirectory, "flashback-preview-off-export.mp4"),
            FlashbackPlaybackPreviewCycle => Path.Combine(outputDirectory, "flashback-playback-preview-cycle.mp4"),
            _ => string.Empty
        };

        return exportPath.Length > 0;
    }
}
