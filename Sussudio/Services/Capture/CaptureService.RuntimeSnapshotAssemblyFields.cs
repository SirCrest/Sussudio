using System;
using System.Collections.Generic;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Private runtime snapshot assembly handoff contract consumed by the final DTO
// assembler after the focused projection partials sample their field groups.
public partial class CaptureService
{
    private sealed class CaptureRuntimeSnapshotAssemblyFields
    {
        public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
        public bool IsInitialized { get; init; }
        public bool IsRecording { get; init; }
        public bool IsAudioPreviewActive { get; init; }
        public CaptureSessionState SessionState { get; init; } = CaptureSessionState.Uninitialized;
        public RuntimeIngestAudioSnapshotFields IngestAudio { get; init; } = new();
        public RuntimeReaderTransportSnapshotFields ReaderTransport { get; init; } = new();
        public RuntimeHdrPipelineSnapshotFields HdrPipeline { get; init; } = new();
        public RuntimeHdrWarmupSnapshotFields HdrWarmup { get; init; } = new();
        public RuntimeSourceTelemetrySnapshotFields SourceTelemetry { get; init; } = new();
        public ObservedFrameSnapshotFields ObservedTelemetry { get; init; }
        public RuntimeRecordingIntegritySnapshotFields RecordingIntegrity { get; init; } = new();
        public string? CurrentDeviceId { get; init; }
        public string? CurrentDeviceName { get; init; }
        public string? ActiveAudioDeviceId { get; init; }
        public string? ActiveAudioDeviceName { get; init; }
        public CaptureSettings? RequestedSettings { get; init; }
        public string? RequestedFrameRateArg { get; init; }
        public uint? ActualWidth { get; init; }
        public uint? ActualHeight { get; init; }
        public double? ActualFrameRate { get; init; }
        public string? ActualFrameRateArg { get; init; }
        public uint? NegotiatedFrameRateNumerator { get; init; }
        public uint? NegotiatedFrameRateDenominator { get; init; }
        public string? NegotiatedPixelFormat { get; init; }
        public string RecordingBackend { get; init; } = "None";
        public string? LastOutputPath { get; init; }
        public string LastFinalizeStatus { get; init; } = "None";
        public DateTimeOffset? LastFinalizeUtc { get; init; }
        public IReadOnlyList<string> LastPreservedArtifacts { get; init; } = Array.Empty<string>();
        public string? FlashbackExportOutputPath { get; init; }
        public string? FlashbackExportVerificationFormat { get; init; }
        public string? FlashbackCodecDowngradeReason { get; init; }
        public double? RuntimeAvSyncDriftMs { get; init; }
        public double? RuntimeAvSyncDriftRateMsPerSec { get; init; }
        public double? RuntimeAvSyncEncoderDriftMs { get; init; }
        public long? RuntimeAvSyncEncoderCorrectionSamples { get; init; }
    }
}
