using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Xunit;

namespace Sussudio.Tests;

public sealed class DeviceAudioRequestControllerTests
{
    [Fact]
    public async Task RepeatedGainChangesCancelOlderWorkAndApplyOnlyTheLatestRequest()
    {
        var fixture = new Fixture();
        fixture.HandleGainChange();
        var olderSource = fixture.GetPendingSource("_gainXuDebounceCts");
        var olderToken = olderSource.Token;

        fixture.SelectedDevice = fixture.CreateDevice();
        var latestDevice = fixture.SelectedDevice;
        fixture.HandleGainChange();
        var latestToken = fixture.GetPendingSource("_gainXuDebounceCts").Token;

        Assert.True(olderSource.IsCancellationRequested);
        Assert.Equal(2, fixture.SaveSettingsCount);

        await EventuallyAsync(() => fixture.PendingUiOperations.Count == 1);
        await fixture.RunPendingUiOperationsAsync();

        var applied = Assert.Single(fixture.AppliedGainRequests);
        Assert.Equal(latestToken, applied.Token);
        Assert.Same(latestDevice, applied.Device);
        Assert.NotEqual(olderToken, applied.Token);
    }

    [Fact]
    public async Task RepeatedModeChangesCancelOlderWorkAndApplyOnlyTheLatestRequest()
    {
        var fixture = new Fixture();
        fixture.HandleModeChange("HDMI");
        var olderSource = fixture.GetPendingSource("_deviceAudioModeCts");
        var olderToken = olderSource.Token;

        fixture.SelectedDevice = fixture.CreateDevice();
        var latestDevice = fixture.SelectedDevice;
        fixture.HandleModeChange("Analog");
        var latestToken = fixture.GetPendingSource("_deviceAudioModeCts").Token;

        Assert.True(olderSource.IsCancellationRequested);
        Assert.Equal(2, fixture.SaveSettingsCount);
        Assert.Equal(2, fixture.PendingUiOperations.Count);

        await fixture.RunPendingUiOperationsAsync();

        var applied = Assert.Single(fixture.AppliedModeRequests);
        Assert.Equal(latestToken, applied.Token);
        Assert.Same(latestDevice, applied.Device);
        Assert.NotEqual(olderToken, applied.Token);
    }

    [Fact]
    public async Task CancelPendingAudioControlWorkCancelsEveryOutstandingRequest()
    {
        var fixture = new Fixture();
        fixture.HandleGainChange();
        var gainSource = fixture.GetPendingSource("_gainXuDebounceCts");
        fixture.HandleModeChange("Analog");
        var modeSource = fixture.GetPendingSource("_deviceAudioModeCts");
        fixture.RequestRefresh();
        var refreshSource = fixture.GetPendingSource("_deviceAudioRefreshCts");
        fixture.ScheduleFlashPersist();
        var flashSource = fixture.GetPendingSource("_gainFlashDebounceCts");

        fixture.CancelPendingAudioControlWork();

        Assert.True(gainSource.IsCancellationRequested);
        Assert.True(modeSource.IsCancellationRequested);
        Assert.True(refreshSource.IsCancellationRequested);
        Assert.True(flashSource.IsCancellationRequested);
        Assert.Equal(2, fixture.SaveSettingsCount);

        await fixture.RunPendingUiOperationsAsync();
        await EventuallyAsync(() => fixture.AllControllerRequestsRetired);

        Assert.Empty(fixture.AppliedGainRequests);
        Assert.Empty(fixture.AppliedModeRequests);
        Assert.Equal(0, fixture.RefreshCount);
        Assert.Equal(0, fixture.FlashPersistCount);
    }

    [Fact]
    public async Task FailedFlashPersistenceReportsStatusForTheCurrentDevice()
    {
        var fixture = new Fixture { FlashPersistResult = false };
        fixture.ScheduleFlashPersist();

        await EventuallyAsync(() => fixture.FlashPersistCount == 1 && fixture.PendingUiOperations.Count == 1);
        await fixture.RunPendingUiOperationsAsync();

        Assert.Equal("Analog gain applied but could not be saved to the device; it may revert after power cycle.", fixture.StatusText);
    }

