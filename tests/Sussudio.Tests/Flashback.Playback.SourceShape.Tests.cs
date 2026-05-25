using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackPlaybackController_PublicPlaybackState_LivesInRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var playbackStateText = rootText;

        AssertContains(playbackStateText, "private volatile FlashbackPlaybackState _state = FlashbackPlaybackState.Live;");
        AssertContains(playbackStateText, "private long _playbackPositionTicks;");
        AssertContains(playbackStateText, "private volatile string _decoderHwAccel = \"N/A\";");
        AssertContains(playbackStateText, "private long _lastAudioPtsTicks;");
        AssertContains(playbackStateText, "private long _lastVideoPtsTicks;");
        AssertContains(playbackStateText, "private bool _wasPlayingBeforeScrub;");
        AssertContains(playbackStateText, "public bool GpuDecodeEnabled { get; set; } = true;");
        AssertContains(playbackStateText, "public FlashbackPlaybackState State => _state;");
        AssertContains(playbackStateText, "public TimeSpan PlaybackPosition");
        AssertContains(playbackStateText, "public TimeSpan GapFromLive");
        AssertContains(playbackStateText, "public bool IsInitialized => _initialized;");
        AssertContains(playbackStateText, "public bool IsDisposed => _disposedFlag != 0;");
        AssertContains(playbackStateText, "public string DecoderHwAccel => _decoderHwAccel;");
        AssertContains(playbackStateText, "private void SetState(FlashbackPlaybackState newState)");
        AssertContains(rootText, "private readonly FlashbackBufferManager _bufferManager;");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_ClampsCommandPositionsBeforeFileLookup()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        // All three scrub-related command paths must clamp via the eviction-aware
        // overload so a long-held scrub doesn't resolve to evicted file PTS.
        const string seekClampBeforeOpen = "cmd = cmd with { Position = ClampPosition(cmd.Position, frozenValidStart) };\n        var seekResumeTarget = SaturatingAdd(cmd.Position, frozenValidStart);";
        const string scrubClampBeforeOpen = "cmd = cmd with { Position = ClampPosition(cmd.Position, frozenValidStart) };\n        decoder ??= CreateDecoder();\n        EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(cmd.Position, frozenValidStart));";

        AssertContains(sourceText, seekClampBeforeOpen);
        AssertContains(sourceText, "if (ShouldYieldSeekToQueuedPlay(commandChannel))\n        {\n            PlaybackPosition = cmd.Position;\n            pendingExactResumeTarget = seekResumeTarget;");
        AssertContains(sourceText, "decoder ??= CreateDecoder();\n        EnsureFileOpen(decoder, ref fileOpen, seekResumeTarget);");
        AssertEqual(1, sourceText.Split(scrubClampBeforeOpen, StringSplitOptions.None).Length - 1, "BeginScrub clamps before file lookup with frozen reference");
        var updateScrubBlock = ExtractTextBetween(
            sourceText,
            "private void HandleUpdateScrubCommand(",
            "    private void HandleEndScrubCommand(");
        AssertContains(updateScrubBlock, "cmd = cmd with { Position = ClampPosition(cmd.Position, frozenValidStart) };\n        if (ShouldYieldScrubUpdateToQueuedControl(commandChannel))");
        AssertContains(updateScrubBlock, "decoder ??= CreateDecoder();\n        EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(cmd.Position, frozenValidStart));");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_TimestampArithmeticIsSaturating()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();

        AssertContains(sourceText, "private static TimeSpan SaturatingAdd(TimeSpan left, TimeSpan right)");
        AssertContains(sourceText, "private static TimeSpan SaturatingSubtract(TimeSpan left, TimeSpan right)");
        AssertContains(sourceText, "if (rightTicks > 0 && leftTicks > long.MaxValue - rightTicks)");
        AssertContains(sourceText, "if (rightTicks < 0 && leftTicks < long.MinValue - rightTicks)");
        AssertContains(sourceText, "if (rightTicks < 0 && leftTicks > long.MaxValue + rightTicks)");
        AssertContains(sourceText, "if (rightTicks > 0 && leftTicks < long.MinValue + rightTicks)");
        AssertDoesNotContain(sourceText, "cmd.Position + frozenValidStart");
        AssertDoesNotContain(sourceText, "PlaybackPosition + frozenValidStart");
        AssertDoesNotContain(sourceText, "PlaybackPosition + cmd.Delta");
        AssertDoesNotContain(sourceText, "bufferPosition + validStartPts");
        AssertDoesNotContain(sourceText, "pos + frozenValidStart");
        AssertDoesNotContain(sourceText, "nudgeFrame.Pts - frozenValidStart");
        AssertDoesNotContain(sourceText, "frame.Pts - validStartPts");
        AssertDoesNotContain(sourceText, "videoFrame.Pts - frozenValidStart");
        AssertDoesNotContain(sourceText, "latestAbsPts - lastFrameAbsPts");
        AssertDoesNotContain(sourceText, "absoluteLatestPts - absoluteFramePts");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_EndOfSegmentOpenFailuresSnapLive()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var segmentSwitchText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "return HandleEndOfSegment(decoder, commandChannel, pacingStopwatch, frozenValidStart, ref fileOpen, cancellationToken);");
        AssertContains(sourceText, "TimeSpan frozenValidStart,\n        ref bool fileOpen,\n        CancellationToken cancellationToken)");
        AssertContains(sourceText, "if (cancellationToken.WaitHandle.WaitOne(50))\n        {\n            return false;\n        }");
        AssertContains(sourceText, "TrySwitchToNextSegment(");
        AssertContains(segmentSwitchText, "Logger.Log($\"FLASHBACK_PLAYBACK_SEGMENT_SWITCH_ERROR path='{nextFile}' type={ex.GetType().Name} msg='{ex.Message}'\");\n            SnapToLiveOnError(decoder, ex, ref fileOpen);\n            return true;");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_FMP4_REOPEN_ERROR path='{currentOpenFilePath}' type={ex.GetType().Name} msg='{ex.Message}'\");\n            SnapToLiveOnError(decoder, ex, ref fileOpen);\n            return false;");
        AssertContains(segmentSwitchText, "if (nextFile == null || IsSamePlaybackPath(nextFile, currentOpenFilePath))");
        AssertContains(segmentSwitchText, "_currentOpenFilePath = nextFile;\n            _decoderHwAccel = decoder.IsD3D11HwAccelerated ? \"D3D11VA\" : \"Software\";");
        AssertContains(sourceText, "ReopenDecoderPlaybackFile(\n                decoder,\n                currentOpenFilePath,\n                ref fileOpen,\n                updateCurrentOpenPath: false,\n                closeOnlyWhenOpen: false);");
        AssertContains(sourceText, "CheckNearLiveEdge(decoder, lastFrameAbsPts, pos, ref fileOpen, requireFrameWarmup: false)");
        AssertOccursBefore(
            sourceText,
            "CheckNearLiveEdge(decoder, lastFrameAbsPts, pos, ref fileOpen, requireFrameWarmup: false)",
            "if (gapFromLive > 2000)");
        AssertOccursBefore(
            sourceText,
            "CheckNearLiveEdge(decoder, lastFrameAbsPts, pos, ref fileOpen, requireFrameWarmup: false)",
            "FLASHBACK_PLAYBACK_WRITE_HEAD_WAIT");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_NormalPlaybackUsesTightNearLiveSnap()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var playbackLoopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");
        var playbackTimingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");
        var playbackSoftwareBudgetText = playbackTimingText;

        AssertContains(sourceText, "private const double ContinuousPlaybackNearLiveSnapFrames = 3.0;");
        AssertContains(sourceText, "private static readonly TimeSpan ContinuousPlaybackNearLiveSnapMinimum = TimeSpan.FromMilliseconds(100);");
        AssertContains(sourceText, "private static readonly TimeSpan RecoveryNearLiveSnapThreshold = TimeSpan.FromMilliseconds(2000);");
        AssertContains(playbackTimingText, "private const double ContinuousPlaybackNearLiveSnapFrames = 3.0;");
        AssertContains(playbackTimingText, "private static readonly TimeSpan ContinuousPlaybackNearLiveSnapMinimum = TimeSpan.FromMilliseconds(100);");
        AssertContains(playbackLoopText, "private static readonly TimeSpan RecoveryNearLiveSnapThreshold = TimeSpan.FromMilliseconds(2000);");
        AssertContains(playbackLoopText, "private bool CheckNearLiveEdge(");
        AssertContains(playbackSoftwareBudgetText, "private const double MaxContinuousSoftwarePlaybackPixelRate = 3840.0 * 2160.0 * 60.0;");
        AssertDoesNotContain(rootText, "private const double ContinuousPlaybackNearLiveSnapFrames = 3.0;");
        AssertDoesNotContain(rootText, "private static readonly TimeSpan RecoveryNearLiveSnapThreshold = TimeSpan.FromMilliseconds(2000);");
        AssertContains(sourceText, "CheckNearLiveEdge(decoder, videoFrame.Pts, newPosition, ref fileOpen)");
        AssertContains(sourceText, "var snapThreshold = requireFrameWarmup\n            ? ResolveContinuousPlaybackNearLiveSnapThreshold()\n            : RecoveryNearLiveSnapThreshold;");
        AssertContains(sourceText, "gapFromLive <= snapThreshold");
        AssertContains(sourceText, "private TimeSpan ResolveContinuousPlaybackNearLiveSnapThreshold()");
        AssertContains(sourceText, "ContinuousPlaybackNearLiveSnapFrames / Math.Min(fps, MaxPlaybackFrameRate)");
        AssertContains(sourceText, "threshold_ms={(long)snapThreshold.TotalMilliseconds}");
        AssertDoesNotContain(sourceText, "gapFromLive <= TimeSpan.FromMilliseconds(2000)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_SnapLiveClearsOpenFileIdentity()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var playbackFramesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");

        var nearLiveBlock = ExtractTextBetween(
            sourceText,
            "Logger.Log($\"FLASHBACK_PLAYBACK_NEAR_LIVE_SNAP",
            "return true;");
        AssertContains(nearLiveBlock, "RestoreLiveAfterNearLiveSnap(decoder, ref fileOpen);");

        var decodeErrorBlock = ExtractTextBetween(
            sourceText,
            "Logger.Log($\"FLASHBACK_PLAYBACK_DECODE_ERROR_STACK",
            "    private bool CheckNearLiveEdge(");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_DECODE_ERROR_SNAP_TO_LIVE type={ex.GetType().Name} error='{ex.Message}'");
        AssertContains(sourceText, "SetLastCommandFailure($\"decode_error:{ex.GetType().Name}{FormatCommandDetail(position: pos)}\");");
        AssertContains(playbackFramesText, "private void SnapToLiveOnError(");
        AssertContains(playbackFramesText, "Logger.Log($\"FLASHBACK_PLAYBACK_DECODE_ERROR_STACK");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_FILE_OPEN_ERROR path='{filePath}' type={ex.GetType().Name} error='{ex.Message}'\");");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_SEEK_ERROR type={ex.GetType().Name} error='{ex.Message}'\");");
        AssertContains(decodeErrorBlock, "RestoreLiveAfterPlaybackDecodeError(decoder, ref fileOpen);");
        AssertContains(playbackFramesText, "private void RestoreLiveAfterPlaybackDecodeError(FlashbackDecoder decoder, ref bool fileOpen)\n        => RestoreLiveAfterDecoderPlaybackFailure(decoder, ref fileOpen, \"decode_error\", resumeRendering: false);");
        AssertContains(playbackFramesText, "private void RestoreLiveAfterNearLiveSnap(FlashbackDecoder decoder, ref bool fileOpen)\n        => RestoreLiveAfterDecoderPlaybackFailure(decoder, ref fileOpen, \"near_live\", resumeRendering: false);");
        AssertContains(playbackFramesText, "CloseDecoderFileBestEffort(decoder, operation);\n        fileOpen = false;\n        _currentOpenFilePath = null;\n        _decoderHwAccel = \"N/A\";");
        AssertContains(playbackFramesText, "ReleasePlaybackFrameForLive(operation);\n        RestoreLiveAudio();");
        AssertContains(playbackFramesText, "SafeResumePreviewSubmission(operation);");
        AssertContains(playbackFramesText, "SetState(FlashbackPlaybackState.Live);");
        AssertContains(sourceText, "private static void CloseDecoderFileBestEffort(FlashbackDecoder decoder, string operation)\n    {\n        try\n        {\n            if (decoder.IsOpen) decoder.CloseFile();\n        }\n        catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_PLAYBACK_DECODER_CLOSE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'\");\n        }\n    }");
        var ensureFileOpenBlock = ExtractTextBetween(
            sourceText,
            "private void EnsureFileOpen",
            "private void CleanupDecoder");
        AssertContains(ensureFileOpenBlock, "CloseDecoderFileBestEffort(decoder, \"ensure_file_open\");\n                fileOpen = false;\n                _currentOpenFilePath = null;\n                _decoderHwAccel = \"N/A\";");
        AssertContains(ensureFileOpenBlock, "if (string.IsNullOrWhiteSpace(filePath))\n        {\n            Logger.Log(\"FLASHBACK_PLAYBACK_NO_FILE\");\n            if (decoder.IsOpen)\n            {\n                CloseDecoderFileBestEffort(decoder, \"ensure_file_open_no_file\");\n            }\n\n            fileOpen = false;\n            _currentOpenFilePath = null;\n            _decoderHwAccel = \"N/A\";\n            return;\n        }");
        AssertContains(ensureFileOpenBlock, "Logger.Log($\"FLASHBACK_PLAYBACK_FILE_OPEN_ERROR path='{filePath}' type={ex.GetType().Name} error='{ex.Message}'\");\n            if (decoder.IsOpen)\n            {\n                CloseDecoderFileBestEffort(decoder, \"ensure_file_open_error\");\n            }\n            _decoderHwAccel = \"N/A\";\n            fileOpen = false;");
        AssertContains(ensureFileOpenBlock, "private static bool IsDecoderFileReady(FlashbackDecoder decoder, bool fileOpen)\n        => fileOpen && decoder.IsOpen;");
        AssertDoesNotContain(sourceText, "EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(cmd.Position, frozenValidStart));\n                        if (!decoder.IsOpen)");
        AssertDoesNotContain(sourceText, "EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(PlaybackPosition, frozenValidStart));\n                        if (!decoder.IsOpen)");
        AssertDoesNotContain(sourceText, "EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(nudgedPos, frozenValidStart));\n                        if (!decoder.IsOpen)");
        AssertEqual(6, sourceText.Split("if (!IsDecoderFileReady(decoder, fileOpen))", StringSplitOptions.None).Length - 1, "All EnsureFileOpen callers gate on fileOpen and decoder.IsOpen");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_PauseFromLive_DisplaysBufferedFrameBeforePaused()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var publicPauseBlock = ExtractTextBetween(
            sourceText,
            "public bool Pause()",
            "    public bool GoLive()");

        var pauseFromLiveBlock = ExtractTextBetween(
            sourceText,
            "else if (State == FlashbackPlaybackState.Live)",
            "    private void HandleNudgeCommand(");

        AssertContains(publicPauseBlock, "return SendCommand(new PlaybackCommand { Kind = CommandKind.Pause });");
        AssertDoesNotContain(publicPauseBlock, "SeekAndDisplay");
        AssertContains(pauseFromLiveBlock, "SafeSuppressPreviewSubmission(\"pause_from_live\");");
        AssertContains(pauseFromLiveBlock, "SafePauseRendering(\"pause_from_live\");");
        AssertContains(pauseFromLiveBlock, "var pauseTarget = ResolvePauseFromLiveTarget(frozenValidStart);");
        AssertContains(pauseFromLiveBlock, "EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(pausePos, frozenValidStart));");
        AssertContains(pauseFromLiveBlock, "if (!IsDecoderFileReady(decoder, fileOpen))");
        AssertContains(pauseFromLiveBlock, "SetNoFileFailure(CommandKind.Pause, pausePos);");
        AssertContains(pauseFromLiveBlock, "if (ShouldYieldPauseFromLiveToQueuedSeekOrPlay(commandChannel))");
        AssertContains(pauseFromLiveBlock, "FLASHBACK_PLAYBACK_PAUSE_FROM_LIVE_DEFER_DISPLAY");
        AssertContains(pauseFromLiveBlock, "if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, pausePos, frozenValidStart, CommandKind.Pause, cts.Token))");
        AssertContains(pauseFromLiveBlock, "RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, \"pause_from_live_display_failed\");");
        AssertContains(pauseFromLiveBlock, "pendingExactResumeTarget = SaturatingAdd(PlaybackPosition, frozenValidStart);");
        AssertContains(pauseFromLiveBlock, "SetState(FlashbackPlaybackState.Paused);");
        AssertContains(pauseFromLiveBlock, "frozen_frame=true");
        AssertContains(sourceText, "private TimeSpan ResolvePauseFromLiveTarget(TimeSpan frozenValidStart)");
        AssertContains(sourceText, "var backoff = TimeSpan.FromSeconds(1.0 / fps);");
        AssertContains(sourceText, "return latestPts - backoff;");
        AssertDoesNotContain(pauseFromLiveBlock, "SeekAndDisplayExactFrame");
        AssertDoesNotContain(sourceText, "private void SeekAndDisplayExactFrame");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_NudgeCreatesDecoderWhenPaused()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();

        var nudgeBlock = ExtractTextBetween(
            sourceText,
            "private void HandleNudgeCommand(",
            "\n}");

        AssertContains(nudgeBlock, "decoder ??= CreateDecoder();");
        AssertContains(nudgeBlock, "EnsureFileOpen(decoder, ref fileOpen, SaturatingAdd(nudgedPos, frozenValidStart));");
        AssertContains(nudgeBlock, "if (!IsDecoderFileReady(decoder, fileOpen))");
        AssertContains(nudgeBlock, "FLASHBACK_PLAYBACK_NUDGE_NO_FILE");
        AssertContains(nudgeBlock, "ReleasePlaybackFrameForLive(\"nudge_no_file\");");
        AssertContains(nudgeBlock, "RestoreLiveAudio();");
        AssertContains(nudgeBlock, "SafeResumePreviewSubmission(\"nudge_no_file\");");
        AssertContains(nudgeBlock, "SafeResumeRendering(\"nudge_no_file\");");
        AssertContains(nudgeBlock, "SetState(FlashbackPlaybackState.Live);");
        AssertContains(nudgeBlock, "if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, nudgedPos, frozenValidStart, CommandKind.Nudge, cts.Token))");
        AssertContains(nudgeBlock, "RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, \"nudge_display_failed\");");
        AssertDoesNotContain(nudgeBlock, "if (decoder != null)");

        return Task.CompletedTask;
    }
}
