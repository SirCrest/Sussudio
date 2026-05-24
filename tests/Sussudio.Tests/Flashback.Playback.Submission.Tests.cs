using System.Threading.Tasks;

static partial class Program
{
    internal static Task FlashbackPlaybackController_SubmitFailuresReleaseDecodedFrames()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var previewFramesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PreviewFrames.cs")
            .Replace("\r\n", "\n");
        var playbackFrameOwnershipText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "private bool TrySubmitAndHoldFrame(DecodedVideoFrame frame, string operation)");
        AssertContains(previewFramesText, "private bool TrySubmitAndHoldFrame(DecodedVideoFrame frame, string operation)");
        AssertContains(previewFramesText, "private static void SubmitFrame(");
        AssertContains(previewFramesText, "private static bool TryValidatePreviewFrame(DecodedVideoFrame frame, out string reason)");
        AssertContains(previewFramesText, "private static bool TryCalculatePreviewFrameBytes(int width, int height, bool isHdr, out int bytes)");
        AssertDoesNotContain(previewFramesText, "private void RestoreLiveAfterSeekDisplayFailure(");
        AssertDoesNotContain(previewFramesText, "private void RestoreLiveAfterPlaybackSubmitFailure(");
        AssertContains(playbackFrameOwnershipText, "private DecodedVideoFrame _previousHeldFrame;");
        AssertContains(playbackFrameOwnershipText, "private bool _hasPreviousHeldFrame;");
        AssertContains(playbackFrameOwnershipText, "private void ReleasePreviousHeldFrame()");
        AssertContains(playbackFrameOwnershipText, "private void HoldSubmittedFrame(DecodedVideoFrame frame)");
        AssertContains(playbackFrameOwnershipText, "private void ReleasePlaybackFrameForLive(string operation)");
        AssertContains(playbackFrameOwnershipText, "private static void ReleaseHeldFrameBestEffort(DecodedVideoFrame frame, string operation)");
        AssertContains(playbackFrameOwnershipText, "private void RestoreLiveAfterSeekDisplayFailure(FlashbackDecoder decoder, ref bool fileOpen, string operation)");
        AssertContains(playbackFrameOwnershipText, "private void RestoreLiveAfterPlaybackSubmitFailure(FlashbackDecoder decoder, ref bool fileOpen, string operation)");
        AssertContains(playbackFrameOwnershipText, "private void RestoreLiveAfterPlaybackDecodeError(FlashbackDecoder decoder, ref bool fileOpen)");
        AssertContains(playbackFrameOwnershipText, "private void RestoreLiveAfterNearLiveSnap(FlashbackDecoder decoder, ref bool fileOpen)");
        AssertContains(playbackFrameOwnershipText, "private void RestoreLiveAfterSoftwarePlaybackBudgetSnap(FlashbackDecoder decoder, ref bool fileOpen, string operation)");
        AssertContains(playbackFrameOwnershipText, "private void RestoreLiveAfterDecoderPlaybackFailure(");
        AssertContains(playbackFrameOwnershipText, "CloseDecoderFileBestEffort(decoder, operation);");
        AssertContains(playbackFrameOwnershipText, "ReleasePlaybackFrameForLive(operation);");
        AssertContains(playbackFrameOwnershipText, "RestoreLiveAudio();");
        AssertContains(playbackFrameOwnershipText, "SafeResumePreviewSubmission(operation);");
        AssertContains(playbackFrameOwnershipText, "SafeResumeRendering(operation);");
        AssertContains(playbackFrameOwnershipText, "SetState(FlashbackPlaybackState.Live);");
        AssertDoesNotContain(previewFramesText, "private DecodedVideoFrame _previousHeldFrame;");
        AssertDoesNotContain(previewFramesText, "private bool _hasPreviousHeldFrame;");
        AssertDoesNotContain(previewFramesText, "_previousHeldFrame = frame;");
        AssertDoesNotContain(previewFramesText, "_hasPreviousHeldFrame = true;");
        AssertDoesNotContain(previewFramesText, "private void ReleasePlaybackFrameForLive(string operation)");
        AssertDoesNotContain(previewFramesText, "private static void ReleaseHeldFrameBestEffort(DecodedVideoFrame frame, string operation)");
        AssertDoesNotContain(rootText, "private DecodedVideoFrame _previousHeldFrame;");
        AssertDoesNotContain(rootText, "private bool _hasPreviousHeldFrame;");
        AssertContains(rootText, "private IPreviewFrameSink? _previewSink;");
        AssertContains(rootText, "private ILiveVideoSource? _videoCapture;");
        AssertContains(rootText, "private volatile WasapiAudioPlayback? _audioPlayback;");
        AssertContains(rootText, "private volatile WasapiAudioCapture? _audioCapture;");
        AssertContains(rootText, "private volatile bool _initialized;");
        AssertContains(rootText, "private volatile int _disposedFlag;");
        AssertContains(rootText, "private int _previewDetachStopTimeoutActive;");
        AssertContains(rootText, "private int _deferredPreviewAttachApplyRetryScheduled;");
        AssertContains(rootText, "private IPreviewFrameSink? _pendingPreviewSinkAfterDetachTimeout;");
        AssertContains(rootText, "private ILiveVideoSource? _pendingVideoCaptureAfterDetachTimeout;");
        AssertContains(rootText, "public void PrepareForPreviewDetach()");
        AssertContains(rootText, "private void DetachPreviewComponentsAfterStopTimeout()");
        AssertContains(rootText, "private bool TryDeferPreviewAttachAfterStopTimeoutUnsafe(");
        AssertContains(rootText, "private void ApplyDeferredPreviewAttachAfterStopTimeout()");
        AssertContains(rootText, "private void ScheduleDeferredPreviewAttachApplyRetry()");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackPlaybackController.Lifecycle.cs")),
            "Flashback playback component lifecycle folded into root controller");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackPlaybackController.PreviewDetachLifecycle.cs")),
            "Flashback playback preview-detach lifecycle folded into root controller");
        AssertContains(sourceText, "if (!TryValidatePreviewFrame(frame, out var skipReason))");
        AssertContains(sourceText, "Interlocked.Increment(ref _playbackSubmitFailures);");
        AssertContains(sourceText, "SetLastSubmitFailure($\"{operation}:{skipReason}\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(frame, $\"{operation}_{skipReason}\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SUBMIT_SKIP op={operation} reason={skipReason}");
        AssertContains(sourceText, "public long PlaybackSubmitFailures => Interlocked.Read(ref _playbackSubmitFailures);");
        AssertContains(sourceText, "public long LastSubmitFailureUtcUnixMs => Interlocked.Read(ref _lastSubmitFailureUtcUnixMs);");
        AssertContains(sourceText, "public string LastSubmitFailure => Volatile.Read(ref _lastSubmitFailure);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _playbackSubmitFailures, 0);");
        AssertContains(sourceText, "ClearLastSubmitFailure();");
        AssertContains(sourceText, "public void UpdatePreviewComponents(IPreviewFrameSink? previewSink, ILiveVideoSource? videoCapture)");
        AssertContains(sourceText, "TryDeferPreviewAttachAfterStopTimeoutUnsafe(previewSink, videoCapture, \"update\")");
        AssertContains(sourceText, "_initialized = previewSink != null && videoCapture != null;");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_UPDATE sink={previewSink != null} capture={videoCapture != null}");
        AssertContains(sourceText, "ApplyPreviewRoutingForState(\"preview_update\");");
        AssertContains(sourceText, "public void PrepareForPreviewDetach()");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_DETACH state={_state} thread_alive={PlaybackThreadAlive}");
        AssertContains(sourceText, "if (!StopPlaybackThread(PreviewDetachThreadStopTimeout, \"preview_detach\"))\n        {\n            Logger.Log(\"FLASHBACK_PLAYBACK_PREVIEW_DETACH_ABORT reason=thread_stop_failed\");\n            RestoreLiveAudio();\n            SafeResumePreviewSubmission(\"preview_detach_timeout\");\n            DetachPreviewComponentsAfterStopTimeout();\n            return;\n        }\n\n        ReleasePlaybackFrameForLive(\"preview_detach\");");
        AssertOccursBefore(sourceText, "SafeResumePreviewSubmission(\"preview_detach_timeout\");", "DetachPreviewComponentsAfterStopTimeout();\n            return;");
        AssertOccursBefore(sourceText, "DetachPreviewComponentsAfterStopTimeout();\n            return;", "ReleasePlaybackFrameForLive(\"preview_detach\");");
        AssertContains(sourceText, "RestoreLiveAudio();\n        SafeResumePreviewSubmission(\"preview_detach\");\n        SetState(FlashbackPlaybackState.Live);");
        AssertContains(sourceText, "private void DetachPreviewComponentsAfterStopTimeout()");
        AssertContains(sourceText, "Volatile.Write(ref _previewDetachStopTimeoutActive, 1);");
        AssertContains(sourceText, "_pendingPreviewSinkAfterDetachTimeout = null;\n            _pendingVideoCaptureAfterDetachTimeout = null;");
        AssertContains(sourceText, "_previewSink = null;\n            _videoCapture = null;\n            _initialized = false;");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_DETACH_DEFER_OWNED_CLEANUP reason=thread_alive");
        AssertContains(sourceText, "private bool TryDeferPreviewAttachAfterStopTimeoutUnsafe(");
        AssertContains(sourceText, "_pendingPreviewSinkAfterDetachTimeout = previewSink;\n        _pendingVideoCaptureAfterDetachTimeout = videoCapture;");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_ATTACH_DEFER op={operation} reason=thread_alive_after_detach_timeout");
        AssertContains(sourceText, "private void ApplyDeferredPreviewAttachAfterStopTimeout()");
        AssertContains(sourceText, "Monitor.TryEnter(_playbackThreadSync, 0, ref lockTaken);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_ATTACH_DEFER_APPLY_SKIP reason=lock_busy");
        AssertContains(sourceText, "ScheduleDeferredPreviewAttachApplyRetry();");
        AssertContains(sourceText, "private void ScheduleDeferredPreviewAttachApplyRetry()");
        AssertContains(sourceText, "Interlocked.CompareExchange(ref _deferredPreviewAttachApplyRetryScheduled, 1, 0)");
        AssertContains(sourceText, "await Task.Delay(25).ConfigureAwait(false);");
        AssertContains(sourceText, "if (Volatile.Read(ref _previewDetachStopTimeoutActive) != 0)");
        AssertContains(sourceText, "Volatile.Write(ref _previewDetachStopTimeoutActive, 0);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _deferredPreviewAttachApplyRetryScheduled, 0);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_ATTACH_DEFER_APPLIED reason=thread_exit");
        AssertContains(sourceText, "ApplyPreviewRoutingForState(\"deferred_preview_attach\");");
        AssertContains(sourceText, "private void ApplyPreviewRoutingForState(string operation)");
        AssertContains(sourceText, "var previewSink = Volatile.Read(ref _previewSink);");
        AssertContains(sourceText, "SetLastSubmitFailure($\"{operation}:missing_preview_sink\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(frame, $\"{operation}_missing_preview_sink\");");
        AssertContains(sourceText, "private static bool TryValidatePreviewFrame(DecodedVideoFrame frame, out string reason)");
        AssertContains(sourceText, "reason = \"invalid_dimensions\";");
        AssertContains(sourceText, "reason = \"null_texture\";");
        AssertContains(sourceText, "reason = \"invalid_subresource\";");
        AssertContains(sourceText, "reason = \"null_data\";");
        AssertContains(sourceText, "reason = \"invalid_data_length\";");
        AssertContains(sourceText, "reason = \"short_data_length\";");
        AssertContains(sourceText, "private static bool TryCalculatePreviewFrameBytes(int width, int height, bool isHdr, out int bytes)");
        AssertContains(sourceText, "var calculated = isHdr\n            ? pixels * 3\n            : pixels + width * (long)(height / 2);");
        AssertContains(sourceText, "private static void ReleaseHeldFrameBestEffort(DecodedVideoFrame frame, string operation)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_RELEASE_HELD_FRAME_WARN");
        AssertContains(sourceText, "SetLastSubmitFailure($\"{operation}:submit_fail:{ex.GetType().Name}\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(frame, $\"{operation}_submit_fail\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(_previousHeldFrame, \"previous_frame\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(videoFrame, \"av_sync_skip\");");
        AssertContains(sourceText, "private void ReleasePlaybackFrameForLive(string operation)");
        AssertContains(sourceText, "private void ReleasePlaybackFrameForLive(string operation)\n    {\n        Interlocked.Exchange(ref _lastAudioPtsTicks, 0);\n        Interlocked.Exchange(ref _lastVideoPtsTicks, 0);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_RELEASE_HELD_FOR_LIVE op={operation}");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"seek_no_file\");");
        AssertContains(sourceText, "SetNoFileFailure(CommandKind.Seek, cmd.Position);");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"scrub_no_file\");");
        AssertContains(sourceText, "SetNoFileFailure(CommandKind.BeginScrub, cmd.Position);");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"scrub_update_no_file\");");
        AssertContains(sourceText, "SetNoFileFailure(CommandKind.UpdateScrub, cmd.Position);");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"play_no_file\");");
        AssertContains(sourceText, "SetNoFileFailure(CommandKind.Play, PlaybackPosition);");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(\"nudge_no_file\");");
        AssertContains(sourceText, "SetNoFileFailure(CommandKind.Nudge, nudgedPos);");
        AssertContains(sourceText, "RestoreLiveAfterNearLiveSnap(decoder, ref fileOpen);");
        AssertContains(sourceText, "RestoreLiveAfterPlaybackDecodeError(decoder, ref fileOpen);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SUBMIT_FAIL");
        AssertContains(sourceText, "TrySubmitAndHoldFrame(nudgeFrame, \"nudge\")");
        AssertContains(sourceText, "TrySubmitAndHoldFrame(frame, \"seek\")");
        AssertContains(sourceText, "TrySubmitAndHoldFrame(videoFrame, \"playback\")");
        AssertContains(sourceText, "var countForPresentCadence = string.Equals(operation, \"playback\", StringComparison.Ordinal);");
        AssertContains(sourceText, "var submitTick = Stopwatch.GetTimestamp();");
        AssertContains(sourceText, "var previewPresentId = Interlocked.Increment(ref _playbackPreviewPresentId);");
        AssertContains(sourceText, "SubmitFrame(previewSink, frame, previewPresentId, countForPresentCadence);");
        AssertContains(sourceText, "sourceSequenceNumber: -1");
        AssertContains(sourceText, "previewPresentId: previewPresentId");
        AssertContains(sourceText, "sourcePtsTicks: frame.Pts.Ticks");
        AssertContains(sourceText, "countForPresentCadence: countForPresentCadence");
        AssertContains(sourceText, "arrivalTick: submitTick");
        AssertContains(sourceText, "schedulerSubmitTick: submitTick");
        AssertDoesNotContain(sourceText, "frame.Width, frame.Height, frame.IsHdr, arrivalTick: 0");
        AssertContains(sourceText, "if (!TrySubmitAndHoldFrame(videoFrame, \"playback\"))\n            {\n                Logger.Log($\"FLASHBACK_PLAYBACK_SUBMIT_STOP pos_ms={(long)PlaybackPosition.TotalMilliseconds}\");\n                RestoreLiveAfterPlaybackSubmitFailure(decoder, ref fileOpen, \"playback_submit_failed\");\n                return false;\n            }");
        AssertContains(sourceText, "private void RestoreLiveAfterPlaybackSubmitFailure(FlashbackDecoder decoder, ref bool fileOpen, string operation)");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(operation);\n        RestoreLiveAudio();\n        SafeResumePreviewSubmission(operation);\n        if (resumeRendering)\n        {\n            SafeResumeRendering(operation);\n        }\n\n        SetState(FlashbackPlaybackState.Live);");
        AssertDoesNotContain(sourceText, "ReleasePreviousHeldFrame();\n        try\n        {\n            SubmitFrame(frame);");
        AssertContains(sourceText, "SubmitFrame(previewSink, frame, previewPresentId, countForPresentCadence);\n            HoldSubmittedFrame(frame);");
        AssertDoesNotContain(sourceText, "ReleasePreviousHeldFrame();\n            SubmitFrame(videoFrame);");

        return Task.CompletedTask;
    }
}
