using System.Text.Json;

namespace Sussudio.Tools;

public sealed class DiagnosticSessionOptions
{
    internal const string DefaultScenario = DiagnosticSessionScenarioCatalog.Observe;
    internal const int DefaultDurationSeconds = 10;
    internal const int DefaultSampleIntervalMs = 1000;
    internal const string CliUsage =
        "diagnostic-session [--scenario " + DiagnosticSessionScenarioCatalog.HelpList + "] [--seconds N] [--sample-ms N] [--output PATH] [--presentmon] [--presentmon-path PATH] [--verify] [--leave-running] [--json]";

    public string Scenario { get; init; } = DefaultScenario;
    public int DurationSeconds { get; init; } = DefaultDurationSeconds;
    public int SampleIntervalMs { get; init; } = DefaultSampleIntervalMs;
    public string? OutputDirectory { get; init; }
    public bool IncludePresentMon { get; init; }
    public string? PresentMonPath { get; init; }
    public bool VerifyRecording { get; init; }
    public bool LeaveRunning { get; init; }
}

public sealed class DiagnosticSessionSample
{
    public long OffsetMs { get; init; }
    public DateTimeOffset TimestampUtc { get; init; }
    public JsonElement Snapshot { get; init; }
}

internal sealed class DiagnosticSessionScenarioPhaseContext
{
    internal required DiagnosticSessionOptions Options { get; init; }

    internal required string Scenario { get; init; }

    internal required DiagnosticSessionScenarioPlan ScenarioPlan { get; init; }

    internal required int DurationSeconds { get; init; }

    internal required int SampleIntervalMs { get; init; }

    internal required string OutputDirectory { get; init; }

    internal required JsonElement InitialSnapshot { get; init; }

    internal required bool InitialSnapshotKnown { get; init; }

    internal required List<string> Actions { get; init; }

    internal required List<string> Warnings { get; init; }

    internal required List<DiagnosticSessionSample> Samples { get; init; }

    internal required DiagnosticSessionCommandChannel CommandChannel { get; init; }

    internal required CancellationTokenSource ScenarioCancellationSource { get; init; }

    internal required CancellationToken ScenarioCancellationToken { get; init; }

    internal required CancellationToken RunCancellationToken { get; init; }

    internal required Action<string> SetStage { get; init; }

    internal required Func<string> GetLastStage { get; init; }

    internal required Action<Exception, string> RecordTerminalException { get; init; }

    internal required Func<Task> WriteLiveStateBestEffortAsync { get; init; }

    internal required Func<Task> WriteSamplingLiveStateBestEffortAsync { get; init; }
}

internal sealed class DiagnosticSessionScenarioPhaseState
{
    internal bool StartedPreview { get; set; }

    internal bool StartedRecording { get; set; }

    internal bool EnabledFlashback { get; set; }

    internal bool DisabledFlashback { get; set; }

    internal bool StartedFlashbackPlayback { get; set; }

    internal PresentMonProbeResult? PresentMon { get; set; }

    internal FlashbackRecordingSettingsDeferredPresetState FlashbackRecordingSettingsDeferredPresetState { get; set; }

    internal DiagnosticSessionScenarioPhaseResult ToResult()
        => new(
            StartedPreview,
            StartedRecording,
            EnabledFlashback,
            DisabledFlashback,
            StartedFlashbackPlayback,
            PresentMon,
            FlashbackRecordingSettingsDeferredPresetState);
}

internal sealed record DiagnosticSessionScenarioPhaseResult(
    bool StartedPreview,
    bool StartedRecording,
    bool EnabledFlashback,
    bool DisabledFlashback,
    bool StartedFlashbackPlayback,
    PresentMonProbeResult? PresentMon,
    FlashbackRecordingSettingsDeferredPresetState FlashbackRecordingSettingsDeferredPresetState)
{
    internal static readonly DiagnosticSessionScenarioPhaseResult Empty = new(
        StartedPreview: false,
        StartedRecording: false,
        EnabledFlashback: false,
        DisabledFlashback: false,
        StartedFlashbackPlayback: false,
        PresentMon: null,
        FlashbackRecordingSettingsDeferredPresetState: default);
}
