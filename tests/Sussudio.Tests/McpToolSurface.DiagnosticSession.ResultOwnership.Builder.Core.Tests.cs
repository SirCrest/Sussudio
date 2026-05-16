static partial class Program
{
    private static void AssertDiagnosticSessionResultBuilderCoreOwnership()
    {
        var builderText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.cs")
            .Replace("\r\n", "\n");
        var resultText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Result.cs")
            .Replace("\r\n", "\n");
        var flatteningText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Flattening.cs")
            .Replace("\r\n", "\n");
        var compositionText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Composition.cs")
            .Replace("\r\n", "\n");
        var analysisText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Analysis.cs")
            .Replace("\r\n", "\n");
        var previewSchedulerText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.PreviewScheduler.cs")
            .Replace("\r\n", "\n");
        var modelsText = ReadRepoFile("tools/Common/DiagnosticSessionResultBuilder.Models.cs")
            .Replace("\r\n", "\n");

        AssertContains(builderText, "internal static partial class DiagnosticSessionResultBuilder");
        AssertContains(builderText, "internal static async Task<DiagnosticSessionResult> BuildAndWriteAsync(");
        AssertContains(resultText, "private static DiagnosticSessionResult CreateResult(");
        AssertContains(flatteningText, "private static DiagnosticSessionResult FlattenResultProjectionSet(");
        AssertContains(compositionText, "private static DiagnosticSessionResultProjectionSet BuildResultProjectionSet(");
        AssertContains(compositionText, "private readonly record struct DiagnosticSessionResultProjectionSet(");
        AssertContains(previewSchedulerText, "private static DiagnosticSessionPreviewSchedulerAnalysis BuildPreviewSchedulerAnalysis(");
        AssertContains(previewSchedulerText, "private readonly record struct DiagnosticSessionPreviewSchedulerAnalysis(");
        AssertContains(modelsText, "internal sealed record DiagnosticSessionResultBuildRequest(");
        AssertContains(builderText, "runState.SetStage(\"result-analysis\")");
        AssertContains(resultText, "return FlattenResultProjectionSet(");
        AssertContains(flatteningText, "return new DiagnosticSessionResult");
        AssertContains(resultText, "var resultProjections = BuildResultProjectionSet(request, runState, analysis);");
        AssertContains(analysisText, "var previewScheduler = BuildPreviewSchedulerAnalysis(initialSnapshot, lastSnapshot, samples);");
        AssertDoesNotContain(builderText, "private static DiagnosticSessionResult CreateResult(");
        AssertDoesNotContain(resultText, "return new DiagnosticSessionResult");
        AssertDoesNotContain(flatteningText, "private static DiagnosticSessionResultProjectionSet BuildResultProjectionSet(");
        AssertDoesNotContain(compositionText, "private static DiagnosticSessionResult FlattenResultProjectionSet(");
    }
}
