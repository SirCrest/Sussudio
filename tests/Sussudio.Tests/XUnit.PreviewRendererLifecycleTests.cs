using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Sussudio.Tests;

[Collection(nameof(PreviewRendererLifecycleCollection))]
public sealed class PreviewRendererLifecycleTests
{
    [Fact]
    public void StopBeforeDispatchLeavesQueuedFramesForTheStopOwner()
    {
        using var fixture = new RendererFixture();
        fixture.EnqueueFrame();
        fixture.Set("_stopRequested", 1);

        Assert.False(fixture.ProcessFrame());

        Assert.Equal(1, fixture.PendingCount);
        Assert.Equal(0L, fixture.Get<long>("_framesDropped"));
        Assert.Equal(0L, fixture.Get<long>("_framesRendered"));
    }

    [Fact]
    public void DeviceResetBeforeDispatchLeavesQueuedFramesForResetAccounting()
    {
        using var fixture = new RendererFixture();
        fixture.EnqueueFrame();
        fixture.Set("_sharedDeviceResetPending", 1);

        Assert.True(fixture.ProcessFrame());

        Assert.Equal(1, fixture.PendingCount);
        Assert.Equal(0L, fixture.Get<long>("_framesDropped"));
        Assert.Equal(0L, fixture.Get<long>("_framesRendered"));
    }

