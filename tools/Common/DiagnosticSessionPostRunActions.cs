using System.Text.Json;
using Sussudio.Models;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;
using static Sussudio.Tools.DiagnosticSessionFlashbackRecordingSettingsScenarios;
using static Sussudio.Tools.DiagnosticSessionFlashbackValidation;

namespace Sussudio.Tools;

internal readonly record struct DiagnosticSessionCleanupResult(bool StoppedRecordingForVerification);

internal static class DiagnosticSessionCleanupActions
{
    internal static async Task<DiagnosticSessionCleanupResult> RunAsync(
        DiagnosticSessionOptions options,
        JsonElement initialSnapshot,
        bool startedRecording,
        bool startedPreview,
        bool enabledFlashback,
        bool disabledFlashback,
        bool startedFlashbackPlayback,
        List<string> actions,
        DiagnosticSessionCommandChannel commandChannel,
        Func<string, int, CancellationToken, Task> tryWaitWithTokenAsync,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException)
    {
        var stoppedRecordingForVerification = await StopRecordingForCleanupAsync(
                options,
                startedRecording,
                actions,
                commandChannel,
                tryWaitWithTokenAsync,
                setStage,
                recordTerminalException)
            .ConfigureAwait(false);

        if (!options.LeaveRunning)
        {
            await RestoreLiveFlashbackPlaybackAsync(
                    startedFlashbackPlayback,
                    actions,
                    commandChannel,
                    setStage,
                    recordTerminalException)
                .ConfigureAwait(false);
            await StopPreviewIfStartedAsync(
                    startedPreview,
                    initialSnapshot,
                    actions,
                    commandChannel,
                    setStage,
                    recordTerminalException)
                .ConfigureAwait(false);
            await RestoreFlashbackEnabledStateAsync(
                    enabledFlashback,
                    disabledFlashback,
                    initialSnapshot,
                    actions,
                    commandChannel,
                    setStage,
                    recordTerminalException)
                .ConfigureAwait(false);
        }

        return new DiagnosticSessionCleanupResult(stoppedRecordingForVerification);
    }

    private static CancellationTokenSource CreateCleanupCts(TimeSpan timeout)
        => new(timeout);

    private static async Task<bool> StopRecordingForCleanupAsync(
        DiagnosticSessionOptions options,
        bool startedRecording,
        List<string> actions,
        DiagnosticSessionCommandChannel commandChannel,
        Func<string, int, CancellationToken, Task> tryWaitWithTokenAsync,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException)
    {
        var shouldStopRecordingForVerification = startedRecording && options.VerifyRecording;
        if (!startedRecording || (!shouldStopRecordingForVerification && options.LeaveRunning))
        {
            return false;
        }

        try
        {
            setStage("cleanup-stop-recording");
            const int recordingCleanupTimeoutMs = 300_000;
            using var cleanupCts = CreateCleanupCts(TimeSpan.FromMilliseconds(recordingCleanupTimeoutMs));
            var stopResponse = await commandChannel.SendWithTokenAsync(
                    AutomationCommandKind.SetRecordingEnabled,
                    new Dictionary<string, object?> { ["enabled"] = false },
                    recordingCleanupTimeoutMs,
                    false,
                    cleanupCts.Token)
                .ConfigureAwait(false);
            actions.Add(shouldStopRecordingForVerification && options.LeaveRunning
                ? "recording stopped for verification"
                : "recording stopped");
            var stoppedRecordingForVerification = shouldStopRecordingForVerification &&
                                                  IsSuccess(stopResponse);
            if (IsSuccess(stopResponse))
            {
                await tryWaitWithTokenAsync("RecordingStopped", recordingCleanupTimeoutMs, cleanupCts.Token)
                    .ConfigureAwait(false);
            }

            return stoppedRecordingForVerification;
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, "cleanup-stop-recording");
            return false;
        }
    }

    private static async Task RestoreLiveFlashbackPlaybackAsync(
        bool startedFlashbackPlayback,
        List<string> actions,
        DiagnosticSessionCommandChannel commandChannel,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException)
    {
        if (!startedFlashbackPlayback)
        {
            return;
        }

        try
        {
            setStage("cleanup-go-live");
            using var cleanupCts = CreateCleanupCts(TimeSpan.FromSeconds(15));
            await commandChannel.SendWithTokenAsync(
                    AutomationCommandKind.FlashbackAction,
                    new Dictionary<string, object?> { ["action"] = "go-live" },
                    15_000,
                    false,
                    cleanupCts.Token)
                .ConfigureAwait(false);
            actions.Add("flashback playback returned live");
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, "cleanup-go-live");
        }
    }

    private static async Task StopPreviewIfStartedAsync(
        bool startedPreview,
        JsonElement initialSnapshot,
        List<string> actions,
        DiagnosticSessionCommandChannel commandChannel,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException)
    {
        if (!startedPreview || GetBool(initialSnapshot, "IsPreviewing"))
        {
            return;
        }

        try
        {
            setStage("cleanup-stop-preview");
            using var cleanupCts = CreateCleanupCts(TimeSpan.FromSeconds(15));
            await commandChannel.SendWithTokenAsync(
                    AutomationCommandKind.SetPreviewEnabled,
                    new Dictionary<string, object?> { ["enabled"] = false },
                    15_000,
                    false,
                    cleanupCts.Token)
                .ConfigureAwait(false);
            actions.Add("preview stopped");
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, "cleanup-stop-preview");
        }
    }

    private static async Task RestoreFlashbackEnabledStateAsync(
        bool enabledFlashback,
        bool disabledFlashback,
        JsonElement initialSnapshot,
        List<string> actions,
        DiagnosticSessionCommandChannel commandChannel,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException)
    {
        if (enabledFlashback && !GetBool(initialSnapshot, "FlashbackActive"))
        {
            try
            {
                setStage("cleanup-restore-flashback-off");
                var cleanupTimeoutMs = AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.SetFlashbackEnabled);
                using var cleanupCts = CreateCleanupCts(TimeSpan.FromMilliseconds(cleanupTimeoutMs));
                await commandChannel.SendWithTokenAsync(
                        AutomationCommandKind.SetFlashbackEnabled,
                        new Dictionary<string, object?> { ["enabled"] = false },
                        cleanupTimeoutMs,
                        false,
                        cleanupCts.Token)
                    .ConfigureAwait(false);
                actions.Add("flashback restored off");
            }
            catch (Exception ex)
            {
                recordTerminalException(ex, "cleanup-restore-flashback-off");
            }
        }

        if (disabledFlashback && GetBool(initialSnapshot, "FlashbackActive"))
        {
            try
            {
                setStage("cleanup-restore-flashback-on");
                var cleanupTimeoutMs = AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.SetFlashbackEnabled);
                using var cleanupCts = CreateCleanupCts(TimeSpan.FromMilliseconds(cleanupTimeoutMs));
                await commandChannel.SendWithTokenAsync(
                        AutomationCommandKind.SetFlashbackEnabled,
                        new Dictionary<string, object?> { ["enabled"] = true },
                        cleanupTimeoutMs,
                        false,
                        cleanupCts.Token)
                    .ConfigureAwait(false);
                actions.Add("flashback restored on");
            }
            catch (Exception ex)
            {
                recordTerminalException(ex, "cleanup-restore-flashback-on");
            }
        }
    }
}

