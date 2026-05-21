using System.Text.Json;

namespace Sussudio.Tools;

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
