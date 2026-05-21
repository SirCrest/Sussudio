using System;

namespace Sussudio.Models;

/// <summary>
/// Core capture diagnostics shared by both the lightweight diagnostics snapshot
/// and the full health snapshot.  CaptureHealthSnapshot extends this with
/// flashback playback/encoder detail, source signal metadata, and AV-sync data.
/// </summary>
public partial class CaptureDiagnosticsSnapshot
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public CaptureSessionState SessionState { get; init; }
    public bool IsRecording { get; init; }
    public string RecordingBackend { get; init; } = "None";
    public string AudioPathMode { get; init; } = "None";
    public string MuxResult { get; init; } = "NotAttempted";
    public long RecordingElapsedMs { get; init; }
    public long LastFrameArrivalMs { get; init; }
    public long EstimatedPipelineLatencyMs { get; init; }
    public double ExpectedFrameRate { get; init; }
    public uint? NegotiatedWidth { get; init; }
    public uint? NegotiatedHeight { get; init; }
    public double? NegotiatedFrameRate { get; init; }
    public string? NegotiatedFrameRateArg { get; init; }
    public uint? NegotiatedFrameRateNumerator { get; init; }
    public uint? NegotiatedFrameRateDenominator { get; init; }
    public string? NegotiatedPixelFormat { get; init; }
    public string? RequestedReaderSubtype { get; init; }
    public string? ReaderSourceStreamType { get; init; }
    public string? ReaderSourceSubtype { get; init; }
    public string? FirstObservedFramePixelFormat { get; init; }
    public string? LatestObservedFramePixelFormat { get; init; }
    public long ObservedP010FrameCount { get; init; }
    public long ObservedNv12FrameCount { get; init; }
    public long ObservedOtherFrameCount { get; init; }
    public bool HdrAutoDowngraded { get; init; }
    public string HdrAutoDowngradeReason { get; init; } = string.Empty;
}
