using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
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
        var metricsCollectionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.Metrics.cs")
            .Replace("\r\n", "\n");
        var seekCapTelemetryText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.Metrics.cs")
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
        AssertDoesNotContain(rootText, "public long PlaybackSeekForwardDecodeCapHits =>");
        AssertDoesNotContain(rootText, "public bool LastPlaybackSeekHitForwardDecodeCap =>");
        AssertDoesNotContain(rootText, "private bool SeekToWithCapTelemetry(");
        AssertDoesNotContain(rootText, "FLASHBACK_PLAYBACK_SEEK_FORWARD_DECODE_CAP");
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

        var flashbackCommandController = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackCommandController.cs")
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
}
