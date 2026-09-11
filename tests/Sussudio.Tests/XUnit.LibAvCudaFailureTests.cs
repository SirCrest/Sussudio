using System.Reflection;
using System.Threading.Channels;
using FFmpeg.AutoGen;
using Xunit;

namespace Sussudio.Tests;

public sealed class LibAvCudaFailureTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly TimeSpan DeadlockGuard = TimeSpan.FromSeconds(5);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CudaSendFailureIsLatchedByOwnerAndReleasesQueuedFrames(bool callbackThrows)
    {
        var assembly = SussudioAssembly.Load();
        assembly.GetType("Sussudio.Services.Recording.LibAvEncoder", true)!
            .GetMethod("InitializeFFmpeg")!.Invoke(null, new object[] { true });
        var sinkType = assembly.GetType("Sussudio.Services.Recording.LibAvRecordingSink", true)!;
        var sink = Activator.CreateInstance(sinkType)!;
        await using var sinkLifetime = (IAsyncDisposable)sink;
        using var cancellation = new CancellationTokenSource();
        using var firstFrame = new TrackedFrame(cancellation.Cancel);
        using var queuedFrame = new TrackedFrame();
        Task? owner = null;
        try
        {
            object? Get(string name) => sinkType.GetField(name, PrivateInstance)!.GetValue(sink);
            void Set(string name, object value) => sinkType.GetField(name, PrivateInstance)!.SetValue(sink, value);
            object Queue(string name)
            {
                var packetType = sinkType.GetField(name, PrivateInstance)!.FieldType.GenericTypeArguments[0];
                var factory = typeof(Channel).GetMethods().Single(method =>
                    method.Name == nameof(Channel.CreateUnbounded) && method.GetParameters().Length == 0);
                var channel = factory.MakeGenericMethod(packetType).Invoke(null, null)!;
                Set(name, channel);
                return channel;
            }

            Queue("_videoQueue");
            Queue("_audioQueue");
            var cudaQueue = Queue("_cudaQueue");
            var writer = cudaQueue.GetType().GetProperty("Writer")!.GetValue(cudaQueue)!;
            var packetType = sinkType.GetNestedType("CudaFramePacket", BindingFlags.NonPublic)!;
            foreach (var frame in new[] { firstFrame, queuedFrame })
            {
                var packet = Activator.CreateInstance(packetType, frame.Pointer)!;
                Assert.True((bool)writer.GetType().GetMethod("TryWrite")!.Invoke(writer, new[] { packet })!);
                frame.TransferToQueue();
            }
            Set("_cudaQueueDepth", 2);
            Set("_started", true);
            Set("_cts", cancellation);

            Exception? notification = null;
            var notifications = 0;
            var notificationThread = 0;
            sinkType.GetProperty("OnEncodingFailed")!.SetValue(sink, (Action<Exception>)(error =>
            {
                notifications++;
                notification = error;
                notificationThread = Environment.CurrentManagedThreadId;
                if (callbackThrows) throw new IOException("Notification subscriber failed.");
            }));

            // The concrete unopened encoder fails before touching frame data. Keep writers
            // open: first-frame release cancels a broken loop instead of letting it report
            // an unrelated finalization failure after swallowing the send exception.
            var encode = sinkType.GetMethod("EncodingLoop", PrivateInstance)!
                .CreateDelegate<Action<CancellationToken>>(sink);
            var ownerThread = 0;
            owner = Task.Factory.StartNew(() =>
            {
                ownerThread = Environment.CurrentManagedThreadId;
                encode(cancellation.Token);
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            Set("_encodingTask", owner);
            await owner.WaitAsync(DeadlockGuard);

            Assert.True(owner.IsCompletedSuccessfully);
            Assert.True((bool)sinkType.GetProperty("EncodingFailed")!.GetValue(sink)!);
            Assert.Equal(1, notifications);
            var failure = Assert.IsType<InvalidOperationException>(notification);
            Assert.Equal("LibAvEncoder is not initialized.", failure.Message);
            Assert.Same(failure, Get("_encodingFailure"));
            Assert.Equal(ownerThread, notificationThread);
            Assert.False((bool)Get("_started")!);
            Assert.Equal(0, (int)Get("_cudaQueueDepth")!);
            var reader = cudaQueue.GetType().GetProperty("Reader")!.GetValue(cudaQueue)!;
            Assert.Equal(0, (int)reader.GetType().GetProperty("Count")!.GetValue(reader)!);
            Assert.Equal(0L, (long)Get("_videoFramesSubmittedToEncoder")!);
            Assert.Equal(0L, (long)Get("_encodedVideoFrames")!);
            Assert.Equal(1, firstFrame.ReleaseCount);
            Assert.Equal(1, queuedFrame.ReleaseCount);

            await ((IAsyncDisposable)sink).DisposeAsync();
            await ((Task)sinkType.GetProperty("CleanupCompletionTask", PrivateInstance)!.GetValue(sink)!)
                .WaitAsync(DeadlockGuard);
            await ((IAsyncDisposable)sink).DisposeAsync();
            Assert.Same(failure, Get("_encodingFailure"));
            Assert.Equal(1, notifications);
            Assert.Equal(1, firstFrame.ReleaseCount);
            Assert.Equal(1, queuedFrame.ReleaseCount);
        }
        finally
        {
            // Join before releasing callback roots or disposing the cancellation source.
            if (owner == null || !owner.IsCompleted) cancellation.Cancel();
            if (owner != null) await owner.WaitAsync(DeadlockGuard);
            await ((IAsyncDisposable)sink).DisposeAsync();
            GC.KeepAlive(firstFrame);
            GC.KeepAlive(queuedFrame);
        }
    }

    private sealed unsafe class TrackedFrame : IDisposable
    {
        private AVFrame* _frame;
        private readonly av_buffer_create_free _release;
        private int _releaseCount;

        public TrackedFrame(Action? released = null)
        {
            _release = (_, data) =>
            {
                ffmpeg.av_free(data);
                Interlocked.Increment(ref _releaseCount);
                released?.Invoke();
            };
            _frame = ffmpeg.av_frame_alloc();
            if (_frame == null) throw new OutOfMemoryException("Could not allocate test AVFrame.");
            AVBufferRef* buffer = null;
            byte* data = null;
            try
            {
                data = (byte*)ffmpeg.av_malloc(16);
                if (data == null) throw new OutOfMemoryException("Could not allocate tracked frame data.");
                buffer = ffmpeg.av_buffer_create(data, 16, _release, null, 0);
                if (buffer == null) throw new OutOfMemoryException("Could not allocate tracked AVBuffer.");
                data = null;
                if (ffmpeg.av_frame_new_side_data_from_buf(
                        _frame, AVFrameSideDataType.AV_FRAME_DATA_SEI_UNREGISTERED, buffer) == null)
                    throw new OutOfMemoryException("Could not attach tracked frame data.");
                buffer = null;
            }
            catch
            {
                ffmpeg.av_free(data);
                ffmpeg.av_buffer_unref(&buffer);
                Dispose();
                throw;
            }
        }

        public IntPtr Pointer => (IntPtr)_frame;
        public int ReleaseCount => Volatile.Read(ref _releaseCount);
        public void TransferToQueue() => _frame = null;
        public void Dispose()
        {
            var frame = _frame;
            _frame = null;
            ffmpeg.av_frame_free(&frame);
            GC.KeepAlive(_release);
        }
    }
}
