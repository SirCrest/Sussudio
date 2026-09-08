using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Sussudio.Tests;

[Collection(nameof(PreviewRendererLifecycleCollection))]
public sealed class CaptureCleanupBarrierTests
{
    private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [Fact]
    public async Task CleanupWaitsForEveryRendererAcknowledgementBeforeDisposingCapture()
    {
        await using var fixture = new CaptureFixture();
        var entered = NewCompletion();
        var release = NewCompletion();
        var secondHandlerCalls = 0;
        fixture.AddCleanupHandler(_ => { entered.TrySetResult(); return release.Task; });
        fixture.AddCleanupHandler(_ => { Interlocked.Increment(ref secondHandlerCalls); return Task.CompletedTask; });

        var cleanup = fixture.CleanupAsync();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.False(cleanup.IsCompleted);
        Assert.False(fixture.ManagerDisposed);
        Assert.Same(fixture.Capture, fixture.OwnedCapture);
        Assert.Equal(0, Volatile.Read(ref secondHandlerCalls));

        release.TrySetResult();
        await cleanup.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, secondHandlerCalls);
        Assert.True(fixture.ManagerDisposed);
        Assert.Null(fixture.OwnedCapture);
    }

    [Fact]
    public async Task FatalCleanupFailurePublishesOriginalErrorAndRetainsOwnersUntilExplicitStopRetry()
    {
        await using var fixture = new CaptureFixture();
        var original = new InvalidOperationException("Synthetic capture failure.");
        var observed = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var failStop = true;
        fixture.AddCleanupHandler(_ => failStop
            ? Task.FromException(new TimeoutException("Synthetic renderer stop timeout."))
            : Task.CompletedTask);
        fixture.Service.GetType().GetEvent("StatusChanged")!.AddEventHandler(fixture.Service,
            (EventHandler<string>)((_, _) => throw new InvalidOperationException("Synthetic status observer failure.")));
        fixture.Service.GetType().GetEvent("ErrorOccurred")!.AddEventHandler(fixture.Service,
            (EventHandler<Exception>)((_, error) => observed.TrySetResult(error)));

        Invoke(fixture.Service, "OnUnifiedVideoCaptureFatalError", null, original);

        Assert.Same(original, await observed.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.False(fixture.ManagerDisposed);
        Assert.Same(fixture.Capture, fixture.OwnedCapture);
        Assert.Equal("Faulted", fixture.Service.GetType().GetProperty("SessionState")!.GetValue(fixture.Service)!.ToString());
        var settings = RuntimeHelpers.GetUninitializedObject(AppType("Sussudio.Models.CaptureSettings"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            InvokeTask(fixture.Service, "StartVideoPreviewAsync", settings, CancellationToken.None));
        Assert.False(fixture.ManagerDisposed);
        var renderer = RuntimeHelpers.GetUninitializedObject(AppType("Sussudio.Services.Preview.D3D11PreviewRenderer"));
        var attachFailure = Assert.Throws<TargetInvocationException>(() => Invoke(fixture.Service, "SetPreviewFrameSink", renderer));
        Assert.IsType<InvalidOperationException>(attachFailure.InnerException);

        failStop = false;
        await InvokeTask(fixture.Service, "StopVideoPreviewWithTeardownAsync", CancellationToken.None);
        Assert.True(fixture.ManagerDisposed);
        Assert.Null(fixture.OwnedCapture);
        Assert.Equal(0, GetField<int>(fixture.Service, "_cleanupRequired"));
    }

    [Fact]
    public async Task MissingRendererAcknowledgementRejectsCleanupWhileD3DSinkRemainsAttached()
    {
        await using var fixture = new CaptureFixture();
        var renderer = RuntimeHelpers.GetUninitializedObject(AppType("Sussudio.Services.Preview.D3D11PreviewRenderer"));
        fixture.SetPreviewSink(renderer);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.CleanupAsync());

        Assert.False(fixture.ManagerDisposed);
        Assert.Same(fixture.Capture, fixture.OwnedCapture);
        Assert.Equal(1, GetField<int>(fixture.Service, "_cleanupRequired"));
        fixture.SetPreviewSink(null);
        await fixture.CleanupAsync();
        Assert.True(fixture.ManagerDisposed);
    }

    [Fact]
    public async Task CaptureDisposalFailureRetainsLocksAndRetriesOnlyAfterAttemptCompletes()
    {
        await using var fixture = new CaptureFixture();
        var failStop = true;
        var entered = NewCompletion();
        var release = NewCompletion();
        fixture.AddCleanupHandler(_ =>
        {
            if (failStop) return Task.FromException(new TimeoutException("Synthetic renderer stop timeout."));
            entered.TrySetResult();
            return release.Task;
        });

        await Assert.ThrowsAsync<TimeoutException>(() => fixture.DisposeServiceAsync());
        Assert.Equal(0, GetField<int>(fixture.Service, "_isDisposed"));
        Assert.Equal(1, GetField<SemaphoreSlim>(fixture.Service, "_sessionTransitionLock").CurrentCount);
        Assert.False(fixture.ManagerDisposed);

        failStop = false;
        var firstRetry = fixture.DisposeServiceAsync();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var secondRetry = fixture.DisposeServiceAsync();
        Assert.Same(firstRetry, secondRetry);
        Assert.False(fixture.ManagerDisposed);
        release.TrySetResult();
        await Task.WhenAll(firstRetry, secondRetry).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(fixture.ManagerDisposed);
        Assert.Equal(1, GetField<int>(fixture.Service, "_isDisposed"));
    }

    [Fact]
    public async Task SynchronousCaptureDisposalTimeoutKeepsTheSameInFlightCleanupTask()
    {
        await using var fixture = new CaptureFixture();
        var entered = NewCompletion();
        var release = NewCompletion();
        var calls = 0;
        fixture.AddCleanupHandler(_ =>
        {
            Interlocked.Increment(ref calls);
            entered.TrySetResult();
            return release.Task;
        });
        var previous = Environment.GetEnvironmentVariable("SUSSUDIO_CAPTURE_SERVICE_DISPOSE_TIMEOUT_MS");
        try
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_CAPTURE_SERVICE_DISPOSE_TIMEOUT_MS", "1000");
            var caller = Task.Run(() => ((IDisposable)fixture.Service).Dispose());
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var ownedTask = GetField<Task>(fixture.Service, "_disposalTask");
            await Assert.ThrowsAsync<TimeoutException>(() => caller);
            Assert.False(ownedTask.IsCompleted);
            Assert.False(fixture.ManagerDisposed);
            var nextCaller = fixture.DisposeServiceAsync();
            Assert.Same(ownedTask, nextCaller);
            Assert.Equal(1, calls);
            release.TrySetResult();
            await nextCaller.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(fixture.ManagerDisposed);
        }
        finally
        {
            release.TrySetResult();
            Environment.SetEnvironmentVariable("SUSSUDIO_CAPTURE_SERVICE_DISPOSE_TIMEOUT_MS", previous);
        }
    }

    [Fact]
    public async Task EventIngressForwardsAdmissionTokenAndActualRendererStopFailure()
    {
        var stop = NewCompletion();
        using var admission = new CancellationTokenSource();
        var calls = 0;
        Func<Task> notify = () => { calls++; return stop.Task; };
        var controller = CreateController("MainViewModelRuntimeEventIngressController", new()
        {
            ["NotifyRendererStopAsync"] = notify,
            ["InvokeOnUiThreadAsync"] = (Func<Func<Task>, CancellationToken, Task>)((operation, token) =>
            {
                Assert.Same(notify, operation);
                Assert.Equal(admission.Token, token);
                return operation();
            }),
            ["EnqueueUiOperation"] = (Func<Func<Task>, string, bool>)((_, _) => throw new InvalidOperationException("Cleanup must bypass the operation queue.")),
        });

        var handoff = InvokeTask(controller, "OnCapturePreCleanupRequested", admission.Token);
        Assert.Same(stop.Task, handoff);
        Assert.Equal(1, calls);
        Assert.False(handoff.IsCompleted);
        stop.TrySetException(new TimeoutException("Synthetic native fence timeout."));
        await Assert.ThrowsAsync<TimeoutException>(() => handoff);
    }

    [Fact]
    public void NotificationDetachKeepsCleanupHandoffUntilDisposalCompletes()
    {
        var cleanupDetachCalls = 0;
        var controller = CreateController("MainViewModelRuntimeEventIngressController", new()
        {
            ["DetachCapturePreCleanupRequested"] = (Action<Func<CancellationToken, Task>>)(_ => cleanupDetachCalls++),
        });

        Invoke(controller, "Detach");
        Assert.Equal(0, cleanupDetachCalls);
        Invoke(controller, "DetachCleanupHandoff");
        Assert.Equal(1, cleanupDetachCalls);
    }

    [Fact]
    public async Task ViewModelCallerTimeoutRetainsOneDisposalOperationAndItsCleanupHandoff()
    {
        var captureEntered = NewCompletion();
        var captureRelease = NewCompletion();
        var begun = 0;
        var stopped = 0;
        var captureCalls = 0;
        var completed = 0;
        var waitCalls = 0;
        var controller = CreateController("MainViewModelDisposalController", new()
        {
            ["TryBeginDispose"] = (Func<bool>)(() => Interlocked.Exchange(ref begun, 1) == 0),
            ["StopRuntimeForDispose"] = (Action)(() => Interlocked.Increment(ref stopped)),
            ["DisposeCaptureServiceAsync"] = (Func<Task>)(() =>
            {
                Interlocked.Increment(ref captureCalls);
                captureEntered.TrySetResult();
                return captureRelease.Task;
            }),
            ["CompleteRuntimeDispose"] = (Action)(() => Interlocked.Increment(ref completed)),
            ["AwaitWithTimeoutAsync"] = (Func<Task, int, string, Task>)((task, _, _) =>
                Interlocked.Increment(ref waitCalls) == 1
                    ? Task.FromException(new TimeoutException("Synthetic caller deadline."))
                    : task),
        });

        var timeout = Assert.Throws<TargetInvocationException>(() => Invoke(controller, "Dispose"));
        Assert.IsType<TimeoutException>(timeout.InnerException);
        await captureEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var ownedTask = GetField<Task>(controller, "_disposalTask");
        var retry = InvokeDisposeAsync(controller);
        Assert.Same(ownedTask, GetField<Task>(controller, "_disposalTask"));
        Assert.False(retry.IsCompleted);
        Assert.Equal(1, captureCalls);
        Assert.Equal(1, stopped);
        Assert.Equal(0, completed);

        captureRelease.TrySetResult();
        await retry.WaitAsync(TimeSpan.FromSeconds(5));
        await InvokeDisposeAsync(controller);
        Assert.Equal(1, captureCalls);
        Assert.Equal(1, completed);
    }

    [Fact]
    public async Task ViewModelDisposalRetriesCompletedCaptureFailureWithoutStoppingRuntimeTwice()
    {
        var begun = 0;
        var stopped = 0;
        var captureCalls = 0;
        var completed = 0;
        var controller = CreateController("MainViewModelDisposalController", new()
        {
            ["TryBeginDispose"] = (Func<bool>)(() => Interlocked.Exchange(ref begun, 1) == 0),
            ["StopRuntimeForDispose"] = (Action)(() => stopped++),
            ["DisposeCaptureServiceAsync"] = (Func<Task>)(() => ++captureCalls == 1
                ? Task.FromException(new TimeoutException("Synthetic capture cleanup failure."))
                : Task.CompletedTask),
            ["CompleteRuntimeDispose"] = (Action)(() => completed++),
            ["AwaitWithTimeoutAsync"] = (Func<Task, int, string, Task>)((task, _, _) => task),
        });

        await Assert.ThrowsAsync<TimeoutException>(() => InvokeDisposeAsync(controller));
        Assert.Equal(0, completed);
        await InvokeDisposeAsync(controller);
        Assert.Equal(2, captureCalls);
        Assert.Equal(1, stopped);
        Assert.Equal(1, completed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CloseAdmissionFailureCompletesRequestAndAllowsRetry(bool timeout)
    {
        var lifecycle = Activator.CreateInstance(AppType("Sussudio.Controllers.WindowCloseLifecycleController"))!;
        var failure = timeout ? (Exception)new TimeoutException("Synthetic renderer stop timeout.")
            : new InvalidOperationException("Synthetic UI admission rejection.");
        var fail = true;
        var cancelled = false;
        var requested = 0;
        var status = string.Empty;
        var controller = CreateController("WindowAppClosingController", new()
        {
            ["LifecycleController"] = lifecycle,
            ["GetStatusText"] = (Func<string>)(() => status),
            ["SetStatusText"] = (Action<string>)(value => status = value),
            ["PrepareForCloseAsync"] = (Func<ValueTask>)(() => fail
                ? new ValueTask(Task.FromException(failure)) : ValueTask.CompletedTask),
            ["RequestWindowClose"] = (Action)(() => requested++),
        });
        var firstRequest = InvokeTask(lifecycle, "GetCompletionTask", CancellationToken.None);
        Invoke(lifecycle, "TryMarkRequested");

        await InvokeTask(controller, "HandleClosingCoreAsync", (Action)(() => cancelled = true));

        var observed = await Record.ExceptionAsync(() => firstRequest);
        Assert.Same(failure, observed);
        Assert.True(cancelled);
        Assert.Equal(0, requested);
        Assert.Contains("Close again to retry", status);
        Assert.False((bool)lifecycle.GetType().GetProperty("IsRecordingStopInProgress")!.GetValue(lifecycle)!);
        Assert.False((bool)lifecycle.GetType().GetProperty("IsAllowedAfterRecordingStop")!.GetValue(lifecycle)!);
        Assert.True((bool)Invoke(lifecycle, "TryMarkRequested")!);

        fail = false;
        var retry = InvokeTask(lifecycle, "GetCompletionTask", CancellationToken.None);
        await InvokeTask(controller, "HandleClosingCoreAsync", (Action)(() => cancelled = true));
        await retry.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(1, requested);
        Assert.True((bool)lifecycle.GetType().GetProperty("IsAllowedAfterRecordingStop")!.GetValue(lifecycle)!);
    }

    private static Type AppType(string name) => SussudioAssembly.Load().GetType(name, throwOnError: true)!;
    private static TaskCompletionSource NewCompletion() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static object? Invoke(object owner, string name, params object?[] arguments)
        => owner.GetType().GetMethod(name, InstanceFlags)!.Invoke(owner, arguments);
    private static Task InvokeTask(object owner, string name, params object?[] arguments)
        => (Task)Invoke(owner, name, arguments)!;
    private static Task InvokeDisposeAsync(object owner) => ((ValueTask)Invoke(owner, "DisposeAsync")!).AsTask();
    private static T GetField<T>(object owner, string name) => (T)owner.GetType().GetField(name, InstanceFlags)!.GetValue(owner)!;
    private static void SetField(object owner, string name, object? value) => owner.GetType().GetField(name, InstanceFlags)!.SetValue(owner, value);

    private static object CreateController(string name, Dictionary<string, object> overrides)
    {
        var contextType = AppType($"Sussudio.Controllers.{name}Context");
        var context = Activator.CreateInstance(contextType)!;
        foreach (var property in contextType.GetProperties())
        {
            if (overrides.TryGetValue(property.Name, out var value))
            {
                property.SetValue(context, value);
                continue;
            }

            var invoke = property.PropertyType.GetMethod("Invoke")!;
            var parameters = invoke.GetParameters().Select(parameter => Expression.Parameter(parameter.ParameterType)).ToArray();
            Expression body = invoke.ReturnType == typeof(void) ? Expression.Empty()
                : invoke.ReturnType == typeof(Task) ? Expression.Constant(Task.CompletedTask, typeof(Task))
                : Expression.Default(invoke.ReturnType);
            property.SetValue(context, Expression.Lambda(property.PropertyType, body, parameters).Compile());
        }

        return Activator.CreateInstance(AppType($"Sussudio.Controllers.{name}"), context)!;
    }

    // Real capture/service cleanup methods run against an inert shared manager.
    // No source reader, GPU device, audio device, or recording sink is created.
    private sealed class CaptureFixture : IAsyncDisposable
    {
        private readonly object _pipeline;
        private readonly object _manager;
        public object Service { get; } = Activator.CreateInstance(AppType("Sussudio.Services.Capture.CaptureService"))!;
        public object Capture { get; } = Activator.CreateInstance(AppType("Sussudio.Services.Capture.UnifiedVideoCapture"), nonPublic: true)!;
        public object? OwnedCapture => _pipeline.GetType().GetProperty("Capture")!.GetValue(_pipeline);
        public bool ManagerDisposed => GetField<int>(_manager, "_disposed") != 0;

        public CaptureFixture()
        {
            _manager = RuntimeHelpers.GetUninitializedObject(AppType("Sussudio.Services.Preview.SharedD3DDeviceManager"));
            SetField(_manager, "_sync", new object());
            SetField(Capture, "_d3dManager", _manager);
            _pipeline = GetField<object>(Service, "_videoPipeline");
            _pipeline.GetType().GetProperty("Capture")!.SetValue(_pipeline, Capture);
            SetField(Service, "_isInitialized", true);
            SetField(Service, "_isVideoPreviewActive", true);
        }

        public void AddCleanupHandler(Func<CancellationToken, Task> handler)
            => Service.GetType().GetEvent("PreCleanupRequested")!.AddEventHandler(Service, handler);
        public void SetPreviewSink(object? sink) => _pipeline.GetType().GetProperty("PreviewFrameSink")!.SetValue(_pipeline, sink);
        public Task CleanupAsync() => InvokeTask(Service, "CleanupAsync", CancellationToken.None);
        public Task DisposeServiceAsync() => ((IAsyncDisposable)Service).DisposeAsync().AsTask();

        public async ValueTask DisposeAsync()
        {
            SetField(Service, "PreCleanupRequested", null);
            SetField(Service, "StatusChanged", null);
            SetField(Service, "ErrorOccurred", null);
            SetPreviewSink(null);
            await DisposeServiceAsync().WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}
