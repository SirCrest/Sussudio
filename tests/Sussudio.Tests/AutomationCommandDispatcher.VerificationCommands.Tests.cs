using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationCommandDispatcher_VerificationCommands_LiveInFocusedPartial()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var verificationCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.VerificationCommands.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.VerifyFile:");
        AssertContains(customCommandsText, "ExecuteVerifyFileCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.VerifyLastRecording:");
        AssertContains(customCommandsText, "ExecuteVerifyLastRecordingCommandAsync(correlationId, cancellationToken)");
        AssertContains(verificationCommandsText, "private async Task<AutomationCommandResponse> ExecuteVerifyFileCommandAsync(");
        AssertContains(verificationCommandsText, "private async Task<AutomationCommandResponse> ExecuteVerifyLastRecordingCommandAsync(");
        AssertContains(verificationCommandsText, "ValidatePathPayload(\n            AutomationCommandKind.VerifyFile,\n            \"filePath\",");
        AssertContains(verificationCommandsText, "_diagnosticsHub\n            .VerifyFileAsync(filePath, verificationProfile, cancellationToken)");
        AssertContains(verificationCommandsText, "_diagnosticsHub.VerifyLastRecordingAsync(cancellationToken)");
        AssertContains(verificationCommandsText, "HdrParity = verification.HdrParity");
        AssertContains(verificationCommandsText, "errorCode: verification.Succeeded ? null : \"verification-failed\"");
        AssertDoesNotContain(customCommandsText, "_diagnosticsHub\n                    .VerifyFileAsync");
        AssertDoesNotContain(customCommandsText, "_diagnosticsHub.VerifyLastRecordingAsync");
        AssertDoesNotContain(customCommandsText, "HdrParity = verification.HdrParity");

        return Task.CompletedTask;
    }
}
