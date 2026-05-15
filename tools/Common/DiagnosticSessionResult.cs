namespace Sussudio.Tools;

public sealed partial class DiagnosticSessionResult
{
    public string SessionId { get; init; } = string.Empty;
    public string Scenario { get; init; } = "observe";
    public bool Success { get; set; }
    public DateTimeOffset StartedUtc { get; init; }
    public DateTimeOffset CompletedUtc { get; set; }
    public string TerminalState { get; set; } = "unknown";
    public string LastStage { get; set; } = string.Empty;
    public string? UnhandledException { get; set; }
    public int RunnerProcessId { get; init; }
    public int DurationSeconds { get; init; }
    public int SampleIntervalMs { get; init; }
    public int SampleCount { get; init; }
    public string OutputDirectory { get; init; } = string.Empty;
    public string LivePath { get; init; } = string.Empty;
    public string SummaryPath { get; init; } = string.Empty;
    public string SamplesPath { get; init; } = string.Empty;
    public string FrameLedgerPath { get; init; } = string.Empty;
    public string TimelinePath { get; init; } = string.Empty;
    public string HealthStatus { get; init; } = "Unknown";
    public string LikelyStage { get; init; } = "diagnostic_unavailable";
    public string Summary { get; init; } = string.Empty;
    public string Evidence { get; init; } = string.Empty;
    public string[] Actions { get; set; } = Array.Empty<string>();
    public string[] Warnings { get; set; } = Array.Empty<string>();
}
