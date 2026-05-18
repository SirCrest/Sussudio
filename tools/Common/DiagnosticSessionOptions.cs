namespace Sussudio.Tools;

public sealed class DiagnosticSessionOptions
{
    internal const string DefaultScenario = DiagnosticSessionScenarios.Observe;
    internal const int DefaultDurationSeconds = 10;
    internal const int DefaultSampleIntervalMs = 1000;
    internal const string CliUsage =
        "diagnostic-session [--scenario " + DiagnosticSessionScenarios.HelpList + "] [--seconds N] [--sample-ms N] [--output PATH] [--presentmon] [--presentmon-path PATH] [--verify] [--leave-running] [--json]";

    public string Scenario { get; init; } = DefaultScenario;
    public int DurationSeconds { get; init; } = DefaultDurationSeconds;
    public int SampleIntervalMs { get; init; } = DefaultSampleIntervalMs;
    public string? OutputDirectory { get; init; }
    public bool IncludePresentMon { get; init; }
    public string? PresentMonPath { get; init; }
    public bool VerifyRecording { get; init; }
    public bool LeaveRunning { get; init; }
}
