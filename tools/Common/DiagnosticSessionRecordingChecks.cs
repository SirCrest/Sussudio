using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionFlashbackRecordingSettingsScenarios;
using static Sussudio.Tools.DiagnosticSessionFlashbackValidation;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;

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

        var hasFlashbackExportVerificationPath = DiagnosticSessionScenarios.TryGetFlashbackExportVerificationPath(
            scenario,
            outputDirectory,
            out var flashbackExportVerificationPath);
        var shouldRunVerification =
            startedRecording ||
            (options.VerifyRecording && hasFlashbackExportVerificationPath);
        if (shouldRunVerification)
        {
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
                    verification = verificationElement.Clone();
                }
                else
                {
                    warnings.Add(AutomationSnapshotFormatter.Get(verificationResponse, "Message", "Verification did not return data."));
                }
            }
            catch (Exception ex)
            {
                recordTerminalException(ex, "recording-verification");
            }
        }
        else if (options.VerifyRecording)
        {
            actions.Add("recording verification skipped: scenario does not produce a recording or export artifact");
        }

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
}

internal readonly record struct DiagnosticSessionRecordingCheckResult(JsonElement? Verification);
