using System.Collections.Generic;

namespace Sussudio.Services.Recording;

// Recording and verification failure identity, mirroring the pattern
// FlashbackExportFailureCodes already establishes for the export path.
// These strings reach ssctl and MCP clients through
// RecordingFinalizeFailureCode and the verification result, so they are a
// wire contract: change a value only alongside its consumers.
internal static class RecordingFailureCodes
{
    internal const string AudioCaptureFailed = "recording-audio-capture-failed";
    internal const string AudioDurationInvalid = "recording-audio-duration-invalid";
    internal const string AudioDurationMismatch = "recording-audio-duration-mismatch";
    internal const string AudioMetadataInvalid = "recording-audio-metadata-invalid";
    internal const string ContextMissing = "recording-context-missing";
    internal const string FinalOutputInvalid = "recording-final-output-invalid";
    internal const string FinalizationFailed = "recording-finalization-failed";
    internal const string FinalizationTimeout = "recording-finalization-timeout";
    internal const string FinalizationUnresolved = "recording-finalization-unresolved";
    internal const string FinalizationWorkerMissing = "recording-finalization-worker-missing";
    internal const string FlashbackEncodeDrainTimeout = "recording-flashback-encode-drain-timeout";
    internal const string FlashbackEncodeFailed = "recording-flashback-encode-failed";
    internal const string FlashbackFinalizationTimeout = "recording-flashback-finalization-timeout";
    internal const string HdrMetadataMismatch = "recording-hdr-metadata-mismatch";
    internal const string HdrValidationFailed = "recording-hdr-validation-failed";
    internal const string LibavFinalizationFailed = "recording-libav-finalization-failed";
    internal const string MicrophoneIntegrityFailed = "recording-microphone-integrity-failed";
    internal const string MuxFailed = "recording-mux-failed";
    internal const string NotGrowing = "recording-not-growing";
    internal const string OutputEmpty = "recording-output-empty";
    internal const string OutputMissing = "recording-output-missing";
    internal const string OutputPathEmpty = "recording-output-path-empty";
    internal const string OutputStatFailed = "recording-output-stat-failed";
    internal const string PacketAllocationFailed = "recording-packet-allocation-failed";
    internal const string ProgramAudioCleanupTimeout = "recording-program-audio-cleanup-timeout";
    internal const string ProgramAudioDisposeFailed = "recording-program-audio-dispose-failed";
    internal const string ProgramAudioIntegrityFailed = "recording-program-audio-integrity-failed";
    internal const string RecoveryRestored = "recording-recovery-restored";
    internal const string ReopenFailed = "recording-reopen-failed";
    internal const string RequiredStreamHasNoPackets = "recording-required-stream-has-no-packets";
    internal const string RuntimeFailed = "recording-runtime-failed";
    internal const string SinkDisposeFailed = "recording-sink-dispose-failed";
    internal const string SinkDisposedBeforeVerification = "recording-sink-disposed-before-verification";
    internal const string StopFailed = "recording-stop-failed";
    internal const string StreamCountInvalid = "recording-stream-count-invalid";
    internal const string StreamInfoFailed = "recording-stream-info-failed";
    internal const string StreamMetadataMissing = "recording-stream-metadata-missing";
    internal const string StreamTopologyMismatch = "recording-stream-topology-mismatch";
    internal const string StructureVerificationException = "recording-structure-verification-exception";
    internal const string StructureVerificationIncomplete = "recording-structure-verification-incomplete";
    internal const string UnifiedStopFailed = "recording-unified-stop-failed";
    internal const string VerificationContextMissing = "recording-verification-context-missing";
    internal const string VideoCaptureCleanupTimeout = "recording-video-capture-cleanup-timeout";
    internal const string VideoCaptureDisposeFailed = "recording-video-capture-dispose-failed";
    internal const string VideoCodecMismatch = "recording-video-codec-mismatch";
    internal const string VideoDimensionsMismatch = "recording-video-dimensions-mismatch";
    internal const string VideoDurationInvalid = "recording-video-duration-invalid";
    internal const string VideoDurationShort = "recording-video-duration-short";

    // Every declared code, for tests and diagnostics that need to prove the
    // registry and the emitting sites agree.
    internal static IReadOnlyCollection<string> All { get; } = new[]
    {
        AudioCaptureFailed,
        AudioDurationInvalid,
        AudioDurationMismatch,
        AudioMetadataInvalid,
        ContextMissing,
        FinalOutputInvalid,
        FinalizationFailed,
        FinalizationTimeout,
        FinalizationUnresolved,
        FinalizationWorkerMissing,
        FlashbackEncodeDrainTimeout,
        FlashbackEncodeFailed,
        FlashbackFinalizationTimeout,
        HdrMetadataMismatch,
        HdrValidationFailed,
        LibavFinalizationFailed,
        MicrophoneIntegrityFailed,
        MuxFailed,
        NotGrowing,
        OutputEmpty,
        OutputMissing,
        OutputPathEmpty,
        OutputStatFailed,
        PacketAllocationFailed,
        ProgramAudioCleanupTimeout,
        ProgramAudioDisposeFailed,
        ProgramAudioIntegrityFailed,
        RecoveryRestored,
        ReopenFailed,
        RequiredStreamHasNoPackets,
        RuntimeFailed,
        SinkDisposeFailed,
        SinkDisposedBeforeVerification,
        StopFailed,
        StreamCountInvalid,
        StreamInfoFailed,
        StreamMetadataMissing,
        StreamTopologyMismatch,
        StructureVerificationException,
        StructureVerificationIncomplete,
        UnifiedStopFailed,
        VerificationContextMissing,
        VideoCaptureCleanupTimeout,
        VideoCaptureDisposeFailed,
        VideoCodecMismatch,
        VideoDimensionsMismatch,
        VideoDurationInvalid,
        VideoDurationShort,
    };
}
