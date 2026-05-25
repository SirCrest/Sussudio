using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;
using static Sussudio.Tools.DiagnosticSessionJsonArtifacts;
using static Sussudio.Tools.DiagnosticSessionResultArtifacts;

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

        var completedUtc = DateTimeOffset.UtcNow;
        var terminalState = runState.GetTerminalState();
        runState.SetStage("summary");

        var result = CreateResult(
            request,
            runState,
            analysis,
            artifactPaths,
            completedUtc,
            terminalState);

        return await WriteSummaryAsync(result, runState, warnings).ConfigureAwait(false);
    }

    private static async Task<DiagnosticSessionResult> WriteSummaryAsync(
        DiagnosticSessionResult result,
        DiagnosticSessionRunState runState,
        List<string> warnings)
    {
        var summaryWritten = false;
        try
        {
            await WriteJsonAsync(result.SummaryPath, result, CancellationToken.None).ConfigureAwait(false);
            summaryWritten = true;
        }
        catch (Exception ex)
        {
            runState.RecordTerminalException(ex, "summary-write");
            result.Success = false;
            result.CompletedUtc = DateTimeOffset.UtcNow;
            result.TerminalState = runState.GetTerminalState();
            result.LastStage = runState.GetResultLastStage();
            result.UnhandledException = runState.TerminalException is null ? null : DiagnosticSessionRunState.FormatTerminalException(runState.TerminalException);
            result.Warnings = warnings.ToArray();
        }

        if (summaryWritten)
        {
            runState.SetStage("summary-written");
        }

        return result;
    }

    private static DiagnosticSessionResult CreateResult(
        DiagnosticSessionResultBuildRequest request,
        DiagnosticSessionRunState runState,
        DiagnosticSessionResultAnalysis analysis,
        DiagnosticSessionResultArtifactPaths artifactPaths,
        DateTimeOffset completedUtc,
        string terminalState)
    {
        var resultProjections = BuildResultProjectionSet(request, runState, analysis);

        return FlattenResultProjectionSet(
            request,
            runState,
            analysis,
            resultProjections,
            artifactPaths,
            completedUtc,
            terminalState);
    }

}

internal sealed record DiagnosticSessionResultBuildRequest(
    DiagnosticSessionOptions Options,
    DiagnosticSessionScenarioPlan ScenarioPlan,
    string SessionId,
    string Scenario,
    int DurationSeconds,
    int SampleIntervalMs,
    string OutputDirectory,
    string LivePath,
    DateTimeOffset StartedUtc,
    int RunnerProcessId,
    int CommandFailureCount,
    IReadOnlyList<DiagnosticSessionSample> Samples,
    JsonElement InitialSnapshot,
    JsonElement HealthSnapshot,
    JsonElement? Timeline,
    JsonElement? Verification,
    PresentMonProbeResult? PresentMon,
    bool StartedPreview,
    bool EnabledFlashback,
    bool StartedFlashbackPlayback,
    bool StoppedRecordingForVerification,
    IReadOnlyList<string> Actions,
    List<string> Warnings);

internal static class ToolJsonOptions
{
    internal static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true };
}

internal static class DiagnosticSessionResultArtifacts
{
    internal static async Task<DiagnosticSessionResultArtifactPaths> WritePreSummaryAsync(
        string outputDirectory,
        string sessionId,
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement? timeline,
        DiagnosticSessionRunState runState)
    {
        var paths = new DiagnosticSessionResultArtifactPaths(
            SummaryPath: Path.Combine(outputDirectory, "summary.json"),
            SamplesPath: Path.Combine(outputDirectory, "samples.json"),
            FrameLedgerPath: Path.Combine(outputDirectory, "frame-ledger.json"),
            TimelinePath: Path.Combine(outputDirectory, "timeline.json"));

        await runState.WriteArtifactBestEffortAsync("write-samples", paths.SamplesPath, samples).ConfigureAwait(false);
        await runState.WriteArtifactBestEffortAsync("write-frame-ledger", paths.FrameLedgerPath, BuildFrameLedgerTrace(sessionId, samples)).ConfigureAwait(false);
        await runState.WriteArtifactBestEffortAsync("write-timeline", paths.TimelinePath, timeline).ConfigureAwait(false);

        return paths;
    }

    private static object BuildFrameLedgerTrace(string sessionId, IReadOnlyList<DiagnosticSessionSample> samples)
    {
        var events = new List<JsonElement>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var sample in samples)
        {
            if (!sample.Snapshot.TryGetProperty("FrameLedgerRecentEvents", out var recentEvents) ||
                recentEvents.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var item in recentEvents.EnumerateArray())
            {
                var key =
                    $"{Get(item, "SourceSequence")}|{Get(item, "Stage")}|{Get(item, "QpcTimestamp")}";
                if (seen.Add(key))
                {
                    events.Add(item.Clone());
                }
            }
        }

        return new
        {
            SessionId = sessionId,
            SampleCount = samples.Count,
            EventCount = events.Count,
            Events = events
        };
    }
}

internal readonly record struct DiagnosticSessionResultArtifactPaths(
    string SummaryPath,
    string SamplesPath,
    string FrameLedgerPath,
    string TimelinePath);

internal static class DiagnosticSessionJsonArtifacts
{
    internal static JsonElement CreateEmptyJsonObject()
    {
        using var document = JsonDocument.Parse("{}");
        return document.RootElement.Clone();
    }

    internal static async Task WriteJsonAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(value, ToolJsonOptions.Pretty);
        await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
    }
}
