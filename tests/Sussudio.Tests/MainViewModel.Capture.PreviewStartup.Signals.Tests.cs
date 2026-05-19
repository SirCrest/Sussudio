using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

static partial class Program
{
    private static Task PreviewStartupSignalsOwnership_LivesInFocusedControllers()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var previewStartupText = ReadRepoFile("Sussudio/MainWindow.PreviewStartup.cs")
            .Replace("\r\n", "\n");
        var previewStartupSignalsText = ReadRepoFile("Sussudio/MainWindow.PreviewStartup.cs")
            .Replace("\r\n", "\n");
        var previewStartupSignalCoordinatorText = ReadRepoFile("Sussudio/Controllers/Preview/Startup/PreviewStartupSignalCoordinator.cs")
            .Replace("\r\n", "\n");
        var previewStartupReadinessSignalControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Startup/PreviewStartupReadinessSignalController.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainWindowText, "InitializePreviewStartupSignalCoordinator();");
        AssertContains(previewStartupSignalsText, "XAML-facing preview startup signal adapter");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.PreviewStartupSignals.cs")),
            "preview startup signal adapter is consolidated into the startup adapter");
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
        AssertDoesNotContain(mainWindowText, "ResetPreviewSignalState()");
        AssertEqual(
            true,
            previewStartupText.Split('\n').Length >= 100,
            "preview startup adapter is a substantial consolidated adapter file");
        AssertDoesNotContain(previewStartupSignalsText, "private readonly PreviewStartupReadinessSignalController");
        AssertDoesNotContain(previewStartupSignalsText, "private long _previewStartupPositionEventCount;");
        AssertDoesNotContain(previewStartupSignalsText, "_readinessSignals.TrackPlaybackPosition(");
        AssertDoesNotContain(previewStartupSignalsText, "PREVIEW_START_SIGNAL");
        AssertDoesNotContain(previewStartupSignalsText, "PREVIEW_START_WAITING");
        AssertDoesNotContain(previewStartupSignalsText, "private static string BuildPreviewStartupSignalList");

        return Task.CompletedTask;
    }

    private static Task PreviewStartupReadinessSignalController_PreservesSignalStateContracts()
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

    private static Task PreviewStartupSignalFormatter_PreservesSignalStrings()
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

    private static Task PreviewStartupFailureTextFormatter_PreservesFailureStrings()
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
}
