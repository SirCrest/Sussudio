static partial class Program
{
    private static void AssertDiagnosticSessionResultBuilderCoreOwnership()
    {
        var builderText = ReadDiagnosticSessionResultBuilderSource();

        AssertContains(builderText, "internal static partial class DiagnosticSessionResultBuilder");
        AssertContains(builderText, "internal static async Task<DiagnosticSessionResult> BuildAndWriteAsync(");
        AssertContains(builderText, "private static DiagnosticSessionResult CreateResult(");
        AssertContains(builderText, "private static DiagnosticSessionPreviewSchedulerAnalysis BuildPreviewSchedulerAnalysis(");
        AssertContains(builderText, "private readonly record struct DiagnosticSessionPreviewSchedulerAnalysis(");
        AssertContains(builderText, "internal sealed record DiagnosticSessionResultBuildRequest(");
        AssertContains(builderText, "runState.SetStage(\"result-analysis\")");
        AssertContains(builderText, "var result = new DiagnosticSessionResult");
        AssertContains(builderText, "var previewScheduler = BuildPreviewSchedulerAnalysis(initialSnapshot, lastSnapshot, samples);");
    }
}
