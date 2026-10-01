using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sussudio.Models;

/// <summary>
/// Owns the wording of the footer status line (<c>MainViewModel.StatusText</c>) and of the
/// <c>CaptureService.StatusChanged</c> events that feed it. Emitters must take their text from
/// here so one place defines how the app talks; a message written inline elsewhere will drift.
/// <para>
/// Style rules:
/// </para>
/// <list type="bullet">
/// <item>A status is a sentence-case fragment with no trailing period. The line is one row of
/// ellipsis-trimmed text, so keep it short and let the log carry the diagnostics.</item>
/// <item>Work in flight is a gerund ending in <c>...</c> (three ASCII dots). A steady or finished
/// state never ends in dots: <c>Recording</c>, <c>Preview started</c>, <c>Recording saved (00:12)</c>.</item>
/// <item>Failure is <c>{Subject} failed: {reason}</c>, built by <see cref="Failed"/>. The subject is
/// a noun phrase for the feature that failed. Do not write <c>Error: ...</c> or <c>Failed to ...</c>.</item>
/// <item>Cancellation is <c>{Subject} canceled</c> (US spelling).</item>
/// <item>A blocked action is an imperative: <c>Stop recording before changing capture settings</c>.</item>
/// <item>A consequence or next step follows a semicolon: <c>Audio unavailable; video preview is still running</c>.
/// Do not chain sentences.</item>
/// <item>A colon introduces a reason. Parentheses carry values (<c>Recording saved (00:12)</c>).</item>
/// <item>ASCII only. Status text is copied into automation JSON, <c>ssctl</c> console output and logs,
/// so no em dash and no ellipsis character.</item>
/// <item>Feature and product names keep their casing: Flashback, HDR, SDR, FFmpeg.</item>
/// </list>
/// <para>
/// Service result messages (<c>FinalizeResult.StatusMessage</c>, Flashback export results) are data
/// consumed by automation and diagnostics tools, not footer copy; they are shown through
/// <see cref="Detail"/> or <see cref="Failed"/> but are not reworded here.
/// </para>
/// </summary>
internal static class StatusMessages
{
    // Steady state and completion.
    public const string Ready = "Ready";
    public const string DeviceReady = "Device ready";
    public const string PreviewStarted = "Preview started";
    public const string PreviewStopped = "Preview stopped";
    public const string AudioPreviewStarted = "Audio preview started";
    public const string AudioPreviewStopped = "Audio preview stopped";
    public const string AudioPreviewUnavailable = "Audio preview unavailable";
    public const string AudioUnavailableVideoRunning = "Audio unavailable; video preview is still running";
    public const string AudioMonitoringUnavailableVideoRunning = "Audio monitoring unavailable; video preview is still running";
    public const string Recording = "Recording";
    public const string FlashbackRecovered = "Flashback recovered after an error";
    public const string FlashbackStoppedAfterRepeatedErrors = "Flashback stopped after repeated errors; use Restart Flashback to retry";

    // Work in flight.
    public const string ScanningForDevices = "Scanning for devices...";
    public const string InitializingDevice = "Initializing device...";
    public const string ApplyingCaptureSettings = "Applying capture settings...";
    public const string StartingRecording = "Starting recording...";
    public const string FinalizingRecording = "Finalizing recording...";
    public const string StoppingRecordingBeforeClose = "Stopping recording before close...";

    // Cancellation.
    public const string DeviceScanCanceled = "Device scan canceled";
    public const string DeviceInitializationCanceled = "Device initialization canceled";
    public const string RecordingStartCanceled = "Recording start canceled";
    public const string RecordingStopCanceled = "Recording stop canceled";
    public const string CloseCanceledRecordingStillSaving = "Still saving recording; close canceled";

    // Blocked actions and unmet preconditions.
    public const string NoDeviceSelected = "No device selected";
    public const string NoCompatibleDevicesFound = "No compatible video capture devices found (see log for details)";
    public const string StopRecordingBeforeSwitchingDevices = "Stop recording before switching capture devices";
    public const string StopRecordingBeforeChangingSettings = "Stop recording before changing capture settings";
    public const string StopRecordingBeforeSwitchingHdr = "Stop recording before switching between HDR and SDR pipelines";
    public const string WaitForSettingsBeforeRecording = "Wait for capture settings to finish applying before recording";
    public const string StartPreviewBeforeScreenshot = "Start preview before capturing a screenshot";
    public const string FlashbackNotActiveForExport = "Flashback export unavailable: Flashback is not active";
    public const string NoHdrResolutionAvailable = "No HDR-capable resolution available for this device";
    public const string HdrRecordingNeedsTenBitCodec = "HDR recording requires HEVC or AV1 (10-bit)";
    public const string SplitEncodeCheckInconclusive = "Split encode availability could not be checked; your selected mode is unchanged";
    public const string AnalogGainNotPersisted = "Analog audio gain applied but could not be saved to the device; it may revert after power cycle";
    public const string AudioShutdownTimedOut = "Audio shutdown timed out; Sussudio must close";

