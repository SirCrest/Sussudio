using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Sussudio.Tests
{
    public sealed class PreviewLifecycleBehaviorTests
    {
        public PreviewLifecycleBehaviorTests() => global::Program.EnsureTargetAssemblyLoadedForXUnit();

        [Fact]
        public Task CoordinatorInitializationFailureFaultsPreviewStartup()
            => global::Program.PreviewLifecycle_CoordinatorFailureFaultsStartup();

        [Fact]
        public Task MissingDeviceFaultsInitialization()
            => global::Program.PreviewLifecycle_MissingDeviceFaultsInitialization();

        [Fact]
        public Task CanceledStartupDoesNotNotifyOrInitialize()
            => global::Program.PreviewLifecycle_CanceledStartupDoesNotMutate();

        [Fact]
        public Task InitializationFailureReachesAutomationResponse()
            => global::Program.PreviewLifecycle_InitializationFailureReachesAutomation();

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public Task OnlyLatestGateWaiterReinitializes(bool useResultOverload)
            => global::Program.PreviewLifecycle_OnlyLatestGateWaiterReinitializes(useResultOverload);

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public Task OnlyLatestPendingCycleWaiterReinitializes(bool cycleFaults)
            => global::Program.PreviewLifecycle_OnlyLatestPendingCycleWaiterReinitializes(cycleFaults);

        [Fact]
        public void PendingCycleContinuationKeepsUiContext()
            => global::Program.PreviewLifecycle_PendingCycleKeepsUiContext();

        [Fact]
        public Task DeviceBusyInitializationKeepsBoundedRetriesAndRepairsState()
            => global::Program.PreviewLifecycle_DeviceBusyRetryIsBounded();

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public Task RecordingThatBeginsDuringPendingCyclePreventsReinitialize(bool recordingTransitioning)
            => global::Program.PreviewLifecycle_RecordingDuringPendingCyclePreventsReinitialize(recordingTransitioning);

        [Fact]
        public Task PendingRecordingStartPreventsReinitialize()
            => global::Program.PreviewLifecycle_PendingRecordingStartPreventsReinitialize();

        [Fact]
        public Task AdmittedReinitializeRejectsRecordingStartAndAllowsStop()
            => global::Program.PreviewLifecycle_AdmittedReinitializeGuardsRecording();

        [Fact]
        public Task ReinitializeWithoutPreviewAlsoRejectsRecordingStart()
            => global::Program.PreviewLifecycle_ReinitializeWithoutPreviewGuardsRecording();

        [Fact]
        public Task BackendStopFailureRestoresTheCapturedVolumeOperation()
            => global::Program.PreviewLifecycle_BackendStopFailureRestoresVolumeOperation();
    }
}

static partial class Program
{
    internal static async Task PreviewLifecycle_CoordinatorFailureFaultsStartup()
    {
        var harness = new PreviewLifecycleHarness(await CreateDisposedPreviewCoordinatorAsync());
        var error = await Assert.ThrowsAsync<ObjectDisposedException>(() => harness.Start());
        Assert.Equal($"Failed to initialize: {error.Message}", harness.Status);
        Assert.False(harness.Initialized);
        Assert.False(harness.Previewing);
        Assert.Equal(1, harness.BuildSettingsCalls);
        Assert.Contains("start-requested", harness.Trace);
        Assert.DoesNotContain("telemetry", harness.Trace);
    }

