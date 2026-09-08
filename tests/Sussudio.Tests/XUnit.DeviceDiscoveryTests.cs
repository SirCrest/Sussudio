using System.Collections;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace Sussudio.Tests;

public sealed class DeviceDiscoveryTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task EnumerationFailureHasAnExplicitPerCallOutcome(bool audioFails, bool synchronousFailure)
    {
        var service = CreateService((audio, taskType) =>
        {
            if (audio == audioFails)
            {
                var failure = new InvalidOperationException(audio ? "audio enumeration unavailable" : "video enumeration unavailable");
                if (synchronousFailure) throw failure;
                return FailedTask(taskType, failure);
            }

            return EmptyListTask(taskType);
        });

        var result = await InvokeResultAsync(service, "EnumerateCaptureDeviceDiscoveryAsync", false);

        Assert.False(Get<bool>(result, "Succeeded"));
        Assert.Contains(audioFails ? "audio enumeration unavailable" : "video enumeration unavailable", Get<string>(result, "Error"));
        Assert.Empty(Get<IEnumerable>(result, "CaptureDevices"));
        Assert.Empty(Get<IEnumerable>(result, "AudioInputDevices"));
    }

    [Fact]
    public async Task SuccessfulEmptyEnumerationIsSuccessfulForBothPublicEntryPoints()
    {
        var service = CreateService((_, taskType) => EmptyListTask(taskType));

        var result = await InvokeResultAsync(service, "EnumerateCaptureDeviceDiscoveryAsync", false);
        var devices = await InvokeResultAsync(service, "EnumerateVideoCaptureDevicesAsync", false);

        Assert.True(Get<bool>(result, "Succeeded"));
        Assert.Null(Get<string?>(result, "Error"));
        Assert.Empty((IEnumerable)devices);
    }

    [Fact]
    public async Task CollectionOnlyEntryPointThrowsInsteadOfReturningFailedEmptyDiscovery()
    {
        var service = CreateService((_, taskType) => FailedTask(taskType, new IOException("device transport failed")));

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => InvokeResultAsync(service, "EnumerateVideoCaptureDevicesAsync", false));

        Assert.Contains("device transport failed", failure.Message);
    }

    [Fact]
    public async Task ResultErrorSurvivesAFollowingSuccessfulScan()
    {
        var fail = true;
        var service = CreateService((_, taskType) => fail
            ? FailedTask(taskType, new IOException("first scan failed"))
            : EmptyListTask(taskType));

        var failedResult = await InvokeResultAsync(service, "EnumerateCaptureDeviceDiscoveryAsync", false);
        fail = false;
        var successfulResult = await InvokeResultAsync(service, "EnumerateCaptureDeviceDiscoveryAsync", false);

        Assert.True(Get<bool>(successfulResult, "Succeeded"));
        Assert.False(Get<bool>(failedResult, "Succeeded"));
        Assert.Contains("first scan failed", Get<string>(failedResult, "Error"));
        Assert.DoesNotContain("first scan failed", Get<string>(service, "LastDiscoverySummary"));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task FormatProbeFailureHasAnExplicitPerCallOutcome(bool background, bool synchronousFailure)
    {
        var service = CreateProbeService(taskType =>
        {
            var failure = new IOException("capture device busy");
            if (synchronousFailure) throw failure;
            return FailedTask(taskType, failure);
        });

        var result = await ProbeResultAsync(service, background);

        Assert.False(Get<bool>(result, "Succeeded"));
        Assert.Contains("capture device busy", Get<string>(result, "Error"));
        Assert.Empty(Get<IEnumerable>(result, background ? "Formats" : "CaptureDevices"));
        if (!background)
        {
            var failure = await Assert.ThrowsAsync<InvalidOperationException>(
                () => InvokeResultAsync(service, "EnumerateVideoCaptureDevicesAsync", true));
            Assert.Contains("capture device busy", failure.Message);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SuccessfulEmptyFormatProbeRemainsSuccessful(bool background)
    {
        var service = CreateProbeService(EmptyListTask);

        var result = await ProbeResultAsync(service, background);

        Assert.True(Get<bool>(result, "Succeeded"));
        Assert.Null(Get<string?>(result, "Error"));
        if (background)
        {
            Assert.False(Get<bool>(result, "HasEnumeratedFormats"));
            Assert.Empty(Get<IEnumerable>(result, "Formats"));
        }
        else
        {
            var device = Assert.Single(Get<IEnumerable>(result, "CaptureDevices").Cast<object>());
            Assert.Empty(Get<IEnumerable>(device, "SupportedFormats"));
            var devices = await InvokeResultAsync(service, "EnumerateVideoCaptureDevicesAsync", true);
            Assert.Single(((IEnumerable)devices).Cast<object>());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FormatProbeErrorSurvivesALaterSuccessfulProbeEvenWhenExceptionMessageIsEmpty(bool background)
    {
        var fail = true;
        var service = CreateProbeService(taskType => fail
            ? FailedTask(taskType, new IOException(string.Empty))
            : EmptyListTask(taskType));

        var failedResult = await ProbeResultAsync(service, background);
        fail = false;
        var successfulResult = await ProbeResultAsync(service, background);

        Assert.True(Get<bool>(successfulResult, "Succeeded"));
        Assert.False(Get<bool>(failedResult, "Succeeded"));
        Assert.Contains(nameof(IOException), Get<string>(failedResult, "Error"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedBackgroundProbePreservesCachedCapabilitiesAndSelection(bool synchronousFailure)
    {
        var fixture = new FormatProbeFixture();
        var service = CreateProbeService(taskType =>
        {
            var failure = new IOException("device disconnected during format probe");
            if (synchronousFailure) throw failure;
            return FailedTask(taskType, failure);
        });

        fixture.Apply(await BackgroundProbeAsync(service, fixture.Device, 41));

        Assert.Same(fixture.CachedFormat, Assert.Single(Get<IEnumerable>(fixture.Device, "SupportedFormats").Cast<object>()));
        Assert.True(Get<bool>(fixture.Device, "IsHdrCapable"));
        Assert.Same(fixture.Device, fixture.SelectedDevice);
        Assert.Same(fixture.CachedFormat, fixture.SelectedFormat);
        Assert.Equal("3840x2160", fixture.SelectedResolution);
        Assert.Equal(60d, fixture.SelectedFrameRate);
        Assert.Equal(0, fixture.CapabilityRebuilds);
        Assert.False(fixture.PendingAutoSelection);
        Assert.Null(fixture.PendingFrameRateBucket);
    }

    [Fact]
    public async Task FailedUnselectedBackgroundProbePreservesPendingSelectionForSelectedDevice()
    {
        var selectedDevice = CreateDevice("currently-selected-device");
        var fixture = new FormatProbeFixture { SelectedDevice = selectedDevice };
        var service = CreateProbeService(taskType => FailedTask(taskType, new IOException("unselected device probe failed")));

        fixture.Apply(await BackgroundProbeAsync(service, fixture.Device, 41));

        Assert.True(fixture.PendingAutoSelection);
        Assert.Equal(60, fixture.PendingFrameRateBucket);
        Assert.Same(selectedDevice, fixture.SelectedDevice);
        Assert.Same(fixture.CachedFormat, Assert.Single(Get<IEnumerable>(fixture.Device, "SupportedFormats").Cast<object>()));
        Assert.True(Get<bool>(fixture.Device, "IsHdrCapable"));
        Assert.Equal(0, fixture.CapabilityRebuilds);
    }

    [Fact]
    public async Task SuccessfulEmptyBackgroundProbeClearsCachedCapabilities()
    {
        var fixture = new FormatProbeFixture { SelectedDevice = null };
        var service = CreateProbeService(EmptyListTask);

        fixture.Apply(await BackgroundProbeAsync(service, fixture.Device, 41));

        Assert.Empty(Get<IEnumerable>(fixture.Device, "SupportedFormats"));
        Assert.False(Get<bool>(fixture.Device, "IsHdrCapable"));
    }

    [Fact]
    public async Task FailedInlineFormatProbePreservesExistingRefreshState()
    {
        var service = CreateProbeService(taskType => FailedTask(taskType, new IOException("probe failed")));
        var fixture = new RefreshFixture
        {
            Discover = () => InvokeResultAsync(service, "EnumerateCaptureDeviceDiscoveryAsync", true)
        };

        await fixture.RefreshAsync();

        fixture.AssertOriginalState();
        Assert.Contains("probe failed", fixture.Status);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task FailedRefreshPreservesDevicesSelectionsSavedIdsAndProbeGeneration(bool throwOnScanFailure, bool enumerationThrows)
    {
        var fixture = new RefreshFixture();
        fixture.Discover = () => enumerationThrows
            ? Task.FromException<object>(new InvalidOperationException("device scan failed"))
            : Task.FromResult(CreateDiscovery(error: "device scan failed"));

        if (throwOnScanFailure)
        {
            var failure = await Assert.ThrowsAsync<InvalidOperationException>(
                () => fixture.RefreshAsync(throwOnScanFailure: true));
            Assert.Contains("device scan failed", failure.Message);
        }
        else
        {
            await fixture.RefreshAsync();
        }

        fixture.AssertOriginalState();
        Assert.Equal("Error scanning devices: device scan failed", fixture.Status);
    }

    [Fact]
    public async Task SuccessfulEmptyRefreshCommitsTheEmptyState()
    {
        var fixture = new RefreshFixture();
        fixture.Discover = () => Task.FromResult(CreateDiscovery());

        await fixture.RefreshAsync(throwOnScanFailure: true);

        Assert.Empty(fixture.Devices);
        Assert.Null(fixture.SelectedDevice);
        Assert.Empty(fixture.AudioDevices);
        Assert.Null(fixture.SelectedAudioId);
        Assert.Null(fixture.SelectedMicrophoneId);
        Assert.Null(fixture.PendingSavedAudioId);
        Assert.Null(fixture.PendingSavedMicrophoneId);
        Assert.Equal(42, fixture.ProbeGeneration);
        Assert.Equal(1, fixture.DeviceReplacements);
        Assert.Equal(1, fixture.AudioReplacements);
        Assert.Equal(0, fixture.PreviewStarts);
        Assert.Contains("No compatible video capture devices found", fixture.Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationBeforeCommitPreservesState(bool beforeInvocation)
    {
        var fixture = new RefreshFixture();
        var completion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Discover = () => completion.Task;
        using var cancellation = new CancellationTokenSource();
        if (beforeInvocation) cancellation.Cancel();

        var refresh = fixture.RefreshAsync(cancellation.Token);
        cancellation.Cancel();
        completion.SetResult(CreateDiscovery());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => refresh);
        fixture.AssertOriginalState();
        Assert.Equal(beforeInvocation ? "Original status" : "Device scan canceled", fixture.Status);
        Assert.Equal(beforeInvocation ? 0 : 1, fixture.DiscoveryCalls);
    }

    [Fact]
    public async Task OlderSuccessfulScanCannotReplaceANewerSuccessfulScan()
    {
        var fixture = new RefreshFixture();
        var olderCompletion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Discover = () => olderCompletion.Task;
        var olderRefresh = fixture.RefreshAsync();
        var newerDevice = CreateDevice("newer");
        fixture.Discover = () => Task.FromResult(CreateDiscovery(devices: [newerDevice]));
        await fixture.RefreshAsync();
        var newerStatus = fixture.Status;

        olderCompletion.SetResult(CreateDiscovery(devices: [CreateDevice("older")]));
        await olderRefresh;

        Assert.Same(newerDevice, Assert.Single(fixture.Devices.Cast<object>()));
        Assert.Same(newerDevice, fixture.SelectedDevice);
        Assert.Equal(newerStatus, fixture.Status);
        Assert.Equal(42, fixture.ProbeGeneration);
        Assert.Equal(1, fixture.DeviceReplacements);
        Assert.Equal(1, fixture.PreviewStarts);
    }

    [Fact]
    public async Task SuccessfulScanKeepsASelectionMadeWhileDiscoveryWasPending()
    {
        var fixture = new RefreshFixture();
        var selectedDuringScan = CreateDevice("chosen-during-scan");
        fixture.Devices.Add(selectedDuringScan);
        var completion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Discover = () => completion.Task;
        var refresh = fixture.RefreshAsync();
        fixture.SelectedDevice = selectedDuringScan;
        var refreshedSelection = CreateDevice("chosen-during-scan");

        completion.SetResult(CreateDiscovery(devices: [CreateDevice("current"), refreshedSelection]));
        await refresh;

        Assert.Same(refreshedSelection, fixture.SelectedDevice);
        Assert.Equal(1, fixture.PreviewStarts);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StaleSuccessAfterANewerFailurePreservesTheRetainedState(bool throwOnScanFailure)
    {
        var fixture = new RefreshFixture();
        var olderCompletion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Discover = () => olderCompletion.Task;
        var olderRefresh = fixture.RefreshAsync(throwOnScanFailure: throwOnScanFailure);
        fixture.Discover = () => Task.FromResult(CreateDiscovery(error: "newer scan failed"));
        await fixture.RefreshAsync();

        olderCompletion.SetResult(CreateDiscovery());
        if (throwOnScanFailure)
        {
            var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => olderRefresh);
            Assert.Contains("superseded", failure.Message);
        }
        else
        {
            await olderRefresh;
        }

        fixture.AssertOriginalState();
        Assert.Equal("Error scanning devices: newer scan failed", fixture.Status);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StaleFailureOrCancellationCannotReplaceNewerStatus(bool cancelOlder)
    {
        var fixture = new RefreshFixture();
        var olderCompletion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Discover = () => olderCompletion.Task;
        using var cancellation = new CancellationTokenSource();
        var olderRefresh = fixture.RefreshAsync(cancellation.Token);
        fixture.Discover = () => Task.FromResult(CreateDiscovery(error: "newer scan failed"));
        await fixture.RefreshAsync();

        if (cancelOlder)
        {
            cancellation.Cancel();
            olderCompletion.SetResult(CreateDiscovery());
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => olderRefresh);
        }
        else
        {
            olderCompletion.SetException(new IOException("older scan failed"));
            await olderRefresh;
        }

        fixture.AssertOriginalState();
        Assert.Equal("Error scanning devices: newer scan failed", fixture.Status);
    }

    private sealed class RefreshFixture
    {
        private readonly object _controller;
        private readonly object _originalDevice = CreateDevice("current");

        public RefreshFixture()
        {
            Devices = NewList(typeof(List<>).MakeGenericType(CaptureDeviceType), _originalDevice);
            SelectedDevice = _originalDevice;
            var context = Activator.CreateInstance(RequireType("Sussudio.Controllers.MainViewModelDeviceRefreshControllerContext"))!;
            SetDelegate(context, "SetStatusText", args => { Status = (string)args[0]!; return null; });
            SetDelegate(context, "IncrementDeviceScanGeneration", _ => ++ProbeGeneration);
            SetDelegate(context, "GetSelectedAudioInputDeviceId", _ => SelectedAudioId);
            SetDelegate(context, "GetSelectedMicrophoneDeviceId", _ => SelectedMicrophoneId);
            SetDelegate(context, "GetSelectedDeviceId", _ => SelectedDevice == null ? null : Get<string>(SelectedDevice, "Id"));
            SetDelegate(context, "EnumerateCaptureDeviceDiscoveryAsync", _ =>
            {
                DiscoveryCalls++;
                return TypedTask(DiscoveryResultType, Discover());
            });
            SetDelegate(context, "ApplyStartupAudioDeviceScan", args =>
            {
                AudioReplacements++;
                AudioDevices = ((IEnumerable)args[0]!).Cast<object>().Select(device => Get<string>(device, "Id")).ToList();
                SelectedAudioId = AudioDevices.FirstOrDefault();
                SelectedMicrophoneId = AudioDevices.FirstOrDefault();
                PendingSavedAudioId = null;
                PendingSavedMicrophoneId = null;
                return null;
            });
            SetDelegate(context, "ReplaceDevices", args =>
            {
                DeviceReplacements++;
                Devices.Clear();
                foreach (var device in (IEnumerable)args[0]!) Devices.Add(device);
                return null;
            });
            SetDelegate(context, "GetDevices", _ => Devices);
            SetDelegate(context, "BeginBackgroundFormatProbe", _ => { ProbeStarts++; return null; });
            SetDelegate(context, "GetLastDiscoverySummary", _ => "Fake discovery");
            SetDelegate(context, "SetSelectedDevice", args => { SelectedDevice = args[0]; return null; });
            SetDelegate(context, "GetSelectedDevice", _ => SelectedDevice);
            SetDelegate(context, "GetPendingSavedDeviceId", _ => PendingSavedDeviceId);
            SetDelegate(context, "SetPendingSavedDeviceId", args => { PendingSavedDeviceId = (string?)args[0]; return null; });

            _controller = Activator.CreateInstance(
                RequireType("Sussudio.Controllers.MainViewModelDeviceRefreshController"),
                context,
                CreatePreviewController())!;
        }

        public Func<Task<object>> Discover { get; set; } = () => Task.FromResult(CreateDiscovery());
        public IList Devices { get; }
        public object? SelectedDevice { get; set; }
        public List<string> AudioDevices { get; private set; } = ["audio", "microphone"];
        public string? SelectedAudioId { get; private set; } = "audio";
        public string? SelectedMicrophoneId { get; private set; } = "microphone";
        public string? PendingSavedDeviceId { get; private set; } = "saved-video";
        public string? PendingSavedAudioId { get; private set; } = "saved-audio";
        public string? PendingSavedMicrophoneId { get; private set; } = "saved-microphone";
        public string Status { get; private set; } = "Original status";
        public long ProbeGeneration { get; private set; } = 41;
        public int DiscoveryCalls { get; private set; }
        public int DeviceReplacements { get; private set; }
        public int AudioReplacements { get; private set; }
        public int ProbeStarts { get; private set; }
        public int PreviewStarts { get; private set; }

        public Task RefreshAsync(CancellationToken cancellationToken = default, bool throwOnScanFailure = false)
            => (Task)_controller.GetType().GetMethod("RefreshDevicesAsync")!.Invoke(
                _controller, [cancellationToken, throwOnScanFailure])!;

        public void AssertOriginalState()
        {
            Assert.Same(_originalDevice, Assert.Single(Devices.Cast<object>()));
            Assert.Same(_originalDevice, SelectedDevice);
            Assert.Equal(new[] { "audio", "microphone" }, AudioDevices);
            Assert.Equal("audio", SelectedAudioId);
            Assert.Equal("microphone", SelectedMicrophoneId);
            Assert.Equal("saved-video", PendingSavedDeviceId);
            Assert.Equal("saved-audio", PendingSavedAudioId);
            Assert.Equal("saved-microphone", PendingSavedMicrophoneId);
            Assert.Equal(41, ProbeGeneration);
            Assert.Equal(0, DeviceReplacements);
            Assert.Equal(0, AudioReplacements);
            Assert.Equal(0, ProbeStarts);
            Assert.Equal(0, PreviewStarts);
        }

        private object CreatePreviewController()
        {
            // This fixture exercises the real no-device preview branch, which never
            // reaches the session coordinator or native capture. Reinitialization is
            // unused because refresh starts preview with userInitiated: false.
            var context = Activator.CreateInstance(RequireType("Sussudio.Controllers.MainViewModelPreviewLifecycleControllerContext"))!;
            SetDelegate(context, "CreateReinitializeController", _ => RuntimeHelpers.GetUninitializedObject(
                RequireType("Sussudio.Controllers.MainViewModelPreviewReinitializeController")));
            SetDelegate(context, "SelectedDevice", _ => null);
            SetDelegate(context, "IsInitialized", _ => false);
            SetDelegate(context, "ShouldStartAudioPreview", _ => false);
            SetDelegate(context, "RaisePreviewStartRequested", _ => { PreviewStarts++; return null; });
            SetDelegate(context, "SetStatusText", _ => null);
            SetDelegate(context, "SetIsInitialized", _ => null);
            return Activator.CreateInstance(RequireType("Sussudio.Controllers.MainViewModelPreviewLifecycleController"), context)!;
        }
    }

    private sealed class FormatProbeFixture
    {
        private readonly object _controller;

        public FormatProbeFixture()
        {
            CachedFormat = Activator.CreateInstance(RequireType("Sussudio.Models.MediaFormat"))!;
            Set(CachedFormat, "Width", 3840u);
            Set(CachedFormat, "Height", 2160u);
            Set(CachedFormat, "FrameRate", 60d);
            Set(CachedFormat, "PixelFormat", "P010");
            Set(CachedFormat, "IsHdr", true);
            Get<IList>(Device, "SupportedFormats").Add(CachedFormat);
            Set(Device, "IsHdrCapable", true);
            SelectedDevice = Device;
            SelectedFormat = CachedFormat;

            var context = Activator.CreateInstance(RequireType("Sussudio.Controllers.MainViewModelDeviceFormatProbeControllerContext"))!;
            SetDelegate(context, "TryEnqueueOnUiThread", args => { ((Action)args[0]!)(); return true; });
            SetDelegate(context, "ReadDeviceScanGeneration", _ => 41L);
            SetDelegate(context, "FindDeviceById", args => (string)args[0]! == Get<string>(Device, "Id") ? Device : null);
            SetDelegate(context, "SetPendingSdrAutoSelectionForDeviceChange", args => { PendingAutoSelection = (bool)args[0]!; return null; });
            SetDelegate(context, "SetPendingSdrAutoFriendlyFrameRateBucket", args => { PendingFrameRateBucket = (int?)args[0]; return null; });
            SetDelegate(context, "GetSelectedDevice", _ => SelectedDevice);
            SetDelegate(context, "RebuildSelectedDeviceCapabilities", _ =>
            {
                CapabilityRebuilds++;
                SelectedFormat = null;
                SelectedResolution = null;
                SelectedFrameRate = 0;
                return null;
            });
            // Failure and unselected-device completion stop before retargeting.
            SetDelegate(context, "CreateRetargetApplier", _ => RuntimeHelpers.GetUninitializedObject(
                RequireType("Sussudio.Controllers.MainViewModelDeviceFormatProbeRetargetApplier")));
            _controller = Activator.CreateInstance(RequireType("Sussudio.Controllers.MainViewModelDeviceFormatProbeController"), context)!;
        }

        public object Device { get; } = CreateDevice("format-probe-device");
        public object CachedFormat { get; }
        public object? SelectedDevice { get; set; }
        public object? SelectedFormat { get; private set; }
        public string? SelectedResolution { get; private set; } = "3840x2160";
        public double SelectedFrameRate { get; private set; } = 60;
        public bool PendingAutoSelection { get; private set; } = true;
        public int? PendingFrameRateBucket { get; private set; } = 60;
        public int CapabilityRebuilds { get; private set; }

        public void Apply(object result)
            => _controller.GetType().GetMethod("OnDeviceFormatProbeCompleted")!.Invoke(_controller, [null, result]);
    }

    private static Type CaptureDeviceType => RequireType("Sussudio.Models.CaptureDevice");
    private static Type DiscoveryResultType => RequireType("Sussudio.Services.Capture.DeviceService+DeviceDiscoveryResult");
    private static Type RequireType(string name) => SussudioAssembly.Load().GetType(name, throwOnError: true)!;
    private static T Get<T>(object instance, string name) => (T)instance.GetType().GetProperty(name)!.GetValue(instance)!;
    private static void Set(object instance, string name, object value) => instance.GetType().GetProperty(name)!.SetValue(instance, value);

    private static object CreateDevice(string id)
    {
        var device = Activator.CreateInstance(CaptureDeviceType)!;
        CaptureDeviceType.GetProperty("Id")!.SetValue(device, id);
        CaptureDeviceType.GetProperty("Name")!.SetValue(device, id);
        return device;
    }

    private static object CreateDiscovery(string? error = null, object[]? devices = null)
        => Activator.CreateInstance(DiscoveryResultType,
            NewList(typeof(ObservableCollection<>).MakeGenericType(CaptureDeviceType), devices ?? []),
            Array.CreateInstance(RequireType("Sussudio.Models.AudioInputDevice"), 0),
            error)!;

    private static IList NewList(Type listType, params object[] items)
    {
        var list = (IList)Activator.CreateInstance(listType)!;
        foreach (var item in items) list.Add(item);
        return list;
    }

    private static object CreateService(Func<bool, Type, object> enumerate)
    {
        var serviceType = RequireType("Sussudio.Services.Capture.DeviceService");
        var constructor = serviceType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(ctor => ctor.GetParameters().Length == 2);
        var parameters = constructor.GetParameters();
        return constructor.Invoke(parameters.Select((parameter, index) =>
            (object)MakeDelegate(parameter.ParameterType, _ => enumerate(index == 1,
                parameter.ParameterType.GetMethod("Invoke")!.ReturnType))).ToArray());
    }

    private static object CreateProbeService(Func<Type, object> probe)
    {
        var serviceType = RequireType("Sussudio.Services.Capture.DeviceService");
        var constructor = serviceType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(ctor => ctor.GetParameters().Length == 3);
        return constructor.Invoke(constructor.GetParameters().Select((parameter, index) =>
            (object)MakeDelegate(parameter.ParameterType, _ =>
            {
                var taskType = parameter.ParameterType.GetMethod("Invoke")!.ReturnType;
                if (index == 2) return probe(taskType);
                if (index == 1) return EmptyListTask(taskType);
                var listType = taskType.GetGenericArguments()[0];
                var videoDevice = Activator.CreateInstance(listType.GetGenericArguments()[0], "Test capture", "format-probe-device")!;
                return TypedTask(listType, Task.FromResult((object)NewList(listType, videoDevice)));
            })).ToArray());
    }

    private static Task<object> ProbeResultAsync(object service, bool background)
        => background
            ? BackgroundProbeAsync(service, CreateDevice("format-probe-device"), 41)
            : InvokeResultAsync(service, "EnumerateCaptureDeviceDiscoveryAsync", true);

    private static async Task<object> BackgroundProbeAsync(object service, object device, long requestId)
    {
        var completion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        var probeEvent = service.GetType().GetEvent("FormatProbeCompleted")!;
        var handler = MakeDelegate(probeEvent.EventHandlerType!, args =>
        {
            completion.TrySetResult(args[1]!);
            return null;
        });
        probeEvent.AddEventHandler(service, handler);
        try
        {
            service.GetType().GetMethod("BeginBackgroundFormatProbe")!.Invoke(service, [device, requestId]);
            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            probeEvent.RemoveEventHandler(service, handler);
        }
    }

    private static object EmptyListTask(Type taskType)
    {
        var listType = taskType.GetGenericArguments()[0];
        return TypedTask(listType, Task.FromResult((object)NewList(listType)));
    }

    private static object FailedTask(Type taskType, Exception failure)
        => TypedTask(taskType.GetGenericArguments()[0], Task.FromException<object>(failure));

    private static object TypedTask(Type resultType, Task<object> task)
        => typeof(DeviceDiscoveryTests).GetMethod(nameof(ConvertTaskAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(resultType).Invoke(null, [task])!;

    private static async Task<T> ConvertTaskAsync<T>(Task<object> task) => (T)await task.ConfigureAwait(false);

    private static async Task<object> InvokeResultAsync(object instance, string method, params object[] arguments)
    {
        var task = (Task)instance.GetType().GetMethod(method)!.Invoke(instance, arguments)!;
        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    private static void SetDelegate(object context, string propertyName, Func<object?[], object?> body)
    {
        var property = context.GetType().GetProperty(propertyName)!;
        property.SetValue(context, MakeDelegate(property.PropertyType, body));
    }

    private static Delegate MakeDelegate(Type delegateType, Func<object?[], object?> body)
    {
        var invoke = delegateType.GetMethod("Invoke")!;
        var parameters = invoke.GetParameters().Select(parameter => Expression.Parameter(parameter.ParameterType)).ToArray();
        var arguments = Expression.NewArrayInit(typeof(object), parameters.Select(parameter => Expression.Convert(parameter, typeof(object))));
        var call = Expression.Invoke(Expression.Constant(body), arguments);
        Expression result = invoke.ReturnType == typeof(void)
            ? Expression.Block(call, Expression.Empty())
            : Expression.Convert(call, invoke.ReturnType);
        return Expression.Lambda(delegateType, result, parameters).Compile();
    }
}
