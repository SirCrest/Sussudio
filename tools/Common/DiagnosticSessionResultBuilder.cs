using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionResultArtifacts;
using static Sussudio.Tools.DiagnosticSessionSummaryWriter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    internal static async Task<DiagnosticSessionResult> BuildAndWriteAsync(
        DiagnosticSessionResultBuildRequest request,
        DiagnosticSessionRunState runState)
    {
        runState.SetStage("result-analysis");
        var analysis = Analyze(request);
        var samples = request.Samples;
        var warnings = request.Warnings;

        var artifactPaths = await WritePreSummaryAsync(
                request.OutputDirectory,
                request.SessionId,
                samples,
                request.Timeline,
                runState)
            .ConfigureAwait(false);

        var verificationSucceeded = request.Verification.HasValue
            ? GetBool(request.Verification.Value, "Succeeded")
            : (bool?)null;
        var completedUtc = DateTimeOffset.UtcNow;
        var terminalState = runState.GetTerminalState();
        runState.SetStage("summary");

        var result = CreateResult(
            request,
            runState,
            analysis,
            artifactPaths,
            verificationSucceeded,
            completedUtc,
            terminalState);

        return await WriteAsync(result, runState, warnings).ConfigureAwait(false);
    }
}
