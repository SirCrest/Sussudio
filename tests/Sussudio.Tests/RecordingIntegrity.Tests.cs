using System;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

// Tests for recording integrity counter summaries and mismatch reporting.
static partial class Program
{
    internal static Task RecordingIntegritySummary_FlagsAudioDiscontinuityAndDrift()
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

    internal static Task RecordingIntegritySummary_ToleratesSingleActiveInFlightFrame()
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

    internal static Task FlashbackRecordingIntegrity_UsesRecordingScopedSequenceGaps()
    {
        var unifiedText = ReadUnifiedVideoCaptureSource();
        var snapshotsText = ReadCaptureServiceRecordingIntegritySource();
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
        var serviceText = ReadCaptureServiceRecordingFinalizationSource();

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

    internal static Task CaptureService_RecordingIntegrityLivesInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingIntegrity.cs");
        var summaryText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingIntegrity.Summary.cs");
        var summaryFieldsText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingIntegrity.SummaryFields.cs");
        var summaryEvaluationText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingIntegrity.SummaryEvaluation.cs");
        var countersText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingIntegrity.Counters.cs");
        var audioText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingIntegrity.Audio.cs");

        AssertContains(rootText, "private RecordingIntegritySummary ResolveRecordingIntegritySummary(");
        AssertDoesNotContain(rootText, "private sealed record RecordingIntegrityCounterSnapshot(");
        AssertDoesNotContain(rootText, "private static RecordingIntegritySummary BuildRecordingIntegritySummary(");
        AssertDoesNotContain(rootText, "private RecordingIntegrityCounterSnapshot GetRecordingIntegrityCountersSinceBaseline(");
        AssertDoesNotContain(rootText, "private RecordingAudioIntegrityCounterSnapshot GetRecordingAudioCountersSinceBaseline(");
        AssertDoesNotContain(rootText, "private static void LogRecordingIntegritySummary(");

        AssertContains(countersText, "private sealed record RecordingIntegrityCounterSnapshot(");
        AssertContains(audioText, "private sealed record RecordingAudioIntegrityCounterSnapshot(");
        AssertContains(summaryText, "private static RecordingIntegritySummary BuildRecordingIntegritySummary(");
        AssertContains(summaryText, "var videoFields = BuildRecordingIntegritySummaryVideoFields(");
        AssertContains(summaryText, "var evaluation = EvaluateRecordingIntegritySummary(");
        AssertContains(summaryText, "private static void LogRecordingIntegritySummary(");
        AssertContains(summaryText, "RECORDING_INTEGRITY ");
        AssertDoesNotContain(summaryText, "new List<string>");
        AssertDoesNotContain(summaryText, "audio_boundary_drops=");
        AssertContains(summaryFieldsText, "private readonly record struct RecordingIntegritySummaryVideoFields");
        AssertContains(summaryFieldsText, "private readonly record struct RecordingIntegritySummaryAudioFields");
        AssertContains(summaryFieldsText, "private static RecordingIntegritySummaryVideoFields BuildRecordingIntegritySummaryVideoFields(");
        AssertContains(summaryFieldsText, "PipelineDroppedFrames = recordingActive");
        AssertContains(summaryEvaluationText, "private static RecordingIntegritySummaryEvaluation EvaluateRecordingIntegritySummary(");
        AssertContains(summaryEvaluationText, "private static string EvaluateRecordingIntegrityAudioStatus(");
        AssertContains(summaryEvaluationText, "RecordingIntegrityAvSyncDriftWarningMs");
        AssertContains(summaryEvaluationText, "audio_boundary_drops=");
        AssertContains(summaryEvaluationText, "private static string FormatRecordingIntegrityDouble(");
        AssertContains(countersText, "private RecordingIntegrityCounterSnapshot GetRecordingIntegrityCountersSinceBaseline(");
        AssertContains(countersText, "private RecordingIntegrityCounterSnapshot CaptureFlashbackRecordingIntegrityCountersSinceBaseline(");
        AssertContains(countersText, "private static long DeltaCounter(");
        AssertContains(audioText, "private RecordingAudioIntegrityCounterSnapshot GetRecordingAudioCountersSinceBaseline(");
        AssertContains(audioText, "private RecordingAudioIntegrityCounterSnapshot CaptureRecordingAudioCounters(");
        AssertContains(audioText, "CreateRecordingAudioCounters(");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.RecordingIntegrity.Logging.cs")),
            "old recording integrity logging partial removed");

