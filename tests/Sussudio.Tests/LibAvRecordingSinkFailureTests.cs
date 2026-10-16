using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Channels;
using Xunit;

namespace Sussudio.Tests;

// The recording sink's encode loop owns every failure on its lanes. A send that throws must end the session the same
// way whichever lane carried the frame; the D3D11 lane is the lane that queues native references, so it carries the
// check (the CUDA lane that used to was never constructed and was removed). Failure modes:
//  - the send exception is swallowed or retried instead of latched (EncodingFailed, _encodingFailure);
//  - OnEncodingFailed fires more than once, off the encode-loop thread, or not at all when the subscriber throws;
//  - the sink keeps admitting work (_started stays true) or counts the failed frame as submitted or encoded;
//  - the texture being sent, or one still queued behind it, is leaked or released twice, or the depth stays nonzero;
//  - a later dispose, or a second one, replaces the first failure, notifies again or releases a texture again.
[SupportedOSPlatform("windows")]
public sealed class LibAvRecordingSinkFailureTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    // Hang guard only: a passing run finishes in milliseconds, and a broken loop that swallows the failure waits on
    // its open writers until this lapses.
    private static readonly TimeSpan DeadlockGuard = TimeSpan.FromSeconds(30);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GpuSendFailureIsLatchedByOwnerAndReleasesQueuedTextures(bool callbackThrows)
    {
        var assembly = SussudioAssembly.Load();
        assembly.GetType("Sussudio.Services.Runtime.FfmpegRuntimeInit", true)!
            .GetMethod("EnsureInitialized")!.Invoke(null, new object[] { true });
        var sinkType = assembly.GetType("Sussudio.Services.Recording.LibAvRecordingSink", true)!;
        var sink = Activator.CreateInstance(sinkType)!;
        await using var sinkLifetime = (IAsyncDisposable)sink;
        using var cancellation = new CancellationTokenSource();
        using var sentTexture = new TrackedComReference();
        using var queuedTexture = new TrackedComReference();
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
            var gpuQueue = Queue("_gpuQueue");
            var writer = gpuQueue.GetType().GetProperty("Writer")!.GetValue(gpuQueue)!;
            var packetType = sinkType.GetNestedType("GpuFramePacket", BindingFlags.NonPublic)!;
            foreach (var texture in new[] { sentTexture, queuedTexture })
            {
                // The producer's AddRef travels with the packet; the sink owns releasing it.
                var packet = Activator.CreateInstance(packetType, texture.AddQueueReference(), 0)!;
                Assert.True((bool)writer.GetType().GetMethod("TryWrite")!.Invoke(writer, new[] { packet })!);
            }
            Set("_gpuQueueDepth", 2);
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

            // The concrete unopened encoder fails before touching the texture. Writers stay open, so only the
            // failure path can end the loop.
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
            Assert.Equal(0, (int)Get("_gpuQueueDepth")!);
            var reader = gpuQueue.GetType().GetProperty("Reader")!.GetValue(gpuQueue)!;
            Assert.Equal(0, (int)reader.GetType().GetProperty("Count")!.GetValue(reader)!);
            Assert.Equal(0L, (long)Get("_videoFramesSubmittedToEncoder")!);
            Assert.Equal(0L, (long)Get("_encodedVideoFrames")!);
            Assert.Equal(0, sentTexture.UnreleasedQueueReferences);
            Assert.Equal(0, queuedTexture.UnreleasedQueueReferences);

            await ((IAsyncDisposable)sink).DisposeAsync();
            await ((Task)sinkType.GetProperty("CleanupCompletionTask", PrivateInstance)!.GetValue(sink)!)
                .WaitAsync(DeadlockGuard);
            await ((IAsyncDisposable)sink).DisposeAsync();
            Assert.Same(failure, Get("_encodingFailure"));
            Assert.Equal(1, notifications);
            Assert.Equal(0, sentTexture.UnreleasedQueueReferences);
            Assert.Equal(0, queuedTexture.UnreleasedQueueReferences);
        }
        finally
        {
            // Join before releasing callback roots or disposing the cancellation source.
            if (owner == null || !owner.IsCompleted) cancellation.Cancel();
            if (owner != null) await owner.WaitAsync(DeadlockGuard);
            await ((IAsyncDisposable)sink).DisposeAsync();
        }
    }

    // A real COM identity (a CCW over a managed object) standing in for an ID3D11Texture2D: on this path the sink only
    // AddRefs and Releases the pointer, never calls through it. The count is read from IUnknown itself, so a leak reads
    // positive and a double release reads negative. A guard reference keeps one extra release from destroying it.
    private sealed class TrackedComReference : IDisposable
    {
        private readonly object _target = new();
        private readonly int _baseline;
        private IntPtr _unknown;

        public TrackedComReference()
        {
            _unknown = Marshal.GetIUnknownForObject(_target);
            Marshal.AddRef(_unknown);
            _baseline = CurrentCount();
        }

        public int UnreleasedQueueReferences => CurrentCount() - _baseline;

        public IntPtr AddQueueReference()
        {
            Marshal.AddRef(_unknown);
            return _unknown;
        }

        private int CurrentCount()
        {
            var count = Marshal.AddRef(_unknown);
            Marshal.Release(_unknown);
            return count - 1;
        }

        public void Dispose()
        {
            var unknown = _unknown;
            _unknown = IntPtr.Zero;
            if (unknown == IntPtr.Zero) return;
            Marshal.Release(unknown);
            Marshal.Release(unknown);
            GC.KeepAlive(_target);
        }
    }
}
