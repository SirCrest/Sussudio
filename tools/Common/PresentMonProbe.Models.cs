namespace Sussudio.Tools;

// Inputs for a short PresentMon capture. App-side present IDs and timestamps
// are optional correlation anchors used to connect Sussudio renderer telemetry
// with the OS-level present stream.
public sealed class PresentMonProbeOptions
{
    public int? ProcessId { get; init; }
    public string ProcessName { get; init; } = "Sussudio";
    public int DurationSeconds { get; init; } = 10;
    public string? PresentMonPath { get; init; }
    public string? OutputFile { get; init; }
    public string? ExpectedSwapChainAddress { get; init; }
    public long? AppPresentId { get; init; }
    public long? AppSourceSequenceNumber { get; init; }
    public long? AppPresentUtcUnixMs { get; init; }
    public long? CaptureStartUtcUnixMs { get; init; }
    public bool KeepCsv { get; init; }
    public bool TrackGpuVideo { get; init; } = true;
}

// Raw process result plus parsed presentation metrics. Callers should inspect
// Success/Message first, then Summary when PresentMon produced usable CSV.
public sealed class PresentMonProbeResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? PresentMonPath { get; init; }
    public int? TargetProcessId { get; init; }
    public string? TargetProcessName { get; init; }
    public string? CsvPath { get; init; }
    public int? ExitCode { get; init; }
    public bool TimedOut { get; init; }
    public string CommandLine { get; init; } = string.Empty;
    public string StdOut { get; init; } = string.Empty;
    public string StdErr { get; init; } = string.Empty;
    public PresentMonCaptureSummary? Summary { get; init; }
}

// Aggregated view of one PresentMon run. The selected swap chain is either the
// expected renderer address from the app snapshot or the best non-artifact
// candidate when the address is unknown.
public sealed class PresentMonCaptureSummary
{
    public int SampleCount { get; init; }
    public int RawSampleCount { get; init; }
    public int ExcludedSampleCount { get; init; }
    public string? ExpectedSwapChainAddress { get; init; }
    public string? SelectedSwapChainAddress { get; init; }
    public bool ExpectedSwapChainMatched { get; init; }
    public PresentMonMetricSummary BetweenPresentsMs { get; init; } = new();
    public PresentMonMetricSummary BetweenDisplayChangeMs { get; init; } = new();
    public PresentMonMetricSummary DisplayedTimeMs { get; init; } = new();
    public PresentMonMetricSummary UntilDisplayedMs { get; init; } = new();
    public PresentMonMetricSummary InPresentApiMs { get; init; } = new();
    public PresentMonMetricSummary CpuBusyMs { get; init; } = new();
    public PresentMonMetricSummary GpuBusyMs { get; init; } = new();
    public PresentMonMetricSummary GpuTimeMs { get; init; } = new();
    public PresentMonMetricSummary DisplayLatencyMs { get; init; } = new();
    public int NotDisplayedFrameCount { get; init; }
    public double NotDisplayedFramePercent { get; init; }
    public IReadOnlyDictionary<string, int> PresentModes { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> PresentRuntimes { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> SyncIntervals { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> AllowsTearing { get; init; } = new Dictionary<string, int>();
    public IReadOnlyList<PresentMonSwapChainSummary> SwapChains { get; init; } = Array.Empty<PresentMonSwapChainSummary>();
    public PresentMonAppCorrelation AppCorrelation { get; init; } = new();
    public bool DisplayedTimeColumnPresent { get; init; }
    public int DisplayChangeUnavailableCount { get; init; }
    public double DisplayChangeUnavailablePercent { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

public sealed class PresentMonAppCorrelation
{
    public bool Available { get; init; }
    public string Reason { get; init; } = "No app present timestamp was supplied.";
    public long AppPresentId { get; init; }
    public long AppSourceSequenceNumber { get; init; }
    public long AppPresentUtcUnixMs { get; init; }
    public double AppPresentOffsetMs { get; init; }
    public int PresentMonRowIndex { get; init; } = -1;
    public double PresentMonCpuStartTimeMs { get; init; }
    public double DeltaMs { get; init; }
    public string Outcome { get; init; } = "Unknown";
    public string PresentMode { get; init; } = string.Empty;
    public double? UntilDisplayedMs { get; init; }
    public double? DisplayLatencyMs { get; init; }
}

public sealed class PresentMonSwapChainSummary
{
    public string Address { get; init; } = string.Empty;
    public int SampleCount { get; init; }
    public bool Selected { get; init; }
    public bool Artifact { get; init; }
    public PresentMonMetricSummary BetweenPresentsMs { get; init; } = new();
    public PresentMonMetricSummary BetweenDisplayChangeMs { get; init; } = new();
    public PresentMonMetricSummary UntilDisplayedMs { get; init; } = new();
    public IReadOnlyDictionary<string, int> PresentModes { get; init; } = new Dictionary<string, int>();
}

public sealed class PresentMonMetricSummary
{
    public int SampleCount { get; init; }
    public double Average { get; init; }
    public double P50 { get; init; }
    public double P95 { get; init; }
    public double P99 { get; init; }
    public double Max { get; init; }
}
