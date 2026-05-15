using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionResultBuilder
{
    private readonly record struct DiagnosticSessionOverviewResultProjection(
        bool Success,
        double ProcessCpuPercentAtEnd,
        double ProcessCpuMaxPercentObserved,
        bool RecordingVerificationRun,
        bool? RecordingVerificationSucceeded,
        string? RecordingVerificationMessage,
        PresentMonProbeResult? PresentMon);

    private static DiagnosticSessionOverviewResultProjection BuildOverviewResultProjection(
        DiagnosticSessionResultBuildRequest request,
        DiagnosticSessionRunState runState,
        DiagnosticSessionResultAnalysis analysis)
    {
        var lastSnapshot = analysis.LastSnapshot;
        var verificationSucceeded = request.Verification.HasValue
            ? GetBool(request.Verification.Value, "Succeeded")
            : (bool?)null;

        return new DiagnosticSessionOverviewResultProjection(
            Success: DetermineDiagnosticSessionSuccess(request, runState, analysis, verificationSucceeded),
            ProcessCpuPercentAtEnd: GetDouble(lastSnapshot, "ProcessCpuPercent"),
            ProcessCpuMaxPercentObserved: analysis.ProcessCpuMaxPercentObserved,
            RecordingVerificationRun: request.Verification.HasValue,
            RecordingVerificationSucceeded: verificationSucceeded,
            RecordingVerificationMessage: request.Verification.HasValue
                ? GetString(request.Verification.Value, "Message") ?? string.Empty
                : null,
            PresentMon: request.PresentMon);
    }

    private static bool DetermineDiagnosticSessionSuccess(
        DiagnosticSessionResultBuildRequest request,
        DiagnosticSessionRunState runState,
        DiagnosticSessionResultAnalysis analysis,
        bool? verificationSucceeded) =>
        request.CommandFailureCount == 0 &&
        runState.TerminalException is null &&
        analysis.DiagnosticHealthSucceeded &&
        (request.PresentMon is null || request.PresentMon.Success) &&
        (!verificationSucceeded.HasValue || verificationSucceeded.Value) &&
        analysis.FlashbackWarningsSucceeded;
}
