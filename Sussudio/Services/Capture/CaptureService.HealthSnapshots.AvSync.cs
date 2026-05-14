namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private AvSyncHealthSnapshotFields CaptureAvSyncHealthSnapshotFields()
    {
        var (captureDriftMs, captureDriftRateMsPerSec) = ComputeAvSyncDrift();
        var (encoderDriftMs, encoderCorrectionSamples) = GetEncoderAvSyncDrift();

        return new AvSyncHealthSnapshotFields(
            captureDriftMs,
            captureDriftRateMsPerSec,
            encoderDriftMs,
            encoderCorrectionSamples);
    }

    private readonly record struct AvSyncHealthSnapshotFields(
        double? CaptureDriftMs,
        double? CaptureDriftRateMsPerSec,
        double? EncoderDriftMs,
        long? EncoderCorrectionSamples);
}
