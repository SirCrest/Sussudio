namespace Sussudio.Tools;

internal static partial class DiagnosticSessionScenarioCatalog
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
    internal const string HelpList =
        Observe + "|" + PreviewOnly + "|" + RecordingOnly + "|" + Flashback + "|" + FlashbackPlayback + "|" + FlashbackStress + "|" + FlashbackScrubStress + "|" + FlashbackRestartCycle + "|" + FlashbackEncoderCycle + "|" + FlashbackExportPlayback + "|" + FlashbackSegmentPlayback + "|" + FlashbackRangeExport + "|" + FlashbackRangeExportAudioSwitch + "|" + FlashbackLifecycle + "|" + FlashbackExportConcurrent + "|" + FlashbackDisableDuringExport + "|" + FlashbackRotatedExport + "|" + FlashbackPreviewCycle + "|" + FlashbackPlaybackPreviewCycle + "|" + FlashbackRecording + "|" + FlashbackRecordingPreviewCycle + "|" + FlashbackRecordingSettingsDeferred + "|" + FlashbackRecordingExportRejected + "|" + FlashbackExportRejected + "|" + Combined;
    internal const string Description =
        "Session scenario: observe, preview-only, recording-only, flashback, flashback-playback, flashback-stress, flashback-scrub-stress, flashback-restart-cycle, flashback-encoder-cycle, flashback-export-playback, flashback-segment-playback, flashback-range-export, flashback-range-export-audio-switch, flashback-lifecycle, flashback-export-concurrent, flashback-disable-during-export, flashback-rotated-export, flashback-preview-cycle, flashback-playback-preview-cycle, flashback-recording, flashback-recording-preview-cycle, flashback-recording-settings-deferred, flashback-recording-export-rejected, flashback-export-rejected, or combined.";

    internal static IReadOnlyList<string> Names => Entries.Select(static entry => entry.Name).ToArray();
}
