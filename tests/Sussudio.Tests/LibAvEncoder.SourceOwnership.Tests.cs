using System.Threading.Tasks;

static partial class Program
{
    internal static Task LibAvEncoder_PacketWritingLivesWithVideoSubmission()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var videoSubmissionText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.VideoSubmission.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.PacketWriting.cs")),
            "video packet drain/write helpers stay folded into video submission");
        AssertContains(videoSubmissionText, "private void DrainEncoderPackets()");
        AssertContains(videoSubmissionText, "private void WriteFilteredPackets()");
        AssertContains(videoSubmissionText, "private void DrainBsfPackets()");
        AssertContains(videoSubmissionText, "private void WritePacket(AVPacket* packet, bool useBsfTimeBase)");
        AssertDoesNotContain(rootText, "private void DrainEncoderPackets()");
        AssertDoesNotContain(rootText, "private void WriteFilteredPackets()");
        AssertDoesNotContain(rootText, "private void DrainBsfPackets()");
        AssertDoesNotContain(rootText, "private void WritePacket(AVPacket* packet, bool useBsfTimeBase)");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_FrameCopyLivesWithVideoSubmission()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var videoSubmissionText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.VideoSubmission.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.FrameCopy.cs")),
            "CPU packed-frame copy is part of video submission, not a standalone partial");
        AssertContains(videoSubmissionText, "private void CopyPackedFrameToVideoFrame(ReadOnlySpan<byte> frameData, LibAvEncoderOptions options)");
        AssertContains(videoSubmissionText, "private static void CopyPlane(byte* sourceStart, byte* destinationStart, int destinationStride, int rowBytes, int rowCount)");
        AssertContains(videoSubmissionText, "Buffer.MemoryCopy(");
        AssertDoesNotContain(rootText, "private void CopyPackedFrameToVideoFrame(ReadOnlySpan<byte> frameData, LibAvEncoderOptions options)");
        AssertDoesNotContain(rootText, "private static void CopyPlane(byte* sourceStart, byte* destinationStart, int destinationStride, int rowBytes, int rowCount)");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_VideoSubmissionLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var videoSubmissionText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.VideoSubmission.cs")
            .Replace("\r\n", "\n");
        var hardwareFramesText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.HardwareFrames.cs")
            .Replace("\r\n", "\n");

        AssertContains(videoSubmissionText, "public void SendVideoFrame(ReadOnlySpan<byte> frameData, int width, int height)");
        AssertContains(videoSubmissionText, "CopyPackedFrameToVideoFrame(frameData[..expectedSize], options);");
        AssertContains(videoSubmissionText, "private void CopyPackedFrameToVideoFrame(ReadOnlySpan<byte> frameData, LibAvEncoderOptions options)");
        AssertContains(videoSubmissionText, "private bool AttachHdrFrameSideDataIfNeeded(LibAvEncoderOptions options)");
        AssertContains(videoSubmissionText, "private bool AttachHdrFrameSideDataToHwFrame(LibAvEncoderOptions options)");
        AssertDoesNotContain(videoSubmissionText, "public void SendGpuVideoFrame(IntPtr d3d11Texture, int subresourceIndex)");
        AssertDoesNotContain(videoSubmissionText, "public void SendCudaVideoFrame(AVFrame* decodedFrame)");
        AssertContains(hardwareFramesText, "public void SendGpuVideoFrame(IntPtr d3d11Texture, int subresourceIndex)");
        AssertContains(hardwareFramesText, "public void SendCudaVideoFrame(AVFrame* decodedFrame)");
        AssertContains(hardwareFramesText, "CopySubresourceRegion");
        AssertContains(hardwareFramesText, "AttachHdrFrameSideDataToHwFrame(options)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.HardwareSubmission.cs")),
            "hardware frame submission lives with LibAvEncoder.HardwareFrames.cs");
        AssertDoesNotContain(rootText, "public void SendVideoFrame(ReadOnlySpan<byte> frameData, int width, int height)");
        AssertDoesNotContain(rootText, "public void SendGpuVideoFrame(IntPtr d3d11Texture, int subresourceIndex)");
        AssertDoesNotContain(rootText, "public void SendCudaVideoFrame(AVFrame* decodedFrame)");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_InitializationLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var initializationText = rootText;

        AssertContains(initializationText, "public static void InitializeFFmpeg(bool requireNativeRuntime = false)");
        AssertContains(initializationText, "public void Initialize(LibAvEncoderOptions options)");
        AssertContains(initializationText, "ThrowIfError(ffmpeg.avcodec_open2(_videoCodecCtx, codec, null), \"avcodec_open2\");");
        AssertContains(initializationText, "ApplyMp4MuxerOptions(options.ContainerFormat, options.FragmentedMp4, &muxerOptions, \"open\");");
        AssertContains(initializationText, "CleanupResources(writeTrailer: false);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.Initialization.cs")),
            "LibAvEncoder initialization folded into the encoder root");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_SetupAndModelsLiveInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var initializationText = rootText;
        var audioText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.Audio.cs")
            .Replace("\r\n", "\n");
        var videoSubmissionText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.VideoSubmission.cs")
            .Replace("\r\n", "\n");
        var modelsText = rootText;
        var hardwareFramesText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.HardwareFrames.cs")
            .Replace("\r\n", "\n");

        AssertContains(audioText, "public void SendAudioSamples(ReadOnlySpan<byte> f32leSamples)");
        AssertContains(audioText, "public void SendMicrophoneSamples(ReadOnlySpan<byte> f32leSamples)");
        AssertContains(audioText, "private unsafe struct AudioStreamState");
        AssertContains(audioText, "private void DrainStreamEncoderPackets(ref AudioStreamState s)");
        AssertContains(audioText, "private void WriteStreamPacket(ref AudioStreamState s, AVPacket* packet)");
        AssertContains(audioText, "private void FlushPendingStreamSamples(ref AudioStreamState s, string streamLabel,");
        AssertContains(audioText, "private void CopyToAccumulator(ref AudioStreamState s, ReadOnlySpan<byte> source, int destinationOffset)");
        AssertContains(audioText, "private void EncodeStreamChunk(ref AudioStreamState s, byte* inputPtr, int inputSamples,");
        AssertContains(audioText, "private void DrainBufferedFrames(ref AudioStreamState s, bool flushPartialFrame)");
        AssertContains(audioText, "private void SendPreparedStreamFrame(ref AudioStreamState s, int sampleCount)");
        AssertContains(audioText, "private void CopyQueuedSamplesToStreamFrame(ref AudioStreamState s, int sampleCount)");
        AssertContains(audioText, "private void RemoveQueuedStreamSamples(ref AudioStreamState s, int sampleCount)");
        AssertContains(audioText, "private void InitializeAudioIfNeeded(LibAvEncoderOptions options)");
        AssertContains(audioText, "private void InitializeMicrophoneIfNeeded(LibAvEncoderOptions options)");
        AssertContains(audioText, "ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_AAC)");
        AssertContains(audioText, "private void ConfigureAudioCodecContext(AVCodecContext* codecContext, LibAvEncoderOptions options, AVCodec* codec)");
        AssertContains(audioText, "private void InitializeAudioResampler(LibAvEncoderOptions options)");
        AssertContains(audioText, "private void AllocateAudioFrame()");
        AssertContains(audioText, "private void AllocateAudioAccumulator(LibAvEncoderOptions options)");
        AssertContains(audioText, "private void AllocateAudioSampleQueue(LibAvEncoderOptions options)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.AudioSetup.cs")),
            "Audio setup helpers live with audio stream initialization");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.AudioSubmission.cs")),
            "Audio sample submission folded into LibAvEncoder.Audio.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.AudioQueue.cs")),
            "Audio queue and A/V sync helpers folded into LibAvEncoder.Audio.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.AudioInitialization.cs")),
            "Audio stream initialization folded into LibAvEncoder.Audio.cs");
        AssertContains(videoSubmissionText, "private bool AttachHdrFrameSideDataIfNeeded(LibAvEncoderOptions options)");
        AssertContains(videoSubmissionText, "private bool AttachHdrFrameSideDataToHwFrame(LibAvEncoderOptions options)");
        AssertContains(videoSubmissionText, "ffmpeg.av_mastering_display_metadata_create_side_data(_videoFrame)");
        AssertContains(videoSubmissionText, "ffmpeg.av_mastering_display_metadata_create_side_data(_hwFrame)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.HdrSideData.cs")),
            "HDR side-data helpers live with LibAvEncoder.VideoSubmission.cs");
        AssertContains(modelsText, "internal sealed record LibAvEncoderOptions");
        AssertContains(modelsText, "internal readonly record struct RotateOutputResult");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.Models.cs")),
            "LibAvEncoder option/result models live with the encoder root");
        AssertContains(initializationText, "private void ConfigureVideoCodecContext(AVCodecContext* codecContext, LibAvEncoderOptions options)");
        AssertContains(initializationText, "private void ApplyEncoderPrivateOptions(AVCodecContext* codecContext, LibAvEncoderOptions options)");
        AssertContains(initializationText, "private void InitializeVideoBitstreamFilterIfNeeded(LibAvEncoderOptions options)");
        AssertContains(initializationText, "private static string? GetVideoBitstreamFilterSpec(LibAvEncoderOptions options)");
        AssertContains(initializationText, "private static string MapNvencPreset(string? preset)");
        AssertContains(initializationText, "private static bool TryMapSplitEncodeMode(string? splitEncodeMode, out long value)");
        AssertContains(initializationText, "private static AVRational ResolveFrameRate(LibAvEncoderOptions options)");
        AssertContains(initializationText, "private static bool IsSampleFormatSupported(AVCodec* codec, AVSampleFormat sampleFormat)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.VideoSetup.cs")),
            "Video codec setup helpers live with encoder initialization");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.CodecPolicy.cs")),
            "LibAvEncoder codec/filter/rational policy lives with encoder initialization");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.Initialization.cs")),
            "LibAvEncoder initialization lives with the encoder root");
        AssertContains(hardwareFramesText, "private static IntPtr CreateSingleTexture2D(IntPtr d3d11Device, int width, int height, bool isP010, uint bindFlags)");
        AssertContains(hardwareFramesText, "private void InitializeHardwareFramesIfNeeded(LibAvEncoderOptions options)");
        AssertContains(hardwareFramesText, "framesCtx->initial_pool_size = 0;");
        AssertContains(hardwareFramesText, "private void InitializeCudaHardwareFrames(LibAvEncoderOptions options)");
        AssertContains(hardwareFramesText, "_useCudaHardwareFrames = true;");
        AssertContains(hardwareFramesText, "AVPixelFormat.AV_PIX_FMT_CUDA");
        AssertContains(hardwareFramesText, "public void SendGpuVideoFrame(IntPtr d3d11Texture, int subresourceIndex)");
        AssertContains(hardwareFramesText, "public void SendCudaVideoFrame(AVFrame* decodedFrame)");
        AssertContains(hardwareFramesText, "CopySubresourceRegion");
        AssertContains(hardwareFramesText, "AttachHdrFrameSideDataToHwFrame(options)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.HardwareFrames.Cuda.cs")),
            "CUDA hardware frame adoption lives with the hardware frame initializer");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.HardwareSubmission.cs")),
            "hardware frame submission lives with LibAvEncoder.HardwareFrames.cs");
        AssertContains(initializationText, "private static void ValidateOptions(LibAvEncoderOptions options)");
        AssertContains(initializationText, "private static void ValidateRequiredVideoOptions(LibAvEncoderOptions options)");
        AssertContains(initializationText, "private static void ValidateAudioOptions(LibAvEncoderOptions options)");
        AssertContains(initializationText, "private static void ValidateHdrOptions(LibAvEncoderOptions options)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.OptionsValidation.cs")),
            "LibAvEncoder option validation folded into encoder initialization");
        AssertDoesNotContain(rootText, "private void ConfigureAudioCodecContext(AVCodecContext* codecContext, LibAvEncoderOptions options, AVCodec* codec)");
        AssertDoesNotContain(rootText, "private void InitializeAudioResampler(LibAvEncoderOptions options)");
        AssertDoesNotContain(rootText, "private void AllocateAudioFrame()");
        AssertDoesNotContain(rootText, "private bool AttachHdrFrameSideDataIfNeeded(LibAvEncoderOptions options)");
        AssertDoesNotContain(rootText, "private bool AttachHdrFrameSideDataToHwFrame(LibAvEncoderOptions options)");
        AssertDoesNotContain(initializationText, "private static IntPtr CreateSingleTexture2D(IntPtr d3d11Device, int width, int height, bool isP010, uint bindFlags)");
        AssertDoesNotContain(initializationText, "private void InitializeHardwareFramesIfNeeded(LibAvEncoderOptions options)");
        AssertDoesNotContain(initializationText, "private void InitializeCudaHardwareFrames(LibAvEncoderOptions options)");
        AssertDoesNotContain(hardwareFramesText, "private void InitializeVideoBitstreamFilterIfNeeded(LibAvEncoderOptions options)");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_OutputLifecycleLivesInFocusedOwner()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var outputLifecycleText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.OutputLifecycle.cs")
            .Replace("\r\n", "\n");

        AssertContains(outputLifecycleText, "public RotateOutputResult RotateOutput(string newPath)");
        AssertContains(outputLifecycleText, "private void CloseCurrentOutputIo()");
        AssertContains(outputLifecycleText, "private void ReinitializeOutputContext(string outputPath)");
        AssertContains(outputLifecycleText, "private void ReinitializeVideoStream()");
        AssertContains(outputLifecycleText, "private void ResetSegmentRuntimeState()");
        AssertContains(outputLifecycleText, "private static unsafe void ApplyMp4MuxerOptions(");
        AssertContains(outputLifecycleText, "frag_keyframe+empty_moov");
        AssertContains(outputLifecycleText, "public void FlushAndClose()");
        AssertContains(outputLifecycleText, "public void Dispose()");
        AssertContains(outputLifecycleText, "private void CleanupResources(bool writeTrailer)");
        AssertContains(outputLifecycleText, "var finalMicSamplesReceived = ReleaseNativeResources(useCudaHardwareFrames);");
        AssertContains(outputLifecycleText, "ffmpeg.av_write_trailer(_formatCtx)");
        AssertContains(outputLifecycleText, "private long ReleaseNativeResources(bool useCudaHardwareFrames)");
        AssertContains(outputLifecycleText, "ffmpeg.avio_closep(&_formatCtx->pb)");
        AssertContains(outputLifecycleText, "Marshal.Release(_hwPoolTextures[i]);");
        AssertContains(outputLifecycleText, "ffmpeg.avcodec_free_context(&videoCodecCtx)");
        AssertContains(outputLifecycleText, "_isOpen = false;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.OutputRotation.cs")),
            "output rotation folded into LibAvEncoder.OutputLifecycle.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.ResourceCleanup.cs")),
            "resource cleanup folded into LibAvEncoder.OutputLifecycle.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.NativeResourceRelease.cs")),
            "Native resource release folded into LibAvEncoder.OutputLifecycle.cs");
        AssertDoesNotContain(rootText, "public RotateOutputResult RotateOutput(string newPath)");
        AssertDoesNotContain(rootText, "public void FlushAndClose()");
        AssertDoesNotContain(rootText, "public void Dispose()");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_FragmentedMp4UsesShortFragmentsForPlayback()
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
