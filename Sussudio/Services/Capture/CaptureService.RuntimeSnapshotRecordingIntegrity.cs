using System;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Read-only recording-integrity projection for runtime snapshots. Classification
// and counter capture stay in the RecordingIntegrity partials.
public partial class CaptureService
{
    private sealed class RuntimeRecordingIntegritySnapshotFields
    {
        public string Status { get; init; } = "NotStarted";
        public bool Complete { get; init; }
        public string Backend { get; init; } = "None";
        public DateTimeOffset? CompletedUtc { get; init; }
        public long SourceFrames { get; init; }
        public long AcceptedFrames { get; init; }
        public long PipelineDroppedFrames { get; init; }
        public long QueueDroppedFrames { get; init; }
        public long SubmittedFrames { get; init; }
        public long EncodedFrames { get; init; }
        public long PacketsWritten { get; init; }
        public long EncoderDroppedFrames { get; init; }
        public long SequenceGaps { get; init; }
        public int QueueMaxDepth { get; init; }
        public long QueueOldestFrameAgeMs { get; init; }
        public long BackpressureWaitMs { get; init; }
        public long BackpressureEvents { get; init; }
        public long BackpressureMaxWaitMs { get; init; }
        public string AudioStatus { get; init; } = "Disabled";
        public bool AudioEnabled { get; init; }
        public bool AudioCaptureActive { get; init; }
        public long AudioFramesArrived { get; init; }
        public long AudioFramesWrittenToSink { get; init; }
        public long AudioSamplesEncoded { get; init; }
        public long AudioDropEvents { get; init; }
        public long AudioDiscontinuities { get; init; }
        public long AudioTimestampErrors { get; init; }
        public long AudioCallbackGaps { get; init; }
        public double? AvSyncDriftMs { get; init; }
        public double? AvSyncDriftRateMsPerSec { get; init; }
        public double? EncoderAvSyncDriftMs { get; init; }
        public long? EncoderAvSyncCorrectionSamples { get; init; }
        public string Reason { get; init; } = "No recording has completed.";
    }

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
