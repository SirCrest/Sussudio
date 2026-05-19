using System;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Read-only recording-integrity projection for runtime snapshots. Classification
// and counter capture stay in the RecordingIntegrity partials.
public partial class CaptureService
{
    private static RuntimeRecordingIntegritySnapshotFields CaptureRuntimeRecordingIntegritySnapshotFields(
        RecordingIntegritySummary recordingIntegrity)
    {
        return new RuntimeRecordingIntegritySnapshotFields
        {
            Status = recordingIntegrity.Status,
            Complete = recordingIntegrity.Complete,
            Backend = recordingIntegrity.Backend,
            CompletedUtc = recordingIntegrity.CompletedUtc,
            SourceFrames = recordingIntegrity.SourceFrames,
            AcceptedFrames = recordingIntegrity.AcceptedFrames,
            PipelineDroppedFrames = recordingIntegrity.PipelineDroppedFrames,
            QueueDroppedFrames = recordingIntegrity.QueueDroppedFrames,
            SubmittedFrames = recordingIntegrity.SubmittedFrames,
            EncodedFrames = recordingIntegrity.EncodedFrames,
            PacketsWritten = recordingIntegrity.PacketsWritten,
            EncoderDroppedFrames = recordingIntegrity.EncoderDroppedFrames,
            SequenceGaps = recordingIntegrity.SequenceGaps,
            QueueMaxDepth = recordingIntegrity.QueueMaxDepth,
            QueueOldestFrameAgeMs = recordingIntegrity.QueueOldestFrameAgeMs,
            BackpressureWaitMs = recordingIntegrity.BackpressureWaitMs,
            BackpressureEvents = recordingIntegrity.BackpressureEvents,
            BackpressureMaxWaitMs = recordingIntegrity.BackpressureMaxWaitMs,
            AudioStatus = recordingIntegrity.AudioStatus,
            AudioEnabled = recordingIntegrity.AudioEnabled,
            AudioCaptureActive = recordingIntegrity.AudioCaptureActive,
            AudioFramesArrived = recordingIntegrity.AudioFramesArrived,
            AudioFramesWrittenToSink = recordingIntegrity.AudioFramesWrittenToSink,
            AudioSamplesEncoded = recordingIntegrity.AudioSamplesEncoded,
            AudioDropEvents = recordingIntegrity.AudioDropEvents,
            AudioDiscontinuities = recordingIntegrity.AudioDiscontinuities,
            AudioTimestampErrors = recordingIntegrity.AudioTimestampErrors,
            AudioCallbackGaps = recordingIntegrity.AudioCallbackGaps,
            AvSyncDriftMs = recordingIntegrity.AvSyncDriftMs,
            AvSyncDriftRateMsPerSec = recordingIntegrity.AvSyncDriftRateMsPerSec,
            EncoderAvSyncDriftMs = recordingIntegrity.EncoderAvSyncDriftMs,
            EncoderAvSyncCorrectionSamples = recordingIntegrity.EncoderAvSyncCorrectionSamples,
            Reason = recordingIntegrity.Reason
        };
    }
}
