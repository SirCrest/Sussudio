using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

public sealed class RecordingSettingsRuntimeTests
{
    [Fact]
    public Task CoalescedRequestRetainsEarlierFieldsAndReportsSuperseded()
        => Program.RecordingSettings_CoalescedRequestRetainsEarlierFields();

    [Fact]
    public Task RebuildFailureRollsBackAndSameSelectionCanRetry()
        => Program.RecordingSettings_RebuildFailureRollsBackAndRetries();

    [Fact]
    public Task ApplicationReportsAcceptedDeferredAndCanceledStates()
        => Program.RecordingSettings_ReportsAcceptedDeferredAndCanceled();
}

static partial class Program
{
    internal static async Task RecordingSettings_CoalescedRequestRetainsEarlierFields()
    {
        var harness = CreateCaptureSessionCoordinatorHarness();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            SetPrivateField(harness.CaptureService, "_currentSettings", NewRecordingApplicationSettings("High", "Auto"));
            var blocker = EnqueueCoordinatorOperation(harness, "StartVideoPreview", async token =>
            {
                entered.SetResult();
                await release.Task.WaitAsync(token);
            });
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(3));

            var firstSelection = NewRecordingApplicationSelection("High", "TwoWay");
            var formatSelection = NewRecordingApplicationSelection("High", "TwoWay", "Av1Mp4");
            var lastSelection = NewRecordingApplicationSelection("Custom", "TwoWay", "Av1Mp4");
            var first = InvokeRecordingApplication(harness.Coordinator, firstSelection, "EncoderParameters");
            var format = InvokeRecordingApplication(harness.Coordinator, formatSelection, "RecordingFormat");
            var last = InvokeRecordingApplication(harness.Coordinator, lastSelection, "EncoderParameters");
            Assert.False(last.IsCompleted);
            release.SetResult();
            await blocker.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.Equal("Superseded", await RecordingApplicationOutcomeAsync(first));
            Assert.Equal("Deferred", await RecordingApplicationOutcomeAsync(format));
            Assert.Equal("Deferred", await RecordingApplicationOutcomeAsync(last));
            AssertRecordingApplicationSelection(lastSelection, GetPrivateField(harness.CaptureService, "_currentSettings")!);
            Assert.Equal(1L, GetLongProperty(GetCoordinatorSnapshot(harness.Coordinator), "CommandsCoalesced"));
        }
        finally
        {
            release.TrySetResult();
            await DisposeCaptureSessionCoordinatorHarnessAsync(harness);
        }
    }

    internal static async Task RecordingSettings_RebuildFailureRollsBackAndRetries()
    {
        object? backend = null;
        var desiredSelection = NewRecordingApplicationSelection("Custom", "TwoWay");
        var attempts = 0;
        var shouldFail = true;
        var failure = new InvalidOperationException("injected recording-settings rebuild failure");
        var service = NewRecordingApplicationService(_ =>
        {
            attempts++;
            if (shouldFail) return Task.FromException(failure);
            SetPropertyOrBackingField(backend!, "SettingsSnapshot", NewRecordingApplicationSettings("Custom", "TwoWay"));
            return Task.CompletedTask;
        });
        backend = GetPrivateField(service, "_flashbackBackend")!;
        var errors = new List<Exception>();
        ObserveCaptureErrors(service, errors.Add);
        try
        {
            // Desired settings already match the selection, but the backend does
            // not. A same-value request must repair that drift instead of no-op.
            SetPrivateField(service, "_currentSettings", NewRecordingApplicationSettings("Custom", "TwoWay"));
            SetPropertyOrBackingField(backend, "SettingsSnapshot", NewRecordingApplicationSettings("High", "Auto"));
            SetPropertyOrBackingField(backend, "Sink", RuntimeHelpers.GetUninitializedObject(RequireType("Sussudio.Services.Flashback.FlashbackEncoderSink")));
            var failed = InvokeRecordingApplication(service, desiredSelection, "EncoderParameters");
            Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => failed));
            Assert.Equal(1, attempts);
            AssertRecordingApplicationSelection(desiredSelection, GetPrivateField(service, "_currentSettings")!);
            Assert.Empty(errors);
            Assert.NotEqual("Faulted", GetPropertyValue(service, "SessionState")!.ToString());

            // A request changing every field also restores the entire previous
            // desired snapshot if rebuilding fails.
            var different = NewRecordingApplicationSelection("Medium", "ThreeWay", "Av1Mp4", 77, "P7");
            await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeRecordingApplication(service, different, "RecordingFormat"));
            AssertRecordingApplicationSelection(desiredSelection, GetPrivateField(service, "_currentSettings")!);

            shouldFail = false;
            Assert.Equal("Applied", await RecordingApplicationOutcomeAsync(InvokeRecordingApplication(service, desiredSelection, "EncoderParameters")));
            Assert.Equal(3, attempts);
            Assert.Equal("Applied", await RecordingApplicationOutcomeAsync(InvokeRecordingApplication(service, desiredSelection, "EncoderParameters")));
            Assert.Equal(3, attempts);
        }
        finally
        {
            // The sink above is an identity-only sentinel. Never let cleanup
            // invoke native methods on the uninitialized sentinel instance.
            SetPropertyOrBackingField(backend, "Sink", null);
            SetPropertyOrBackingField(backend, "SettingsSnapshot", null);
            await InvokeDisposeAsync(service);
        }
    }

    internal static async Task RecordingSettings_ReportsAcceptedDeferredAndCanceled()
    {
        var attempts = 0;
        var service = NewRecordingApplicationService(_ => { attempts++; return Task.CompletedTask; });
        var backend = GetPrivateField(service, "_flashbackBackend")!;
        var recordingBackend = GetPrivateField(service, "_recordingBackend")!;
        try
        {
            var selection = NewRecordingApplicationSelection("Custom", "ThreeWay");
            Assert.Equal("Accepted", await RecordingApplicationOutcomeAsync(InvokeRecordingApplication(service, selection, "EncoderParameters")));
            SetPrivateField(service, "_currentSettings", NewRecordingApplicationSettings("High", "Auto"));
            Assert.Equal("Deferred", await RecordingApplicationOutcomeAsync(InvokeRecordingApplication(service, selection, "EncoderParameters")));
            AssertRecordingApplicationSelection(selection, GetPrivateField(service, "_currentSettings")!);
            var activeSettings = NewRecordingApplicationSettings("Custom", "ThreeWay");
            var activeSink = RuntimeHelpers.GetUninitializedObject(RequireType("Sussudio.Services.Flashback.FlashbackEncoderSink"));
            SetPropertyOrBackingField(backend, "Sink", activeSink);
            SetPropertyOrBackingField(backend, "SettingsSnapshot", activeSettings);
            SetPropertyOrBackingField(recordingBackend, "Sink", activeSink);
            SetPropertyOrBackingField(recordingBackend, "SettingsSnapshot", activeSettings);
            SetPrivateField(service, "_isRecording", true);
            var duringRecording = NewRecordingApplicationSelection("Medium", "TwoWay");
            Assert.Equal("Deferred", await RecordingApplicationOutcomeAsync(InvokeRecordingApplication(service, duringRecording, "EncoderParameters")));
            AssertRecordingApplicationSelection(duringRecording, GetPrivateField(service, "_currentSettings")!);
            Assert.Equal(0, attempts);
            Assert.Equal(true, GetPrivateField(service, "_pendingFlashbackSettingsChange"));
            AssertRecordingApplicationSelection(selection, GetPropertyValue(recordingBackend, "SettingsSnapshot")!);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => InvokeRecordingApplication(service, selection, "EncoderParameters", cancellation.Token));
            AssertRecordingApplicationSelection(duringRecording, GetPrivateField(service, "_currentSettings")!);
        }
        finally
        {
            SetPrivateField(service, "_isRecording", false);
            SetPropertyOrBackingField(backend, "Sink", null);
            SetPropertyOrBackingField(backend, "SettingsSnapshot", null);
            SetPropertyOrBackingField(recordingBackend, "Sink", null);
            SetPropertyOrBackingField(recordingBackend, "SettingsSnapshot", null);
            await InvokeDisposeAsync(service);
        }
    }

    private static object NewRecordingApplicationService(Func<CancellationToken, Task> rebuild)
    {
        var constructor = RequireType("Sussudio.Services.Capture.CaptureService")
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(candidate => candidate.GetParameters().Length == 3);
        return constructor.Invoke(new object?[] { Activator.CreateInstance(RequireType("Sussudio.Services.Runtime.ProcessSupervisor")), null, rebuild });
    }

    private static object NewRecordingApplicationSelection(string quality, string split, string format = "HevcMp4", double bitrate = 55, string preset = "P4")
        => Activator.CreateInstance(RequireType("Sussudio.Models.RecordingSettingsSelection"),
            ParseEnum("Sussudio.Models.RecordingFormat", format), ParseEnum("Sussudio.Models.VideoQuality", quality), bitrate,
            ParseEnum("Sussudio.Models.NvencPreset", preset), ParseEnum("Sussudio.Models.SplitEncodeMode", split))!;

    private static object NewRecordingApplicationSettings(string quality, string split)
    {
        var settings = Activator.CreateInstance(RequireType("Sussudio.Models.CaptureSettings"))!;
        var selection = NewRecordingApplicationSelection(quality, split);
        selection.GetType().GetMethod("ApplyTo", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(selection, new[] { settings });
        return settings;
    }

    private static Task InvokeRecordingApplication(object target, object selection, string kind, CancellationToken cancellationToken = default)
        => (Task)target.GetType().GetMethod("ApplyRecordingSettingsAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(target, new[] { selection, ParseEnum("Sussudio.Models.RecordingSettingsChangeKind", kind), (object)cancellationToken })!;

    private static async Task<string> RecordingApplicationOutcomeAsync(Task task)
    {
        await task.WaitAsync(TimeSpan.FromSeconds(3));
        return GetPropertyValue(task, "Result")!.ToString()!;
    }

    private static void AssertRecordingApplicationSelection(object selection, object settings)
        => Assert.True((bool)selection.GetType().GetMethod("Matches", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(selection, new[] { settings })!);
}
