using System;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

// Tests for recording integrity counter summaries and mismatch reporting.
static partial class Program
{
    private static Task RecordingIntegritySummary_DefaultsAreExplicit()
    {
        var summaryType = RequireType("Sussudio.Models.RecordingIntegritySummary");
        var notStarted = summaryType.GetProperty("NotStarted", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
            ?? throw new InvalidOperationException("RecordingIntegritySummary.NotStarted missing.");

        AssertEqual("NotStarted", GetStringProperty(notStarted, "Status"), "RecordingIntegritySummary default status");
        AssertEqual(false, GetBoolProperty(notStarted, "Complete"), "RecordingIntegritySummary default complete");
        AssertEqual("None", GetStringProperty(notStarted, "Backend"), "RecordingIntegritySummary default backend");
        AssertEqual(0L, GetLongProperty(notStarted, "SourceFrames"), "RecordingIntegritySummary default source frames");
        AssertEqual(0L, GetLongProperty(notStarted, "AcceptedFrames"), "RecordingIntegritySummary default accepted frames");
        AssertEqual(0L, GetLongProperty(notStarted, "EncodedFrames"), "RecordingIntegritySummary default encoded frames");
        AssertEqual(0, GetIntProperty(notStarted, "QueueMaxDepth"), "RecordingIntegritySummary default max queue depth");
        AssertEqual("Disabled", GetStringProperty(notStarted, "AudioStatus"), "RecordingIntegritySummary default audio status");
        AssertEqual(false, GetBoolProperty(notStarted, "AudioEnabled"), "RecordingIntegritySummary default audio enabled");
        AssertEqual("No recording has completed.", GetStringProperty(notStarted, "Reason"), "RecordingIntegritySummary default reason");

        return Task.CompletedTask;
    }

    private static Task RecordingIntegritySnapshotContract_ExposesAutomationFields()
    {
        foreach (var typeName in new[]
        {
            "Sussudio.Models.CaptureRuntimeSnapshot",
            "Sussudio.Models.AutomationSnapshot"
        })
        {
            var snapshotType = RequireType(typeName);
            AssertProperty(snapshotType, "RecordingIntegrityStatus", typeof(string));
            AssertProperty(snapshotType, "RecordingIntegrityComplete", typeof(bool));
            AssertProperty(snapshotType, "RecordingIntegrityBackend", typeof(string));
            AssertProperty(snapshotType, "RecordingIntegrityCompletedUtc", typeof(DateTimeOffset?));
            AssertProperty(snapshotType, "RecordingIntegritySourceFrames", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAcceptedFrames", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityPipelineDroppedFrames", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityQueueDroppedFrames", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegritySubmittedFrames", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityEncodedFrames", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityPacketsWritten", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityEncoderDroppedFrames", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegritySequenceGaps", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityQueueMaxDepth", typeof(int));
            AssertProperty(snapshotType, "RecordingIntegrityQueueOldestFrameAgeMs", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityBackpressureWaitMs", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityBackpressureEvents", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityBackpressureMaxWaitMs", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAudioStatus", typeof(string));
            AssertProperty(snapshotType, "RecordingIntegrityAudioEnabled", typeof(bool));
            AssertProperty(snapshotType, "RecordingIntegrityAudioCaptureActive", typeof(bool));
            AssertProperty(snapshotType, "RecordingIntegrityAudioFramesArrived", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAudioFramesWrittenToSink", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAudioSamplesEncoded", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAudioDropEvents", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAudioDiscontinuities", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAudioTimestampErrors", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAudioCallbackGaps", typeof(long));
            AssertProperty(snapshotType, "RecordingIntegrityAvSyncDriftMs", typeof(double?));
            AssertProperty(snapshotType, "RecordingIntegrityAvSyncDriftRateMsPerSec", typeof(double?));
            AssertProperty(snapshotType, "RecordingIntegrityEncoderAvSyncDriftMs", typeof(double?));
            AssertProperty(snapshotType, "RecordingIntegrityEncoderAvSyncCorrectionSamples", typeof(long?));
            AssertProperty(snapshotType, "RecordingIntegrityReason", typeof(string));
        }

        return Task.CompletedTask;
    }

    private static Task RecordingIntegrityAutomationProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.cs")
            .Replace("\r\n", "\n");
        var recordingProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.RecordingIntegrity.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var recordingIntegrity = BuildRecordingIntegrityProjection(captureRuntime);");
        AssertContains(snapshotProjectionText, "RecordingIntegrityStatus = recordingIntegrity.Status,");
        AssertContains(snapshotProjectionText, "RecordingIntegrityAudioFramesWrittenToSink = recordingIntegrity.AudioFramesWrittenToSink,");
        AssertContains(snapshotProjectionText, "RecordingIntegrityEncoderAvSyncDriftMs = recordingIntegrity.EncoderAvSyncDriftMs,");
        AssertContains(snapshotProjectionText, "RecordingIntegrityReason = recordingIntegrity.Reason,");
        AssertDoesNotContain(snapshotProjectionText, "RecordingIntegrityStatus = captureRuntime.RecordingIntegrityStatus,");
        AssertDoesNotContain(snapshotProjectionText, "RecordingIntegrityAudioFramesWrittenToSink = captureRuntime.RecordingIntegrityAudioFramesWrittenToSink,");
        AssertDoesNotContain(snapshotProjectionText, "RecordingIntegrityEncoderAvSyncDriftMs = captureRuntime.RecordingIntegrityEncoderAvSyncDriftMs,");
        AssertDoesNotContain(snapshotProjectionText, "RecordingIntegrityReason = captureRuntime.RecordingIntegrityReason,");

        AssertContains(recordingProjectionText, "private static RecordingIntegrityProjection BuildRecordingIntegrityProjection(CaptureRuntimeSnapshot captureRuntime)");
        AssertContains(recordingProjectionText, "private readonly record struct RecordingIntegrityProjection");
        AssertContains(recordingProjectionText, "Status = captureRuntime.RecordingIntegrityStatus,");
        AssertContains(recordingProjectionText, "AudioFramesWrittenToSink = captureRuntime.RecordingIntegrityAudioFramesWrittenToSink,");
        AssertContains(recordingProjectionText, "EncoderAvSyncDriftMs = captureRuntime.RecordingIntegrityEncoderAvSyncDriftMs,");
        AssertContains(recordingProjectionText, "Reason = captureRuntime.RecordingIntegrityReason");

        return Task.CompletedTask;
    }

    private static Task RecordingIntegritySummary_FlagsAudioDiscontinuityAndDrift()
    {
        var summary = InvokeBuildRecordingIntegritySummary(
            audioDiscontinuities: 2,
            avSyncDriftMs: 750.0,
            encoderAvSyncDriftMs: -650.0);

        AssertEqual("Incomplete", GetStringProperty(summary, "Status"), "Recording integrity should flag audio discontinuity/drift.");
        AssertEqual(false, GetBoolProperty(summary, "Complete"), "Recording integrity should not be complete with audio issues.");
        AssertEqual("Incomplete", GetStringProperty(summary, "AudioStatus"), "Audio integrity should be incomplete with discontinuity/drift.");

        var reason = GetStringProperty(summary, "Reason");
        AssertContains(reason, "audio_discontinuities=2");
        AssertContains(reason, "av_sync_drift_ms=750");
        AssertContains(reason, "encoder_av_sync_drift_ms=-650");

        return Task.CompletedTask;
    }

    private static Task RecordingIntegritySummary_ToleratesSingleActiveInFlightFrame()
    {
        var summary = InvokeBuildRecordingIntegritySummary(
            audioDiscontinuities: 0,
            avSyncDriftMs: 0.0,
            encoderAvSyncDriftMs: 0.0,
            recordingActive: true,
            sourceFrames: 121,
            acceptedFrames: 120);

        AssertEqual("Active", GetStringProperty(summary, "Status"), "Recording integrity should tolerate one active in-flight frame.");
        AssertEqual(0L, GetLongProperty(summary, "PipelineDroppedFrames"), "Active in-flight frame should not count as a pipeline drop.");
        AssertContains(GetStringProperty(summary, "Reason"), "Recording active; all delivered source frames have reached the recording boundary so far.");

        return Task.CompletedTask;
    }

    private static Task FlashbackRecordingIntegrity_UsesRecordingScopedSequenceGaps()
    {
        var unifiedText = ReadUnifiedVideoCaptureSource();
        var snapshotsText = System.IO.File.ReadAllText(System.IO.Path.Combine(
            GetRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.RecordingIntegrity.cs"));
        var snapshotHelpersText = System.IO.File.ReadAllText(System.IO.Path.Combine(
            GetRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.Snapshots.cs"));
        var snapshotAvSyncText = System.IO.File.ReadAllText(System.IO.Path.Combine(
            GetRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.SnapshotAvSync.cs"));
        var serviceText = System.IO.File.ReadAllText(System.IO.Path.Combine(
            GetRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.RecordingFinalizeRecord.cs"));

        AssertContains(unifiedText, "public long FlashbackRecordingSequenceGaps");
        AssertContains(unifiedText, "TrackFlashbackRecordingAcceptedSequence(sourceSequence)");
        AssertContains(snapshotsText, "CaptureFlashbackRecordingIntegrityCountersSinceBaseline");
        AssertContains(snapshotsText, "videoCapture.FlashbackRecordingSequenceGaps");
        AssertContains(serviceText, "CaptureFlashbackRecordingIntegrityCountersSinceBaseline(flashbackSink, flashbackVideoCapture)");
        AssertContains(snapshotsText, "if (sink.TryGetEncoderAvSyncDrift(out var driftMs, out var correctionSamples))");
        AssertDoesNotContain(snapshotHelpersText, "private (double? DriftMs, double? RateMsPerSec) ComputeAvSyncDrift()");
        AssertContains(snapshotAvSyncText, "private (double? DriftMs, double? RateMsPerSec) ComputeAvSyncDrift()");
        AssertContains(snapshotAvSyncText, "private (double? EncoderDriftMs, long? EncoderCorrectionSamples) GetEncoderAvSyncDrift()");
        AssertContains(snapshotsText, "encoderAvSyncDriftMs = driftMs;");
        AssertContains(snapshotsText, "encoderAvSyncCorrectionSamples = correctionSamples;");
        AssertContains(snapshotsText, "avSyncDriftMs: null,\n            avSyncDriftRateMsPerSec: null,\n            encoderAvSyncDriftMs: null,\n            encoderAvSyncCorrectionSamples: null");

        return Task.CompletedTask;
    }

    private static Task SharedFormatter_RendersRecordingIntegrity()
    {
        var toolAssembly = LoadToolAssembly(System.IO.Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var formatterType = toolAssembly.GetType("Sussudio.Tools.AutomationSnapshotFormatter")
            ?? throw new InvalidOperationException("Sussudio.Tools.AutomationSnapshotFormatter type not found.");
        var formatSnapshot = formatterType.GetMethod("FormatSnapshot", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("AutomationSnapshotFormatter.FormatSnapshot not found.");

        const string json = """
                            {"Snapshot":{"SessionState":"Ready","StatusText":"Idle","SelectedDeviceName":"Synthetic","SelectedDeviceId":"device-1","IsInitialized":true,"IsPreviewing":true,"IsRecording":false,"SelectedResolution":"3840x2160","SelectedFrameRate":120,"SelectedRecordingFormat":"HEVC","SelectedQuality":"High","SelectedPreset":"P5","SelectedSplitEncodeMode":"Auto","SelectedVideoFormat":"MJPG","ShowAllCaptureOptions":true,"PreviewVolumePercent":42.5,"IsStatsVisible":true,"IsHdrEnabled":false,"IsHdrAvailable":true,"HdrOutputActive":false,"HdrRuntimeState":"Inactive","RequestedPipelineMode":"SDR","ActivePipelineMode":"SDR","PipelineModeMatched":true,"IsAudioEnabled":true,"IsAudioPreviewEnabled":false,"IsCustomAudioInputEnabled":false,"AudioPeak":0,"AudioClipping":false,"AudioSignalPresent":false,"AudioReaderActive":false,"AudioFramesArrived":0,"AudioFramesWrittenToSink":0,"VideoReaderActive":true,"IngestVideoFramesArrived":120,"IngestVideoFramesWrittenToSink":120,"EncoderVideoFramesEnqueued":0,"EncoderVideoFramesEncoded":0,"FfmpegVideoQueueDepth":0,"VideoDropsQueueSaturated":0,"IngestLastVideoFrameAgeMs":5,"EncoderLastEnqueueAgeMs":0,"EncoderLastWriteAgeMs":0,"MemoryPreference":"Gpu","VideoRequestedSubtype":"MJPG","VideoNegotiatedSubtype":"MJPG","VideoIngestErrorCount":0,"SourceReaderReadOutstanding":false,"SourceReaderReadOutstandingMs":0,"SourceReaderLastFrameTickMs":0,"SourceReaderFrameChannelDepth":0,"WasapiCaptureCallbackCount":0,"WasapiCaptureCallbackAvgIntervalMs":0,"WasapiCaptureCallbackMaxIntervalMs":0,"WasapiCaptureCallbackSevereGapCount":0,"WasapiCaptureAudioDiscontinuityCount":0,"WasapiCaptureAudioTimestampErrorCount":0,"WasapiCaptureAudioGlitchCount":0,"WasapiCaptureCallbackSilenceCount":0,"WasapiCaptureLastCallbackTickMs":0,"WasapiCaptureAudioLevelEventsFired":0,"WasapiPlaybackRenderCallbackCount":0,"WasapiPlaybackRenderSilenceCount":0,"WasapiPlaybackQueueDepth":0,"WasapiPlaybackQueueDropCount":0,"WasapiPlaybackLastRenderTickMs":0,"OutputPath":"","RecordingTime":"00:00:00","RecordingSizeInfo":"0 B","RecordingBitrateInfo":"0 Mbps","RecordingBackend":"None","AudioPathMode":"None","MuxResult":"NotAttempted","RecordingIntegrityStatus":"Complete","RecordingIntegrityComplete":true,"RecordingIntegrityBackend":"LibAv","RecordingIntegritySourceFrames":120,"RecordingIntegrityAcceptedFrames":120,"RecordingIntegrityPipelineDroppedFrames":0,"RecordingIntegrityQueueDroppedFrames":0,"RecordingIntegritySubmittedFrames":120,"RecordingIntegrityEncodedFrames":120,"RecordingIntegrityPacketsWritten":120,"RecordingIntegrityEncoderDroppedFrames":0,"RecordingIntegritySequenceGaps":0,"RecordingIntegrityQueueMaxDepth":2,"RecordingIntegrityQueueOldestFrameAgeMs":0,"RecordingIntegrityBackpressureWaitMs":0,"RecordingIntegrityBackpressureEvents":0,"RecordingIntegrityBackpressureMaxWaitMs":0,"RecordingIntegrityAudioStatus":"Clean","RecordingIntegrityAudioEnabled":true,"RecordingIntegrityAudioCaptureActive":true,"RecordingIntegrityAudioFramesArrived":48000,"RecordingIntegrityAudioFramesWrittenToSink":48000,"RecordingIntegrityAudioSamplesEncoded":48000,"RecordingIntegrityAudioDropEvents":0,"RecordingIntegrityAudioDiscontinuities":0,"RecordingIntegrityAudioTimestampErrors":0,"RecordingIntegrityAudioCallbackGaps":0,"RecordingIntegrityAvSyncDriftMs":1.25,"RecordingIntegrityAvSyncDriftRateMsPerSec":0.1,"RecordingIntegrityEncoderAvSyncDriftMs":1.0,"RecordingIntegrityEncoderAvSyncCorrectionSamples":0,"RecordingIntegrityReason":"Every delivered source frame reached the recording boundary.","LastOutputPath":"","LastOutputSizeBytes":0,"LastFinalizeStatus":"Stopped","PerformanceScore":100,"PerformancePerfectionMet":true,"PerformanceSummary":"OK","EstimatedPipelineLatencyMs":1,"CaptureCadenceObservedFps":120,"ExpectedCaptureFrameRate":120,"CaptureCadenceSampleCount":300,"CaptureCadenceAverageIntervalMs":8.3,"CaptureCadenceP95IntervalMs":8.5,"CaptureCadenceMaxIntervalMs":9.0,"CaptureCadenceJitterStdDevMs":0.1,"CaptureCadenceSevereGapCount":0,"CaptureCadenceEstimatedDroppedFrames":0,"CaptureCadenceEstimatedDropPercent":0}}
                            """;
        using var document = JsonDocument.Parse(json);
        var output = formatSnapshot.Invoke(null, new object[] { document.RootElement, false })?.ToString()
            ?? throw new InvalidOperationException("AutomationSnapshotFormatter.FormatSnapshot returned null.");

        AssertContains(output, "Integrity: Complete complete=true backend=LibAv source=120 accepted=120");
        AssertContains(output, "boundaryDrops=0 queueDrops=0 encoderDrops=0 seqGaps=0 submitted=120 encoded=120 packets=120");
        AssertContains(output, "qMax=2 qOldestMs=0 backpressure=0ms/0 max=0ms");
        AssertContains(output, "Audio Integrity: Clean enabled=true active=true arrived=48000 written=48000 encoded=48000");
        AssertContains(output, "drift=1.25ms encoderDrift=1.0ms corr=0");

        return Task.CompletedTask;
    }

    private static void AssertProperty(Type type, string propertyName, Type propertyType)
    {
        var property = type.GetProperty(propertyName)
            ?? throw new InvalidOperationException($"{type.Name}.{propertyName} missing.");
        AssertEqual(propertyType, property.PropertyType, $"{type.Name}.{propertyName} type");
    }

    private static object InvokeBuildRecordingIntegritySummary(
        long audioDiscontinuities,
        double avSyncDriftMs,
        double encoderAvSyncDriftMs,
        bool recordingActive = false,
        long sourceFrames = 120,
        long acceptedFrames = 120)
    {
        var serviceType = RequireType("Sussudio.Services.Capture.CaptureService");
        var counterType = serviceType.GetNestedType("RecordingIntegrityCounterSnapshot", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RecordingIntegrityCounterSnapshot missing.");
        var audioCounterType = serviceType.GetNestedType("RecordingAudioIntegrityCounterSnapshot", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RecordingAudioIntegrityCounterSnapshot missing.");

        var counters = Activator.CreateInstance(
            counterType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object?[]
            {
                "LibAv",
                120L,
                120L,
                120L,
                0L,
                0L,
                0L,
                2,
                0L,
                0L,
                0L,
                0L,
                false,
                null,
                null
            },
            culture: null)
            ?? throw new InvalidOperationException("Could not create RecordingIntegrityCounterSnapshot.");

        var audioCounters = Activator.CreateInstance(
            audioCounterType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new object?[]
            {
                true,
                true,
                48000L,
                48000L,
                48000L,
                0L,
                audioDiscontinuities,
                0L,
                0L,
                avSyncDriftMs,
                0.0,
                encoderAvSyncDriftMs,
                0L
            },
            culture: null)
            ?? throw new InvalidOperationException("Could not create RecordingAudioIntegrityCounterSnapshot.");

        var method = serviceType.GetMethod("BuildRecordingIntegritySummary", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("BuildRecordingIntegritySummary missing.");

        return method.Invoke(
            null,
            new object?[]
            {
                "LibAv",
                recordingActive,
                true,
                recordingActive ? "Recording" : "Stopped",
                recordingActive ? null : DateTimeOffset.UtcNow,
                sourceFrames,
                acceptedFrames,
                counters,
                audioCounters
            })
            ?? throw new InvalidOperationException("BuildRecordingIntegritySummary returned null.");
    }
}
