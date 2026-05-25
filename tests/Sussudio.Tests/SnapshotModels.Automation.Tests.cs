using System;
using Xunit;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    private static readonly string[] AutomationSnapshotCpuMjpegMetricProperties =
    {
        "MjpegDecoderCount",
        "MjpegReorderSampleCount",
        "MjpegPipelineSampleCount",
        "MjpegTotalDecoded",
        "MjpegTotalEmitted",
        "MjpegTotalDropped",
        "MjpegCompressedFramesQueued",
        "MjpegCompressedFramesDequeued",
        "MjpegCompressedDropsQueueFull",
        "MjpegCompressedDropsByteBudget",
        "MjpegCompressedDropsDisposed",
        "MjpegDecodeFailures",
        "MjpegReorderCollisions",
        "MjpegEmitFailures",
        "MjpegCompressedQueueDepth",
        "MjpegCompressedQueueBytes",
        "MjpegCompressedQueueByteBudget",
        "MjpegReorderSkips",
        "MjpegReorderBufferDepth",
    };

    private static readonly string[] MjpegDecoderAutomationSnapshotProperties =
    {
        "WorkerIndex",
        "SampleCount",
        "AvgMs",
        "P95Ms",
        "MaxMs",
    };

    private static void AssertAutomationSnapshotCpuMjpegMetricContract(Type snapshotType)
    {
        var automationSnapshotText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs");

        AssertContains(automationSnapshotText, "public int MjpegDecodeSampleCount { get; init; }");
        AssertContains(automationSnapshotText, "public int MjpegDecoderCount { get; init; }");
        AssertContains(automationSnapshotText, "public MjpegDecoderAutomationSnapshot[] MjpegPerDecoder { get; init; } = Array.Empty<MjpegDecoderAutomationSnapshot>();");
        AssertContains(automationSnapshotText, "public bool MjpegPreviewJitterEnabled { get; init; }");

        foreach (var propertyName in AutomationSnapshotCpuMjpegMetricProperties)
        {
            AssertNotNull(snapshotType.GetProperty(propertyName), $"AutomationSnapshot.{propertyName}");
        }

        var decoderType = RequireType("Sussudio.Models.MjpegDecoderAutomationSnapshot");
        var perDecoderProperty = snapshotType.GetProperty("MjpegPerDecoder")
            ?? throw new InvalidOperationException("AutomationSnapshot.MjpegPerDecoder missing.");
        var elementType = perDecoderProperty.PropertyType.GetElementType()
            ?? throw new InvalidOperationException("AutomationSnapshot.MjpegPerDecoder element type missing.");
        AssertEqual(decoderType, elementType, "AutomationSnapshot.MjpegPerDecoder[] element type");

        foreach (var propertyName in MjpegDecoderAutomationSnapshotProperties)
        {
            AssertNotNull(decoderType.GetProperty(propertyName), $"MjpegDecoderAutomationSnapshot.{propertyName}");
        }
    }

    private static void AssertAutomationSnapshotProperties(Type snapshotType, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            AssertNotNull(snapshotType.GetProperty(propertyName), $"AutomationSnapshot.{propertyName}");
        }
    }

    [Fact]
    public void AutomationSnapshot_ExposesCpuMjpegMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");

        AssertAutomationSnapshotCpuMjpegMetricContract(snapshotType);
    }

    [Fact]
    public void AutomationSnapshot_ExposesMjpegPreviewMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var automationSnapshotText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "MjpegPreviewJitterLastSelectedPreviewPresentId",
            "MjpegPreviewJitterLastSelectedSourceSequenceNumber",
            "MjpegPreviewJitterLastSelectedSourceLatencyMs",
            "MjpegPreviewJitterLastDroppedSourceSequenceNumber",
            "MjpegPreviewJitterClearedDropCount",
            "MjpegPreviewJitterResumeReprimeCount",
            "MjpegPreviewJitterLastDropReason",
            "MjpegPacketHashSampleCount",
            "MjpegPacketHashInputObservedFps",
            "MjpegPacketHashUniqueObservedFps",
            "MjpegPacketHashDuplicateFramePercent",
            "MjpegPacketHashPattern",
            "MjpegPacketHashRecentDuplicateFlags");
        AssertContains(automationSnapshotText, "public bool MjpegPreviewJitterEnabled { get; init; }");
        AssertContains(automationSnapshotText, "public string MjpegPreviewJitterLastDropReason { get; init; } = string.Empty;");
        AssertContains(automationSnapshotText, "public int MjpegPacketHashSampleCount { get; init; }");
        AssertContains(automationSnapshotText, "public int[] MjpegPacketHashRecentDuplicateFlags { get; init; } = Array.Empty<int>();");
        AssertContains(automationSnapshotText, "public int VisualCadenceSampleCount { get; init; }");
    }

    [Fact]
    public void AutomationSnapshot_ExposesPreviewDiagnosticsMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "PreviewD3DFrameLatencyWaitTimeoutCount",
            "PreviewD3DFrameLatencyWaitP95Ms",
            "PreviewD3DFrameLatencyWaitMaxMs",
            "PreviewD3DFrameStatsRecentMissedRefreshCount",
            "PreviewD3DFrameStatsRecentFailureCount",
            "PreviewD3DRenderThreadFailureCount",
            "PreviewD3DLastRenderThreadFailureType",
            "PreviewD3DLastRenderThreadFailureMessage",
            "PreviewD3DLastRenderThreadFailureHResult",
            "DiagnosticHealthStatus",
            "DiagnosticLikelyStage",
            "DiagnosticSummary",
            "DiagnosticEvidence",
            "DiagnosticSourceLane",
            "DiagnosticDecodeLane",
            "DiagnosticPreviewLane",
            "DiagnosticRenderLane",
            "DiagnosticPresentLane",
            "DiagnosticRecordingLane",
            "DiagnosticAudioLane",
            "PreviewPacingLikelySlowStage",
            "PreviewPacingSlowStageConfidence",
            "PreviewPacingSlowStageEvidence");
    }

    [Fact]
    public void AutomationSnapshot_ExposesCaptureCommandMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "CaptureCommandCommandsEnqueued",
            "CaptureCommandCommandsCompleted",
            "CaptureCommandCommandsFailed",
            "CaptureCommandCommandsCanceled",
            "CaptureCommandCommandsCoalesced",
            "CaptureCommandPendingCommands",
            "CaptureCommandMaxPendingCommands",
            "CaptureCommandOldestPendingCommandAgeMs",
            "CaptureCommandLastQueueLatencyMs",
            "CaptureCommandMaxQueueLatencyMs",
            "CaptureCommandLastCommand",
            "CaptureCommandLastOutcome",
            "CaptureCommandLastCorrelationId",
            "CaptureCommandLastError");
    }

    [Fact]
    public void AutomationSnapshot_ExposesCaptureCadenceMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var automationSnapshotText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "EstimatedPipelineLatencyMs",
            "ExpectedCaptureFrameRate",
            "CaptureCadenceSampleCount",
            "CaptureCadenceObservedFps",
            "CaptureCadenceP95IntervalMs",
            "CaptureCadenceP99IntervalMs",
            "CaptureCadenceOnePercentLowFps",
            "CaptureCadenceFivePercentLowFps",
            "CaptureCadenceRecentIntervalsMs",
            "CaptureCadenceEstimatedDroppedFrames",
            "CaptureCadenceEstimatedDropPercent");
        AssertContains(automationSnapshotText, "public long EstimatedPipelineLatencyMs { get; init; }");
        AssertContains(automationSnapshotText, "public double[] CaptureCadenceRecentIntervalsMs { get; init; } = Array.Empty<double>();");
        AssertContains(automationSnapshotText, "public int MjpegDecodeSampleCount { get; init; }");
    }

    [Fact]
    public void AutomationSnapshot_ExposesRecordingMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "RecordingVideoFramesSubmittedToEncoder",
            "RecordingVideoEncoderPts",
            "RecordingVideoEncoderPacketsWritten",
            "RecordingVideoEncoderDroppedFrames",
            "RecordingVideoSequenceGaps",
            "RecordingVideoQueueOldestFrameAgeMs",
            "RecordingVideoQueueLatencyP95Ms",
            "RecordingVideoQueueLatencyP99Ms",
            "RecordingVideoBackpressureWaitMs",
            "RecordingVideoBackpressureEvents");
    }

    [Fact]
    public void AutomationSnapshot_ExposesFlashbackRecordingMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var automationSnapshotText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "FlashbackTotalBytesWritten",
            "FlashbackTempDriveFreeBytes",
            "FlashbackStartupCacheBudgetBytes",
            "FlashbackStartupCacheBytes",
            "FlashbackStartupCacheSessionCount",
            "FlashbackStartupCacheDeletedSessionCount",
            "FlashbackStartupCacheFreedBytes",
            "FlashbackStartupCacheOverBudget",
            "FatalCleanupInProgress",
            "FlashbackCleanupInProgress",
            "FlashbackForceRotateActive",
            "FlashbackVideoFramesSubmittedToEncoder",
            "FlashbackVideoEncoderPacketsWritten",
            "FlashbackVideoSequenceGaps",
            "FlashbackBackendSettingsStale",
            "FlashbackBackendSettingsStaleReason",
            "FlashbackBackendActiveFormat",
            "FlashbackBackendRequestedFormat",
            "FlashbackBackendActivePreset",
            "FlashbackBackendRequestedPreset",
            "FlashbackVideoQueueOldestFrameAgeMs",
            "FlashbackVideoQueueLatencyP95Ms",
            "FlashbackVideoQueueLatencyP99Ms",
            "FlashbackVideoBackpressureWaitMs",
            "FlashbackVideoBackpressureEvents",
            "FlashbackAudioQueueCapacity",
            "FlashbackVideoQueueRejectedFrames",
            "FlashbackVideoQueueLastRejectReason",
            "FlashbackGpuQueueRejectedFrames",
            "FlashbackGpuQueueLastRejectReason");
        AssertContains(automationSnapshotText, "public bool FlashbackActive { get; init; }");
        AssertContains(automationSnapshotText, "public int FlashbackAudioQueueCapacity { get; init; }");
        AssertContains(automationSnapshotText, "public string FlashbackPlaybackState { get; init; }");
        AssertContains(automationSnapshotText, "public bool FlashbackExportActive { get; init; }");
        AssertContains(automationSnapshotText, "public bool FlashbackForceRotateActive { get; init; }");
        AssertContains(automationSnapshotText, "public long FlashbackVideoFramesSubmittedToEncoder { get; init; }");
    }

    [Fact]
    public void AutomationSnapshot_ExposesFlashbackPlaybackMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var automationSnapshotText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "FlashbackPlaybackThreadAlive",
            "FlashbackPlaybackDroppedFrames",
            "FlashbackPlaybackAudioMasterDelayDoubles",
            "FlashbackPlaybackAudioMasterDelayShrinks",
            "FlashbackPlaybackAudioMasterFallbacks",
            "FlashbackPlaybackSegmentSwitches",
            "FlashbackPlaybackFmp4Reopens",
            "FlashbackPlaybackWriteHeadWaits",
            "FlashbackPlaybackNearLiveSnaps",
            "FlashbackPlaybackDecodeErrorSnaps",
            "FlashbackPlaybackSubmitFailures",
            "FlashbackPlaybackLastDropUtcUnixMs",
            "FlashbackPlaybackLastDropReason",
            "FlashbackPlaybackLastSubmitFailureUtcUnixMs",
            "FlashbackPlaybackLastSubmitFailure",
            "FlashbackPlaybackLastSegmentSwitchUtcUnixMs",
            "FlashbackPlaybackLastFmp4ReopenUtcUnixMs",
            "FlashbackPlaybackLastWriteHeadWaitGapMs",
            "FlashbackPlaybackCadenceSampleCount",
            "FlashbackPlaybackP95FrameMs",
            "FlashbackPlaybackP99FrameMs",
            "FlashbackPlaybackMaxFrameMs",
            "FlashbackPlaybackSlowFrames",
            "FlashbackPlaybackSlowFramePercent",
            "FlashbackPlaybackTargetFps",
            "FlashbackPlaybackOnePercentLowFps",
            "FlashbackPlaybackPtsCadenceMismatchCount",
            "FlashbackPlaybackLastPtsCadenceDeltaMs",
            "FlashbackPlaybackLastPtsCadenceExpectedMs",
            "FlashbackPlaybackSeekForwardDecodeCapHits",
            "FlashbackPlaybackLastSeekHitForwardDecodeCap",
            "FlashbackPlaybackDecodeSampleCount",
            "FlashbackPlaybackDecodeAvgMs",
            "FlashbackPlaybackDecodeP95Ms",
            "FlashbackPlaybackDecodeP99Ms",
            "FlashbackPlaybackDecodeMaxMs",
            "FlashbackPlaybackMaxDecodePhase",
            "FlashbackPlaybackMaxDecodeReceiveMs",
            "FlashbackPlaybackMaxDecodeFeedMs",
            "FlashbackPlaybackMaxDecodeReadMs",
            "FlashbackPlaybackMaxDecodeSendMs",
            "FlashbackPlaybackMaxDecodeAudioMs",
            "FlashbackPlaybackMaxDecodeConvertMs",
            "FlashbackPlaybackMaxDecodeUtcUnixMs",
            "FlashbackPlaybackMaxDecodePositionMs",
            "CaptureCadenceP99IntervalMs",
            "CaptureCadenceOnePercentLowFps",
            "FlashbackPlaybackCommandsEnqueued",
            "FlashbackPlaybackCommandsProcessed",
            "FlashbackPlaybackCommandsDropped",
            "FlashbackPlaybackCommandsSkippedNotReady",
            "FlashbackPlaybackScrubUpdatesCoalesced",
            "FlashbackPlaybackSeekCommandsCoalesced",
            "FlashbackPlaybackCommandQueueCapacity",
            "FlashbackPlaybackPendingCommands",
            "FlashbackPlaybackMaxPendingCommands",
            "FlashbackPlaybackLastCommandQueueLatencyMs",
            "FlashbackPlaybackMaxCommandQueueLatencyMs",
            "FlashbackPlaybackMaxCommandQueueLatencyCommand",
            "FlashbackPlaybackLastCommandQueued",
            "FlashbackPlaybackLastCommandProcessed",
            "FlashbackPlaybackLastCommandQueuedUtcUnixMs",
            "FlashbackPlaybackLastCommandProcessedUtcUnixMs",
            "FlashbackPlaybackLastCommandFailureUtcUnixMs",
            "FlashbackPlaybackLastCommandFailure");
        AssertContains(automationSnapshotText, "public string FlashbackPlaybackState { get; init; } = \"N/A\";");
        AssertContains(automationSnapshotText, "public double[] FlashbackPlaybackRecentFrameIntervalsMs { get; init; } = Array.Empty<double>();");
        AssertContains(automationSnapshotText, "public string FlashbackPlaybackLastCommandFailure { get; init; } = string.Empty;");
        AssertContains(automationSnapshotText, "public bool FlashbackExportActive { get; init; }");
    }

    [Fact]
    public void AutomationSnapshot_ExposesFlashbackExportMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var automationSnapshotText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "FlashbackExportActive",
            "FlashbackExportId",
            "FlashbackExportStatus",
            "FlashbackExportOutputPath",
            "FlashbackExportStartedUtcUnixMs",
            "FlashbackExportLastProgressUtcUnixMs",
            "FlashbackExportCompletedUtcUnixMs",
            "FlashbackExportElapsedMs",
            "FlashbackExportLastProgressAgeMs",
            "FlashbackExportOutputBytes",
            "FlashbackExportThroughputBytesPerSec",
            "FlashbackExportSegmentsProcessed",
            "FlashbackExportTotalSegments",
            "FlashbackExportPercent",
            "FlashbackExportInPointMs",
            "FlashbackExportOutPointMs",
            "FlashbackExportMessage",
            "FlashbackExportFailureKind",
            "FlashbackExportForceRotateFallbacks",
            "FlashbackExportLastForceRotateFallbackUtcUnixMs",
            "FlashbackExportLastForceRotateFallbackSegments",
            "FlashbackExportLastForceRotateFallbackInPointMs",
            "FlashbackExportLastForceRotateFallbackOutPointMs",
            "LastExportId");
        AssertContains(automationSnapshotText, "public bool FlashbackExportActive { get; init; }");
        AssertContains(automationSnapshotText, "public string FlashbackExportStatus { get; init; } = \"NotStarted\";");
        AssertContains(automationSnapshotText, "public string? LastExportMessage { get; init; }");
        AssertContains(automationSnapshotText, "public string FlashbackPlaybackState { get; init; }");
    }

    [Fact]
    public void AutomationSnapshot_ExposesVisualCadenceMetrics()
    {
        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        var automationSnapshotText = ReadRepoFile("Sussudio/Models/Automation/AutomationSnapshot.cs");

        AssertAutomationSnapshotProperties(
            snapshotType,
            "VisualCadenceSampleCount",
            "VisualCadenceChangeObservedFps",
            "VisualCadenceRepeatFramePercent",
            "VisualCadenceMotionConfidence",
            "VisualCadenceRecentChangeIntervalsMs",
            "VisualCenterCadenceSampleCount",
            "VisualCenterCadenceChangeObservedFps",
            "VisualCenterCadenceRepeatFramePercent",
            "VisualCenterCadenceMotionConfidence",
            "VisualCenterCadenceRecentChangeIntervalsMs");
        AssertContains(automationSnapshotText, "public int VisualCadenceSampleCount { get; init; }");
        AssertContains(automationSnapshotText, "public double[] VisualCenterCadenceRecentChangeIntervalsMs { get; init; } = Array.Empty<double>();");
        AssertContains(automationSnapshotText, "public MjpegDecoderAutomationSnapshot[] MjpegPerDecoder");
    }

    [Fact]
    public void AutomationOptionsSnapshot_ExposesAdvancedControlState()
    {
        var optionsType = RequireType("Sussudio.Models.AutomationOptionsSnapshot");
        var stringOptionType = RequireType("Sussudio.Models.AutomationStringOption");
        var intOptionType = RequireType("Sussudio.Models.AutomationIntOption");

        AssertNotNull(optionsType.GetProperty("Presets"), "AutomationOptionsSnapshot.Presets");
        AssertNotNull(optionsType.GetProperty("SplitEncodeModes"), "AutomationOptionsSnapshot.SplitEncodeModes");
        AssertNotNull(optionsType.GetProperty("VideoFormats"), "AutomationOptionsSnapshot.VideoFormats");
        AssertNotNull(optionsType.GetProperty("MjpegDecoderCounts"), "AutomationOptionsSnapshot.MjpegDecoderCounts");
        AssertNotNull(optionsType.GetProperty("SelectedPreset"), "AutomationOptionsSnapshot.SelectedPreset");
        AssertNotNull(optionsType.GetProperty("SelectedSplitEncodeMode"), "AutomationOptionsSnapshot.SelectedSplitEncodeMode");
        AssertNotNull(optionsType.GetProperty("SelectedVideoFormat"), "AutomationOptionsSnapshot.SelectedVideoFormat");
        AssertNotNull(optionsType.GetProperty("PreviewVolumePercent"), "AutomationOptionsSnapshot.PreviewVolumePercent");
        AssertNotNull(optionsType.GetProperty("IsStatsVisible"), "AutomationOptionsSnapshot.IsStatsVisible");

        var presetsProperty = optionsType.GetProperty("Presets")
            ?? throw new InvalidOperationException("AutomationOptionsSnapshot.Presets missing.");
        AssertEqual(stringOptionType, presetsProperty.PropertyType.GetElementType(), "AutomationOptionsSnapshot.Presets[] element type");

        var decoderCountsProperty = optionsType.GetProperty("MjpegDecoderCounts")
            ?? throw new InvalidOperationException("AutomationOptionsSnapshot.MjpegDecoderCounts missing.");
        AssertEqual(intOptionType, decoderCountsProperty.PropertyType.GetElementType(), "AutomationOptionsSnapshot.MjpegDecoderCounts[] element type");

        var snapshotType = RequireType("Sussudio.Models.AutomationSnapshot");
        AssertNotNull(snapshotType.GetProperty("SelectedVideoFormat"), "AutomationSnapshot.SelectedVideoFormat");
        AssertNotNull(snapshotType.GetProperty("PreviewVolumePercent"), "AutomationSnapshot.PreviewVolumePercent");
        AssertNotNull(snapshotType.GetProperty("IsStatsVisible"), "AutomationSnapshot.IsStatsVisible");
    }
}
