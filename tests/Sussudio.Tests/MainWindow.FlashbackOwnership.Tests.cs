using System;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task FlashbackPollingTimers_LiveInController()
    {
        var flashbackText = ReadMainWindowFlashbackAdapterSource();
        var mainWindowText = ReadMainWindowCompositionSource();
        var pollingAdapterText = ReadMainWindowFlashbackAdapterSource();
        var timelineAdapterText = ReadMainWindowFlashbackAdapterSource();
        var shutdownCleanupText = ReadMainWindowCompositionSource();
        var shutdownCleanupControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowControllers.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var playbackCoordinatorText = controllerText;

        AssertContains(pollingAdapterText, "private FlashbackPollingController _flashbackPollingController = null!;");
        AssertContains(pollingAdapterText, "private void InitializeFlashbackPollingController()");
        AssertContains(pollingAdapterText, "IsWindowClosing = () => _isWindowClosing,");
        AssertContains(pollingAdapterText, "=> _flashbackPollingController.StartStatusPolling();");
        AssertContains(pollingAdapterText, "_flashbackPollingController.StopStatusPolling();");
        AssertContains(pollingAdapterText, "StopFlashbackCtiAnchorTimer();");
        AssertContains(pollingAdapterText, "=> _flashbackPollingController.StartPlaybackPolling();");
        AssertContains(pollingAdapterText, "=> _flashbackPollingController.StopPlaybackPolling();");
        AssertContains(mainWindowText, "InitializeFlashbackPollingController();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Flashback.Interactions.cs")),
            "Flashback polling adapter folded into the MainWindow root composition adapter");
        AssertContains(timelineAdapterText, "StartStatusPolling = StartFlashbackStatusPolling,");
        AssertContains(shutdownCleanupText, "StopFlashbackStatusPolling();");
        AssertContains(shutdownCleanupControllerText, "_context.StopTimers();");
        AssertContains(flashbackText, "StartPlaybackPolling = StartFlashbackPlaybackPolling,");
        AssertContains(flashbackText, "StopPlaybackPolling = StopFlashbackPlaybackPolling,");
        AssertContains(playbackCoordinatorText, "_context.StartPlaybackPolling();");
        AssertContains(playbackCoordinatorText, "_context.StopPlaybackPolling();");
        AssertContains(controllerText, "internal sealed class FlashbackPollingController");
        AssertContains(controllerText, "private DispatcherQueueTimer? _statusTimer;");
        AssertContains(controllerText, "private DispatcherQueueTimer? _playbackTimer;");
        AssertContains(controllerText, "public void StartStatusPolling()");
        AssertContains(controllerText, "public void StopStatusPolling()");
        AssertContains(controllerText, "public void StartPlaybackPolling()");
        AssertContains(controllerText, "public void StopPlaybackPolling()");
        AssertContains(controllerText, "_context.ViewModel.UpdateFlashbackBufferStatus();");
        AssertContains(controllerText, "_context.ViewModel.FlashbackPlaybackPosition = playback.PlaybackPosition;");
        AssertContains(controllerText, "FLASHBACK_STATUS_TIMER_FAIL");
        AssertContains(controllerText, "FLASHBACK_PLAYBACK_TIMER_FAIL");
        AssertDoesNotContain(flashbackText, "private DispatcherQueueTimer? _flashbackStatusTimer;");
        AssertDoesNotContain(flashbackText, "private void FlashbackStatusTimer_Tick(");
        AssertDoesNotContain(flashbackText, "private void FlashbackPlaybackTimer_Tick(");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlayheadMotion_LivesInController()
    {
        var flashbackText = ReadMainWindowFlashbackAdapterSource();
        var mainWindowText = ReadMainWindowCompositionSource();
        var scrubText = ReadMainWindowFlashbackAdapterSource();
        var scrubControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var playheadText = ReadMainWindowFlashbackAdapterSource();
        var pollingAdapterText = ReadMainWindowFlashbackAdapterSource();
        var controllerRootText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var controllerText = controllerRootText;
        var playbackCoordinatorText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");

        AssertContains(playheadText, "XAML-facing Flashback playhead motion adapter");
        AssertContains(playheadText, "private FlashbackPlayheadMotionController _flashbackPlayheadMotionController = null!;");
        AssertContains(playheadText, "private void InitializeFlashbackPlayheadMotionController()");
        AssertContains(playheadText, "IsScrubbing = () => _flashbackScrubInteractionController.IsScrubbing,");
        AssertContains(playheadText, "private void RequestFlashbackPlayheadSnapOnNextUpdate()");
        AssertContains(playheadText, "private void PositionFlashbackMagneticPlayhead(double x, double trackWidth)");
        AssertContains(playheadText, "private void RefreshFlashbackCtiMotion(string reason)");
        AssertContains(playheadText, "=> _flashbackPlayheadMotionController.RefreshCtiMotion(reason);");
        AssertContains(playheadText, "private void StopFlashbackCtiAnchorTimer()");
        AssertContains(playheadText, "=> _flashbackPlayheadMotionController.StopCtiAnchorTimer();");
        AssertContains(mainWindowText, "InitializeFlashbackPlayheadMotionController();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Flashback.Interactions.cs")),
            "Flashback playhead adapter folded into the MainWindow root composition adapter");
        AssertOccursBefore(mainWindowText, "InitializeFlashbackScrubInteractionController();", "InitializeFlashbackPlayheadMotionController();");
        AssertOccursBefore(mainWindowText, "InitializeFlashbackPlayheadMotionController();", "InitializeFlashbackTimelineController();");
        AssertContains(controllerRootText, "internal sealed class FlashbackPlayheadMotionControllerContext");
        AssertContains(controllerRootText, "internal sealed class FlashbackPlayheadMotionController");
        AssertContains(controllerRootText, "private enum FlashbackPlayheadMotion");
        AssertContains(controllerRootText, "private Visual? _flashbackPlayheadVisual;");
        AssertContains(controllerRootText, "private DispatcherQueueTimer? _flashbackCtiAnchorTimer;");
        AssertContains(controllerRootText, "private CompositionEasingFunction? _flashbackPlayheadEaseLinear;");
        AssertContains(controllerRootText, "private bool _snapFlashbackPlayheadOnNextUpdate;");
        AssertContains(controllerRootText, "public void RequestSnapOnNextUpdate()");
        AssertContains(controllerRootText, "public void PositionMagneticPlayhead(double x, double trackWidth)");
        AssertContains(controllerText, "public void RefreshCtiMotion(string reason)");
        AssertContains(controllerText, "public void StopCtiAnchorTimer()");
        AssertContains(controllerText, "private void StartFlashbackCtiAnchorTimer()");
        AssertContains(controllerText, "private void FlashbackCtiAnchorTimer_Tick(DispatcherQueueTimer sender, object args)");
        AssertContains(controllerText, "FlashbackTimelineGeometry.IsUsableTrackDimension(trackW)");
        AssertContains(controllerText, "state == FlashbackPlaybackState.Live");
        AssertContains(controllerText, "SnapPlayheadVisualsToFraction(1.0, trackW);");
        AssertContains(controllerText, "StartLinearPlayheadExtrapolation(");
        AssertContains(controllerText, "RefreshCtiMotion(\"anchor_tick\");");
        AssertContains(controllerText, "FLASHBACK_CTI_ANCHOR_TICK_FAIL");
        AssertContains(controllerText, "private void EnsureFlashbackPlayheadVisuals()");
        AssertContains(controllerText, "private void PositionFlashbackPlayhead(double x, double trackWidth, FlashbackPlayheadMotion motion)");
        AssertContains(controllerText, "private void StartLinearPlayheadExtrapolation(");
        AssertContains(controllerText, "private static void StartLinearKeyframe(");
        AssertContains(controllerText, "private void SnapPlayheadVisualsToFraction(");
        AssertContains(controllerText, "private void AnimateFlashbackPlayheadX(");
        AssertContains(controllerText, "private static void SnapFlashbackPlayheadX(");
        AssertContains(controllerText, "ElementCompositionPreview.SetIsTranslationEnabled(_context.Playhead, true);");
        AssertContains(controllerText, "Canvas.SetLeft(_context.Playhead, 0);");
        AssertContains(controllerText, "var labelX = Math.Clamp(x - labelW / 2, 0, Math.Max(0, trackWidth - labelW));");
        AssertContains(controllerText, "var lineX = (float)(x - 1);");
        AssertContains(controllerText, "var handleX = (float)(x - 5);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Flashback", "FlashbackPlayheadMotionController.Cti.cs")),
            "Flashback playhead CTI partial is consolidated into the motion controller root");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Flashback", "FlashbackPlayheadMotionController.Visuals.cs")),
            "Flashback playhead visuals partial is consolidated into the motion controller root");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Flashback", "FlashbackPlayheadMotionController.cs")),
            "Flashback playhead motion folded into Flashback UI controllers");
        AssertContains(scrubText, "PositionMagneticPlayhead = PositionFlashbackMagneticPlayhead,");
        AssertContains(scrubControllerText, "_context.PositionMagneticPlayhead(x, width);");
        AssertContains(playbackCoordinatorText, "_context.RefreshCtiMotion(\"state_change\");");
        AssertContains(pollingAdapterText, "StopFlashbackCtiAnchorTimer();");
        AssertContains(playbackCoordinatorText, "_context.RequestPlayheadSnapOnNextUpdate();");
        AssertDoesNotContain(playheadText, "private DispatcherQueueTimer? _flashbackCtiAnchorTimer;");
        AssertDoesNotContain(playheadText, "private void StartLinearPlayheadExtrapolation(");
        AssertDoesNotContain(playheadText, "FLASHBACK_CTI_ANCHOR_TICK_FAIL");
        AssertDoesNotContain(flashbackText, "private enum FlashbackPlayheadMotion");
        AssertDoesNotContain(flashbackText, "private Visual? _flashbackPlayheadVisual;");
        AssertDoesNotContain(flashbackText, "private DispatcherQueueTimer? _flashbackCtiAnchorTimer;");
        AssertDoesNotContain(flashbackText, "private void StartLinearPlayheadExtrapolation(");

        return Task.CompletedTask;
    }

    internal static Task FlashbackPlaybackPresentation_LivesInController()
    {
        var flashbackText = ReadMainWindowFlashbackAdapterSource();
        var mainWindowText = ReadMainWindowCompositionSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var playbackCoordinatorText = controllerText;
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");

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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Flashback", "FlashbackPlaybackUiCoordinator.cs")),
            "Flashback playback presentation and UI coordination live with Flashback UI controllers");
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

    internal static Task FlashbackSettingsBindings_LiveInController()
    {
        var flashbackText = ReadMainWindowFlashbackAdapterSource();
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var adapterText = ReadMainWindowFlashbackAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var commandAdapterText = ReadMainWindowFlashbackAdapterSource();
        var commandControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private FlashbackSettingsBindingController _flashbackSettingsBindingController = null!;");
        AssertContains(adapterText, "private void InitializeFlashbackSettingsBindingController()");
        AssertContains(adapterText, "FlashbackEnabledToggle = FlashbackEnabledToggle,");
        AssertContains(adapterText, "FlashbackGpuDecodeToggle = FlashbackGpuDecodeToggle,");
        AssertContains(adapterText, "FlashbackBufferDurationCombo = FlashbackBufferDurationCombo,");
        AssertContains(adapterText, "ApplyFlashbackTimelineLockout = ApplyFlashbackTimelineLockout");
        AssertContains(adapterText, "private void ApplyInitialFlashbackSettings()");
        AssertContains(adapterText, "=> _flashbackSettingsBindingController.ApplyInitialSettings();");
        AssertContains(adapterText, "private void AttachFlashbackSettingsBindings()");
        AssertContains(adapterText, "=> _flashbackSettingsBindingController.AttachBindings();");
        AssertContains(adapterText, "private void SyncFlashbackGpuDecodeSetting()");
        AssertContains(adapterText, "=> _flashbackSettingsBindingController.SyncGpuDecodeToggle();");
        AssertContains(adapterText, "private void SyncFlashbackBufferDurationSetting()");
        AssertContains(adapterText, "=> _flashbackSettingsBindingController.SyncBufferDurationSelection();");
        AssertContains(adapterText, "private void FlashbackBufferDurationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)");
        AssertContains(adapterText, "if (ViewModel == null || _flashbackSettingsBindingController == null)");
        AssertContains(adapterText, "_flashbackSettingsBindingController.HandleBufferDurationSelectionChanged();");
        AssertContains(mainWindowText, "InitializeFlashbackSettingsBindingController();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Flashback.Interactions.cs")),
            "Flashback settings adapter folded into the MainWindow root composition adapter");
        AssertContains(bindingsText, "ApplyInitialFlashbackSettings();");
        AssertContains(bindingsText, "AttachFlashbackSettingsBindings();");

        AssertContains(controllerText, "internal sealed class FlashbackSettingsBindingControllerContext");
        AssertContains(controllerText, "internal sealed class FlashbackSettingsBindingController");
        AssertContains(controllerText, "public void ApplyInitialSettings()");
        AssertContains(controllerText, "_context.FlashbackEnabledToggle.IsOn = _context.ViewModel.IsFlashbackEnabled;");
        AssertContains(controllerText, "_context.FlashbackGpuDecodeToggle.IsOn = _context.ViewModel.FlashbackGpuDecode;");
        AssertContains(controllerText, "_context.ApplyFlashbackTimelineLockout();");
        AssertContains(controllerText, "SyncBufferDurationSelection();");
        AssertContains(controllerText, "public void AttachBindings()");
        AssertContains(controllerText, "_context.FlashbackGpuDecodeToggle.Toggled +=");
        AssertContains(controllerText, "_context.ViewModel.FlashbackGpuDecode = _context.FlashbackGpuDecodeToggle.IsOn;");
        AssertContains(controllerText, "public void SyncGpuDecodeToggle()");
        AssertContains(controllerText, "_context.FlashbackGpuDecodeToggle.IsOn = _context.ViewModel.FlashbackGpuDecode;");
        AssertContains(controllerText, "public void SyncBufferDurationSelection()");
        AssertContains(controllerText, "currentTag == selectedMinutes");
        AssertContains(controllerText, "_context.FlashbackBufferDurationCombo.SelectedItem = item;");
        AssertContains(controllerText, "public void HandleBufferDurationSelectionChanged()");
        AssertContains(controllerText, "int.TryParse(tag, out var minutes)");
        AssertContains(controllerText, "_context.ViewModel.FlashbackBufferMinutes = minutes;");
        AssertContains(controllerText, "FLASHBACK_UI_BUFFER_DURATION_CHANGED");
        AssertContains(propertyChangedText, "TryHandleFlashback = TryHandleFlashbackPropertyChanged");
        AssertContains(flashbackPropertyChangedText, "SyncGpuDecodeSetting = SyncFlashbackGpuDecodeSetting,");
        AssertContains(flashbackPropertyChangedText, "SyncBufferDurationSetting = SyncFlashbackBufferDurationSetting");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.FlashbackGpuDecode):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.SyncGpuDecodeSetting();");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.FlashbackBufferMinutes):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.SyncBufferDurationSetting();");

        AssertContains(commandAdapterText, "private FlashbackCommandController _flashbackCommandController = null!;");
        AssertContains(commandAdapterText, "private void InitializeFlashbackCommandController()");
        AssertContains(commandAdapterText, "private void FlashbackEnabledToggle_Toggled(object sender, RoutedEventArgs e)");
        AssertContains(commandAdapterText, "=> _flashbackCommandController.ToggleEnabled(nameof(FlashbackEnabledToggle_Toggled));");
        AssertContains(commandAdapterText, "private void FlashbackApplyButton_Click(object sender, RoutedEventArgs e)");
        AssertContains(commandAdapterText, "=> _flashbackCommandController.ApplySettings(nameof(FlashbackApplyButton_Click));");
        AssertContains(commandControllerText, "private async Task ApplyFlashbackEnabledToggleAsync(bool requestedEnabled)");
        AssertContains(commandControllerText, "=> _ = _context.RunUiEventHandlerAsync(() => _context.ViewModel.RestartFlashbackAsync(), operationName);");
        AssertContains(commandControllerText, "public bool HandleFullScreenKeyboardCommand(VirtualKey key)");
        AssertContains(commandControllerText, "NudgePlayback(TimeSpan.FromSeconds(-1), \"nudge left\", \"FLASHBACK_UI_NUDGE_REJECTED direction=left\");");
        AssertContains(commandControllerText, "NudgePlayback(TimeSpan.FromSeconds(1), \"nudge right\", \"FLASHBACK_UI_NUDGE_REJECTED direction=right\");");
        AssertContains(mainWindowText, "InitializeFlashbackCommandController();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Flashback.Interactions.cs")),
            "Flashback command adapter folded into the MainWindow root composition adapter");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Flashback", "FlashbackCommandController.cs")),
            "Flashback command controller folded into FlashbackUiControllers.cs");
        AssertDoesNotContain(flashbackText, "private async Task ApplyFlashbackEnabledToggleAsync(bool requestedEnabled)");
        AssertDoesNotContain(bindingsText, "FlashbackEnabledToggle.IsOn = ViewModel.IsFlashbackEnabled;");
        AssertDoesNotContain(bindingsText, "FlashbackGpuDecodeToggle.IsOn = ViewModel.FlashbackGpuDecode;");
        AssertDoesNotContain(bindingsText, "FlashbackGpuDecodeToggle.Toggled +=");
        AssertDoesNotContain(bindingsText, "foreach (ComboBoxItem item in FlashbackBufferDurationCombo.Items)");
        AssertDoesNotContain(flashbackText, "foreach (ComboBoxItem item in FlashbackBufferDurationCombo.Items)");
        AssertDoesNotContain(flashbackPropertyChangedText, "FlashbackGpuDecodeToggle.IsOn = ViewModel.FlashbackGpuDecode;");
        AssertDoesNotContain(flashbackPropertyChangedText, "FlashbackBufferDurationCombo.SelectedItem = item;");

        return Task.CompletedTask;
    }

    internal static Task FlashbackTimelineTrackLayout_LivesInController()
    {
        var flashbackText = ReadMainWindowFlashbackAdapterSource();
        var timelineAdapterText = ReadMainWindowFlashbackAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var animationControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var playbackCoordinatorText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(timelineAdapterText, "FlashbackTrackBackground = FlashbackTrackBackground,");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Flashback.Interactions.cs")),
            "Flashback timeline adapter folded into the MainWindow root composition adapter");
        AssertContains(timelineAdapterText, "FlashbackScrubArea = FlashbackScrubArea,");
        AssertContains(timelineAdapterText, "FlashbackPlayhead = FlashbackPlayhead,");
        AssertContains(timelineAdapterText, "FlashbackLiveEdge = FlashbackLiveEdge,");
        AssertContains(controllerText, "public required FrameworkElement FlashbackTrackBackground { get; init; }");
        AssertContains(controllerText, "public required FrameworkElement FlashbackScrubArea { get; init; }");
        AssertContains(controllerText, "public required FrameworkElement FlashbackPlayhead { get; init; }");
        AssertContains(controllerText, "public required FrameworkElement FlashbackLiveEdge { get; init; }");
        AssertContains(controllerText, "public void ApplyTrackSize(double width, double height)");
        AssertContains(controllerText, "_context.FlashbackTrackBackground.Width = width;");
        AssertContains(controllerText, "_context.FlashbackTrackBackground.Height = height;");
        AssertContains(controllerText, "_context.FlashbackScrubArea.Width = width;");
        AssertContains(controllerText, "_context.FlashbackScrubArea.Height = height;");
        AssertContains(controllerText, "_context.FlashbackPlayhead.Height = height;");
        AssertContains(controllerText, "_context.FlashbackLiveEdge.Height = height;");
        AssertContains(controllerText, "Canvas.SetLeft(_context.FlashbackLiveEdge, width - 2);");
        AssertContains(controllerText, "private readonly FlashbackTimelineAnimationController _animationController;");
        AssertContains(controllerText, "_animationController.Animate(show: true);");
        AssertContains(controllerText, "_animationController.Animate(show: false);");
        AssertContains(animationControllerText, "internal sealed class FlashbackTimelineAnimationController");
        AssertContains(animationControllerText, "private Storyboard? _timelineStoryboard;");
        AssertContains(animationControllerText, "public bool IsAnimating { get; private set; }");
        AssertContains(animationControllerText, "public void CollapseImmediately()");
        AssertContains(animationControllerText, "public void ResetForFullScreen()");
        AssertContains(animationControllerText, "private void CompleteAnimation(Storyboard storyboard)");
        AssertContains(controllerText, "private Storyboard? _timelineStoryboard;");
        AssertContains(controllerText, "new DoubleAnimation");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Flashback", "FlashbackTimelineAnimationController.cs")),
            "timeline animation folded into Flashback UI controllers");
        AssertContains(flashbackText, "private void FlashbackTrack_SizeChanged(object sender, SizeChangedEventArgs e)");
        AssertContains(flashbackText, "=> _flashbackPlaybackUiCoordinator.HandleTrackSizeChanged(e.NewSize.Width, e.NewSize.Height);");
        AssertContains(playbackCoordinatorText, "public void HandleTrackSizeChanged(double width, double height)");
        AssertContains(playbackCoordinatorText, "_context.ApplyTrackSize(width, height);");
        AssertOccursBefore(playbackCoordinatorText, "_context.ApplyTrackSize(width, height);", "_context.RequestPlayheadSnapOnNextUpdate();");
        AssertOccursBefore(playbackCoordinatorText, "_context.RequestPlayheadSnapOnNextUpdate();", "UpdatePosition();");
        AssertOccursBefore(playbackCoordinatorText, "UpdatePosition();", "_context.UpdateMarkers();");
        AssertOccursBefore(playbackCoordinatorText, "_context.UpdateMarkers();", "_context.RefreshCtiMotion(\"size_changed\");");
        AssertContains(agentMapText, "timeline visibility, lockout, toggle synchronization, timeline track layout");
        AssertContains(agentMapText, "sizing, show/hide storyboard state");
        AssertContains(agentMapText, "show/hide storyboard state");
        AssertDoesNotContain(flashbackText, "FlashbackTrackBackground.Width =");
        AssertDoesNotContain(flashbackText, "FlashbackTrackBackground.Height =");
        AssertDoesNotContain(flashbackText, "FlashbackScrubArea.Width =");
        AssertDoesNotContain(flashbackText, "FlashbackScrubArea.Height =");
        AssertDoesNotContain(flashbackText, "FlashbackPlayhead.Height =");
        AssertDoesNotContain(flashbackText, "FlashbackLiveEdge.Height =");
        AssertDoesNotContain(flashbackText, "Canvas.SetLeft(FlashbackLiveEdge");

        return Task.CompletedTask;
    }

    internal static Task FlashbackMarkerPresentation_LivesInController()
    {
        var flashbackText = ReadMainWindowFlashbackAdapterSource();
        var mainWindowText = ReadMainWindowCompositionSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var playbackCoordinatorText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");

        AssertContains(flashbackText, "private FlashbackMarkerPresentationController _flashbackMarkerPresentationController = null!;");
        AssertContains(flashbackText, "private void InitializeFlashbackMarkerPresentationController()");
        AssertContains(flashbackText, "ScrubArea = FlashbackScrubArea,");
        AssertContains(flashbackText, "InPointMarker = FlashbackInPointMarker,");
        AssertContains(flashbackText, "OutPointMarker = FlashbackOutPointMarker,");
        AssertContains(flashbackText, "SelectionRegion = FlashbackSelectionRegion,");
        AssertContains(flashbackText, "=> _flashbackMarkerPresentationController.UpdateMarkers(");
        AssertContains(flashbackText, "ViewModel.FlashbackBufferFilledDuration,");
        AssertContains(flashbackText, "ViewModel.FlashbackInPoint,");
        AssertContains(flashbackText, "ViewModel.FlashbackOutPoint);");
        AssertContains(mainWindowText, "InitializeFlashbackMarkerPresentationController();");
        AssertContains(controllerText, "internal sealed class FlashbackMarkerPresentationController");
        AssertContains(controllerText, "public static string FormatDuration(TimeSpan value)");
        AssertContains(controllerText, "public void UpdateMarkers(TimeSpan bufferDuration, TimeSpan? inPoint, TimeSpan? outPoint)");
        AssertContains(controllerText, "_context.InPointMarker.Visibility = Visibility.Visible;");
        AssertContains(controllerText, "_context.OutPointMarker.Visibility = Visibility.Visible;");
        AssertContains(controllerText, "_context.SelectionRegion.Visibility = Visibility.Visible;");
        AssertContains(controllerText, "Canvas.SetLeft(_context.SelectionRegion, selLeft);");
        AssertContains(flashbackText, "UpdateMarkers = UpdateFlashbackMarkers,");
        AssertContains(playbackCoordinatorText, "_context.UpdateMarkers();");
        AssertContains(propertyChangedText, "TryHandleFlashback = TryHandleFlashbackPropertyChanged");
        AssertContains(flashbackPropertyChangedText, "UpdateRangeMarkers = UpdateFlashbackMarkers,");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.FlashbackInPoint):");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.FlashbackOutPoint):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.UpdateRangeMarkers();");
        AssertDoesNotContain(flashbackText, "private static string FormatFlashbackDuration(TimeSpan ts)");
        AssertDoesNotContain(flashbackText, "Canvas.SetLeft(");
        AssertDoesNotContain(flashbackText, "FlashbackInPointMarker.Visibility = Visibility.Visible;");
        AssertDoesNotContain(flashbackText, "FlashbackSelectionRegion.Visibility = Visibility.Visible;");

        return Task.CompletedTask;
    }

    internal static Task FlashbackExportProgressPresentation_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var flashbackText = ReadMainWindowFlashbackAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");

        AssertContains(flashbackText, "private FlashbackExportProgressPresentationController _flashbackExportProgressPresentationController = null!;");
        AssertContains(flashbackText, "private void InitializeFlashbackExportProgressPresentationController()");
        AssertContains(flashbackText, "FlashbackExportProgressBar = FlashbackExportProgressBar,");
        AssertContains(flashbackText, "=> _flashbackExportProgressPresentationController.UpdateProgress(progress);");
        AssertContains(flashbackText, "=> _flashbackExportProgressPresentationController.UpdateExporting(isExporting);");
        AssertContains(mainWindowText, "InitializeFlashbackExportProgressPresentationController();");
        AssertContains(mainWindowText, "InitializeFlashbackPropertyChangedController();");
        AssertContains(propertyChangedText, "TryHandleFlashback = TryHandleFlashbackPropertyChanged");
        AssertContains(flashbackPropertyChangedText, "UpdateExportProgress = UpdateFlashbackExportProgress,");
        AssertContains(flashbackPropertyChangedText, "UpdateExportingPresentation = UpdateFlashbackExportingPresentation,");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.FlashbackExportProgress):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.UpdateExportProgress(_context.GetExportProgress());");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.IsFlashbackExporting):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.UpdateExportingPresentation(_context.IsExporting());");
        AssertContains(controllerText, "internal sealed class FlashbackExportProgressPresentationController");
        AssertContains(controllerText, "public void UpdateProgress(double progress)");
        AssertContains(controllerText, "_context.FlashbackExportProgressBar.Value = progress;");
        AssertContains(controllerText, "public void UpdateExporting(bool isExporting)");
        AssertContains(controllerText, "_context.FlashbackExportProgressBar.Visibility = isExporting");
        AssertContains(controllerText, "? Visibility.Visible");
        AssertContains(controllerText, ": Visibility.Collapsed;");
        AssertContains(controllerText, "if (!isExporting)");
        AssertContains(controllerText, "_context.FlashbackExportProgressBar.Value = 0;");
        AssertDoesNotContain(flashbackPropertyChangedText, "FlashbackExportProgressBar.Value = ViewModel.FlashbackExportProgress;");
        AssertDoesNotContain(flashbackPropertyChangedText, "FlashbackExportProgressBar.Visibility = ViewModel.IsFlashbackExporting");

        return Task.CompletedTask;
    }

    internal static Task MainWindowFlashbackScrub_EndsOnReleaseCancelAndCaptureLost()
    {
        var flashbackWindowText = ReadMainWindowFlashbackAdapterSource();
        var flashbackCommandControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs")
            .Replace("\r\n", "\n");
        var flashbackScrubText = ReadMainWindowFlashbackAdapterSource();
        var flashbackScrubControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs")
            .Replace("\r\n", "\n");
        var flashbackGeometryText = flashbackScrubControllerText;
        var flashbackPlayheadText = ReadMainWindowFlashbackAdapterSource();
        var flashbackPlayheadControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs")
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
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Flashback.Interactions.cs")),
            "Flashback scrub adapter folded into the MainWindow root composition adapter");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Flashback", "FlashbackScrubInteractionController.cs")),
            "Flashback scrub interaction folded into Flashback UI controllers");
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
        var flashbackTimelineControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs")
            .Replace("\r\n", "\n");
        var flashbackTimelineAnimationControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs")
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Flashback", "FlashbackTimelineController.cs")),
            "Flashback timeline folded into Flashback UI controllers");
        AssertDoesNotContain(mainWindowText, "private bool _suppressFlashbackEnabledToggle;");
        AssertDoesNotContain(flashbackWindowText, "ApplyFlashbackEnabledToggleAsync(requestedEnabled)");

        return Task.CompletedTask;
    }
}
