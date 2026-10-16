using System.Text;

namespace Sussudio.Tools.Performance;

/// <summary>
/// Reads a frame-trace dump (<c>perf trace dump</c>, Chrome Trace Event JSON) and reports where frames spent their
/// time: per-stage latency from arrival, the slowest frames with a stage breakdown, present-cadence hitches with the
/// named cause (an overlapping GC pause or marker, the stage that grew most, or a late arrival), and ambient events.
/// Pure and offline: it needs no running app. Contract: docs/architecture/performance-telemetry.md sections 2.4 and 7.
/// </summary>
public static class FrameTraceAnalyzer
{
    /// <summary>Analyzes dump text. Throws <see cref="FrameTraceFormatException"/> for anything that is not a complete trace.</summary>
    public static FrameTraceAnalysis AnalyzeJson(string json, FrameTraceAnalysisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json), writable: false);
        return Analyze(stream, options);
    }

    /// <summary>Analyzes a dump file. A missing or unreadable file is a <see cref="FrameTraceFormatException"/> naming the path.</summary>
    public static FrameTraceAnalysis AnalyzeFile(string path, FrameTraceAnalysisOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        FileStream stream;
        try
        {
            stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1 << 16, FileOptions.SequentialScan);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            throw new FrameTraceFormatException($"Cannot read the frame trace '{path}': {ex.Message}", ex);
        }

        using (stream)
        {
            return Analyze(stream, options);
        }
    }

    public static FrameTraceAnalysis Analyze(Stream stream, FrameTraceAnalysisOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return FrameTraceAnalysisBuilder.Build(FrameTraceReader.Read(stream), options ?? new FrameTraceAnalysisOptions());
    }
}
