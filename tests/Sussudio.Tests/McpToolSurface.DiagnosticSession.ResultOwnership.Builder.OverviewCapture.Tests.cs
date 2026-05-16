static partial class Program
{
    private static void AssertDiagnosticSessionResultBuilderOverviewAndCaptureProjectionOwnership()
    {
        var resultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Result.cs")
            .Replace("\r\n", "\n");
        var compositionText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Composition.cs")
            .Replace("\r\n", "\n");
        var overviewResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.OverviewResult.cs")
            .Replace("\r\n", "\n");
        var captureResultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.CaptureResult.cs")
            .Replace("\r\n", "\n");

        AssertContains(compositionText, "Overview: BuildOverviewResultProjection(request, runState, analysis)");
        AssertContains(resultText, "var overviewResult = resultProjections.Overview;");
        AssertContains(resultText, "Success = overviewResult.Success,");
        AssertContains(overviewResultText, "private readonly record struct DiagnosticSessionOverviewResultProjection(");
        AssertContains(overviewResultText, "private static DiagnosticSessionOverviewResultProjection BuildOverviewResultProjection(");
        AssertContains(overviewResultText, "var verificationSucceeded = request.Verification.HasValue");
        AssertContains(overviewResultText, "Success: DetermineDiagnosticSessionSuccess(request, runState, analysis, verificationSucceeded)");
        AssertContains(overviewResultText, "ProcessCpuPercentAtEnd: GetDouble(lastSnapshot, \"ProcessCpuPercent\")");
        AssertContains(overviewResultText, "ProcessCpuMaxPercentObserved: analysis.ProcessCpuMaxPercentObserved");
        AssertContains(overviewResultText, "RecordingVerificationMessage: request.Verification.HasValue");
        AssertContains(overviewResultText, "PresentMon: request.PresentMon");
        AssertContains(overviewResultText, "private static bool DetermineDiagnosticSessionSuccess(");
        AssertContains(overviewResultText, "request.CommandFailureCount == 0 &&");
        AssertContains(overviewResultText, "runState.TerminalException is null &&");
        AssertContains(overviewResultText, "analysis.DiagnosticHealthSucceeded &&");
        AssertContains(overviewResultText, "(request.PresentMon is null || request.PresentMon.Success) &&");
        AssertContains(overviewResultText, "(!verificationSucceeded.HasValue || verificationSucceeded.Value) &&");
        AssertContains(overviewResultText, "analysis.FlashbackWarningsSucceeded");
        AssertDoesNotContain(resultText, "request.CommandFailureCount == 0 &&");
        AssertDoesNotContain(resultText, "ProcessCpuPercentAtEnd = GetDouble(lastSnapshot");
        AssertDoesNotContain(resultText, "RecordingVerificationMessage = request.Verification.HasValue");
        AssertContains(compositionText, "Capture: BuildCaptureResultProjection(analysis)");
        AssertContains(resultText, "var captureResult = resultProjections.Capture;");
        AssertContains(captureResultText, "private readonly record struct DiagnosticSessionCaptureResultProjection(");
        AssertContains(captureResultText, "private static DiagnosticSessionCaptureResultProjection BuildCaptureResultProjection(");
        AssertContains(captureResultText, "SelectedResolutionAtEnd: GetString(lastSnapshot, \"SelectedResolution\") ?? string.Empty");
        AssertContains(captureResultText, "SourceWidthAtEnd: (int)(GetNullableLong(lastSnapshot, \"SourceWidth\") ?? 0)");
        AssertContains(captureResultText, "SourceTelemetrySummaryAtEnd: GetString(lastSnapshot, \"SourceTelemetrySummaryText\") ?? string.Empty");
        AssertDoesNotContain(resultText, "SelectedResolutionAtEnd = GetString(lastSnapshot");
        AssertDoesNotContain(resultText, "SourceWidthAtEnd = (int)(GetNullableLong");
        AssertDoesNotContain(resultText, "SourceTelemetrySummaryAtEnd = GetString(lastSnapshot");
    }
}
