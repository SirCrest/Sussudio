using System.Text.Json;
using static Sussudio.Tools.DiagnosticSessionAutomationResponseJson;

namespace Sussudio.Tools;

internal static class DiagnosticSessionRecordingVerification
{
    internal static async Task<JsonElement?> RunAsync(
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
