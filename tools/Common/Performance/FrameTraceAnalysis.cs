using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sussudio.Tools.Performance;

// Result types of the frame-trace analyzer (docs/architecture/performance-telemetry.md sections 2.4 and 7). Property
// names are the JSON field names after the camel-case naming policy; times are milliseconds unless the name says
// otherwise. Every collection is non-null and every number is finite, so JSON and text always render.

/// <summary>Raised for input that is not a usable frame-trace dump: empty, not JSON, truncated, wrong shape, unreadable.</summary>
public sealed class FrameTraceFormatException : Exception
{
    public FrameTraceFormatException(string message)
        : base(message)
    {
    }

    public FrameTraceFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Knobs of the analysis. Defaults are what <c>ssctl perf trace analyze</c> uses.</summary>
public sealed class FrameTraceAnalysisOptions
{
    /// <summary>Slowest frames listed with their per-stage breakdown.</summary>
    public int TopFrames { get; init; } = 10;

    /// <summary>A present interval is a hitch when it exceeds the median interval times this factor.</summary>
    public double HitchFactor { get; init; } = 1.5;

    /// <summary>... and exceeds the median by at least this many milliseconds (keeps rounding noise out).</summary>
    public double HitchMinExcessMs { get; init; } = 2.0;

    /// <summary>Hitches listed with attributions, worst first. The count of all hitches is always reported.</summary>
    public int MaxHitches { get; init; } = 20;
}

/// <summary>How the trace was written and how arrival time was recovered.</summary>
public sealed record FrameTraceInfo(
    long ParsedEvents,
    long SkippedEvents,
    bool HasMetadata,
    double QpcFrequency,
    long OriginQpc,
    string? StartedUtc,
    long? Capacity,
    long? Recorded,
    long? Retained,
    long Overwritten,
    bool? Running,
    string ArrivalSource,
    int ArrivalFallbackFrames,
    double DurationMs);

/// <summary>
/// Frame totals. A frame is every event that shares one non-negative id and is not an ambient stage. A frame is
/// "missing stages" when a stage that most frames have is absent from it; such frames stay in every total.
/// </summary>
public sealed record FrameTraceFrameCounts(int Total, int Complete, int MissingStages, int WithDrop);

/// <summary>Per-stage durations over the frames that have the stage, and when the stage ended relative to arrival.</summary>
public sealed record TraceStageStats(
    string Stage,
    bool Aggregate,
    int Frames,
    int MissingFrames,
    double MeanMs,
    double P50Ms,
    double P90Ms,
    double P99Ms,
    double MaxMs,
    double EndLatencyP50Ms,
    double EndLatencyP99Ms);

/// <summary>A latency computed per frame from several stages (for example arrival to present).</summary>
public sealed record TraceDerivedStats(
    string Name,
    string Description,
    int Count,
    double MeanMs,
    double P50Ms,
    double P90Ms,
    double P99Ms,
    double MaxMs);

/// <summary>One named reason an interval was long: a stage that grew, a late arrival, or an overlapping event.</summary>
public sealed record TraceAttribution(string Kind, string Name, double Ms, string Detail);

public sealed record TraceHitch(
    long FrameId,
    long PreviousFrameId,
    double AtMs,
    double IntervalMs,
    double? ArrivalIntervalMs,
    string LikelyCause,
    IReadOnlyList<TraceAttribution> Attributions);

/// <summary>Cadence of the present stage: median interval, hitch threshold, and the worst hitches with causes.</summary>
public sealed record TracePresentCadence(
    string Stage,
    int Intervals,
    double MedianMs,
    double P99Ms,
    double MaxMs,
    double HitchThresholdMs,
    int HitchCount,
    IReadOnlyList<TraceHitch> Hitches);

public sealed record TraceFrameStage(string Stage, double StartMs, double DurationMs, double EndMs);

/// <summary>
/// One of the slowest frames. <see cref="TotalMs"/> is arrival to present when the frame has a present, otherwise
/// the frame's lifetime (arrival to its last event), so a dropped frame still ranks. Both are measured from the
/// frame's arrival, so a stage that ended by then (<see cref="PreArrivalStages"/>, normally capture.read, the wait for
/// the device to deliver the frame) is not part of that interval and is never named as <see cref="DominantStage"/>;
/// it stays in <see cref="Stages"/> with a negative start. <see cref="DominantStage"/> is the stage that spent the most
/// time inside the interval (a stage that straddles arrival counts only the part after it); aggregate stages never win.
/// For a presented frame the interval also ends at the present, so a stage that ended after it
/// (<see cref="AfterPresentStages"/>: the recording and Flashback encoders, which run on their own thread and cannot
/// delay a present) is not part of it either and is never named dominant.
/// </summary>
public sealed record TraceSlowFrame(
    long Id,
    double ArrivalMs,
    double TotalMs,
    string RankedBy,
    bool Dropped,
    IReadOnlyList<string> MissingStages,
    string? DominantStage,
    IReadOnlyList<TraceFrameStage> Stages,
    string? DropReason = null,
    IReadOnlyList<string>? PreArrivalStages = null,
    IReadOnlyList<string>? AfterPresentStages = null);

/// <summary>Events with no frame (GC, UI stall, automation command, markers): count and size per name.</summary>
public sealed record TraceAmbientStats(string Name, int Count, double TotalMs, double MaxMs);

/// <summary>How many drops of one kind carried a given reason (args.reason of the drop event).</summary>
public sealed record TraceDropReason(string Reason, int Count);

/// <summary>
/// Drop events by name. <see cref="Reasons"/> breaks <see cref="Count"/> down by the reason the dump names (the preview
/// renderer's reason); drops from a dump that predates reasons simply have none, so the reasons can sum to less than the count.
/// </summary>
public sealed record TraceDropStats(string Name, int Count, IReadOnlyList<TraceDropReason> Reasons);

public sealed class FrameTraceAnalysis
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    public FrameTraceAnalysis(
        FrameTraceInfo trace,
        FrameTraceFrameCounts frames,
        IReadOnlyList<TraceStageStats> stages,
        IReadOnlyList<TraceDerivedStats> derived,
        TracePresentCadence? presentCadence,
        IReadOnlyList<TraceSlowFrame> slowestFrames,
        IReadOnlyList<TraceAmbientStats> ambientEvents,
        IReadOnlyList<TraceDropStats> drops,
        IReadOnlyList<string> warnings)
    {
        Trace = trace;
        Frames = frames;
        Stages = stages;
        Derived = derived;
        PresentCadence = presentCadence;
        SlowestFrames = slowestFrames;
        AmbientEvents = ambientEvents;
        Drops = drops;
        Warnings = warnings;
    }

    public FrameTraceInfo Trace { get; }

    public FrameTraceFrameCounts Frames { get; }

    public IReadOnlyList<TraceStageStats> Stages { get; }

    public IReadOnlyList<TraceDerivedStats> Derived { get; }

    public TracePresentCadence? PresentCadence { get; }

    public IReadOnlyList<TraceSlowFrame> SlowestFrames { get; }

    public IReadOnlyList<TraceAmbientStats> AmbientEvents { get; }

    public IReadOnlyList<TraceDropStats> Drops { get; }

    public IReadOnlyList<string> Warnings { get; }

    /// <summary>The analysis as indented camel-case JSON, the same findings as the typed result.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>A plain-text report for a terminal: what was traced, where the time went, which frames hitched and why.</summary>
    public string FormatSummary() => FrameTraceAnalysisFormatter.Format(this);
}
