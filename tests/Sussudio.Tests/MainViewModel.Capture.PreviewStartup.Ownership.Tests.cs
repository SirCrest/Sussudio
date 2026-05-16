using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

static partial class Program
{
    private static Task PreviewStartupOwnership_LivesInControllers()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var previewStartupText = ReadRepoFile("Sussudio/MainWindow.PreviewStartup.cs")
            .Replace("\r\n", "\n");
        var previewStartupSessionControllerText = ReadRepoFile("Sussudio/Controllers/PreviewStartup/PreviewStartupSessionController.cs")
            .Replace("\r\n", "\n");
        var previewStartupWatchdogText = ReadRepoFile("Sussudio/MainWindow.PreviewStartupWatchdog.cs")
            .Replace("\r\n", "\n");
        var previewStartupWatchdogControllerText = ReadRepoFile("Sussudio/Controllers/PreviewStartup/PreviewStartupWatchdogController.cs")
            .Replace("\r\n", "\n");
        var previewFadeInText = ReadRepoFile("Sussudio/MainWindow.PreviewFadeIn.cs")
            .Replace("\r\n", "\n");
        var previewFadeInControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewFadeInController.cs")
            .Replace("\r\n", "\n");
        var previewStartupSignalsText = ReadRepoFile("Sussudio/MainWindow.PreviewStartupSignals.cs")
            .Replace("\r\n", "\n");
        var previewStartupReadinessSignalControllerText = ReadRepoFile("Sussudio/Controllers/PreviewStartup/PreviewStartupReadinessSignalController.cs")
            .Replace("\r\n", "\n");
        var previewStartupSignalCoordinatorText = ReadRepoFile("Sussudio/Controllers/PreviewStartup/PreviewStartupSignalCoordinator.cs")
            .Replace("\r\n", "\n");
        var previewStartupFailureText = ReadRepoFile("Sussudio/Controllers/PreviewStartup/PreviewStartupFailureTextFormatter.cs")
            .Replace("\r\n", "\n");
        var previewRendererText = ReadRepoFile("Sussudio/MainWindow.PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var previewRuntimeSnapshotText = ReadRepoFile("Sussudio/MainWindow.PreviewRuntimeSnapshot.cs")
            .Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs")
            .Replace("\r\n", "\n");
        var previewPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedPreview.cs")
            .Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewLifecycleEventController.cs")
            .Replace("\r\n", "\n");
        var previewReinitText = ReadRepoFile("Sussudio/MainWindow.PreviewReinit.cs")
            .Replace("\r\n", "\n");
        var previewReinitTransitionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewReinitTransitionController.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainWindowText, "InitializePreviewStartupSessionController();");
        AssertContains(mainWindowText, "InitializePreviewReinitTransitionController();");
        AssertContains(mainWindowText, "InitializePreviewLifecycleEventController();");
        AssertContains(mainWindowText, "InitializePreviewStartupSignalCoordinator();");
        AssertContains(mainWindowText, "InitializePreviewStartupWatchdogController();");
        AssertContains(previewStartupText, "private PreviewStartupSessionController _previewStartupSessionController = null!;");
        AssertContains(previewStartupText, "private void InitializePreviewStartupSessionController()");
        AssertContains(previewStartupText, "private PreviewStartupState CurrentPreviewStartupState");
        AssertContains(previewStartupText, "private string PreviewStartupAttemptLabel");
        AssertContains(previewStartupText, "private bool ShouldBeginPreviewStartupAttempt");
        AssertContains(previewStartupText, "_previewStartupSessionController.SetState(state, reason)");
        AssertContains(previewStartupText, "_previewStartupSessionController.BeginAttempt(");
        AssertContains(previewStartupText, "_previewStartupSessionController.MarkFirstVisualConfirmed(DateTimeOffset.UtcNow)");
        AssertContains(previewStartupText, "_previewStartupSessionController.Reset(keepRecoveryCount)");
        AssertContains(previewStartupText, "PREVIEW_START_STATE state={state} attempt={PreviewStartupAttemptLabel}");
        AssertContains(previewStartupText, "PREVIEW_START_REQUESTED attempt={PreviewStartupAttemptId}");
        AssertContains(previewStartupText, "PREVIEW_FIRST_VISUAL_IGNORED attempt={PreviewStartupAttemptLabel}");
        AssertContains(previewStartupText, "PREVIEW_FIRST_VISUAL_CONFIRMED attempt={PreviewStartupAttemptLabel}");
        AssertContains(previewStartupSessionControllerText, "internal enum PreviewStartupState");
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
        AssertContains(previewStartupSessionControllerText, "public bool ShouldBeginAttempt => string.IsNullOrWhiteSpace(AttemptId) || IsFailed || IsIdle;");
        AssertContains(previewStartupSessionControllerText, "public bool BeginAttempt(string attemptId, DateTimeOffset requestedUtc)");
        AssertContains(previewStartupSessionControllerText, "public bool SetState(PreviewStartupState state, string? reason = null)");
        AssertContains(previewStartupSessionControllerText, "public void MarkRendererAttached(DateTimeOffset attachedUtc)");
        AssertContains(previewStartupSessionControllerText, "public bool MarkFirstVisualConfirmed(DateTimeOffset firstVisualUtc)");
        AssertContains(previewStartupSessionControllerText, "public void SetMissingSignals(string? missingSignals)");
        AssertContains(previewStartupSessionControllerText, "public bool Reset(bool keepRecoveryCount = false)");
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
        AssertContains(previewFadeInText, "private PreviewFadeInController _previewFadeInController = null!;");
        AssertContains(previewFadeInText, "private void InitializePreviewFadeInController()");
        AssertContains(previewFadeInText, "private void SchedulePreviewFadeIn()");
        AssertContains(previewFadeInText, "private void StopPreviewFadeInTimer()");
        AssertContains(previewFadeInControllerText, "private const int PreviewFadeInFrameThreshold = 3;");
        AssertContains(previewFadeInControllerText, "private DispatcherQueueTimer? _timer;");
        AssertContains(previewFadeInControllerText, "public void Schedule()");
        AssertContains(previewFadeInControllerText, "public void Stop()");
        AssertContains(previewStartupSignalsText, "XAML-facing preview startup signal adapter");
        AssertContains(previewStartupSignalsText, "private PreviewStartupSignalCoordinator _previewStartupSignalCoordinator = null!;");
        AssertContains(previewStartupSignalsText, "private void InitializePreviewStartupSignalCoordinator()");
        AssertContains(previewStartupSignalsText, "IsSignalWindowActive = IsPreviewStartupSignalWindowActive,");
        AssertContains(previewStartupSignalsText, "ConfirmFirstVisual = ConfirmPreviewFirstVisual,");
        AssertContains(previewStartupSignalsText, "GetPlaybackSnapshotState = GetPreviewStartupPlaybackSnapshotState");
        AssertContains(previewStartupSignalsText, "private long PreviewStartupGpuPositionEventCount => _previewStartupSignalCoordinator.PositionEventCount;");
        AssertContains(previewStartupSignalsText, "private bool IsPreviewStartupSignalWindowActive()");
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
        AssertContains(previewStartupFailureText, "internal static class PreviewStartupFailureTextFormatter");
        AssertContains(previewStartupFailureText, "public static string FormatTimeoutReason(int timeoutMs, string? missingSignals)");
        AssertContains(previewStartupFailureText, "public static string FormatTimeoutStatusText(string? missingSignals)");
        AssertContains(previewStartupFailureText, "public static string FormatFailureStopStatusText(string reason)");
        AssertContains(previewStartupWatchdogControllerText, "PreviewStartupFailureTextFormatter.FormatTimeoutReason(");
        AssertContains(previewStartupWatchdogControllerText, "PreviewStartupFailureTextFormatter.FormatTimeoutStatusText(");
        AssertContains(previewStartupWatchdogControllerText, "PreviewStartupFailureTextFormatter.FormatFailureStopStatusText(reason)");
        AssertContains(previewStartupWatchdogControllerText, "PREVIEW_START_WATCHDOG_STARTED");
        AssertContains(previewStartupWatchdogControllerText, "PREVIEW_START_TIMEOUT_IGNORED reason=user-or-shutdown-stop-requested");
        AssertContains(previewStartupWatchdogControllerText, "PREVIEW_START_TIMEOUT attempt={_context.GetAttemptLabel()}");
        AssertContains(previewStartupWatchdogControllerText, "PREVIEW_START_FAILURE_STOP begin");
        AssertContains(previewRuntimeSnapshotText, "StartupState = CurrentPreviewStartupState.ToString(),");
        AssertDoesNotContain(previewRendererText, "_previewStartupState.ToString()");
        AssertContains(propertyChangedText, "await TryHandlePreviewPropertyChangedAsync(propertyName)");
        AssertContains(previewPropertyChangedText, "_previewLifecycleEventController.TryHandlePropertyChangedAsync(propertyName);");
        AssertContains(previewPropertyChangedText, "_previewLifecycleEventController.HandlePreviewStartRequested();");
        AssertContains(previewPropertyChangedText, "_previewLifecycleEventController.HandlePreviewStopRequested();");
        AssertContains(previewPropertyChangedText, "Preview-specific ViewModel event adapter");
        AssertContains(previewLifecycleControllerText, "await HandlePreviewingChangedAsync();");
        AssertContains(previewLifecycleControllerText, "_context.HandlePreviewReinitializingChanged();");
        AssertContains(previewLifecycleControllerText, "if (_context.ShouldBeginPreviewStartupAttempt())");
        AssertContains(previewLifecycleControllerText, "_stopRequestedByUser = _stopRequestedByUser || !_context.ViewModel.IsPreviewReinitializing;");
        AssertContains(previewLifecycleControllerText, "_context.StartPreviewStartupWatchdog();");
        AssertContains(previewLifecycleControllerText, "_context.ShowStopPreviewButtonPresentation();");
        AssertContains(previewLifecycleControllerText, "_context.ShowStartPreviewButtonPresentation();");
        AssertContains(previewLifecycleControllerText, "_context.ApplyHdrToggleEnabledState();");
        AssertDoesNotContain(previewStartupText, "private bool _isPreviewReinitAnimating;");
        AssertDoesNotContain(previewStartupText, "private bool _previewStopRequestedByUser;");
        AssertDoesNotContain(previewPropertyChangedText, "private async Task ViewModel_PreviewReinitRequested(string reason)");
        AssertDoesNotContain(previewPropertyChangedText, "private Task ViewModel_PreviewRendererStopRequested()");
        AssertDoesNotContain(previewPropertyChangedText, "private void HandlePreviewReinitializingChanged()");
        AssertContains(previewReinitText, "private PreviewReinitTransitionController _previewReinitTransitionController = null!;");
        AssertContains(previewReinitText, "private bool IsPreviewReinitAnimating");
        AssertContains(previewReinitText, "=> _previewReinitTransitionController.IsAnimating;");
        AssertContains(previewReinitText, "private async Task ViewModel_PreviewReinitRequested(string reason)");
        AssertContains(previewReinitText, "private Task ViewModel_PreviewRendererStopRequested()");
        AssertContains(previewReinitText, "private void HandlePreviewReinitializingChanged()");
        AssertContains(previewReinitTransitionControllerText, "internal sealed class PreviewReinitTransitionController");
        AssertContains(previewReinitTransitionControllerText, "public bool IsAnimating { get; private set; }");
        AssertContains(previewReinitTransitionControllerText, "public void BeginAnimateOut(string reason, string callerName)");
        AssertContains(previewReinitTransitionControllerText, "public PreviewReinitCompletionPresentation GetCompletionPresentation(");
        AssertContains(previewReinitTransitionControllerText, "public void CompleteFirstVisualTransition(string attemptLabel, string callerName)");
        AssertContains(previewReinitTransitionControllerText, "public void ResetConfirmedVisualTransition(string attemptLabel, string reason, string callerName)");
        AssertContains(previewReinitTransitionControllerText, "public void ClearForStartupReset(bool preserveReinitAnimation, string callerName)");
        AssertContains(previewReinitTransitionControllerText, "public void Clear(string callerName, bool logWhenInactive = true, string? operationName = null)");
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
        AssertDoesNotContain(previewStartupText, "private void StartPreviewStartupWatchdog()");
        AssertDoesNotContain(previewStartupText, "private Task HandlePreviewStartupTimeoutAsync()");
        AssertDoesNotContain(previewStartupText, "PreviewStartupFailureTextFormatter.FormatTimeoutReason(");
        AssertDoesNotContain(previewStartupText, "private const int PreviewStartupDefaultVisualTimeoutMs = 10000;");
        AssertDoesNotContain(previewStartupSignalsText, "private readonly PreviewStartupReadinessSignalController");
        AssertDoesNotContain(previewStartupSignalsText, "private long _previewStartupPositionEventCount;");
        AssertDoesNotContain(previewStartupSignalsText, "_readinessSignals.TrackPlaybackPosition(");
        AssertDoesNotContain(previewStartupSignalsText, "PREVIEW_START_SIGNAL");
        AssertDoesNotContain(previewStartupSignalsText, "PREVIEW_START_WAITING");
        AssertDoesNotContain(mainWindowText, "ResetPreviewSignalState()");
        AssertDoesNotContain(previewStartupText, "private void ConfigurePreviewStartupSignals(PreviewStartupStrategy strategy, PreviewStartupSignalFlags requiredSignals)");
        AssertDoesNotContain(previewStartupText, "private void SchedulePreviewFadeIn()");
        AssertDoesNotContain(previewStartupSignalsText, "private static string BuildPreviewStartupSignalList");
        AssertDoesNotContain(previewStartupText, "no-visual-confirmation-within-{PreviewStartupVisualTimeoutMs}ms");
        AssertDoesNotContain(previewStartupText, "Preview failed to attach to UI (session started but no visual confirmation).");
        AssertDoesNotContain(previewStartupText, "Preview failed to start (missing readiness signal:");

        return Task.CompletedTask;
    }
}