    private static async Task EventuallyAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        while (!condition())
        {
            await Task.Delay(1, timeout.Token);
        }
    }

    private sealed class Fixture
    {
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly Type _controllerType;
        private readonly object _controller;
        private readonly object _context;
        private int _saveSettingsCount;
        private int _refreshCount;
        private int _flashPersistCount;

        public Fixture()
        {
            var assembly = SussudioAssembly.Load();
            var deviceType = assembly.GetType("Sussudio.Models.CaptureDevice", throwOnError: true)!;
            SelectedDevice = RuntimeHelpers.GetUninitializedObject(deviceType);

            var contextType = assembly.GetType("Sussudio.Controllers.MainViewModelDeviceAudioRequestControllerContext", throwOnError: true)!;
            _context = Activator.CreateInstance(contextType, nonPublic: true)!;
            SetCallback("EnqueueUiOperation", args =>
            {
                PendingUiOperations.Enqueue((Func<Task>)args[0]!);
                return true;
            });
            SetCallback("IsDisposing", _ => false);
            SetCallback("IsLoadingSettings", _ => false);
            SetCallback("IsRefreshingDeviceAudioControls", _ => false);
            SetCallback("IsDeviceAudioControlSupported", _ => true);
            SetCallback("IsRecording", _ => false);
            SetCallback("GetSelectedDeviceAudioMode", _ => "Analog");
            SetCallback("GetSelectedDevice", _ => SelectedDevice);
            SetCallback("SaveSettings", _ =>
            {
                Interlocked.Increment(ref _saveSettingsCount);
                return null;
            });
            SetCallback("RefreshDeviceAudioControlsAsync", args =>
            {
                ((CancellationToken)args[2]!).ThrowIfCancellationRequested();
                Interlocked.Increment(ref _refreshCount);
                return Task.CompletedTask;
            });
            SetCallback("ApplyDeviceAudioModeAsync", args =>
            {
                var token = (CancellationToken)args[2]!;
                token.ThrowIfCancellationRequested();
                AppliedModeRequests.Add((args[1]!, token));
                return Task.FromResult(true);
            });
            SetCallback("ApplyAnalogAudioGainAsync", args =>
            {
                var token = (CancellationToken)args[2]!;
                token.ThrowIfCancellationRequested();
                AppliedGainRequests.Add((args[1]!, token));
                return Task.FromResult(true);
            });
            SetCallback("PersistAnalogAudioGainAsync", args =>
            {
                ((CancellationToken)args[2]!).ThrowIfCancellationRequested();
                Interlocked.Increment(ref _flashPersistCount);
                return Task.FromResult(FlashPersistResult);
            });
            SetCallback("IsCurrentSelectedDevice", args => ReferenceEquals(args[0], SelectedDevice));
            SetCallback("SetStatusText", args =>
            {
                StatusText = (string)args[0]!;
                return null;
            });

            _controllerType = assembly.GetType("Sussudio.Controllers.MainViewModelDeviceAudioRequestController", throwOnError: true)!;
            _controller = Activator.CreateInstance(
                _controllerType,
                Instance,
                binder: null,
                args: new[] { _context },
                culture: null)!;
        }

        public object? SelectedDevice { get; set; }
        public ConcurrentQueue<Func<Task>> PendingUiOperations { get; } = new();
        public List<(object Device, CancellationToken Token)> AppliedModeRequests { get; } = new();
        public List<(object Device, CancellationToken Token)> AppliedGainRequests { get; } = new();
        public int SaveSettingsCount => Volatile.Read(ref _saveSettingsCount);
        public int RefreshCount => Volatile.Read(ref _refreshCount);
        public int FlashPersistCount => Volatile.Read(ref _flashPersistCount);
        public bool FlashPersistResult { get; set; } = true;
        public string? StatusText { get; private set; }
        public bool AllControllerRequestsRetired =>
            GetPendingSourceOrNull("_gainXuDebounceCts") is null &&
            GetPendingSourceOrNull("_deviceAudioModeCts") is null &&
            GetPendingSourceOrNull("_deviceAudioRefreshCts") is null &&
            GetPendingSourceOrNull("_gainFlashDebounceCts") is null;

        public object CreateDevice()
            => RuntimeHelpers.GetUninitializedObject(SelectedDevice!.GetType());

        public CancellationTokenSource GetPendingSource(string fieldName)
            => GetPendingSourceOrNull(fieldName)
               ?? throw new InvalidOperationException($"Controller field {fieldName} was not set.");

        public void HandleGainChange() => Invoke("HandleAnalogAudioGainPercentChanged", 75.0);
        public void HandleModeChange(string mode) => Invoke("HandleSelectedDeviceAudioModeChanged", mode);
        public void RequestRefresh() => Invoke("RequestDeviceAudioControlsRefresh", SelectedDevice);
        public void ScheduleFlashPersist() => Invoke("ScheduleAnalogGainFlashPersist", SelectedDevice!, (byte)0x55);
        public void CancelPendingAudioControlWork() => Invoke("CancelPendingAudioControlWork");

        public async Task RunPendingUiOperationsAsync()
        {
            while (PendingUiOperations.TryDequeue(out var operation))
            {
                await operation();
            }
        }

        private void SetCallback(string propertyName, Func<object?[], object?> callback)
        {
            var property = _context.GetType().GetProperty(propertyName, Instance)!;
            var invoke = property.PropertyType.GetMethod("Invoke")!;
            var parameters = invoke.GetParameters()
                .Select((parameter, index) => Expression.Parameter(parameter.ParameterType, $"arg{index}"))
                .ToArray();
            var arguments = Expression.NewArrayInit(
                typeof(object),
                parameters.Select(parameter => Expression.Convert(parameter, typeof(object))));
            var callbackCall = Expression.Invoke(Expression.Constant(callback), arguments);
            Expression body = invoke.ReturnType == typeof(void)
                ? Expression.Block(callbackCall, Expression.Empty())
                : Expression.Convert(callbackCall, invoke.ReturnType);
            property.SetValue(_context, Expression.Lambda(property.PropertyType, body, parameters).Compile());
        }

        private CancellationTokenSource? GetPendingSourceOrNull(string fieldName)
            => (CancellationTokenSource?)_controllerType.GetField(fieldName, Instance)!.GetValue(_controller);

        private void Invoke(string methodName, params object?[] arguments)
        {
            try
            {
                _controllerType.GetMethod(methodName, Instance)!.Invoke(_controller, arguments);
            }
            catch (TargetInvocationException error) when (error.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }
    }
}
