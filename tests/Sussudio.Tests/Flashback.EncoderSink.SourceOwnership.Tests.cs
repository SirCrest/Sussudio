using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackEncoderSink_StartupLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.Startup.cs")
            .Replace("\r\n", "\n");

        AssertContains(startupText, "public Task StartAsync(FlashbackSessionContext context, CancellationToken cancellationToken = default, TimeSpan ptsBaseOffset = default)");
        AssertContains(startupText, "ValidateSessionContext(context);");
        AssertContains(startupText, "var tsPath = _bufferManager.AcquireSegmentPath(out var startupGeneratedSegment);");
        AssertContains(startupText, "_encodingTask = Task.Factory.StartNew(");
        AssertContains(startupText, "FLASHBACK_SINK_START_FAIL");
        AssertContains(startupText, "_bufferManager.AbandonGeneratedSegmentPath(startupGeneratedSegmentPath, restoreActivePath: null);");
        AssertDoesNotContain(rootText, "public Task StartAsync(FlashbackSessionContext context, CancellationToken cancellationToken = default, TimeSpan ptsBaseOffset = default)");
        AssertDoesNotContain(rootText, "FLASHBACK_SINK_START_FAIL");

        return Task.CompletedTask;
    }

    private static Task FlashbackEncoderSink_PacketDrainLivesInFocusedPartial()
    {
        var loopText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.EncodingLoop.cs").Replace("\r\n", "\n");
        var packetDrainText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackEncoderSink.PacketDrain.cs").Replace("\r\n", "\n");

        AssertContains(loopText, "private void EncodingLoop(CancellationToken cancellationToken)");
        AssertContains(loopText, "DrainAudioPackets(audioQueue.Reader)");
        AssertContains(loopText, "DrainVideoPackets(videoQueue.Reader, VideoDrainBatchLimit)");
        AssertContains(loopText, "var finalPts = ResolveEncoderPts();");
        AssertDoesNotContain(loopText, "private bool DrainVideoPackets(");
        AssertDoesNotContain(loopText, "private bool DrainGpuPackets(");
        AssertDoesNotContain(loopText, "private TimeSpan ResolveEncoderPts()");
        AssertDoesNotContain(loopText, "private void OnVideoFrameEncoded()");

        AssertContains(packetDrainText, "private bool DrainVideoPackets(ChannelReader<VideoFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(packetDrainText, "private bool DrainGpuPackets(ChannelReader<GpuFramePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(packetDrainText, "private bool DrainAudioPackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(packetDrainText, "private bool DrainMicrophonePackets(ChannelReader<AudioSamplePacket> reader, int maxPackets = int.MaxValue)");
        AssertContains(packetDrainText, "private void OnVideoFrameEncoded()");
        AssertContains(packetDrainText, "private TimeSpan ResolveEncoderPts()");
        AssertContains(packetDrainText, "MfSourceReaderVideoCapture.GetFrameSizeBytes");
        AssertContains(packetDrainText, "FrameEncoded?.Invoke(this, encoded);");

        return Task.CompletedTask;
    }
}
