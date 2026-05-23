using System;

namespace Sussudio.Models;

public sealed partial class AutomationSnapshot
{
    public double AudioPeak { get; init; }
    public bool AudioClipping { get; init; }
    public bool AudioSignalPresent { get; init; }
    public bool AudioMutedSuspected { get; init; }
    public bool AudioReaderActive { get; init; }
    public long AudioFramesArrived { get; init; }
    public long AudioFramesWrittenToSink { get; init; }
    public bool VideoReaderActive { get; init; }
    public long IngestVideoFramesArrived { get; init; }
    public long IngestVideoFramesWrittenToSink { get; init; }
    public long IngestLastVideoFrameAgeMs { get; init; }
    public long VideoIngestErrorCount { get; init; }
    public long MfSourceReaderFramesDelivered { get; init; }
    public long MfSourceReaderFramesDropped { get; init; }
    public string? MfSourceReaderNegotiatedFormat { get; init; }
    public string MemoryPreference { get; init; } = "Cpu";
    public string VideoRequestedSubtype { get; init; } = "unknown";
    public string VideoNegotiatedSubtype { get; init; } = "unknown";
    public int FrameLedgerCapacity { get; init; }
    public long FrameLedgerEventCount { get; init; }
    public long FrameLedgerDroppedEventCount { get; init; }
    public FrameLedgerEventSnapshot[] FrameLedgerRecentEvents { get; init; } = Array.Empty<FrameLedgerEventSnapshot>();
    public long EncoderVideoFramesEnqueued { get; init; }
    public long EncoderVideoFramesEncoded { get; init; }
    public long EncoderLastEnqueueAgeMs { get; init; }
    public long EncoderLastWriteAgeMs { get; init; }

    // === Thread Health Probes ===
    // Source reader
    public bool SourceReaderReadOutstanding { get; init; }
    public long SourceReaderReadOutstandingMs { get; init; }
    public long SourceReaderLastFrameTickMs { get; init; }
    public int SourceReaderFrameChannelDepth { get; init; }

    // WASAPI capture
    public long WasapiCaptureCallbackCount { get; init; }
    public double WasapiCaptureCallbackAvgIntervalMs { get; init; }
    public double WasapiCaptureCallbackMaxIntervalMs { get; init; }
    public long WasapiCaptureCallbackSevereGapCount { get; init; }
    public long WasapiCaptureAudioDiscontinuityCount { get; init; }
    public long WasapiCaptureAudioTimestampErrorCount { get; init; }
    public long WasapiCaptureAudioGlitchCount { get; init; }
    public int WasapiCaptureCallbackSilenceCount { get; init; }
    public long WasapiCaptureLastCallbackTickMs { get; init; }
    public long WasapiCaptureAudioLevelEventsFired { get; init; }
    public long WasapiCaptureAudioLevelLastFireTickMs { get; init; }

    // WASAPI playback
    public long WasapiPlaybackRenderCallbackCount { get; init; }
    public int WasapiPlaybackRenderSilenceCount { get; init; }
    public int WasapiPlaybackQueueDepth { get; init; }
    public int WasapiPlaybackQueueDropCount { get; init; }
    public double WasapiPlaybackQueueDurationMs { get; init; }
    public double WasapiPlaybackActiveChunkDurationMs { get; init; }
    public double WasapiPlaybackEndpointQueuedDurationMs { get; init; }
    public double WasapiPlaybackBufferedDurationMs { get; init; }
    public double WasapiPlaybackStreamLatencyMs { get; init; }
    public long WasapiPlaybackLastRenderTickMs { get; init; }

    // === Memory & GC ===
    public double MemoryWorkingSetMb { get; init; }
    public double MemoryPrivateBytesMb { get; init; }
    public double MemoryManagedHeapMb { get; init; }
    public double MemoryTotalAllocatedMb { get; init; }
    public double ProcessCpuPercent { get; init; }
    public double ProcessCpuTotalProcessorTimeMs { get; init; }
    public double MemoryGcHeapSizeMb { get; init; }
    public int MemoryGcGen0Collections { get; init; }
    public int MemoryGcGen1Collections { get; init; }
    public int MemoryGcGen2Collections { get; init; }
    public double MemoryGcPauseTimePercent { get; init; }
    public double MemoryGcFragmentationPercent { get; init; }
    public int ThreadPoolWorkerAvailable { get; init; }
    public int ThreadPoolWorkerMax { get; init; }
    public int ThreadPoolIoAvailable { get; init; }
    public int ThreadPoolIoMax { get; init; }

    // === AV Sync ===
    public double? AvSyncCaptureDriftMs { get; init; }
    public double? AvSyncCaptureDriftRateMsPerSec { get; init; }
    public double? AvSyncEncoderDriftMs { get; init; }
    public long? AvSyncEncoderCorrectionSamples { get; init; }
}
