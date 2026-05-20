using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackPlaybackController_Fmp4ReopenRetriesAreGuarded()
    {
        var sourceText = ReadFlashbackPlaybackControllerPlaybackSource();
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.cs")
            .Replace("\r\n", "\n");
        var segmentEdgesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.PlaybackSegmentEdges.cs")
            .Replace("\r\n", "\n");
        var decoderFilesText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderFiles.cs")
            .Replace("\r\n", "\n");
        var decoderReopenText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderReopen.cs")
            .Replace("\r\n", "\n");
        var decoderSegmentReopenText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackPlaybackController.DecoderSegmentReopen.cs")
            .Replace("\r\n", "\n");

        AssertContains(sourceText, "private bool TryReopenCurrentFileAndSeek(");
        AssertContains(decoderSegmentReopenText, "private bool TryReopenCurrentFmp4BeforeSegmentSwitch(");
        AssertContains(decoderSegmentReopenText, "private bool HandleActiveFmp4ReopenAtSegmentEdge(");
        AssertContains(segmentEdgesText, "TryReopenCurrentFmp4BeforeSegmentSwitch(");
        AssertContains(segmentEdgesText, "return HandleActiveFmp4ReopenAtSegmentEdge(");
        AssertDoesNotContain(segmentEdgesText, "FLASHBACK_PLAYBACK_FMP4_REOPEN_BEFORE_SEGMENT_SWITCH_ERROR");
        AssertDoesNotContain(segmentEdgesText, "FLASHBACK_PLAYBACK_FMP4_REOPEN_SEEK_FAIL");
        AssertContains(sourceText, "private bool TryReopenCurrentFileAndSeekKeyframe(");
        AssertContains(sourceText, "private static readonly TimeSpan ActiveFmp4ReopenNearLiveGuard = TimeSpan.FromMilliseconds(250);");
        AssertContains(sourceText, "private static readonly TimeSpan AdjacentSegmentSeekFallbackWindow = TimeSpan.FromSeconds(3);");
        AssertContains(decoderReopenText, "private static readonly TimeSpan ActiveFmp4ReopenNearLiveGuard = TimeSpan.FromMilliseconds(250);");
        AssertContains(decoderReopenText, "private static readonly TimeSpan AdjacentSegmentSeekFallbackWindow = TimeSpan.FromSeconds(3);");
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
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SEEK_FORWARD_DECODE_CAP");
        AssertContains(sourceText, "Interlocked.Increment(ref _playbackSeekForwardDecodeCapHits)");
        AssertContains(sourceText, "if (ShouldSkipActiveFmp4ReopenNearLive(seekTarget, reason))\n            {\n                SetReopenFailure(reason, \"near_live\", seekTarget);\n                return false;\n            }\n\n            return TryReopenCurrentFileAndSeek(decoder, ref fileOpen, seekTarget, reason, cancellationToken);");
        AssertContains(sourceText, "if (TrySeekAdjacentSegmentStart(decoder, ref fileOpen, seekTarget, reason, out _, cancellationToken))\n        {\n            return true;\n        }\n\n        SetReopenFailure(reason, \"seek_failed\", seekTarget);");
        AssertContains(sourceText, "if (SeekToWithCapTelemetry(decoder, seekTarget, reason, cancellationToken))\n            {\n                return true;\n            }\n\n            SetReopenFailure(reason, \"seek_failed\", seekTarget);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_REOPEN_SEEK_FAIL");
        AssertContains(sourceText, "if (decoder.SeekToKeyframe(seekTarget, cancellationToken))\n            {\n                return true;\n            }\n\n            SetReopenFailure(reason, \"keyframe_seek_failed\", seekTarget);");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_REOPEN_KEYFRAME_SEEK_FAIL");
        AssertContains(decoderFilesText, "private void ReopenDecoderPlaybackFile(");
        AssertContains(sourceText, "updateCurrentOpenPath: true,\n                closeOnlyWhenOpen: true);");
        AssertContains(sourceText, "updateCurrentOpenPath: false,\n                closeOnlyWhenOpen: false);");
        AssertContains(decoderFilesText, "private void MarkDecoderPlaybackFileClosed(ref bool fileOpen)");
        AssertContains(decoderFilesText, "_decoderHwAccel = \"N/A\";\n        fileOpen = false;\n        _currentOpenFilePath = null;");
        AssertDoesNotContain(decoderReopenText, "private void ReopenDecoderPlaybackFile(");
        AssertDoesNotContain(decoderReopenText, "private void MarkDecoderPlaybackFileClosed(ref bool fileOpen)");
        AssertDoesNotContain(decoderSegmentReopenText, "private void ReopenDecoderPlaybackFile(");
        AssertDoesNotContain(decoderSegmentReopenText, "private void MarkDecoderPlaybackFileClosed(ref bool fileOpen)");
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
        AssertContains(sourceText, "private bool SeekAndDisplayKeyframe(");
        var seekDisplayBlock = ExtractTextBetween(
            sourceText,
            "private bool SeekAndDisplayKeyframe(",
            "    private void RecordSeekDisplayDecodeFailure");
        AssertContains(seekDisplayBlock, "CancellationToken cancellationToken");
        AssertContains(seekDisplayBlock, "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(seekDisplayBlock, "decoder.SeekToKeyframe(filePts, cancellationToken)");
        AssertContains(seekDisplayBlock, "TryDecodeNextVideoFrameWithMetrics(decoder, out var frame, cancellationToken)");
        AssertContains(seekDisplayBlock, "var frameOwned = gotFrame;");
        AssertContains(seekDisplayBlock, "frameOwned = false;");
        AssertContains(seekDisplayBlock, "ReleaseHeldFrameBestEffort(frame, \"seek_cancelled\")");
        AssertContains(seekDisplayBlock, "if (frameOwned)\n                {\n                    ReleaseHeldFrameBestEffort(frame, \"seek_cancelled\");\n                }");
        AssertContains(seekDisplayBlock, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(seekDisplayBlock, "throw;");
        AssertOccursBefore(seekDisplayBlock, "cancellationToken.ThrowIfCancellationRequested();", "decoder.SeekToKeyframe(filePts, cancellationToken)");
        AssertOccursBefore(seekDisplayBlock, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)", "catch (Exception ex)");
        AssertContains(seekDisplayBlock, "TrySeekAdjacentSegmentStart(decoder, ref fileOpen, filePts, $\"seek_display:{kind}\", out var adjacentFilePts, cancellationToken)");
        AssertContains(seekDisplayBlock, "RecordSeekDisplayDecodeFailure(kind, bufferPosition, filePts);");
        AssertContains(sourceText, "private void RecordSeekDisplayDecodeFailure(CommandKind kind, TimeSpan bufferPosition, TimeSpan filePts)");
        AssertContains(sourceText, "RecordPlaybackDroppedFrame(\"seek_display_no_frame\");");
        AssertContains(sourceText, "FLASHBACK_PLAYBACK_SEEK_NO_FRAME_SNAP_TO_LIVE");
        AssertContains(sourceText, "return gotFrame;");
        AssertContains(sourceText, "private void RestoreLiveAfterSeekDisplayFailure(FlashbackDecoder decoder, ref bool fileOpen, string operation)");
        AssertContains(sourceText, "CloseDecoderFileBestEffort(decoder, operation);\n        fileOpen = false;\n        _currentOpenFilePath = null;\n        _decoderHwAccel = \"N/A\";\n        ReleasePlaybackFrameForLive(operation);");
        AssertContains(sourceText, "ReleasePlaybackFrameForLive(operation);\n        RestoreLiveAudio();\n        SafeResumePreviewSubmission(operation);\n        SafeResumeRendering(operation);\n        SetState(FlashbackPlaybackState.Live);");
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

}
