namespace Sussudio.Tools;

public sealed class DiagnosticSessionOptions
{
    public string Scenario { get; init; } = "observe";
    public int DurationSeconds { get; init; } = 10;
    public int SampleIntervalMs { get; init; } = 1000;
    public string? OutputDirectory { get; init; }
    public bool IncludePresentMon { get; init; }
    public string? PresentMonPath { get; init; }
    public bool VerifyRecording { get; init; }
    public bool LeaveRunning { get; init; }
}
