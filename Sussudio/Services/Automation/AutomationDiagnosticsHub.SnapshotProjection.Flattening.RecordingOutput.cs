using System;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static RecordingOutputFlattenedProjection BuildRecordingOutputFlattenedProjection(
        RecordingBackendProjection recordingBackend,
        RecordingOutputProjection recordingOutput)
        => new()
        {
            Backend = recordingBackend.Backend,
            AudioPathMode = recordingBackend.AudioPathMode,
            MuxResult = recordingBackend.MuxResult,
            OutputPath = recordingOutput.OutputPath,
            RecordingTime = recordingOutput.RecordingTime,
            RecordingSizeInfo = recordingOutput.RecordingSizeInfo,
            RecordingBitrateInfo = recordingOutput.RecordingBitrateInfo,
            RecordingVideoBytes = recordingOutput.RecordingVideoBytes,
            RecordingAudioBytes = recordingOutput.RecordingAudioBytes,
            RecordingTotalBytes = recordingOutput.RecordingTotalBytes,
            RecordingFileGrowing = recordingOutput.RecordingFileGrowing,
            LastOutputPath = recordingOutput.LastOutputPath,
            LastFinalizeStatus = recordingOutput.LastFinalizeStatus,
            LastFinalizeUtc = recordingOutput.LastFinalizeUtc,
            LastOutputExists = recordingOutput.LastOutputExists,
            LastOutputSizeBytes = recordingOutput.LastOutputSizeBytes,
            LastVerification = recordingOutput.LastVerification
        };

    private readonly record struct RecordingOutputFlattenedProjection
    {
        public string Backend { get; init; }
        public string AudioPathMode { get; init; }
        public string MuxResult { get; init; }
        public string OutputPath { get; init; }
        public string RecordingTime { get; init; }
        public string RecordingSizeInfo { get; init; }
        public string RecordingBitrateInfo { get; init; }
        public long RecordingVideoBytes { get; init; }
        public long RecordingAudioBytes { get; init; }
        public long RecordingTotalBytes { get; init; }
        public bool RecordingFileGrowing { get; init; }
        public string? LastOutputPath { get; init; }
        public string LastFinalizeStatus { get; init; }
        public DateTimeOffset? LastFinalizeUtc { get; init; }
        public bool LastOutputExists { get; init; }
        public long? LastOutputSizeBytes { get; init; }
        public RecordingVerificationResult? LastVerification { get; init; }
    }
}
