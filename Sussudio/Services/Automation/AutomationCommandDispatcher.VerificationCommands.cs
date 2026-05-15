using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
    private async Task<AutomationCommandResponse> ExecuteVerifyFileCommandAsync(
        JsonElement payload,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var filePath = ValidatePathPayload(
            AutomationCommandKind.VerifyFile,
            "filePath",
            RequireString(payload, "filePath"));
        var verificationProfile = GetString(payload, "verificationProfile");
        var verifyStartedAt = Stopwatch.GetTimestamp();
        var verification = await _diagnosticsHub
            .VerifyFileAsync(filePath, verificationProfile, cancellationToken)
            .ConfigureAwait(false);
        var elapsedMs = (long)Math.Round(Stopwatch.GetElapsedTime(verifyStartedAt).TotalMilliseconds);
        return CreateVerificationResponse(correlationId, verification, elapsedMs);
    }

    private async Task<AutomationCommandResponse> ExecuteVerifyLastRecordingCommandAsync(
        string correlationId,
        CancellationToken cancellationToken)
    {
        var verifyStartedAt = Stopwatch.GetTimestamp();
        var verification = await _diagnosticsHub.VerifyLastRecordingAsync(cancellationToken).ConfigureAwait(false);
        var elapsedMs = (long)Math.Round(Stopwatch.GetElapsedTime(verifyStartedAt).TotalMilliseconds);
        return CreateVerificationResponse(correlationId, verification, elapsedMs);
    }

    private AutomationCommandResponse CreateVerificationResponse(
        string correlationId,
        RecordingVerificationResult verification,
        long elapsedMs)
    {
        return CreateResponse(
            correlationId,
            verification.Message,
            data: new
            {
                Verification = verification,
                HdrParity = verification.HdrParity
            },
            errorCode: verification.Succeeded ? null : "verification-failed",
            success: verification.Succeeded,
            status: verification.Succeeded ? AutomationResponseStatus.Ok : AutomationResponseStatus.Error,
            elapsedMs: elapsedMs);
    }
}
