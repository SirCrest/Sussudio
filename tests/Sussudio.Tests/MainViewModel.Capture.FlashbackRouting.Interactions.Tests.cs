using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

static partial class Program
{
    internal static Task MainWindowFlashbackScrub_EndsOnReleaseCancelAndCaptureLost()
    {
        var flashbackWindowText = ReadMainWindowFlashbackAdapterSource();
        var flashbackCommandControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs")
            .Replace("\r\n", "\n");
        var flashbackScrubText = ReadMainWindowFlashbackAdapterSource();
        var flashbackScrubControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackScrubInteractionController.cs")
            .Replace("\r\n", "\n");
        var flashbackGeometryText = flashbackScrubControllerText;
        var flashbackPlayheadText = ReadMainWindowFlashbackAdapterSource();
        var flashbackPlayheadControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackPlayheadMotionController.cs")
            .Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var fullScreenWindowText = ReadMainWindowShellChromeAdapterSource();
        var fullScreenControllerText = ReadRepoFile("Sussudio/Controllers/FullScreen/FullScreenController.cs")
            .Replace("\r\n", "\n");
        var xamlText = ReadRepoFile("Sussudio/MainWindow.xaml")
            .Replace("\r\n", "\n");

        AssertContains(xamlText, "PointerReleased=\"FlashbackScrubArea_PointerReleased\"");
        AssertContains(xamlText, "PointerCanceled=\"FlashbackScrubArea_PointerCanceled\"");
        AssertContains(xamlText, "PointerCaptureLost=\"FlashbackScrubArea_PointerCaptureLost\"");
        AssertContains(flashbackScrubText, "XAML-facing Flashback pointer scrub adapter");
        AssertContains(flashbackScrubText, "private FlashbackScrubInteractionController _flashbackScrubInteractionController = null!;");
        AssertContains(flashbackScrubText, "private void InitializeFlashbackScrubInteractionController()");
        AssertContains(flashbackScrubText, "PositionMagneticPlayhead = PositionFlashbackMagneticPlayhead,");
        AssertContains(flashbackScrubText, "RefreshCtiMotion = RefreshFlashbackCtiMotion,");
        AssertContains(flashbackScrubText, "GetTickCount64 = () => Environment.TickCount64,");
        AssertContains(flashbackScrubControllerText, "internal sealed class FlashbackScrubInteractionController");
        AssertContains(flashbackScrubControllerText, "private bool _isScrubbing;");
        AssertContains(flashbackScrubControllerText, "private TimeSpan? _lastPointerPosition;");
        AssertContains(flashbackScrubControllerText, "private long _lastUpdateTick;");
        AssertContains(flashbackScrubControllerText, "public bool IsScrubbing => _isScrubbing;");
        AssertContains(flashbackScrubControllerText, "private void End(UIElement? element, Pointer pointer, string reason, TimeSpan? releasePosition = null)");
        AssertContains(flashbackScrubControllerText, "if (!_context.ViewModel.FlashbackBeginScrub(targetPosition))\n        {\n            _lastPointerPosition = null;\n            _context.ViewModel.ReportFlashbackPlaybackRejection(\"scrub begin\", \"FLASHBACK_UI_SCRUB_BEGIN_REJECTED\");\n            return;\n        }");
        AssertContains(flashbackScrubControllerText, "if (!_context.ViewModel.FlashbackUpdateScrub(targetPosition))\n        {\n            _context.ViewModel.ReportFlashbackPlaybackRejection(\"scrub update\", \"FLASHBACK_UI_SCRUB_UPDATE_REJECTED\");\n            End(element, e.Pointer, \"update_rejected\");\n            return;\n        }");
        AssertContains(flashbackScrubText, "private void FlashbackScrubArea_PointerReleased(object sender, PointerRoutedEventArgs e)");
        AssertContains(flashbackScrubText, "=> _flashbackScrubInteractionController.PointerReleased(sender as UIElement, e);");
        AssertContains(flashbackScrubControllerText, "TimeSpan? releasePosition = null;\n        if (_isScrubbing)");
        AssertContains(flashbackScrubControllerText, "var targetPosition = ComputeScrubPosition(e);\n            releasePosition = targetPosition;\n            _lastPointerPosition = targetPosition;\n            if (!_context.ViewModel.FlashbackUpdateScrub(targetPosition))");
        AssertContains(flashbackScrubControllerText, "_context.ViewModel.ReportFlashbackPlaybackRejection(\"scrub release update\", \"FLASHBACK_UI_SCRUB_RELEASE_UPDATE_REJECTED\");");
        AssertContains(flashbackScrubControllerText, "End(element, e.Pointer, \"released\", releasePosition);");
        AssertContains(flashbackCommandControllerText, "ReportFlashbackPlaybackRejection(\"set in point\", \"FLASHBACK_UI_SET_IN_REJECTED\")");
        AssertContains(flashbackCommandControllerText, "ReportFlashbackPlaybackRejection(\"set out point\", \"FLASHBACK_UI_SET_OUT_REJECTED\")");
        AssertContains(flashbackCommandControllerText, "ReportFlashbackPlaybackRejection(\"clear in/out\", \"FLASHBACK_UI_CLEAR_INOUT_REJECTED\")");
        AssertContains(flashbackCommandControllerText, "Logger.Log($\"FLASHBACK_UI_SET_IN pos_ms={(long)pos.Value.TotalMilliseconds}\");");
        AssertContains(flashbackCommandControllerText, "Logger.Log($\"FLASHBACK_UI_SET_OUT pos_ms={(long)pos.Value.TotalMilliseconds}\");");
        AssertContains(flashbackCommandControllerText, "Logger.Log(\"FLASHBACK_UI_CLEAR_INOUT\");");
        AssertContains(flashbackCommandControllerText, "ReportFlashbackPlaybackRejection(\"pause\", \"FLASHBACK_UI_PAUSE_REJECTED\")");
        AssertContains(flashbackCommandControllerText, "ReportFlashbackPlaybackRejection(\"play\", \"FLASHBACK_UI_PLAY_REJECTED\")");
        AssertContains(flashbackCommandControllerText, "ReportFlashbackPlaybackRejection(\"go live\", \"FLASHBACK_UI_GOLIVE_REJECTED\")");
        AssertContains(flashbackCommandControllerText, "Logger.Log(\"FLASHBACK_UI_PAUSE\");");
        AssertContains(flashbackCommandControllerText, "Logger.Log(\"FLASHBACK_UI_PLAY\");");
        AssertContains(flashbackCommandControllerText, "Logger.Log(\"FLASHBACK_UI_GOLIVE\");");
        AssertContains(flashbackCommandControllerText, "public bool HandleFullScreenKeyboardCommand(VirtualKey key)");
        AssertContains(flashbackCommandControllerText, "case VirtualKey.I:");
        AssertContains(flashbackCommandControllerText, "SetInPointAtPlayhead();");
        AssertContains(flashbackCommandControllerText, "case VirtualKey.O:");
        AssertContains(flashbackCommandControllerText, "SetOutPointAtPlayhead();");
        AssertContains(flashbackCommandControllerText, "case VirtualKey.Space:");
        AssertContains(flashbackCommandControllerText, "TogglePlayPause();");
        AssertContains(flashbackCommandControllerText, "case VirtualKey.L:");
        AssertContains(flashbackCommandControllerText, "GoLive();");
        AssertContains(flashbackCommandControllerText, "case VirtualKey.Left:");
        AssertContains(flashbackCommandControllerText, "NudgePlayback(TimeSpan.FromSeconds(-1), \"nudge left\", \"FLASHBACK_UI_NUDGE_REJECTED direction=left\");");
        AssertContains(flashbackCommandControllerText, "case VirtualKey.Right:");
        AssertContains(flashbackCommandControllerText, "NudgePlayback(TimeSpan.FromSeconds(1), \"nudge right\", \"FLASHBACK_UI_NUDGE_REJECTED direction=right\");");
        AssertContains(flashbackCommandControllerText, "ReportFlashbackPlaybackRejection(operationName, rejectionDetail)");
        AssertContains(flashbackScrubControllerText, "_isScrubbing = true;\n        _lastPointerPosition = targetPosition;\n        _lastUpdateTick = 0;\n        element?.CapturePointer(e.Pointer);");
        AssertContains(flashbackScrubControllerText, "var carriedPosition = _isScrubbing ? _lastPointerPosition : null;");
        AssertContains(flashbackScrubControllerText, "var ended = releasePosition.HasValue\n            ? _context.ViewModel.FlashbackEndScrubAt(releasePosition.Value)\n            : _context.ViewModel.FlashbackEndScrub();\n        if (!ended)\n        {\n            _context.ViewModel.ReportFlashbackPlaybackRejection($\"scrub end ({reason})\", $\"FLASHBACK_UI_SCRUB_END_REJECTED reason={reason}\");\n        }");
        AssertContains(flashbackScrubControllerText, "ClearLocalState();\n        element?.ReleasePointerCapture(pointer);");
        AssertContains(flashbackScrubControllerText, "FLASHBACK_UI_SCRUB_END");
        AssertContains(flashbackScrubText, "FlashbackScrubArea_PointerCanceled");
        AssertContains(flashbackScrubText, "FlashbackScrubArea_PointerCaptureLost");
        AssertContains(flashbackScrubControllerText, "FlashbackTimelineGeometry.TryComputeFraction(pos.X, width, out var fraction)");
        AssertContains(flashbackScrubControllerText, "FlashbackTimelineGeometry.IsUsableDuration(bufferDuration)");
        AssertContains(flashbackScrubControllerText, "FlashbackTimelineGeometry.ComputePosition(fraction, bufferDuration)");
        AssertContains(flashbackScrubControllerText, "FlashbackTimelineGeometry.TryComputePosition(");
        AssertContains(flashbackGeometryText, "internal static class FlashbackTimelineGeometry");
        AssertContains(flashbackGeometryText, "public static bool TryComputeFraction(double x, double width, out double fraction)");
        AssertContains(flashbackGeometryText, "public static bool TryComputePosition(double x, double width, TimeSpan bufferDuration, out TimeSpan position)");
        AssertContains(flashbackGeometryText, "public static TimeSpan ComputePosition(double fraction, TimeSpan bufferDuration)");
        AssertContains(flashbackGeometryText, "public static bool IsUsableTrackDimension(double value)");
        AssertContains(flashbackGeometryText, "public static bool IsUsableDuration(TimeSpan value)");
        AssertContains(flashbackPlayheadControllerText, "FlashbackTimelineGeometry.IsUsableTrackDimension(trackW)");
        AssertDoesNotContain(flashbackPlayheadText, "FlashbackTimelineGeometry.IsUsableTrackDimension(trackW)");
        AssertDoesNotContain(flashbackScrubText, "private static bool TryComputeFlashbackTimelineFraction(double x, double width, out double fraction)");
        AssertDoesNotContain(flashbackScrubText, "private static bool IsUsableFlashbackTrackDimension(double value)");
        AssertDoesNotContain(flashbackScrubText, "private static bool IsUsableFlashbackDuration(TimeSpan value)");
        AssertContains(fullScreenWindowText, "HandleFlashbackKeyboardCommand = _flashbackCommandController.HandleFullScreenKeyboardCommand,");
        AssertContains(fullScreenWindowText, "private void OnContentKeyDown(object sender, KeyRoutedEventArgs e)\n        => _fullScreenController.OnKeyDown(e);");
        AssertContains(fullScreenControllerText, "private void HandleFlashbackKeyDown(KeyRoutedEventArgs e)");
        AssertContains(fullScreenControllerText, "if (!_context.ViewModel.IsFlashbackEnabled || _context.FlashbackTimelinePanel.Visibility != Visibility.Visible)");
        AssertContains(fullScreenControllerText, "if (_context.HandleFlashbackKeyboardCommand(e.Key))\n        {\n            e.Handled = true;\n        }");
        AssertDoesNotContain(fullScreenWindowText, "if (!ViewModel.IsFlashbackEnabled || FlashbackTimelinePanel.Visibility != Visibility.Visible)");
        AssertDoesNotContain(fullScreenWindowText, "_flashbackCommandController.HandleFullScreenKeyboardCommand(e.Key)");
        AssertContains(fullScreenWindowText, "EndFlashbackScrubForFullScreen = _flashbackScrubInteractionController.EndForFullScreen,");
        AssertContains(fullScreenWindowText, "ResetFlashbackTimelineAnimation = _flashbackTimelineController.ResetAnimationForFullScreen,");
        AssertContains(fullScreenWindowText, "SyncFlashbackTimelineToggle = _flashbackTimelineController.SyncToggle,");
        AssertContains(fullScreenControllerText, "var timelineVisibleAtExit = ShouldShowFlashbackTimeline();");
        AssertContains(fullScreenControllerText, "private bool ShouldShowFlashbackTimeline()\n        => _context.ViewModel.IsFlashbackEnabled && _context.ViewModel.IsFlashbackTimelineVisible;");
        AssertDoesNotContain(fullScreenWindowText, "private bool ShouldShowFlashbackTimeline()");
        AssertDoesNotContain(fullScreenWindowText, "=> _flashbackScrubInteractionController.EndForFullScreen();");
        AssertContains(flashbackScrubControllerText, "var carriedPosition = _lastPointerPosition;\n        Logger.Log($\"FLASHBACK_SCRUB_END_FULLSCREEN carried_position_ms={(long?)carriedPosition?.TotalMilliseconds}\");");
        AssertContains(flashbackScrubControllerText, "var ended = carriedPosition.HasValue\n            ? _context.ViewModel.FlashbackEndScrubAt(carriedPosition.Value)\n            : _context.ViewModel.FlashbackEndScrub();\n        if (!ended)");
        AssertContains(flashbackScrubControllerText, "ReportFlashbackPlaybackRejection(\"scrub end (fullscreen_enter)\", \"FLASHBACK_UI_SCRUB_END_REJECTED reason=fullscreen_enter\")");
        AssertDoesNotContain(flashbackScrubControllerText, "var carriedPosition = _context.ViewModel.FlashbackPlaybackPosition;");
        AssertDoesNotContain(fullScreenWindowText, "ReportFlashbackPlaybackRejection(\"nudge left\", \"FLASHBACK_UI_NUDGE_REJECTED direction=left\")");
        AssertDoesNotContain(fullScreenWindowText, "ReportFlashbackPlaybackRejection(\"nudge right\", \"FLASHBACK_UI_NUDGE_REJECTED direction=right\")");
        AssertDoesNotContain(fullScreenWindowText, "ReportFlashbackPlaybackRejection(\"nudge left\", \"FLASHBACK_UI_NUDGE_REJECTED direction=left\")");
        AssertDoesNotContain(fullScreenWindowText, "ReportFlashbackPlaybackRejection(\"nudge right\", \"FLASHBACK_UI_NUDGE_REJECTED direction=right\")");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.FullScreenFlashbackBridge.cs")),
            "Flashback fullscreen bridge is consolidated into the fullscreen adapter");
        AssertDoesNotContain(flashbackScrubText, "private bool _isFlashbackScrubbing;");
        AssertDoesNotContain(flashbackScrubText, "private TimeSpan? _lastScrubPointerPosition;");
        AssertDoesNotContain(flashbackScrubText, "private long _lastScrubUpdateTick;");
        AssertDoesNotContain(flashbackScrubControllerText, "var carriedPosition = _isScrubbing ? _context.ViewModel.FlashbackPlaybackPosition : (TimeSpan?)null;");
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Flashback.Interactions.cs")),
            "Flashback scrub adapter lives in the consolidated Flashback interaction adapter");
        AssertDoesNotContain(mainWindowText, "private bool _isFlashbackScrubbing;");
        AssertDoesNotContain(mainWindowText, "private TimeSpan? _lastScrubPointerPosition;");

        return Task.CompletedTask;
    }

    internal static Task FlashbackTimelineGeometry_PreservesScrubMath()
    {
        var geometryType = RequireType("Sussudio.Controllers.FlashbackTimelineGeometry");
        var tryComputeFraction = geometryType.GetMethod("TryComputeFraction", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FlashbackTimelineGeometry.TryComputeFraction was not found.");
        var tryComputePosition = geometryType.GetMethod("TryComputePosition", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FlashbackTimelineGeometry.TryComputePosition was not found.");
        var computePosition = geometryType.GetMethod("ComputePosition", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FlashbackTimelineGeometry.ComputePosition was not found.");
        var isUsableTrackDimension = geometryType.GetMethod("IsUsableTrackDimension", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FlashbackTimelineGeometry.IsUsableTrackDimension was not found.");
        var isUsableDuration = geometryType.GetMethod("IsUsableDuration", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FlashbackTimelineGeometry.IsUsableDuration was not found.");

        object?[] middleFractionArgs = { 50d, 100d, 0d };
        AssertEqual(true, (bool)tryComputeFraction.Invoke(null, middleFractionArgs)!, "middle fraction computed");
        AssertEqual(0.5d, (double)middleFractionArgs[2]!, "middle fraction value");

        object?[] leftClampArgs = { -10d, 100d, 0d };
        AssertEqual(true, (bool)tryComputeFraction.Invoke(null, leftClampArgs)!, "left fraction computed");
        AssertEqual(0d, (double)leftClampArgs[2]!, "left fraction clamps");

        object?[] rightClampArgs = { 120d, 100d, 0d };
        AssertEqual(true, (bool)tryComputeFraction.Invoke(null, rightClampArgs)!, "right fraction computed");
        AssertEqual(1d, (double)rightClampArgs[2]!, "right fraction clamps");

        object?[] invalidWidthArgs = { 50d, 0d, 1d };
        AssertEqual(false, (bool)tryComputeFraction.Invoke(null, invalidWidthArgs)!, "zero width rejects fraction");
        AssertEqual(0d, (double)invalidWidthArgs[2]!, "rejected fraction resets");

        object?[] invalidXArgs = { double.NaN, 100d, 1d };
        AssertEqual(false, (bool)tryComputeFraction.Invoke(null, invalidXArgs)!, "non-finite x rejects fraction");
        AssertEqual(0d, (double)invalidXArgs[2]!, "non-finite fraction resets");

        AssertEqual(TimeSpan.FromSeconds(5), computePosition.Invoke(null, new object[] { 0.25d, TimeSpan.FromSeconds(20) }), "compute position");
        AssertEqual(TimeSpan.Zero, computePosition.Invoke(null, new object[] { -1d, TimeSpan.FromSeconds(20) }), "compute position left clamp");
        AssertEqual(TimeSpan.FromSeconds(20), computePosition.Invoke(null, new object[] { 2d, TimeSpan.FromSeconds(20) }), "compute position right clamp");
        AssertEqual(TimeSpan.Zero, computePosition.Invoke(null, new object[] { 0.5d, TimeSpan.Zero }), "compute position zero duration");

        object?[] positionArgs = { 25d, 100d, TimeSpan.FromSeconds(20), TimeSpan.Zero };
        AssertEqual(true, (bool)tryComputePosition.Invoke(null, positionArgs)!, "position computed");
        AssertEqual(TimeSpan.FromSeconds(5), positionArgs[3], "position value");

        object?[] invalidPositionArgs = { 25d, 100d, TimeSpan.Zero, TimeSpan.FromSeconds(1) };
        AssertEqual(false, (bool)tryComputePosition.Invoke(null, invalidPositionArgs)!, "zero duration rejects position");
        AssertEqual(TimeSpan.Zero, invalidPositionArgs[3], "rejected position resets");

        AssertEqual(true, (bool)isUsableTrackDimension.Invoke(null, new object[] { 1d })!, "positive track is usable");
        AssertEqual(false, (bool)isUsableTrackDimension.Invoke(null, new object[] { double.PositiveInfinity })!, "infinite track is unusable");
        AssertEqual(true, (bool)isUsableDuration.Invoke(null, new object[] { TimeSpan.FromMilliseconds(1) })!, "positive duration is usable");
        AssertEqual(false, (bool)isUsableDuration.Invoke(null, new object[] { TimeSpan.Zero })!, "zero duration is unusable");

        return Task.CompletedTask;
    }


    internal static Task MainWindowFlashbackToggle_RollsBackUiStateOnFailure()
    {
        var flashbackWindowText = ReadMainWindowFlashbackAdapterSource();
        var flashbackCommandAdapterText = ReadMainWindowFlashbackAdapterSource();
        var flashbackCommandControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs")
            .Replace("\r\n", "\n");
        var flashbackTimelineText = ReadMainWindowFlashbackAdapterSource();
        var fullScreenText = ReadMainWindowShellChromeAdapterSource();
        var flashbackSettingsText = ReadMainWindowFlashbackAdapterSource();
        var flashbackTimelineControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackTimelineController.cs")
            .Replace("\r\n", "\n");
        var flashbackTimelineAnimationControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackTimelineController.cs")
            .Replace("\r\n", "\n");
        var flashbackSettingsControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs")
            .Replace("\r\n", "\n");
        var mainWindowText = ReadMainWindowCompositionSource();
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var flashbackPropertyChangedControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs")
            .Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var viewModelText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.FlashbackState.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainWindowText, "InitializeFlashbackCommandController();");
        AssertContains(mainWindowText, "InitializeFlashbackTimelineController();");
        AssertContains(mainWindowText, "InitializeFlashbackSettingsBindingController();");
        AssertContains(viewModelText, "partial void OnIsFlashbackEnabledChanged(bool value)");
        AssertContains(viewModelText, "IsFlashbackTimelineVisible = false;");
        AssertContains(bindingsText, "ApplyInitialFlashbackSettings();");
        AssertContains(flashbackSettingsText, "private FlashbackSettingsBindingController _flashbackSettingsBindingController = null!;");
        AssertContains(flashbackSettingsText, "ApplyFlashbackTimelineLockout = ApplyFlashbackTimelineLockout");
        AssertContains(flashbackSettingsControllerText, "_context.FlashbackEnabledToggle.IsOn = _context.ViewModel.IsFlashbackEnabled;");
        AssertContains(flashbackSettingsControllerText, "_context.ApplyFlashbackTimelineLockout();");
        AssertContains(propertyChangedText, "TryHandleFlashback = TryHandleFlashbackPropertyChanged");
        AssertContains(flashbackPropertyChangedText, "private void InitializeFlashbackPropertyChangedController()");
        AssertContains(flashbackPropertyChangedText, "ApplyTimelineLockout = ApplyFlashbackTimelineLockout,");
        AssertContains(flashbackPropertyChangedText, "ApplyTimelineVisibility = ApplyFlashbackTimelineVisibility,");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.IsFlashbackEnabled):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.ApplyTimelineLockout();");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.IsFlashbackTimelineVisible):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.ApplyTimelineVisibility(_context.IsTimelineVisible());");
        AssertContains(flashbackTimelineText, "private FlashbackTimelineController _flashbackTimelineController = null!;");
        AssertContains(flashbackTimelineText, "FlashbackToggle = FlashbackToggle,");
        AssertContains(flashbackTimelineText, "FlashbackTimelinePanel = FlashbackTimelinePanel,");
        AssertContains(flashbackTimelineText, "SnapPlayheadOnNextOpen = RequestFlashbackPlayheadSnapOnNextUpdate,");
        AssertContains(flashbackTimelineText, "ClearScrubInteraction = ClearFlashbackScrubInteractionForLockout,");
        AssertContains(flashbackTimelineText, "=> _flashbackTimelineController.OnToggleChecked();");
        AssertContains(flashbackTimelineText, "=> _flashbackTimelineController.ApplyLockout();");
        AssertContains(fullScreenText, "ResetFlashbackTimelineAnimation = _flashbackTimelineController.ResetAnimationForFullScreen,");
        AssertContains(flashbackTimelineControllerText, "public void ResetAnimationForFullScreen()");
        AssertDoesNotContain(flashbackTimelineText, "ResetFlashbackTimelineAnimationForFullScreen");
        AssertContains(flashbackTimelineText, "=> _flashbackScrubInteractionController.ClearForLockout();");
        AssertContains(flashbackTimelineControllerText, "internal sealed class FlashbackTimelineController");
        AssertContains(flashbackTimelineControllerText, "private readonly FlashbackTimelineAnimationController _animationController;");
        AssertContains(flashbackTimelineAnimationControllerText, "private Storyboard? _timelineStoryboard;");
        AssertContains(flashbackTimelineAnimationControllerText, "_snapPlayheadOnNextOpen();");
        AssertContains(flashbackTimelineAnimationControllerText, "private void CompleteAnimation(Storyboard storyboard)");
        AssertContains(flashbackTimelineControllerText, "private bool _suppressToggle;");
        AssertContains(flashbackTimelineControllerText, "if (!_context.ViewModel.IsFlashbackEnabled)\n        {\n            ApplyLockout();\n            return;\n        }");
        AssertContains(flashbackTimelineControllerText, "_context.ViewModel.IsFlashbackTimelineVisible = true;");
        AssertContains(flashbackTimelineControllerText, "_context.ViewModel.IsFlashbackTimelineVisible = false;");
        AssertContains(flashbackTimelineControllerText, "_context.FlashbackToggle.IsEnabled = flashbackEnabled;");
        AssertContains(flashbackTimelineControllerText, "_context.FlashbackTimelinePanel.IsHitTestVisible = flashbackEnabled;");
        AssertContains(flashbackTimelineControllerText, "SyncToggle(isVisible: false);");
        AssertContains(flashbackTimelineControllerText, "_context.ClearScrubInteraction();");
        AssertContains(flashbackTimelineControllerText, "CollapseImmediately();");
        AssertContains(flashbackTimelineControllerText, "=> _animationController.CollapseImmediately();");
        AssertContains(flashbackTimelineControllerText, "=> _animationController.ResetForFullScreen();");
        AssertContains(flashbackCommandAdapterText, "private FlashbackCommandController _flashbackCommandController = null!;");
        AssertContains(flashbackCommandAdapterText, "private void InitializeFlashbackCommandController()");
        AssertContains(flashbackCommandAdapterText, "FlashbackEnabledToggle = FlashbackEnabledToggle,");
        AssertContains(flashbackCommandAdapterText, "RunUiEventHandlerAsync = RunUiEventHandlerAsync");
        AssertContains(flashbackCommandAdapterText, "=> _flashbackCommandController.ToggleEnabled(nameof(FlashbackEnabledToggle_Toggled));");
        AssertContains(flashbackCommandControllerText, "if (_suppressFlashbackEnabledToggle)");
        AssertContains(flashbackCommandControllerText, "var requestedEnabled = _context.FlashbackEnabledToggle.IsOn;");
        AssertContains(flashbackCommandControllerText, "ApplyFlashbackEnabledToggleAsync(requestedEnabled)");
        AssertContains(flashbackCommandControllerText, "private async Task ApplyFlashbackEnabledToggleAsync(bool requestedEnabled)");
        AssertContains(flashbackCommandControllerText, "var previousEnabled = _context.ViewModel.IsFlashbackEnabled;");
        AssertContains(flashbackCommandControllerText, "_context.ViewModel.IsFlashbackEnabled = requestedEnabled;");
        AssertContains(flashbackCommandControllerText, "_context.ViewModel.IsFlashbackEnabled = previousEnabled;");
        AssertContains(flashbackCommandControllerText, "_suppressFlashbackEnabledToggle = true;");
        AssertContains(flashbackCommandControllerText, "_context.FlashbackEnabledToggle.IsOn = previousEnabled;");
        AssertContains(flashbackCommandControllerText, "_suppressFlashbackEnabledToggle = false;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Flashback", "FlashbackCommandController.cs")),
            "Flashback command controller folded into FlashbackUiControllers.cs");
        AssertDoesNotContain(mainWindowText, "private bool _suppressFlashbackEnabledToggle;");
        AssertDoesNotContain(flashbackWindowText, "ApplyFlashbackEnabledToggleAsync(requestedEnabled)");

        return Task.CompletedTask;
    }
}
