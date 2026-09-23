using System.Buffers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using FFmpeg.AutoGen;
using Xunit;

namespace Sussudio.Tests;

internal static class LibAvRecordingDrainBehaviorTests
{
    internal static async Task VerifyAudioInterleavingAsync()
    {
        const int totalFrames = 60;
        const int samplesPerPacket = 12_000;
        const int width = 64;
        const int height = 64;
        var assembly = SussudioAssembly.Load();
        Type TypeOf(string name) => assembly.GetType(name, throwOnError: true)!;
        var encoderType = TypeOf("Sussudio.Services.Recording.LibAvEncoder");
        encoderType.GetMethod("InitializeFFmpeg")!.Invoke(null, new object[] { true });
        var sinkType = TypeOf("Sussudio.Services.Recording.LibAvRecordingSink");
        var sink = Activator.CreateInstance(sinkType)!;
        var directory = Directory.CreateTempSubdirectory("sussudio-drain-");
        using var cancellation = new CancellationTokenSource();
        Task? owner = null;
        try
        {
            var outputPath = Path.Combine(directory.FullName, "recording.mp4");
            var options = Activator.CreateInstance(TypeOf("Sussudio.Services.Recording.LibAvEncoderOptions"))!;
            Set(options, "OutputPath", outputPath);
            Set(options, "CodecName", "libx264");
            Set(options, "Width", width);
            Set(options, "Height", height);
            Set(options, "FrameRate", 120d);
            Set(options, "BitRate", 1_000_000u);
            Set(options, "AudioEnabled", true);
            Set(options, "MicrophoneEnabled", true);
            var encoder = GetField(sink, "_encoder")!;
            encoderType.GetMethod("Initialize")!.Invoke(encoder, new[] { options });

            var settings = Activator.CreateInstance(TypeOf("Sussudio.Models.CaptureSettings"))!;
            Set(settings, "Format", Enum.Parse(TypeOf("Sussudio.Models.RecordingFormat"), "H264Mp4"));
            Set(settings, "AudioEnabled", true);
            Set(settings, "MicrophoneEnabled", true);
            var context = Activator.CreateInstance(TypeOf("Sussudio.Services.Contracts.RecordingContext"))!;
            Set(context, "Settings", settings);
            Set(context, "FinalOutputPath", outputPath);
            Set(context, "VideoOutputPath", outputPath);
            Set(context, "EffectiveWidth", (uint)width);
            Set(context, "EffectiveHeight", (uint)height);
            Set(context, "EffectiveFrameRate", 120d);
            SetField(sink, "_context", context);
            SetField(sink, "_width", width);
            SetField(sink, "_height", height);
            SetField(sink, "_audioEnabled", true);
            SetField(sink, "_microphoneEnabled", true);
            SetField(sink, "_started", true);
            SetField(sink, "_cts", cancellation);
            var video = CreateQueue(sink, "_videoQueue");
            var audio = CreateQueue(sink, "_audioQueue");
            var microphone = CreateQueue(sink, "_microphoneQueue");
            var videoPacketType = sinkType.GetNestedType("VideoFramePacket", BindingFlags.NonPublic)!;
            for (var frame = 0; frame < totalFrames; frame++)
            {
                var length = width * height * 3 / 2;
                var buffer = ArrayPool<byte>.Shared.Rent(length);
                buffer.AsSpan(0, width * height).Fill(16);
                buffer.AsSpan(width * height, width * height / 2).Fill(128);
                Write(video, Activator.CreateInstance(videoPacketType,
                    new object?[] { buffer, null, length, Environment.TickCount64, null })!);
            }
            SetField(sink, "_videoQueueDepth", totalFrames);
            Complete(video);
            EnqueueAudio(audio, "_audioQueueDepth");
            EnqueueAudio(microphone, "_microphoneQueueDepth");

            var observations = new List<(long Frame, long Audio, long Microphone, int RemainingVideo)>();
            Exception? callbackFailure = null;
            EventHandler<long> frameEncoded = (_, frame) =>
            {
                try
                {
                    observations.Add((frame, Read<long>(sink, "AudioSamplesReceived"),
                        Read<long>(sink, "MicrophoneSamplesReceived"), (int)GetField(sink, "_videoQueueDepth")!));
                    if (frame == 1)
                    {
                        // Simulate both sources delivering another packet during a video
                        // batch. The owner must revisit them before exhausting video work.
                        EnqueueAudio(audio, "_audioQueueDepth");
                        EnqueueAudio(microphone, "_microphoneQueueDepth");
                        Complete(audio);
                        Complete(microphone);
                    }
                }
                catch (Exception error)
                {
                    callbackFailure = error;
                    cancellation.Cancel();
                }
            };
            sinkType.GetEvent("FrameEncoded")!.AddEventHandler(sink, frameEncoded);
            var encode = sinkType.GetMethod("EncodingLoop", PrivateInstance)!
                .CreateDelegate<Action<CancellationToken>>(sink);
            owner = Task.Factory.StartNew(() => encode(cancellation.Token), CancellationToken.None,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
            SetField(sink, "_encodingTask", owner);
            await owner.WaitAsync(TimeSpan.FromSeconds(15));

            Assert.Null(callbackFailure);
            Assert.Null(GetField(sink, "_encodingFailure"));
            Assert.Equal(totalFrames, observations.Count);
            Assert.Equal(samplesPerPacket, observations[0].Audio);
            Assert.Equal(samplesPerPacket, observations[0].Microphone);
            var revisited = observations.First(observation =>
                observation.Audio > samplesPerPacket && observation.Microphone > samplesPerPacket);
            Assert.InRange(revisited.Frame, 2, totalFrames - 1);
            Assert.True(revisited.RemainingVideo > 0, "Audio must advance while video work remains queued.");
            Assert.Equal(samplesPerPacket * 2L, Read<long>(sink, "AudioSamplesReceived"));
            Assert.Equal(samplesPerPacket * 2L, Read<long>(sink, "MicrophoneSamplesReceived"));
            Assert.True((bool)GetField(sink, "_structureVerificationCompleted")!);
            Assert.Equal(new[] { "video", "device_audio", "microphone" },
                (IReadOnlyList<string>)GetField(sink, "_verifiedRequestedTracks")!);
            Assert.Equal(new[] { "video", "device_audio", "microphone" },
                (IReadOnlyList<string>)GetField(sink, "_verifiedObservedTracks")!);
            foreach (var field in new[] { "_videoQueueDepth", "_audioQueueDepth", "_microphoneQueueDepth" })
                Assert.Equal(0, (int)GetField(sink, field)!);
            using var reopened = File.Open(outputPath, FileMode.Open, FileAccess.Read, FileShare.None);
            Assert.True(reopened.Length > 0);

            void EnqueueAudio(object queue, string depthField)
            {
                var length = samplesPerPacket * 2 * sizeof(float);
                var buffer = ArrayPool<byte>.Shared.Rent(length);
                buffer.AsSpan(0, length).Clear();
                var packetType = sinkType.GetNestedType("AudioSamplePacket", BindingFlags.NonPublic)!;
                Write(queue, Activator.CreateInstance(packetType, buffer, length)!);
                SetField(sink, depthField, (int)GetField(sink, depthField)! + 1);
            }
        }
        finally
        {
            if (owner == null || !owner.IsCompleted) cancellation.Cancel();
            if (owner != null) await owner.WaitAsync(TimeSpan.FromSeconds(15));
            await ((IAsyncDisposable)sink).DisposeAsync();
            directory.Delete(recursive: true);
        }
    }

    // HDR/P010 counterpart to VerifyAudioInterleavingAsync. Drives the same real
    // LibAvEncoder/EncodingLoop path but with LibAvEncoderOptions.IsP010 = true,
    // which flips the codec's pix_fmt from AV_PIX_FMT_NV12 to AV_PIX_FMT_P010LE
    // (LibAvEncoder.cs / LibAvEncoder.VideoFrames.cs). Audio is disabled so the
    // test isolates the P010 video path. Uses "hevc_nvenc" as the codec, matching
    // production's EncoderSupport.MapNvencCodecName(RecordingFormat.HevcMp4) --
    // P010/HDR encoding in this codebase is coupled to NVENC everywhere it is
    // configured (LibAvRecordingSink.CreateOptions), there is no software P010
    // encoder path to fall back to. This requires an NVENC-capable GPU at test
    // time; the xUnit adapter independently probes this exact codec/input format.
    internal static async Task VerifyP010EncodeRoundTripAsync()
    {
        const int totalFrames = 30;
        // NVENC rejects tiny resolutions: at 64x64 (the size the SDR libx264 test uses)
        // avcodec_open2 fails with -22 EINVAL. 256x256 clears the HEVC minimum while
        // staying small enough to encode 30 frames quickly.
        const int width = 256;
        const int height = 256;
        var assembly = SussudioAssembly.Load();
        Type TypeOf(string name) => assembly.GetType(name, throwOnError: true)!;
        var encoderType = TypeOf("Sussudio.Services.Recording.LibAvEncoder");
        encoderType.GetMethod("InitializeFFmpeg")!.Invoke(null, new object[] { true });
        var sinkType = TypeOf("Sussudio.Services.Recording.LibAvRecordingSink");
        var sink = Activator.CreateInstance(sinkType)!;
        var directory = Directory.CreateTempSubdirectory("sussudio-drain-p010-");
        using var cancellation = new CancellationTokenSource();
        Task? owner = null;
        try
        {
            var outputPath = Path.Combine(directory.FullName, "recording.mp4");
            var options = Activator.CreateInstance(TypeOf("Sussudio.Services.Recording.LibAvEncoderOptions"))!;
            Set(options, "OutputPath", outputPath);
            Set(options, "CodecName", "hevc_nvenc");
            Set(options, "Width", width);
            Set(options, "Height", height);
            Set(options, "FrameRate", 30d);
            Set(options, "BitRate", 2_000_000u);
            Set(options, "IsP010", true);
            Set(options, "HdrEnabled", true);
            Set(options, "AudioEnabled", false);
            Set(options, "MicrophoneEnabled", false);
            var encoder = GetField(sink, "_encoder")!;
            encoderType.GetMethod("Initialize")!.Invoke(encoder, new[] { options });

            var settings = Activator.CreateInstance(TypeOf("Sussudio.Models.CaptureSettings"))!;
            Set(settings, "Format", Enum.Parse(TypeOf("Sussudio.Models.RecordingFormat"), "HevcMp4"));
            Set(settings, "AudioEnabled", false);
            Set(settings, "MicrophoneEnabled", false);
            var context = Activator.CreateInstance(TypeOf("Sussudio.Services.Contracts.RecordingContext"))!;
            Set(context, "Settings", settings);
            Set(context, "FinalOutputPath", outputPath);
            Set(context, "VideoOutputPath", outputPath);
            Set(context, "EffectiveWidth", (uint)width);
            Set(context, "EffectiveHeight", (uint)height);
            Set(context, "EffectiveFrameRate", 30d);
            Set(context, "HdrPipelineActive", true);
            Set(context, "VideoInputPixelFormat", "p010le");
            SetField(sink, "_context", context);
            SetField(sink, "_width", width);
            SetField(sink, "_height", height);
            SetField(sink, "_audioEnabled", false);
            SetField(sink, "_microphoneEnabled", false);
            SetField(sink, "_started", true);
            SetField(sink, "_cts", cancellation);
            var video = CreateQueue(sink, "_videoQueue");
            var audio = CreateQueue(sink, "_audioQueue");
            var videoPacketType = sinkType.GetNestedType("VideoFramePacket", BindingFlags.NonPublic)!;

            // P010 is 4:2:0 with 16-bit samples (MSB-justified: the 10 significant
            // bits sit in the top bits of each 16-bit word), so the packed frame is
            // double the byte size of the NV12 equivalent (width*height*3/2):
            // width*height*3 total, per PooledVideoFrame.GetFrameSizeBytes and
            // LibAvEncoder.CopyPackedFrameToVideoFrame's rowBytes = width*2 math.
            var length = width * height * 3;
            for (var frame = 0; frame < totalFrames; frame++)
            {
                var buffer = ArrayPool<byte>.Shared.Rent(length);
                FillP010Fixture(buffer, length, width * height);
                Write(video, Activator.CreateInstance(videoPacketType,
                    new object?[] { buffer, null, length, Environment.TickCount64, null })!);
            }
            SetField(sink, "_videoQueueDepth", totalFrames);
            Complete(video);
            Complete(audio);

            Exception? callbackFailure = null;
            var encodedFrames = 0;
            EventHandler<long> frameEncoded = (_, _) =>
            {
                try
                {
                    encodedFrames++;
                }
                catch (Exception error)
                {
                    callbackFailure = error;
                    cancellation.Cancel();
                }
            };
            sinkType.GetEvent("FrameEncoded")!.AddEventHandler(sink, frameEncoded);
            var encode = sinkType.GetMethod("EncodingLoop", PrivateInstance)!
                .CreateDelegate<Action<CancellationToken>>(sink);
            owner = Task.Factory.StartNew(() => encode(cancellation.Token), CancellationToken.None,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
            SetField(sink, "_encodingTask", owner);
            await owner.WaitAsync(TimeSpan.FromSeconds(15));

            Assert.Null(callbackFailure);
            Assert.Null(GetField(sink, "_encodingFailure"));
            Assert.Equal(totalFrames, encodedFrames);
            // _structureVerificationCompleted only flips true after
            // InProcessRecordingStructureVerifier.Verify succeeds, which (because
            // context.HdrPipelineActive is true) includes reopening the file and
            // asserting the video stream's color_primaries/color_trc/color_space
            // are BT.2020/PQ/non-constant-luminance and its codec is HEVC -- a
            // real, cheap, in-process check that P010/HDR metadata actually landed
            // in the encoded output, with no ffprobe process spawn required.
            Assert.True((bool)GetField(sink, "_structureVerificationCompleted")!);
            foreach (var field in new[] { "_videoQueueDepth", "_audioQueueDepth" })
                Assert.Equal(0, (int)GetField(sink, field)!);
            using var reopened = File.Open(outputPath, FileMode.Open, FileAccess.Read, FileShare.None);
            Assert.True(reopened.Length > 0);
        }
        finally
        {
            if (owner == null || !owner.IsCompleted) cancellation.Cancel();
            if (owner != null) await owner.WaitAsync(TimeSpan.FromSeconds(15));
            await ((IAsyncDisposable)sink).DisposeAsync();
            directory.Delete(recursive: true);
        }
    }

    internal static Task VerifyCaptureServiceP010LifecycleAsync()
        => RecordingNativeTestChild.VerifyLifecycleAsync("p010");

    internal static Task VerifyCaptureServiceP010MismatchRollbackAsync()
        => RecordingNativeTestChild.VerifyLifecycleAsync("mismatch");

    internal static async Task RunCaptureServiceChildAsync(string scenario, Assembly assembly, string directory)
    {
        await using var fixture = new CaptureServiceFixture(assembly, directory, sourceIsP010: scenario == "p010");
        try
        {
            if (scenario == "p010") await VerifyCaptureServiceP010LifecycleCoreAsync(fixture);
            else if (scenario == "mismatch") await VerifyCaptureServiceP010MismatchRollbackCoreAsync(fixture);
            else throw new ArgumentException("Unknown recording lifecycle scenario.", nameof(scenario));
        }
        catch (Exception error)
        {
            // Preserve the assertion if cleanup also fails or the parent must terminate cleanup.
            Console.Error.WriteLine(error);
            throw;
        }
    }

    private static async Task VerifyCaptureServiceP010LifecycleCoreAsync(CaptureServiceFixture fixture)
    {
        const int totalFrames = 30;
        await fixture.InitializeAsync();
        Assert.Equal("Ready", Read<object>(fixture.Service, "SessionState").ToString());
        var frames = Channel.CreateUnbounded<ulong>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        EventHandler<ulong> captured = (_, count) => frames.Writer.TryWrite(count);
        fixture.Service.GetType().GetEvent("FrameCaptured")!.AddEventHandler(fixture.Service, captured);
        await fixture.StartAsync();
        Assert.True(Read<bool>(fixture.Service, "IsRecording"));
        Assert.Equal("Recording", Read<object>(fixture.Service, "SessionState").ToString());
        Assert.True(Read<bool>(fixture.RuntimeSnapshot(), "IsRecording"));
        Assert.True(Read<bool>(fixture.HealthSnapshot(), "IsRecording"));
        var context = Read<object>(GetField(fixture.Service, "_recordingBackend")!, "Context");
        Assert.True(Read<bool>(context, "HdrPipelineActive"));
        Assert.Equal("p010le", Read<string>(context, "VideoInputPixelFormat"));

        var bytes = new byte[CaptureServiceFixture.Width * CaptureServiceFixture.Height * 3];
        FillP010Fixture(bytes, bytes.Length, CaptureServiceFixture.Width * CaptureServiceFixture.Height);
        for (var frame = 0; frame < totalFrames; frame++) fixture.DeliverFrame(bytes);
        using (var encodedDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
        {
            ulong encoded;
            do { encoded = await frames.Reader.ReadAsync(encodedDeadline.Token); }
            while (encoded < totalFrames);
            Assert.Equal((ulong)totalFrames, encoded);
        }
        Assert.Equal(totalFrames, Read<long>(fixture.Source, "RecordingFramesDelivered"));
        Assert.Equal(totalFrames, Read<long>(fixture.Source, "VideoFramesWrittenToSink"));
        Assert.Equal(0, Read<long>(fixture.Source, "RecordingFramesRejected"));
        Assert.Equal(0, Read<long>(fixture.Source, "RecordingQueueRejectedFrames"));

        await fixture.StopAsync();
        var runtime = fixture.RuntimeSnapshot();
        Assert.False(Read<bool>(fixture.Service, "IsRecording"));
        Assert.Equal("Ready", Read<object>(fixture.Service, "SessionState").ToString());
        Assert.True(Read<bool>(runtime, "RecordingFinalizationVerificationCompleted"));
        Assert.False(Read<bool>(runtime, "RecordingFinalizationCleanupPending"));
        Assert.Equal("Saved", Read<string>(runtime, "RecordingFinalizeOutcome"));
        Assert.Equal(new[] { "video" }, Read<IReadOnlyList<string>>(runtime, "RecordingRequestedTracks"));
        Assert.Equal(new[] { "video" }, Read<IReadOnlyList<string>>(runtime, "RecordingObservedTracks"));
        Assert.True(Read<bool>(runtime, "RecordingIntegrityComplete"));
        foreach (var name in new[] { "SourceFrames", "AcceptedFrames", "SubmittedFrames", "EncodedFrames" })
            Assert.Equal(totalFrames, Read<long>(runtime, "RecordingIntegrity" + name));
        foreach (var name in new[] { "PipelineDroppedFrames", "QueueDroppedFrames", "EncoderDroppedFrames", "SequenceGaps" })
            Assert.Equal(0, Read<long>(runtime, "RecordingIntegrity" + name));

        var health = fixture.HealthSnapshot();
        Assert.False(Read<bool>(health, "IsRecording"));
        Assert.Equal("Ready", Read<object>(health, "SessionState").ToString());
        Assert.False(Read<bool>(health, "RecordingEncodingFailed"));
        Assert.Equal(0, Read<int>(health, "FfmpegVideoQueueDepth"));
        Assert.Equal(0, Read<int>(health, "FfmpegAudioQueueDepth"));
        var outputPath = Read<string>(runtime, "LastOutputPath");
        Assert.Equal(Read<string>(context, "FinalOutputPath"), outputPath);
        using (var reopened = File.Open(outputPath, FileMode.Open, FileAccess.Read, FileShare.None))
            Assert.True(reopened.Length > 0);
        var verifier = Activator.CreateInstance(fixture.TypeOf("Sussudio.Services.Recording.InProcessRecordingStructureVerifier"), nonPublic: true)!;
        var verification = verifier.GetType().GetMethod("Verify")!.Invoke(verifier, new object?[] { context, null })!;
        Assert.True(Read<bool>(verification, "Succeeded"), Read<string>(verification, "Detail"));
        AssertTenBitHevcOutput(outputPath);
        Assert.Empty(Directory.EnumerateFiles(fixture.RecoveryDirectory));

        await fixture.StopAsync();
        Assert.Equal(outputPath, Read<string>(fixture.RuntimeSnapshot(), "LastOutputPath"));
        Assert.False(Read<bool>(fixture.Service, "IsRecording"));
        Assert.Equal(0, fixture.ProcessCalls);
    }

    private static async Task VerifyCaptureServiceP010MismatchRollbackCoreAsync(CaptureServiceFixture fixture)
    {
        await fixture.InitializeAsync();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(fixture.StartAsync);
        Assert.Equal("Recording requires P010, but the active source-reader session negotiated NV12.", error.Message);
        Assert.False(Read<bool>(fixture.Service, "IsRecording"));
        Assert.Equal("Faulted", Read<object>(fixture.Service, "SessionState").ToString());
        var backend = GetField(fixture.Service, "_recordingBackend")!;
        Assert.False(Read<bool>(backend, "HasActiveBackend"));
        Assert.Null(Read<object?>(backend, "Context"));
        Assert.Null(Read<object?>(backend, "PendingLibAvDrainTask"));
        Assert.False((bool)GetField(fixture.Source, "_recordingActive")!);
        Assert.Null(GetField(fixture.Source, "_recordingEncoder"));
        Assert.Equal(0, (int)GetField(fixture.Service, "_recordingAudioStartInProgress")!);
        Assert.Empty(Directory.EnumerateFileSystemEntries(fixture.OutputDirectory));
        Assert.Equal(0, fixture.ProcessCalls);

        await fixture.CleanupAsync();
        Assert.Null(Read<object?>(GetField(fixture.Service, "_videoPipeline")!, "Capture"));
        await fixture.InitializeAsync();
        Assert.Equal("Ready", Read<object>(fixture.Service, "SessionState").ToString());
        Assert.False(Read<bool>(fixture.Service, "IsRecording"));
        Assert.Equal(0, fixture.ProcessCalls);
    }

    private static unsafe void AssertTenBitHevcOutput(string outputPath)
    {
        AVFormatContext* input = null;
        try
        {
            Assert.True(ffmpeg.avformat_open_input(&input, outputPath, null, null) >= 0);
            Assert.True(ffmpeg.avformat_find_stream_info(input, null) >= 0);
            var video = ffmpeg.av_find_best_stream(input, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, null, 0);
            Assert.True(video >= 0);
            var parameters = input->streams[video]->codecpar;
            Assert.Equal(AVCodecID.AV_CODEC_ID_HEVC, parameters->codec_id);
            // P010 is the input memory layout; decoded HEVC describes the stored bit depth.
            var descriptor = ffmpeg.av_pix_fmt_desc_get((AVPixelFormat)parameters->format);
            Assert.True(descriptor != null);
            Assert.Equal(3, descriptor->nb_components);
            for (var component = 0; component < 3; component++)
                Assert.Equal(10, descriptor->comp[(uint)component].depth);
            Assert.Equal(1, descriptor->log2_chroma_w);
            Assert.Equal(1, descriptor->log2_chroma_h);
        }
        finally
        {
            if (input != null) ffmpeg.avformat_close_input(&input);
        }
    }

    // Only the hardware source boundary is synthetic. Recording state, queues,
    // backend attachment, rollback, finalization, and resource disposal are real.
    private sealed class CaptureServiceFixture : IAsyncDisposable
    {
        internal const int Width = 256;
        internal const int Height = 256;
        private readonly Assembly _assembly;
        private readonly object _settings;
        private readonly object _device;
        private readonly BoundaryProxy _process;
        private readonly FrameIngress _ingress;
        private readonly string _fixtureDirectory;
        internal object Service { get; }
        internal object Source { get; }
        internal string OutputDirectory { get; }
        internal string RecoveryDirectory => Path.Combine(_fixtureDirectory, "recovery");
        internal int ProcessCalls => Volatile.Read(ref _process.Calls);

        internal CaptureServiceFixture(Assembly assembly, string directory, bool sourceIsP010)
        {
            _assembly = assembly;
            _fixtureDirectory = directory;
            Assert.Equal(RecoveryDirectory, Environment.GetEnvironmentVariable("SUSSUDIO_RECOVERY_DIRECTORY"));
            var process = DispatchProxy.Create(TypeOf("Sussudio.Services.Runtime.IProcessSupervisor"), typeof(BoundaryProxy));
            _process = (BoundaryProxy)process;
            var telemetry = DispatchProxy.Create(TypeOf("Sussudio.Services.Contracts.ISourceSignalTelemetryProvider"), typeof(BoundaryProxy));
            ((BoundaryProxy)telemetry).Response = TypeOf("Sussudio.Models.SourceSignalTelemetrySnapshot")
                .GetMethod("CreateUnavailable")!.Invoke(null, new object?[] { "Synthetic recording fixture", null });
            Service = Activator.CreateInstance(TypeOf("Sussudio.Services.Capture.CaptureService"), PrivateInstance,
                binder: null, new object?[] { process, telemetry, null }, culture: null)!;
            Source = Activator.CreateInstance(TypeOf("Sussudio.Services.Capture.UnifiedVideoCapture"), nonPublic: true)!;
            SetField(Source, "_width", Width);
            SetField(Source, "_height", Height);
            SetField(Source, "_fps", 30d);
            SetField(Source, "_isP010", sourceIsP010);
            SetField(Source, "_nativeInputFormat", sourceIsP010 ? "P010" : "NV12");
            SetField(Source, "_negotiatedFormat", sourceIsP010 ? "P010 256x256@30" : "NV12 256x256@30");
            SetField(Source, "_capture", Activator.CreateInstance(TypeOf("Sussudio.Services.Capture.MfSourceReaderVideoCapture"), nonPublic: true)!);
            var video = GetField(Service, "_videoPipeline")!;
            video.GetType().GetMethod("InstallCapture")!.Invoke(video, new[] { Source });
            _ingress = Source.GetType().GetMethod("OnFrameArrived", PrivateInstance)!.CreateDelegate<FrameIngress>(Source);
            _device = Activator.CreateInstance(TypeOf("Sussudio.Models.CaptureDevice"))!;
            Set(_device, "Id", "synthetic-recording-source");
            Set(_device, "Name", "Synthetic recording source");
            _settings = Activator.CreateInstance(TypeOf("Sussudio.Models.CaptureSettings"))!;
            Set(_settings, "Width", (uint)Width);
            Set(_settings, "Height", (uint)Height);
            Set(_settings, "FrameRate", 30d);
            Set(_settings, "RequestedPixelFormat", "P010");
            Set(_settings, "Format", Enum.Parse(TypeOf("Sussudio.Models.RecordingFormat"), "HevcMp4"));
            Set(_settings, "Quality", Enum.Parse(TypeOf("Sussudio.Models.VideoQuality"), "Custom"));
            Set(_settings, "CustomBitrateMbps", 2d);
            Set(_settings, "HdrEnabled", true);
            Set(_settings, "AudioEnabled", false);
            Set(_settings, "MicrophoneEnabled", false);
            OutputDirectory = Directory.CreateDirectory(Path.Combine(_fixtureDirectory, "output")).FullName;
            Set(_settings, "OutputPath", OutputDirectory);
        }

        internal Type TypeOf(string name) => _assembly.GetType(name, throwOnError: true)!;
        internal Task InitializeAsync() => TransitionAsync("InitializeAsync", _device, _settings, CancellationToken.None);
        internal Task StartAsync() => TransitionAsync("StartRecordingAsync", _settings, CancellationToken.None);
        internal Task StopAsync() => TransitionAsync("StopRecordingAsync", CancellationToken.None);
        internal Task CleanupAsync() => TransitionAsync("CleanupAsync", CancellationToken.None);
        internal object RuntimeSnapshot() => Service.GetType().GetMethod("GetRuntimeSnapshot")!.Invoke(Service, null)!;
        internal object HealthSnapshot() => Service.GetType().GetMethod("GetHealthSnapshot")!.Invoke(Service, null)!;
        internal void DeliverFrame(byte[] frame) => _ingress(frame, Width, Height, Environment.TickCount64);

        private Task TransitionAsync(string name, params object[] arguments)
            => (Task)Service.GetType().GetMethod(name, arguments.Select(argument => argument.GetType()).ToArray())!
                .Invoke(Service, arguments)!;

        public async ValueTask DisposeAsync()
        {
            // Native owners remain in this child for their entire lifetime. The parent
            // bounds a stalled disposal by terminating the child before deleting files.
            await ((IAsyncDisposable)Service).DisposeAsync();
            Assert.Equal(0, ProcessCalls);
        }

        private delegate void FrameIngress(ReadOnlySpan<byte> frame, int width, int height, long arrivalTick);
    }

    public class BoundaryProxy : DispatchProxy
    {
        public object? Response;
        public int Calls;
        protected override object Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Interlocked.Increment(ref Calls);
            var response = Response ?? throw new InvalidOperationException("Synthetic recording fixture must never launch a process.");
            return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(response.GetType()).Invoke(null, new[] { response })!;
        }
    }

    private static object CreateQueue(object sink, string name)
    {
        var packetType = sink.GetType().GetField(name, PrivateInstance)!.FieldType.GenericTypeArguments[0];
        var factory = typeof(Channel).GetMethods().Single(method =>
            method.Name == nameof(Channel.CreateUnbounded) && method.GetParameters().Length == 0);
        var queue = factory.MakeGenericMethod(packetType).Invoke(null, null)!;
        SetField(sink, name, queue);
        return queue;
    }

    private static void Write(object queue, object packet)
    {
        var writer = queue.GetType().GetProperty("Writer")!.GetValue(queue)!;
        Assert.True((bool)writer.GetType().GetMethod("TryWrite")!.Invoke(writer, new[] { packet })!);
    }

    private static void Complete(object queue)
    {
        var writer = queue.GetType().GetProperty("Writer")!.GetValue(queue)!;
        Assert.True((bool)writer.GetType().GetMethod("TryComplete")!.Invoke(writer, new object?[] { null })!);
    }

    // Kept out of the async test body on purpose: Span<T> is a ref struct and C# 12
    // forbids ref structs in async methods (CS9202), so the cast has to live here.
    private static void FillP010Fixture(byte[] buffer, int length, int lumaSampleCount)
    {
        var samples = MemoryMarshal.Cast<byte, ushort>(buffer.AsSpan(0, length));
        // 10-bit luma ~64 (the SDR fixture's 8-bit Y=16 black level scaled to 10-bit),
        // MSB-justified into the top bits of each 16-bit word as P010LE expects.
        samples[..lumaSampleCount].Fill(unchecked((ushort)(64 << 6)));
        // 10-bit neutral chroma ~512 (the SDR fixture's 8-bit UV=128 scaled), same layout.
        samples[lumaSampleCount..].Fill(unchecked((ushort)(512 << 6)));
    }

    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static object? GetField(object instance, string name) => instance.GetType().GetField(name, PrivateInstance)!.GetValue(instance);
    private static void SetField(object instance, string name, object? value) => instance.GetType().GetField(name, PrivateInstance)!.SetValue(instance, value);
    private static void Set(object instance, string name, object value) => instance.GetType().GetProperty(name)!.SetValue(instance, value);
    private static T Read<T>(object instance, string name) => (T)instance.GetType().GetProperty(name)!.GetValue(instance)!;
}
