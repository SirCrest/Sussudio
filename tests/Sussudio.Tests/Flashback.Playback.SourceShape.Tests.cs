using System.Reflection;
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

    internal static Task FlashbackPlaybackController_InitialState_IsLive()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;

        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController constructor not found.");

        var controller = ctor.Invoke(new[] { bufferManager });

        var stateStr = GetPropertyValue(controller, "State")?.ToString();
        AssertEqual("Live", stateStr, "Initial state is Live");

        var position = (TimeSpan)GetPropertyValue(controller, "PlaybackPosition")!;
        AssertEqual(TimeSpan.Zero, position, "Initial PlaybackPosition");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_CommandsNoOpBeforeInitialize()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;

        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)!;
        var controller = ctor.Invoke(new[] { bufferManager });

        var playMethod = controllerType.GetMethod("Play", BindingFlags.Public | BindingFlags.Instance);
        var pauseMethod = controllerType.GetMethod("Pause", BindingFlags.Public | BindingFlags.Instance);
        var goLiveMethod = controllerType.GetMethod("GoLive", BindingFlags.Public | BindingFlags.Instance);

        playMethod?.Invoke(controller, null);
        pauseMethod?.Invoke(controller, null);
        goLiveMethod?.Invoke(controller, null);

        var stateStr = GetPropertyValue(controller, "State")?.ToString();
        AssertEqual("Live", stateStr, "State unchanged after no-op commands");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_SuccessfulNoOps_ClearStaleCommandFailure()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController constructor not found.");
        var controller = ctor.Invoke(new[] { bufferManager });
        SetPrivateField(controller, "_initialized", true);

        try
        {
            SeedCommandFailure(controller, "old_failure:EndScrub");
            AssertEqual(false, (bool)controllerType.GetMethod("EndScrub")!.Invoke(controller, null)!, "EndScrub live/no-thread no-op reports failure");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "EndScrub no-op clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "EndScrub no-op clears stale failure UTC");

            SeedCommandFailure(controller, "old_failure:GoLive");
            AssertEqual(false, (bool)controllerType.GetMethod("GoLive")!.Invoke(controller, null)!, "GoLive live/no-thread no-op reports failure");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "GoLive no-op clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "GoLive no-op clears stale failure UTC");

            SeedCommandFailure(controller, "old_failure:Nudge");
            AssertEqual(false, (bool)controllerType.GetMethod("NudgePosition")!.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(8.33) })!, "Nudge live/no-thread no-op reports failure");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "Nudge no-op clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "Nudge no-op clears stale failure UTC");
        }
        finally
        {
            (controller as IDisposable)?.Dispose();
            (bufferManager as IDisposable)?.Dispose();
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_CoalescedCommands_ClearStaleCommandFailure()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;
        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        var ctor = controllerType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: new[] { bufferManagerType },
            modifiers: null)
            ?? throw new InvalidOperationException("FlashbackPlaybackController constructor not found.");
        var controller = ctor.Invoke(new[] { bufferManager });
        var sendSeek = controllerType.GetMethod("SendSeekCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendSeekCommand not found.");
        var sendUpdateScrub = controllerType.GetMethod("SendUpdateScrubCommand", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SendUpdateScrubCommand not found.");

        try
        {
            AssertEqual(true, (bool)sendSeek.Invoke(controller, new object[] { TimeSpan.FromSeconds(1) })!, "Initial seek enqueues");
            var initialSeekQueuedUtc = GetLongProperty(controller, "LastCommandQueuedUtcUnixMs");
            SeedCommandFailure(controller, "old_failure:Seek");
            AssertEqual(true, (bool)sendSeek.Invoke(controller, new object[] { TimeSpan.FromSeconds(2) })!, "Coalesced seek succeeds");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "Coalesced seek clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "Coalesced seek clears stale failure UTC");
            AssertEqual("Seek", GetStringProperty(controller, "LastCommandQueued"), "Coalesced seek keeps physical queued-command name");
            AssertEqual(initialSeekQueuedUtc, GetLongProperty(controller, "LastCommandQueuedUtcUnixMs"), "Coalesced seek does not refresh queued-command timestamp");
            AssertEqual(1L, GetLongProperty(controller, "SeekCommandsCoalesced"), "Coalesced seek counter");

            AssertEqual(true, (bool)sendUpdateScrub.Invoke(controller, new object[] { TimeSpan.FromSeconds(3) })!, "Initial scrub update enqueues");
            var initialScrubQueuedUtc = GetLongProperty(controller, "LastCommandQueuedUtcUnixMs");
            SeedCommandFailure(controller, "old_failure:UpdateScrub");
            AssertEqual(true, (bool)sendUpdateScrub.Invoke(controller, new object[] { TimeSpan.FromSeconds(4) })!, "Coalesced scrub update succeeds");
            AssertEqual(string.Empty, GetStringProperty(controller, "LastCommandFailure"), "Coalesced scrub update clears stale failure");
            AssertEqual(0L, GetLongProperty(controller, "LastCommandFailureUtcUnixMs"), "Coalesced scrub update clears stale failure UTC");
            AssertEqual("UpdateScrub", GetStringProperty(controller, "LastCommandQueued"), "Coalesced scrub update keeps physical queued-command name");
            AssertEqual(initialScrubQueuedUtc, GetLongProperty(controller, "LastCommandQueuedUtcUnixMs"), "Coalesced scrub update does not refresh queued-command timestamp");
            AssertEqual(1L, GetLongProperty(controller, "ScrubUpdatesCoalesced"), "Coalesced scrub update counter");
        }
        finally
        {
            (controller as IDisposable)?.Dispose();
            (bufferManager as IDisposable)?.Dispose();
        }

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

    internal static Task FlashbackPlaybackController_PlaybackTransitions_UseBestEffortAudioPreviewGuards()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var metricsCollectionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var audioRoutingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.AudioRouting.cs")
            .Replace("\r\n", "\n");
        var audioCallbackText = audioRoutingText;
        var audioPreviewGuardsText = audioRoutingText;
        var audioPrebufferText = audioRoutingText;
        var audioMasterText = audioRoutingText;
        var audioMasterFallbacksText = audioMasterText;
        var playbackTimingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");
        var playbackSoftwareBudgetText = playbackTimingText;
        var playbackSegmentSwitchText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");
        var audioMasterClockText = audioMasterText;
        var wasapiPlaybackText = ReadRepoFile("Sussudio/Services/Audio/WasapiAudioPlayback.cs")
            .Replace("\r\n", "\n");
        var wasapiPlaybackRenderText = wasapiPlaybackText;

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackPlaybackController.AudioMasterPacing.cs")),
            "audio-master pacing folded into FlashbackPlaybackController.AudioRouting.cs");

        AssertContains(sourceText, "private void SafeSuppressPreviewSubmission(string operation)");
        AssertContains(sourceText, "private void SafeResumePreviewSubmission(string operation)");
        AssertContains(sourceText, "private void SafePauseRendering(string operation)");
        AssertContains(sourceText, "private void SafeResumeRendering(string operation)");
        AssertContains(sourceText, "private void SafeResumePlaybackRendering(string operation)");
        AssertContains(sourceText, "private void SafeFlushPlayback(string operation)");
        AssertContains(audioPreviewGuardsText, "private void SafeSuppressPreviewSubmission(string operation)");
        AssertContains(audioPreviewGuardsText, "private void SafeResumePreviewSubmission(string operation)");
        AssertContains(audioPreviewGuardsText, "private void SafePauseRendering(string operation)");
        AssertContains(audioPreviewGuardsText, "private void SafeResumeRendering(string operation)");
        AssertContains(audioPreviewGuardsText, "private void SafeResumePlaybackRendering(string operation)");
        AssertContains(audioPreviewGuardsText, "private void SafeFlushPlayback(string operation)");
        AssertContains(sourceText, "private const double PlaybackAudioPrebufferTargetMs = 180.0;");
        AssertContains(sourceText, "private const double PlaybackAudioPrebufferDiscardThresholdMs = 250.0;");
        AssertContains(sourceText, "private const int PlaybackAudioPrebufferTimeoutMs = 1000;");
        AssertContains(sourceText, "private const int PlaybackAudioPrebufferRetryDelayMs = 20;");
        AssertContains(sourceText, "private const int PlaybackAudioPrebufferDecodeFrameBudget = 96;");
        AssertContains(audioPrebufferText, "private const double PlaybackAudioPrebufferTargetMs = 180.0;");
        AssertContains(audioPrebufferText, "private const double PlaybackAudioPrebufferDiscardThresholdMs = 250.0;");
        AssertContains(audioPrebufferText, "private const int PlaybackAudioPrebufferTimeoutMs = 1000;");
        AssertContains(audioPrebufferText, "private const int PlaybackAudioPrebufferRetryDelayMs = 20;");
        AssertContains(audioPrebufferText, "private const int PlaybackAudioPrebufferDecodeFrameBudget = 96;");
        AssertDoesNotContain(rootText, "private const double PlaybackAudioPrebufferTargetMs = 180.0;");
        AssertDoesNotContain(rootText, "private const int PlaybackAudioPrebufferDecodeFrameBudget = 96;");
        AssertContains(sourceText, "var prebufferedFrames = new Queue<DecodedVideoFrame>();");
        AssertContains(sourceText, "ClearPrebufferedFrames(prebufferedFrames, $\"command_{cmd.Kind}\");");
        AssertContains(sourceText, "private void PrimePlaybackAudioBuffer(");
        AssertContains(sourceText, "TimeSpan resumeTarget,");
        AssertContains(sourceText, "while (decodedFrames < PlaybackAudioPrebufferDecodeFrameBudget)");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(frame, $\"audio_prebuffer_{operation}\");");
        AssertContains(sourceText, "released_frames={prebufferReleasedFrames}");
        AssertDoesNotContain(sourceText, "prebufferedFrames.Enqueue(frame);");
        AssertContains(sourceText, "cancellationToken.WaitHandle.WaitOne(waitMs)");
        AssertContains(sourceText, "bufferedMs > PlaybackAudioPrebufferDiscardThresholdMs");
        AssertContains(sourceText, "rewound = TryRewindPlaybackAudioPrebuffer(decoder, ref fileOpen, resumeTarget, operation, cancellationToken);");
        AssertContains(sourceText, "private bool TryRewindPlaybackAudioPrebuffer(");
        AssertContains(sourceText, "TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, resumeTarget, $\"prebuffer_discard_{operation}\", cancellationToken)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_PREBUFFER_REWIND operation={operation}");
        AssertContains(sourceText, "ClearPrebufferedFrames(prebufferedFrames, $\"prebuffer_discard_{operation}\");");
        AssertContains(sourceText, "eof_retries={eofRetries}");
        AssertContains(sourceText, "rewound={rewound}");
        AssertDoesNotContain(sourceText, "if ((reachedEnd && decodedFrames > 0) ||");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_PREBUFFER operation={operation}");
        AssertContains(sourceText, "SafeResumePlaybackRendering(\"seek_resume\")");
        AssertContains(sourceText, "SafeResumePlaybackRendering(\"end_scrub_resume\")");
        AssertContains(sourceText, "SafeResumePlaybackRendering(\"play\")");
        AssertContains(sourceText, "ApplyAudioRoutingForState(\"audio_update\");");
        AssertContains(sourceText, "private void ApplyAudioRoutingForState(string operation)");
        AssertContains(sourceText, "case FlashbackPlaybackState.Live:\n                RestoreLiveAudio();");
        AssertContains(sourceText, "case FlashbackPlaybackState.Playing:\n                SuppressLiveAudio();\n                SafeResumeRendering(operation);");
        AssertContains(sourceText, "case FlashbackPlaybackState.Paused:\n            case FlashbackPlaybackState.Scrubbing:\n                SuppressLiveAudio();\n                SafePauseRendering(operation);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_WARN");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_WARN");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PREVIEW_WARN op=suppress operation={operation} type={ex.GetType().Name}");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_WARN op=pause operation={operation} type={ex.GetType().Name}");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_WARN op=flush operation={operation} type={ex.GetType().Name}");
        AssertContains(sourceText, "SafeSuppressPreviewSubmission(\"begin_scrub\")");
        AssertContains(sourceText, "SafeResumePreviewSubmission(\"scrub_no_file\")");
        AssertContains(sourceText, "RestoreLiveForPlaybackThreadExit(ref decoder, ref fileOpen, \"go_live\")");
        AssertContains(sourceText, "SafeResumePreviewSubmission(operation);");
        AssertContains(sourceText, "RestoreLiveAfterPlaybackDecodeError(decoder, ref fileOpen);");
        AssertContains(sourceText, "SafeFlushPlayback(\"restore_live_audio\")");
        AssertContains(sourceText, "SafeResumeRendering(\"play_no_file\")");
        AssertContains(sourceText, "SafeResumeRendering(\"nudge_no_file\")");
        AssertContains(sourceText, "if (_audioPlayback == null)\n        {\n            decoder.AudioChunkCallback = null;\n            return;\n        }");
        AssertContains(sourceText, "if (!TryValidatePlaybackAudioChunk(chunk, out var invalidReason))");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_DROP reason={invalidReason}");
        AssertContains(sourceText, "ReturnPlaybackAudioChunkBestEffort(chunk, $\"playback_audio_{invalidReason}\");");
        AssertContains(sourceText, "private static bool TryValidatePlaybackAudioChunk(DecodedAudioChunk chunk, out string reason)");
        AssertContains(audioCallbackText, "private void RestoreAudioCallback(FlashbackDecoder decoder, long audioStartGateTicks = 0)");
        AssertContains(audioCallbackText, "private static bool TryValidatePlaybackAudioChunk(DecodedAudioChunk chunk, out string reason)");
        AssertContains(audioCallbackText, "private static void ReturnPlaybackAudioChunkBestEffort(DecodedAudioChunk chunk, string operation)");
        AssertContains(audioRoutingText, "private void RestoreAudioCallback(FlashbackDecoder decoder, long audioStartGateTicks = 0)");
        AssertContains(audioRoutingText, "private static bool TryValidatePlaybackAudioChunk(DecodedAudioChunk chunk, out string reason)");
        AssertContains(audioRoutingText, "private static void ReturnPlaybackAudioChunkBestEffort(DecodedAudioChunk chunk, string operation)");
        AssertContains(sourceText, "reason = \"length_exceeds_buffer\";");
        AssertContains(sourceText, "reason = \"unaligned_length\";");
        AssertContains(sourceText, "private static void ReturnPlaybackAudioChunkBestEffort(DecodedAudioChunk chunk, string operation)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_AUDIO_RETURN_WARN");
        AssertContains(sourceText, "ReturnPlaybackAudioChunkBestEffort(chunk, \"playback_audio_non_monotonic_pts\");");
        AssertContains(sourceText, "ReturnPlaybackAudioChunkBestEffort(chunk, \"playback_audio_before_gate\");");
        AssertContains(sourceText, "pb.EnqueuePooledSamples(chunk.Samples, chunk.ValidLength, chunk.Pts.Ticks);");
        AssertContains(sourceText, "private const double MaxContinuousSoftwarePlaybackPixelRate = 3840.0 * 2160.0 * 60.0;");
        AssertContains(playbackSoftwareBudgetText, "private const double MaxContinuousSoftwarePlaybackPixelRate = 3840.0 * 2160.0 * 60.0;");
        AssertDoesNotContain(rootText, "private const double MaxContinuousSoftwarePlaybackPixelRate = 3840.0 * 2160.0 * 60.0;");
        AssertContains(playbackSoftwareBudgetText, "private bool TrySnapLiveForSoftwarePlaybackBudget(FlashbackDecoder decoder, ref bool fileOpen, string operation)");
        AssertContains(playbackSoftwareBudgetText, "private bool ShouldSnapLiveForSoftwarePlaybackBudget(");
        AssertContains(playbackSoftwareBudgetText, "private void SnapLiveForSoftwarePlaybackBudget(FlashbackDecoder decoder, ref bool fileOpen, string operation)");
        AssertContains(playbackSoftwareBudgetText, "private void UpdateDecoderHwAccel(FlashbackDecoder decoder)");
        AssertContains(sourceText, "private bool TrySnapLiveForSoftwarePlaybackBudget(FlashbackDecoder decoder, ref bool fileOpen, string operation)");
        AssertContains(sourceText, "private bool ShouldSnapLiveForSoftwarePlaybackBudget(");
        AssertContains(sourceText, "GpuDecodeEnabled &&\n               !decoder.IsD3D11HwAccelerated &&\n               pixelRate > MaxContinuousSoftwarePlaybackPixelRate");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SOFTWARE_DECODE_SNAP_TO_LIVE");
        AssertContains(sourceText, "SetLastCommandFailure($\"software_decode_over_budget:{operation}{FormatCommandDetail(position: pos)}\");");
        AssertContains(sourceText, "RestoreLiveAfterSoftwarePlaybackBudgetSnap(decoder, ref fileOpen, operation);");
        AssertContains(sourceText, "TrySnapLiveForSoftwarePlaybackBudget(decoder, ref fileOpen, \"play\")");
        AssertContains(sourceText, "SnapLiveForSoftwarePlaybackBudget(decoder, ref fileOpen, \"playback_decode\");");
        AssertContains(sourceText, "private void UpdateDecoderHwAccel(FlashbackDecoder decoder)");
        AssertContains(sourceText, "const double MaxAudioMasterCorrectionMs = 250.0;");
        AssertContains(sourceText, "const double syncThresholdMs = 100.0;");
        AssertContains(sourceText, "private string _pendingAudioMasterFallbackReason = string.Empty;");
        AssertContains(audioMasterClockText, "private long _audioClockPtsTicks;");
        AssertContains(audioMasterClockText, "private long _audioClockWallTicks;");
        AssertContains(audioMasterClockText, "private const long AudioMasterClockStaleThresholdTicks = TimeSpan.TicksPerMillisecond * 200;");
        AssertContains(audioMasterClockText, "private void RefreshAudioMasterClock()");
        AssertContains(audioMasterClockText, "private bool TryGetFreshAudioMasterClock(");
        AssertContains(audioMasterClockText, "private bool TryComputeAudioMasterDriftMs(long videoPtsTicks, out double driftMs)");
        AssertContains(audioMasterClockText, "public double AvDriftMs");
        AssertContains(audioMasterClockText, "var renderingPts = _audioPlayback?.RenderingPtsTicks ?? 0;");
        AssertContains(audioMasterClockText, "return TimeSpan.FromTicks(renderingPts - videoPts).TotalMilliseconds;");
        AssertDoesNotContain(metricsCollectionText, "public double AvDriftMs");
        AssertDoesNotContain(metricsCollectionText, "RenderingPtsTicks");
        AssertContains(audioMasterText, "public long PlaybackAudioMasterDelayDoubles => Interlocked.Read(ref _playbackAudioMasterDelayDoubles);");
        AssertContains(audioMasterText, "public long PlaybackAudioMasterDelayShrinks => Interlocked.Read(ref _playbackAudioMasterDelayShrinks);");
        AssertContains(audioMasterFallbacksText, "private long _playbackAudioMasterFallbacks;");
        AssertContains(audioMasterFallbacksText, "private long _playbackAudioMasterUnavailableFallbacks;");
        AssertContains(audioMasterFallbacksText, "private long _playbackAudioMasterStaleFallbacks;");
        AssertContains(audioMasterFallbacksText, "private long _playbackAudioMasterDriftOutlierFallbacks;");
        AssertContains(audioMasterFallbacksText, "private string _playbackAudioMasterLastFallbackReason = string.Empty;");
        AssertContains(audioMasterFallbacksText, "private string _pendingAudioMasterFallbackReason = string.Empty;");
        AssertContains(audioMasterFallbacksText, "public long PlaybackAudioMasterFallbacks => Interlocked.Read(ref _playbackAudioMasterFallbacks);");
        AssertContains(audioMasterFallbacksText, "public long PlaybackAudioMasterUnavailableFallbacks => Interlocked.Read(ref _playbackAudioMasterUnavailableFallbacks);");
        AssertContains(audioMasterFallbacksText, "public string PlaybackAudioMasterLastFallbackReason => Volatile.Read(ref _playbackAudioMasterLastFallbackReason);");
        AssertContains(audioMasterFallbacksText, "public double PlaybackAudioMasterLastFallbackClockAgeMs => _playbackAudioMasterLastFallbackClockAgeMs;");
        AssertContains(audioMasterFallbacksText, "private void RecordAudioMasterFallback(string reason, double driftMs, long clockAgeTicks)");
        AssertContains(audioMasterFallbacksText, "private static bool IsTransientAudioMasterFallbackCandidate(string reason)");
        AssertContains(audioMasterFallbacksText, "private void CommitPendingAudioMasterFallback()");
        AssertContains(audioMasterFallbacksText, "private void CommitAudioMasterFallback(string reason, double driftMs, long clockAgeTicks)");
        AssertDoesNotContain(metricsCollectionText, "public long PlaybackAudioMasterDelayDoubles =>");
        AssertDoesNotContain(metricsCollectionText, "public long PlaybackAudioMasterFallbacks =>");
        AssertDoesNotContain(metricsCollectionText, "public string PlaybackAudioMasterLastFallbackReason =>");
        AssertDoesNotContain(rootText, "private long _audioClockPtsTicks;");
        AssertDoesNotContain(rootText, "private long _playbackAudioMasterFallbacks;");
        AssertDoesNotContain(rootText, "private string _pendingAudioMasterFallbackReason = string.Empty;");
        AssertContains(sourceText, "private static bool IsTransientAudioMasterFallbackCandidate(string reason)");
        AssertContains(sourceText, "string.Equals(reason, \"unavailable\", StringComparison.Ordinal)");
        AssertContains(sourceText, "string.Equals(reason, \"stale-clock\", StringComparison.Ordinal)");
        AssertContains(sourceText, "string.Equals(reason, \"drift-outlier\", StringComparison.Ordinal)");
        AssertContains(sourceText, "ClearPendingAudioMasterFallback();");
        AssertContains(sourceText, "CommitPendingAudioMasterFallback();");
        AssertContains(sourceText, "CommitAudioMasterFallback(");
        AssertContains(sourceText, "if (Math.Abs(diffMs) > MaxAudioMasterCorrectionMs)\n            {\n                // WASAPI render PTS can lag decoded video by the endpoint buffer/device");
        AssertContains(sourceText, "WallClockPace(pacingStopwatch, frameDuration);\n                return;");
        AssertContains(sourceText, "PrimePlaybackAudioBuffer(decoder, prebufferedFrames, ref fileOpen, coalescedSeekTarget, \"seek_resume\", cts.Token);");
        AssertContains(sourceText, "SafeResumePlaybackRendering(\"seek_resume\");");
        AssertContains(sourceText, "PrimePlaybackAudioBuffer(decoder, prebufferedFrames, ref fileOpen, endScrubTarget, \"end_scrub_resume\", cts.Token);");
        AssertContains(sourceText, "SafeResumePlaybackRendering(\"end_scrub_resume\");");
        AssertContains(sourceText, "PrimePlaybackAudioBuffer(decoder, prebufferedFrames, ref fileOpen, seekTarget, \"play\", cts.Token);");
        AssertContains(sourceText, "SafeResumePlaybackRendering(\"play\");");
        AssertContains(sourceText, "private void ResetPlaybackPtsCadenceBaseline()");
        AssertContains(playbackSegmentSwitchText, "ResetPlaybackPtsCadenceBaseline();\n            pacingStopwatch.Restart();\n            playbackContinues = true;\n            return true;");
        AssertContains(sourceText, "if (string.IsNullOrEmpty(_pendingAudioMasterFallbackReason))");
        AssertContains(sourceText, "_pendingAudioMasterFallbackReason = reason;");
        AssertContains(sourceText, "CommitAudioMasterFallback(");
        AssertContains(sourceText, "_pendingAudioMasterFallbackReason,");
        AssertContains(sourceText, "var correctionMs = Math.Min(diffMs - syncThresholdMs, Math.Min(0.1, nominalDelayMs * 0.02));");
        AssertContains(sourceText, "adjustedDelayMs = nominalDelayMs + Math.Max(0, correctionMs);");
        AssertContains(sourceText, "var correctionMs = Math.Min(-diffMs - syncThresholdMs, Math.Min(0.1, nominalDelayMs * 0.02));");
        AssertContains(sourceText, "adjustedDelayMs = Math.Max(0, nominalDelayMs - Math.Max(0, correctionMs));");
        AssertDoesNotContain(sourceText, "adjustedDelayMs = nominalDelayMs * 2;");
        AssertDoesNotContain(sourceText, "adjustedDelayMs = Math.Max(0, nominalDelayMs + diffMs);");
        AssertContains(wasapiPlaybackText, "if (Volatile.Read(ref _renderingPaused) != 0 && !_resumeRequested) return;");
        AssertContains(wasapiPlaybackText, "_resumeRequested = false;\n        _pauseRequested = true;");
        AssertContains(wasapiPlaybackText, "if (Volatile.Read(ref _renderingPaused) == 0 && !_pauseRequested) return;");
        AssertContains(wasapiPlaybackText, "public void ResumeRendering(double prebufferMs = 0, int prebufferTimeoutMs = 0)");
        AssertContains(wasapiPlaybackText, "Volatile.Write(ref _resumePrebufferFrames, Math.Max(0, prebufferFrames));");
        AssertContains(wasapiPlaybackText, "_resumeRequested = true;\n        _renderEvent?.Set();");
        AssertContains(wasapiPlaybackRenderText, "internal sealed class WasapiAudioPlayback : IDisposable");
        AssertContains(wasapiPlaybackRenderText, "private void RenderThreadMain()");
        AssertContains(wasapiPlaybackRenderText, "if (!_resumeRequested)\n                {\n                    continue;\n                }");
        AssertContains(wasapiPlaybackRenderText, "WASAPI_PLAYBACK_RENDER_RESUME_CANCELED_PENDING_PAUSE");
        AssertContains(wasapiPlaybackRenderText, "WaitForResumePrebuffer();");
        AssertContains(wasapiPlaybackRenderText, "WASAPI_PLAYBACK_RENDER_PREBUFFER target_ms={FramesToMilliseconds(targetFrames):F1}");
        AssertContains(wasapiPlaybackRenderText, "private int PlaybackBufferedFramesForResume()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioPlayback.RenderThread.cs")),
            "WASAPI playback render thread folded into the playback lifecycle root");
        AssertDoesNotContain(wasapiPlaybackText, "public void ResumeRendering()\n    {\n        if (Volatile.Read(ref _started) == 0) return;\n        if (Volatile.Read(ref _renderingPaused) == 0 && !_pauseRequested) return;\n\n        _pauseRequested = false;");
        AssertDoesNotContain(wasapiPlaybackRenderText, "GetCurrentPadding(pre-fill)");
        AssertDoesNotContain(wasapiPlaybackRenderText, "IAudioRenderClient.GetBuffer(pre-fill)");
        AssertDoesNotContain(wasapiPlaybackRenderText, "AUDCLNT_BUFFERFLAGS_SILENT");
        AssertDoesNotContain(wasapiPlaybackRenderText, "WASAPI_PREFILL_WARN");
        AssertContains(wasapiPlaybackText, "private int _playbackQueueDepth;");
        AssertContains(wasapiPlaybackText, "public int PlaybackQueueDepth => Math.Max(0, Volatile.Read(ref _playbackQueueDepth));");
        AssertContains(wasapiPlaybackText, "internal void EnqueuePooledSamples(byte[] pooledBuffer, int validLength, long ptsTicks = 0)");
        AssertContains(wasapiPlaybackText, "if (TryWriteChunk(chunk)) return;");
        AssertContains(wasapiPlaybackText, "private bool TryWriteChunk(PlaybackChunk chunk)");
        AssertContains(wasapiPlaybackText, "Interlocked.Increment(ref _playbackQueueDepth);\n        if (_sampleQueue.Writer.TryWrite(chunk))");
        AssertContains(wasapiPlaybackText, "DecrementPlaybackQueueDepth();\n        return false;");
        AssertContains(wasapiPlaybackText, "private bool TryDequeueChunk(out PlaybackChunk chunk)");
        AssertContains(wasapiPlaybackText, "DecrementPlaybackQueueDepth();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Audio", "WasapiAudioPlayback.Queue.cs")),
            "WASAPI playback queue state stays folded into the lifecycle root");
        AssertContains(wasapiPlaybackText, "private const int OutputSampleRate = 48000;");
        AssertContains(wasapiPlaybackText, "private const uint MaxRenderWriteFrames = OutputSampleRate / 50; // 20ms");
        AssertContains(wasapiPlaybackRenderText, "var framesToWrite = Math.Min(_bufferFrameCount - paddingFrames, MaxRenderWriteFrames);");
        AssertDoesNotContain(wasapiPlaybackRenderText, "var framesToWrite = _bufferFrameCount - paddingFrames;");
        AssertContains(wasapiPlaybackRenderText, "UpdateRenderingPtsForActiveChunk();");
        AssertContains(wasapiPlaybackRenderText, "var frameOffset = Math.Max(0, _activeChunkOffset) / OutputBlockAlign;");
        AssertContains(wasapiPlaybackRenderText, "var offsetTicks = frameOffset * TimeSpan.TicksPerSecond / OutputSampleRate;");
        AssertDoesNotContain(wasapiPlaybackText, "_sampleQueue.Reader.Count");
        AssertDoesNotContain(wasapiPlaybackText, "_sampleQueue.Writer.TryWrite(chunk))\n        {\n            Interlocked.Increment(ref _playbackQueueDepth);");
        AssertDoesNotContain(sourceText, "_videoCapture?.SuppressPreviewSubmission();\n                        SuppressLiveAudio();\n                        _audioPlayback?.PauseRendering();");

        return Task.CompletedTask;
    }
}
