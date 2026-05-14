using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticSessionModels_AreSplitFromRunnerBehavior()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var modelText = ReadDiagnosticSessionModelsSource();

        AssertContains(modelText, "public sealed class DiagnosticSessionOptions");
        AssertContains(modelText, "public sealed class DiagnosticSessionResult");
        AssertContains(modelText, "public sealed class DiagnosticSessionSample");
        AssertContains(modelText, "public string TerminalState { get; set; }");
        AssertContains(modelText, "public JsonElement Snapshot { get; init; }");
        AssertContains(runnerText, "public static class DiagnosticSessionRunner");
        AssertContains(runnerText, "public static async Task<DiagnosticSessionResult> RunAsync(");
        AssertDoesNotContain(runnerText, "public sealed class DiagnosticSessionResult");
        AssertDoesNotContain(runnerText, "public sealed class DiagnosticSessionOptions");
        AssertDoesNotContain(runnerText, "public sealed class DiagnosticSessionSample");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionResultFormatter_OwnsFormattedSummaryText()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var formatterText = ReadDiagnosticSessionResultFormatterSource();

        AssertContains(formatterText, "public static partial class DiagnosticSessionResultFormatter");
        AssertContains(formatterText, "public static string Format(DiagnosticSessionResult result)");
        AssertContains(formatterText, "== Diagnostic Session:");
        AssertContains(formatterText, "private static void AppendOverview(");
        AssertContains(formatterText, "private static void AppendFlashbackSections(");
        AssertContains(formatterText, "private static void AppendPreviewSections(");
        AssertContains(formatterText, "private static void AppendArtifacts(");
        AssertContains(formatterText, "\"Flashback Playback Perf: \"");
        AssertContains(formatterText, "private static string FormatFrameRate(");
        AssertContains(runnerText, "return DiagnosticSessionResultFormatter.Format(result);");
        AssertDoesNotContain(runnerText, "== Diagnostic Session:");
        AssertDoesNotContain(runnerText, "\"Flashback Playback Perf: \"");
        AssertDoesNotContain(runnerText, "private static string FormatFrameRate(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionResultBuilder_OwnsSummaryConstruction()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var builderText = ReadDiagnosticSessionResultBuilderSource();

        AssertContains(builderText, "internal static partial class DiagnosticSessionResultBuilder");
        AssertContains(builderText, "internal static async Task<DiagnosticSessionResult> BuildAndWriteAsync(");
        AssertContains(builderText, "private static DiagnosticSessionResult CreateResult(");
        AssertContains(builderText, "internal sealed record DiagnosticSessionResultBuildRequest(");
        AssertContains(builderText, "runState.SetStage(\"result-analysis\")");
        AssertContains(builderText, "var result = new DiagnosticSessionResult");
        AssertContains(builderText, "var artifactPaths = await WritePreSummaryAsync(");
        AssertContains(builderText, "SummaryPath = artifactPaths.SummaryPath");
        AssertContains(builderText, "SamplesPath = artifactPaths.SamplesPath");
        AssertContains(builderText, "FrameLedgerPath = artifactPaths.FrameLedgerPath");
        AssertContains(builderText, "TimelinePath = artifactPaths.TimelinePath");
        AssertContains(builderText, "runState.SetStage(\"summary\")");
        AssertContains(builderText, "return await WriteAsync(result, runState, warnings).ConfigureAwait(false);");
        AssertContains(runnerText, "DiagnosticSessionResultBuilder.BuildAndWriteAsync(");
        AssertContains(runnerText, "new DiagnosticSessionResultBuildRequest(");
        AssertDoesNotContain(runnerText, "SetStage(\"result-analysis\")");
        AssertDoesNotContain(runnerText, "var result = new DiagnosticSessionResult");
        AssertDoesNotContain(runnerText, "WriteArtifactBestEffortAsync(\"write-samples\"");
        AssertDoesNotContain(runnerText, "RecordTerminalException(ex, \"summary-write\")");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionSummaryWriter_OwnsSummaryWriteFailures()
    {
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var writerText = ReadRepoFile("tools/Common/DiagnosticSessionSummaryWriter.cs")
            .Replace("\r\n", "\n");

        AssertContains(writerText, "internal static class DiagnosticSessionSummaryWriter");
        AssertContains(writerText, "internal static async Task<DiagnosticSessionResult> WriteAsync(");
        AssertContains(writerText, "await WriteJsonAsync(result.SummaryPath, result, CancellationToken.None)");
        AssertContains(writerText, "runState.RecordTerminalException(ex, \"summary-write\")");
        AssertContains(writerText, "result.Success = false;");
        AssertContains(writerText, "result.CompletedUtc = DateTimeOffset.UtcNow;");
        AssertContains(writerText, "result.TerminalState = runState.GetTerminalState();");
        AssertContains(writerText, "result.LastStage = runState.GetResultLastStage();");
        AssertContains(writerText, "result.Warnings = warnings.ToArray();");
        AssertContains(writerText, "runState.SetStage(\"summary-written\")");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionSummaryWriter;");
        AssertContains(builderText, "WriteAsync(result, runState, warnings)");
        AssertDoesNotContain(builderText, "RecordTerminalException(ex, \"summary-write\")");
        AssertDoesNotContain(builderText, "SetStage(\"summary-written\")");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionResultArtifacts_OwnPreSummaryWrites()
    {
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var artifactsText = ReadRepoFile("tools/Common/DiagnosticSessionResultArtifacts.cs")
            .Replace("\r\n", "\n");

        AssertContains(artifactsText, "internal static class DiagnosticSessionResultArtifacts");
        AssertContains(artifactsText, "internal static async Task<DiagnosticSessionResultArtifactPaths> WritePreSummaryAsync(");
        AssertContains(artifactsText, "internal readonly record struct DiagnosticSessionResultArtifactPaths(");
        AssertContains(artifactsText, "SummaryPath: Path.Combine(outputDirectory, \"summary.json\")");
        AssertContains(artifactsText, "SamplesPath: Path.Combine(outputDirectory, \"samples.json\")");
        AssertContains(artifactsText, "FrameLedgerPath: Path.Combine(outputDirectory, \"frame-ledger.json\")");
        AssertContains(artifactsText, "TimelinePath: Path.Combine(outputDirectory, \"timeline.json\")");
        AssertContains(artifactsText, "runState.WriteArtifactBestEffortAsync(\"write-samples\", paths.SamplesPath, samples)");
        AssertContains(artifactsText, "runState.WriteArtifactBestEffortAsync(\"write-frame-ledger\", paths.FrameLedgerPath, BuildFrameLedgerTrace(sessionId, samples))");
        AssertContains(artifactsText, "runState.WriteArtifactBestEffortAsync(\"write-timeline\", paths.TimelinePath, timeline)");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionResultArtifacts;");
        AssertContains(builderText, "WritePreSummaryAsync(");
        AssertDoesNotContain(builderText, "Path.Combine(request.OutputDirectory, \"samples.json\")");
        AssertDoesNotContain(builderText, "BuildFrameLedgerTrace(request.SessionId, samples)");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionText_OwnsSharedFormattingHelpers()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var formatterText = ReadDiagnosticSessionResultFormatterSource();
        var validationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.cs")
            .Replace("\r\n", "\n");
        var textHelpersText = ReadRepoFile("tools/Common/DiagnosticSessionText.cs")
            .Replace("\r\n", "\n");

        AssertContains(textHelpersText, "internal static class DiagnosticSessionText");
        AssertContains(textHelpersText, "internal static string FormatOptional(string value)");
        AssertContains(textHelpersText, "string.IsNullOrWhiteSpace(value) ? \"none\" : value");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionText;");
        AssertContains(formatterText, "using static Sussudio.Tools.DiagnosticSessionText;");
        AssertContains(validationText, "using static Sussudio.Tools.DiagnosticSessionText;");
        AssertDoesNotContain(runnerText, "private static string FormatOptional(");
        AssertDoesNotContain(formatterText, "private static string FormatOptional(");
        AssertDoesNotContain(validationText, "private static string FormatOptional(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionJsonArtifacts_OwnsArtifactsAndResponseExtraction()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var initialSnapshotText = ReadRepoFile("tools/Common/DiagnosticSessionInitialSnapshot.cs")
            .Replace("\r\n", "\n");
        var artifactsText = ReadRepoFile("tools/Common/DiagnosticSessionJsonArtifacts.cs")
            .Replace("\r\n", "\n");

        AssertContains(artifactsText, "internal static class DiagnosticSessionJsonArtifacts");
        AssertContains(artifactsText, "internal static JsonElement CreateEmptyJsonObject()");
        AssertContains(artifactsText, "internal static async Task WriteJsonAsync<T>(");
        AssertContains(artifactsText, "internal static object BuildFrameLedgerTrace(");
        AssertContains(artifactsText, "internal static bool TryGetSnapshot(");
        AssertContains(artifactsText, "internal static bool TryGetVerification(");
        AssertContains(initialSnapshotText, "using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;");
        AssertDoesNotContain(runnerText, "using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;");
        AssertDoesNotContain(runnerText, "private static async Task WriteJsonAsync<T>(");
        AssertDoesNotContain(runnerText, "private static bool TryGetSnapshot(");
        AssertDoesNotContain(runnerText, "private static bool TryGetVerification(");

        return Task.CompletedTask;
    }
}
