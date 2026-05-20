namespace Sussudio.Tools;

public sealed partial class DiagnosticSessionResult
{
    // Session summary and artifact paths.
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

    // End-of-run overview.
    public double ProcessCpuPercentAtEnd { get; init; }
    public double ProcessCpuMaxPercentObserved { get; init; }
    public bool RecordingVerificationRun { get; init; }
    public bool? RecordingVerificationSucceeded { get; init; }
    public string? RecordingVerificationMessage { get; init; }
    public PresentMonProbeResult? PresentMon { get; init; }

    // Capture/source summary.
    public string SelectedResolutionAtEnd { get; init; } = string.Empty;
    public double SelectedFrameRateAtEnd { get; init; }
    public string SelectedFriendlyFrameRateAtEnd { get; init; } = string.Empty;
    public string SelectedExactFrameRateArgAtEnd { get; init; } = string.Empty;
    public string SelectedVideoFormatAtEnd { get; init; } = string.Empty;
    public string VideoRequestedSubtypeAtEnd { get; init; } = string.Empty;
    public string VideoNegotiatedSubtypeAtEnd { get; init; } = string.Empty;
    public int SourceWidthAtEnd { get; init; }
    public int SourceHeightAtEnd { get; init; }
    public double DetectedSourceFrameRateAtEnd { get; init; }
    public string DetectedSourceFrameRateArgAtEnd { get; init; } = string.Empty;
    public bool SourceIsHdrAtEnd { get; init; }
    public string SourceTelemetrySummaryAtEnd { get; init; } = string.Empty;
}