internal static class DiagnosticSessionRecordingChecks
{
    internal static async Task<DiagnosticSessionRecordingCheckResult> RunAsync(
        DiagnosticSessionOptions options,
        DiagnosticSessionScenarioPlan scenarioPlan,
        string scenario,
        string outputDirectory,
        JsonElement initialSnapshot,
        IReadOnlyList<DiagnosticSessionSample> samples,
        bool startedRecording,
        FlashbackRecordingSettingsDeferredPresetState flashbackRecordingSettingsDeferredPresetState,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException,
        CancellationToken cancellationToken)
    {
        var verification = default(JsonElement?);

        if (scenarioPlan.RunFlashbackRecordingSettingsDeferred)
        {
            try
            {
                setStage("settings-deferred-restore");
                await VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(
                        actions,
                        warnings,
                        flashbackRecordingSettingsDeferredPresetState,
                        sendAsync,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                recordTerminalException(ex, "settings-deferred-restore");
            }
        }

        verification = await RunRecordingVerificationAsync(
                options,
                scenario,
                outputDirectory,
                startedRecording,
                actions,
                warnings,
                sendAsync,
                setStage,
                recordTerminalException)
            .ConfigureAwait(false);

        if (scenarioPlan.RequiresFlashbackRecordingValidation)
        {
            try
            {
                setStage("recording-validation");
                ValidateFlashbackRecordingSession(initialSnapshot, samples, warnings);
            }
            catch (Exception ex)
            {
                recordTerminalException(ex, "recording-validation");
            }
        }

        return new DiagnosticSessionRecordingCheckResult(verification);
    }

    private static async Task<JsonElement?> RunRecordingVerificationAsync(
        DiagnosticSessionOptions options,
        string scenario,
        string outputDirectory,
        bool startedRecording,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendAsync,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException)
    {
        var hasFlashbackExportVerificationPath = DiagnosticSessionScenarioCatalog.TryGetFlashbackExportVerificationPath(
            scenario,
            outputDirectory,
            out var flashbackExportVerificationPath);
        var shouldRunVerification =
            startedRecording ||
            (options.VerifyRecording && hasFlashbackExportVerificationPath);
        if (!shouldRunVerification)
        {
            if (options.VerifyRecording)
            {
                actions.Add("recording verification skipped: scenario does not produce a recording or export artifact");
            }

            return null;
        }

        try
        {
            setStage("recording-verification");
            var verificationCommand = "VerifyLastRecording";
            Dictionary<string, object?>? verificationPayload = null;
            if (!startedRecording)
            {
                verificationCommand = "VerifyFile";
                verificationPayload = new Dictionary<string, object?>
                {
                    ["filePath"] = flashbackExportVerificationPath,
                    ["strict"] = true,
                    ["verificationProfile"] = "flashback-export"
                };
            }

            var verificationResponse = await sendAsync(verificationCommand, verificationPayload, 60_000).ConfigureAwait(false);
            if (TryGetVerification(verificationResponse, out var verificationElement))
            {
                return verificationElement.Clone();
            }

            warnings.Add(AutomationSnapshotFormatter.Get(verificationResponse, "Message", "Verification did not return data."));
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, "recording-verification");
        }

        return null;
    }
}

internal readonly record struct DiagnosticSessionRecordingCheckResult(JsonElement? Verification);
