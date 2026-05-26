using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task FlashbackDecoder_DiscardedAudioFramesAreUnreffed()
    {
        var sourceText = ReadFlashbackDecoderSource();

        var audioDecodeBlock = ExtractTextBetween(
            sourceText,
            "private void DecodeAndDeliverAudioPacket",
            "// ── Private: Frame Conversion");
        AssertContains(audioDecodeBlock, "if (callback == null)\n            {\n                ffmpeg.av_frame_unref(_audioFrame);\n                continue; // Codec advanced, but no delivery during seek/scrub\n            }");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_MjpegPlaybackUsesSingleThreadLowLatencyDecode()
    {
        var sourceText = ReadFlashbackDecoderSource();

        AssertContains(sourceText, "if (codecPar->codec_id == AVCodecID.AV_CODEC_ID_MJPEG)\n        {\n            _videoCodecCtx->thread_count = 1;\n        }");
        AssertOccursBefore(
            sourceText,
            "if (codecPar->codec_id == AVCodecID.AV_CODEC_ID_MJPEG)",
            "ThrowIfError(\n            ffmpeg.avcodec_open2(_videoCodecCtx, codec, null),");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_PtsConversionRejectsInvalidTimestamps()
    {
        var sourceText = ReadFlashbackDecoderSource();
        var timestampFragmentPath = Path.Combine(
            GetRepoRoot(),
            "Sussudio",
            "Services",
            "Flashback",
            "FlashbackDecoder.Timestamps.cs");

        AssertEqual(false, File.Exists(timestampFragmentPath), "Flashback decoder timestamp helpers stay folded into caller owners");
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

    internal static Task FlashbackDecoder_InputStreamsAndFrameSizesAreBounded()
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

    internal static Task FlashbackDecoder_AudioOutputBuffersAreBounded()
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

    internal static Task FlashbackDecoder_AudioSetupLivesInAudioOutputPartial()
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

    internal static Task FlashbackDecoder_SoftwareFramePlanesAreValidated()
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

    internal static Task FlashbackDecoder_D3D11FramesAreValidated()
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

    internal static Task FlashbackDecoder_HeldFrameCleanupIsBestEffort()
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

    internal static Task FlashbackDecoder_DecodeLoopsObserveCancellation()
    {
        var sourceText = ReadFlashbackDecoderSource();
        var decodeLoopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.Playback.cs")
            .Replace("\r\n", "\n");

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

        AssertContains(decodeLoopText, "cancellationToken.ThrowIfCancellationRequested();");
        AssertContains(decodeLoopText, "if (!FeedNextVideoPacket(cancellationToken))");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_RejectsInitializeAfterDispose()
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

        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var d3d11Text = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.VideoSetup.cs")
            .Replace("\r\n", "\n");
        AssertDoesNotContain(rootText, "public void Initialize(IntPtr d3dDevicePtr, IntPtr d3dContextPtr)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.D3D11.cs")),
            "Flashback decoder D3D11VA initialization lives with video decoder setup.");
        var initializeBlock = ExtractTextBetween(
            d3d11Text,
            "public void Initialize(IntPtr d3dDevicePtr, IntPtr d3dContextPtr)",
            "    private static AVPixelFormat GetFormatD3D11");
        AssertContains(initializeBlock, "ThrowIfDisposed();");
        AssertOccursBefore(initializeBlock, "ThrowIfDisposed();", "if (_initialized)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_ClearsAudioCallbackOnDispose()
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

        var sourceText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var disposeBlock = ExtractTextBetween(
            sourceText,
            "public void Dispose()",
            "// Free persistent D3D11VA device context");
        AssertContains(disposeBlock, "AudioChunkCallback = null;");
        AssertOccursBefore(disposeBlock, "AudioChunkCallback = null;", "CloseFileCore();");

        return Task.CompletedTask;
    }

    internal static Task FlashbackSuppressedExceptionsUseAppLogs()
    {
        var decoderText = ReadFlashbackDecoderSource();
        var d3d11Text = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.VideoSetup.cs").Replace("\r\n", "\n");
        var d3d11DiscoveryText = d3d11Text;

        var openFileBlock = ExtractTextBetween(
            decoderText,
            "public void OpenFile(string filePath)",
            "    /// <summary>\n    /// Closes the currently open file");
        AssertContains(openFileBlock, "FLASHBACK_DECODER_OPEN_WARN");
        AssertContains(openFileBlock, "CloseFileCore();\n            throw;");
        AssertContains(decoderText, "var closedPath = _currentFilePath;\n        CloseFileCore();\n        Logger.Log($\"FLASHBACK_DECODER_CLOSE path='{closedPath}'\");");
        AssertContains(decoderText, "_currentPosition = TimeSpan.Zero;\n        _currentFilePath = null;\n        _needsConvert = false;");
        AssertDoesNotContain(openFileBlock, "System.Diagnostics.Trace.TraceWarning");
        AssertContains(decoderText, "FLASHBACK_DECODER_INIT d3d11va=false reason=exception type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(d3d11Text, "var codec = FindD3D11VADecoder(codecPar->codec_id, out var codecName);");
        AssertContains(d3d11Text, "FLASHBACK_DECODER_D3D11VA_SKIP reason=no_d3d11_device_ctx_decoder");
        AssertContains(d3d11Text, "FLASHBACK_DECODER_D3D11VA_SKIP reason=exception type={ex.GetType().Name} msg='{ex.Message}'");
        AssertContains(d3d11Text, "private static string DescribeHardwareConfigs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.D3D11Discovery.cs")),
            "Flashback decoder D3D11VA discovery folded into video decoder setup owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.D3D11.cs")),
            "Flashback decoder D3D11VA setup folded into video decoder setup owner");
        AssertContains(d3d11DiscoveryText, "private static AVCodec* FindD3D11VADecoder(AVCodecID codecId, out string codecName)");
        AssertContains(d3d11DiscoveryText, "ffmpeg.avcodec_find_decoder_by_name(preferredName)");
        AssertContains(d3d11DiscoveryText, "AVCodecID.AV_CODEC_ID_AV1 => \"av1\"");
        AssertContains(d3d11DiscoveryText, "FLASHBACK_DECODER_D3D11VA_SELECT source=preferred codec={codecName}");
        AssertContains(d3d11DiscoveryText, "FLASHBACK_DECODER_D3D11VA_CANDIDATE source={source} codec={codecName} configs=[{hardwareConfigSummary}] d3d11_device_ctx={hasD3D11DeviceConfig}");
        AssertContains(d3d11DiscoveryText, "private static string DescribeHardwareConfigs(AVCodec* codec, out bool hasD3D11DeviceConfig)");
        AssertContains(d3d11DiscoveryText, "ffmpeg.avcodec_get_hw_config(codec, i)");
        AssertContains(d3d11DiscoveryText, "pixelFormat == AVPixelFormat.AV_PIX_FMT_D3D11");
        AssertContains(d3d11DiscoveryText, "deviceType == AVHWDeviceType.AV_HWDEVICE_TYPE_D3D11VA");
        AssertContains(d3d11DiscoveryText, "AvCodecHwConfigMethodHwDeviceCtx");
        AssertContains(d3d11DiscoveryText, "private static string FormatHardwareConfigMethods(int methods)");
        AssertContains(d3d11DiscoveryText, "private static string GetPixelFormatName(AVPixelFormat pixelFormat)");
        AssertContains(d3d11DiscoveryText, "private static string GetHardwareDeviceName(AVHWDeviceType deviceType)");

        return Task.CompletedTask;
    }



    // FlashbackDecoder: CalculateFrameBufferSize

    internal static Task FlashbackDecoder_CalculateFrameBufferSize_Nv12()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        var method = decoderType.GetMethod("CalculateFrameBufferSize",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CalculateFrameBufferSize not found.");

        // NV12: width * height + width * (height / 2)
        var size1080 = (int)method.Invoke(null, new object[] { 1920, 1080, false })!;
        AssertEqual(1920 * 1080 + 1920 * (1080 / 2), size1080, "NV12 1080p buffer size");

        var size720 = (int)method.Invoke(null, new object[] { 1280, 720, false })!;
        AssertEqual(1280 * 720 + 1280 * (720 / 2), size720, "NV12 720p buffer size");

        var size4k = (int)method.Invoke(null, new object[] { 3840, 2160, false })!;
        AssertEqual(3840 * 2160 + 3840 * (2160 / 2), size4k, "NV12 4K buffer size");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_CalculateFrameBufferSize_P010()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        var method = decoderType.GetMethod("CalculateFrameBufferSize",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CalculateFrameBufferSize not found.");

        // P010: width * height * 2 + width * (height / 2) * 2
        var size1080 = (int)method.Invoke(null, new object[] { 1920, 1080, true })!;
        AssertEqual(1920 * 1080 * 2 + 1920 * (1080 / 2) * 2, size1080, "P010 1080p buffer size");

        var size4k = (int)method.Invoke(null, new object[] { 3840, 2160, true })!;
        AssertEqual(3840 * 2160 * 2 + 3840 * (2160 / 2) * 2, size4k, "P010 4K buffer size");

        // P010 should be exactly 2x NV12
        var nv12Size = (int)method.Invoke(null, new object[] { 1920, 1080, false })!;
        AssertEqual(nv12Size * 2, size1080, "P010 is 2x NV12");

        return Task.CompletedTask;
    }

    // FlashbackDecoder: state guard properties

    internal static Task FlashbackDecoder_ValidationHelpersLiveWithRootLifecycle()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var videoOutputText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.VideoOutput.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "private static int CalculateFrameBufferSize(int width, int height, bool isHdr)");
        AssertContains(rootText, "private static void ValidateVideoDimensions(int width, int height)");
        AssertContains(rootText, "private static bool TryValidateSoftwareVideoFrame(");
        AssertContains(rootText, "private static bool TryValidatePlane(AVFrame* frame, int planeIndex, int minLineSize, out string failure)");
        AssertContains(rootText, "private static bool TryValidateD3D11VideoFrame(AVFrame* frame, int width, int height, out string failure)");
        AssertContains(rootText, "private static bool TryGetInputStreamCount(AVFormatContext* formatCtx, out int streamCount, out string failureMessage)");
        AssertContains(rootText, "private static bool IsValidStreamIndex(int streamIndex, int streamCount)");
        AssertDoesNotContain(videoOutputText, "private static bool TryValidateSoftwareVideoFrame(");
        AssertDoesNotContain(videoOutputText, "private static bool TryValidatePlane(AVFrame* frame, int planeIndex, int minLineSize, out string failure)");
        AssertDoesNotContain(videoOutputText, "private static bool TryValidateD3D11VideoFrame(AVFrame* frame, int width, int height, out string failure)");
        AssertContains(videoOutputText, "private void CopyFramePlanesToBuffer(");
        AssertContains(videoOutputText, "private void ConvertYuv420pToNv12(");
        AssertContains(videoOutputText, "private void ConvertYuv420p10leToP010(");
        AssertContains(videoOutputText, "private static void InterleaveUvRow(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.VideoConversion.cs")),
            "FlashbackDecoder.VideoConversion.cs folded into FlashbackDecoder.VideoOutput.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.Validation.cs")),
            "FlashbackDecoder validation helpers folded into decoder root");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_LifetimeCleanupLivesWithRootLifecycle()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "private void CloseFileCore()");
        AssertContains(rootText, "internal static void ReleaseHeldFrame(DecodedVideoFrame frame)");
        AssertContains(rootText, "private static void ReleaseHeldFrameBestEffort(DecodedVideoFrame frame, string operation)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.Lifetime.cs")),
            "FlashbackDecoder file-close cleanup lives with the root lifecycle owner");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_StateGuardsAndTimingLiveWithOwners()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var decodeLoopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.Playback.cs")
            .Replace("\r\n", "\n");

        AssertContains(decodeLoopText, "private void AddLastDecodeReceiveMs(double elapsedMs)");
        AssertContains(decodeLoopText, "private static double ElapsedMsSince(long startTimestamp)");
        AssertContains(rootText, "private static void ThrowIfError(int errorCode, string operation)");
        AssertContains(rootText, "private static string GetErrorString(int errorCode)");
        AssertContains(rootText, "private static InvalidOperationException CreateException(string message)");
        AssertContains(rootText, "private void ThrowIfNotInitialized()");
        AssertContains(rootText, "private void ThrowIfNotOpen()");
        AssertContains(rootText, "private void ThrowIfDisposed()");
        AssertDoesNotContain(rootText, "private void AddLastDecodeReceiveMs(double elapsedMs)");
        AssertDoesNotContain(rootText, "private static double ElapsedMsSince(long startTimestamp)");
        AssertDoesNotContain(decodeLoopText, "private static void ThrowIfError(int errorCode, string operation)");
        AssertDoesNotContain(decodeLoopText, "private void ThrowIfNotInitialized()");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_OutputTypesLiveWithDecoderRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.OutputTypes.cs")),
            "Flashback decoder output DTOs stay folded into the decoder root surface.");
        AssertContains(rootText, "internal readonly struct DecodedVideoFrame");
        AssertContains(rootText, "internal readonly struct DecodedAudioChunk");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_VideoSetupOwnsHardwareAndSoftwareSetup()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var videoSetupText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.VideoSetup.cs")
            .Replace("\r\n", "\n");

        AssertContains(videoSetupText, "private void InitializeVideoDecoder()");
        AssertContains(videoSetupText, "public void Initialize(IntPtr d3dDevicePtr, IntPtr d3dContextPtr)");
        AssertContains(videoSetupText, "private bool TryInitializeD3D11VADecoder(AVCodecParameters* codecPar)");
        AssertContains(videoSetupText, "private static AVCodec* FindD3D11VADecoder(AVCodecID codecId, out string codecName)");
        AssertContains(videoSetupText, "private void AllocateVideoOutputBuffers()");
        AssertDoesNotContain(rootText, "private void InitializeVideoDecoder()");
        AssertDoesNotContain(rootText, "public void Initialize(IntPtr d3dDevicePtr, IntPtr d3dContextPtr)");
        AssertDoesNotContain(rootText, "private void AllocateVideoOutputBuffers()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Flashback", "FlashbackDecoder.D3D11.cs")),
            "FlashbackDecoder D3D11VA setup folded into video setup owner");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_PlaybackOwnsSeekingAndDecodeLoop()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var playbackText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.Playback.cs")
            .Replace("\r\n", "\n");

        AssertContains(playbackText, "public bool SeekToKeyframe(TimeSpan target, CancellationToken cancellationToken = default)");
        AssertContains(playbackText, "public bool SeekTo(TimeSpan target, CancellationToken cancellationToken = default)");
        AssertContains(playbackText, "FLASHBACK_DECODER_SEEK_FALLBACK_OK");
        AssertContains(playbackText, "FLASHBACK_DECODER_SEEK_CAP_HIT");
        AssertContains(playbackText, "public bool TryDecodeNextVideoFrame(out DecodedVideoFrame frame, CancellationToken cancellationToken = default)");
        AssertContains(playbackText, "private bool FeedNextVideoPacket(CancellationToken cancellationToken = default)");
        AssertContains(playbackText, "private void AddLastDecodeReceiveMs(double elapsedMs)");
        AssertContains(playbackText, "private static double ElapsedMsSince(long startTimestamp)");
        AssertOccursBefore(
            playbackText,
            "public bool SeekTo(TimeSpan target, CancellationToken cancellationToken = default)",
            "public bool TryDecodeNextVideoFrame(out DecodedVideoFrame frame, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(rootText, "public bool SeekToKeyframe(TimeSpan target, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(rootText, "public bool SeekTo(TimeSpan target, CancellationToken cancellationToken = default)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_DecodeLoopLivesWithPlayback()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.cs")
            .Replace("\r\n", "\n");
        var playbackText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackDecoder.Playback.cs")
            .Replace("\r\n", "\n");

        AssertContains(playbackText, "private PlaybackDecodePhaseTimings _lastDecodePhaseTimings;");
        AssertContains(playbackText, "public PlaybackDecodePhaseTimings LastDecodePhaseTimings => _lastDecodePhaseTimings;");
        AssertContains(playbackText, "public readonly record struct PlaybackDecodePhaseTimings(");
        AssertContains(playbackText, "public bool TryDecodeNextVideoFrame(out DecodedVideoFrame frame, CancellationToken cancellationToken = default)");
        AssertContains(playbackText, "private bool FeedNextVideoPacket(CancellationToken cancellationToken = default)");
        AssertContains(playbackText, "ffmpeg.av_read_frame(_formatCtx, _packet)");
        AssertContains(playbackText, "DecodeAndDeliverAudioPacket(_packet);");
        AssertDoesNotContain(rootText, "private PlaybackDecodePhaseTimings _lastDecodePhaseTimings;");
        AssertDoesNotContain(rootText, "public PlaybackDecodePhaseTimings LastDecodePhaseTimings => _lastDecodePhaseTimings;");
        AssertDoesNotContain(rootText, "public readonly record struct PlaybackDecodePhaseTimings(");
        AssertDoesNotContain(rootText, "public bool TryDecodeNextVideoFrame(out DecodedVideoFrame frame, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(rootText, "private bool FeedNextVideoPacket(CancellationToken cancellationToken = default)");

        return Task.CompletedTask;
    }

    internal static Task FlashbackDecoder_DefaultState_IsNotOpenAndNotInitialized()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        var decoder = Activator.CreateInstance(decoderType)!;

        var isOpenProp = decoderType.GetProperty("IsOpen",
            BindingFlags.Public | BindingFlags.Instance);
        AssertNotNull(isOpenProp, "FlashbackDecoder.IsOpen");
        AssertEqual(false, (bool)isOpenProp!.GetValue(decoder)!, "IsOpen default");

        return Task.CompletedTask;
    }

    // FlashbackDecoder: Dispose is safe when not initialized

    internal static Task FlashbackDecoder_DisposeBeforeInitialize_DoesNotThrow()
    {
        var decoderType = RequireType("Sussudio.Services.Flashback.FlashbackDecoder");
        var decoder = Activator.CreateInstance(decoderType)!;

        // Dispose via IDisposable
        if (decoder is IDisposable disposable)
        {
            disposable.Dispose();
        }
        else
        {
            var disposeMethod = decoderType.GetMethod("Dispose",
                BindingFlags.Public | BindingFlags.Instance);
            disposeMethod?.Invoke(decoder, null);
        }

        return Task.CompletedTask;
    }
}
