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

    private static AvSyncFlattenedProjection BuildAvSyncFlattenedProjection(AvSyncProjection avSync)
        => new()
        {
            CaptureDriftMs = avSync.CaptureDriftMs,
            CaptureDriftRateMsPerSec = avSync.CaptureDriftRateMsPerSec,
            EncoderDriftMs = avSync.EncoderDriftMs,
            EncoderCorrectionSamples = avSync.EncoderCorrectionSamples
        };

    private readonly record struct AvSyncProjection
    {
        public double? CaptureDriftMs { get; init; }
        public double? CaptureDriftRateMsPerSec { get; init; }
        public double? EncoderDriftMs { get; init; }
        public long? EncoderCorrectionSamples { get; init; }
    }

    private readonly record struct AvSyncFlattenedProjection
    {
        public double? CaptureDriftMs { get; init; }
        public double? CaptureDriftRateMsPerSec { get; init; }
        public double? EncoderDriftMs { get; init; }
        public long? EncoderCorrectionSamples { get; init; }
    }
}
