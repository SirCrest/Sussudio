using System.IO;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task PreviewStartupSignalsOwnership_LivesInFocusedControllers()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var previewStartupText = ReadMainWindowPreviewStartupAdapterSource();
        var previewStartupSignalsText = ReadMainWindowPreviewStartupAdapterSource();
        var previewStartupSignalCoordinatorText = ReadRepoFile("Sussudio/Controllers/Preview/Startup/PreviewStartupControllers.cs")
            .Replace("\r\n", "\n");
        var previewStartupReadinessSignalControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Startup/PreviewStartupControllers.cs")
            .Replace("\r\n", "\n");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "Startup", "PreviewStartupSignalCoordinator.cs")),
            "preview startup signal coordinator folded into PreviewStartupControllers.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "Startup", "PreviewStartupReadinessSignalController.cs")),
            "preview startup readiness controller folded into PreviewStartupControllers.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "Startup", "PreviewStartupSignalsController.cs")),
            "preview startup signals controller folded into PreviewStartupControllers.cs");

        AssertContains(mainWindowText, "InitializePreviewStartupSignalCoordinator();");
        AssertContains(previewStartupSignalsText, "private PreviewStartupSignalCoordinator _previewStartupSignalCoordinator = null!;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.PreviewStartup.Signals.cs")),
            "old marker-only preview startup signal partial removed");
        AssertContains(previewStartupSignalsText, "private PreviewStartupSignalCoordinator _previewStartupSignalCoordinator = null!;");
        AssertContains(previewStartupSignalsText, "private void InitializePreviewStartupSignalCoordinator()");
        AssertContains(previewStartupSignalsText, "IsSignalWindowActive = IsPreviewStartupSignalWindowActive,");
        AssertContains(previewStartupSignalsText, "ConfirmFirstVisual = ConfirmPreviewFirstVisual,");
        AssertContains(previewStartupSignalsText, "GetPlaybackSnapshotState = GetPreviewStartupPlaybackSnapshotState");
        AssertContains(previewStartupSignalsText, "private long PreviewStartupGpuPositionEventCount => _previewStartupSignalCoordinator.PositionEventCount;");
        AssertContains(previewStartupSignalsText, "private bool IsPreviewStartupSignalWindowActive()");
        AssertContains(previewStartupSignalsText, "=> _previewStartupSessionController.IsSignalWindowActive(ViewModel.IsPreviewing);");
        AssertContains(previewStartupSignalsText, "private void ResetPreviewSignalState()");
        AssertContains(previewStartupSignalsText, "private void ConfigurePreviewStartupSignals(PreviewStartupStrategy strategy, PreviewStartupSignalFlags requiredSignals)");
        AssertContains(previewStartupSignalsText, "private void LogPreviewStartupPlaybackSnapshot(string reason)");
        AssertContains(previewStartupSignalsText, "=> _previewStartupSignalCoordinator.BuildMissingSignals();");
        AssertContains(previewStartupSignalsText, "=> _previewStartupSignalCoordinator.Configure(strategy, requiredSignals);");
        AssertContains(previewStartupSignalsText, "=> _previewStartupSignalCoordinator.LogPlaybackSnapshot(reason);");
        AssertContains(previewStartupSignalsText, "new PreviewStartupPlaybackSnapshotState(");
        AssertContains(previewStartupSignalCoordinatorText, "internal sealed class PreviewStartupSignalCoordinatorContext");
        AssertContains(previewStartupSignalCoordinatorText, "internal sealed record PreviewStartupPlaybackSnapshotState(");
        AssertContains(previewStartupSignalCoordinatorText, "internal sealed class PreviewStartupSignalCoordinator");
        AssertContains(previewStartupSignalCoordinatorText, "private readonly PreviewStartupReadinessSignalController _readinessSignals = new();");
        AssertContains(previewStartupSignalCoordinatorText, "private bool _expectGpuDualSignals;");
        AssertContains(previewStartupSignalCoordinatorText, "private long _positionEventCount;");
        AssertContains(previewStartupSignalCoordinatorText, "public PreviewStartupReadinessSignalSnapshot Snapshot => _readinessSignals.Snapshot;");
        AssertContains(previewStartupSignalCoordinatorText, "public long PositionEventCount => Interlocked.Read(ref _positionEventCount);");
        AssertContains(previewStartupSignalCoordinatorText, "public void Configure(PreviewStartupStrategy strategy, PreviewStartupSignalFlags requiredSignals)");
        AssertContains(previewStartupSignalCoordinatorText, "public void MarkGpuStartupSignal(PreviewStartupSignalFlags signal, string signalName)");
        AssertContains(previewStartupSignalCoordinatorText, "public void MarkGpuStartupSignalPlaybackAdvancing(TimeSpan position)");
        AssertContains(previewStartupSignalCoordinatorText, "private void HandleGpuStartupSignalResult(PreviewStartupReadinessSignalResult? result, string signalName)");
        AssertContains(previewStartupSignalCoordinatorText, "private void TryConfirmFirstVisualFromGpuSignals(PreviewStartupReadinessSignalResult result)");
        AssertContains(previewStartupSignalCoordinatorText, "PREVIEW_START_STRATEGY");
        AssertContains(previewStartupSignalCoordinatorText, "PREVIEW_START_SIGNAL");
        AssertContains(previewStartupSignalCoordinatorText, "PREVIEW_START_WAITING");
        AssertContains(previewStartupSignalCoordinatorText, "PREVIEW_START_POSITION_IGNORED");
        AssertContains(previewStartupSignalCoordinatorText, "PREVIEW_START_POSITION_BASELINE");
        AssertContains(previewStartupSignalCoordinatorText, "PREVIEW_START_POSITION_CHECK");
        AssertContains(previewStartupSignalCoordinatorText, "PREVIEW_START_PLAYBACK_SNAPSHOT");
        AssertContains(previewStartupReadinessSignalControllerText, "internal sealed class PreviewStartupReadinessSignalController");
        AssertContains(previewStartupReadinessSignalControllerText, "public static readonly TimeSpan PlaybackAdvanceThreshold = TimeSpan.FromMilliseconds(33);");
        AssertContains(previewStartupReadinessSignalControllerText, "public PreviewStartupReadinessSignalSnapshot Snapshot => new(");
        AssertContains(previewStartupReadinessSignalControllerText, "public string Configure(");
        AssertContains(previewStartupReadinessSignalControllerText, "public PreviewStartupReadinessSignalResult MarkSignal(");
        AssertContains(previewStartupReadinessSignalControllerText, "public PreviewStartupPlaybackPositionResult TrackPlaybackPosition(");
        AssertContains(previewStartupReadinessSignalControllerText, "PreviewStartupSignalFormatter.FormatMissingSignals(");
        AssertContains(previewStartupSignalCoordinatorText, "PreviewStartupSignalFormatter.FormatSignalList(");
        AssertDoesNotContain(mainWindowText, "ResetPreviewSignalState()");
        AssertEqual(
            true,
            previewStartupText.Split('\n').Length >= 100,
            "preview startup adapter family remains a substantial adapter surface");
        AssertDoesNotContain(previewStartupSignalsText, "private readonly PreviewStartupReadinessSignalController");
        AssertDoesNotContain(previewStartupSignalsText, "private long _previewStartupPositionEventCount;");
        AssertDoesNotContain(previewStartupSignalsText, "_readinessSignals.TrackPlaybackPosition(");
        AssertDoesNotContain(previewStartupSignalsText, "PREVIEW_START_SIGNAL");
        AssertDoesNotContain(previewStartupSignalsText, "PREVIEW_START_WAITING");
        AssertDoesNotContain(previewStartupSignalsText, "private static string BuildPreviewStartupSignalList");
        AssertDoesNotContain(previewStartupSignalsText, "CurrentPreviewStartupState is PreviewStartupState.StartingSession");

        return Task.CompletedTask;
    }

    internal static Task PreviewStartupReadinessSignalController_PreservesSignalStateContracts()
    {
        var controllerType = RequireType("Sussudio.Controllers.PreviewStartupReadinessSignalController");
        var signalType = RequireType("Sussudio.Models.PreviewStartupSignalFlags");
        var strategyType = RequireType("Sussudio.Models.PreviewStartupStrategy");
        var statusType = RequireType("Sussudio.Controllers.PreviewStartupReadinessSignalStatus");
        var playbackStatusType = RequireType("Sussudio.Controllers.PreviewStartupPlaybackPositionStatus");

        var controller = Activator.CreateInstance(controllerType, nonPublic: true)!;
        var configure = controllerType.GetMethod("Configure", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PreviewStartupReadinessSignalController.Configure was not found.");
        var markSignal = controllerType.GetMethod("MarkSignal", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PreviewStartupReadinessSignalController.MarkSignal was not found.");
        var trackPlaybackPosition = controllerType.GetMethod("TrackPlaybackPosition", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PreviewStartupReadinessSignalController.TrackPlaybackPosition was not found.");
        var markFirstVisualConfirmed = controllerType.GetMethod("MarkFirstVisualConfirmed", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PreviewStartupReadinessSignalController.MarkFirstVisualConfirmed was not found.");
        var snapshotProperty = controllerType.GetProperty("Snapshot", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("PreviewStartupReadinessSignalController.Snapshot was not found.");

        object Signals(int value) => Enum.ToObject(signalType, value);
        object Strategy(string name) => Enum.Parse(strategyType, name);
        object Status(string name) => Enum.Parse(statusType, name);
        object PlaybackStatus(string name) => Enum.Parse(playbackStatusType, name);

        var requiredSignals = Signals(1 | 2 | 4);
        var initialMissing = configure.Invoke(controller, new object[] { Strategy("D3D11VideoProcessor"), requiredSignals, true, false })?.ToString();
        AssertEqual("MediaOpened+FirstCaptureFrame+PlaybackAdvancing", initialMissing, "initial missing readiness signals");

        var mediaOpened = markSignal.Invoke(controller, new object[] { Signals(1), true, false })!;
        AssertEqual(Status("Accepted"), GetPropertyValue(mediaOpened, "Status"), "media-opened accepted");
        AssertEqual("FirstCaptureFrame+PlaybackAdvancing", GetStringProperty(mediaOpened, "MissingSignals"), "media-opened missing signals");
        AssertEqual(false, GetBoolProperty(mediaOpened, "AllRequiredSignalsReceived"), "media-opened not ready");

        var mediaSnapshot = GetPropertyValue(mediaOpened, "Snapshot")!;
        AssertEqual(true, GetBoolProperty(mediaSnapshot, "GpuSignalMediaOpened"), "media-opened snapshot flag");
        AssertEqual(Signals(1), GetPropertyValue(mediaSnapshot, "ReceivedSignals"), "media-opened received flags");

        var duplicate = markSignal.Invoke(controller, new object[] { Signals(1), true, false })!;
        AssertEqual(Status("Duplicate"), GetPropertyValue(duplicate, "Status"), "duplicate media-opened status");

        var playback = trackPlaybackPosition.Invoke(controller, new object[] { TimeSpan.FromMilliseconds(40), true, false })!;
        AssertEqual(PlaybackStatus("BaselineCaptured"), GetPropertyValue(playback, "Status"), "playback baseline status");
        var playbackSignal = GetPropertyValue(playback, "SignalResult")!;
        AssertEqual(Status("Accepted"), GetPropertyValue(playbackSignal, "Status"), "playback advancing accepted");
        AssertEqual("FirstCaptureFrame", GetStringProperty(playbackSignal, "MissingSignals"), "playback advancing missing signals");

        var firstFrame = markSignal.Invoke(controller, new object[] { Signals(2), true, false })!;
        AssertEqual(Status("Accepted"), GetPropertyValue(firstFrame, "Status"), "first frame accepted");
        AssertEqual(true, GetBoolProperty(firstFrame, "AllRequiredSignalsReceived"), "all required readiness signals received");
        AssertEqual(string.Empty, GetStringProperty(firstFrame, "MissingSignals"), "no missing readiness signals");

        markFirstVisualConfirmed.Invoke(controller, Array.Empty<object>());
        var finalSnapshot = snapshotProperty.GetValue(controller)!;
        AssertEqual(Signals(1 | 2 | 4 | 8), GetPropertyValue(finalSnapshot, "ReceivedSignals"), "first visual signal preserved in received flags");

        return Task.CompletedTask;
    }

    internal static Task PreviewStartupSignalFormatter_PreservesSignalStrings()
    {
        var formatterType = RequireType("Sussudio.Controllers.PreviewStartupSignalFormatter");
        var signalType = RequireType("Sussudio.Models.PreviewStartupSignalFlags");
        var formatSignalList = formatterType.GetMethod("FormatSignalList", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewStartupSignalFormatter.FormatSignalList was not found.");
        var formatMissingSignals = formatterType.GetMethod("FormatMissingSignals", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewStartupSignalFormatter.FormatMissingSignals was not found.");

        object Signals(int value) => Enum.ToObject(signalType, value);

        AssertEqual("None", formatSignalList.Invoke(null, new[] { Signals(0) })?.ToString(), "no startup signals");
        AssertEqual("None", formatSignalList.Invoke(null, new[] { Signals(16) })?.ToString(), "unknown startup signals");
        AssertEqual(
            "MediaOpened+FirstCaptureFrame+PlaybackAdvancing+FirstVisual",
            formatSignalList.Invoke(null, new[] { Signals(1 | 2 | 4 | 8) })?.ToString(),
            "startup signal order");
        AssertEqual(
            "FirstCaptureFrame+FirstVisual",
            formatMissingSignals.Invoke(null, new object[] { Signals(1 | 2 | 4 | 8), Signals(1 | 4), false })?.ToString(),
            "missing startup signals");
        AssertEqual(
            string.Empty,
            formatMissingSignals.Invoke(null, new object[] { Signals(1 | 2), Signals(1 | 2), false })?.ToString(),
            "no missing required startup signals");
        AssertEqual(
            "FirstVisual",
            formatMissingSignals.Invoke(null, new object[] { Signals(0), Signals(0), false })?.ToString(),
            "first visual required when no explicit startup signals exist");
        AssertEqual(
            string.Empty,
            formatMissingSignals.Invoke(null, new object[] { Signals(0), Signals(0), true })?.ToString(),
            "first visual confirmed with no explicit startup signals");

        return Task.CompletedTask;
    }

    internal static Task PreviewStartupFailureTextFormatter_PreservesFailureStrings()
    {
        var watchdogType = RequireType("Sussudio.Controllers.PreviewStartupWatchdogController");
        var formatTimeoutReason = watchdogType.GetMethod("FormatTimeoutReason", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewStartupWatchdogController.FormatTimeoutReason was not found.");
        var formatTimeoutStatusText = watchdogType.GetMethod("FormatTimeoutStatusText", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewStartupWatchdogController.FormatTimeoutStatusText was not found.");
        var formatFailureStopStatusText = watchdogType.GetMethod("FormatFailureStopStatusText", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewStartupWatchdogController.FormatFailureStopStatusText was not found.");

        AssertEqual(
            "no-visual-confirmation-within-10000ms",
            formatTimeoutReason.Invoke(null, new object?[] { 10000, null })?.ToString(),
            "timeout reason without missing signals");
        AssertEqual(
            "no-visual-confirmation-within-10000ms",
            formatTimeoutReason.Invoke(null, new object?[] { 10000, string.Empty })?.ToString(),
            "timeout reason with empty missing signals");
        AssertEqual(
            "no-visual-confirmation-within-10000ms",
            formatTimeoutReason.Invoke(null, new object?[] { 10000, "   " })?.ToString(),
            "timeout reason with whitespace missing signals");
        AssertEqual(
            "no-visual-confirmation-within-10000ms missing:FirstCaptureFrame+FirstVisual",
            formatTimeoutReason.Invoke(null, new object?[] { 10000, "FirstCaptureFrame+FirstVisual" })?.ToString(),
            "timeout reason with missing signals");
        AssertEqual(
            "Preview failed to attach to UI (session started but no visual confirmation).",
            formatTimeoutStatusText.Invoke(null, new object?[] { null })?.ToString(),
            "timeout status without missing signals");
        AssertEqual(
            "Preview failed to attach to UI (session started but no visual confirmation).",
            formatTimeoutStatusText.Invoke(null, new object?[] { "   " })?.ToString(),
            "timeout status with whitespace missing signals");
        AssertEqual(
            "Preview failed to start (missing readiness signal: FirstCaptureFrame+FirstVisual).",
            formatTimeoutStatusText.Invoke(null, new object?[] { "FirstCaptureFrame+FirstVisual" })?.ToString(),
            "timeout status with missing signals");
        AssertEqual(
            "Preview startup failed: no-visual-confirmation-within-10000ms missing:FirstCaptureFrame+FirstVisual",
            formatFailureStopStatusText.Invoke(null, new object?[] { "no-visual-confirmation-within-10000ms missing:FirstCaptureFrame+FirstVisual" })?.ToString(),
            "failure stop status");

        return Task.CompletedTask;
    }

    internal static Task PreviewStartupWatchdogOwnership_LivesInFocusedController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var previewStartupText = ReadMainWindowPreviewStartupAdapterSource();
        var previewStartupWatchdogText = ReadMainWindowPreviewStartupAdapterSource();
        var previewStartupWatchdogControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Startup/PreviewStartupControllers.cs")
            .Replace("\r\n", "\n");
        var previewStartupSignalFormatterText = ReadRepoFile("Sussudio/Controllers/Preview/Startup/PreviewStartupControllers.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainWindowText, "InitializePreviewStartupWatchdogController();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.PreviewStartup.Watchdog.cs")),
            "preview startup watchdog adapter folded into the preview startup session adapter");
        AssertContains(previewStartupWatchdogText, "private PreviewStartupWatchdogController _previewStartupWatchdogController = null!;");
        AssertContains(previewStartupWatchdogText, "private void InitializePreviewStartupWatchdogController()");
        AssertContains(previewStartupWatchdogText, "IsWaitingForFirstVisual = () => _previewStartupSessionController.IsWaitingForFirstVisual,");
        AssertContains(previewStartupWatchdogText, "private void StartPreviewStartupWatchdog()");
        AssertContains(previewStartupWatchdogText, "=> _previewStartupWatchdogController.Start();");
        AssertContains(previewStartupWatchdogText, "private void StopPreviewStartupWatchdog()");
        AssertContains(previewStartupWatchdogText, "=> _previewStartupWatchdogController.Stop();");
        AssertContains(previewStartupWatchdogText, "private void SchedulePreviewStartupFailureStop(string reason)");
        AssertContains(previewStartupWatchdogText, "=> _previewStartupWatchdogController.ScheduleFailureStop(reason);");
        AssertContains(previewStartupWatchdogText, "private void ResetPreviewStartupFailureStopSchedule()");
        AssertContains(previewStartupWatchdogText, "=> _previewStartupWatchdogController.ResetFailureStopSchedule();");
        AssertContains(previewStartupWatchdogText, "GetTimeoutDiagnosticSnapshot = GetPreviewStartupTimeoutDiagnosticSnapshot,");
        AssertContains(previewStartupWatchdogText, "private PreviewStartupTimeoutDiagnosticSnapshot GetPreviewStartupTimeoutDiagnosticSnapshot()");
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
        AssertContains(previewStartupWatchdogControllerText, "PreviewStartupSignalFormatter.FormatTimeoutDiagnosticPayload(");
        AssertContains(previewStartupWatchdogControllerText, "_context.GetTimeoutDiagnosticSnapshot()");
        AssertContains(previewStartupWatchdogControllerText, "FormatTimeoutStatusText(_context.GetMissingSignals())");
        AssertContains(previewStartupWatchdogControllerText, "FormatFailureStopStatusText(reason)");
        AssertContains(previewStartupSignalFormatterText, "internal readonly record struct PreviewStartupTimeoutDiagnosticSnapshot");
        AssertContains(previewStartupSignalFormatterText, "public static string FormatTimeoutDiagnosticPayload(PreviewStartupTimeoutDiagnosticSnapshot snapshot)");
        AssertContains(previewStartupSignalFormatterText, "required={FormatSignalList(snapshot.RequiredSignals)}");
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
        AssertDoesNotContain(previewStartupWatchdogText, "CurrentPreviewStartupState == PreviewStartupState.WaitingForFirstVisual");
        AssertDoesNotContain(previewStartupWatchdogText, "placeholder={NoDevicePlaceholder.Visibility}");
        AssertDoesNotContain(previewStartupWatchdogText, "PreviewStartupSignalFormatter.FormatSignalList(_previewStartupRequiredSignals)");
        AssertDoesNotContain(previewStartupText, "_previewStartupFailureStopScheduled");
        AssertEqual(
            true,
            previewStartupText.Split('\n').Length >= 100,
            "preview startup adapter family remains a substantial adapter surface");
        AssertDoesNotContain(previewStartupText, "private Task HandlePreviewStartupTimeoutAsync()");
        AssertDoesNotContain(previewStartupText, "PreviewStartupFailureTextFormatter.FormatTimeoutReason(");
        AssertDoesNotContain(previewStartupText, "private const int PreviewStartupDefaultVisualTimeoutMs = 10000;");
        AssertDoesNotContain(previewStartupText, "no-visual-confirmation-within-{PreviewStartupVisualTimeoutMs}ms");
        AssertDoesNotContain(previewStartupText, "Preview failed to attach to UI (session started but no visual confirmation).");
        AssertDoesNotContain(previewStartupText, "Preview failed to start (missing readiness signal:");

        return Task.CompletedTask;
    }

    internal static async Task PreviewStartupWatchdogController_PreservesTimeoutContracts()
    {
        var controllerType = RequireType("Sussudio.Controllers.PreviewStartupWatchdogController");
        var formatterType = RequireType("Sussudio.Controllers.PreviewStartupSignalFormatter");
        var formatTimeoutDiagnosticPayload = formatterType.GetMethod("FormatTimeoutDiagnosticPayload", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("PreviewStartupSignalFormatter.FormatTimeoutDiagnosticPayload was not found.");
        var timeoutDiagnosticSnapshot = CreatePreviewStartupTimeoutDiagnosticSnapshot();
        AssertEqual(
            "placeholder=False gpuVisible=True cpuVisible=False strategy=D3D11VideoProcessor required=FirstCaptureFrame+FirstVisual received=None missing=FirstCaptureFrame+FirstVisual",
            formatTimeoutDiagnosticPayload.Invoke(null, new[] { timeoutDiagnosticSnapshot }),
            "timeout diagnostic payload formatting");

        var context = CreatePreviewStartupWatchdogContext(
            isWaitingForFirstVisual: () => true,
            isWindowClosing: () => false,
            isPreviewStopRequestedByUser: () => false,
            isPreviewing: () => true,
            getElapsedMilliseconds: () => 1234.0,
            buildMissingSignals: () => "FirstCaptureFrame+FirstVisual",
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
            out var ignoredRecorder);
        var ignoredController = Activator.CreateInstance(controllerType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, binder: null, args: new[] { ignoredContext }, culture: null)!;
        var ignoredTask = InvokeNonPublicInstanceMethod(ignoredController, "HandleTimeoutAsync", null) as Task
            ?? throw new InvalidOperationException("PreviewStartupWatchdogController.HandleTimeoutAsync did not return a Task.");
        await ignoredTask.ConfigureAwait(false);

        AssertEqual(0, ignoredRecorder.StatusTexts.Count, "ignored timeout does not publish status");
        AssertEqual(0, ignoredRecorder.StopPreviewReasons.Count, "ignored timeout does not stop preview");
        AssertEqual(null, ignoredRecorder.FailureReason, "ignored timeout does not mark failed");
    }

    internal static Task PreviewStartupWatchdogController_GatesFailureStopScheduling()
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
        SetPropertyOrBackingField(context, "GetTimeoutDiagnosticSnapshot", CreatePreviewStartupTimeoutDiagnosticSnapshotFactory());
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

    private static object CreatePreviewStartupTimeoutDiagnosticSnapshot()
    {
        var snapshotType = RequireType("Sussudio.Controllers.PreviewStartupTimeoutDiagnosticSnapshot");
        var strategyType = RequireType("Sussudio.Models.PreviewStartupStrategy");
        var signalsType = RequireType("Sussudio.Models.PreviewStartupSignalFlags");
        return Activator.CreateInstance(
            snapshotType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: new[]
            {
                "False",
                "True",
                "False",
                Enum.Parse(strategyType, "D3D11VideoProcessor"),
                Enum.Parse(signalsType, "FirstCaptureFrame, FirstVisual"),
                Enum.Parse(signalsType, "None"),
                "FirstCaptureFrame+FirstVisual",
            },
            culture: null)!;
    }

    private static Delegate CreatePreviewStartupTimeoutDiagnosticSnapshotFactory()
    {
        var snapshot = CreatePreviewStartupTimeoutDiagnosticSnapshot();
        var snapshotType = snapshot.GetType();
        var delegateType = typeof(Func<>).MakeGenericType(snapshotType);
        return Expression.Lambda(delegateType, Expression.Constant(snapshot, snapshotType)).Compile();
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

    internal static Task PreviewStartupLifecycleEventOwnership_LivesInFocusedController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var previewStartupText = ReadMainWindowPreviewStartupAdapterSource();
        var previewFadeInText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewFadeInControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var previewPropertyChangedText = ReadMainWindowPropertyChangedPreviewAdapterSource();
        var previewPropertyChangedHandler = ExtractMemberCode(previewPropertyChangedText, "TryHandlePreviewPropertyChangedAsync");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var previewReinitText = ReadMainWindowPreviewTransitionsAdapterSource();

        AssertContains(mainWindowText, "InitializePreviewLifecycleEventController();");
        AssertContains(previewFadeInText, "private PreviewFadeInController _previewFadeInController = null!;");
        AssertContains(previewFadeInText, "private void InitializePreviewFadeInController()");
        AssertContains(previewFadeInText, "private void SchedulePreviewFadeIn()");
        AssertContains(previewFadeInText, "private void StopPreviewFadeInTimer()");
        AssertContains(previewFadeInControllerText, "private const int PreviewFadeInFrameThreshold = 3;");
        AssertContains(previewFadeInControllerText, "private DispatcherQueueTimer? _timer;");
        AssertContains(previewFadeInControllerText, "public void Schedule()");
        AssertContains(previewFadeInControllerText, "public void Stop()");
        AssertContains(propertyChangedText, "TryHandlePreviewAsync = TryHandlePreviewPropertyChangedAsync,");
        AssertContains(previewPropertyChangedText, "_previewLifecycleEventController.TryHandlePropertyChangedAsync(propertyName);");
        AssertContains(previewPropertyChangedText, "_previewLifecycleEventController.HandlePreviewStartRequested();");
        AssertContains(previewPropertyChangedText, "_previewLifecycleEventController.HandlePreviewStopRequested();");
        AssertContains(previewPropertyChangedText, "private PreviewLifecycleEventController _previewLifecycleEventController = null!;");
        AssertContains(previewLifecycleControllerText, "await HandlePreviewingChangedAsync();");
        AssertContains(previewLifecycleControllerText, "_context.HandlePreviewReinitializingChanged();");
        AssertContains(previewLifecycleControllerText, "if (_context.ShouldBeginPreviewStartupAttempt())");
        AssertContains(previewLifecycleControllerText, "_stopRequestedByUser = _stopRequestedByUser || !_context.ViewModel.IsPreviewReinitializing;");
        AssertContains(previewLifecycleControllerText, "_context.StartPreviewStartupWatchdog();");
        AssertContains(previewLifecycleControllerText, "_context.ShowStopPreviewButtonPresentation();");
        AssertContains(previewLifecycleControllerText, "_context.ShowStartPreviewButtonPresentation();");
        AssertContains(previewLifecycleControllerText, "_context.ApplyHdrToggleEnabledState();");
        AssertDoesNotContain(previewPropertyChangedHandler, "ViewModel_PreviewReinitRequested(");
        AssertDoesNotContain(previewPropertyChangedHandler, "ViewModel_PreviewRendererStopRequested(");
        AssertDoesNotContain(previewPropertyChangedHandler, "HandlePreviewReinitializingChanged(");
        AssertDoesNotContain(previewReinitText, "renderer.StopRenderThread();");

        return Task.CompletedTask;
    }

    internal static Task PreviewStop_RampsAudioDownBeforePreviewTeardown()
    {
        var previewAudioFadeControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var previewReinitText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewPropertyChangedText = ReadMainWindowPropertyChangedPreviewAdapterSource();
        var previewPropertyChangedHandler = ExtractMemberCode(previewPropertyChangedText, "TryHandlePreviewPropertyChangedAsync");
        var previewVolumeTransitionText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.AudioState.cs")
            .Replace("\r\n", "\n");
        var audioVolumeTransitionText = ReadRepoFile("Sussudio/ViewModels/PreviewAudioTransitionControllers.cs")
            .Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureLifecycleControllers.cs")
            .Replace("\r\n", "\n");

        var previewButtonActionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var previewButtonClick = ExtractMemberCode(previewButtonActionControllerText, "TogglePreviewAsync");
        AssertContains(previewButtonClick, "var audioFadeOutTask = _context.StartPreviewAudioFadeOutAsync();");
        AssertContains(previewButtonClick, "var previewFadeOutTask = _context.AnimatePreviewOutAsync();");
        AssertContains(previewButtonClick, "await Task.WhenAll(audioFadeOutTask, previewFadeOutTask);");
        AssertOccursBefore(previewButtonClick, "await Task.WhenAll(audioFadeOutTask, previewFadeOutTask);", "await viewModel.StopPreviewAsync(userInitiated: true);");

        var uiFadeOut = ExtractMemberCode(previewAudioFadeControllerText, "StartFadeOutAsync");
        AssertContains(uiFadeOut, "_context.ViewModel.VolumeSaveOverride = volumeTarget;");
        AssertContains(uiFadeOut, "To = 0,");
        AssertContains(uiFadeOut, "_context.ViewModel.PreviewVolume = 0;");
        AssertContains(uiFadeOut, "PREVIEW_AUDIO_FADE_OUT_STARTED");

        var vmStopRamp = ExtractMemberCode(previewVolumeTransitionText, "RampPreviewVolumeDownForStopAsync");
        AssertContains(vmStopRamp, "_previewAudioVolumeTransitionController.RampDownForStopAsync(cancellationToken)");

        var vmRampDown = ExtractMemberCode(audioVolumeTransitionText, "RampDownForAudioTransitionAsync");
        AssertContains(vmRampDown, "VolumeSaveOverride = persistedVolume;");
        AssertContains(vmRampDown, "_context.SetPreviewVolume(startingVolume * eased);");
        AssertContains(vmRampDown, "_context.SetPreviewVolume(0);");

        var stopPreview = ExtractTextBetween(
            previewLifecycleControllerText,
            "public async Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)",
            "\n}\n");
        AssertContains(stopPreview, "await _context.RampPreviewVolumeDownForStopAsync(cancellationToken);");
        AssertOccursBefore(stopPreview, "await _context.RampPreviewVolumeDownForStopAsync(cancellationToken);", "_context.RaisePreviewStopRequested();");
        AssertOccursBefore(stopPreview, "await _context.RampPreviewVolumeDownForStopAsync(cancellationToken);", "await _context.SessionCoordinator.StopAudioPreviewAsync(cancellationToken);");

        AssertDoesNotContain(previewPropertyChangedHandler, "ViewModel_PreviewRendererStopRequested(");
        var previewReinitStop = ExtractMemberCode(previewReinitText, "ViewModel_PreviewRendererStopRequested");
        AssertContains(previewReinitStop, "=> _previewRendererHostController.StopRendererForReinitTeardownAsync();");
        AssertDoesNotContain(previewReinitStop, "renderer.StopRenderThread();");

        return Task.CompletedTask;
    }

    internal static Task PreviewStartup_BeginsDeviceDiscoveryBeforeRecordingCapabilityProbesFinish()
    {
        var settingsText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.SettingsPersistence.cs")
            .Replace("\r\n", "\n");
        var recordingRuntimeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var recordingCapabilityControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs")
            .Replace("\r\n", "\n");
        var rootViewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var controllerGraphText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelControllerGraph.cs")
            .Replace("\r\n", "\n");
        var deviceRefreshControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelDeviceControllers.cs")
            .Replace("\r\n", "\n");

        var initialize = ExtractMemberCode(settingsText, "InitializeAsync");
        AssertContains(initialize, "LoadSettings();");
        AssertContains(initialize, "StartRecordingCapabilityRefresh();");
        AssertContains(initialize, "return Task.CompletedTask;");
        AssertDoesNotContain(initialize, "await Task.WhenAll");
        AssertOccursBefore(initialize, "LoadSettings();", "StartRecordingCapabilityRefresh();");

        var startupRefresh = ExtractMemberCode(recordingCapabilityControllerText, "Start");
        AssertContains(startupRefresh, "TrackStartupRefreshTask(RefreshRecordingFormatCapabilitiesAsync(), \"recording formats\");");
        AssertContains(startupRefresh, "TrackStartupRefreshTask(RefreshSplitEncodeCapabilitiesAsync(), \"split encode modes\");");
        AssertDoesNotContain(settingsText, "private void StartRecordingCapabilityRefresh()");
        AssertDoesNotContain(recordingCapabilityControllerText, "private void StartRecordingCapabilityRefresh()");
        AssertContains(recordingRuntimeText, "private void StartRecordingCapabilityRefresh()");
        AssertContains(recordingRuntimeText, "=> _recordingCapabilityController.Start();");

        var recordingFormatRefresh = ExtractMemberCode(recordingCapabilityControllerText, "RefreshRecordingFormatCapabilitiesAsync");
        AssertContains(recordingFormatRefresh, "support.HasH264Nvenc");
        AssertContains(recordingFormatRefresh, "support.HasHevcNvenc");
        AssertContains(recordingFormatRefresh, "support.HasAv1Nvenc");
        AssertDoesNotContain(recordingFormatRefresh, "support.HasAv1)");

        var splitEncodeRefresh = ExtractMemberCode(recordingCapabilityControllerText, "RefreshSplitEncodeCapabilitiesAsync");
        AssertContains(splitEncodeRefresh, "if (!support.Supports2Way)");
        AssertContains(splitEncodeRefresh, "modes.Remove(\"2-way\");");
        AssertContains(splitEncodeRefresh, "if (!support.Supports3Way)");
        AssertContains(splitEncodeRefresh, "modes.Remove(\"3-way\");");
        AssertContains(splitEncodeRefresh, "_context.SetSelectedSplitEncodeMode(\"Auto\");");

        AssertContains(rootViewModelText, "=> _deviceRefreshController.RefreshDevicesAsync(cancellationToken);");
        AssertContains(controllerGraphText, "var deviceRefreshController = CreateDeviceRefreshController(viewModel, previewLifecycleController);");
        AssertContains(controllerGraphText, "viewModel._deviceService.EnumerateCaptureDeviceDiscoveryAsync(waitForFormatProbes: false)");

        var refreshDevices = ExtractMemberCode(deviceRefreshControllerText, "RefreshDevicesAsync");
        AssertContains(refreshDevices, "var discovery = await _context.EnumerateCaptureDeviceDiscoveryAsync()");
        AssertContains(refreshDevices, "ApplyStartupAudioDeviceScan(");
        AssertOccursBefore(refreshDevices, "_context.EnumerateCaptureDeviceDiscoveryAsync()", "ApplyStartupAudioDeviceScan(");
        AssertOccursBefore(refreshDevices, "ApplyStartupAudioDeviceScan(", "_context.ReplaceDevices(devices.ToList());");
        AssertOccursBefore(refreshDevices, "_context.ReplaceDevices(devices.ToList());", "_context.BeginBackgroundFormatProbe(discoveredDevice, scanGeneration);");
        AssertOccursBefore(refreshDevices, "_context.BeginBackgroundFormatProbe(discoveredDevice, scanGeneration);", "ApplySuccessfulDeviceScanAsync(");
        var successfulScan = ExtractTextBetween(
            deviceRefreshControllerText,
            "private async Task ApplySuccessfulDeviceScanAsync",
            "\n    }\n}");
        AssertOccursBefore(successfulScan, "var savedDeviceId = _context.GetPendingSavedDeviceId();", "await _previewLifecycleController.StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertOccursBefore(successfulScan, "_context.SetSelectedDevice(nextSelectedDevice);", "await _previewLifecycleController.StartPreviewAsync(userInitiated: false, cancellationToken);");
        AssertOccursBefore(refreshDevices, "_context.EnumerateCaptureDeviceDiscoveryAsync()", "ApplySuccessfulDeviceScanAsync(");

        return Task.CompletedTask;
    }

    internal static Task PreviewStartup_PrimesUiAndAudioBeforePreviewReveal()
    {
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var audioBindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var audioControlBindingControllerText = ReadRepoFile("Sussudio/Controllers/Audio/AudioControlBindingController.cs")
            .Replace("\r\n", "\n");
        var previewActionsText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewFadeInText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewFadeInControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var previewAudioFadeText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewAudioFadeControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var previewTransitionText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewTransitionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var launchEntranceShellText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchFlowController.cs")
            .Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var previewPropertyChangedText = ReadMainWindowPropertyChangedPreviewAdapterSource();
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadMainWindowShellChromeAdapterSource();
        var launchStartupText = ReadRepoFile("Sussudio/Controllers/Launch/LaunchFlowController.cs")
            .Replace("\r\n", "\n");
        var xamlText = ReadRepoFile("Sussudio/MainWindow.xaml")
            .Replace("\r\n", "\n");

        AssertContains(propertyChangedText, "TryHandlePreviewAsync = TryHandlePreviewPropertyChangedAsync,");
        AssertContains(previewPropertyChangedText, "_previewLifecycleEventController.TryHandlePropertyChangedAsync(propertyName);");
        AssertContains(previewLifecycleControllerText, "await HandlePreviewingChangedAsync();");

        var previewStartRequested = ExtractMemberCode(previewLifecycleControllerText, "HandlePreviewStartRequested");
        AssertContains(previewStartRequested, "_context.BeginPreviewStartupAttempt();");
        AssertContains(previewStartRequested, "_context.PrimePreviewAudioFadeIn();");
        AssertContains(previewStartRequested, "_context.PreparePreviewStartupPresentation();");
        AssertOccursBefore(previewStartRequested, "_context.PrimePreviewAudioFadeIn();", "_context.PreparePreviewStartupPresentation();");

        var playEntranceAnimation = ExtractMemberCode(launchEntranceShellText, "PlayEntranceAnimation");
        AssertContains(playEntranceAnimation, "LAUNCH_PREVIEW_REVEAL_DEFERRED");
        AssertContains(playEntranceAnimation, "_context.AddPreviewShellEntranceAnimations(storyboard, easing, 900, 400);");
        AssertDoesNotContain(playEntranceAnimation, "Storyboard.SetTarget(volumeAnim, PreviewVolumeSlider);");

        var animatePreviewInAdapter = ExtractMemberCode(previewTransitionText, "AnimatePreviewInAsync");
        AssertContains(animatePreviewInAdapter, "_previewTransitionAnimationController.AnimatePreviewInAsync();");

        var animatePreviewIn = ExtractMemberCode(previewTransitionControllerText, "AnimatePreviewInAsync");
        AssertContains(animatePreviewIn, "_context.FadeInVideoFrameShadow(0, 400);");
        AssertContains(animatePreviewIn, "AnimatePreviewShellInAsync(350)");
        AssertContains(animatePreviewIn, "AnimatePreviewTransitionAsync(1.0, 1.0, 250, EasingMode.EaseOut)");
        AssertOccursBefore(animatePreviewIn, "_context.FadeInVideoFrameShadow(0, 400);", "AnimatePreviewShellInAsync(350)");

        var preparePresentation = ExtractMemberCode(previewTransitionControllerText, "PrepareStartupPresentation");
        AssertContains(preparePresentation, "FadeOutElement(_context.NoDevicePlaceholder);");
        AssertContains(preparePresentation, "_context.StartPreviewStartupOverlay();");
        AssertContains(preparePresentation, "_context.PreviewContentGrid.Opacity = 0.0;");

        var revealUnavailable = ExtractMemberCode(previewTransitionControllerText, "RevealUnavailablePlaceholder");
        AssertContains(revealUnavailable, "AnimatePreviewShellInAsync(300)");
        AssertContains(revealUnavailable, "FadeInElement(_context.NoDevicePlaceholder);");

        var primeAudioAdapter = ExtractMemberCode(previewAudioFadeText, "PrimePreviewAudioFadeIn");
        AssertContains(primeAudioAdapter, "_previewAudioFadeController.PrimeFadeIn();");

        var primeAudio = ExtractMemberCode(previewAudioFadeControllerText, "PrimeFadeIn");
        AssertContains(primeAudio, "_context.ViewModel.VolumeSaveOverride = volumeTarget;");
        AssertContains(primeAudio, "_context.ViewModel.PreviewVolume = 0;");
        AssertContains(primeAudio, "_context.PreviewVolumeSlider.Value = 0;");

        var startAudioFadeAdapter = ExtractMemberCode(previewAudioFadeText, "StartPreviewAudioFadeIn");
        AssertContains(startAudioFadeAdapter, "_previewAudioFadeController.StartFadeIn(durationMs);");

        var startAudioFade = ExtractMemberCode(previewAudioFadeControllerText, "StartFadeIn");
        AssertContains(startAudioFade, "Storyboard.SetTarget(volumeAnimation, _context.PreviewVolumeSlider);");
        AssertContains(startAudioFade, "CompleteFadeIn(applyTarget: true)");

        AssertContains(previewFadeInText, "=> _previewFadeInController.Schedule();");
        var schedulePreviewFadeIn = ExtractMemberCode(previewFadeInControllerText, "Schedule");
        AssertContains(schedulePreviewFadeIn, "StartPreviewAudioFadeIn();");
        AssertOccursBefore(schedulePreviewFadeIn, "_ = _context.AnimatePreviewInAsync();", "_context.StartPreviewAudioFadeIn();");

        var setupBindings = ExtractMemberCode(bindingsText, "SetupBindings");
        AssertContains(setupBindings, "ApplyInitialAudioControlBindings();");

        var initialAudioBindingsAdapter = ExtractMemberCode(audioBindingsText, "ApplyInitialAudioControlBindings");
        AssertContains(initialAudioBindingsAdapter, "_audioControlBindingController.ApplyInitialAudioControlBindings();");

        var initialAudioBindings = ExtractMemberCode(audioControlBindingControllerText, "ApplyInitialAudioControlBindings");
        AssertContains(initialAudioBindings, "_context.PrimePreviewAudioFadeIn();");
        AssertContains(initialAudioBindings, "_context.CancelPreviewAudioFadeInForUser();");
        AssertOccursBefore(initialAudioBindings, "_context.PrimePreviewAudioFadeIn();", "_context.PreviewVolumeSlider.ValueChanged +=");

        var previewButtonClick = ExtractMemberCode(previewActionsText, "PreviewButton_Click");
        AssertContains(previewButtonClick, "RunUiEventHandlerAsync(() => TogglePreviewFromButtonAsync(), nameof(PreviewButton_Click))");
        var previewButtonActionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var togglePreviewAsync = ExtractMemberCode(previewButtonActionControllerText, "TogglePreviewAsync");
        AssertContains(togglePreviewAsync, "if (!viewModel.IsPreviewing)\n        {\n            _context.RevealPreviewUnavailablePlaceholder();\n        }");

        var mainWindowLoaded = ExtractMemberCode(startupText, "MainWindow_Loaded");
        AssertContains(mainWindowLoaded, "=> _launchStartupController.HandleLoaded(nameof(MainWindow_Loaded));");
        var launchLoaded = ExtractMemberCode(launchStartupText, "HandleLoaded");
        AssertOccursBefore(launchLoaded, "_context.PrimePreviewAudioFadeIn();", "await _context.RefreshDevicesAsync();");
        AssertContains(launchLoaded, "_context.RevealPreviewUnavailablePlaceholder();");

        AssertDoesNotContain(xamlText, "No preview available");

        return Task.CompletedTask;
    }
    internal static Task PreviewStartupSessionReinitOwnership_LivesInFocusedControllers()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var previewStartupText = ReadMainWindowPreviewStartupAdapterSource();
        var previewStartupSessionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Startup/PreviewStartupControllers.cs")
            .Replace("\r\n", "\n");
        var previewReinitText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewReinitTransitionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var previewRendererText = ReadMainWindowPreviewRendererAdapterSource();
        var previewRuntimeSnapshotText = previewRendererText;
        var previewRuntimeSnapshotSamplingControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotControllers.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainWindowText, "InitializePreviewStartupSessionController();");
        AssertContains(mainWindowText, "InitializePreviewReinitTransitionController();");
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Composition.cs")),
            "preview reinit adapter lives in the preview transitions composition partial");
        AssertContains(previewStartupText, "private PreviewStartupSessionController _previewStartupSessionController = null!;");
        AssertContains(previewStartupText, "private void InitializePreviewStartupSessionController()");
        AssertContains(previewStartupText, "private PreviewStartupState CurrentPreviewStartupState");
        AssertContains(previewStartupText, "private string PreviewStartupAttemptLabel");
        AssertContains(previewStartupText, "private bool ShouldBeginPreviewStartupAttempt");
        AssertContains(previewStartupText, "new PreviewStartupSessionControllerContext");
        AssertContains(previewStartupText, "ResetSignalState = ResetPreviewSignalState,");
        AssertContains(previewStartupText, "StopWatchdog = StopPreviewStartupWatchdog,");
        AssertContains(previewStartupText, "ScheduleFadeIn = SchedulePreviewFadeIn,");
        AssertContains(previewStartupText, "=> _previewStartupSessionController.SetStartupState(state, reason);");
        AssertContains(previewStartupText, "=> _previewStartupSessionController.BeginStartupAttempt();");
        AssertContains(previewStartupText, "=> _previewStartupSessionController.ConfirmFirstVisual(source);");
        AssertContains(previewStartupText, "=> _previewStartupSessionController.ResetStartupTracking(keepRecoveryCount, preserveReinitAnimation);");
        AssertContains(previewStartupSessionControllerText, "internal enum PreviewStartupState");
        AssertContains(previewStartupSessionControllerText, "internal sealed class PreviewStartupSessionControllerContext");
        AssertContains(previewStartupSessionControllerText, "internal sealed class PreviewStartupSessionController");
        AssertContains(previewStartupSessionControllerText, "public PreviewStartupState State { get; private set; } = PreviewStartupState.Idle;");
        AssertContains(previewStartupSessionControllerText, "public string? AttemptId { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public DateTimeOffset? RequestedUtc { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public DateTimeOffset? RendererAttachedUtc { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public DateTimeOffset? FirstVisualUtc { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public string? LastFailureReason { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public string? MissingSignals { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public int RecoveryAttemptCount { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public bool FirstVisualConfirmed { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public bool ShouldRefreshMissingSignalsForSnapshot => IsWaitingForFirstVisual || IsFailed;");
        AssertContains(previewStartupSessionControllerText, "public bool ShouldBeginAttempt => string.IsNullOrWhiteSpace(AttemptId) || IsFailed || IsIdle;");
        AssertContains(previewStartupSessionControllerText, "public bool IsSignalWindowActive(bool isPreviewing)");
        AssertContains(previewStartupSessionControllerText, "public string AttemptLabel => AttemptId ?? \"none\";");
        AssertContains(previewStartupSessionControllerText, "public void BeginStartupAttempt()");
        AssertContains(previewStartupSessionControllerText, "public void SetStartupState(PreviewStartupState state, string? reason = null)");
        AssertContains(previewStartupSessionControllerText, "public void ConfirmFirstVisual(string source)");
        AssertContains(previewStartupSessionControllerText, "public void ResetStartupTracking(bool keepRecoveryCount = false, bool preserveReinitAnimation = false)");
        AssertContains(previewStartupSessionControllerText, "PREVIEW_START_STATE state={state} attempt={AttemptLabel}");
        AssertContains(previewStartupSessionControllerText, "PREVIEW_START_REQUESTED attempt={AttemptId}");
        AssertContains(previewStartupSessionControllerText, "PREVIEW_FIRST_VISUAL_IGNORED attempt={AttemptLabel}");
        AssertContains(previewStartupSessionControllerText, "PREVIEW_FIRST_VISUAL_CONFIRMED attempt={AttemptLabel}");
        AssertContains(previewStartupSessionControllerText, "public void MarkRendererAttached(DateTimeOffset attachedUtc)");
        AssertContains(previewStartupSessionControllerText, "public bool MarkFirstVisualConfirmed(DateTimeOffset firstVisualUtc)");
        AssertContains(previewStartupSessionControllerText, "public void SetMissingSignals(string? missingSignals)");
        AssertContains(previewRuntimeSnapshotText, "StartupSessionController = _previewStartupSessionController,");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "StartupState = startupSession.State.ToString(),");
        AssertContains(previewReinitText, "private PreviewReinitTransitionController _previewReinitTransitionController = null!;");
        AssertContains(previewReinitText, "private bool IsPreviewReinitAnimating");
        AssertContains(previewReinitText, "=> _previewReinitTransitionController.IsAnimating;");
        AssertContains(previewReinitText, "private async Task ViewModel_PreviewReinitRequested(string reason)");
        AssertContains(previewReinitText, "private Task ViewModel_PreviewRendererStopRequested()");
        AssertContains(previewReinitText, "private void HandlePreviewReinitializingChanged()");
        AssertContains(previewReinitText, "=> _previewReinitTransitionController.HandleReinitializingChanged(");
        AssertContains(previewReinitText, "new PreviewReinitCompletionPresentationContext");
        AssertContains(previewReinitText, "IsPreviewReinitializing = ViewModel.IsPreviewReinitializing,");
        AssertContains(previewReinitText, "IsPreviewing = ViewModel.IsPreviewing,");
        AssertContains(previewReinitText, "IsFirstVisualConfirmed = IsPreviewFirstVisualConfirmed,");
        AssertContains(previewReinitText, "AttemptLabel = PreviewStartupAttemptLabel,");
        AssertContains(previewReinitText, "CallerName = nameof(HandleViewModelPropertyChangedAsync),");
        AssertContains(previewReinitText, "UpdateDeviceApplyButtonState = UpdateDeviceApplyButtonState,");
        AssertContains(previewReinitText, "RevealUnavailablePlaceholder = RevealPreviewUnavailablePlaceholder,");
        AssertContains(previewReinitText, "StopPreviewStartupOverlay = StopPreviewStartupOverlay,");
        AssertContains(previewReinitText, "ResetPreviewContentTransform = ResetPreviewContentTransform,");
        AssertContains(previewReinitText, "ShowStartPreviewButtonPresentation = ShowStartPreviewButtonPresentation,");
        AssertContains(previewReinitTransitionControllerText, "internal sealed class PreviewReinitTransitionController");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "PreviewReinitTransitionController.cs")),
            "preview reinit transition state lives with preview transition animation ownership");
        AssertContains(previewReinitTransitionControllerText, "internal sealed class PreviewReinitCompletionPresentationContext");
        AssertContains(previewReinitTransitionControllerText, "public bool IsAnimating { get; private set; }");
        AssertContains(previewReinitTransitionControllerText, "public void BeginAnimateOut(string reason, string callerName)");
        AssertContains(previewReinitTransitionControllerText, "public PreviewReinitCompletionPresentation GetCompletionPresentation(");
        AssertContains(previewReinitTransitionControllerText, "public void HandleReinitializingChanged(PreviewReinitCompletionPresentationContext context)");
        AssertContains(previewReinitTransitionControllerText, "public void CompleteFirstVisualTransition(string attemptLabel, string callerName)");
        AssertContains(previewReinitTransitionControllerText, "public void ResetConfirmedVisualTransition(string attemptLabel, string reason, string callerName)");
        AssertContains(previewReinitTransitionControllerText, "public void ClearForStartupReset(bool preserveReinitAnimation, string callerName)");
        AssertContains(previewReinitTransitionControllerText, "public void Clear(string callerName, bool logWhenInactive = true, string? operationName = null)");
        AssertDoesNotContain(previewStartupText, "_previewStartupSessionController.BeginAttempt(");
        AssertDoesNotContain(previewStartupText, "_previewStartupSessionController.Reset(keepRecoveryCount)");
        AssertDoesNotContain(previewStartupText, "PREVIEW_FIRST_VISUAL_CONFIRMED attempt=");
        AssertDoesNotContain(previewRendererText, "_previewStartupState.ToString()");
        AssertDoesNotContain(previewStartupText, "private bool _isPreviewReinitAnimating;");
        AssertDoesNotContain(previewStartupText, "private bool _previewStopRequestedByUser;");
        AssertDoesNotContain(previewReinitText, "private bool _isPreviewReinitAnimating;");
        AssertDoesNotContain(mainWindowText, "private enum PreviewStartupState");
        AssertDoesNotContain(previewStartupText, "private enum PreviewStartupState");
        AssertDoesNotContain(previewStartupText, "private PreviewStartupState _previewStartupState = PreviewStartupState.Idle;");
        AssertDoesNotContain(previewStartupText, "private string? _previewStartupAttemptId;");
        AssertDoesNotContain(previewStartupText, "private DateTimeOffset? _previewStartupRequestedUtc;");
        AssertDoesNotContain(previewStartupText, "private DateTimeOffset? _previewRendererAttachedUtc;");
        AssertDoesNotContain(previewStartupText, "private DateTimeOffset? _previewFirstVisualUtc;");
        AssertDoesNotContain(previewStartupText, "private string? _previewLastFailureReason;");
        AssertDoesNotContain(previewStartupText, "private string? _previewStartupMissingSignals;");
        AssertDoesNotContain(previewStartupText, "private int _previewRecoveryAttemptCount;");
        AssertDoesNotContain(previewStartupText, "private bool _previewFirstVisualConfirmed;");
        AssertDoesNotContain(previewReinitText, "case PreviewReinitCompletionPresentation.");
        AssertDoesNotContain(previewReinitText, "GetCompletionPresentation(");

        return Task.CompletedTask;
    }

    internal static Task PreviewStartupSessionController_PreservesAttemptStateContracts()
    {
        var controllerType = RequireType("Sussudio.Controllers.PreviewStartupSessionController");
        var contextType = RequireType("Sussudio.Controllers.PreviewStartupSessionControllerContext");
        var stateType = RequireType("Sussudio.Controllers.PreviewStartupState");
        var events = new List<string>();
        var isPreviewing = true;
        var isStopRequested = false;
        var now = new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero);
        var context = Activator.CreateInstance(contextType, nonPublic: true)!;

        void SetContext(string propertyName, object value)
        {
            var property = contextType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"PreviewStartupSessionControllerContext.{propertyName} was not found.");
            property.SetValue(context, value);
        }

        SetContext("IsPreviewing", new Func<bool>(() => isPreviewing));
        SetContext("IsPreviewStopRequestedByUser", new Func<bool>(() => isStopRequested));
        SetContext("GetSelectedDeviceName", new Func<string?>(() => "Cam Link 4K"));
        SetContext("ResetSignalState", new Action(() => events.Add("reset-signals")));
        SetContext("ResetFailureStopSchedule", new Action(() => events.Add("reset-failure-stop")));
        SetContext("MarkFirstVisualSignalConfirmed", new Action(() => events.Add("mark-signal-visual")));
        SetContext("StopWatchdog", new Action(() => events.Add("stop-watchdog")));
        SetContext("StopOverlay", new Action(() => events.Add("stop-overlay")));
        SetContext("StopFadeInTimer", new Action(() => events.Add("stop-fade-timer")));
        SetContext("ScheduleFadeIn", new Action(() => events.Add("schedule-fade")));
        SetContext("CompleteFirstVisualTransition", new Action<string, string>((attempt, caller) => events.Add($"complete-reinit:{attempt}:{caller}")));
        SetContext("ClearReinitTransitionForStartupReset", new Action<bool, string>((preserve, caller) => events.Add($"clear-reinit:{preserve}:{caller}")));
        SetContext("Log", new Action<string>(message => events.Add($"log:{message}")));
        SetContext("CreateAttemptId", new Func<string>(() => "attempt-1"));
        SetContext("GetUtcNow", new Func<DateTimeOffset>(() => now));

        var controller = Activator.CreateInstance(controllerType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, binder: null, args: new[] { context }, culture: null)!;
        var beginStartupAttempt = controllerType.GetMethod("BeginStartupAttempt")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.BeginStartupAttempt was not found.");
        var setStartupState = controllerType.GetMethod("SetStartupState")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.SetStartupState was not found.");
        var markRendererAttached = controllerType.GetMethod("MarkRendererAttached")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.MarkRendererAttached was not found.");
        var markFirstVisualConfirmed = controllerType.GetMethod("MarkFirstVisualConfirmed")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.MarkFirstVisualConfirmed was not found.");
        var confirmFirstVisual = controllerType.GetMethod("ConfirmFirstVisual")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.ConfirmFirstVisual was not found.");
        var setMissingSignals = controllerType.GetMethod("SetMissingSignals")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.SetMissingSignals was not found.");
        var resetStartupTracking = controllerType.GetMethod("ResetStartupTracking")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.ResetStartupTracking was not found.");
        var getElapsedMilliseconds = controllerType.GetMethod("GetElapsedMilliseconds")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.GetElapsedMilliseconds was not found.");
        var isSignalWindowActive = controllerType.GetMethod("IsSignalWindowActive")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.IsSignalWindowActive was not found.");

        object State(string value) => Enum.Parse(stateType, value);
        bool SignalWindowActive(bool previewing) => (bool)isSignalWindowActive.Invoke(controller, new object[] { previewing })!;

        AssertEqual(State("Idle"), GetPropertyValue(controller, "State"), "initial startup state");
        AssertEqual(true, GetBoolProperty(controller, "ShouldBeginAttempt"), "initial attempt gate");
        AssertEqual(false, GetBoolProperty(controller, "ShouldRefreshMissingSignalsForSnapshot"), "idle does not refresh missing signals");
        AssertEqual(false, SignalWindowActive(previewing: true), "idle signal window inactive");

        beginStartupAttempt.Invoke(controller, Array.Empty<object>());
        AssertEqual(State("StartingSession"), GetPropertyValue(controller, "State"), "state after begin attempt");
        AssertEqual(true, SignalWindowActive(previewing: true), "starting session signal window active");
        AssertEqual(false, SignalWindowActive(previewing: false), "stopped preview signal window inactive");
        AssertEqual("attempt-1", GetStringProperty(controller, "AttemptId"), "attempt id after begin");
        AssertEqual(now, GetPropertyValue(controller, "RequestedUtc"), "requested UTC after begin");
        AssertEqual(false, GetBoolProperty(controller, "FirstVisualConfirmed"), "first visual reset on begin");
        AssertEqual(false, GetBoolProperty(controller, "ShouldBeginAttempt"), "active attempt gate");
        AssertEqual(1250.0, getElapsedMilliseconds.Invoke(controller, new object[] { now.AddMilliseconds(1250) }), "elapsed milliseconds");
        AssertEqual(
            "reset-signals|reset-failure-stop|log:PREVIEW_START_STATE state=StartingSession attempt=attempt-1 recovery=0 reason=-|log:PREVIEW_START_REQUESTED attempt=attempt-1 device=Cam Link 4K",
            string.Join("|", events),
            "begin startup orchestration order");

        events.Clear();
        setStartupState.Invoke(controller, new object?[] { State("StartingSession"), null });
        AssertEqual(string.Empty, string.Join("|", events), "duplicate state without reason suppresses log");
        setStartupState.Invoke(controller, new object?[] { State("Failed"), "renderer-attach-failed:test" });
        AssertEqual(State("Failed"), GetPropertyValue(controller, "State"), "failed state");
        AssertEqual(false, SignalWindowActive(previewing: true), "failed state signal window inactive");
        AssertEqual(true, GetBoolProperty(controller, "ShouldRefreshMissingSignalsForSnapshot"), "failed state refreshes missing signals");
        AssertEqual("renderer-attach-failed:test", GetStringProperty(controller, "LastFailureReason"), "failure reason retained");
        AssertEqual(true, GetBoolProperty(controller, "ShouldBeginAttempt"), "failed attempt gate");
        resetStartupTracking.Invoke(controller, new object[] { false, false });
        AssertEqual(State("Idle"), GetPropertyValue(controller, "State"), "terminal reset returns idle");
        AssertEqual(string.Empty, GetStringProperty(controller, "AttemptId"), "terminal reset clears attempt id");

        events.Clear();
        beginStartupAttempt.Invoke(controller, Array.Empty<object>());
        setStartupState.Invoke(controller, new object?[] { State("WaitingForFirstVisual"), null });
        setMissingSignals.Invoke(controller, new object?[] { "FirstVisual" });
        markRendererAttached.Invoke(controller, new object[] { now.AddMilliseconds(100) });
        AssertEqual(true, GetBoolProperty(controller, "IsWaitingForFirstVisual"), "waiting state predicate");
        AssertEqual(true, GetBoolProperty(controller, "ShouldRefreshMissingSignalsForSnapshot"), "waiting state refreshes missing signals");
        AssertEqual(true, SignalWindowActive(previewing: true), "waiting state signal window active");
        AssertEqual(now.AddMilliseconds(100), GetPropertyValue(controller, "RendererAttachedUtc"), "renderer attached UTC");
        AssertEqual(true, markFirstVisualConfirmed.Invoke(controller, new object[] { now.AddMilliseconds(300) }), "first visual confirmation");
        AssertEqual(false, markFirstVisualConfirmed.Invoke(controller, new object[] { now.AddMilliseconds(400) }), "duplicate first visual suppressed");
        AssertEqual(true, GetBoolProperty(controller, "FirstVisualConfirmed"), "first visual confirmed flag");
        AssertEqual(false, SignalWindowActive(previewing: true), "confirmed first visual signal window inactive");
        AssertEqual(now.AddMilliseconds(300), GetPropertyValue(controller, "FirstVisualUtc"), "first visual UTC");
        AssertEqual("FirstVisual", GetStringProperty(controller, "MissingSignals"), "missing signals cached until adapter clears them");

        events.Clear();
        beginStartupAttempt.Invoke(controller, Array.Empty<object>());
        setStartupState.Invoke(controller, new object?[] { State("WaitingForFirstVisual"), null });
        setMissingSignals.Invoke(controller, new object?[] { "FirstVisual" });
        now = now.AddMilliseconds(250);
        confirmFirstVisual.Invoke(controller, new object[] { "D3D11FirstFrame" });
        AssertEqual(State("Rendering"), GetPropertyValue(controller, "State"), "first visual moves to rendering");
        AssertEqual(string.Empty, GetStringProperty(controller, "MissingSignals"), "first visual clears missing signals");
        AssertEqual(
            "reset-signals|reset-failure-stop|log:PREVIEW_START_STATE state=StartingSession attempt=attempt-1 recovery=0 reason=-|log:PREVIEW_START_REQUESTED attempt=attempt-1 device=Cam Link 4K|log:PREVIEW_START_STATE state=WaitingForFirstVisual attempt=attempt-1 recovery=0 reason=-|mark-signal-visual|log:PREVIEW_START_STATE state=Rendering attempt=attempt-1 recovery=0 reason=-|stop-watchdog|stop-overlay|schedule-fade|complete-reinit:attempt-1:ConfirmPreviewFirstVisual|log:PREVIEW_FIRST_VISUAL_CONFIRMED attempt=attempt-1 source=D3D11FirstFrame elapsedMs=250 recovery=0",
            string.Join("|", events),
            "first visual orchestration order");

        events.Clear();
        beginStartupAttempt.Invoke(controller, Array.Empty<object>());
        setStartupState.Invoke(controller, new object?[] { State("WaitingForFirstVisual"), null });
        isStopRequested = true;
        confirmFirstVisual.Invoke(controller, new object[] { "D3D11FirstFrame" });
        AssertEqual(false, GetBoolProperty(controller, "FirstVisualConfirmed"), "stop request suppresses first visual");
        AssertContains(string.Join("|", events), "log:PREVIEW_FIRST_VISUAL_IGNORED attempt=attempt-1 source=D3D11FirstFrame reason=stop-requested");
        isStopRequested = false;

        events.Clear();
        setStartupState.Invoke(controller, new object?[] { State("WaitingForFirstVisual"), null });
        resetStartupTracking.Invoke(controller, new object[] { false, true });
        AssertEqual(State("Idle"), GetPropertyValue(controller, "State"), "nonterminal reset returns idle");
        AssertEqual(string.Empty, GetStringProperty(controller, "MissingSignals"), "nonterminal reset clears missing signals");
        AssertEqual(
            "stop-watchdog|stop-overlay|stop-fade-timer|clear-reinit:True:ResetPreviewStartupTracking|reset-signals|reset-failure-stop|log:PREVIEW_START_STATE state=Idle attempt=none recovery=0 reason=-",
            string.Join("|", events),
            "reset orchestration order");

        return Task.CompletedTask;
    }

    internal static Task PreviewReinitialization_WaitsForPendingFlashbackCycle()
    {
        var viewModelFiles = ReadMainViewModelCodeFiles();
        var viewModelSharedStateText = viewModelFiles["MainViewModel.cs"];
        var viewModelPreviewStateText = viewModelFiles["MainViewModel.cs"];
        var viewModelCaptureStateText = viewModelFiles["MainViewModel.cs"];
        var viewModelFlashbackStateText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var rawPreviewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var rawPreviewReinitializeControllerText = rawPreviewLifecycleControllerText;

        AssertContains(viewModelFlashbackStateText, "private const int FlashbackCycleBeforeReinitializeTimeoutMs = 30000;");
        AssertContains(viewModelCaptureStateText, "private const int PreviewReinitializeDebounceMs = 250;");
        AssertContains(viewModelPreviewStateText, "private int _previewReinitializeGeneration;");
        AssertContains(viewModelSharedStateText, "private int _previewReinitializeGeneration;");
        AssertContains(viewModelFiles["MainViewModel.cs"], "=> _previewLifecycleController.ReinitializeDeviceAsync(reason);");
        AssertContains(rawPreviewLifecycleControllerText, "=> _previewReinitializeController.ReinitializeDeviceAsync(reason);");
        AssertContains(rawPreviewReinitializeControllerText, "var reinitializeGeneration = _context.IncrementReinitializeGeneration();");
        AssertContains(rawPreviewReinitializeControllerText, "await Task.Delay(_context.PreviewReinitializeDebounceMs).ConfigureAwait(true);");
        AssertContains(rawPreviewReinitializeControllerText, "_context.ReadReinitializeGeneration() != reinitializeGeneration");
        AssertContains(rawPreviewReinitializeControllerText, "REINIT_COALESCED reason='{reason}' generation={reinitializeGeneration}");
        AssertContains(rawPreviewReinitializeControllerText, "await _context.AwaitWithTimeoutAsync(");
        AssertContains(rawPreviewReinitializeControllerText, "\"Flashback encoder settings cycle before reinitialize\").ConfigureAwait(false);");
        AssertContains(rawPreviewReinitializeControllerText, "REINIT_WAIT_FLASHBACK_CYCLE_TIMEOUT reason={reason} timeoutMs={_context.FlashbackCycleBeforeReinitializeTimeoutMs}");
        AssertContains(rawPreviewReinitializeControllerText, "REINIT_WAIT_FLASHBACK_CYCLE_FAULT");
        AssertContains(rawPreviewReinitializeControllerText, "_context.ClearPendingFlashbackCycleIfSameAndCompleted(pendingCycle);");

        return Task.CompletedTask;
    }

    internal static Task PreviewReinitTransitionController_PreservesTransitionStateContracts()
    {
        var controllerType = RequireType("Sussudio.Controllers.PreviewReinitTransitionController");
        var presentationType = RequireType("Sussudio.Controllers.PreviewReinitCompletionPresentation");
        var contextType = RequireType("Sussudio.Controllers.PreviewReinitCompletionPresentationContext");
        var controller = Activator.CreateInstance(controllerType, nonPublic: true)!;
        var beginAnimateOut = controllerType.GetMethod("BeginAnimateOut")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.BeginAnimateOut was not found.");
        var getCompletionPresentation = controllerType.GetMethod("GetCompletionPresentation")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.GetCompletionPresentation was not found.");
        var handleReinitializingChanged = controllerType.GetMethod("HandleReinitializingChanged")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.HandleReinitializingChanged was not found.");
        var completeFirstVisualTransition = controllerType.GetMethod("CompleteFirstVisualTransition")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.CompleteFirstVisualTransition was not found.");
        var resetConfirmedVisualTransition = controllerType.GetMethod("ResetConfirmedVisualTransition")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.ResetConfirmedVisualTransition was not found.");
        var clearForStartupReset = controllerType.GetMethod("ClearForStartupReset")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.ClearForStartupReset was not found.");
        var clear = controllerType.GetMethod("Clear")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.Clear was not found.");

        object Presentation(string value) => Enum.Parse(presentationType, value);

        object GetPresentation(bool isPreviewReinitializing, bool isPreviewing, bool isFirstVisualConfirmed)
            => getCompletionPresentation.Invoke(
                controller,
                new object[] { isPreviewReinitializing, isPreviewing, isFirstVisualConfirmed })!;

        object CreateContext(
            bool isPreviewReinitializing,
            bool isPreviewing,
            bool isFirstVisualConfirmed,
            string attemptLabel,
            string callerName,
            List<string> events)
        {
            var context = Activator.CreateInstance(contextType, nonPublic: true)!;
            SetPropertyOrBackingField(context, "IsPreviewReinitializing", isPreviewReinitializing);
            SetPropertyOrBackingField(context, "IsPreviewing", isPreviewing);
            SetPropertyOrBackingField(context, "IsFirstVisualConfirmed", isFirstVisualConfirmed);
            SetPropertyOrBackingField(context, "AttemptLabel", attemptLabel);
            SetPropertyOrBackingField(context, "CallerName", callerName);
            SetPropertyOrBackingField(context, "UpdateDeviceApplyButtonState", new Action(() => events.Add("update-apply")));
            SetPropertyOrBackingField(context, "RevealUnavailablePlaceholder", new Action(() => events.Add("reveal-unavailable")));
            SetPropertyOrBackingField(context, "StopPreviewStartupOverlay", new Action(() => events.Add("stop-overlay")));
            SetPropertyOrBackingField(context, "ResetPreviewContentTransform", new Action(() => events.Add("reset-transform")));
            SetPropertyOrBackingField(context, "ShowStartPreviewButtonPresentation", new Action(() => events.Add("show-start")));
            return context;
        }

        void HandleReinitializingChanged(
            bool isPreviewReinitializing,
            bool isPreviewing,
            bool isFirstVisualConfirmed,
            List<string> events)
            => handleReinitializingChanged.Invoke(
                controller,
                new[]
                {
                    CreateContext(
                        isPreviewReinitializing,
                        isPreviewing,
                        isFirstVisualConfirmed,
                        "attempt-3",
                        "HandleViewModelPropertyChangedAsync",
                        events),
                });

        AssertEqual(false, GetBoolProperty(controller, "IsAnimating"), "initial reinit animation inactive");
        AssertEqual(
            Presentation("ShowStartPreviewButton"),
            GetPresentation(isPreviewReinitializing: false, isPreviewing: false, isFirstVisualConfirmed: false),
            "idle stopped preview shows start presentation");

        beginAnimateOut.Invoke(controller, new object[] { "format-change", "ViewModel_PreviewReinitRequested" });
        AssertEqual(true, GetBoolProperty(controller, "IsAnimating"), "begin reinit marks animation active");
        AssertEqual(
            Presentation("RevealUnavailablePlaceholder"),
            GetPresentation(isPreviewReinitializing: false, isPreviewing: false, isFirstVisualConfirmed: false),
            "completed reinit without preview reveals unavailable placeholder");
        AssertEqual(
            Presentation("ResetConfirmedVisual"),
            GetPresentation(isPreviewReinitializing: false, isPreviewing: true, isFirstVisualConfirmed: true),
            "completed reinit after first visual resets presentation");
        AssertEqual(
            Presentation("None"),
            GetPresentation(isPreviewReinitializing: false, isPreviewing: true, isFirstVisualConfirmed: false),
            "completed reinit before first visual keeps waiting");

        completeFirstVisualTransition.Invoke(controller, new object[] { "attempt-1", "ConfirmPreviewFirstVisual" });
        AssertEqual(false, GetBoolProperty(controller, "IsAnimating"), "first visual clears active reinit animation");

        beginAnimateOut.Invoke(controller, new object[] { "format-change", "ViewModel_PreviewReinitRequested" });
        clearForStartupReset.Invoke(controller, new object[] { true, "ResetPreviewStartupTracking" });
        AssertEqual(true, GetBoolProperty(controller, "IsAnimating"), "startup reset can preserve reinit animation");
        clearForStartupReset.Invoke(controller, new object[] { false, "ResetPreviewStartupTracking" });
        AssertEqual(false, GetBoolProperty(controller, "IsAnimating"), "startup reset clears animation when not preserving");

        beginAnimateOut.Invoke(controller, new object[] { "format-change", "ViewModel_PreviewReinitRequested" });
        resetConfirmedVisualTransition.Invoke(controller, new object[] { "attempt-2", "reinit-stop-failed", "HandleViewModelPropertyChangedAsync" });
        AssertEqual(false, GetBoolProperty(controller, "IsAnimating"), "confirmed visual reset clears active animation");

        beginAnimateOut.Invoke(controller, new object[] { "format-change", "ViewModel_PreviewReinitRequested" });
        clear.Invoke(controller, new object?[] { "PreviewButton_Click", true, "PreviewButton_Click" });
        AssertEqual(false, GetBoolProperty(controller, "IsAnimating"), "explicit clear marks animation inactive");

        var idleStoppedEvents = new List<string>();
        HandleReinitializingChanged(
            isPreviewReinitializing: false,
            isPreviewing: false,
            isFirstVisualConfirmed: false,
            idleStoppedEvents);
        AssertEqual(
            "update-apply,show-start",
            string.Join(",", idleStoppedEvents),
            "idle stopped preview updates apply state then shows start presentation");

        beginAnimateOut.Invoke(controller, new object[] { "format-change", "ViewModel_PreviewReinitRequested" });
        var stoppedReinitCompletionEvents = new List<string>();
        HandleReinitializingChanged(
            isPreviewReinitializing: false,
            isPreviewing: false,
            isFirstVisualConfirmed: false,
            stoppedReinitCompletionEvents);
        AssertEqual(
            "update-apply,reveal-unavailable",
            string.Join(",", stoppedReinitCompletionEvents),
            "completed reinit without preview updates apply state then reveals unavailable placeholder");
        AssertEqual(false, GetBoolProperty(controller, "IsAnimating"), "unavailable placeholder completion clears active animation");

        beginAnimateOut.Invoke(controller, new object[] { "format-change", "ViewModel_PreviewReinitRequested" });
        var confirmedReinitCompletionEvents = new List<string>();
        HandleReinitializingChanged(
            isPreviewReinitializing: false,
            isPreviewing: true,
            isFirstVisualConfirmed: true,
            confirmedReinitCompletionEvents);
        AssertEqual(
            "update-apply,stop-overlay,reset-transform",
            string.Join(",", confirmedReinitCompletionEvents),
            "confirmed visual completion updates apply state, stops overlay, and resets content transform");
        AssertEqual(false, GetBoolProperty(controller, "IsAnimating"), "confirmed visual completion clears active animation");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelPreviewLifecycle_LivesInController()
    {
        var rootText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var previewStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelCaptureLifecycleControllers.cs")
            .Replace("\r\n", "\n");
        var previewReinitializeControllerText = previewLifecycleControllerText;
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(previewStateText, "private Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewStateText, "=> _previewLifecycleController.ReinitializeDeviceAsync(reason);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Capture.cs")),
            "MainViewModel capture lifecycle facade partial");
        if (File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.PreviewReinitialization.cs")))
        {
            throw new InvalidOperationException("Preview reinitialization should not live in a tiny pass-through partial.");
        }
        AssertEqual(
            false,
            File.Exists(Path.Combine(
                GetRepoRoot(),
                "Sussudio",
                "Controllers",
                "ViewModel",
                "MainViewModelPreviewReinitializeController.cs")),
            "Preview reinitialize transaction controller lives with preview lifecycle owner");
        AssertContains(previewLifecycleControllerText, "private readonly MainViewModelPreviewReinitializeController _previewReinitializeController;");
        AssertContains(previewLifecycleControllerText, "public Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewLifecycleControllerText, "=> _previewReinitializeController.ReinitializeDeviceAsync(reason);");
        AssertContains(previewLifecycleControllerText, "namespace Sussudio.Controllers;");
        AssertContains(previewLifecycleControllerText, "internal sealed class MainViewModelPreviewLifecycleController");
        AssertContains(previewReinitializeControllerText, "namespace Sussudio.Controllers;");
        AssertContains(previewReinitializeControllerText, "internal sealed class MainViewModelPreviewReinitializeController");
        AssertContains(previewReinitializeControllerText, "public void CancelPendingPreviewRestart()");
        AssertContains(previewReinitializeControllerText, "public void ResetPendingPreviewRestartCancellation()");
        AssertContains(previewReinitializeControllerText, "public async Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewReinitializeControllerText, "private readonly MainViewModelPreviewReinitializeControllerContext _context;");
        AssertDoesNotContain(previewReinitializeControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(previewReinitializeControllerText, "_viewModel.");
        AssertContains(previewReinitializeControllerText, "var reinitializeGeneration = _context.IncrementReinitializeGeneration();");
        AssertContains(previewReinitializeControllerText, "await Task.Delay(_context.PreviewReinitializeDebounceMs).ConfigureAwait(true);");
        AssertContains(previewReinitializeControllerText, "_context.ReadReinitializeGeneration() != reinitializeGeneration");
        AssertContains(previewReinitializeControllerText, "await _context.AwaitWithTimeoutAsync(");
        AssertContains(previewReinitializeControllerText, "FlashbackCycleBeforeReinitializeTimeoutMs");
        AssertContains(previewReinitializeControllerText, "await _context.WaitReinitializeGateAsync();");
        AssertContains(previewReinitializeControllerText, "await _context.NotifyPreviewReinitRequestedAsync(reason);");
        AssertContains(previewReinitializeControllerText, "await _context.NotifyRendererStopAsync();");
        AssertContains(previewReinitializeControllerText, "await _previewLifecycleController.StopPreviewAsync(userInitiated: false, teardownPipeline: true, CancellationToken.None);");
        AssertContains(previewReinitializeControllerText, "await _previewLifecycleController.InitializeDeviceAsync();");
        AssertContains(previewReinitializeControllerText, "await _previewLifecycleController.StartPreviewAsync(userInitiated: false);");
        AssertContains(previewReinitializeControllerText, "_context.ReleaseReinitializeGate();");
        AssertDoesNotContain(previewStateText, "private async Task ReinitializeDeviceAsync(string reason)");
        AssertContains(rootText, "private Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewStateText, "public Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)");
        AssertContains(previewStateText, "public Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)");
        AssertContains(agentMapText, "`Sussudio/Controllers/ViewModel/MainViewModelCaptureLifecycleControllers.cs`");
        AssertDoesNotContain(agentMapText, "`Sussudio/Controllers/ViewModel/MainViewModelPreviewReinitializeController.cs`");
        AssertDoesNotContain(cleanupPlanText, "`MainViewModel.PreviewReinitialization.cs`");
        AssertContains(cleanupPlanText, "`Sussudio/Controllers/ViewModel/MainViewModelCaptureLifecycleControllers.cs`");
        AssertDoesNotContain(cleanupPlanText, "`Sussudio/Controllers/ViewModel/MainViewModelPreviewReinitializeController.cs`");

        return Task.CompletedTask;
    }
}
