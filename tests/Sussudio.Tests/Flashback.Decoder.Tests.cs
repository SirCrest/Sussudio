using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackDecoder_DiscardedAudioFramesAreUnreffed()
    {
        var sourceText = ReadFlashbackDecoderSource();

        var audioDecodeBlock = ExtractTextBetween(
            sourceText,
            "private void DecodeAndDeliverAudioPacket",
            "// ── Private: Frame Conversion");
        AssertContains(audioDecodeBlock, "if (callback == null)\n            {\n                ffmpeg.av_frame_unref(_audioFrame);\n                continue; // Codec advanced, but no delivery during seek/scrub\n            }");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_MjpegPlaybackUsesSingleThreadLowLatencyDecode()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "if (codecPar->codec_id == AVCodecID.AV_CODEC_ID_MJPEG)\n        {\n            _videoCodecCtx->thread_count = 1;\n        }");
        AssertOccursBefore(
            sourceText,
            "if (codecPar->codec_id == AVCodecID.AV_CODEC_ID_MJPEG)",
            "ThrowIfError(\n            ffmpeg.avcodec_open2(_videoCodecCtx, codec, null),");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_PtsConversionRejectsInvalidTimestamps()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "var pts = DecodePtsToTimeSpan(ResolveBestEffortFrameTimestamp(_videoFrame), _videoTimeBase);");
        AssertContains(sourceText, "var pts = DecodePtsToTimeSpan(ResolveBestEffortFrameTimestamp(_audioFrame), _audioTimeBase);");
        AssertContains(sourceText, "var streamTimestamp = ToStreamTimestamp(target, _videoTimeBase);");
        AssertContains(sourceText, "_formatCtx, _videoStreamIndex, streamTimestamp, ffmpeg.AVSEEK_FLAG_BACKWARD);");
        AssertContains(sourceText, "var timestampUs = ToAvTimeBaseTimestamp(target);");
        AssertContains(sourceText, "_formatCtx, -1, timestampUs, ffmpeg.AVSEEK_FLAG_BACKWARD);");
        AssertContains(sourceText, "FLASHBACK_DECODER_SEEK_FALLBACK_OK");
        AssertContains(sourceText, "private static TimeSpan DecodePtsToTimeSpan(long pts, AVRational timeBase)");
        AssertContains(sourceText, "private static long ResolveBestEffortFrameTimestamp(AVFrame* frame)");
        AssertContains(sourceText, "frame->best_effort_timestamp != ffmpeg.AV_NOPTS_VALUE");
        AssertContains(sourceText, "return frame->best_effort_timestamp;");
        AssertContains(sourceText, "return frame->pts;");
        AssertContains(sourceText, "if (pts == ffmpeg.AV_NOPTS_VALUE || timeBase.num <= 0 || timeBase.den <= 0)");
        AssertContains(sourceText, "if (!double.IsFinite(seconds) || seconds <= 0 || seconds > TimeSpan.MaxValue.TotalSeconds)");
        AssertContains(sourceText, "private static long ToAvTimeBaseTimestamp(TimeSpan value)");
        AssertContains(sourceText, "private static long ToStreamTimestamp(TimeSpan value, AVRational timeBase)");
        AssertContains(sourceText, "if (value <= TimeSpan.Zero || timeBase.num <= 0 || timeBase.den <= 0)");
        AssertContains(sourceText, "var timestamp = value.TotalSeconds * timeBase.den / timeBase.num;");
        AssertContains(sourceText, "if (!double.IsFinite(microseconds) || microseconds >= long.MaxValue)\n        {\n            return long.MaxValue;\n        }");
        AssertContains(sourceText, "if (!double.IsFinite(timestamp) || timestamp >= long.MaxValue)\n        {\n            return long.MaxValue;\n        }");
        AssertContains(sourceText, "private bool _suppressRecoverableSeekLogsForNextVideoFrame;");
        AssertContains(sourceText, "_suppressRecoverableSeekLogsForNextVideoFrame = true;");
        AssertContains(sourceText, "using var recoverableSeekLogScope = BeginRecoverableSeekLogSuppressionIfNeeded();");
        AssertContains(sourceText, "private IDisposable? BeginRecoverableSeekLogSuppressionIfNeeded()");
        AssertContains(sourceText, "return LibAvEncoder.SuppressRecoverableSeekFfmpegLogs();");
        AssertContains(sourceText, "_suppressRecoverableSeekLogsForNextVideoFrame = false;");
        AssertDoesNotContain(sourceText, "(long)(target.TotalSeconds * ffmpeg.AV_TIME_BASE)");
        AssertDoesNotContain(sourceText, "var seconds = (double)_videoFrame->pts * _videoTimeBase.num / _videoTimeBase.den;\n            pts = TimeSpan.FromSeconds(seconds);");
        AssertDoesNotContain(sourceText, "var seconds = (double)_audioFrame->pts * _audioTimeBase.num / _audioTimeBase.den;\n            pts = TimeSpan.FromSeconds(seconds);");
        AssertDoesNotContain(sourceText, "DecodePtsToTimeSpan(_videoFrame->pts, _videoTimeBase)");
        AssertDoesNotContain(sourceText, "DecodePtsToTimeSpan(_audioFrame->pts, _audioTimeBase)");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_InputStreamsAndFrameSizesAreBounded()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "private const int MaxSupportedInputStreams = 64;");
        AssertContains(sourceText, "private const int MaxDecodedVideoDimension = 8192;");
        AssertContains(sourceText, "private const int MaxDecodedVideoFrameBytes = 512 * 1024 * 1024;");
        AssertContains(sourceText, "private const int MaxMpegTsProbeSizeBytes = 20 * 1024 * 1024;");
        AssertContains(sourceText, "private const int MaxMpegTsAnalyzeDurationUs = 5 * 1000 * 1000;");
        AssertContains(sourceText, "_formatCtx->probesize = MaxMpegTsProbeSizeBytes;");
        AssertContains(sourceText, "_formatCtx->max_analyze_duration = MaxMpegTsAnalyzeDurationUs;");
        AssertContains(sourceText, "if (!TryGetInputStreamCount(_formatCtx, out var streamCount, out var streamCountFailure))");
        AssertContains(sourceText, "if (!IsValidStreamIndex(_videoStreamIndex, streamCount))");
        AssertContains(sourceText, "if (_audioStreamIndex >= 0 && !IsValidStreamIndex(_audioStreamIndex, streamCount))");
        AssertContains(sourceText, "FLASHBACK_DECODER_AUDIO_WARN reason=invalid_stream_index");
        AssertContains(sourceText, "ValidateVideoDimensions(_videoWidth, _videoHeight);");
        AssertContains(sourceText, "private static void ValidateVideoDimensions(int width, int height)");
        AssertContains(sourceText, "width > MaxDecodedVideoDimension");
        AssertContains(sourceText, "height > MaxDecodedVideoDimension");
        AssertContains(sourceText, "(width & 1) != 0");
        AssertContains(sourceText, "var pixels = (long)width * height;");
        AssertContains(sourceText, "if (bytes <= 0 || bytes > MaxDecodedVideoFrameBytes || bytes > int.MaxValue)");
        AssertDoesNotContain(sourceText, "return width * height * 2 + width * (height / 2) * 2;");
        AssertDoesNotContain(sourceText, "return width * height + width * (height / 2);");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_AudioOutputBuffersAreBounded()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "private const int MaxDecodedAudioFrameBytes = 16 * 1024 * 1024;");
        AssertContains(sourceText, "byte[]? result = null;\n        var returnResultToPool = true;");
        AssertContains(sourceText, "if (inputSamples <= 0)\n            {\n                return new DecodedAudioChunk { Samples = Array.Empty<byte>(), ValidLength = 0, Pts = pts };\n            }");
        AssertContains(sourceText, "maxOutputSamples = ToBoundedAudioSampleCount((long)inputSamples * 2);");
        AssertContains(sourceText, "if (!TryCalculateAudioBufferBytes(maxOutputSamples, out var outputBytesNeeded))");
        AssertContains(sourceText, "FLASHBACK_DECODER_AUDIO_WARN reason=invalid_output_size");
        AssertContains(sourceText, "if (!TryCalculateAudioBufferBytes(outputSamplesProduced, out var validBytes) || validBytes > result.Length)");
        AssertContains(sourceText, "FLASHBACK_DECODER_AUDIO_WARN reason=invalid_converted_size");
        AssertContains(sourceText, "returnResultToPool = false;");
        AssertContains(sourceText, "finally\n        {\n            ffmpeg.av_frame_unref(_audioFrame);\n            if (returnResultToPool && result is { Length: > 0 })");
        AssertContains(sourceText, "ArrayPool<byte>.Shared.Return(result);");
        AssertContains(sourceText, "private static int ToBoundedAudioSampleCount(long sampleCount)");
        AssertContains(sourceText, "private static bool TryCalculateAudioBufferBytes(int sampleCount, out int bytes)");
        AssertContains(sourceText, "var calculated = (long)sampleCount * OutputAudioChannels * sizeof(float);");
        AssertDoesNotContain(sourceText, "var outputBytesNeeded = maxOutputSamples * OutputAudioChannels * sizeof(float);");
        AssertDoesNotContain(sourceText, "var validBytes = outputSamplesProduced * OutputAudioChannels * sizeof(float);");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_AudioSetupLivesInAudioOutputPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var audioOutputText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.AudioOutput.cs")
            .Replace("\r\n", "\n");

        AssertDoesNotContain(rootText, "private void InitializeAudioDecoder()");
        AssertDoesNotContain(rootText, "private void InitializeAudioResampler()");
        AssertContains(audioOutputText, "private void InitializeAudioDecoder()");
        AssertContains(audioOutputText, "private void InitializeAudioResampler()");
        AssertContains(audioOutputText, "FLASHBACK_DECODER_AUDIO codec=");
        AssertContains(audioOutputText, "swr_alloc_set_opts2");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_SoftwareFramePlanesAreValidated()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "if (actualFormat != AVPixelFormat.AV_PIX_FMT_NONE && actualFormat != _decodedPixelFormat)");
        AssertContains(sourceText, "if (!TryValidateSoftwareVideoFrame(_videoFrame, _decodedPixelFormat, _videoWidth, _videoHeight, _isHdr, out var frameFailure))");
        AssertContains(sourceText, "FLASHBACK_DECODER_VIDEO_WARN reason=invalid_software_frame");
        AssertContains(sourceText, "ffmpeg.av_frame_unref(_videoFrame);\n            return default;");
        var softwareOutputBlock = ExtractTextBetween(
            sourceText,
            "var outputSize = CalculateFrameBufferSize(_videoWidth, _videoHeight, _isHdr);",
            "    private void CopyFramePlanesToBuffer");
        AssertContains(softwareOutputBlock, "finally\n        {\n            ffmpeg.av_frame_unref(_videoFrame);\n        }");
        AssertOccursBefore(softwareOutputBlock, "CopyFramePlanesToBuffer((byte*)dataPtr, outputSize);", "finally\n        {\n            ffmpeg.av_frame_unref(_videoFrame);\n        }");
        AssertContains(sourceText, "private static bool TryValidateSoftwareVideoFrame(");
        AssertContains(sourceText, "width_mismatch frame={frame->width} expected={width}");
        AssertContains(sourceText, "height_mismatch frame={frame->height} expected={height}");
        AssertContains(sourceText, "format == AVPixelFormat.AV_PIX_FMT_YUV420P");
        AssertContains(sourceText, "format == AVPixelFormat.AV_PIX_FMT_YUV420P10LE");
        AssertContains(sourceText, "failure = $\"unsupported_format:{format}\";");
        AssertContains(sourceText, "private static bool TryValidatePlane(AVFrame* frame, int planeIndex, int minLineSize, out string failure)");
        AssertContains(sourceText, "var plane = (uint)planeIndex;");
        AssertContains(sourceText, "failure = $\"plane_{planeIndex}_null\";");
        AssertContains(sourceText, "failure = $\"plane_{planeIndex}_linesize:{frame->linesize[plane]}<{minLineSize}\";");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_D3D11FramesAreValidated()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "if (!TryValidateD3D11VideoFrame(clonedFrame, _videoWidth, _videoHeight, out var d3dFrameFailure))");
        AssertContains(sourceText, "Logger.Log($\"FLASHBACK_DECODER_VIDEO_WARN reason=invalid_d3d11_frame detail='{d3dFrameFailure}' w={_videoWidth} h={_videoHeight}\");\n                ffmpeg.av_frame_free(&clonedFrame);\n                return default;");
        AssertContains(sourceText, "private static bool TryValidateD3D11VideoFrame(AVFrame* frame, int width, int height, out string failure)");
        AssertContains(sourceText, "failure = \"texture_null\";");
        AssertContains(sourceText, "failure = $\"subresource_out_of_range:{subresource}\";");
        AssertOccursBefore(sourceText, "TryValidateD3D11VideoFrame(clonedFrame, _videoWidth, _videoHeight", "var texturePtr = (IntPtr)clonedFrame->data[0];");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_HeldFrameCleanupIsBestEffort()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "private static void ReleaseHeldFrameBestEffort(DecodedVideoFrame frame, string operation)");
        AssertContains(sourceText, "FLASHBACK_DECODER_RELEASE_HELD_FRAME_WARN");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(_pendingVideoFrame, \"seek_keyframe_pending\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(bestFrame.Value, \"seek_replace_best\");");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(bestFrame.Value, \"seek_best_superseded\");");
        AssertContains(sourceText, "var bestFrameTransferred = false;");
        AssertContains(sourceText, "bestFrameTransferred = true;\n                        return true;");
        AssertContains(sourceText, "finally\n        {\n            if (!bestFrameTransferred && bestFrame != null)\n            {\n                ReleaseHeldFrameBestEffort(bestFrame.Value, \"seek_best_abandoned\");\n            }\n        }");
        AssertContains(sourceText, "ReleaseHeldFrameBestEffort(_pendingVideoFrame, \"close_pending\");");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_DecodeLoopsObserveCancellation()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "public bool SeekToKeyframe(TimeSpan target, CancellationToken cancellationToken = default)");
        AssertContains(sourceText, "public bool SeekTo(TimeSpan target, CancellationToken cancellationToken = default)");
        AssertContains(sourceText, "public bool TryDecodeNextVideoFrame(out DecodedVideoFrame frame, CancellationToken cancellationToken = default)");
        AssertContains(sourceText, "private bool FeedNextVideoPacket(CancellationToken cancellationToken = default)");
        AssertContains(sourceText, "if (!SeekToKeyframe(target, cancellationToken))");
        AssertContains(sourceText, "if (!TryDecodeNextVideoFrame(out var frame, cancellationToken))");
        AssertContains(sourceText, "if (!FeedNextVideoPacket(cancellationToken))");
        AssertContains(sourceText, "cancellationToken.ThrowIfCancellationRequested();");

        var seekToBlock = ExtractTextBetween(
            sourceText,
            "public bool SeekTo(TimeSpan target",
            "    /// <summary>\n    /// Decodes the next video frame.");
        AssertContains(seekToBlock, "cancellationToken.ThrowIfCancellationRequested();");
        AssertOccursBefore(seekToBlock, "cancellationToken.ThrowIfCancellationRequested();\n                if (!TryDecodeNextVideoFrame", "if (!TryDecodeNextVideoFrame(out var frame, cancellationToken))");

        var decodeBlock = ExtractTextBetween(
            sourceText,
            "public bool TryDecodeNextVideoFrame(out DecodedVideoFrame frame",
            "    public void Dispose()");
        AssertContains(decodeBlock, "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(decodeBlock, "if (!FeedNextVideoPacket(cancellationToken))");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_RejectsInitializeAfterDispose()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        using var decoder = (IDisposable)Activator.CreateInstance(decoderType)!;
        var initialize = decoderType.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackDecoder.Initialize not found.");

        decoder.Dispose();

        try
        {
            initialize.Invoke(decoder, new object[] { IntPtr.Zero, IntPtr.Zero });
            throw new InvalidOperationException("Expected disposed decoder initialization to be rejected.");
        }
        catch (TargetInvocationException ex) when (ex.InnerException is ObjectDisposedException)
        {
        }

        var sourceText = ReadFlashbackDecoderSource();
        var initializeBlock = ExtractTextBetween(
            sourceText,
            "public void Initialize(IntPtr d3dDevicePtr, IntPtr d3dContextPtr)",
            "    /// <summary>\n    /// Opens a .ts or .mp4 file for decoding.");
        AssertContains(initializeBlock, "ThrowIfDisposed();");
        AssertOccursBefore(initializeBlock, "ThrowIfDisposed();", "if (_initialized)");

        return Task.CompletedTask;
    }

    private static Task FlashbackDecoder_ClearsAudioCallbackOnDispose()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        using var decoder = (IDisposable)Activator.CreateInstance(decoderType)!;
        var callbackProperty = decoderType.GetProperty("AudioChunkCallback", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("FlashbackDecoder.AudioChunkCallback not found.");

        var callbackType = callbackProperty.PropertyType;
        var callbackParameter = Expression.Parameter(callbackType.GetGenericArguments()[0], "chunk");
        var callback = Expression.Lambda(callbackType, Expression.Empty(), callbackParameter).Compile();
        callbackProperty.SetValue(decoder, callback);

        decoder.Dispose();

        AssertEqual(null, callbackProperty.GetValue(decoder), "Disposed decoder clears audio callback");

        var sourceText = ReadFlashbackDecoderSource();
        var disposeBlock = ExtractTextBetween(
            sourceText,
            "public void Dispose()",
            "private bool FeedNextVideoPacket");
        AssertContains(disposeBlock, "AudioChunkCallback = null;");
        AssertOccursBefore(disposeBlock, "AudioChunkCallback = null;", "CloseFileCore();");

        return Task.CompletedTask;
    }

}