        return Task.CompletedTask;
    }

    internal static Task SharedFormatter_RendersRecordingIntegrity()
    {
        var toolAssembly = LoadToolAssembly(System.IO.Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var formatterType = toolAssembly.GetType("Sussudio.Tools.AutomationSnapshotFormatter")
            ?? throw new InvalidOperationException("Sussudio.Tools.AutomationSnapshotFormatter type not found.");
        var formatSnapshot = formatterType.GetMethod("FormatSnapshot", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("AutomationSnapshotFormatter.FormatSnapshot not found.");

        const string json = """
                            {"Snapshot":{"SessionState":"Ready","StatusText":"Idle","SelectedDeviceName":"Synthetic","SelectedDeviceId":"device-1","IsInitialized":true,"IsPreviewing":true,"IsRecording":false,"SelectedResolution":"3840x2160","SelectedFrameRate":120,"SelectedRecordingFormat":"HEVC","SelectedQuality":"High","SelectedPreset":"P5","SelectedSplitEncodeMode":"Auto","SelectedVideoFormat":"MJPG","PreviewVolumePercent":42.5,"IsStatsVisible":true,"IsHdrEnabled":false,"IsHdrAvailable":true,"HdrOutputActive":false,"HdrRuntimeState":"Inactive","RequestedPipelineMode":"SDR","ActivePipelineMode":"SDR","PipelineModeMatched":true,"IsAudioEnabled":true,"IsAudioPreviewEnabled":false,"IsCustomAudioInputEnabled":false,"AudioPeak":0,"AudioClipping":false,"AudioSignalPresent":false,"AudioReaderActive":false,"AudioFramesArrived":0,"AudioFramesWrittenToSink":0,"VideoReaderActive":true,"IngestVideoFramesArrived":120,"IngestVideoFramesWrittenToSink":120,"EncoderVideoFramesEnqueued":0,"EncoderVideoFramesEncoded":0,"FfmpegVideoQueueDepth":0,"VideoDropsQueueSaturated":0,"IngestLastVideoFrameAgeMs":5,"EncoderLastEnqueueAgeMs":0,"EncoderLastWriteAgeMs":0,"MemoryPreference":"Gpu","VideoRequestedSubtype":"MJPG","VideoNegotiatedSubtype":"MJPG","VideoIngestErrorCount":0,"SourceReaderReadOutstanding":false,"SourceReaderReadOutstandingMs":0,"SourceReaderLastFrameTickMs":0,"SourceReaderFrameChannelDepth":0,"WasapiCaptureCallbackCount":0,"WasapiCaptureCallbackAvgIntervalMs":0,"WasapiCaptureCallbackMaxIntervalMs":0,"WasapiCaptureCallbackSevereGapCount":0,"WasapiCaptureAudioDiscontinuityCount":0,"WasapiCaptureAudioTimestampErrorCount":0,"WasapiCaptureAudioGlitchCount":0,"WasapiCaptureCallbackSilenceCount":0,"WasapiCaptureLastCallbackTickMs":0,"WasapiCaptureAudioLevelEventsFired":0,"WasapiPlaybackRenderCallbackCount":0,"WasapiPlaybackRenderSilenceCount":0,"WasapiPlaybackQueueDepth":0,"WasapiPlaybackQueueDropCount":0,"WasapiPlaybackLastRenderTickMs":0,"OutputPath":"","RecordingTime":"00:00:00","RecordingSizeInfo":"0 B","RecordingBitrateInfo":"0 Mbps","RecordingBackend":"None","AudioPathMode":"None","MuxResult":"NotAttempted","RecordingIntegrityStatus":"Complete","RecordingIntegrityComplete":true,"RecordingIntegrityBackend":"LibAv","RecordingIntegritySourceFrames":120,"RecordingIntegrityAcceptedFrames":120,"RecordingIntegrityPipelineDroppedFrames":0,"RecordingIntegrityQueueDroppedFrames":0,"RecordingIntegritySubmittedFrames":120,"RecordingIntegrityEncodedFrames":120,"RecordingIntegrityPacketsWritten":120,"RecordingIntegrityEncoderDroppedFrames":0,"RecordingIntegritySequenceGaps":0,"RecordingIntegrityQueueMaxDepth":2,"RecordingIntegrityQueueOldestFrameAgeMs":0,"RecordingIntegrityBackpressureWaitMs":0,"RecordingIntegrityBackpressureEvents":0,"RecordingIntegrityBackpressureMaxWaitMs":0,"RecordingIntegrityAudioStatus":"Clean","RecordingIntegrityAudioEnabled":true,"RecordingIntegrityAudioCaptureActive":true,"RecordingIntegrityAudioFramesArrived":48000,"RecordingIntegrityAudioFramesWrittenToSink":48000,"RecordingIntegrityAudioSamplesEncoded":48000,"RecordingIntegrityAudioDropEvents":0,"RecordingIntegrityAudioDiscontinuities":0,"RecordingIntegrityAudioTimestampErrors":0,"RecordingIntegrityAudioCallbackGaps":0,"RecordingIntegrityAvSyncDriftMs":1.25,"RecordingIntegrityAvSyncDriftRateMsPerSec":0.1,"RecordingIntegrityEncoderAvSyncDriftMs":1.0,"RecordingIntegrityEncoderAvSyncCorrectionSamples":0,"RecordingIntegrityReason":"Every delivered source frame reached the recording boundary.","LastOutputPath":"","LastOutputSizeBytes":0,"LastFinalizeStatus":"Stopped","PerformanceScore":100,"PerformancePerfectionMet":true,"PerformanceSummary":"OK","EstimatedPipelineLatencyMs":1,"CaptureCadenceObservedFps":120,"ExpectedCaptureFrameRate":120,"CaptureCadenceSampleCount":300,"CaptureCadenceAverageIntervalMs":8.3,"CaptureCadenceP95IntervalMs":8.5,"CaptureCadenceMaxIntervalMs":9.0,"CaptureCadenceJitterStdDevMs":0.1,"CaptureCadenceSevereGapCount":0,"CaptureCadenceEstimatedDroppedFrames":0,"CaptureCadenceEstimatedDropPercent":0}}
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
