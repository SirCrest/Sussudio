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

    internal static Task FlashbackPlaybackController_Fmp4ReopenRetriesAreGuarded()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var segmentEdgesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");
        var segmentSwitchText = segmentEdgesText;
        var positioningText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.Positioning.cs")
            .Replace("\r\n", "\n");
        var decoderReopenText = positioningText;
        var decoderSegmentReopenText = segmentEdgesText;
        var seekDisplayText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");
        var metricsCollectionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var seekCapTelemetryText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "private bool TryReopenCurrentFileAndSeek(");
        AssertContains(decoderSegmentReopenText, "private bool TryReopenCurrentFmp4BeforeSegmentSwitch(");
        AssertContains(decoderSegmentReopenText, "private bool HandleActiveFmp4ReopenAtSegmentEdge(");
        AssertContains(segmentSwitchText, "TryReopenCurrentFmp4BeforeSegmentSwitch(");
        AssertContains(segmentEdgesText, "TrySwitchToNextSegment(");
        AssertContains(segmentEdgesText, "return HandleActiveFmp4ReopenAtSegmentEdge(");
        AssertContains(segmentSwitchText, "FLASHBACK_PLAYBACK_SEGMENT_SWITCH_ERROR");
        AssertContains(segmentSwitchText, "FLASHBACK_PLAYBACK_SEGMENT_SWITCH_SEEK_FAIL");
        AssertContains(sourceText, "private bool TryReopenCurrentFileAndSeekKeyframe(");
        AssertContains(sourceText, "private static readonly TimeSpan ActiveFmp4ReopenNearLiveGuard = TimeSpan.FromMilliseconds(250);");
        AssertContains(sourceText, "private static readonly TimeSpan AdjacentSegmentSeekFallbackWindow = TimeSpan.FromSeconds(3);");
        AssertContains(decoderReopenText, "private static readonly TimeSpan ActiveFmp4ReopenNearLiveGuard = TimeSpan.FromMilliseconds(250);");
        AssertContains(decoderReopenText, "private static readonly TimeSpan AdjacentSegmentSeekFallbackWindow = TimeSpan.FromSeconds(3);");
        AssertContains(decoderReopenText, "private bool TrySeekAdjacentSegmentStart(");
        AssertContains(decoderReopenText, "FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK");
        AssertDoesNotContain(rootText, "private static readonly TimeSpan ActiveFmp4ReopenNearLiveGuard = TimeSpan.FromMilliseconds(250);");
        AssertDoesNotContain(rootText, "private static readonly TimeSpan AdjacentSegmentSeekFallbackWindow = TimeSpan.FromSeconds(3);");
        AssertContains(sourceText, "private bool ShouldSkipActiveFmp4ReopenNearLive(TimeSpan seekTarget, string reason)");
        AssertContains(sourceText, "var latestPts = _bufferManager.LatestPts;");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_REOPEN_SKIP_NEAR_LIVE");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_REOPEN_ERROR");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_REOPEN_KEYFRAME_ERROR");
        AssertContains(sourceText, "private bool TrySeekAdjacentSegmentStart(");
        AssertContains(sourceText, "var nextPath = _bufferManager.GetNextSegmentFile(currentPath);");
        AssertContains(sourceText, "var nextStart = _bufferManager.GetSegmentStartPts(nextPath);");
        AssertContains(sourceText, "if (targetGap > AdjacentSegmentSeekFallbackWindow)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK_FAIL");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_ADJACENT_SEGMENT_SEEK_ERROR");
        AssertContains(sourceText, "private static bool IsSamePlaybackPath(string? left, string? right)");
        AssertContains(sourceText, "Path.GetFullPath(left)");
        AssertContains(sourceText, "Path.GetFullPath(right)");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PATH_COMPARE_WARN");
        AssertContains(sourceText, "&& IsSamePlaybackPath(path, _bufferManager.ActiveFilePath)");
        AssertContains(sourceText, "if (fileOpen && decoder.IsOpen && IsSamePlaybackPath(filePath, _currentOpenFilePath))\n            return;");
        AssertContains(sourceText, "if (State == FlashbackPlaybackState.Paused &&\n            IsSamePlaybackPath(prevFile, _currentOpenFilePath) &&\n            !requireExactResumeSeek)");
        AssertContains(sourceText, "MarkDecoderPlaybackFileClosed(ref fileOpen);\n            return false;");
        AssertContains(sourceText, "private bool TrySeekWithActiveFmp4Reopen(");
        AssertContains(sourceText, "if (SeekToWithCapTelemetry(decoder, seekTarget, reason, cancellationToken))\n        {\n            return true;\n        }");
        AssertContains(sourceText, "private bool SeekToWithCapTelemetry(");
        AssertContains(seekCapTelemetryText, "private long _playbackSeekForwardDecodeCapHits;");
        AssertContains(seekCapTelemetryText, "private int _lastPlaybackSeekHitForwardDecodeCap;");
        AssertContains(seekCapTelemetryText, "public long PlaybackSeekForwardDecodeCapHits => Interlocked.Read(ref _playbackSeekForwardDecodeCapHits);");
        AssertContains(seekCapTelemetryText, "public bool LastPlaybackSeekHitForwardDecodeCap => Volatile.Read(ref _lastPlaybackSeekHitForwardDecodeCap) != 0;");
        AssertContains(seekCapTelemetryText, "private bool SeekToWithCapTelemetry(");
        AssertContains(seekCapTelemetryText, "FLASHBACK_PLAYBACK_SEEK_FORWARD_DECODE_CAP");
        AssertContains(seekCapTelemetryText, "Interlocked.Increment(ref _playbackSeekForwardDecodeCapHits)");
        AssertContains(rootText, "public long PlaybackSeekForwardDecodeCapHits =>");
        AssertContains(rootText, "public bool LastPlaybackSeekHitForwardDecodeCap =>");
        AssertContains(rootText, "private bool SeekToWithCapTelemetry(");
        AssertContains(rootText, "FLASHBACK_PLAYBACK_SEEK_FORWARD_DECODE_CAP");
        AssertContains(sourceText, "if (ShouldSkipActiveFmp4ReopenNearLive(seekTarget, reason))\n            {\n                SetReopenFailure(reason, \"near_live\", seekTarget);\n                return false;\n            }\n\n            return TryReopenCurrentFileAndSeek(decoder, ref fileOpen, seekTarget, reason, cancellationToken);");
        AssertContains(sourceText, "if (TrySeekAdjacentSegmentStart(decoder, ref fileOpen, seekTarget, reason, out _, cancellationToken))\n        {\n            return true;\n        }\n\n        SetReopenFailure(reason, \"seek_failed\", seekTarget);");
        AssertContains(sourceText, "if (SeekToWithCapTelemetry(decoder, seekTarget, reason, cancellationToken))\n            {\n                return true;\n            }\n\n            SetReopenFailure(reason, \"seek_failed\", seekTarget);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_REOPEN_SEEK_FAIL");
        AssertContains(sourceText, "if (decoder.SeekToKeyframe(seekTarget, cancellationToken))\n            {\n                return true;\n            }\n\n            SetReopenFailure(reason, \"keyframe_seek_failed\", seekTarget);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_REOPEN_KEYFRAME_SEEK_FAIL");
        AssertContains(positioningText, "private void ReopenDecoderPlaybackFile(");
        AssertContains(sourceText, "updateCurrentOpenPath: true,\n                closeOnlyWhenOpen: true);");
        AssertContains(sourceText, "updateCurrentOpenPath: false,\n                closeOnlyWhenOpen: false);");
        AssertContains(positioningText, "private void MarkDecoderPlaybackFileClosed(ref bool fileOpen)");
        AssertContains(positioningText, "_decoderHwAccel = \"N/A\";\n        fileOpen = false;\n        _currentOpenFilePath = null;");
        AssertContains(positioningText, "private static void CloseDecoderFileBestEffort(FlashbackDecoder decoder, string operation)");
        AssertContains(positioningText, "private void CleanupDecoder(ref FlashbackDecoder? decoder, ref bool fileOpen)");
        AssertContains(positioningText, "FLASHBACK_PLAYBACK_DECODER_CLEANUP_COMPLETE");
        AssertContains(decoderReopenText, "private void ReopenDecoderPlaybackFile(");
        AssertContains(decoderReopenText, "private void MarkDecoderPlaybackFileClosed(ref bool fileOpen)");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackPlaybackController.DecoderReopen.cs")),
            "active fMP4 reopen recovery folded into decoder file ownership");
        AssertContains(sourceText, "private long SuppressAudioForFmp4Reopen(FlashbackDecoder decoder)");
        AssertContains(sourceText, "Interlocked.Increment(ref _playbackReopenAudioNullWindowCount);\n        decoder.AudioChunkCallback = null;");
        AssertContains(sourceText, "private void RestoreAudioAfterFmp4Reopen(");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_REOPEN_AUDIO_GATE");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_REOPEN_ERROR reason={reason} path='{currentPath}' type={ex.GetType().Name} msg='{ex.Message}'\");\n            MarkDecoderPlaybackFileClosed(ref fileOpen);");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_PLAYBACK_REOPEN_KEYFRAME_ERROR reason={reason} path='{currentPath}' type={ex.GetType().Name} msg='{ex.Message}'\");\n            MarkDecoderPlaybackFileClosed(ref fileOpen);");
        AssertContains(sourceText, "SetReopenFailure(reason, \"no_current_file\", seekTarget);");
        AssertContains(sourceText, "SetReopenFailure(reason, ex.GetType().Name, seekTarget);");
        AssertContains(sourceText, "private void SetReopenFailure(string reason, string detail, TimeSpan position)");
        AssertContains(sourceText, "SetLastCommandFailure($\"reopen_failed:{reason}:{detail}{FormatCommandDetail(position: position)}\");");
        AssertContains(sourceText, "if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.Seek, cts.Token))");
        AssertContains(sourceText, "if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.BeginScrub, cts.Token))");
        AssertContains(sourceText, "if (!SeekAndDisplayKeyframe(decoder, ref fileOpen, cmd.Position, frozenValidStart, CommandKind.UpdateScrub, cts.Token))");
        AssertContains(sourceText, "SetSeekDisplayFailure(kind, \"no_file\", bufferPosition);");
        AssertContains(sourceText, "SetSeekDisplayFailure(kind, \"seek_failed\", bufferPosition);");
        AssertContains(sourceText, "SetSeekDisplayFailure(kind, \"submit_failed\", bufferPosition);");
        AssertContains(sourceText, "SetSeekDisplayFailure(kind, \"no_frame\", bufferPosition);");
        AssertContains(sourceText, "SetSeekDisplayFailure(kind, ex.GetType().Name, bufferPosition);");
        AssertContains(seekDisplayText, "private bool SeekAndDisplayKeyframe(");
        AssertContains(seekDisplayText, "private bool TryDecodeAndDisplaySeekFrame(");
        AssertContains(seekDisplayText, "private void RecordSeekDisplayDecodeFailure(");
        AssertDoesNotContain(agentMapText, "FlashbackPlaybackController.SeekDisplay.cs");
        AssertDoesNotContain(cleanupPlanText, "FlashbackPlaybackController.SeekDisplay.cs");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackPlaybackController.SeekDisplay.cs")),
            "Flashback seek-display logic folded into playback frame ownership");
        var seekDisplayBlock = ExtractTextBetween(
            seekDisplayText,
            "private bool SeekAndDisplayKeyframe(",
            "\n}");
        AssertContains(seekDisplayBlock, "CancellationToken cancellationToken");
        AssertContains(seekDisplayBlock, "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(seekDisplayBlock, "decoder.SeekToKeyframe(filePts, cancellationToken)");
        AssertContains(seekDisplayBlock, "TryDecodeAndDisplaySeekFrame(");
        AssertContains(seekDisplayText, "TryDecodeNextVideoFrameWithMetrics(decoder, out var frame, cancellationToken)");
        AssertContains(seekDisplayText, "var frameOwned = gotFrame;");
        AssertContains(seekDisplayText, "frameOwned = false;");
        AssertContains(seekDisplayText, "ReleaseHeldFrameBestEffort(frame, \"seek_cancelled\")");
        AssertContains(seekDisplayText, "if (frameOwned)\n            {\n                ReleaseHeldFrameBestEffort(frame, \"seek_cancelled\");\n            }");
        AssertContains(seekDisplayBlock, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(seekDisplayBlock, "throw;");
        AssertOccursBefore(seekDisplayBlock, "cancellationToken.ThrowIfCancellationRequested();", "decoder.SeekToKeyframe(filePts, cancellationToken)");
        AssertOccursBefore(seekDisplayBlock, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)", "catch (Exception ex)");
        AssertContains(seekDisplayText, "TrySeekAdjacentSegmentStart(decoder, ref fileOpen, filePts, $\"seek_display:{kind}\", out var adjacentFilePts, cancellationToken)");
        AssertContains(seekDisplayText, "RecordSeekDisplayDecodeFailure(kind, bufferPosition, filePts);");
        AssertContains(seekDisplayText, "private void RecordSeekDisplayDecodeFailure(CommandKind kind, TimeSpan bufferPosition, TimeSpan filePts)");
        AssertContains(sourceText, "RecordPlaybackDroppedFrame(\"seek_display_no_frame\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SEEK_NO_FRAME_SNAP_TO_LIVE");
        AssertContains(sourceText, "return gotFrame;");
        AssertContains(sourceText, "private void RestoreLiveAfterSeekDisplayFailure(FlashbackDecoder decoder, ref bool fileOpen, string operation)");
        AssertContains(sourceText, "CloseDecoderFileBestEffort(decoder, operation);\n        fileOpen = false;\n        _currentOpenFilePath = null;\n        _decoderHwAccel = \"N/A\";\n        ReleasePlaybackFrameForLive(operation);");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(operation);\n        RestoreLiveAudio();\n        SafeResumePreviewSubmission(operation);\n        if (resumeRendering)\n        {\n            SafeResumeRendering(operation);\n        }\n\n        SetState(FlashbackPlaybackState.Live);");
        AssertContains(sourceText, "RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, \"seek_display_failed\");");
        AssertContains(sourceText, "RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, \"begin_scrub_display_failed\");");
        AssertContains(sourceText, "RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, \"scrub_update_display_failed\");");
        AssertContains(sourceText, "private void SetSeekDisplayFailure(CommandKind kind, string detail, TimeSpan position)");
        AssertContains(sourceText, "SetLastCommandFailure($\"seek_display_failed:{kind}:{detail}{FormatCommandDetail(position: position)}\");");
        AssertContains(sourceText, "TimeSpan? pendingExactResumeTarget = null;");
        AssertContains(sourceText, "var seekResumeTarget = SaturatingAdd(cmd.Position, frozenValidStart);");
        AssertContains(sourceText, "var coalescedSeekTarget = seekResumeTarget;");
        AssertContains(sourceText, "pendingExactResumeTarget = seekResumeTarget;");
        AssertContains(sourceText, "var pendingPlayTarget = pendingExactResumeTarget ?? SaturatingAdd(PlaybackPosition, frozenValidStart);");
        AssertContains(sourceText, "var requireExactResumeSeek = pendingExactResumeTarget.HasValue;");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_RESUME_EXACT_SEEK");
        AssertContains(sourceText, "if (ShouldYieldSeekToQueuedPlay(commandChannel))");
        AssertContains(sourceText, "MarkCommandNoOp(CommandKind.Seek, \"superseded_by_play\", cmd.Position);");
        AssertContains(sourceText, "if (ShouldYieldPauseFromLiveToQueuedSeekOrPlay(commandChannel))");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_PAUSE_FROM_LIVE_DEFER_DISPLAY");
        AssertContains(sourceText, "if (!TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, coalescedSeekTarget, \"seek_resume\", cts.Token))");
        AssertContains(sourceText, "if (!TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, endScrubTarget, \"end_scrub\", cts.Token))");
        AssertContains(sourceText, "if (!TrySeekWithActiveFmp4Reopen(decoder, ref fileOpen, seekTarget, \"play\", cts.Token))");
        AssertContains(sourceText, "if (!ShouldSkipActiveFmp4ReopenNearLive(filePts, \"seek_keyframe\"))\n                    {\n                        Logger.Log($\"FLASHBACK_PLAYBACK_SEEK_REOPEN_ACTIVE offset_ms={(long)filePts.TotalMilliseconds}\");\n                        if (TryReopenCurrentFileAndSeekKeyframe(decoder, ref fileOpen, filePts, \"seek_keyframe\", cancellationToken))\n                            goto seekSuccess;\n                    }");
        AssertContains(sourceText, "SetReopenFailure(\"segment_switch\", \"seek_failed\", segSwitchTarget);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SEGMENT_SWITCH_SEEK_FAIL");
        AssertContains(sourceText, "RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, \"segment_switch_seek_failed\");");
        AssertContains(sourceText, "SetReopenFailure(\"fmp4_reopen\", \"seek_failed\", resumeTarget);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_FMP4_REOPEN_SEEK_FAIL");
        AssertContains(sourceText, "RestoreLiveAfterSeekDisplayFailure(decoder, ref fileOpen, \"fmp4_reopen_seek_failed\");");
        AssertDoesNotContain(sourceText, "decoder.OpenFile(_currentOpenFilePath!)");
        AssertDoesNotContain(sourceText, "decoder.OpenFile(_currentOpenFilePath);");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_InOutPoints_DefaultToUnset()
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

        var inPointProp = controllerType.GetProperty("InPoint", BindingFlags.Public | BindingFlags.Instance);
        var outPointProp = controllerType.GetProperty("OutPoint", BindingFlags.Public | BindingFlags.Instance);

        AssertNotNull(inPointProp, "FlashbackPlaybackController.InPoint");
        AssertNotNull(outPointProp, "FlashbackPlaybackController.OutPoint");
        foreach (var propertyName in new[]
                 {
                     "CommandsEnqueued",
                     "CommandsProcessed",
                     "CommandsDropped",
                     "CommandsSkippedNotReady",
                     "ScrubUpdatesCoalesced",
                     "PendingCommands",
                     "MaxPendingCommands",
                     "LastCommandQueueLatencyMs",
                     "MaxCommandQueueLatencyMs",
                     "LastCommandQueued",
                     "LastCommandProcessed",
                     "LastCommandQueuedUtcUnixMs",
                     "LastCommandProcessedUtcUnixMs",
                     "LastCommandFailureUtcUnixMs",
                     "LastCommandFailure",
                     "PlaybackThreadAlive"
                 })
        {
            AssertNotNull(controllerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance), propertyName);
        }

        var clearMethod = controllerType.GetMethod("ClearInOutPoints", BindingFlags.Public | BindingFlags.Instance);
        AssertNotNull(clearMethod, "FlashbackPlaybackController.ClearInOutPoints");
        clearMethod!.Invoke(controller, null);

        var sourceText = ReadFlashbackPlaybackControllerSource();
        AssertContains(
            sourceText,
            "var pending = Interlocked.Increment(ref _pendingCommands);\n        var droppedOldest = false;\n        var droppedCommand = default(PlaybackCommand);\n        if (!_commandChannel.Writer.TryWrite(queuedCommand) &&\n            (!IsCommandChannelOpenForDropRetry() ||\n             !TryDropOldestQueuedCommandForNewCommand(out droppedCommand) ||\n             !(droppedOldest = _commandChannel.Writer.TryWrite(queuedCommand))))\n        {\n            DecrementPendingCommands();");
        AssertContains(sourceText, "if (droppedOldest)\n        {\n            TrackDroppedQueuedCommand(droppedCommand, queuedCommand.Kind);\n        }");
        AssertContains(sourceText, "UpdateMaxPendingCommands(pending);");
        AssertContains(sourceText, "private bool IsCommandChannelOpenForDropRetry()");
        AssertContains(sourceText, "private bool TryDropOldestQueuedCommandForNewCommand(out PlaybackCommand droppedCommand)");
        AssertContains(sourceText, "private void TrackDroppedQueuedCommand(PlaybackCommand droppedCommand, CommandKind newCommandKind)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_InOutPoints_ClearInvalidCounterpart()
    {
        var sourceText = ReadFlashbackPlaybackControllerSource();

        AssertContains(sourceText, "var outTicks = Interlocked.Read(ref _outPointTicks);\n        if (outTicks != long.MinValue && outTicks <= pos.Ticks)\n        {\n            OutPoint = null;\n            Logger.Log(\"FLASHBACK_PLAYBACK_CLEAR_OUT invalid_range\");\n        }");
        AssertContains(sourceText, "var inTicks = Interlocked.Read(ref _inPointTicks);\n        if (inTicks != long.MinValue && inTicks >= pos.Ticks)\n        {\n            InPoint = null;\n            Logger.Log(\"FLASHBACK_PLAYBACK_CLEAR_IN invalid_range\");\n        }");
        AssertContains(sourceText, "var pos = overridePosition.HasValue\n            ? NormalizeMarkerPosition(overridePosition.Value)\n            : PlaybackPosition;\n        ClearLastCommandFailure();\n        InPoint = pos;");
        AssertContains(sourceText, "var pos = overridePosition.HasValue\n            ? NormalizeMarkerPosition(overridePosition.Value)\n            : PlaybackPosition;\n        ClearLastCommandFailure();\n        OutPoint = pos;");
        AssertContains(sourceText, "public TimeSpan SetInPoint() => SetInPointAt(null);");
        AssertContains(sourceText, "public TimeSpan SetInPointAt(TimeSpan position) => SetInPointAt((TimeSpan?)position);");
        AssertContains(sourceText, "public TimeSpan SetOutPoint() => SetOutPointAt(null);");
        AssertContains(sourceText, "public TimeSpan SetOutPointAt(TimeSpan position) => SetOutPointAt((TimeSpan?)position);");
        AssertContains(sourceText, "InPoint = null;\n        OutPoint = null;\n        ClearLastCommandFailure();");

        var flashbackCommandController = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs")
            .Replace("\r\n", "\n");
        AssertContains(flashbackCommandController, "_context.ViewModel.FlashbackSetInPointAt(_context.ViewModel.FlashbackPlaybackPosition)");
        AssertContains(flashbackCommandController, "_context.ViewModel.FlashbackSetOutPointAt(_context.ViewModel.FlashbackPlaybackPosition)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_InOutPointSettersNormalizeMarkers()
    {
        var sourceText = ReadFlashbackPlaybackControllerSource();
        var markersText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.Positioning.cs");

        AssertContains(markersText, "private long _inPointFilePtsTicks = long.MinValue;");
        AssertContains(markersText, "private long _outPointFilePtsTicks = long.MinValue;");
        AssertContains(markersText, "Interlocked.Exchange(ref _inPointTicks, normalized?.Ticks ?? long.MinValue);\n            Interlocked.Exchange(ref _inPointFilePtsTicks, normalized.HasValue ? SaturatingAdd(normalized.Value, _bufferManager.ValidStartPts).Ticks : long.MinValue);");
        AssertContains(markersText, "Interlocked.Exchange(ref _outPointTicks, normalized?.Ticks ?? long.MinValue);\n            Interlocked.Exchange(ref _outPointFilePtsTicks, normalized.HasValue ? SaturatingAdd(normalized.Value, _bufferManager.ValidStartPts).Ticks : long.MinValue);");
        AssertContains(markersText, "public TimeSpan? InPointFilePts");
        AssertContains(markersText, "public TimeSpan? OutPointFilePts");
        AssertContains(markersText, "public void RestoreInOutPoints(\n        TimeSpan? inPoint,\n        TimeSpan? outPoint,\n        TimeSpan? inPointFilePts,\n        TimeSpan? outPointFilePts)");
        AssertContains(markersText, "Interlocked.Exchange(ref _inPointFilePtsTicks, inPointFilePts.Value.Ticks);");
        AssertContains(markersText, "Interlocked.Exchange(ref _outPointFilePtsTicks, outPointFilePts.Value.Ticks);");
        AssertContains(sourceText, "private TimeSpan NormalizeMarkerPosition(TimeSpan position)\n    {\n        if (position <= TimeSpan.Zero)\n        {\n            return TimeSpan.Zero;\n        }\n\n        var bufferDuration = _bufferManager.BufferedDuration;\n        return position > bufferDuration ? bufferDuration : position;\n    }");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_InOutPointChangesStopAfterDispose()
    {
        var bufferManagerType = RequireType("Sussudio.Services.Flashback.FlashbackBufferManager");
        var bufferManager = Activator.CreateInstance(bufferManagerType, new object?[] { null })!;

        var controllerType = RequireType("Sussudio.Services.Flashback.FlashbackPlaybackController");
        using var controller = (IDisposable)Activator.CreateInstance(controllerType, new[] { bufferManager })!;

        var setInPoint = controllerType.GetMethod("SetInPoint", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackPlaybackController.SetInPoint not found.");
        var setOutPoint = controllerType.GetMethod("SetOutPoint", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackPlaybackController.SetOutPoint not found.");
        var clearInOut = controllerType.GetMethod("ClearInOutPoints", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackPlaybackController.ClearInOutPoints not found.");

        setInPoint.Invoke(controller, null);
        controller.Dispose();
        clearInOut.Invoke(controller, null);
        setOutPoint.Invoke(controller, null);

        AssertEqual((TimeSpan?)TimeSpan.Zero, (TimeSpan?)GetPropertyValue(controller, "InPoint"), "disposed InPoint");
        AssertEqual<object?>(null, GetPropertyValue(controller, "OutPoint"), "disposed OutPoint");

        var sourceText = ReadFlashbackPlaybackControllerSource();
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SET_IN_SKIP reason=disposed");
        AssertContains(sourceText, "SetLastCommandFailure(\"disposed:SetInPoint\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SET_OUT_SKIP reason=disposed");
        AssertContains(sourceText, "SetLastCommandFailure(\"disposed:SetOutPoint\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_CLEAR_INOUT_SKIP reason=disposed");
        AssertContains(sourceText, "SetLastCommandFailure(\"disposed:ClearInOutPoints\");");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_ClampPosition_BoundsMarkersToBufferedDuration()
    {
        var sourceText = ReadFlashbackPlaybackControllerSource();

        AssertContains(sourceText, "var bufferDuration = _bufferManager.BufferedDuration;\n        var inTicks = Interlocked.Read(ref _inPointTicks);");
        AssertContains(sourceText, "var max = outTicks == long.MinValue ? bufferDuration : TimeSpan.FromTicks(outTicks);\n        if (max > bufferDuration) max = bufferDuration;");
        AssertContains(sourceText, "private TimeSpan ClampPosition(TimeSpan position) => ClampPosition(position, null);");
        AssertContains(sourceText, "private TimeSpan ClampPosition(TimeSpan position, TimeSpan? frozenValidStart)");
        AssertContains(sourceText, "var currentValidStart = _bufferManager.ValidStartPts;");
        AssertContains(sourceText, "var evictedDelta = currentValidStart - frozenValidStart.Value;");

        return Task.CompletedTask;
    }
    internal static Task FlashbackPlaybackController_FrameDuration_GuardsInvalidDecoderFps()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var metricsCollectionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var playbackTimingText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");
        var playbackPtsCadenceText = playbackTimingText;

        AssertDoesNotContain(sourceText, "TimeSpan.FromSeconds(1.0 / Math.Max(decoder.FrameRate, 1.0))");
        AssertContains(sourceText, "frameDuration = ResolveFrameDuration(decoder);");
        AssertContains(sourceText, "private TimeSpan ResolveFrameDuration(FlashbackDecoder decoder)");
        AssertContains(sourceText, "if (!double.IsFinite(fps) || fps <= 0)\n        {\n            fps = decoder.FrameRate;\n        }");
        AssertContains(sourceText, "if (!double.IsFinite(fps) || fps <= 0)\n        {\n            fps = FallbackPlaybackFrameRate;\n        }");
        AssertContains(sourceText, "private const double FallbackPlaybackFrameRate = 60.0;");
        AssertContains(sourceText, "private const double MaxPlaybackFrameRate = 1000.0;");
        AssertContains(playbackTimingText, "private const double FallbackPlaybackFrameRate = 60.0;");
        AssertContains(playbackTimingText, "private const double MaxPlaybackFrameRate = 1000.0;");
        AssertDoesNotContain(rootText, "private const double FallbackPlaybackFrameRate = 60.0;");
        AssertDoesNotContain(rootText, "private const double MaxPlaybackFrameRate = 1000.0;");
        AssertContains(sourceText, "fps = Math.Min(fps, MaxPlaybackFrameRate);");
        AssertContains(sourceText, "_playbackTargetFps = fps;");
        AssertContains(sourceText, "public double PlaybackTargetFps => _playbackTargetFps;");
        AssertContains(sourceText, "return TimeSpan.FromSeconds(1.0 / fps);");
        AssertContains(sourceText, "TrackDecodedPtsCadence(videoFrame.Pts, frameDuration);");
        AssertContains(playbackPtsCadenceText, "private void TrackDecodedPtsCadence(TimeSpan pts, TimeSpan expectedFrameDuration)");
        AssertContains(playbackPtsCadenceText, "private void ResetPlaybackPtsCadenceBaseline()");
        AssertContains(playbackPtsCadenceText, "private void RecordPlaybackPtsCadenceMismatch(");
        AssertContains(playbackPtsCadenceText, "private long _lastPlaybackCadencePtsTicks = -1;");
        AssertContains(playbackPtsCadenceText, "private long _playbackPtsCadenceMismatchCount;");
        AssertContains(playbackPtsCadenceText, "private long _lastPlaybackPtsCadenceMismatchUtcUnixMs;");
        AssertContains(playbackPtsCadenceText, "private double _lastPlaybackPtsCadenceDeltaMs;");
        AssertContains(playbackPtsCadenceText, "private double _lastPlaybackPtsCadenceExpectedMs;");
        AssertContains(playbackPtsCadenceText, "public long PlaybackPtsCadenceMismatchCount => Interlocked.Read(ref _playbackPtsCadenceMismatchCount);");
        AssertContains(playbackPtsCadenceText, "public long LastPlaybackPtsCadenceMismatchUtcUnixMs => Interlocked.Read(ref _lastPlaybackPtsCadenceMismatchUtcUnixMs);");
        AssertContains(playbackPtsCadenceText, "public double LastPlaybackPtsCadenceDeltaMs => _lastPlaybackPtsCadenceDeltaMs;");
        AssertContains(playbackPtsCadenceText, "public double LastPlaybackPtsCadenceExpectedMs => _lastPlaybackPtsCadenceExpectedMs;");
        AssertContains(playbackPtsCadenceText, "FLASHBACK_PLAYBACK_PTS_CADENCE_MISMATCH");
        AssertDoesNotContain(metricsCollectionText, "public long PlaybackPtsCadenceMismatchCount =>");
        AssertDoesNotContain(metricsCollectionText, "public double LastPlaybackPtsCadenceDeltaMs =>");
        AssertContains(sourceText, "public long PlaybackPtsCadenceMismatchCount => Interlocked.Read(ref _playbackPtsCadenceMismatchCount);");
        AssertContains(sourceText, "Interlocked.Exchange(ref _playbackPtsCadenceMismatchCount, 0);");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_PtsCadenceTelemetry_TracksMismatches()
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
        var track = controllerType.GetMethod("TrackDecodedPtsCadence", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("TrackDecodedPtsCadence not found.");
        var reset = controllerType.GetMethod("ResetPlaybackMetrics", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ResetPlaybackMetrics not found.");
        var expected = TimeSpan.FromMilliseconds(1000.0 / 120.0);

        try
        {
            track.Invoke(controller, new object[] { expected, expected });
            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(1000.0 / 60.0), expected });
            AssertEqual(0L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "matching decoded PTS cadence count");

            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(1000.0 / 30.0), expected });
            AssertEqual(1L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "slow decoded PTS cadence count");
            AssertNearlyEqual(1000.0 / 60.0, GetDoubleProperty(controller, "LastPlaybackPtsCadenceDeltaMs"), 0.1, "slow decoded PTS cadence delta");
            AssertNearlyEqual(expected.TotalMilliseconds, GetDoubleProperty(controller, "LastPlaybackPtsCadenceExpectedMs"), 0.1, "decoded PTS expected cadence");
            if (GetLongProperty(controller, "LastPlaybackPtsCadenceMismatchUtcUnixMs") <= 0)
            {
                throw new InvalidOperationException("Expected decoded PTS cadence mismatch timestamp to be populated.");
            }

            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(1000.0 / 30.0), expected });
            AssertEqual(2L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "duplicate decoded PTS cadence count");
            AssertNearlyEqual(0.0, GetDoubleProperty(controller, "LastPlaybackPtsCadenceDeltaMs"), 0.1, "duplicate decoded PTS cadence delta");

            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(25.0), expected });
            AssertEqual(3L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "backward decoded PTS cadence count");
            if (GetDoubleProperty(controller, "LastPlaybackPtsCadenceDeltaMs") >= 0)
            {
                throw new InvalidOperationException("Expected backward decoded PTS cadence delta to be negative.");
            }

            track.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(1000.0 / 24.0), expected });
            AssertEqual(3L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "valid cadence after backward PTS remains clean");

            reset.Invoke(controller, null);
            AssertEqual(0L, GetLongProperty(controller, "PlaybackPtsCadenceMismatchCount"), "decoded PTS cadence reset count");
            AssertEqual(0.0, GetDoubleProperty(controller, "LastPlaybackPtsCadenceDeltaMs"), "decoded PTS cadence reset delta");
        }
        finally
        {
            (controller as IDisposable)?.Dispose();
            (bufferManager as IDisposable)?.Dispose();
        }

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_ResetClearsDecodeMetrics()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var metricsText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        var resetMetricsBlock = ExtractTextBetween(
            sourceText,
            "private void ResetPlaybackMetrics()",
            "private void RestoreAudioCallback");
        AssertContains(metricsText, "private long _playbackFrameCount;");
        AssertContains(metricsText, "private long _playbackDroppedFrames;");
        AssertContains(metricsText, "private readonly Stopwatch _playbackFpsClock = new();");
        AssertContains(metricsText, "private const int PlaybackCadenceSampleCapacity = 240;");
        AssertContains(metricsText, "private readonly double[] _playbackFrameIntervalsMs = new double[PlaybackCadenceSampleCapacity];");
        AssertContains(metricsText, "public long PlaybackFrameCount => Interlocked.Read(ref _playbackFrameCount);");
        AssertContains(metricsText, "public string LastPlaybackDropReason => Volatile.Read(ref _lastPlaybackDropReason);");
        AssertContains(metricsText, "public double PlaybackAvgFrameMs => _playbackAvgFrameMs;");
        AssertDoesNotContain(metricsText, "private long _lastPlaybackCadencePtsTicks = -1;");
        AssertDoesNotContain(metricsText, "private long _playbackPtsCadenceMismatchCount;");
        AssertContains(metricsText, "private void ResetPlaybackMetrics()");
        AssertContains(metricsText, "Interlocked.Exchange(ref _playbackPreviewPresentId, 0);");
        AssertContains(metricsText, "lock (_playbackDecodeLock)");
        AssertContains(metricsText, "Array.Clear(_playbackDecodeDurationsMs);");
        AssertContains(metricsText, "_playbackDecodeDurationHead = 0;");
        AssertContains(metricsText, "_playbackDecodeDurationCount = 0;");
        AssertContains(metricsText, "public readonly record struct PlaybackCadenceMetrics(");
        AssertContains(metricsText, "public PlaybackCadenceMetrics GetPlaybackCadenceMetrics()");
        AssertContains(metricsText, "private static double PercentileFromSorted(double[] sortedSamples, double percentile)");
        AssertContains(metricsText, "public readonly record struct PlaybackDecodeMetrics(");
        AssertContains(metricsText, "public PlaybackDecodeMetrics GetPlaybackDecodeMetrics()");
        AssertContains(metricsText, "private readonly double[] _playbackDecodeDurationsMs = new double[PlaybackCadenceSampleCapacity];");
        AssertContains(metricsText, "private double _playbackMaxDecodeTotalMs;");
        AssertContains(metricsText, "private string _playbackMaxDecodePhase = string.Empty;");
        AssertContains(metricsText, "public string PlaybackMaxDecodePhase => Volatile.Read(ref _playbackMaxDecodePhase);");
        AssertContains(metricsText, "public double PlaybackMaxDecodeSendMs => _playbackMaxDecodeSendMs;");
        AssertContains(metricsText, "public long PlaybackMaxDecodePositionMs => Interlocked.Read(ref _playbackMaxDecodePositionMs);");
        AssertContains(metricsText, "private bool TryDecodeNextVideoFrameWithMetrics(");
        AssertContains(metricsText, "private void TrackPlaybackDecodeDuration(");
        AssertContains(metricsText, "private static string ResolveDominantDecodePhase(FlashbackDecoder.PlaybackDecodePhaseTimings phaseTimings)");
        AssertContains(rootText, "private long _playbackFrameCount;");
        AssertContains(rootText, "private readonly Stopwatch _playbackFpsClock = new();");
        AssertContains(rootText, "private readonly double[] _playbackFrameIntervalsMs = new double[PlaybackCadenceSampleCapacity];");
        AssertContains(rootText, "private string _playbackMaxDecodePhase = string.Empty;");
        AssertContains(resetMetricsBlock, "Interlocked.Exchange(ref _playbackPreviewPresentId, 0);");
        AssertContains(sourceText, "if (phaseTimings.FeedMs > max) { phase = \"feed\"; max = phaseTimings.FeedMs; }");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackController_SubmitFailuresReleaseDecodedFrames()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var playbackFrameOwnershipText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackFrames.cs")
            .Replace("\r\n", "\n");
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "private bool TrySubmitAndHoldFrame(DecodedVideoFrame frame, string operation)");
        AssertContains(playbackFrameOwnershipText, "private bool TrySubmitAndHoldFrame(DecodedVideoFrame frame, string operation)");
        AssertContains(playbackFrameOwnershipText, "private static void SubmitFrame(");
        AssertContains(playbackFrameOwnershipText, "private static bool TryValidatePreviewFrame(DecodedVideoFrame frame, out string reason)");
        AssertContains(playbackFrameOwnershipText, "private static bool TryCalculatePreviewFrameBytes(int width, int height, bool isHdr, out int bytes)");
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
        AssertDoesNotContain(rootText, "private DecodedVideoFrame _previousHeldFrame;");
        AssertDoesNotContain(rootText, "private bool _hasPreviousHeldFrame;");
        AssertEqual(
            false,
            System.IO.File.Exists(System.IO.Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackPlaybackController.PreviewFrames.cs")),
            "Flashback playback preview-frame submission folded into PlaybackFrames");
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
