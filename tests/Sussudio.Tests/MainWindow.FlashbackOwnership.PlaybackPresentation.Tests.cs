using System;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task FlashbackPlaybackPresentation_LivesInController()
    {
        var flashbackText = ReadMainWindowFlashbackAdapterSource();
        var mainWindowText = ReadMainWindowCompositionSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackPlaybackPresentationController.cs").Replace("\r\n", "\n");
        var playbackCoordinatorText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackPlaybackUiCoordinator.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedFlashback.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackPropertyChangedController.cs").Replace("\r\n", "\n");

        AssertContains(flashbackText, "private FlashbackPlaybackPresentationController _flashbackPlaybackPresentationController = null!;");
        AssertContains(flashbackText, "private void InitializeFlashbackPlaybackPresentationController()");
        AssertContains(flashbackText, "PlayPauseIcon = FlashbackPlayPauseIcon,");
        AssertContains(flashbackText, "GoLiveButton = FlashbackGoLiveButton,");
        AssertContains(flashbackText, "BufferDurationText = FlashbackBufferDurationText,");
        AssertContains(flashbackText, "PlayheadTimeText = FlashbackPlayheadTimeText,");
        AssertContains(mainWindowText, "InitializeFlashbackPlaybackPresentationController();");
        AssertContains(controllerText, "internal sealed class FlashbackPlaybackPresentationController");
        AssertContains(controllerText, "public static string GetPlayPauseGlyph(FlashbackPlaybackState state)");
        AssertContains(controllerText, "public static bool IsGoLiveEnabled(FlashbackPlaybackState state)");
        AssertContains(controllerText, "public static string FormatPositionLabel(");
        AssertContains(controllerText, "\"\\uE769\"");
        AssertContains(controllerText, "\"\\uE768\"");
        AssertContains(controllerText, "return \"LIVE\";");
        AssertContains(controllerText, "return $\"-{FlashbackMarkerPresentationController.FormatDuration(gapFromLive)} / {totalText}\";");
        AssertContains(flashbackText, "private FlashbackPlaybackUiCoordinator _flashbackPlaybackUiCoordinator = null!;");
        AssertContains(flashbackText, "private void InitializeFlashbackPlaybackUiCoordinator()");
        AssertContains(mainWindowText, "InitializeFlashbackPlaybackUiCoordinator();");
        AssertOccursBefore(mainWindowText, "InitializeFlashbackPlaybackPresentationController();", "InitializeFlashbackPlaybackUiCoordinator();");
        AssertOccursBefore(mainWindowText, "InitializeFlashbackPlaybackUiCoordinator();", "InitializeFlashbackExportProgressPresentationController();");
        AssertContains(playbackCoordinatorText, "internal sealed class FlashbackPlaybackUiCoordinatorContext");
        AssertContains(playbackCoordinatorText, "internal sealed class FlashbackPlaybackUiCoordinator");
        AssertContains(playbackCoordinatorText, "_context.PlaybackPresentation.UpdateState(state);");
        AssertContains(playbackCoordinatorText, "_context.StartPlaybackPolling();");
        AssertContains(playbackCoordinatorText, "_context.StopPlaybackPolling();");
        AssertContains(playbackCoordinatorText, "_context.RefreshCtiMotion(\"state_change\");");
        AssertContains(playbackCoordinatorText, "public void UpdateBufferPresentation()\n    {\n        UpdateBufferFill();\n        UpdatePosition();\n        _context.UpdateMarkers();\n    }");
        AssertContains(playbackCoordinatorText, "_context.PlaybackPresentation.UpdateBufferFill(duration);");
        AssertContains(playbackCoordinatorText, "_context.PlaybackPresentation.UpdatePosition(");
        AssertContains(playbackCoordinatorText, "_context.RefreshCtiMotion(\"position_change\");");
        AssertContains(flashbackText, "private void UpdateFlashbackBufferPresentation()\n        => _flashbackPlaybackUiCoordinator.UpdateBufferPresentation();");
        AssertContains(flashbackPropertyChangedText, "UpdateBuffer = UpdateFlashbackBufferPresentation,");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.FlashbackBufferFillPercent):");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.FlashbackBufferDiskBytes):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.UpdateBuffer();");
        AssertDoesNotContain(flashbackPropertyChangedText, "UpdateFlashbackBufferFill();\n        UpdateFlashbackPositionUI();");
        AssertDoesNotContain(flashbackText, "_flashbackPlaybackPresentationController.UpdateState(state);");
        AssertDoesNotContain(flashbackText, "if (state == FlashbackPlaybackState.Playing)");
        AssertDoesNotContain(flashbackText, "RefreshFlashbackCtiMotion(\"position_change\");");
        AssertDoesNotContain(flashbackText, "FlashbackPlayPauseIcon.Glyph =");
        AssertDoesNotContain(flashbackText, "FlashbackGoLiveButton.IsEnabled =");
        AssertDoesNotContain(flashbackText, "FlashbackBufferDurationText.Text =");
        AssertDoesNotContain(flashbackText, "FlashbackPlayheadTimeText.Text =");

        var controllerType = RequireType("Sussudio.Controllers.FlashbackPlaybackPresentationController");
        var stateType = RequireType("Sussudio.Models.FlashbackPlaybackState");
        var getPlayPauseGlyph = controllerType.GetMethod("GetPlayPauseGlyph", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FlashbackPlaybackPresentationController.GetPlayPauseGlyph was not found.");
        var isGoLiveEnabled = controllerType.GetMethod("IsGoLiveEnabled", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FlashbackPlaybackPresentationController.IsGoLiveEnabled was not found.");
        var formatPositionLabel = controllerType.GetMethod("FormatPositionLabel", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FlashbackPlaybackPresentationController.FormatPositionLabel was not found.");

        object State(string name) => Enum.Parse(stateType, name);

        AssertEqual("\uE769", getPlayPauseGlyph.Invoke(null, new[] { State("Playing") })?.ToString(), "playing glyph");
        AssertEqual("\uE769", getPlayPauseGlyph.Invoke(null, new[] { State("Live") })?.ToString(), "live glyph");
        AssertEqual("\uE768", getPlayPauseGlyph.Invoke(null, new[] { State("Paused") })?.ToString(), "paused glyph");
        AssertEqual("\uE768", getPlayPauseGlyph.Invoke(null, new[] { State("Scrubbing") })?.ToString(), "scrubbing glyph");
        AssertEqual(false, (bool)isGoLiveEnabled.Invoke(null, new[] { State("Live") })!, "live disables go-live button");
        AssertEqual(false, (bool)isGoLiveEnabled.Invoke(null, new[] { State("Disabled") })!, "disabled disables go-live button");
        AssertEqual(true, (bool)isGoLiveEnabled.Invoke(null, new[] { State("Paused") })!, "paused enables go-live button");
        AssertEqual(
            "LIVE",
            formatPositionLabel.Invoke(null, new object[] { State("Live"), TimeSpan.FromSeconds(125), TimeSpan.FromSeconds(5) })?.ToString(),
            "live position label");
        AssertEqual(
            "-0:05 / 2:05",
            formatPositionLabel.Invoke(null, new object[] { State("Paused"), TimeSpan.FromSeconds(125), TimeSpan.FromSeconds(5) })?.ToString(),
            "buffered position label");

        return Task.CompletedTask;
    }
}
