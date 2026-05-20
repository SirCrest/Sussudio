using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

static partial class Program
{
    internal static Task PreviewStartupWatchdogOwnership_LivesInFocusedController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var previewStartupText = ReadRepoFile("Sussudio/MainWindow.PreviewStartup.cs")
            .Replace("\r\n", "\n");
        var previewStartupWatchdogText = ReadRepoFile("Sussudio/MainWindow.PreviewStartup.cs")
            .Replace("\r\n", "\n");
        var previewStartupWatchdogControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Startup/PreviewStartupWatchdogController.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainWindowText, "InitializePreviewStartupWatchdogController();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.PreviewStartupWatchdog.cs")),
            "preview startup watchdog adapter is consolidated into the startup adapter");
        AssertContains(previewStartupWatchdogText, "private PreviewStartupWatchdogController _previewStartupWatchdogController = null!;");
        AssertContains(previewStartupWatchdogText, "private void InitializePreviewStartupWatchdogController()");
        AssertContains(previewStartupWatchdogText, "private void StartPreviewStartupWatchdog()");
        AssertContains(previewStartupWatchdogText, "=> _previewStartupWatchdogController.Start();");
        AssertContains(previewStartupWatchdogText, "private void StopPreviewStartupWatchdog()");
        AssertContains(previewStartupWatchdogText, "=> _previewStartupWatchdogController.Stop();");
        AssertContains(previewStartupWatchdogText, "private void SchedulePreviewStartupFailureStop(string reason)");
        AssertContains(previewStartupWatchdogText, "=> _previewStartupWatchdogController.ScheduleFailureStop(reason);");
        AssertContains(previewStartupWatchdogText, "private void ResetPreviewStartupFailureStopSchedule()");
        AssertContains(previewStartupWatchdogText, "=> _previewStartupWatchdogController.ResetFailureStopSchedule();");
        AssertContains(previewStartupWatchdogText, "private string BuildPreviewStartupTimeoutDiagnosticPayload()");
        AssertContains(previewStartupWatchdogControllerText, "internal sealed class PreviewStartupWatchdogControllerContext");
        AssertContains(previewStartupWatchdogControllerText, "internal sealed class PreviewStartupWatchdogController");
        AssertContains(previewStartupWatchdogControllerText, "private const int PreviewStartupDefaultVisualTimeoutMs = 10000;");
        AssertContains(previewStartupWatchdogControllerText, "private const int PreviewStartupMinVisualTimeoutMs = 1000;");
        AssertContains(previewStartupWatchdogControllerText, "private const int PreviewStartupMaxVisualTimeoutMs = 15000;");
        AssertContains(previewStartupWatchdogControllerText, "private readonly Lazy<int> _visualTimeoutMs = new(static () =>");
        AssertContains(previewStartupWatchdogControllerText, "private DispatcherQueueTimer? _watchdogTimer;");
        AssertContains(previewStartupWatchdogControllerText, "private DispatcherQueueTimer? _telemetryTimer;");
        AssertContains(previewStartupWatchdogControllerText, "private int _failureStopScheduled;");
        AssertContains(previewStartupWatchdogControllerText, "public int VisualTimeoutMs => _visualTimeoutMs.Value;");
        AssertContains(previewStartupWatchdogControllerText, "public void Start()");
        AssertContains(previewStartupWatchdogControllerText, "public void Stop()");
        AssertContains(previewStartupWatchdogControllerText, "public void ScheduleFailureStop(string reason)");
        AssertContains(previewStartupWatchdogControllerText, "public void ResetFailureStopSchedule()");
        AssertContains(previewStartupWatchdogControllerText, "private void TelemetryTimer_Tick(object? sender, object e)");
        AssertContains(previewStartupWatchdogControllerText, "private async void WatchdogTimer_Tick(object? sender, object e)");
        AssertContains(previewStartupWatchdogControllerText, "private Task HandleTimeoutAsync()");
        AssertContains(previewStartupWatchdogControllerText, "private static string FormatTimeoutReason(int timeoutMs, string? missingSignals)");
        AssertContains(previewStartupWatchdogControllerText, "private static string FormatTimeoutStatusText(string? missingSignals)");
        AssertContains(previewStartupWatchdogControllerText, "private static string FormatFailureStopStatusText(string reason)");
        AssertContains(previewStartupWatchdogControllerText, "var timeoutReason = FormatTimeoutReason(");
        AssertContains(previewStartupWatchdogControllerText, "FormatTimeoutStatusText(_context.GetMissingSignals())");
        AssertContains(previewStartupWatchdogControllerText, "FormatFailureStopStatusText(reason)");
        AssertContains(previewStartupWatchdogControllerText, "PREVIEW_START_WATCHDOG_STARTED");
        AssertContains(previewStartupWatchdogControllerText, "PREVIEW_START_TIMEOUT_IGNORED reason=user-or-shutdown-stop-requested");
        AssertContains(previewStartupWatchdogControllerText, "PREVIEW_START_TIMEOUT attempt={_context.GetAttemptLabel()}");
        AssertContains(previewStartupWatchdogControllerText, "PREVIEW_START_FAILURE_STOP begin");
        AssertEqual(
            false,
            File.Exists(Path.Combine(
                GetRepoRoot(),
                "Sussudio",
                "Controllers",
                "Preview",
                "Startup",
                "PreviewStartupFailureTextFormatter.cs")),
            "preview startup failure text formatter helper");
        AssertDoesNotContain(mainWindowText, "_previewStartupVisualTimeoutMs");
        AssertDoesNotContain(mainWindowText, "_previewStartupWatchdogTimer");
        AssertDoesNotContain(previewStartupWatchdogText, "DispatcherQueueTimer");
        AssertDoesNotContain(previewStartupWatchdogText, "Interlocked");
        AssertDoesNotContain(previewStartupWatchdogText, "EnvironmentHelpers.GetIntFromEnv");
        AssertDoesNotContain(previewStartupWatchdogText, "PreviewStartupFailureTextFormatter.FormatTimeoutReason(");
        AssertDoesNotContain(previewStartupWatchdogText, "PreviewStartupFailureTextFormatter.FormatTimeoutStatusText(");
        AssertDoesNotContain(previewStartupWatchdogText, "PreviewStartupFailureTextFormatter.FormatFailureStopStatusText(");
        AssertDoesNotContain(previewStartupWatchdogText, "private DispatcherQueueTimer? _previewStartupWatchdogTimer;");
        AssertDoesNotContain(previewStartupWatchdogText, "private DispatcherQueueTimer? _previewStartupTelemetryTimer;");
        AssertDoesNotContain(previewStartupWatchdogText, "private int _previewStartupFailureStopScheduled;");
        AssertDoesNotContain(previewStartupWatchdogText, "private Task HandlePreviewStartupTimeoutAsync()");
        AssertDoesNotContain(previewStartupText, "_previewStartupFailureStopScheduled");
        AssertEqual(
            true,
            previewStartupText.Split('\n').Length >= 100,
            "preview startup adapter is a substantial consolidated adapter file");
        AssertDoesNotContain(previewStartupText, "private Task HandlePreviewStartupTimeoutAsync()");
        AssertDoesNotContain(previewStartupText, "PreviewStartupFailureTextFormatter.FormatTimeoutReason(");
        AssertDoesNotContain(previewStartupText, "private const int PreviewStartupDefaultVisualTimeoutMs = 10000;");
        AssertDoesNotContain(previewStartupText, "no-visual-confirmation-within-{PreviewStartupVisualTimeoutMs}ms");
        AssertDoesNotContain(previewStartupText, "Preview failed to attach to UI (session started but no visual confirmation).");
        AssertDoesNotContain(previewStartupText, "Preview failed to start (missing readiness signal:");

        return Task.CompletedTask;
    }

    private static async Task PreviewStartupWatchdogController_PreservesTimeoutContracts()
    {
        var controllerType = RequireType("Sussudio.Controllers.PreviewStartupWatchdogController");
        var context = CreatePreviewStartupWatchdogContext(
            isWaitingForFirstVisual: () => true,
            isWindowClosing: () => false,
            isPreviewStopRequestedByUser: () => false,
            isPreviewing: () => true,
            getElapsedMilliseconds: () => 1234.0,
            buildMissingSignals: () => "FirstCaptureFrame+FirstVisual",
            buildTimeoutDiagnosticPayload: () => "placeholder=False gpuVisible=True cpuVisible=False strategy=D3D11VideoProcessor required=FirstCaptureFrame+FirstVisual received=None missing=FirstCaptureFrame+FirstVisual",
            out var recorder);
        var controller = Activator.CreateInstance(controllerType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, binder: null, args: new[] { context }, culture: null)!;

        var timeoutTask = InvokeNonPublicInstanceMethod(controller, "HandleTimeoutAsync", null) as Task
            ?? throw new InvalidOperationException("PreviewStartupWatchdogController.HandleTimeoutAsync did not return a Task.");
        await timeoutTask.ConfigureAwait(false);

        AssertEqual("FirstCaptureFrame+FirstVisual", recorder.MissingSignals, "timeout caches missing signals");
        AssertEqual("no-visual-confirmation-within-10000ms missing:FirstCaptureFrame+FirstVisual", recorder.FailureReason, "timeout failure reason");
        AssertEqual(true, recorder.OverlayStopped, "timeout stops startup overlay");
        AssertEqual("timeout", recorder.PlaybackSnapshotReasons.Single(), "timeout logs playback snapshot");
        AssertEqual("no-visual-confirmation-within-10000ms missing:FirstCaptureFrame+FirstVisual", recorder.StopPreviewReasons.Single(), "timeout forces teardown");
        AssertEqual(
            "Preview failed to start (missing readiness signal: FirstCaptureFrame+FirstVisual).",
            recorder.StatusTexts[0],
            "timeout status text");
        AssertEqual(
            "Preview startup failed: no-visual-confirmation-within-10000ms missing:FirstCaptureFrame+FirstVisual",
            recorder.StatusTexts[1],
            "failure stop status text");

        var ignoredContext = CreatePreviewStartupWatchdogContext(
            isWaitingForFirstVisual: () => true,
            isWindowClosing: () => true,
            isPreviewStopRequestedByUser: () => false,
            isPreviewing: () => true,
            getElapsedMilliseconds: () => 1.0,
            buildMissingSignals: () => "FirstVisual",
            buildTimeoutDiagnosticPayload: () => "ignored=true",
            out var ignoredRecorder);
        var ignoredController = Activator.CreateInstance(controllerType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, binder: null, args: new[] { ignoredContext }, culture: null)!;
        var ignoredTask = InvokeNonPublicInstanceMethod(ignoredController, "HandleTimeoutAsync", null) as Task
            ?? throw new InvalidOperationException("PreviewStartupWatchdogController.HandleTimeoutAsync did not return a Task.");
        await ignoredTask.ConfigureAwait(false);

        AssertEqual(0, ignoredRecorder.StatusTexts.Count, "ignored timeout does not publish status");
        AssertEqual(0, ignoredRecorder.StopPreviewReasons.Count, "ignored timeout does not stop preview");
        AssertEqual(null, ignoredRecorder.FailureReason, "ignored timeout does not mark failed");
    }

    private static Task PreviewStartupWatchdogController_GatesFailureStopScheduling()
    {
        var controllerType = RequireType("Sussudio.Controllers.PreviewStartupWatchdogController");
        var scheduledOperations = new List<(Func<Task> Operation, string Name)>();
        var context = CreatePreviewStartupWatchdogContext(
            isWaitingForFirstVisual: () => true,
            isWindowClosing: () => false,
            isPreviewStopRequestedByUser: () => false,
            isPreviewing: () => true,
            getElapsedMilliseconds: () => 1.0,
            buildMissingSignals: () => "FirstVisual",
            buildTimeoutDiagnosticPayload: () => "singleFlight=true",
            out _,
            runUiEventHandlerAsync: (operation, name) =>
            {
                scheduledOperations.Add((operation, name));
                return Task.CompletedTask;
            });
        var controller = Activator.CreateInstance(controllerType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, binder: null, args: new[] { context }, culture: null)!;
        var scheduleFailureStop = controllerType.GetMethod("ScheduleFailureStop", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PreviewStartupWatchdogController.ScheduleFailureStop was not found.");
        var resetFailureStopSchedule = controllerType.GetMethod("ResetFailureStopSchedule", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PreviewStartupWatchdogController.ResetFailureStopSchedule was not found.");

        scheduleFailureStop.Invoke(controller, new object[] { "first" });
        scheduleFailureStop.Invoke(controller, new object[] { "second" });
        AssertEqual(1, scheduledOperations.Count, "failure stop schedules once while pending");
        AssertEqual("PreviewStartupFailureStop", scheduledOperations[0].Name, "failure stop operation name");

        resetFailureStopSchedule.Invoke(controller, null);
        scheduleFailureStop.Invoke(controller, new object[] { "third" });
        AssertEqual(2, scheduledOperations.Count, "failure stop can schedule after reset");

        return Task.CompletedTask;
    }

    private static object CreatePreviewStartupWatchdogContext(
        Func<bool> isWaitingForFirstVisual,
        Func<bool> isWindowClosing,
        Func<bool> isPreviewStopRequestedByUser,
        Func<bool> isPreviewing,
        Func<double> getElapsedMilliseconds,
        Func<string> buildMissingSignals,
        Func<string> buildTimeoutDiagnosticPayload,
        out PreviewStartupWatchdogTestRecorder recorder,
        Func<Func<Task>, string, Task>? runUiEventHandlerAsync = null)
    {
        var contextType = RequireType("Sussudio.Controllers.PreviewStartupWatchdogControllerContext");
        var context = Activator.CreateInstance(contextType, nonPublic: true)!;
        recorder = new PreviewStartupWatchdogTestRecorder();
        var localRecorder = recorder;

        SetPropertyOrBackingField(context, "DispatcherQueue", null);
        SetPropertyOrBackingField(context, "IsWaitingForFirstVisual", isWaitingForFirstVisual);
        SetPropertyOrBackingField(context, "IsSignalWindowActive", new Func<bool>(() => true));
        SetPropertyOrBackingField(context, "IsWindowClosing", isWindowClosing);
        SetPropertyOrBackingField(context, "IsPreviewStopRequestedByUser", isPreviewStopRequestedByUser);
        SetPropertyOrBackingField(context, "IsPreviewing", isPreviewing);
        SetPropertyOrBackingField(context, "GetElapsedMilliseconds", getElapsedMilliseconds);
        SetPropertyOrBackingField(context, "GetAttemptLabel", new Func<string>(() => "attempt-test"));
        SetPropertyOrBackingField(context, "BuildMissingSignals", buildMissingSignals);
        SetPropertyOrBackingField(context, "GetMissingSignals", new Func<string?>(() => localRecorder.MissingSignals));
        SetPropertyOrBackingField(context, "SetMissingSignals", new Action<string?>(value => localRecorder.MissingSignals = value));
        SetPropertyOrBackingField(context, "MarkStartupFailed", new Action<string>(reason => localRecorder.FailureReason = reason));
        SetPropertyOrBackingField(context, "BuildTimeoutDiagnosticPayload", buildTimeoutDiagnosticPayload);
        SetPropertyOrBackingField(context, "LogPlaybackSnapshot", new Action<string>(reason => localRecorder.PlaybackSnapshotReasons.Add(reason)));
        SetPropertyOrBackingField(context, "StopStartupOverlay", new Action(() => localRecorder.OverlayStopped = true));
        SetPropertyOrBackingField(context, "SetStatusText", new Action<string>(value => localRecorder.StatusTexts.Add(value)));
        SetPropertyOrBackingField(context, "StopPreviewForFailureAsync", new Func<string, Task>(reason =>
        {
            localRecorder.StopPreviewReasons.Add(reason);
            return Task.CompletedTask;
        }));
        SetPropertyOrBackingField(
            context,
            "RunUiEventHandlerAsync",
            runUiEventHandlerAsync ?? new Func<Func<Task>, string, Task>((operation, _) => operation()));
        return context;
    }

    private sealed class PreviewStartupWatchdogTestRecorder
    {
        public string? MissingSignals { get; set; }
        public string? FailureReason { get; set; }
        public bool OverlayStopped { get; set; }
        public List<string> PlaybackSnapshotReasons { get; } = [];
        public List<string> StatusTexts { get; } = [];
        public List<string> StopPreviewReasons { get; } = [];
    }
}
