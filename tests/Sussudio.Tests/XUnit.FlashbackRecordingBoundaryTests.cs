using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public sealed class FlashbackRecordingBoundaryTests
{
    private static readonly TimeSpan DeadlockGuard = TimeSpan.FromSeconds(5);

    [Fact]
    public void CaptureFreezesAcceptedCountsAndPublishesOnlyAnActiveRecordingFence()
    {
        using var sink = new SinkScenario();
        sink.Set("_videoFramesEnqueued", 2L);
        sink.Set("_audioPacketsAccepted", 3L);
        sink.Set("_microphonePacketsAccepted", 5L);
        sink.Set("_gpuFramesEnqueued", 7L);
        var fence = sink.Capture();
        Assert.Equal(0, (int)sink.Get("_recordingActive")!);
        Assert.Same(fence, sink.Get("_activeRecordingBoundaryFence"));
        sink.SetAccepted(99);
        Assert.Equal(2L, (long)fence.GetType().GetProperty("VideoPacketsAccepted")!.GetValue(fence)!);
        Assert.Equal(3L, (long)fence.GetType().GetProperty("AudioPacketsAccepted")!.GetValue(fence)!);
        Assert.Equal(5L, (long)fence.GetType().GetProperty("MicrophonePacketsAccepted")!.GetValue(fence)!);
        Assert.Equal(7L, (long)fence.GetType().GetProperty("GpuPacketsAccepted")!.GetValue(fence)!);

        using var inactiveSink = new SinkScenario();
        inactiveSink.Capture(recording: false);
        Assert.Equal(0, (int)inactiveSink.Get("_recordingActive")!);
        Assert.Null(inactiveSink.Get("_activeRecordingBoundaryFence"));
    }

    [Fact]
    public async Task EmptyBoundaryResolvesBothVideoLanesAtZero()
    {
        using var sink = new SinkScenario();
        var fence = sink.Capture();
        Assert.True(Resolved(fence));
        Assert.Equal(TimeSpan.Zero, EndPts(fence));
        Assert.True(await sink.Wait(fence).WaitAsync(DeadlockGuard));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task RetirementBeforeOrAfterCaptureResolvesTheActualVideoLane(bool gpu, bool retireBeforeCapture)
    {
        using var sink = new SinkScenario();
        sink.Set(gpu ? "_gpuFramesEnqueued" : "_videoFramesEnqueued", 1L);
        if (retireBeforeCapture) sink.Retire(gpu, 100);
        var fence = sink.Capture();
        Assert.Equal(retireBeforeCapture, Resolved(fence));
        if (!retireBeforeCapture) sink.Retire(gpu, 100);
        Assert.True(Resolved(fence));
        Assert.Equal(TimeSpan.FromTicks(100), EndPts(fence));
        Assert.True(await sink.Wait(fence).WaitAsync(DeadlockGuard));
    }

    [Fact]
    public async Task LaterPreviewVideoCannotExtendTheBoundaryWhileAudioIsBehind()
    {
        using var sink = new SinkScenario();
        sink.Set("_videoFramesEnqueued", 1L);
        sink.Set("_gpuFramesEnqueued", 1L);
        sink.Set("_audioPacketsAccepted", 1L);
        sink.Set("_microphonePacketsAccepted", 1L);
        var fence = sink.Capture();
        sink.Retire(false, 100);
        sink.Retire(true, 150);
        using var cancellation = new CancellationTokenSource();
        var wait = sink.Wait(fence, cancellation.Token);
        try
        {
            // Wait executes its first predicate synchronously, before its first delay.
            Assert.False(wait.IsCompleted);
            sink.Set("_videoFramesEnqueued", 2L);
            sink.Set("_gpuFramesEnqueued", 2L);
            sink.Retire(false, 300);
            sink.Retire(true, 400);
            sink.Set("_audioPacketsAccepted", 2L);
            sink.Set("_microphonePacketsAccepted", 2L);
            sink.Set("_videoQueueDepth", 1);
            sink.Set("_gpuQueueDepth", 1);
            sink.Set("_audioQueueDepth", 1);
            sink.Set("_microphoneQueueDepth", 1);
            sink.Set("_audioPacketsRetired", 1L);
            sink.Set("_microphonePacketsRetired", 1L);
            Assert.True(await wait.WaitAsync(DeadlockGuard));
            Assert.Equal(TimeSpan.FromTicks(150), EndPts(fence));
        }
        finally
        {
            cancellation.Cancel();
            await ObserveCancellation(wait);
        }
    }

    [Theory]
    [InlineData("_videoPacketsRetired")]
    [InlineData("_gpuPacketsRetired")]
    [InlineData("_audioPacketsRetired")]
    [InlineData("_microphonePacketsRetired")]
    public async Task EveryRetirementCounterIndependentlyGatesCompletion(string laggingCounter)
    {
        using var sink = new SinkScenario();
        sink.SetAccepted(1);
        var fence = sink.Capture();
        sink.Retire(false, 100);
        sink.Retire(true, 200);
        sink.Set("_audioPacketsRetired", 1L);
        sink.Set("_microphonePacketsRetired", 1L);
        // Isolate each term in the real wait predicate with a resolved fence.
        sink.Set(laggingCounter, 0L);
        using var cancellation = new CancellationTokenSource();
        var wait = sink.Wait(fence, cancellation.Token);
        try
        {
            Assert.False(wait.IsCompleted);
            sink.Set(laggingCounter, 1L);
            Assert.True(await wait.WaitAsync(DeadlockGuard));
        }
        finally
        {
            cancellation.Cancel();
            await ObserveCancellation(wait);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RetiredCountsAloneCannotCompleteAnUnresolvedVideoBoundary(bool gpu)
    {
        using var sink = new SinkScenario();
        sink.Set(gpu ? "_gpuFramesEnqueued" : "_videoFramesEnqueued", 1L);
        var fence = sink.Capture();
        sink.Set(gpu ? "_gpuPacketsRetired" : "_videoPacketsRetired", 1L);
        using var cancellation = new CancellationTokenSource();
        var wait = sink.Wait(fence, cancellation.Token);
        try
        {
            Assert.False(wait.IsCompleted);
            ObserveRetirement(fence, gpu, 1, 100);
            Assert.True(await wait.WaitAsync(DeadlockGuard));
        }
        finally
        {
            cancellation.Cancel();
            await ObserveCancellation(wait);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedFenceAcceptsOnlyExactBoundaryAndNeverOverwritesItsFirstPts(bool gpu)
    {
        using var sink = new SinkScenario();
        sink.Set(gpu ? "_gpuFramesEnqueued" : "_videoFramesEnqueued", 2L);
        var fence = sink.Capture();
        ObserveRetirement(fence, gpu, 1, 10);
        ObserveRetirement(fence, gpu, 3, 300);
        Assert.False(Resolved(fence));
        ObserveRetirement(fence, gpu, 2, 100);
        ObserveRetirement(fence, gpu, 2, 999);
        ObserveRetirement(fence, gpu, 3, 1000);
        Assert.True(Resolved(fence));
        Assert.Equal(TimeSpan.FromTicks(100), EndPts(fence));
    }

    [Fact]
    public async Task IncompleteBoundaryHonorsCancellation()
    {
        using var sink = new SinkScenario();
        sink.Set("_audioPacketsAccepted", 1L);
        var fence = sink.Capture();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sink.Wait(fence, cancellation.Token).WaitAsync(DeadlockGuard));
        Assert.Equal(cancellation.Token, error.CancellationToken);
    }

    [Fact]
    public async Task ZeroTimeoutReturnsFalseWithoutWaiting()
    {
        using var sink = new SinkScenario();
        sink.Set("_audioPacketsAccepted", 1L);
        var fence = sink.Capture();
        Assert.False(await sink.Wait(fence, timeout: TimeSpan.Zero).WaitAsync(DeadlockGuard));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TerminalEncoderReleasesBoundaryWaitForCallerFailureInspection(bool completedTask)
    {
        using var sink = new SinkScenario();
        sink.Set("_audioPacketsAccepted", 1L);
        var fence = sink.Capture();
        if (completedTask) sink.Set("_encodingTask", Task.CompletedTask);
        else sink.Set("_encodingFailure", new InvalidOperationException("scripted encoder failure"));
        Assert.True(await sink.Wait(fence).WaitAsync(DeadlockGuard));
    }

    private static bool Resolved(object fence)
        => (bool)fence.GetType().GetProperty("HasResolvedVideoEndPts")!.GetValue(fence)!;

    private static TimeSpan EndPts(object fence)
        => (TimeSpan)fence.GetType().GetProperty("EndPts")!.GetValue(fence)!;

    private static void ObserveRetirement(object fence, bool gpu, long retired, long ticks)
        => fence.GetType().GetMethod("ObserveVideoRetirement")!.Invoke(fence, new object[] { gpu, retired, ticks });

    private static async Task ObserveCancellation(Task task)
    {
        try { await task.WaitAsync(DeadlockGuard); }
        catch (OperationCanceledException) { }
    }

    // Normal constructors initialize locks, encoder ownership, and disposal state.
    // No StartAsync/native encoder initialization or real capture worker is involved.
    private sealed class SinkScenario : IDisposable
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private readonly Type _type;
        private readonly object _sink;
        private readonly IDisposable _manager;

        public SinkScenario()
        {
            var assembly = SussudioAssembly.Load();
            var managerType = assembly.GetType("Sussudio.Services.Flashback.FlashbackBufferManager", true)!;
            _manager = (IDisposable)Activator.CreateInstance(managerType, new object?[] { null })!;
            _type = assembly.GetType("Sussudio.Services.Flashback.FlashbackEncoderSink", true)!;
            try { _sink = Activator.CreateInstance(_type, new object[] { _manager })!; }
            catch { _manager.Dispose(); throw; }
        }

        public void Set(string name, object value)
            => (_type.GetField(name, PrivateInstance) ?? throw new MissingFieldException(_type.FullName, name))
                .SetValue(_sink, value);

        public object? Get(string name)
            => (_type.GetField(name, PrivateInstance) ?? throw new MissingFieldException(_type.FullName, name))
                .GetValue(_sink);

        public void SetAccepted(long count)
        {
            Set("_videoFramesEnqueued", count);
            Set("_gpuFramesEnqueued", count);
            Set("_audioPacketsAccepted", count);
            Set("_microphonePacketsAccepted", count);
        }

        public object Capture(bool recording = true)
        {
            Set("_recordingActive", recording ? 1 : 0);
            object?[] arguments = { false };
            var fence = _type.GetMethod("CaptureRecordingBoundaryFence", PrivateInstance)!.Invoke(_sink, arguments)!;
            Assert.Equal(recording, (bool)arguments[0]!);
            return fence;
        }

        public void Retire(bool gpu, long ticks)
            => _type.GetMethod("RetireVideoPacket", PrivateInstance)!.Invoke(_sink, new object[] { gpu, ticks });

        public Task<bool> Wait(object fence, CancellationToken token = default, TimeSpan? timeout = null)
            => (Task<bool>)_type.GetMethod("WaitForRecordingBoundaryAsync", PrivateInstance)!
                .Invoke(_sink, new object[] { fence, timeout ?? TimeSpan.FromSeconds(30), token })!;

        public void Dispose()
        {
            try { ((IDisposable)_sink).Dispose(); }
            finally { _manager.Dispose(); }
        }
    }
}
