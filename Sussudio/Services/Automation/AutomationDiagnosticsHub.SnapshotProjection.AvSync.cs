using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static AvSyncProjection BuildAvSyncProjection(CaptureRuntimeSnapshot captureRuntime)
        => new()
        {
            CaptureDriftMs = captureRuntime.AvSyncCaptureDriftMs,
            CaptureDriftRateMsPerSec = captureRuntime.AvSyncCaptureDriftRateMsPerSec,
            EncoderDriftMs = captureRuntime.AvSyncEncoderDriftMs,
            EncoderCorrectionSamples = captureRuntime.AvSyncEncoderCorrectionSamples
        };

    private readonly record struct AvSyncProjection
    {
        public double? CaptureDriftMs { get; init; }
        public double? CaptureDriftRateMsPerSec { get; init; }
        public double? EncoderDriftMs { get; init; }
        public long? EncoderCorrectionSamples { get; init; }
    }
}
