namespace Sussudio.Services.Performance;

// Wire DTOs of CaptureAudioOutput. Dependency-free on purpose (no WASAPI types): the probe that fills them is
// AudioOutputProbe, and the ssctl formatter contract test links this file to serialize real instances.
// Contract: docs/architecture/performance-telemetry.md section 2.9.

/// <summary>Counters from one probe run that explain what the capture saw (and how much of the tap was recorded).</summary>
public sealed record AudioProbeDiagnostics(
    int LoopbackPackets,
    long LoopbackFrames,
    double ActivationMs,
    double FirstPacketMs,
    int WaitTimeouts,
    bool BufferTruncated,
    long TapFrames,
    int TapPackets,
    long TapRejectedPackets);

/// <summary>
/// Response of CaptureAudioOutput. <see cref="Status"/> is <c>captured</c> when audible audio was captured (the analysis
/// verdict then says whether it had defects), otherwise a visible failure: <c>unsupported-os</c>, <c>tap-busy</c>,
/// <c>activation-failed</c>, <c>initialize-failed</c>, <c>capture-failed</c>, <c>no-audio-rendered</c> (the loopback
/// delivered nothing, or only silence) or <c>wav-write-failed</c>. The analysis (and WAV, when requested) is still
/// returned for <c>no-audio-rendered</c> and <c>wav-write-failed</c>.
/// </summary>
public sealed record AudioOutputProbeResult(
    string Status,
    string Message,
    double RequestedSeconds,
    double CapturedSeconds,
    int ProcessId,
    string Source,
    string? WavPath,
    long WavBytes,
    AudioAnalysis? Analysis,
    AudioLatencyResult? Latency,
    AudioProbeDiagnostics? Diagnostics,
    string? FailureStage,
    string? FailureHResult)
{
    public bool Captured => Status == "captured";
}