    [Fact]
    public void DispatchRejectsPreviousGenerationAndAccountsItsSourceOnce()
    {
        using var fixture = new RendererFixture();
        fixture.EnqueueFrame(sourceSequence: 42, presentId: 101, sourcePts: 9_000);
        fixture.Set("_submissionGeneration", 1L);
        fixture.Set("_submissionGenerationDropReason", "flashback-go-live");

        Assert.True(fixture.ProcessFrame());
        Assert.True(fixture.ProcessFrame());

        Assert.Equal(0, fixture.PendingCount);
        Assert.Equal(1L, fixture.Get<long>("_framesDropped"));
        Assert.Equal(0L, fixture.Get<long>("_framesRendered"));
        Assert.Equal(42L, fixture.Get<long>("_lastDroppedSourceSequenceNumber"));
        Assert.Equal(101L, fixture.Get<long>("_lastDroppedPreviewPresentId"));
        Assert.Equal(9_000L, fixture.Get<long>("_lastDroppedSourcePtsTicks"));
        Assert.Equal("flashback-go-live:stale", fixture.Get<string>("_lastDropReason"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RendererStopTimeoutRetainsHostOwnerAndAllowsDisposalRetry(bool nativeFenceBlocked)
    {
        using var fixture = new RendererFixture();
        fixture.PrepareForStop();
        fixture.EnqueueFrame();
        using var releaseWorker = new ManualResetEventSlim(false);
        var worker = new Thread(() => releaseWorker.Wait()) { IsBackground = true };
        worker.Start();
        fixture.Set("_renderThread", worker);
        fixture.Set("_nativeStopFenceTimeoutMs", 20);
        fixture.Set("_renderThreadStopTimeoutMs", 20);
        fixture.Set("_inNativeCall", nativeFenceBlocked ? 1 : 0);
        Action firstFrame = () => { };
        Action<string> failed = _ => { };
        fixture.Set("FirstFrameRendered", firstFrame);
        fixture.Set("RenderThreadFailed", failed);
        var hostType = SussudioAssembly.Load().GetType("Sussudio.Controllers.PreviewRendererHostController", throwOnError: true)!;
        var host = RuntimeHelpers.GetUninitializedObject(hostType);
        var ownerField = hostType.GetField("_d3d11Renderer", BindingFlags.Instance | BindingFlags.NonPublic)!;
        ownerField.SetValue(host, fixture.Instance);
        try
        {
            var failure = Assert.Throws<TargetInvocationException>(() =>
                hostType.GetMethod("CleanupPreviewResources", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(host, null));
            Assert.IsType<TimeoutException>(failure.InnerException);
            Assert.Same(fixture.Instance, ownerField.GetValue(host));
            Assert.Same(firstFrame, fixture.Get<object>("FirstFrameRendered"));
            Assert.Same(failed, fixture.Get<object>("RenderThreadFailed"));
            Assert.Same(worker, fixture.Get<Thread>("_renderThread"));
            var disposeFailure = Assert.Throws<TargetInvocationException>(() => fixture.Invoke("Dispose"));
            Assert.IsType<TimeoutException>(disposeFailure.InnerException);
            Assert.Equal(0, fixture.Get<int>("_disposed"));
            Assert.Equal(1, fixture.PendingCount);
            fixture.Get<ManualResetEventSlim>("_frameReadyEvent").Set();
        }
        finally
        {
            fixture.Set("_inNativeCall", 0);
            releaseWorker.Set();
            worker.Join(TimeSpan.FromSeconds(5));
        }

        fixture.Invoke("Dispose");
        Assert.Equal(1, fixture.Get<int>("_disposed"));
        Assert.Null(fixture.Get<Thread?>("_renderThread"));
        Assert.Equal(0, fixture.PendingCount);
        Assert.Throws<ObjectDisposedException>(() => fixture.Get<ManualResetEventSlim>("_frameReadyEvent").Wait(0));
        fixture.Invoke("Dispose");
    }

    [Fact]
    public void QueuedUnbindCanCompleteInlineBeforeJoiningItsRenderWorker()
    {
        using var fixture = new RendererFixture();
        fixture.Set("_swapChainBound", 1);
        fixture.Set("_swapChainAddress", 111L);
        var request = fixture.Invoke("GetOrCreateSwapChainUnbindRequest")!;
        var completion = GetUnbindCompletion(request);
        Exception? workerFailure = null;
        var worker = new Thread(() =>
        {
            try { completion.Task.GetAwaiter().GetResult(); }
            catch (Exception ex) { workerFailure = ex; }
        }) { IsBackground = true };
        worker.Start();
        try
        {
            Assert.Same(request, fixture.Invoke("GetOrCreateSwapChainUnbindRequest"));
            fixture.AcknowledgeUnbind(request);
            Assert.True(worker.Join(TimeSpan.FromSeconds(1)));
            Assert.Null(workerFailure);
            Assert.True(completion.Task.IsCompletedSuccessfully);
            Assert.Equal(0, fixture.Get<int>("_swapChainBound"));
            Assert.Equal(0L, fixture.Get<long>("_swapChainAddress"));

            // The previously queued callback arrives after a newer binding.
            fixture.Set("_swapChainBound", 1);
            fixture.Set("_swapChainAddress", 222L);
            fixture.AcknowledgeUnbind(request);
            Assert.Equal(1, fixture.Get<int>("_swapChainBound"));
            Assert.Equal(222L, fixture.Get<long>("_swapChainAddress"));
        }
        finally
        {
            completion.TrySetResult(null);
            worker.Join(TimeSpan.FromSeconds(5));
        }
    }

    [Fact]
    public async Task UnbindTimeoutRetainsRequestAndStaleCallbackCannotDetachAnotherChain()
    {
        using var fixture = new RendererFixture();
        fixture.Set("_swapChainBound", 1);
        fixture.Set("_swapChainAddress", 111L);
        var request = fixture.Invoke("GetOrCreateSwapChainUnbindRequest")!;
        var completion = GetUnbindCompletion(request);
        await Assert.ThrowsAsync<TimeoutException>(() => completion.Task.WaitAsync(TimeSpan.FromMilliseconds(20)));
        Assert.False(completion.Task.IsCompleted);
        Assert.Same(request, fixture.Invoke("GetOrCreateSwapChainUnbindRequest"));
        Assert.Equal(1, fixture.Get<int>("_swapChainBound"));

        fixture.Set("_swapChain", fixture.CreateUninitializedFieldValue("_swapChain"));
        fixture.Set("_swapChainAddress", 222L);
        var replacement = fixture.Invoke("GetOrCreateSwapChainUnbindRequest")!;
        Assert.NotSame(request, replacement);
        fixture.AcknowledgeUnbind(request);
        await Assert.ThrowsAsync<InvalidOperationException>(() => completion.Task);
        Assert.Equal(1, fixture.Get<int>("_swapChainBound"));
        Assert.Equal(222L, fixture.Get<long>("_swapChainAddress"));

        fixture.AcknowledgeUnbind(replacement);
        Assert.True(GetUnbindCompletion(replacement).Task.IsCompletedSuccessfully);
        Assert.Equal(0, fixture.Get<int>("_swapChainBound"));
        fixture.Set("_swapChain", null);
    }

    [Fact]
    public async Task UnavailablePanelRetainsBoundSwapChainUntilAnAcknowledgedRetry()
    {
        using var fixture = new RendererFixture();
        fixture.Set("_swapChainBound", 1);
        fixture.Set("_swapChainAddress", 111L);
        var request = fixture.Invoke("GetOrCreateSwapChainUnbindRequest")!;

        fixture.Invoke("ExecuteSwapChainUnbindOnUiThread", request);
        await Assert.ThrowsAsync<InvalidOperationException>(() => GetUnbindCompletion(request).Task);
        Assert.Equal(1, fixture.Get<int>("_swapChainBound"));
        Assert.Equal(111L, fixture.Get<long>("_swapChainAddress"));
        Assert.Same(request, fixture.Get<object>("_pendingSwapChainUnbind"));

        var retry = fixture.Invoke("GetOrCreateSwapChainUnbindRequest")!;
        Assert.NotSame(request, retry);
        fixture.AcknowledgeUnbind(retry);
        Assert.True(GetUnbindCompletion(retry).Task.IsCompletedSuccessfully);
        Assert.Equal(0, fixture.Get<int>("_swapChainBound"));
    }

    [Fact]
    public void FailedRenderThreadCleanupRetainsNativeHandleUntilStopRetriesAfterUnbind()
    {
        using var fixture = new RendererFixture();
        fixture.PrepareForStop();
        fixture.PrepareForResourceCleanup();
        var nativeHandle = fixture.InstallOwnedLatencyEvent();
        fixture.Set("_swapChainBound", 1);
        // The offline renderer has no dispatcher. Exercise the real exit cleanup
        // failure path without entering WinUI or a graphics-driver call.
        fixture.Invoke("CleanupRenderThreadExit");
        Assert.Equal(1, fixture.Get<int>("_renderThreadCleanupPending"));
        Assert.Equal(1, fixture.Get<int>("_swapChainBound"));
        Assert.True(GetHandleInformation(nativeHandle, out _));

        var request = fixture.Get<object>("_pendingSwapChainUnbind");
        fixture.AcknowledgeUnbind(request);
        fixture.Invoke("Dispose");
        Assert.Equal(0, fixture.Get<int>("_renderThreadCleanupPending"));
        Assert.Equal(1, fixture.Get<int>("_disposed"));
        Assert.Null(fixture.Get<Thread?>("_renderThread"));
        Assert.False(GetHandleInformation(nativeHandle, out _));
    }

    [Fact]
    public void UnbindAcknowledgementPrecedesJoinAndResourceRelease()
    {
        var stop = ReadMember("D3D11PreviewRenderer.cs", "public void Stop()");
        AssertBefore(stop, "UnbindSwapChainFromPanel();", "renderThread.Join(");
        AssertBefore(stop, "renderThread.Join(", "if (Volatile.Read(ref _renderThreadCleanupPending)");
        AssertBefore(stop, "Volatile.Write(ref _renderThreadCleanupPending, 0);", "_renderThread = null;");
        var cleanup = ReadMember("D3D11PreviewRenderer.Resources.cs", "private void CleanupD3DResources()");
        AssertBefore(cleanup, "UnbindSwapChainFromPanel();", "DisposeProcessorResources();");
        var uiUnbind = ReadMember("D3D11PreviewRenderer.cs", "private void ExecuteSwapChainUnbindOnUiThread(");
        AssertBefore(uiUnbind, "if (_panel?.XamlRoot == null)", "panelNative.SetSwapChain(IntPtr.Zero);");
        Assert.Contains("throw new InvalidOperationException", uiUnbind);
        var acknowledge = ReadMember("D3D11PreviewRenderer.cs", "private void ExecuteSwapChainUnbindRequest(");
        AssertBefore(acknowledge, "unbind();", "Volatile.Write(ref _swapChainBound, 0);");
        AssertBefore(acknowledge, "Volatile.Write(ref _swapChainBound, 0);", "request.Completion.TrySetResult(null);");
        var reset = ReadMember("D3D11PreviewRenderer.cs", "private void HandlePendingSharedDeviceResetOnRenderThread()");
        AssertBefore(reset, "if (Volatile.Read(ref _stopRequested)", "InitializeD3D();");
    }

    private static TaskCompletionSource<object?> GetUnbindCompletion(object request)
        => (TaskCompletionSource<object?>)request.GetType().GetProperty("Completion")!.GetValue(request)!;

    [Fact]
    public void DisposingLatencyHandleClosesTheNativeHandleExactlyOnce()
    {
        using var fixture = new RendererFixture();
        var nativeHandle = fixture.InstallOwnedLatencyEvent();
        Assert.True(GetHandleInformation(nativeHandle, out _));

        fixture.Invoke("DisposeFrameLatencyWaitHandle");
        fixture.Invoke("DisposeFrameLatencyWaitHandle");

        Assert.Null(fixture.Get<object?>("_frameLatencyWaitHandle"));
        var isOpen = GetHandleInformation(nativeHandle, out _);
        var error = Marshal.GetLastPInvokeError();
        Assert.False(isOpen);
        Assert.Equal(6, error); // ERROR_INVALID_HANDLE
    }

    [Fact]
    public void ReconfiguringWithoutWaitableSupportReleasesThePreviousHandle()
    {
        using var fixture = new RendererFixture();
        var nativeHandle = fixture.InstallOwnedLatencyEvent();

        fixture.Invoke("ConfigureFrameLatencyWaitableObject");

        Assert.Null(fixture.Get<object?>("_frameLatencyWaitHandle"));
        var isOpen = GetHandleInformation(nativeHandle, out _);
        var error = Marshal.GetLastPInvokeError();
        Assert.False(isOpen);
        Assert.Equal(6, error);
    }

    [Fact]
    public void FailedEvictionRemovesOldEntryAndLeavesCandidateWithCaller()
    {
        var cache = CreateCache();
        var victim = new LifetimeProbe(throwOnDispose: true);
        var candidate = new LifetimeProbe();
        CacheCall(cache, "Add", new IntPtr(1), 0, victim);
        for (var index = 2; index <= 8; index++)
        {
            CacheCall(cache, "Add", new IntPtr(index), 0, new LifetimeProbe());
        }

        var failure = Assert.Throws<TargetInvocationException>(() =>
            CacheCall(cache, "Add", new IntPtr(9), 0, candidate));

        Assert.IsType<InvalidOperationException>(failure.InnerException);
        Assert.Equal(1, victim.DisposeCount);
        Assert.Equal(0, candidate.DisposeCount);
        Assert.Equal(7, cache.GetType().GetProperty("Count")!.GetValue(cache));
        var lookup = new object?[] { new IntPtr(1), 0, null };
        Assert.False((bool)CacheCall(cache, "TryGet", lookup)!);
        lookup[0] = new IntPtr(9);
        Assert.False((bool)CacheCall(cache, "TryGet", lookup)!);

        candidate.Dispose();
        CacheCall(cache, "Clear");
        Assert.Equal(1, candidate.DisposeCount);
        Assert.Equal(1, victim.DisposeCount);
    }

    [Fact]
    public void FailedClearStillReleasesEveryEntryAndRemainsIdempotent()
    {
        var cache = CreateCache();
        var resources = Enumerable.Range(1, 8)
            .Select(index => new LifetimeProbe(throwOnDispose: index == 1 || index == 4)).ToArray();
        for (var index = 0; index < resources.Length; index++)
        {
            CacheCall(cache, "Add", new IntPtr(index + 1), 0, resources[index]);
        }

        var failure = Assert.Throws<TargetInvocationException>(() => CacheCall(cache, "Clear"));

        Assert.IsType<InvalidOperationException>(failure.InnerException);
        Assert.All(resources, resource => Assert.Equal(1, resource.DisposeCount));
        Assert.Equal(0, cache.GetType().GetProperty("Count")!.GetValue(cache));
        CacheCall(cache, "Clear");
        Assert.All(resources, resource => Assert.Equal(1, resource.DisposeCount));
    }

    [Fact]
    public void DisplayReadinessPrecedesSelectionAndRechecksLifecycleState()
    {
        var dispatch = ReadMember("D3D11PreviewRenderer.cs", "private bool ProcessRenderThreadFrameOrIdle()");
        var wait = dispatch.IndexOf("WaitForFrameLatencySignal();", StringComparison.Ordinal);
        var dequeue = dispatch.IndexOf("TryDequeuePendingFrame(out var frame)", StringComparison.Ordinal);
        AssertBefore(dispatch, "TryResizeOutputForPendingFrame();", "WaitForFrameLatencySignal();");
        Assert.True(wait >= 0 && dequeue > wait);
        var afterWaitBeforeDequeue = dispatch[wait..dequeue];
        Assert.Contains("_stopRequested", afterWaitBeforeDequeue);
        Assert.Contains("_sharedDeviceResetPending", afterWaitBeforeDequeue);
        AssertBefore(dispatch, "frame.SubmissionGeneration", "RenderFrame(frame);");
    }

    [Fact]
    public void SwapChainReplacementClosesLatencyHandleWhileResizePreservesIt()
    {
        var resize = ReadMember("D3D11PreviewRenderer.Resources.cs", "private void ResizeCompositionSwapChain(");
        Assert.DoesNotContain("DisposeFrameLatencyWaitHandle", resize);
        Assert.DoesNotContain("ConfigureFrameLatencyWaitableObject", resize);
        var replacement = ReadMember("D3D11PreviewRenderer.Resources.cs", "private void RecreateSdrCompositionSwapChain(");
        AssertBefore(replacement, "DisposeFrameLatencyWaitHandle();", "_swapChain!.Dispose();");
        var cleanup = ReadMember("D3D11PreviewRenderer.Resources.cs", "private void CleanupD3DResources()");
        AssertBefore(cleanup, "DisposeFrameLatencyWaitHandle();", "_swapChain?.Dispose();");
    }

    [Fact]
    public void NativeInputCacheReleasesBeforeProcessorAndDeviceOwners()
    {
        var processor = ReadMember("D3D11PreviewRenderer.Resources.cs", "private void DisposeProcessorResources()");
        AssertBefore(processor, "ClearExternalInputViewCache();", "_videoProcessorEnumerator?.Dispose();");
        var cleanup = ReadMember("D3D11PreviewRenderer.Resources.cs", "private void CleanupD3DResources()");
        AssertBefore(cleanup, "DisposeProcessorResources();", "_device?.Dispose();");
        var creation = ReadMember("D3D11PreviewRenderer.RenderPasses.cs", "private ID3D11VideoProcessorInputView ResolveExternalInputView(");
        var failure = creation[creation.IndexOf("catch", StringComparison.Ordinal)..];
        Assert.Contains("view.Dispose();", failure);
        Assert.Contains("textureOwner.Dispose();", failure);
        Assert.Contains("Marshal.Release(texturePointer);", failure);
    }

    private static object CreateCache()
        => Activator.CreateInstance(SussudioAssembly.Load().GetType(
            "Sussudio.Services.Preview.PreviewInputViewCache`1", throwOnError: true)!
            .MakeGenericType(typeof(LifetimeProbe)))!;

    private static object? CacheCall(object cache, string method, params object?[] arguments)
        => cache.GetType().GetMethod(method)!.Invoke(cache, arguments);

    private sealed class LifetimeProbe(bool throwOnDispose = false) : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;
            if (throwOnDispose) { throw new InvalidOperationException("Synthetic disposal failure."); }
        }
    }

    // Native objects cannot be exercised by the offline fixture. Keep these few
    // ownership/order assertions alongside behavioral lifecycle coverage.
    private static string ReadMember(string fileName, string declaration)
    {
        var root = new DirectoryInfo(Environment.CurrentDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, ".git")) &&
               !File.Exists(Path.Combine(root.FullName, ".git")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        var source = File.ReadAllText(Path.Combine(root!.FullName, "Sussudio", "Services", "Preview", fileName));
        return global::Program.ExtractDeclaredMemberCode(source, declaration);
    }

    private static void AssertBefore(string source, string earlier, string later)
    {
        var first = source.IndexOf(earlier, StringComparison.Ordinal);
        var second = source.IndexOf(later, StringComparison.Ordinal);
        Assert.True(first >= 0 && second > first, $"Expected '{earlier}' before '{later}'.");
    }

    // A renderer fixture intentionally has no swap chain, panel, or GPU device.
    // These tests run the real queue and native-handle owners without entering
    // WinUI or graphics-driver calls.
    private sealed class RendererFixture : IDisposable
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly Type _rendererType = SussudioAssembly.Load().GetType(
            "Sussudio.Services.Preview.D3D11PreviewRenderer", throwOnError: true)!;
        private readonly object _renderer;
        private readonly Type _frameType;
        private readonly object _queue;
        private readonly ManualResetEventSlim _frameReady = new(false);

        public RendererFixture()
        {
            _renderer = RuntimeHelpers.GetUninitializedObject(_rendererType);
            _frameType = _rendererType.GetNestedType("PendingFrame", BindingFlags.NonPublic)!;
            _queue = Activator.CreateInstance(typeof(ConcurrentQueue<>).MakeGenericType(_frameType))!;
            Set("_lifecycleLock", new object());
            Set("_frameReadyEvent", _frameReady);
            Set("_pendingFrames", _queue);
            Set("_renderThread", Thread.CurrentThread);
            Set("_maxPendingFrames", 4);
            Set("_configuredOutputWidth", 1);
            Set("_configuredOutputHeight", 1);
            Set("_requestedOutputWidth", 1);
            Set("_requestedOutputHeight", 1);
        }

        public object Instance => _renderer;
        public int PendingCount => Get<int>("_pendingFrameCount");

        public void PrepareForStop()
        {
            foreach (var name in new[] { "_presentCadenceLock", "_pipelineLatencyLock", "_renderCpuTimingLock", "_frameLatencyWaitTimingLock", "_slowFrameDiagnosticsLock" })
            {
                Set(name, new object());
            }

            foreach (var name in new[] { "_presentIntervalWindowMs", "_pipelineLatencyWindowMs", "_inputUploadCpuTimingWindowMs", "_renderSubmitCpuTimingWindowMs", "_presentCallTimingWindowMs", "_renderTotalCpuTimingWindowMs", "_frameLatencyWaitTimingWindowMs", "_slowFrameDiagnostics" })
            {
                Set(name, Array.CreateInstance(_rendererType.GetField(name, InstanceFlags)!.FieldType.GetElementType()!, 1));
            }

            var historyType = _rendererType.GetField("_presentFrameTimeHistory", InstanceFlags)!.FieldType;
            Set("_presentFrameTimeHistory", Activator.CreateInstance(historyType, new object?[] { 1, null }));
        }

        // Exercise the actual request owner with an explicit successful UI action;
        // native panel calls require separate validation on an available WinUI surface.
        public void AcknowledgeUnbind(object request)
            => Invoke("ExecuteSwapChainUnbindRequest", request, (Action)(() => { }));

        public object CreateUninitializedFieldValue(string name)
            => RuntimeHelpers.GetUninitializedObject(_rendererType.GetField(name, InstanceFlags)!.FieldType);

        public void PrepareForResourceCleanup()
        {
            foreach (var field in _rendererType.GetFields(InstanceFlags).Where(field => field.FieldType.IsArray && field.GetValue(_renderer) == null))
            {
                field.SetValue(_renderer, Array.CreateInstance(field.FieldType.GetElementType()!, 0));
            }

            var cacheType = _rendererType.GetField("_externalInputViews", InstanceFlags)!.FieldType;
            Set("_externalInputViews", Activator.CreateInstance(cacheType));
        }

        public void EnqueueFrame(long sourceSequence = 1, long presentId = 1, long sourcePts = 1)
        {
            var constructor = _frameType.GetConstructors(InstanceFlags)
                .Single(candidate => candidate.GetParameters().Any(parameter => parameter.Name == "rawData"));
            var values = constructor.GetParameters().Select(parameter => parameter.Name switch
            {
                "width" or "height" => (object)16,
                "arrivalTick" => Stopwatch.GetTimestamp(),
                "sourceSequenceNumber" => sourceSequence,
                "previewPresentId" => presentId,
                "sourcePtsTicks" => sourcePts,
                _ => parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null,
            }).ToArray();
            Invoke("EnqueuePendingFrame", constructor.Invoke(values));
        }

        public bool ProcessFrame() => (bool)Invoke("ProcessRenderThreadFrameOrIdle")!;

        public IntPtr InstallOwnedLatencyEvent()
        {
            using var signal = new EventWaitHandle(false, EventResetMode.AutoReset);
            var nativeHandle = signal.SafeWaitHandle.DangerousGetHandle();
            var owner = new SafeWaitHandle(nativeHandle, ownsHandle: true);
            signal.SafeWaitHandle.SetHandleAsInvalid();
            Set("_frameLatencyWaitHandle", owner);
            return nativeHandle;
        }

        public void Set(string name, object? value)
            => (_rendererType.GetField(name, InstanceFlags)
                ?? throw new MissingFieldException(_rendererType.FullName, name)).SetValue(_renderer, value);

        public T Get<T>(string name)
            => (T)(_rendererType.GetField(name, InstanceFlags)
                ?? throw new MissingFieldException(_rendererType.FullName, name)).GetValue(_renderer)!;

        public object? Invoke(string name, params object?[] values)
            => (_rendererType.GetMethod(name, InstanceFlags)
                ?? throw new MissingMethodException(_rendererType.FullName, name)).Invoke(_renderer, values);

        public void Dispose()
        {
            Invoke("DisposeFrameLatencyWaitHandle");
            var dequeue = _queue.GetType().GetMethod("TryDequeue")!;
            var arguments = new object?[] { null };
            while ((bool)dequeue.Invoke(_queue, arguments)!)
            {
                ((IDisposable)arguments[0]!).Dispose();
            }
            _frameReady.Dispose();
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetHandleInformation(IntPtr handle, out uint flags);
}

// Native handle values can be reused as soon as they are closed. Run this small
// collection alone so another test cannot allocate the just-released value
// between disposal and the native ownership check.
[CollectionDefinition(nameof(PreviewRendererLifecycleCollection), DisableParallelization = true)]
public sealed class PreviewRendererLifecycleCollection
{
}
