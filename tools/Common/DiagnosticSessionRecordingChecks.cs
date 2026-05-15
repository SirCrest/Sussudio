using System.Text.Json;
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

        verification = await DiagnosticSessionRecordingVerification.RunAsync(
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
}

internal readonly record struct DiagnosticSessionRecordingCheckResult(JsonElement? Verification);