    // InfoBar copy. An InfoBar message is a full sentence and keeps its period, unlike the
    // footer fragments above; the other rules (ASCII, product-name casing) still apply.
    public const string FlashbackSnapToLiveNotice = "Returned to live after a playback error.";
    public const string FlashbackNotRunningNotice = "Flashback is not running. Use Restart Flashback to retry.";

    // Failures. The subject names the feature; Failed() joins the reason.
    public static string DeviceInitializationFailed(string? reason) => Failed("Device initialization", reason);
    public static string DeviceScanFailed(string? reason) => Failed("Device scan", reason);
    public static string PreviewFailed(string? reason) => Failed("Preview", reason);
    public static string RecordingFailed(string? reason) => Failed("Recording", reason);
    public static string CaptureFailed(string? reason) => Failed("Capture", reason);
    public static string CaptureSettingsUpdateFailed(string? reason) => Failed("Capture settings update", reason);
    public static string FolderSelectionFailed(string? reason) => Failed("Folder selection", reason);
    public static string ScreenshotFailed(string? reason) => Failed("Screenshot", reason);
    public static string ExportFailed(string? reason) => Failed("Export", reason);
    public static string SaveFailed(string? reason) => Failed("Save", reason);
    public static string FlashbackFailed(string? reason) => Failed("Flashback", reason);
    public static string FlashbackRestartFailed(string? reason) => Failed("Flashback restart", reason);
    public static string CloseFailed(string? reason) => Failed("Close", reason) + "; close again to retry";
    public static string SettingsSaveFailed(string? reason) => Failed("Settings save", reason) + "; changes may revert after restart";
    public static string RecordingStopFailedCloseCanceled(string? reason) => Failed("Recording stop", reason) + "; close canceled";

    // Parameterized states.
    public static string RecordingSaved(string elapsed) => $"Recording saved ({elapsed})";

    public static string StillFinalizingRecording(string stage)
        => $"Still finalizing recording ({stage.ToLowerInvariant()})...";

    public static string SelectedDevice(string deviceName) => $"Selected device: {deviceName}";

    public static string PreviewMode(uint width, uint height, double frameRate)
        => string.Create(
            CultureInfo.InvariantCulture,
            $"Preview: {width}x{height}@{frameRate:0.###}fps");

    public static string DevicesFound(int count, long elapsedMs, bool slowScan)
    {
        var noun = count == 1 ? "device" : "devices";
        var summary = string.Create(CultureInfo.InvariantCulture, $"Found {count} {noun} in {elapsedMs} ms");
        return slowScan ? summary + " (slow system enumeration)" : summary;
    }

    public static string NoHdrFrameRateAvailable(string resolution)
        => $"No HDR-capable frame rate available for {resolution}";

    public static string RecordingFormatUnavailable(string format)
        => $"Recording format '{format}' is unavailable in this FFmpeg runtime; choose another format";

    public static string SplitEncodeModeUnavailable(string mode)
        => $"Split encode mode '{mode}' is unavailable in this FFmpeg runtime; choose another mode";

    public static string ScreenshotSaved(string fileName) => $"Screenshot saved: {fileName}";

    public static string DeviceAudioModeSet(string mode) => $"Device audio mode set to {mode}";

    public static string DeviceAudioModeChangeFailed(string mode) => $"Device audio mode change failed ({mode})";

    public static string AnalogGainSet(double percent)
        => string.Create(CultureInfo.InvariantCulture, $"Analog audio gain set to {percent:0}%");

    public static string AnalogGainChangeFailed(double percent)
        => string.Create(CultureInfo.InvariantCulture, $"Analog audio gain change failed ({percent:0}%)");

    /// <summary>
    /// Formats <c>{subject} failed: {reason}</c>. A reason that already starts with
    /// <c>{subject} failed</c> is shown as is, so a service failure such as
    /// <c>Recording failed (...)</c> is not prefixed twice.
    /// </summary>
    public static string Failed(string subject, string? reason = null)
    {
        var prefix = subject + " failed";
        var text = Detail(reason);
        if (text.Length == 0)
        {
            return prefix;
        }

        return StartsWithFailure(text, prefix)
            ? text
            : prefix + ": " + text;
    }

    // Exact case and a word boundary: "Recording failed (...)" and "Recording failed during
    // finalization: ..." are already failure text, but a reason that merely reads
    // "device scan failed" in lowercase, or "Recording failedX", is not.
    private static bool StartsWithFailure(string text, string prefix)
        => text.StartsWith(prefix, StringComparison.Ordinal) &&
           (text.Length == prefix.Length || !char.IsLetterOrDigit(text[prefix.Length]));

    /// <summary>
    /// Failure text for a UI operation identified by its internal name (a button handler or a
    /// queued view-model operation). The internal name is a log token and never reaches the user.
    /// </summary>
    public static string OperationFailed(string operationName, string? reason)
        => Failed(DescribeOperation(operationName), reason);

