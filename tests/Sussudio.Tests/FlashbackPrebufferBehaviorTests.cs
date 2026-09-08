using System.Buffers;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Threading.Channels;
using FFmpeg.AutoGen;
using Xunit;
using BundledRuntime = Sussudio.Tests.InProcessRecordingStructureVerifierTests.BundledRuntime;

namespace Sussudio.Tests;

public sealed class FlashbackPrebufferBehaviorTests : IClassFixture<BundledRuntime>
{
    private readonly BundledRuntime _runtime;

    public FlashbackPrebufferBehaviorTests(BundledRuntime runtime) => _runtime = runtime;

    [Fact]
    public void OneCpuFrameIsRetainedAndConsumedOnceWithoutRewinding()
    {
        using var session = new PrebufferSession(_runtime);
        var initialData = Read<IntPtr>(session.InitialFrame, "Data");
        Assert.NotEqual(IntPtr.Zero, initialData);
        session.Commands.OnPeek = count =>
        {
            if (count == 2) session.Commands.EnqueueStop();
        };

        session.Prime();

        Assert.Equal(1, session.HeldFrameCount);
        Assert.Equal(1, session.Commands.PendingCount);
        var retained = session.ReadPlaybackFrame();
        Assert.Equal(session.ResumeTarget, Read<TimeSpan>(retained, "Pts"));
        Assert.Equal(initialData, Read<IntPtr>(retained, "Data"));
        Assert.Equal(0, session.HeldFrameCount);
        Assert.Equal(session.ResumeTarget + TimeSpan.FromMilliseconds(20),
            Read<TimeSpan>(session.ReadPlaybackFrame(), "Pts"));
        Assert.True(session.Commands.ReadStop());
    }

