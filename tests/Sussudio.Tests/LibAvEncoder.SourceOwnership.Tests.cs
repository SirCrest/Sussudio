using System.Threading.Tasks;

static partial class Program
{
    private static Task LibAvEncoder_PacketWritingLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var packetWritingText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.PacketWriting.cs")
            .Replace("\r\n", "\n");

        AssertContains(packetWritingText, "private void DrainEncoderPackets()");
        AssertContains(packetWritingText, "private void WriteFilteredPackets()");
        AssertContains(packetWritingText, "private void DrainBsfPackets()");
        AssertContains(packetWritingText, "private void WritePacket(AVPacket* packet, bool useBsfTimeBase)");
        AssertDoesNotContain(rootText, "private void DrainEncoderPackets()");
        AssertDoesNotContain(rootText, "private void WriteFilteredPackets()");
        AssertDoesNotContain(rootText, "private void DrainBsfPackets()");
        AssertDoesNotContain(rootText, "private void WritePacket(AVPacket* packet, bool useBsfTimeBase)");

        return Task.CompletedTask;
    }

    private static Task LibAvEncoder_FrameCopyLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var frameCopyText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.FrameCopy.cs")
            .Replace("\r\n", "\n");

        AssertContains(frameCopyText, "private void CopyPackedFrameToVideoFrame(ReadOnlySpan<byte> frameData, LibAvEncoderOptions options)");
        AssertContains(frameCopyText, "private static void CopyPlane(byte* sourceStart, byte* destinationStart, int destinationStride, int rowBytes, int rowCount)");
        AssertContains(frameCopyText, "Buffer.MemoryCopy(");
        AssertDoesNotContain(rootText, "private void CopyPackedFrameToVideoFrame(ReadOnlySpan<byte> frameData, LibAvEncoderOptions options)");
        AssertDoesNotContain(rootText, "private static void CopyPlane(byte* sourceStart, byte* destinationStart, int destinationStride, int rowBytes, int rowCount)");

        return Task.CompletedTask;
    }

    private static Task LibAvEncoder_VideoSubmissionLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var videoSubmissionText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.VideoSubmission.cs")
            .Replace("\r\n", "\n");
        var hardwareSubmissionText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.HardwareSubmission.cs")
            .Replace("\r\n", "\n");

        AssertContains(videoSubmissionText, "public void SendVideoFrame(ReadOnlySpan<byte> frameData, int width, int height)");
        AssertContains(videoSubmissionText, "CopyPackedFrameToVideoFrame(frameData[..expectedSize], options);");
        AssertDoesNotContain(videoSubmissionText, "public void SendGpuVideoFrame(IntPtr d3d11Texture, int subresourceIndex)");
        AssertDoesNotContain(videoSubmissionText, "public void SendCudaVideoFrame(AVFrame* decodedFrame)");
        AssertContains(hardwareSubmissionText, "public void SendGpuVideoFrame(IntPtr d3d11Texture, int subresourceIndex)");
        AssertContains(hardwareSubmissionText, "public void SendCudaVideoFrame(AVFrame* decodedFrame)");
        AssertContains(hardwareSubmissionText, "CopySubresourceRegion");
        AssertContains(hardwareSubmissionText, "AttachHdrFrameSideDataToHwFrame(options)");
        AssertDoesNotContain(rootText, "public void SendVideoFrame(ReadOnlySpan<byte> frameData, int width, int height)");
        AssertDoesNotContain(rootText, "public void SendGpuVideoFrame(IntPtr d3d11Texture, int subresourceIndex)");
        AssertDoesNotContain(rootText, "public void SendCudaVideoFrame(AVFrame* decodedFrame)");

        return Task.CompletedTask;
    }

    private static Task LibAvEncoder_InitializationLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var initializationText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.Initialization.cs")
            .Replace("\r\n", "\n");

        AssertContains(initializationText, "public static void InitializeFFmpeg(bool requireNativeRuntime = false)");
        AssertContains(initializationText, "public void Initialize(LibAvEncoderOptions options)");
        AssertContains(initializationText, "ThrowIfError(ffmpeg.avcodec_open2(_videoCodecCtx, codec, null), \"avcodec_open2\");");
        AssertContains(initializationText, "ApplyMp4MuxerOptions(options.ContainerFormat, options.FragmentedMp4, &muxerOptions, \"open\");");
        AssertContains(initializationText, "CleanupResources(writeTrailer: false);");
        AssertDoesNotContain(rootText, "public static void InitializeFFmpeg(bool requireNativeRuntime = false)");
        AssertDoesNotContain(rootText, "public void Initialize(LibAvEncoderOptions options)");

        return Task.CompletedTask;
    }

    private static Task LibAvEncoder_SetupAndModelsLiveInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var audioText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.Audio.cs")
            .Replace("\r\n", "\n");
        var audioQueueText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.AudioQueue.cs")
            .Replace("\r\n", "\n");
        var audioInitializationText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.AudioInitialization.cs")
            .Replace("\r\n", "\n");
        var audioSubmissionText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.AudioSubmission.cs")
            .Replace("\r\n", "\n");
        var audioSetupText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.AudioSetup.cs")
            .Replace("\r\n", "\n");
        var hdrSideDataText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.HdrSideData.cs")
            .Replace("\r\n", "\n");
        var modelsText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.Models.cs")
            .Replace("\r\n", "\n");
        var videoSetupText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.VideoSetup.cs")
            .Replace("\r\n", "\n");
        var hardwareFramesText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.HardwareFrames.cs")
            .Replace("\r\n", "\n");
        var cudaHardwareFramesText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.HardwareFrames.Cuda.cs")
            .Replace("\r\n", "\n");

        AssertContains(audioSubmissionText, "public void SendAudioSamples(ReadOnlySpan<byte> f32leSamples)");
        AssertContains(audioSubmissionText, "public void SendMicrophoneSamples(ReadOnlySpan<byte> f32leSamples)");
        AssertContains(audioText, "private unsafe struct AudioStreamState");
        AssertContains(audioText, "private void DrainStreamEncoderPackets(ref AudioStreamState s)");
        AssertContains(audioText, "private void WriteStreamPacket(ref AudioStreamState s, AVPacket* packet)");
        AssertContains(audioText, "private void FlushPendingStreamSamples(ref AudioStreamState s, string streamLabel,");
        AssertContains(audioText, "private void CopyToAccumulator(ref AudioStreamState s, ReadOnlySpan<byte> source, int destinationOffset)");
        AssertContains(audioQueueText, "private void EncodeStreamChunk(ref AudioStreamState s, byte* inputPtr, int inputSamples,");
        AssertContains(audioQueueText, "private void DrainBufferedFrames(ref AudioStreamState s, bool flushPartialFrame)");
        AssertContains(audioQueueText, "private void SendPreparedStreamFrame(ref AudioStreamState s, int sampleCount)");
        AssertContains(audioQueueText, "private void CopyQueuedSamplesToStreamFrame(ref AudioStreamState s, int sampleCount)");
        AssertContains(audioQueueText, "private void RemoveQueuedStreamSamples(ref AudioStreamState s, int sampleCount)");
        AssertContains(audioInitializationText, "private void InitializeAudioIfNeeded(LibAvEncoderOptions options)");
        AssertContains(audioInitializationText, "private void InitializeMicrophoneIfNeeded(LibAvEncoderOptions options)");
        AssertContains(audioInitializationText, "ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_AAC)");
        AssertContains(audioSetupText, "private void ConfigureAudioCodecContext(AVCodecContext* codecContext, LibAvEncoderOptions options, AVCodec* codec)");
        AssertContains(audioSetupText, "private void InitializeAudioResampler(LibAvEncoderOptions options)");
        AssertContains(audioSetupText, "private void AllocateAudioFrame()");
        AssertContains(audioSetupText, "private void AllocateAudioAccumulator(LibAvEncoderOptions options)");
        AssertContains(audioSetupText, "private void AllocateAudioSampleQueue(LibAvEncoderOptions options)");
        AssertContains(hdrSideDataText, "private bool AttachHdrFrameSideDataIfNeeded(LibAvEncoderOptions options)");
        AssertContains(hdrSideDataText, "private bool AttachHdrFrameSideDataToHwFrame(LibAvEncoderOptions options)");
        AssertContains(hdrSideDataText, "ffmpeg.av_mastering_display_metadata_create_side_data(_videoFrame)");
        AssertContains(hdrSideDataText, "ffmpeg.av_mastering_display_metadata_create_side_data(_hwFrame)");
        AssertContains(modelsText, "internal sealed record LibAvEncoderOptions");
        AssertContains(modelsText, "internal readonly record struct RotateOutputResult");
        AssertContains(videoSetupText, "private void ConfigureVideoCodecContext(AVCodecContext* codecContext, LibAvEncoderOptions options)");
        AssertContains(videoSetupText, "private void ApplyEncoderPrivateOptions(AVCodecContext* codecContext, LibAvEncoderOptions options)");
        AssertContains(videoSetupText, "private void InitializeVideoBitstreamFilterIfNeeded(LibAvEncoderOptions options)");
        AssertContains(hardwareFramesText, "private static IntPtr CreateSingleTexture2D(IntPtr d3d11Device, int width, int height, bool isP010, uint bindFlags)");
        AssertContains(hardwareFramesText, "private void InitializeHardwareFramesIfNeeded(LibAvEncoderOptions options)");
        AssertContains(hardwareFramesText, "framesCtx->initial_pool_size = 0;");
        AssertContains(cudaHardwareFramesText, "private void InitializeCudaHardwareFrames(LibAvEncoderOptions options)");
        AssertContains(cudaHardwareFramesText, "_useCudaHardwareFrames = true;");
        AssertContains(cudaHardwareFramesText, "AVPixelFormat.AV_PIX_FMT_CUDA");
        AssertDoesNotContain(audioText, "public void SendAudioSamples(ReadOnlySpan<byte> f32leSamples)");
        AssertDoesNotContain(audioText, "public void SendMicrophoneSamples(ReadOnlySpan<byte> f32leSamples)");
        AssertDoesNotContain(audioText, "private void InitializeAudioIfNeeded(LibAvEncoderOptions options)");
        AssertDoesNotContain(audioText, "private void InitializeMicrophoneIfNeeded(LibAvEncoderOptions options)");
        AssertDoesNotContain(audioText, "private void EncodeStreamChunk(ref AudioStreamState s, byte* inputPtr, int inputSamples,");
        AssertDoesNotContain(audioSubmissionText, "private void EncodeStreamChunk(ref AudioStreamState s, byte* inputPtr, int inputSamples,");
        AssertDoesNotContain(audioQueueText, "private void FlushPendingStreamSamples(ref AudioStreamState s, string streamLabel,");
        AssertDoesNotContain(audioQueueText, "private void CopyToAccumulator(ref AudioStreamState s, ReadOnlySpan<byte> source, int destinationOffset)");
        AssertDoesNotContain(rootText, "private void ConfigureAudioCodecContext(AVCodecContext* codecContext, LibAvEncoderOptions options, AVCodec* codec)");
        AssertDoesNotContain(rootText, "private void InitializeAudioResampler(LibAvEncoderOptions options)");
        AssertDoesNotContain(rootText, "private void AllocateAudioFrame()");
        AssertDoesNotContain(rootText, "private bool AttachHdrFrameSideDataIfNeeded(LibAvEncoderOptions options)");
        AssertDoesNotContain(rootText, "private bool AttachHdrFrameSideDataToHwFrame(LibAvEncoderOptions options)");
        AssertDoesNotContain(rootText, "internal sealed record LibAvEncoderOptions");
        AssertDoesNotContain(rootText, "internal readonly record struct RotateOutputResult");
        AssertDoesNotContain(videoSetupText, "private static IntPtr CreateSingleTexture2D(IntPtr d3d11Device, int width, int height, bool isP010, uint bindFlags)");
        AssertDoesNotContain(videoSetupText, "private void InitializeHardwareFramesIfNeeded(LibAvEncoderOptions options)");
        AssertDoesNotContain(videoSetupText, "private void InitializeCudaHardwareFrames(LibAvEncoderOptions options)");
        AssertDoesNotContain(hardwareFramesText, "_useCudaHardwareFrames = true;");
        AssertDoesNotContain(hardwareFramesText, "private void InitializeVideoBitstreamFilterIfNeeded(LibAvEncoderOptions options)");

        return Task.CompletedTask;
    }

    private static Task LibAvEncoder_OutputLifecycleLivesInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var rotationText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.OutputRotation.cs")
            .Replace("\r\n", "\n");
        var muxerOptionsText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.MuxerOptions.cs")
            .Replace("\r\n", "\n");
        var nativeResourceReleaseText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.NativeResourceRelease.cs")
            .Replace("\r\n", "\n");
        var resourceCleanupText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.ResourceCleanup.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.OutputLifecycle.cs")),
            "LibAvEncoder.OutputLifecycle.cs has been replaced by focused output partials");
        AssertContains(rotationText, "public RotateOutputResult RotateOutput(string newPath)");
        AssertContains(rotationText, "private void CloseCurrentOutputIo()");
        AssertContains(rotationText, "private void ReinitializeOutputContext(string outputPath)");
        AssertContains(rotationText, "private void ReinitializeVideoStream()");
        AssertContains(rotationText, "private void ResetSegmentRuntimeState()");
        AssertContains(muxerOptionsText, "private static unsafe void ApplyMp4MuxerOptions(");
        AssertContains(muxerOptionsText, "frag_keyframe+empty_moov");
        AssertContains(resourceCleanupText, "public void FlushAndClose()");
        AssertContains(resourceCleanupText, "public void Dispose()");
        AssertContains(resourceCleanupText, "private void CleanupResources(bool writeTrailer)");
        AssertContains(resourceCleanupText, "var finalMicSamplesReceived = ReleaseNativeResources(useCudaHardwareFrames);");
        AssertContains(resourceCleanupText, "ffmpeg.av_write_trailer(_formatCtx)");
        AssertContains(nativeResourceReleaseText, "private long ReleaseNativeResources(bool useCudaHardwareFrames)");
        AssertContains(nativeResourceReleaseText, "ffmpeg.avio_closep(&_formatCtx->pb)");
        AssertContains(nativeResourceReleaseText, "Marshal.Release(_hwPoolTextures[i]);");
        AssertContains(nativeResourceReleaseText, "ffmpeg.avcodec_free_context(&videoCodecCtx)");
        AssertContains(nativeResourceReleaseText, "_isOpen = false;");
        AssertDoesNotContain(rootText, "public RotateOutputResult RotateOutput(string newPath)");
        AssertDoesNotContain(rootText, "public void FlushAndClose()");
        AssertDoesNotContain(rootText, "public void Dispose()");
        AssertDoesNotContain(rotationText, "private void CleanupResources(bool writeTrailer)");
        AssertDoesNotContain(resourceCleanupText, "Marshal.Release(_hwPoolTextures[i]);");
        AssertDoesNotContain(resourceCleanupText, "ffmpeg.avcodec_free_context(&videoCodecCtx)");
        AssertDoesNotContain(resourceCleanupText, "private static unsafe void ApplyMp4MuxerOptions(");

        return Task.CompletedTask;
    }

    private static Task LibAvEncoder_FragmentedMp4UsesShortFragmentsForPlayback()
    {
        var sourceText = ReadLibAvEncoderSource();

        AssertContains(sourceText, "private static unsafe void ApplyMp4MuxerOptions(");
        AssertContains(sourceText, "ApplyMp4MuxerOptions(options.ContainerFormat, options.FragmentedMp4, &muxerOptions, \"open\");");
        AssertContains(sourceText, "ApplyMp4MuxerOptions(containerFormat, _options?.FragmentedMp4 ?? false, &muxerOptions, \"rotate\");");
        AssertContains(sourceText, "frag_keyframe+empty_moov");
        AssertContains(sourceText, "ffmpeg.av_dict_set(muxerOptions, \"frag_duration\", \"100000\", 0)");
        AssertContains(sourceText, "ffmpeg.av_dict_set(muxerOptions, \"flush_packets\", \"1\", 0)");
        AssertDoesNotContain(sourceText, "var movflags = options.FragmentedMp4\n                        ? \"frag_keyframe+empty_moov\"");
        AssertDoesNotContain(sourceText, "var movflags = (_options?.FragmentedMp4 ?? false)\n                    ? \"frag_keyframe+empty_moov\"");

        return Task.CompletedTask;
    }
}