    /// <summary>
    /// Makes an exception message or service message fit the status line: collapses whitespace
    /// and line breaks to single spaces and drops trailing periods. Returns an empty string when
    /// nothing readable remains.
    /// </summary>
    public static string Detail(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(raw.Length);
        var pendingSpace = false;
        foreach (var c in raw)
        {
            if (char.IsWhiteSpace(c))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(c);
        }

        return builder.ToString().TrimEnd('.');
    }

    private const string ViewModelPropertyChangedPrefix = "ViewModel_PropertyChanged:";

    private static readonly string[] HandlerSuffixes = { "_Click", "_Toggled", "_Loaded" };
    private static readonly string[] ControlSuffixes = { "Button", "Toggle" };

    // Subjects for operation names whose fallback wording would read badly (log banners, HDR/MJPEG
    // casing, several names for one feature). An unlisted name still gets a readable fallback.
    private static readonly Dictionary<string, string> OperationSubjects = new(StringComparer.Ordinal)
    {
        ["MainWindow_Loaded"] = "Startup",
        ["RecordButton_Click"] = "Recording",
        ["PreviewButton_Click"] = "Preview",
        ["RefreshButton_Click"] = "Device scan",
        ["ApplyDeviceButton_Click"] = "Device selection",
        ["BrowseButton_Click"] = "Folder selection",
        ["OpenRecordingsButton_Click"] = "Open recordings folder",
        ["ScreenshotButton_Click"] = "Screenshot",
        ["RecordingRecoveryOpenLocationButton_Click"] = "Open recording location",
        ["FlashbackExportButton_Click"] = "Export",
        ["FlashbackSaveLast5mButton_Click"] = "Save",
        ["FlashbackEnabledToggle_Toggled"] = "Flashback change",
        ["FlashbackApplyButton_Click"] = "Flashback restart",
        ["FlashbackHealthRestartButton_Click"] = "Flashback restart",
        ["PreviewStartupFailureStop"] = "Preview stop",
        ["format probe hdr retarget"] = "Capture settings update",
        ["format probe sdr retarget"] = "Capture settings update",
        ["format probe session mismatch"] = "Capture settings update",
        ["audio monitoring enable"] = "Audio monitoring change",
        ["audio monitoring mute"] = "Audio monitoring change",
        ["audio preview restart + flashback cycle"] = "Audio preview restart",
        ["custom audio toggle"] = "Audio input change",
        ["custom audio device change"] = "Audio input change",
        ["mic monitor toggle"] = "Microphone monitoring change",
        ["mic monitor device switch"] = "Microphone monitoring change",
        ["format change reinitialize"] = "Capture settings update",
        ["video format override reinitialize"] = "Capture settings update",
        ["mjpeg decoder count reinitialize"] = "Capture settings update",
        ["hdr toggle reinitialize"] = "Capture settings update",
        ["audio device invalidated reinit"] = "Audio device recovery",
        ["system resume reinit"] = "Capture recovery after resume",
        ["analog gain flash persist failed"] = "Analog gain save",
    };

    private static string DescribeOperation(string operationName)
    {
        if (OperationSubjects.TryGetValue(operationName, out var subject))
        {
            return subject;
        }

        if (operationName.StartsWith(ViewModelPropertyChangedPrefix, StringComparison.Ordinal))
        {
            return "UI update";
        }

        return Humanize(operationName);
    }

    // Fallback for an operation name with no entry above: "SomeButton_Click" -> "Some",
    // "device audio mode change" -> "Device audio mode change". Drops an event-handler suffix and a
    // trailing control word, splits words at lower-to-upper boundaries and separators, and leaves
    // acronyms as written.
    private static string Humanize(string operationName)
    {
        var name = operationName;
        foreach (var suffix in HandlerSuffixes)
        {
            if (name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal))
            {
                name = name[..^suffix.Length];
                break;
            }
        }

        foreach (var suffix in ControlSuffixes)
        {
            if (name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal))
            {
                name = name[..^suffix.Length];
                break;
            }
        }

        var builder = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c == '_' || c == ':' || c == ' ')
            {
                if (builder.Length > 0 && builder[^1] != ' ')
                {
                    builder.Append(' ');
                }

                continue;
            }

            var startsWord = i > 0 && char.IsUpper(c) && char.IsLower(name[i - 1]);
            if (startsWord && builder.Length > 0 && builder[^1] != ' ')
            {
                builder.Append(' ');
            }

            var wordFollowsLowercase = startsWord && i + 1 < name.Length && char.IsLower(name[i + 1]);
            builder.Append(builder.Length == 0
                ? char.ToUpperInvariant(c)
                : wordFollowsLowercase ? char.ToLowerInvariant(c) : c);
        }

        var text = builder.ToString().Trim();
        return text.Length == 0 ? "Operation" : text;
    }
}
