using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

static partial class Program
{
    private static Task MainWindowFlashbackScrub_EndsOnReleaseCancelAndCaptureLost()
    {
        var flashbackWindowText = ReadRepoFile("Sussudio/MainWindow.Flashback.cs")
            .Replace("\r\n", "\n");
        var flashbackScrubText = ReadRepoFile("Sussudio/MainWindow.FlashbackScrub.cs")
            .Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var fullScreenWindowText = ReadRepoFile("Sussudio/MainWindow.FullScreen.cs")
            .Replace("\r\n", "\n");
        var fullScreenControllerText = (
            ReadRepoFile("Sussudio/Controllers/FullScreenController.cs")
            + "\n" + ReadRepoFile("Sussudio/Controllers/FullScreenController.Transitions.cs"))
            .Replace("\r\n", "\n");
        var xamlText = ReadRepoFile("Sussudio/MainWindow.xaml")
            .Replace("\r\n", "\n");

        AssertContains(xamlText, "PointerReleased=\"FlashbackScrubArea_PointerReleased\"");
        AssertContains(xamlText, "PointerCanceled=\"FlashbackScrubArea_PointerCanceled\"");
        AssertContains(xamlText, "PointerCaptureLost=\"FlashbackScrubArea_PointerCaptureLost\"");
        AssertContains(flashbackScrubText, "Flashback pointer scrub interaction");
        AssertContains(flashbackScrubText, "private bool _isFlashbackScrubbing;");
        AssertContains(flashbackScrubText, "private TimeSpan? _lastScrubPointerPosition;");
        AssertContains(flashbackScrubText, "private long _lastScrubUpdateTick;");
        AssertContains(flashbackScrubText, "private void EndFlashbackScrubInteraction(UIElement? element, Pointer pointer, string reason, TimeSpan? releasePosition = null)");
        AssertContains(flashbackScrubText, "if (!ViewModel.FlashbackBeginScrub(targetPosition))\n        {\n            _lastScrubPointerPosition = null;\n            ViewModel.ReportFlashbackPlaybackRejection(\"scrub begin\", \"FLASHBACK_UI_SCRUB_BEGIN_REJECTED\");\n            return;\n        }");
        AssertContains(flashbackScrubText, "if (!ViewModel.FlashbackUpdateScrub(targetPosition))\n        {\n            ViewModel.ReportFlashbackPlaybackRejection(\"scrub update\", \"FLASHBACK_UI_SCRUB_UPDATE_REJECTED\");\n            EndFlashbackScrubInteraction(sender as UIElement, e.Pointer, \"update_rejected\");\n            return;\n        }");
        AssertContains(flashbackScrubText, "private void FlashbackScrubArea_PointerReleased(object sender, PointerRoutedEventArgs e)");
        AssertContains(flashbackScrubText, "TimeSpan? releasePosition = null;\n        if (_isFlashbackScrubbing)");
        AssertContains(flashbackScrubText, "var targetPosition = ComputeFlashbackScrubPosition(e);\n            releasePosition = targetPosition;\n            _lastScrubPointerPosition = targetPosition;\n            if (!ViewModel.FlashbackUpdateScrub(targetPosition))");
        AssertContains(flashbackScrubText, "ViewModel.ReportFlashbackPlaybackRejection(\"scrub release update\", \"FLASHBACK_UI_SCRUB_RELEASE_UPDATE_REJECTED\");");
        AssertContains(flashbackScrubText, "EndFlashbackScrubInteraction(sender as UIElement, e.Pointer, \"released\", releasePosition);");
        AssertContains(flashbackWindowText, "ReportFlashbackPlaybackRejection(\"set in point\", \"FLASHBACK_UI_SET_IN_REJECTED\")");
        AssertContains(flashbackWindowText, "ReportFlashbackPlaybackRejection(\"set out point\", \"FLASHBACK_UI_SET_OUT_REJECTED\")");
        AssertContains(flashbackWindowText, "ReportFlashbackPlaybackRejection(\"clear in/out\", \"FLASHBACK_UI_CLEAR_INOUT_REJECTED\")");
        AssertContains(flashbackWindowText, "Logger.Log($\"FLASHBACK_UI_SET_IN pos_ms={(long)pos.Value.TotalMilliseconds}\");");
        AssertContains(flashbackWindowText, "Logger.Log($\"FLASHBACK_UI_SET_OUT pos_ms={(long)pos.Value.TotalMilliseconds}\");");
        AssertContains(flashbackWindowText, "Logger.Log(\"FLASHBACK_UI_CLEAR_INOUT\");");
        AssertContains(flashbackWindowText, "ReportFlashbackPlaybackRejection(\"pause\", \"FLASHBACK_UI_PAUSE_REJECTED\")");
        AssertContains(flashbackWindowText, "ReportFlashbackPlaybackRejection(\"play\", \"FLASHBACK_UI_PLAY_REJECTED\")");
        AssertContains(flashbackWindowText, "ReportFlashbackPlaybackRejection(\"go live\", \"FLASHBACK_UI_GOLIVE_REJECTED\")");
        AssertContains(flashbackWindowText, "Logger.Log(\"FLASHBACK_UI_PAUSE\");");
        AssertContains(flashbackWindowText, "Logger.Log(\"FLASHBACK_UI_PLAY\");");
        AssertContains(flashbackWindowText, "Logger.Log(\"FLASHBACK_UI_GOLIVE\");");
        AssertContains(flashbackScrubText, "_isFlashbackScrubbing = true;\n        _lastScrubPointerPosition = targetPosition;\n        _lastScrubUpdateTick = 0;\n        (sender as UIElement)?.CapturePointer(e.Pointer);");
        AssertContains(flashbackScrubText, "var carriedPosition = _isFlashbackScrubbing ? _lastScrubPointerPosition : null;");
        AssertContains(flashbackScrubText, "var ended = releasePosition.HasValue\n            ? ViewModel.FlashbackEndScrubAt(releasePosition.Value)\n            : ViewModel.FlashbackEndScrub();\n        if (!ended)\n        {\n            ViewModel.ReportFlashbackPlaybackRejection($\"scrub end ({reason})\", $\"FLASHBACK_UI_SCRUB_END_REJECTED reason={reason}\");\n        }");
        AssertContains(flashbackScrubText, "_isFlashbackScrubbing = false;\n        _lastScrubUpdateTick = 0;\n        _lastScrubPointerPosition = null;\n        element?.ReleasePointerCapture(pointer);");
        AssertContains(flashbackScrubText, "FLASHBACK_UI_SCRUB_END");
        AssertContains(flashbackScrubText, "FlashbackScrubArea_PointerCanceled");
        AssertContains(flashbackScrubText, "FlashbackScrubArea_PointerCaptureLost");
        AssertContains(flashbackScrubText, "if (!TryComputeFlashbackTimelineFraction(pos.X, width, out var fraction)) return;");
        AssertContains(flashbackScrubText, "if (!TryComputeFlashbackTimelineFraction(pos.X, width, out var fraction)) return TimeSpan.Zero;");
        AssertContains(flashbackScrubText, "private static bool TryComputeFlashbackTimelineFraction(double x, double width, out double fraction)");
        AssertContains(flashbackScrubText, "if (!IsUsableFlashbackTrackDimension(width) || !double.IsFinite(x))");
        AssertContains(flashbackScrubText, "private static bool IsUsableFlashbackTrackDimension(double value)\n        => double.IsFinite(value) && value > 0;");
        AssertContains(flashbackScrubText, "private static bool IsUsableFlashbackDuration(TimeSpan value)\n        => double.IsFinite(value.TotalSeconds) && value > TimeSpan.Zero;");
        AssertContains(fullScreenWindowText, "if (ViewModel.IsFlashbackEnabled && FlashbackTimelinePanel.Visibility == Visibility.Visible)");
        AssertContains(fullScreenWindowText, "ReportFlashbackPlaybackRejection(\"nudge left\", \"FLASHBACK_UI_NUDGE_REJECTED direction=left\")");
        AssertContains(fullScreenWindowText, "ReportFlashbackPlaybackRejection(\"nudge right\", \"FLASHBACK_UI_NUDGE_REJECTED direction=right\")");
        AssertContains(fullScreenControllerText, "var timelineVisibleAtExit = _context.ShouldShowFlashbackTimeline();");
        AssertContains(fullScreenWindowText, "private bool ShouldShowFlashbackTimeline()");
        AssertContains(fullScreenWindowText, "return ViewModel.IsFlashbackEnabled && ViewModel.IsFlashbackTimelineVisible;");
        AssertContains(fullScreenWindowText, "var carriedPosition = _lastScrubPointerPosition;\n        Logger.Log($\"FLASHBACK_SCRUB_END_FULLSCREEN carried_position_ms={(long?)carriedPosition?.TotalMilliseconds}\");");
        AssertContains(fullScreenWindowText, "var ended = carriedPosition.HasValue\n            ? ViewModel?.FlashbackEndScrubAt(carriedPosition.Value) ?? false\n            : ViewModel?.FlashbackEndScrub() ?? false;\n        if (!ended)");
        AssertContains(fullScreenWindowText, "ReportFlashbackPlaybackRejection(\"scrub end (fullscreen_enter)\", \"FLASHBACK_UI_SCRUB_END_REJECTED reason=fullscreen_enter\")");
        AssertDoesNotContain(fullScreenWindowText, "var carriedPosition = ViewModel?.FlashbackPlaybackPosition;");
        AssertDoesNotContain(flashbackScrubText, "var carriedPosition = _isFlashbackScrubbing ? ViewModel.FlashbackPlaybackPosition : (TimeSpan?)null;");
        AssertDoesNotContain(flashbackWindowText, "private void FlashbackScrubArea_PointerPressed(");
        AssertDoesNotContain(mainWindowText, "private bool _isFlashbackScrubbing;");
        AssertDoesNotContain(mainWindowText, "private TimeSpan? _lastScrubPointerPosition;");

        return Task.CompletedTask;
    }
}
