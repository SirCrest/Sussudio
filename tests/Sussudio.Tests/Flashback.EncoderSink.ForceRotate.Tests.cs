using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackEncoderSink_RotateFailureRestoresActiveSegment()
    {
        var sinkText = ReadFlashbackEncoderSinkSource();
        var bufferText = ReadFlashbackBufferManagerSource();

        var rotateBlock = ExtractTextBetween(
            sinkText,
            "private bool RotateSegment(TimeSpan currentPts)",
            "    public FlashbackForceRotateResult ForceRotateForExport");
        AssertContains(rotateBlock, "string? completedPath = null;");
        AssertContains(rotateBlock, "string? newPath = null;");
        AssertContains(rotateBlock, "var encoderRotated = false;");
        AssertContains(rotateBlock, "completedPath = _tsFilePath;");
        AssertContains(rotateBlock, "var completedStartPts = _segmentStartPts;");
        AssertContains(rotateBlock, "newPath = _bufferManager.GenerateSegmentPath();");
        AssertContains(rotateBlock, "encoderRotated = true;");
        AssertOccursBefore(rotateBlock, "encoderRotated = true;", "_tsFilePath = newPath;");
        AssertOccursBefore(rotateBlock, "_tsFilePath = newPath;", "_bufferManager.OnSegmentCompleted(completedPath!, completedStartPts, currentPts, segmentBytes);");
        AssertContains(rotateBlock, "if (newPath != null && !encoderRotated)\n            {\n                _bufferManager.AbandonGeneratedSegmentPath(newPath, completedPath);\n            }");

        var abandonBlock = ExtractTextBetween(
            bufferText,
            "public void AbandonGeneratedSegmentPath",
            "    public void OnSegmentCompleted");
        AssertContains(abandonBlock, "if (IsSameSegmentPath(_activeSegmentPath, generatedPath))");
        AssertContains(abandonBlock, "_activeSegmentPath = restoreActivePath;");
        AssertContains(abandonBlock, "_nextSegmentIndex--;");
        AssertContains(abandonBlock, "if (!IsSameSegmentPath(generatedPath, restoreActivePath))");
        AssertContains(abandonBlock, "TryDeleteFile(generatedPath);");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_RegistersSegmentsOnCancellationAndRotationFailure()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        var cancelBlock = ExtractTextBetween(
            sourceText,
            "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)",
            "        catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_SINK_ENCODING_LOOP_FATAL");
        AssertContains(cancelBlock, "Logger.Log(\"FLASHBACK_SINK_ENCODING_LOOP_CANCELLED\");");
        AssertContains(cancelBlock, "CompletePendingForceRotateWithEmptyResult();");
        AssertContains(cancelBlock, "var cancelPts = ResolveEncoderPts();");
        AssertContains(cancelBlock, "if (cancelPts > _segmentStartPts)");
        AssertContains(cancelBlock, "var cancelSegmentBytes = NonNegativeByteDelta(_encoder.TotalBytesWritten, Interlocked.Read(ref _segmentStartBytes));");
        AssertContains(cancelBlock, "_bufferManager.OnSegmentCompleted(_tsFilePath, _segmentStartPts, cancelPts, cancelSegmentBytes);");
        AssertContains(cancelBlock, "FLASHBACK_SINK_ENCODING_LOOP_CANCELLED_SEGMENT_REGISTERED");
        AssertContains(cancelBlock, "FLASHBACK_SINK_CANCELLED_SEGMENT_REGISTER_FAIL");
        AssertContains(cancelBlock, "ReturnAllRemainingQueuedBuffers();");
        AssertOccursBefore(cancelBlock, "_bufferManager.OnSegmentCompleted(_tsFilePath, _segmentStartPts, cancelPts, cancelSegmentBytes);", "ReturnAllRemainingQueuedBuffers();");

        var rotateFailureBlock = ExtractTextBetween(
            sourceText,
            "catch (Exception ex)\n        {\n            if (newPath != null && !encoderRotated)",
            "    public FlashbackForceRotateResult ForceRotateForExport");
        AssertContains(rotateFailureBlock, "Interlocked.Increment(ref _segmentRotationFailures);");
        AssertContains(rotateFailureBlock, "var failPts = ResolveEncoderPts();");
        AssertContains(rotateFailureBlock, "if (failPts > _segmentStartPts)");
        AssertContains(rotateFailureBlock, "var failSegmentBytes = NonNegativeByteDelta(_encoder.TotalBytesWritten, Interlocked.Read(ref _segmentStartBytes));");
        AssertContains(rotateFailureBlock, "_bufferManager.OnSegmentCompleted(completedPath, _segmentStartPts, failPts, failSegmentBytes);");
        AssertContains(rotateFailureBlock, "FLASHBACK_SINK_ROTATE_FAIL_SEGMENT_REGISTERED");
        AssertContains(rotateFailureBlock, "FLASHBACK_SINK_ROTATE_FAIL_SEGMENT_REGISTER_FAIL");
        AssertContains(rotateFailureBlock, "_segmentStartPts = currentPts;");
        AssertOccursBefore(rotateFailureBlock, "_bufferManager.OnSegmentCompleted(completedPath, _segmentStartPts, failPts, failSegmentBytes);", "_segmentStartPts = currentPts;");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_ForceRotateRejectsFailedEncoder()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        var forceRotateBlock = ExtractTextBetween(
            sourceText,
            "public FlashbackForceRotateResult ForceRotateForExport",
            "    private bool TryCancelForceRotate");
        AssertContains(forceRotateBlock, "CancellationToken cancellationToken = default");
        AssertContains(forceRotateBlock, "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(forceRotateBlock, "if (inPoint < TimeSpan.Zero || outPoint <= inPoint)");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_RANGE");
        AssertOccursBefore(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_RANGE", "var request = new ForceRotateRequest();");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_INACTIVE");
        AssertContains(forceRotateBlock, "if (_encodingFailure != null || _encodingTask?.IsCompleted == true)");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED");
        AssertContains(forceRotateBlock, "return FlashbackForceRotateResult.Failed();");
        AssertContains(forceRotateBlock, "var request = new ForceRotateRequest();");
        AssertContains(forceRotateBlock, "if (!_started || _disposed || _encodingFailure != null || _encodingTask?.IsCompleted == true)");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_AFTER_LOCK");
        AssertOccursBefore(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_REJECTED_AFTER_LOCK", "_forceRotateRequest = request;");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_ForceRotateSkipsCompletedPendingRequest()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs").Replace("\r\n", "\n");
        var forceRotateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotate.cs").Replace("\r\n", "\n");
        var forceRotateExecutionText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.ForceRotateExecution.cs").Replace("\r\n", "\n");
        var loopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs").Replace("\r\n", "\n");

        var loopBlock = ExtractTextBetween(
            loopText,
            "if (Volatile.Read(ref _forceRotateRequested))",
            "                if (videoQueue.Reader.Completion.IsCompleted");
        var executionBlock = ExtractTextBetween(
            forceRotateExecutionText,
            "private bool ProcessPendingForceRotate(",
            "    }\n}");

        AssertContains(sourceText, "private sealed class ForceRotateRequest");
        AssertContains(forceRotateText, "private sealed class ForceRotateRequest");
        AssertContains(forceRotateText, "private const int ForceRotateCommittedGraceMs = 1_000;");
        AssertContains(forceRotateText, "public bool TryBeginCommit()\n            => Interlocked.CompareExchange(ref _state, StateCommitting, StatePending) == StatePending;");
        AssertContains(forceRotateText, "public bool TryCancel()");
        AssertContains(forceRotateText, "public void Complete(IReadOnlyList<string> paths)");
        AssertDoesNotContain(rootText, "private sealed class ForceRotateRequest");
        AssertDoesNotContain(rootText, "private const int ForceRotateCommittedGraceMs = 1_000;");
        AssertContains(loopBlock, "if (ProcessPendingForceRotate(videoQueue, audioQueue, microphoneQueue, gpuQueue))");
        AssertContains(loopBlock, "madeProgress = true;\n                        continue;");
        AssertContains(executionBlock, "localRequest = _forceRotateRequest;\n            _forceRotateRequest = null;");
        AssertContains(executionBlock, "if (localRequest == null)\n            {\n                Logger.Log(\"FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=no_pending_request\");\n                return true;\n            }");
        AssertOccursBefore(executionBlock, "FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=no_pending_request", "while (DrainAudioPackets(audioQueue.Reader, AudioDrainBatchLimit))");
        AssertContains(executionBlock, "if (localRequest.IsCompleted)\n            {\n                Logger.Log(\"FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed\");\n                return true;\n            }");
        AssertOccursBefore(executionBlock, "FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed", "while (DrainAudioPackets(audioQueue.Reader, AudioDrainBatchLimit))");
        AssertContains(executionBlock, "var forceRotateDrainAborted = ShouldAbortForceRotateDrain(localRequest, \"before_drain\", inFlightCount);");
        AssertContains(sourceText, "private const int AudioDrainBatchLimit = 128;");
        AssertContains(executionBlock, "while (DrainAudioPackets(audioQueue.Reader, AudioDrainBatchLimit))");
        AssertContains(executionBlock, "while (DrainMicrophonePackets(microphoneQueue.Reader, AudioDrainBatchLimit))");
        AssertContains(executionBlock, "while (DrainGpuPackets(gpuQueue.Reader, GpuDrainBatchLimit))");
        AssertContains(executionBlock, "while (DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit))");
        AssertDoesNotContain(executionBlock, "while (DrainGpuPackets(gpuQueue.Reader))");
        AssertDoesNotContain(executionBlock, "while (DrainVideoPackets(videoQueue.Reader))");
        AssertContains(executionBlock, "if (ShouldAbortForceRotateDrain(localRequest, \"audio\", inFlightCount))");
        AssertContains(executionBlock, "if (ShouldAbortForceRotateDrain(localRequest, \"microphone\", inFlightCount))");
        AssertContains(executionBlock, "if (ShouldAbortForceRotateDrain(localRequest, \"gpu\", inFlightCount))");
        AssertContains(executionBlock, "if (ShouldAbortForceRotateDrain(localRequest, \"video\", inFlightCount))");
        AssertContains(executionBlock, "if (forceRotateDrainAborted)\n            {\n                return true;\n            }");
        AssertOccursBefore(executionBlock, "if (forceRotateDrainAborted)\n            {\n                return true;\n            }", "var currentPts = ResolveEncoderPts();");
        AssertContains(executionBlock, "if (localRequest.IsCompleted)\n            {\n                Logger.Log(\"FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed_after_drain\");\n                return true;\n            }");
        AssertOccursBefore(executionBlock, "while (DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit))", "FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed_after_drain");
        AssertOccursBefore(executionBlock, "FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed_after_drain", "var currentPts = ResolveEncoderPts();");
        AssertContains(executionBlock, "if (!localRequest.TryBeginCommit())\n                {\n                    Logger.Log(\"FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed_before_rotate\");\n                    return true;\n                }");
        AssertOccursBefore(executionBlock, "FLASHBACK_SINK_FORCE_ROTATE_SKIP reason=request_completed_before_rotate", "if (!RotateSegment(currentPts))");
        AssertContains(sourceText, "private static bool ShouldAbortForceRotateDrain(");
        AssertContains(sourceText, "if (!request.IsCompleted)");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_SINK_FORCE_ROTATE_ABORT_DRAIN phase={phase} in_flight_rounds={inFlightRounds}\");");
        AssertContains(sourceText, "private bool DrainAudioPackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(sourceText, "private bool DrainMicrophonePackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(sourceText, "while (drainedCount < maxPackets && reader.TryRead(out var packet))");
        AssertContains(executionBlock, "catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_SINK_FORCE_ROTATE_FAIL type={ex.GetType().Name} msg={ex.Message}\");\n            localRequest?.CompleteEmpty();\n            throw;\n        }");
        AssertOccursBefore(executionBlock, "localRequest?.CompleteEmpty();\n            throw;", "finally\n        {\n            lock (_videoQueueSync)");
        AssertContains(executionBlock, "finally\n        {\n            lock (_videoQueueSync)\n            {\n                Volatile.Write(ref _forceRotateDraining, false);\n            }\n        }");

        var forceRotateBlock = ExtractTextBetween(
            sourceText,
            "public FlashbackForceRotateResult ForceRotateForExport",
            "    private bool TryCancelForceRotate");
        AssertContains(forceRotateBlock, "if (!request.Task.Wait(TimeSpan.FromSeconds(timeoutSeconds), cancellationToken))");
        AssertContains(forceRotateBlock, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_CANCELLED");
        AssertContains(forceRotateBlock, "var cancelled = TryCancelForceRotate(request);");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_TIMEOUT_COMMITTED");
        AssertContains(sourceText, "private const int ForceRotateCommittedGraceMs = 1_000;");
        AssertContains(forceRotateBlock, "if (request.Task.Wait(TimeSpan.FromMilliseconds(ForceRotateCommittedGraceMs)))\n                    {\n                        return FlashbackForceRotateResult.Completed(request.Task.GetAwaiter().GetResult());\n                    }");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_TIMEOUT_COMMITTED_PENDING");
        AssertContains(forceRotateBlock, "return FlashbackForceRotateResult.CommittedPending();");
        AssertContains(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_CANCELLED_COMMITTED");
        AssertContains(forceRotateBlock, "return FlashbackForceRotateResult.CanceledBeforeCommit();");
        AssertContains(forceRotateBlock, "return FlashbackForceRotateResult.Completed(request.Task.GetAwaiter().GetResult());");
        AssertDoesNotContain(forceRotateBlock, "FLASHBACK_SINK_FORCE_ROTATE_CANCELLED_COMMITTED\");\n                _ = request.Task.GetAwaiter().GetResult();");
        AssertDoesNotContain(forceRotateBlock, "return request.Task.Result;");
        AssertDoesNotContain(sourceText, "_forceRotateTcs");
        AssertDoesNotContain(sourceText, "localTcs.Task.IsCompleted");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_FatalSegmentRegistrationFailuresAreLogged()
    {
        var sourceText = ReadFlashbackEncoderSinkSource();

        var fatalBlock = ExtractTextBetween(
            sourceText,
            "catch (Exception ex)\n        {\n            Logger.Log($\"FLASHBACK_SINK_ENCODING_LOOP_FATAL",
            "            ReturnAllRemainingQueuedBuffers();");
        AssertContains(fatalBlock, "catch (Exception segmentEx)");
        AssertContains(fatalBlock, "FLASHBACK_SINK_FATAL_SEGMENT_REGISTER_FAIL");
        AssertContains(fatalBlock, "Preserve the original fatal error.");

        return Task.CompletedTask;
    }
}
