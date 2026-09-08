using System.Buffers;
using System.Reflection;
using System.Threading.Channels;
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

    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static object? GetField(object instance, string name) => instance.GetType().GetField(name, PrivateInstance)!.GetValue(instance);
    private static void SetField(object instance, string name, object value) => instance.GetType().GetField(name, PrivateInstance)!.SetValue(instance, value);
    private static void Set(object instance, string name, object value) => instance.GetType().GetProperty(name)!.SetValue(instance, value);
    private static T Read<T>(object instance, string name) => (T)instance.GetType().GetProperty(name)!.GetValue(instance)!;
}