    internal static async Task PreviewLifecycle_MissingDeviceFaultsInitialization()
    {
        var harness = new PreviewLifecycleHarness { Device = null };
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => harness.Initialize());
        Assert.Equal("No capture device selected.", error.Message);
        Assert.Equal("Failed to initialize: No capture device selected.", harness.Status);
        Assert.False(harness.Initialized);
        Assert.Equal(0, harness.BuildSettingsCalls);
    }

    internal static async Task PreviewLifecycle_CanceledStartupDoesNotMutate()
    {
        var harness = new PreviewLifecycleHarness();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => harness.Start(cancellation.Token));
        Assert.Empty(harness.Trace);
        Assert.Equal(0, harness.BuildSettingsCalls);
    }

    internal static async Task PreviewLifecycle_InitializationFailureReachesAutomation()
    {
        var harness = new PreviewLifecycleHarness(await CreateDisposedPreviewCoordinatorAsync());
        var devices = (IList)Activator.CreateInstance(typeof(ObservableCollection<>).MakeGenericType(harness.Device!.GetType()))!;
        devices.Add(harness.Device);
        var viewModel = CreateConfiguredProxy(RequireType("Sussudio.Services.Automation.IAutomationViewModel"), (method, arguments) =>
        {
            return method?.Name switch
            {
                "get_IsInitialized" => harness.Initialized,
                "get_Devices" => devices,
                "SetPreviewEnabledAsync" => harness.SetPreviewEnabled((bool)arguments![0]!, (CancellationToken)arguments[1]!),
                _ => GetDefaultReturnValue(method)
            };
        });
        var diagnostics = CreateConfiguredProxy(RequireType("Sussudio.Services.Contracts.IAutomationDiagnosticsHub"),
            (method, _) => GetDefaultReturnValue(method));
        var window = CreateConfiguredProxy(RequireType("Sussudio.Services.Contracts.IAutomationWindowControl"),
            (method, _) => GetDefaultReturnValue(method));
        var dispatcher = CreateAutomationCommandDispatcher(viewModel, diagnostics, window, authToken: null);
        var request = CreateAutomationCommandRequest("SetPreviewEnabled", null, "{\"enabled\":true}");

        var response = await ExecuteAutomationCommandAsync(dispatcher, request);

        AssertAutomationResponse(response, success: false, errorCode: "command-failed", status: "error", "preview initialization failure");
        Assert.Equal("failed", GetAutomationLifecycle(response));
        Assert.Equal(GetPublicProperty(request, "CorrelationId"), GetPublicProperty(response, "CorrelationId"));
        Assert.Contains("CaptureSessionCoordinator", (string)GetPublicProperty(response, "Message")!);
        Assert.False(harness.Initialized);
        Assert.False(harness.Previewing);
        Assert.Equal(1, harness.BuildSettingsCalls);
    }

    internal static async Task PreviewLifecycle_OnlyLatestGateWaiterReinitializes(bool useResultOverload)
    {
        var harness = new PreviewLifecycleHarness { Initialized = true, Previewing = true };
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stops = 0;
        harness.StopRenderer = () => ++stops == 1 ? releaseFirst.Task : Task.FromException(CreatePreviewRendererAbort());
        var first = harness.Reinitialize("first");
        Assert.Equal(new[] { "reinit:first" }, harness.Reinitializations());
        var stale = useResultOverload ? harness.Reinitialize("stale") : harness.ReinitializeWithoutResult("stale");
        var latest = harness.Reinitialize("latest");
        Assert.False(stale.IsCompleted);
        Assert.False(latest.IsCompleted);

        releaseFirst.SetException(CreatePreviewRendererAbort());
        await Task.WhenAll(first, stale, latest).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(await first);
        Assert.False(await latest);
        if (useResultOverload)
        {
            Assert.False(await (Task<bool>)stale);
        }
        Assert.Equal(new[] { "reinit:first", "reinit:latest" }, harness.Reinitializations());
        Assert.False(harness.Reinitializing);
        Assert.False(await harness.Reinitialize("after-failure").WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(new[] { "reinit:first", "reinit:latest", "reinit:after-failure" }, harness.Reinitializations());
    }

    internal static async Task PreviewLifecycle_OnlyLatestPendingCycleWaiterReinitializes(bool cycleFaults)
    {
        var releaseCycle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var harness = new PreviewLifecycleHarness { Initialized = true, Previewing = true, PendingCycle = releaseCycle.Task };
        var stale = harness.Reinitialize("stale-cycle");
        var latest = harness.Reinitialize("latest-cycle");
        Assert.Empty(harness.Reinitializations());
        if (cycleFaults)
        {
            releaseCycle.SetException(new InvalidOperationException("synthetic encoder cycle failure"));
        }
        else
        {
            releaseCycle.SetResult();
        }

        await Task.WhenAll(stale, latest).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.False(await stale);
        Assert.False(await latest);
        Assert.Equal(new[] { "reinit:latest-cycle" }, harness.Reinitializations());
        Assert.Null(harness.PendingCycle);
        Assert.False(harness.Reinitializing);
    }

    internal static void PreviewLifecycle_PendingCycleKeepsUiContext()
    {
        using var uiContext = new PreviewLifecyclePumpContext();
        var previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(uiContext);
        try
        {
            var releaseCycle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var observedContexts = new ConcurrentQueue<SynchronizationContext?>();
            var harness = new PreviewLifecycleHarness
            {
                Initialized = true,
                Previewing = true,
                PendingCycle = releaseCycle.Task,
                ObserveCallback = () => observedContexts.Enqueue(SynchronizationContext.Current)
            };
            var request = harness.Reinitialize("ui-affinity");
            Assert.Empty(harness.Reinitializations());
            _ = Task.Run(() => releaseCycle.SetResult());

            uiContext.RunUntilCompleted(request);

            Assert.False(request.GetAwaiter().GetResult());
            Assert.NotEmpty(observedContexts);
            Assert.All(observedContexts, context => Assert.Same(uiContext, context));
            Assert.Equal(new[] { "reinit:ui-affinity" }, harness.Reinitializations());
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    internal static async Task PreviewLifecycle_DeviceBusyRetryIsBounded()
    {
        var harness = new PreviewLifecycleHarness(await CreateDisposedPreviewCoordinatorAsync())
        {
            Previewing = true,
            BuildFailure = new COMException("synthetic device busy", unchecked((int)0xC00D36E6)),
            StopRenderer = () => Task.CompletedTask
        };

        Assert.False(await harness.Reinitialize("busy").WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.Equal(4, harness.BuildSettingsCalls); // Initial attempt, two retries, then one recovery attempt.
        Assert.False(harness.Initialized);
        Assert.False(harness.Previewing);
        Assert.False(harness.Reinitializing);
        Assert.Equal("Failed to apply format: synthetic device busy", harness.Status);
        Assert.False(await harness.Reinitialize("after-busy").WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(5, harness.BuildSettingsCalls);
    }

    private static Exception CreatePreviewRendererAbort()
        => (Exception)Activator.CreateInstance(RequireType("Sussudio.Controllers.PreviewRendererReinitStopTimeoutException"),
            "synthetic renderer stop timeout", new TimeoutException("renderer is still active"))!;

    internal static async Task PreviewLifecycle_RecordingDuringPendingCyclePreventsReinitialize(bool recordingTransitioning)
    {
        var releaseCycle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var harness = new PreviewLifecycleHarness { Initialized = true, Previewing = true, PendingCycle = releaseCycle.Task };
        var reinitialize = harness.Reinitialize("recording-during-cycle");
        Assert.False(reinitialize.IsCompleted);

        harness.Recording = !recordingTransitioning;
        harness.RecordingTransitioning = recordingTransitioning;
        releaseCycle.SetResult();

        Assert.False(await reinitialize.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Empty(harness.Reinitializations());
        Assert.DoesNotContain("stop-requested", harness.Trace);
        Assert.True(harness.Previewing);
        Assert.False(harness.ReinitializeAdmitted);
        Assert.Equal("Stop recording before changing capture settings.", harness.Status);
    }

    internal static async Task PreviewLifecycle_PendingRecordingStartPreventsReinitialize()
    {
        var releaseRecording = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var harness = new PreviewLifecycleHarness
        {
            Initialized = true,
            Previewing = true,
            StartRecording = _ => releaseRecording.Task
        };
        var recording = harness.SetRecording(true);
        Assert.True(harness.RecordingTransitioning);
        Assert.False(harness.Recording);

        Assert.False(await harness.Reinitialize("recording-start-pending"));
        Assert.Empty(harness.Reinitializations());
        Assert.DoesNotContain("stop-requested", harness.Trace);
        releaseRecording.SetResult();
        await recording.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(harness.Recording);
        Assert.False(harness.RecordingTransitioning);
        Assert.Equal(1, harness.RecordingStartCalls);
    }

    internal static async Task PreviewLifecycle_AdmittedReinitializeGuardsRecording()
    {
        var releaseRenderer = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var harness = new PreviewLifecycleHarness
        {
            Initialized = true,
            Previewing = true,
            StopRenderer = () => releaseRenderer.Task
        };
        var reinitialize = harness.Reinitialize("recording-guard");
        Assert.True(harness.ReinitializeAdmitted);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => harness.SetRecording(true));
        Assert.Equal("Wait for capture settings to finish applying before starting recording.", error.Message);
        Assert.Equal(error.Message, harness.Status);
        Assert.Equal(0, harness.RecordingStartCalls);
        Assert.False(harness.RecordingTransitioning);

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => harness.SetRecording(true, canceled.Token));
        Assert.True(harness.ReinitializeAdmitted);

        // A stop remains available even if capture's recording state becomes true
        // during the admitted interval (for example, a delayed state notification).
        harness.Recording = true;
        await harness.SetRecording(false);
        Assert.False(harness.Recording);
        Assert.Equal(1, harness.RecordingStopCalls);

        releaseRenderer.SetException(CreatePreviewRendererAbort());
        Assert.False(await reinitialize.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.False(harness.ReinitializeAdmitted);
        await harness.SetRecording(true);
        Assert.Equal(1, harness.RecordingStartCalls);
        Assert.True(harness.Recording);
    }

    internal static async Task PreviewLifecycle_ReinitializeWithoutPreviewGuardsRecording()
    {
        var harness = new PreviewLifecycleHarness { BuildFailure = new InvalidOperationException("synthetic initialization failure") };
        Task? recording = null;
        harness.BeforeBuildSettings = () =>
        {
            Assert.False(harness.Reinitializing); // Presentation state only covers active preview.
            Assert.True(harness.ReinitializeAdmitted);
            recording = harness.SetRecording(true);
        };

        Assert.False(await harness.Reinitialize("ready-state-reinitialize"));
        Assert.NotNull(recording);
        await Assert.ThrowsAsync<InvalidOperationException>(() => recording!);
        Assert.Equal(0, harness.RecordingStartCalls);
        Assert.False(harness.ReinitializeAdmitted);
        Assert.False(harness.RecordingTransitioning);

        harness.BeforeBuildSettings = null;
        harness.BuildFailure = null;
        harness.Initialized = true;
        await harness.SetRecording(true);
        Assert.Equal(1, harness.RecordingStartCalls);
    }

    internal static async Task PreviewLifecycle_BackendStopFailureRestoresVolumeOperation()
    {
        var harness = new PreviewLifecycleHarness(await CreateDisposedPreviewCoordinatorAsync())
        {
            Initialized = true,
            Previewing = true,
            AudioPreviewActive = true
        };

        await Assert.ThrowsAsync<ObjectDisposedException>(() => harness.Stop());

        Assert.Equal(1, harness.VolumeRestoreCalls);
        Assert.Equal(41, harness.RestoredVolumeGeneration);
        Assert.Equal(new[] { "volume-ramp", "stop-requested", "previewing:False", "volume-restore" }, harness.Trace);
    }

    private static async Task<object> CreateDisposedPreviewCoordinatorAsync()
    {
        // The real coordinator rejects work after disposal before touching this inert service.
        // Its empty worker is drained before any lifecycle command is submitted.
        var service = RuntimeHelpers.GetUninitializedObject(RequireType("Sussudio.Services.Capture.CaptureService"));
        var coordinator = Activator.CreateInstance(RequireType("Sussudio.Services.Capture.CaptureSessionCoordinator"), service)!;
        await ((IAsyncDisposable)coordinator).DisposeAsync();
        return coordinator;
    }

    private sealed class PreviewLifecycleHarness
    {
        private readonly object _controller;
        private readonly object _recordingController;
        public object? Device = BuildDevice();
        public bool Initialized;
        public bool Previewing;
        public bool Reinitializing;
        public bool Recording;
        public bool RecordingTransitioning;
        public bool AudioPreviewActive;
        public string Status = string.Empty;
        public int BuildSettingsCalls;
        public Exception? BuildFailure;
        public Action? BeforeBuildSettings;
        public Task? PendingCycle;
        public Action? ObserveCallback;
        public Func<Task> StopRenderer = () => Task.FromException(CreatePreviewRendererAbort());
        public Func<CancellationToken, Task> StartRecording = _ => Task.CompletedTask;
        public int RecordingStartCalls;
        public int RecordingStopCalls;
        public int VolumeRestoreCalls;
        public long RestoredVolumeGeneration;
        public bool ReinitializeAdmitted => (bool)_controller.GetType().GetProperty("IsReinitializeAdmitted")!.GetValue(_controller)!;
        public ConcurrentQueue<string> Trace { get; } = new();

        public PreviewLifecycleHarness(object? coordinator = null)
        {
            var lifecycleType = RequireType("Sussudio.Controllers.MainViewModelPreviewLifecycleController");
            var reinitializeType = RequireType("Sussudio.Controllers.MainViewModelPreviewReinitializeController");
            var lifecycle = Activator.CreateInstance(RequireType("Sussudio.Controllers.MainViewModelPreviewLifecycleControllerContext"), nonPublic: true)!;
            var reinitialize = Activator.CreateInstance(RequireType("Sussudio.Controllers.MainViewModelPreviewReinitializeControllerContext"), nonPublic: true)!;
            var format = Activator.CreateInstance(RequireType("Sussudio.Models.MediaFormat"))!;
            foreach (var context in new[] { lifecycle, reinitialize })
            {
                SetFactory(context, "SelectedDevice", () => Device);
                Set(context, "IsInitialized", new Func<bool>(() => Initialized));
                Set(context, "SetIsInitialized", new Action<bool>(value => { Record("initialized:" + value); Initialized = value; }));
                Set(context, "IsPreviewing", new Func<bool>(() => Previewing));
                Set(context, "SetIsPreviewing", new Action<bool>(value => { Record("previewing:" + value); Previewing = value; }));
                Set(context, "IsPreviewReinitializing", new Func<bool>(() => Reinitializing));
                Set(context, "IsRecording", new Func<bool>(() => Recording));
                Set(context, "SetStatusText", new Action<string>(value => { Record("status:" + value); Status = value; }));
            }
            Set(lifecycle, "SessionCoordinator", coordinator);
            SetFactory(lifecycle, "BuildCaptureSettings", () =>
            {
                BuildSettingsCalls++;
                BeforeBuildSettings?.Invoke();
                if (BuildFailure is not null) throw BuildFailure;
                return BuildSettings(hdrEnabled: false);
            });
            Set(lifecycle, "InvokeOnUiThreadAsync", new Func<Func<Task>, CancellationToken, Task>((operation, _) => operation()));
            var volumeOperation = Activator.CreateInstance(RequireType("Sussudio.Controllers.PreviewAudioVolumeOperation"), 41L)!;
            SetAsyncOperation(lifecycle, "RampPreviewVolumeDownForStopAsync", _ =>
            {
                Record("volume-ramp");
                return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(volumeOperation.GetType()).Invoke(null, new[] { volumeOperation })!;
            });
            SetObjectAction(lifecycle, "RestorePreviewVolumeAfterStopFailed", operation =>
            {
                Record("volume-restore");
                VolumeRestoreCalls++;
                RestoredVolumeGeneration = (long)operation.GetType().GetProperty("Generation")!.GetValue(operation)!;
            });
            Set(lifecycle, "ShouldStartAudioPreview", new Func<bool>(() => false));
            Set(lifecycle, "IsAudioPreviewActive", new Func<bool>(() => AudioPreviewActive));
            Set(lifecycle, "RaisePreviewStartRequested", new Action(() => Record("start-requested")));
            Set(lifecycle, "RaisePreviewStopRequested", new Action(() => Record("stop-requested")));
            Set(lifecycle, "ApplyLatestSourceTelemetryForPreviewStart", new Action(() => Record("telemetry")));
            SetFactory(reinitialize, "SelectedFormat", () => format);
            Set(reinitialize, "IsRecordingTransitioning", new Func<bool>(() => RecordingTransitioning));
            Set(reinitialize, "SetIsPreviewReinitializing", new Action<bool>(value => { Record("reinitializing:" + value); Reinitializing = value; }));
            Set(reinitialize, "PreviewReinitializeDebounceMs", 0);
            Set(reinitialize, "PendingFlashbackCycleTask", new Func<Task?>(() => PendingCycle));
            Set(reinitialize, "FlashbackCycleBeforeReinitializeTimeoutMs", 5000);
            Set(reinitialize, "AwaitWithTimeoutAsync", new Func<Task, int, string, Task>((task, _, _) => task));
            Set(reinitialize, "ClearPendingFlashbackCycleIfSameAndCompleted", new Action<Task>(task =>
            {
                Record("clear-cycle");
                if (ReferenceEquals(PendingCycle, task) && task.IsCompleted) PendingCycle = null;
            }));
            Set(reinitialize, "NotifyPreviewReinitRequestedAsync", new Func<string, Task>(reason => { Record("reinit:" + reason); return Task.CompletedTask; }));
            Set(reinitialize, "NotifyRendererStopAsync", new Func<Task>(() => StopRenderer()));
            Set(reinitialize, "PendingDeferredCaptureCleanupTask", new Func<Task?>(() => null));
            Set(reinitialize, "ReinitDeviceBusyCleanupTimeoutMs", 5000);
            var factoryProperty = lifecycle.GetType().GetProperty("CreateReinitializeController")!;
            var lifecycleParameter = Expression.Parameter(lifecycleType, "lifecycle");
            var constructor = reinitializeType.GetConstructor(new[] { reinitialize.GetType(), lifecycleType })!;
            factoryProperty.SetValue(lifecycle, Expression.Lambda(factoryProperty.PropertyType,
                Expression.New(constructor, Expression.Constant(reinitialize), lifecycleParameter), lifecycleParameter).Compile());
            _controller = Activator.CreateInstance(lifecycleType, lifecycle)!;

            var recording = Activator.CreateInstance(RequireType("Sussudio.Controllers.MainViewModelRecordingTransitionControllerContext"), nonPublic: true)!;
            Set(recording, "IsRecording", new Func<bool>(() => Recording));
            Set(recording, "SetIsRecording", new Action<bool>(value => Recording = value));
            Set(recording, "IsInitialized", new Func<bool>(() => Initialized));
            Set(recording, "HasSelectedDevice", new Func<bool>(() => Device != null));
            Set(recording, "GetStatusText", new Func<string>(() => Status));
            Set(recording, "SetStatusText", new Action<string>(value => Status = value));
            Set(recording, "SetIsRecordingTransitioning", new Action<bool>(value => RecordingTransitioning = value));
            Set(recording, "InvokeOnUiThreadAsync", new Func<Func<Task>, CancellationToken, Task>((operation, _) => operation()));
            SetFactory(recording, "BuildCaptureSettings", () => BuildSettings(hdrEnabled: false));
            SetAsyncOperation(recording, "StartRecordingAsync", cancellationToken =>
            {
                RecordingStartCalls++;
                return StartRecording(cancellationToken);
            });
            Set(recording, "StopRecordingAsync", new Func<CancellationToken, Task>(_ =>
            {
                RecordingStopCalls++;
                return Task.CompletedTask;
            }));
            Set(recording, "GetSessionIsRecording", new Func<bool>(() => Recording));
            foreach (var property in new[] { "RestartRecordingStopwatch", "StopRecordingStopwatch", "ClearRecordingBitrateSamples" })
                Set(recording, property, new Action(() => { }));
            Set(recording, "SetRecordingSizeInfo", new Action<string>(_ => { }));
            Set(recording, "SetRecordingBitrateInfo", new Action<string>(_ => { }));
            Set(recording, "GetRecordingTime", new Func<string>(() => "00:00"));
            _recordingController = Activator.CreateInstance(RequireType("Sussudio.Controllers.MainViewModelRecordingTransitionController"), recording, _controller)!;
        }

        public Task Initialize() => Call("InitializeDeviceAsync", CancellationToken.None);
        public Task Start(CancellationToken cancellationToken = default) => Call("StartPreviewAsync", true, cancellationToken);
        public Task Stop(CancellationToken cancellationToken = default) => Call("StopPreviewAsync", true, false, cancellationToken);
        public Task SetRecording(bool enabled, CancellationToken cancellationToken = default)
            => (Task)_recordingController.GetType().GetMethod("SetRecordingDesiredStateAsync")!.Invoke(_recordingController, new object[] { enabled, cancellationToken })!;
        public Task SetPreviewEnabled(bool enabled, CancellationToken cancellationToken) => Call("SetPreviewEnabledAsync", enabled, cancellationToken);
        public Task<bool> Reinitialize(string reason) => (Task<bool>)Call("ReinitializeDeviceWithResultAsync", reason);
        public Task ReinitializeWithoutResult(string reason) => Call("ReinitializeDeviceAsync", reason);
        public string[] Reinitializations() => Trace.Where(item => item.StartsWith("reinit:", StringComparison.Ordinal)).ToArray();
        private Task Call(string method, params object[] arguments) => (Task)_controller.GetType().GetMethod(method)!.Invoke(_controller, arguments)!;
        private void Record(string item) { ObserveCallback?.Invoke(); Trace.Enqueue(item); }
        private static void Set(object context, string property, object? value) => SetPropertyOrBackingField(context, property, value);
        private static void SetFactory(object context, string propertyName, Func<object?> factory)
        {
            var property = context.GetType().GetProperty(propertyName)!;
            var returnType = property.PropertyType.GetMethod("Invoke")!.ReturnType;
            property.SetValue(context, Expression.Lambda(property.PropertyType,
                Expression.Convert(Expression.Invoke(Expression.Constant(factory)), returnType)).Compile());
        }

        private static void SetAsyncOperation(object context, string propertyName, Func<CancellationToken, object> operation)
        {
            var property = context.GetType().GetProperty(propertyName)!;
            var invoke = property.PropertyType.GetMethod("Invoke")!;
            var parameters = invoke.GetParameters().Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name)).ToArray();
            property.SetValue(context, Expression.Lambda(property.PropertyType,
                Expression.Convert(Expression.Invoke(Expression.Constant(operation), parameters[^1]), invoke.ReturnType), parameters).Compile());
        }

        private static void SetObjectAction(object context, string propertyName, Action<object> action)
        {
            var property = context.GetType().GetProperty(propertyName)!;
            var parameter = Expression.Parameter(property.PropertyType.GetMethod("Invoke")!.GetParameters()[0].ParameterType);
            property.SetValue(context, Expression.Lambda(property.PropertyType,
                Expression.Invoke(Expression.Constant(action), Expression.Convert(parameter, typeof(object))), parameter).Compile());
        }
    }

    private sealed class PreviewLifecyclePumpContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _callbacks = new();
        public override void Post(SendOrPostCallback callback, object? state) => _callbacks.Add((callback, state));
        public void RunUntilCompleted(Task task)
        {
            while (!task.IsCompleted)
            {
                Assert.True(_callbacks.TryTake(out var callback, TimeSpan.FromSeconds(5)), "UI continuation was not posted.");
                callback.Callback(callback.State);
            }
        }
        public void Dispose() => _callbacks.Dispose();
    }
}
