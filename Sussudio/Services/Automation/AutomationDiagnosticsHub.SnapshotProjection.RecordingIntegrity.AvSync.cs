using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingIntegrityAvSyncProjection BuildRecordingIntegrityAvSyncProjection(
        CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            AvSyncDriftMs = captureRuntime.RecordingIntegrityAvSyncDriftMs,
            AvSyncDriftRateMsPerSec = captureRuntime.RecordingIntegrityAvSyncDriftRateMsPerSec,
            EncoderAvSyncDriftMs = captureRuntime.RecordingIntegrityEncoderAvSyncDriftMs,
            EncoderAvSyncCorrectionSamples = captureRuntime.RecordingIntegrityEncoderAvSyncCorrectionSamples
        };

    private readonly record struct RecordingIntegrityAvSyncProjection
    {
        public double? AvSyncDriftMs { get; init; }
        public double? AvSyncDriftRateMsPerSec { get; init; }
        public double? EncoderAvSyncDriftMs { get; init; }
        public long? EncoderAvSyncCorrectionSamples { get; init; }
    }
}
