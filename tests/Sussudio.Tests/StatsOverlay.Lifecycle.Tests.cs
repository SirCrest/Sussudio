static partial class Program
{
    private static Task StatsOverlayLifecycle_LivesInController()
    {
        var statsOverlayText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs").Replace("\r\n", "\n");
        var statsOverlayCompositionText = statsOverlayText;
        var frameTimeOverlayText = statsOverlayText;
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var shutdownCleanupText = ReadRepoFile("Sussudio/MainWindow.ShutdownCleanup.cs").Replace("\r\n", "\n");
        var shutdownCleanupControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowShutdownCleanupController.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayController.cs").Replace("\r\n", "\n");
        var dockAnimationText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayController.DockAnimation.cs").Replace("\r\n", "\n");
        var frameTimeControllerText = ReadRepoFile("Sussudio/Controllers/Stats/FrameTimeOverlayPresentationController.cs").Replace("\r\n", "\n");
        var frameTimeGeometryText = ReadRepoFile("Sussudio/Controllers/Stats/FrameTimeOverlayGeometry.cs").Replace("\r\n", "\n");

        AssertContains(statsOverlayCompositionText, "private StatsOverlayController _statsOverlayController = null!;");
        AssertContains(statsOverlayCompositionText, "private void InitializeStatsOverlayController()");
        AssertContains(statsOverlayCompositionText, "InitializeFrameTimeOverlayPresentationController();");
        AssertContains(statsOverlayCompositionText, "StatsToggle = StatsToggle,");
        AssertContains(statsOverlayCompositionText, "IsWindowClosing = () => _isWindowClosing,");
        AssertContains(statsOverlayCompositionText, "SetStatsVisible = visible => ViewModel.IsStatsVisible = visible,");
        AssertContains(statsOverlayCompositionText, "UpdateStatsDock = _statsDockRefreshController.RefreshDock,");
        AssertContains(bindingsText, "AttachStatsOverlayToggleBindings();");
        AssertContains(statsOverlayText, "private void AttachStatsOverlayToggleBindings()");
        AssertContains(statsOverlayText, "=> _statsOverlayController.AttachToggleBindings();");
        AssertContains(statsOverlayText, "private void DetachStatsOverlayToggleBindings()");
        AssertContains(statsOverlayText, "=> _statsOverlayController.DetachToggleBindings();");
        AssertContains(shutdownCleanupText, "DetachStatsOverlayToggleBindings();");
        AssertOccursBefore(shutdownCleanupText, "DetachStatsOverlayToggleBindings();", "StopStatsDockPolling();");
        AssertContains(shutdownCleanupControllerText, "_context.StopStatsOverlay();");
        AssertContains(statsOverlayText, "=> _statsOverlayController.SyncStatsVisibility(visible, immediate);");
        AssertContains(statsOverlayText, "=> _statsOverlayController.SetFrameTimeOverlayVisible(visible);");
        AssertContains(frameTimeOverlayText, "private FrameTimeOverlayPresentationController _frameTimeOverlayPresentationController = null!;");
        AssertContains(frameTimeOverlayText, "private void InitializeFrameTimeOverlayPresentationController()");
        AssertContains(frameTimeOverlayText, "_frameTimeOverlayPresentationController = new FrameTimeOverlayPresentationController(new FrameTimeOverlayPresentationControllerContext");
        AssertContains(frameTimeOverlayText, "ExpectedLine = FrameTime_ExpectedLine");
        AssertContains(frameTimeOverlayText, "_frameTimeOverlayPresentationController.Apply(snapshot);");
        AssertContains(mainWindowText, "InitializeStatsOverlayController();");
        AssertOccursBefore(mainWindowText, "InitializeStatsOverlayController();", "InitializeStatsSectionChromeController();");
        AssertDoesNotContain(mainWindowText, "private DispatcherQueueTimer? _statsPollTimer;");
        AssertDoesNotContain(mainWindowText, "private Storyboard? _statsDockStoryboard;");
        AssertContains(controllerText, "internal sealed partial class StatsOverlayController");
        AssertContains(controllerText, "private DispatcherQueueTimer? _statsPollTimer;");
        AssertContains(controllerText, "public required ToggleButton StatsToggle { get; init; }");
        AssertContains(controllerText, "public required Func<bool> IsWindowClosing { get; init; }");
        AssertContains(controllerText, "public required Action<bool> SetStatsVisible { get; init; }");
        AssertContains(controllerText, "private bool _toggleBindingsAttached;");
        AssertContains(controllerText, "public void AttachToggleBindings()");
        AssertContains(controllerText, "if (_toggleBindingsAttached)");
        AssertContains(controllerText, "_context.StatsToggle.Checked += StatsToggle_Checked;");
        AssertContains(controllerText, "_context.StatsToggle.Unchecked += StatsToggle_Unchecked;");
        AssertContains(controllerText, "_context.FrameTimeOverlayToggle.Checked += FrameTimeOverlayToggle_Checked;");
        AssertContains(controllerText, "_context.FrameTimeOverlayToggle.Unchecked += FrameTimeOverlayToggle_Unchecked;");
        AssertContains(controllerText, "_toggleBindingsAttached = true;");
        AssertContains(controllerText, "public void DetachToggleBindings()");
        AssertContains(controllerText, "if (!_toggleBindingsAttached)");
        AssertContains(controllerText, "_context.StatsToggle.Checked -= StatsToggle_Checked;");
        AssertContains(controllerText, "_context.StatsToggle.Unchecked -= StatsToggle_Unchecked;");
        AssertContains(controllerText, "_context.FrameTimeOverlayToggle.Checked -= FrameTimeOverlayToggle_Checked;");
        AssertContains(controllerText, "_context.FrameTimeOverlayToggle.Unchecked -= FrameTimeOverlayToggle_Unchecked;");
        AssertContains(controllerText, "_toggleBindingsAttached = false;");
        AssertContains(controllerText, "public void HandleStatsToggleChecked()");
        AssertContains(controllerText, "if (_context.IsWindowClosing())");
        AssertContains(controllerText, "_context.SetStatsVisible(true);");
        AssertContains(controllerText, "public void HandleStatsToggleUnchecked()");
        AssertContains(controllerText, "=> _context.SetStatsVisible(false);");
        AssertContains(controllerText, "public void SyncStatsVisibility(bool visible, bool immediate = false)");
        AssertContains(controllerText, "if (_context.StatsToggle.IsChecked != visible)");
        AssertContains(controllerText, "_context.StatsToggle.IsChecked = visible;");
        AssertContains(controllerText, "ApplyStatsVisibility(visible, immediate);");
        AssertContains(controllerText, "public void ApplyStatsVisibility(bool visible, bool immediate = false)");
        AssertContains(controllerText, "public void SetFrameTimeOverlayVisible(bool visible)");
        AssertContains(controllerText, "private void StatsToggle_Checked(object sender, RoutedEventArgs e)");
        AssertContains(controllerText, "private void StatsToggle_Unchecked(object sender, RoutedEventArgs e)");
        AssertContains(controllerText, "private void FrameTimeOverlayToggle_Checked(object sender, RoutedEventArgs e)");
        AssertContains(controllerText, "private void FrameTimeOverlayToggle_Unchecked(object sender, RoutedEventArgs e)");
        AssertContains(controllerText, "STATS_POLL_TIMER_FAIL");
        AssertContains(dockAnimationText, "internal sealed partial class StatsOverlayController");
        AssertContains(dockAnimationText, "private Storyboard? _statsDockStoryboard;");
        AssertContains(dockAnimationText, "public void ShowDockPanel()");
        AssertContains(dockAnimationText, "public void HideDockPanel(bool immediate = false)");
        AssertContains(dockAnimationText, "private Storyboard CreateStatsDockStoryboard(bool showing)");
        AssertContains(dockAnimationText, "EnableDependentAnimation = true");
        AssertContains(frameTimeControllerText, "internal sealed class FrameTimeOverlayPresentationController");
        AssertContains(frameTimeControllerText, "public void Apply(StatsSnapshot snapshot)");
        AssertContains(frameTimeGeometryText, "internal static class FrameTimeOverlayGeometry");
        AssertContains(frameTimeGeometryText, "FallbackWidth = 500");
        AssertContains(frameTimeGeometryText, "FallbackHeight = 92");
        AssertDoesNotContain(statsOverlayText, "private void StatsPollTimer_Tick(");
        AssertDoesNotContain(statsOverlayText, "private Storyboard CreateStatsDockStoryboard(");
        AssertDoesNotContain(statsOverlayText, "ViewModel.IsStatsVisible = true;");
        AssertDoesNotContain(statsOverlayText, "ViewModel.IsStatsVisible = false;");
        AssertDoesNotContain(statsOverlayText, "if (_isWindowClosing)");
        AssertDoesNotContain(statsOverlayText, "private void StatsToggle_Checked(");
        AssertDoesNotContain(statsOverlayText, "private void StatsToggle_Unchecked(");
        AssertDoesNotContain(statsOverlayText, "private void FrameTimeOverlayToggle_Checked(");
        AssertDoesNotContain(statsOverlayText, "private void FrameTimeOverlayToggle_Unchecked(");
        AssertDoesNotContain(bindingsText, "StatsToggle.Checked +=");
        AssertDoesNotContain(bindingsText, "StatsToggle.Unchecked +=");
        AssertDoesNotContain(bindingsText, "FrameTimeOverlayToggle.Checked +=");
        AssertDoesNotContain(bindingsText, "FrameTimeOverlayToggle.Unchecked +=");
        AssertDoesNotContain(controllerText, "private Storyboard? _statsDockStoryboard;");
        AssertDoesNotContain(controllerText, "private Storyboard CreateStatsDockStoryboard(");
        AssertDoesNotContain(statsOverlayText, "line.Points.Clear();");
        AssertDoesNotContain(frameTimeOverlayText, "line.Points.Clear();");

        return Task.CompletedTask;
    }

    private static Task StatsSectionChrome_LivesInFocusedPartial()
    {
        var statsOverlayText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Stats/StatsSectionChromeController.cs").Replace("\r\n", "\n");

        AssertContains(statsOverlayText, "private StatsSectionChromeController _statsSectionChromeController = null!;");
        AssertContains(statsOverlayText, "private void InitializeStatsSectionChromeController()");
        AssertContains(statsOverlayText, "private void StatsSectionHeader_Tapped(object sender, TappedRoutedEventArgs e)");
        AssertContains(statsOverlayText, "private void SetStatsSectionVisible(string section, bool visible)");
        AssertContains(statsOverlayText, "=> _statsSectionChromeController.ToggleFromHeader(sender);");
        AssertContains(statsOverlayText, "=> _statsSectionChromeController.SetVisible(section, visible);");
        AssertContains(controllerText, "internal sealed class StatsSectionChromeControllerContext");
        AssertContains(controllerText, "internal sealed class StatsSectionChromeController");
        AssertContains(controllerText, "public void ToggleFromHeader(object sender)");
        AssertContains(controllerText, "public void SetVisible(string section, bool visible)");
        AssertContains(controllerText, "_context.StatsDockPanel.FindName(contentName) as StackPanel");
        AssertContains(controllerText, "content.Visibility = collapsing ? Visibility.Collapsed : Visibility.Visible;");
        AssertContains(controllerText, "content.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;");
        AssertContains(controllerText, "rotate.Angle = expanded ? 0 : -90;");
        AssertContains(controllerText, "_context.RefreshDiagnosticsSection();");
        AssertContains(mainWindowText, "ViewModel.StatsSectionVisibilityHandler = SetStatsSectionVisible;");
        AssertContains(mainWindowText, "InitializeStatsSectionChromeController();");
        AssertContains(statsOverlayText, "RefreshDiagnosticsSection = _statsDockRefreshController.RefreshDiagnosticsSection");
        AssertDoesNotContain(statsOverlayText, "StatsDockPanel.FindName(contentName)");
        AssertDoesNotContain(statsOverlayText, "rotate.Angle =");
        AssertDoesNotContain(statsOverlayText, "UpdateDiagnosticsSection(snapshot");

        return Task.CompletedTask;
    }
}
