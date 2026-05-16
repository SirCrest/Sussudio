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

        AssertContains(videoSubmissionText, "public void SendVideoFrame(ReadOnlySpan<byte> frameData, int width, int height)");
        AssertContains(videoSubmissionText, "public void SendGpuVideoFrame(IntPtr d3d11Texture, int subresourceIndex)");
        AssertContains(videoSubmissionText, "public void SendCudaVideoFrame(AVFrame* decodedFrame)");
        AssertContains(videoSubmissionText, "CopySubresourceRegion");
        AssertContains(videoSubmissionText, "AttachHdrFrameSideDataToHwFrame(options)");
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

        AssertContains(audioSubmissionText, "public void SendAudioSamples(ReadOnlySpan<byte> f32leSamples)");
        AssertContains(audioSubmissionText, "public void SendMicrophoneSamples(ReadOnlySpan<byte> f32leSamples)");
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
        AssertDoesNotContain(audioText, "public void SendAudioSamples(ReadOnlySpan<byte> f32leSamples)");
        AssertDoesNotContain(audioText, "public void SendMicrophoneSamples(ReadOnlySpan<byte> f32leSamples)");
        AssertDoesNotContain(audioText, "private void InitializeAudioIfNeeded(LibAvEncoderOptions options)");
        AssertDoesNotContain(audioText, "private void InitializeMicrophoneIfNeeded(LibAvEncoderOptions options)");
        AssertDoesNotContain(rootText, "private void ConfigureAudioCodecContext(AVCodecContext* codecContext, LibAvEncoderOptions options, AVCodec* codec)");
        AssertDoesNotContain(rootText, "private void InitializeAudioResampler(LibAvEncoderOptions options)");
        AssertDoesNotContain(rootText, "private void AllocateAudioFrame()");
        AssertDoesNotContain(rootText, "private bool AttachHdrFrameSideDataIfNeeded(LibAvEncoderOptions options)");
        AssertDoesNotContain(rootText, "private bool AttachHdrFrameSideDataToHwFrame(LibAvEncoderOptions options)");
        AssertDoesNotContain(rootText, "internal sealed record LibAvEncoderOptions");
        AssertDoesNotContain(rootText, "internal readonly record struct RotateOutputResult");

        return Task.CompletedTask;
    }
}