    [Fact]
    public void ASecondCpuFrameClearsTheWholeQueueAndRewindsToTheResumeTarget()
    {
        using var session = new PrebufferSession(_runtime);
        session.Commands.OnPeek = count =>
        {
            if (count == 3) session.Commands.EnqueueStop();
        };

        session.Prime();

        Assert.Equal(3, session.Commands.PeekCount);
        Assert.Equal(0, session.HeldFrameCount);
        Assert.Equal(1, session.Commands.PendingCount);
        Assert.Equal(session.ResumeTarget, Read<TimeSpan>(session.ReadPlaybackFrame(), "Pts"));
        Assert.Equal(session.ResumeTarget + TimeSpan.FromMilliseconds(20),
            Read<TimeSpan>(session.ReadPlaybackFrame(), "Pts"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HardwareMarkedFramesAreReleasedWhenPrimingReturnsWithTheDecoderRewound(bool afterRetainedCpuFrame)
    {
        using var session = new PrebufferSession(_runtime);
        var native = session.CreateHardwareMarkedFrame();
        Assert.Equal(2, native.ReferenceCount);
        if (!afterRetainedCpuFrame) session.SetPendingDecoderFrame(native.Frame);
        session.Commands.OnPeek = count =>
        {
            if (afterRetainedCpuFrame && count == 2)
            {
                Assert.Equal(1, session.HeldFrameCount);
                session.SetPendingDecoderFrame(native.Frame);
            }
            if (count == (afterRetainedCpuFrame ? 3 : 2)) session.Commands.EnqueueStop();
        };

        session.Prime();

        // The frame owns a real AVBuffer reference, but no D3D11 texture. This
        // executes the hardware-frame ownership branch, not GPU decode/display.
        Assert.Equal(1, native.ReferenceCount);
        Assert.Equal(0, session.HeldFrameCount);
        Assert.Equal(session.ResumeTarget, Read<TimeSpan>(session.ReadPlaybackFrame(), "Pts"));
        session.ClearFrames();
        Assert.Equal(1, native.ReferenceCount);
    }

    [Fact]
    public void RewindPreservesBufferedAudioAndRejectsAlreadyAcceptedTimestamps()
    {
        using var session = new PrebufferSession(_runtime);
        session.Commands.OnPeek = count =>
        {
            if (count != 3) return;
            session.SendAudio(TimeSpan.FromSeconds(1), milliseconds: 200);
            session.Commands.EnqueueStop();
        };

        session.Prime();

        Assert.Equal(0, session.HeldFrameCount);
        Assert.Equal(200, session.BufferedAudioMs);
        Assert.Equal(TimeSpan.FromSeconds(1).Ticks, session.LastAcceptedAudioPts);
        Assert.Equal(session.ResumeTarget, Read<TimeSpan>(session.ReadPlaybackFrame(), "Pts"));

        session.SendAudio(TimeSpan.FromMilliseconds(900), milliseconds: 20);
        session.SendAudio(TimeSpan.FromSeconds(1), milliseconds: 20);
        Assert.Equal(200, session.BufferedAudioMs);
        Assert.Equal(1, session.AudioQueueDepth);
        session.SendAudio(TimeSpan.FromMilliseconds(1100), milliseconds: 20);
        Assert.Equal(220, session.BufferedAudioMs);
        Assert.Equal(2, session.AudioQueueDepth);
        Assert.Equal(TimeSpan.FromMilliseconds(1100).Ticks, session.LastAcceptedAudioPts);
    }

    [Theory]
    [InlineData(180, false)]
    [InlineData(250, false)]
    [InlineData(251, true)]
    public void AudioBeyondTheDiscardLimitFlushesVideoAndRestartsTheAudioGate(int bufferedMs, bool discarded)
    {
        using var session = new PrebufferSession(_runtime);
        session.Commands.OnPeek = count =>
        {
            if (count == 2) session.SendAudio(TimeSpan.FromSeconds(1), bufferedMs);
        };

        session.Prime();

        Assert.Equal(2, session.Commands.PeekCount);
        Assert.Equal(discarded ? 0 : 1, session.HeldFrameCount);
        Assert.Equal(discarded ? 0 : bufferedMs, session.BufferedAudioMs);
        Assert.Equal(discarded ? 0 : 1, session.AudioQueueDepth);
        Assert.Equal(discarded ? 0 : TimeSpan.FromSeconds(1).Ticks, session.LastAcceptedAudioPts);
        Assert.Equal(session.ResumeTarget, Read<TimeSpan>(session.ReadPlaybackFrame(), "Pts"));
        Assert.Equal(session.ResumeTarget + TimeSpan.FromMilliseconds(20),
            Read<TimeSpan>(session.ReadPlaybackFrame(), "Pts"));

        session.SendAudio(session.ResumeTarget - TimeSpan.FromMilliseconds(1), milliseconds: 20);
        Assert.Equal(discarded ? 0 : bufferedMs, session.BufferedAudioMs);
        session.SendAudio(session.ResumeTarget + TimeSpan.FromMilliseconds(40), milliseconds: 20);
        Assert.Equal(discarded ? 20 : bufferedMs, session.BufferedAudioMs);
    }

    [Fact]
    public void AacMediaPrimesTheRealAudioQueueAndResumesAtTheRequestedVideoFrame()
    {
        using var session = new PrebufferSession(_runtime, "h264-program.mp4");

        session.Prime();

        Assert.InRange(session.BufferedAudioMs, 180, 250);
        Assert.True(session.AudioQueueDepth > 0);
        Assert.True(session.LastAcceptedAudioPts > session.ResumeTarget.Ticks);
        Assert.Equal(0, session.HeldFrameCount);
        Assert.Equal(session.ResumeTarget, Read<TimeSpan>(session.ReadPlaybackFrame(), "Pts"));
        session.FlushAudio();
        Assert.Equal(0, session.BufferedAudioMs);
        Assert.Equal(0, session.AudioQueueDepth);
    }

    [Fact]
    public unsafe void HevcHdrSoftwareDecodePreservesHdrAndTheP010OutputLayout()
    {
        using var session = new PrebufferSession(_runtime, "hevc-hdr-explicit.mp4");

        var frame = session.ReadPlaybackFrame();

        Assert.True(Read<bool>(frame, "IsHdr"));
        Assert.False(Read<bool>(frame, "IsD3D11Texture"));
        Assert.Equal(64, Read<int>(frame, "Width"));
        Assert.Equal(64, Read<int>(frame, "Height"));
        Assert.Equal(session.ResumeTarget, Read<TimeSpan>(frame, "Pts"));
        var length = Read<int>(frame, "DataLength");
        Assert.Equal(64 * 64 * 3, length);
        var data = Read<IntPtr>(frame, "Data");
        Assert.NotEqual(IntPtr.Zero, data);
        var samples = new ReadOnlySpan<ushort>((void*)data, length / sizeof(ushort));
        // The committed compressed black fixture decodes to planar Y=66, U=V=512.
        // P010 stores each ten-bit sample in the high bits of its sixteen-bit word.
        foreach (var sample in samples[..(64 * 64)])
            Assert.Equal((ushort)(66 << 6), sample);
        foreach (var sample in samples[(64 * 64)..])
            Assert.Equal((ushort)(512 << 6), sample);
    }

    [Fact]
    public void ACommandAlreadyWaitingPreventsDecodeAndRemainsAvailableToItsOwner()
    {
        using var session = new PrebufferSession(_runtime);
        session.Commands.EnqueueStop();

        session.Prime();

        Assert.Equal(0, session.HeldFrameCount);
        Assert.True(session.HasPendingDecoderFrame);
        Assert.Equal(session.ResumeTarget, Read<TimeSpan>(session.ReadPlaybackFrame(), "Pts"));
        Assert.True(session.Commands.ReadStop());
        Assert.Equal(0, session.Commands.PendingCount);
    }

    [Fact]
    public void CancellationAfterRetentionLeavesTheFrameForProductionQueueCleanup()
    {
        using var session = new PrebufferSession(_runtime);
        using var cancellation = new CancellationTokenSource();
        session.Commands.OnPeek = count =>
        {
            if (count == 2) cancellation.Cancel();
        };

        var error = Assert.Throws<OperationCanceledException>(() => session.Prime(cancellation.Token));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(1, session.HeldFrameCount);
        // Prime borrows this queue from the playback-thread owner. Invoke its
        // real cleanup method; this test does not run the thread-exit lifecycle.
        session.ClearFrames();
        Assert.Equal(0, session.HeldFrameCount);
        session.ClearFrames();
        Assert.Equal(0, session.HeldFrameCount);
    }

    [Fact]
    public void CancellationIsObservedWhenTemporaryEofRequiresAnotherAttempt()
    {
        using var session = new PrebufferSession(_runtime);
        using var cancellation = new CancellationTokenSource();
        session.ReadToTemporaryEof();
        session.Commands.OnPeek = count =>
        {
            if (count == 2) cancellation.Cancel();
        };

        var error = Assert.Throws<OperationCanceledException>(() => session.Prime(cancellation.Token));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(2, session.Commands.PeekCount);
        Assert.Equal(0, session.HeldFrameCount);
        Assert.False(session.HasPendingDecoderFrame);
    }

    private sealed class PrebufferSession : IDisposable
    {
        private readonly DirectoryInfo? _directory;
        private readonly string _path = string.Empty;
        private readonly byte[] _originalHash;
        private readonly object _manager = null!;
        private readonly object _controller = null!;
        private readonly object _decoder = null!;
        private readonly object _audio = null!;
        private readonly object _frames = null!;
        private readonly bool _inputWritten;
        private readonly List<NativeFrameReference> _nativeFrames = new();

        public PrebufferSession(BundledRuntime runtime, string fileName = "h264-video.mp4")
        {
            var input = File.ReadAllBytes(Path.Combine(runtime.FixtureDirectory, fileName));
            _originalHash = SHA256.HashData(input);
            Assert.Equal(runtime.FixtureHashes[fileName], Convert.ToHexString(_originalHash));
            try
            {
                _directory = Directory.CreateTempSubdirectory("sussudio-prebuffer-");
                _path = Path.Combine(_directory.FullName, fileName);
                _manager = Activator.CreateInstance(runtime.Type("Sussudio.Services.Flashback.FlashbackBufferManager"),
                    new object?[] { null })!;
                _controller = Activator.CreateInstance(runtime.Type("Sussudio.Services.Flashback.FlashbackPlaybackController"), _manager)!;
                _decoder = Activator.CreateInstance(runtime.Type("Sussudio.Services.Flashback.FlashbackDecoder"))!;
                _audio = Activator.CreateInstance(runtime.Type("Sussudio.Services.Audio.WasapiAudioPlayback"))!;
                _frames = Activator.CreateInstance(typeof(Queue<>).MakeGenericType(
                    runtime.Type("Sussudio.Services.Flashback.DecodedVideoFrame")))!;
                var commandType = runtime.Type("Sussudio.Services.Flashback.FlashbackPlaybackCommandMailbox+Command");
                var stop = Activator.CreateInstance(commandType)!;
                Set(stop, "Kind", Enum.Parse(runtime.Type("Sussudio.Services.Flashback.FlashbackPlaybackCommandMailbox+CommandKind"), "Stop"));
                Commands = (ICommandBoundary)Activator.CreateInstance(typeof(CommandBoundary<>).MakeGenericType(commandType), stop)!;
                File.WriteAllBytes(_path, input);
                _inputWritten = true;
                Set(_controller, "GpuDecodeEnabled", false);
                // Exercise the real managed audio queue without a WASAPI endpoint
                // or render thread, as in WasapiNegotiatedFormatAndWorkerLifetimeTests.
                SetField(_audio, "_initialized", 1);
                SetField(_controller, "_audioPlayback", _audio);
                SetField(_controller, "_currentOpenFilePath", _path);
                Invoke(_decoder, "Initialize", IntPtr.Zero, IntPtr.Zero);
                Invoke(_decoder, "OpenFile", _path);
                Assert.True((bool)Invoke(_decoder, "SeekTo", ResumeTarget, CancellationToken.None)!);
                InitialFrame = GetField(_decoder, "_pendingVideoFrame")!;
                Assert.True(HasPendingDecoderFrame);
                Assert.Equal(ResumeTarget, Read<TimeSpan>(InitialFrame, "Pts"));
                Invoke(_controller, "RestoreAudioCallback", _decoder, ResumeTarget.Ticks, 0L);
            }
            catch (Exception setupFailure)
            {
                try { Dispose(); }
                catch (Exception cleanupFailure)
                {
                    throw new AggregateException("Prebuffer fixture setup and cleanup failed.", setupFailure, cleanupFailure);
                }
                throw;
            }
        }

        public TimeSpan ResumeTarget { get; } = TimeSpan.FromMilliseconds(400);
        public object InitialFrame { get; } = null!;
        public ICommandBoundary Commands { get; } = null!;
        public int HeldFrameCount => ((ICollection)_frames).Count;
        public int AudioQueueDepth => Read<int>(_audio, "PlaybackQueueDepth");
        public double BufferedAudioMs => Read<double>(_audio, "PlaybackBufferedDurationMs");
        public long LastAcceptedAudioPts => (long)GetField(_controller, "_lastAudioPtsTicks")!;
        public bool HasPendingDecoderFrame => (bool)GetField(_decoder, "_hasPendingVideoFrame")!;

        public void Prime(CancellationToken cancellationToken = default)
        {
            object?[] arguments = { _decoder, _frames, Commands.Reader, true, ResumeTarget, "behavior_test", cancellationToken, false };
            Invoke(_controller, "PrimePlaybackAudioBuffer", arguments);
            Assert.True((bool)arguments[3]!);
        }

        public object ReadPlaybackFrame()
        {
            object?[] arguments = { _decoder, _frames, null, CancellationToken.None };
            Assert.True((bool)Invoke(_controller, "TryReadNextPlaybackFrame", arguments)!);
            return arguments[2]!;
        }

        public void ReadToTemporaryEof()
        {
            // A completed-file frame count would require BeginCompletedInputDrain.
            // Here temporary EOF is the desired input to the real retry path.
            for (var count = 0; count <= 100; count++)
            {
                object?[] arguments = { null, CancellationToken.None };
                if (!(bool)Invoke(_decoder, "TryDecodeNextVideoFrame", arguments)!) return;
            }
            Assert.Fail("The two-second fixture did not reach temporary EOF within its video-frame bound.");
        }

        public void SendAudio(TimeSpan pts, int milliseconds)
        {
            var callback = (Delegate)Read<object>(_decoder, "AudioChunkCallback");
            var chunk = Activator.CreateInstance(callback.GetType().GenericTypeArguments[0])!;
            var length = milliseconds * 48 * 2 * sizeof(float);
            var samples = ArrayPool<byte>.Shared.Rent(length);
            samples.AsSpan(0, length).Clear();
            Set(chunk, "Samples", samples);
            Set(chunk, "ValidLength", length);
            Set(chunk, "Pts", pts);
            // The production callback returns rejected buffers or transfers them
            // to WASAPI. Flush/Dispose owns accepted buffers from this point.
            callback.DynamicInvoke(chunk);
        }

        public NativeFrameReference CreateHardwareMarkedFrame()
        {
            var native = new NativeFrameReference(InitialFrame);
            _nativeFrames.Add(native);
            return native;
        }

        public void SetPendingDecoderFrame(object frame)
        {
            SetField(_decoder, "_pendingVideoFrame", frame);
            SetField(_decoder, "_hasPendingVideoFrame", true);
        }

        public void ClearFrames() => Invoke(_controller, "ClearPrebufferedFrames", _frames, "behavior_test_cleanup");
        public void FlushAudio() => Invoke(_audio, "Flush");

        public void Dispose()
        {
            List<Exception>? failures = null;
            Attempt(() =>
            {
                if (_controller != null && _frames != null) ClearFrames();
            });
            Attempt(() => (_decoder as IDisposable)?.Dispose());
            Attempt(() => (_audio as IDisposable)?.Dispose());
            Attempt(() => (_controller as IDisposable)?.Dispose());
            Attempt(() => (_manager as IDisposable)?.Dispose());
            foreach (var native in _nativeFrames) Attempt(native.Dispose);
            Attempt(() =>
            {
                if (_inputWritten)
                {
                    Assert.Equal(_originalHash, SHA256.HashData(File.ReadAllBytes(_path)));
                    using var exclusive = File.Open(_path, FileMode.Open, FileAccess.Read, FileShare.None);
                }
            });
            Attempt(() => _directory?.Delete(recursive: true));
            if (failures != null) throw new AggregateException("Prebuffer fixture cleanup failed.", failures);

            void Attempt(Action cleanup)
            {
                try { cleanup(); }
                catch (Exception failure)
                {
                    (failures ??= new()).Add(failure);
                }
            }
        }
    }

    // This wrapper schedules external arrivals at a real channel boundary. It
    // contains no prebuffer decisions, frame queue, audio policy, or rewind logic.
    private interface ICommandBoundary
    {
        object Reader { get; }
        Action<int>? OnPeek { get; set; }
        int PeekCount { get; }
        int PendingCount { get; }
        void EnqueueStop();
        bool ReadStop();
    }

    private sealed class CommandBoundary<T> : ChannelReader<T>, ICommandBoundary
    {
        private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();
        private readonly T _stop;

        public CommandBoundary(T stop) => _stop = stop;
        public object Reader => this;
        public Action<int>? OnPeek { get; set; }
        public int PeekCount { get; private set; }
        public int PendingCount => _channel.Reader.Count;
        public void EnqueueStop() => Assert.True(_channel.Writer.TryWrite(_stop));
        public bool ReadStop() => _channel.Reader.TryRead(out var item) && EqualityComparer<T>.Default.Equals(_stop, item);
        public override bool TryRead([MaybeNullWhen(false)] out T item) => _channel.Reader.TryRead(out item);
        public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default) =>
            _channel.Reader.WaitToReadAsync(cancellationToken);

        public override bool TryPeek([MaybeNullWhen(false)] out T item)
        {
            PeekCount++;
            OnPeek?.Invoke(PeekCount);
            return _channel.Reader.TryPeek(out item);
        }
    }

    private sealed unsafe class NativeFrameReference : IDisposable
    {
        private AVFrame* _frame;
        private AVBufferRef* _observer;
        private bool _bufferAttached;

        public NativeFrameReference(object template)
        {
            try
            {
                _frame = ffmpeg.av_frame_alloc();
                _observer = ffmpeg.av_buffer_alloc(1);
                if (_frame == null || _observer == null)
                    throw new OutOfMemoryException("Could not allocate a native frame reference fixture.");
                _frame->buf[0] = ffmpeg.av_buffer_ref(_observer);
                if (_frame->buf[0] == null)
                    throw new OutOfMemoryException("Could not retain the native frame buffer.");
                _bufferAttached = true;
                Frame = Activator.CreateInstance(template.GetType())!;
                foreach (var property in template.GetType().GetProperties()) property.SetValue(Frame, property.GetValue(template));
                Set(Frame, "HeldFrame", (IntPtr)_frame);
                Set(Frame, "IsD3D11Texture", true);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public object Frame { get; } = null!;
        public int ReferenceCount => ffmpeg.av_buffer_get_ref_count(_observer);

        public void Dispose()
        {
            // Queue and decoder owners have already quiesced. A count of one
            // means production freed the frame; its pointer must not be touched.
            if (_frame != null && (!_bufferAttached || _observer == null || ReferenceCount > 1))
            {
                var frame = _frame;
                ffmpeg.av_frame_free(&frame);
            }
            _frame = null;
            var observer = _observer;
            _observer = null;
            ffmpeg.av_buffer_unref(&observer);
        }
    }

    private const BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static T Read<T>(object instance, string name) => (T)instance.GetType().GetProperty(name, InstanceFlags)!.GetValue(instance)!;
    private static void Set(object instance, string name, object value) => instance.GetType().GetProperty(name, InstanceFlags)!.SetValue(instance, value);
    private static object? GetField(object instance, string name) => instance.GetType().GetField(name, InstanceFlags)!.GetValue(instance);
    private static void SetField(object instance, string name, object value) => instance.GetType().GetField(name, InstanceFlags)!.SetValue(instance, value);

    private static object? Invoke(object instance, string name, params object?[] arguments)
    {
        try
        {
            return instance.GetType().GetMethod(name, InstanceFlags)!.Invoke(instance, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}
