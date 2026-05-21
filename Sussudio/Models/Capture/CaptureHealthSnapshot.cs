using System;
using System.Collections.Generic;

namespace Sussudio.Models;

/// <summary>
/// Full health snapshot extending <see cref="CaptureDiagnosticsSnapshot"/> with
/// flashback playback/encoder detail, source signal metadata, and AV-sync data.
/// </summary>
public sealed partial class CaptureHealthSnapshot : CaptureDiagnosticsSnapshot
{
    // ── Source signal extended metadata ─────────────────────────────────
    public string? SourceVideoFormat { get; init; }
    public string? SourceColorimetry { get; init; }
    public string? SourceQuantization { get; init; }
    public string? SourceHdrTransferFunction { get; init; }
    public int? SourceHdrTransferCode { get; init; }
    public string? SourceFirmware { get; init; }
    public string? SourceAudioFormat { get; init; }
    public string? SourceAudioSampleRate { get; init; }
    public string? SourceInputSource { get; init; }
    public string? SourceUsbHostProtocol { get; init; }
    public string? SourceHdcpMode { get; init; }
    public string? SourceHdcpVersion { get; init; }
    public string? SourceRxTxHdcpVersion { get; init; }
    public string? SourceRawTimingHex { get; init; }
    public IReadOnlyList<SourceTelemetryDetailEntry> SourceTelemetryDetails { get; init; } = Array.Empty<SourceTelemetryDetailEntry>();

    // ── Queue age / enqueue tracking ───────────────────────────────────
    public long LastVideoEnqueueAgeMs { get; init; }
    public long LastVideoWriteAgeMs { get; init; }

    // ── AV Sync diagnostics ────────────────────────────────────────────
    public double? AvSyncCaptureDriftMs { get; init; }
    public double? AvSyncCaptureDriftRateMsPerSec { get; init; }
    public double? AvSyncEncoderDriftMs { get; init; }
    public long? AvSyncEncoderCorrectionSamples { get; init; }
}
