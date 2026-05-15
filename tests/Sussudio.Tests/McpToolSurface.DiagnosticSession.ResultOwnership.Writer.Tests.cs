using System.Threading.Tasks;

static partial class Program
{
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
}
