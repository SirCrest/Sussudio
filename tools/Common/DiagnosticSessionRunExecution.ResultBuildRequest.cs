using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionRunExecution
{
    private static DiagnosticSessionResultBuildRequest CreateResultBuildRequest(
        DiagnosticSessionOptions options,
        DiagnosticSessionRunBootstrap runBootstrap,
        string livePath,
        int commandFailureCount,
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement initialSnapshot,
        DiagnosticSessionPostRunSnapshotResult postRunSnapshots,
        JsonElement? verification,
        PresentMonProbeResult? presentMon,
        bool startedPreview,
        bool enabledFlashback,
        bool startedFlashbackPlayback,
        bool stoppedRecordingForVerification,
        IReadOnlyList<string> actions,
        List<string> warnings)
    {
        return new DiagnosticSessionResultBuildRequest(
            options,
            runBootstrap.ScenarioPlan,
            runBootstrap.SessionId,
            runBootstrap.Scenario,
            runBootstrap.DurationSeconds,
            runBootstrap.SampleIntervalMs,
            runBootstrap.OutputDirectory,
            livePath,
            runBootstrap.StartedUtc,
            runBootstrap.RunnerProcessId,
            commandFailureCount,
            samples,
            initialSnapshot,
            postRunSnapshots.HealthSnapshot,
            postRunSnapshots.Timeline,
            verification,
            presentMon,
            startedPreview,
            enabledFlashback,
            startedFlashbackPlayback,
            stoppedRecordingForVerification,
            actions,
            warnings);
    }
}
