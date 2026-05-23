using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;
using static Sussudio.Tools.DiagnosticSessionFlashbackRecordingSettingsScenarios;
using static Sussudio.Tools.DiagnosticSessionFlashbackValidation;

namespace Sussudio.Tools;

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
