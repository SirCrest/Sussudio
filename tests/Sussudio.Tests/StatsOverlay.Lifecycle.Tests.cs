using Xunit;

namespace Sussudio.Tests;

public class StatsOverlayLifecycleTests
{
    [Fact]
    public void StatsOverlayLifecycle_LivesInController()
    {
        var statsOverlayText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs");
        var statsOverlayCompositionText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs");
        var frameTimeOverlayText = statsOverlayCompositionText;
        var statsDockGraphText = ReadRepoFile("Sussudio/Controllers/Stats/StatsDockControllerGraph.cs");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs");
        var shutdownCleanupText = ReadRepoFile("Sussudio/MainWindow.ShutdownCleanup.cs");
        var shutdownCleanupControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowShutdownCleanupController.cs");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayController.cs");
        var dockAnimationText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayController.DockAnimation.cs");
        var frameTimeControllerText = ReadRepoFile("Sussudio/Controllers/Stats/FrameTimeOverlayPresentationController.cs");
        var frameTimeGeometryText = ReadRepoFile("Sussudio/Controllers/Stats/FrameTimeOverlayGeometry.cs");

        AssertContains(statsOverlayText, "private StatsOverlayCompositionController _statsOverlayCompositionController = null!;");
        AssertContains(statsOverlayText, "private void InitializeStatsOverlayCompositionController()");
        AssertContains(statsOverlayCompositionText, "internal sealed class StatsOverlayCompositionController");
        AssertContains(statsOverlayCompositionText, "private readonly StatsOverlayController _statsOverlayController;");
        AssertContains(statsOverlayCompositionText, "private readonly StatsDockControllerGraph _statsDockControllerGraph;");
        AssertContains(statsOverlayCompositionText, "private readonly StatsSnapshotProvider _statsSnapshotProvider;");
        AssertContains(statsOverlayCompositionText, "private readonly FrameTimeOverlayPresentationController _frameTimeOverlayPresentationController;");
        AssertContains(statsOverlayCompositionText, "private readonly StatsSectionChromeController _statsSectionChromeController;");
        AssertContains(statsOverlayCompositionText, "public required StatsOverlayShellContext Shell { get; init; }");
        AssertContains(statsOverlayCompositionText, "public required StatsOverlaySnapshotSourceContext SnapshotSources { get; init; }");
        AssertContains(statsOverlayCompositionText, "public required StatsOverlayDockTargetsContext DockTargets { get; init; }");
        AssertContains(statsOverlayCompositionText, "public required StatsOverlayHardwareSourceContext HardwareSources { get; init; }");
        AssertContains(statsOverlayCompositionText, "public required StatsOverlayFrameTimeTargetsContext FrameTimeTargets { get; init; }");
        AssertContains(statsOverlayCompositionText, "_statsSnapshotProvider = CreateSnapshotProvider(context);");
        AssertContains(statsOverlayCompositionText, "_frameTimeOverlayPresentationController = CreateFrameTimeOverlayPresentationController(context);");
        AssertContains(statsOverlayCompositionText, "_statsDockControllerGraph = CreateDockControllerGraph(context);");
        AssertContains(statsOverlayCompositionText, "_statsOverlayController = CreateOverlayController(context);");
        AssertContains(statsOverlayCompositionText, "_statsSectionChromeController = CreateSectionChromeController(context);");
        AssertContains(statsOverlayText, "Shell = new StatsOverlayShellContext");
        AssertContains(statsOverlayText, "SnapshotSources = new StatsOverlaySnapshotSourceContext");
        AssertContains(statsOverlayText, "DockTargets = new StatsOverlayDockTargetsContext");
        AssertContains(statsOverlayText, "HardwareSources = new StatsOverlayHardwareSourceContext");
        AssertContains(statsOverlayText, "FrameTimeTargets = new StatsOverlayFrameTimeTargetsContext");
        AssertContains(statsOverlayText, "StatsToggle = StatsToggle,");
        AssertContains(statsOverlayText, "IsWindowClosing = () => _isWindowClosing,");
        AssertContains(statsOverlayText, "SetStatsVisible = visible => ViewModel.IsStatsVisible = visible,");
        AssertContains(statsOverlayCompositionText, "StatsToggle = context.Shell.StatsToggle,");
        AssertContains(statsOverlayCompositionText, "GetCaptureHealthSnapshot = context.SnapshotSources.GetCaptureHealthSnapshot,");
        AssertContains(statsOverlayCompositionText, "DiagnosticsContent = context.DockTargets.DiagnosticsContent,");
        AssertContains(statsOverlayCompositionText, "GetMjpegPipelineTimingDetails = context.HardwareSources.GetMjpegPipelineTimingDetails,");
        AssertContains(statsOverlayCompositionText, "UpdateStatsDock = _statsDockControllerGraph.RefreshDock,");
        AssertContains(statsOverlayCompositionText, "UpdateFrameTimeOverlay = UpdateFrameTimeOverlay,");
        AssertContains(statsDockGraphText, "internal sealed class StatsDockControllerGraph");
        AssertContains(statsDockGraphText, "public void RefreshDock()");
        AssertContains(bindingsText, "AttachStatsOverlayToggleBindings();");
        AssertContains(bindingsText, "ApplyStatsVisibility(ViewModel.IsStatsVisible, immediate: true);");
        AssertDoesNotContain(bindingsText, "_statsOverlayController.SyncStatsVisibility(ViewModel.IsStatsVisible");
        AssertContains(statsOverlayText, "private void AttachStatsOverlayToggleBindings()");
        AssertContains(statsOverlayText, "=> _statsOverlayCompositionController.AttachToggleBindings();");
        AssertContains(statsOverlayText, "private void DetachStatsOverlayToggleBindings()");
        AssertContains(statsOverlayText, "=> _statsOverlayCompositionController.DetachToggleBindings();");
        AssertContains(shutdownCleanupText, "DetachStatsOverlayToggleBindings();");
        AssertOccursBefore(shutdownCleanupText, "DetachStatsOverlayToggleBindings();", "StopStatsDockPolling();");
        AssertContains(shutdownCleanupControllerText, "_context.StopStatsOverlay();");
        AssertContains(statsOverlayText, "=> _statsOverlayCompositionController.ApplyStatsVisibility(visible, immediate);");
        AssertContains(statsOverlayCompositionText, "public bool TryHandlePropertyChanged(string propertyName, bool isStatsVisible)");
        AssertContains(statsOverlayCompositionText, "case nameof(MainViewModel.IsStatsVisible):");
        AssertContains(statsOverlayCompositionText, "ApplyStatsVisibility(isStatsVisible);");
        AssertContains(statsOverlayText, "=> _statsOverlayCompositionController.SetFrameTimeOverlayVisible(visible);");
        AssertContains(frameTimeOverlayText, "private readonly FrameTimeOverlayPresentationController _frameTimeOverlayPresentationController;");
        AssertContains(frameTimeOverlayText, "private static FrameTimeOverlayPresentationController CreateFrameTimeOverlayPresentationController(");
        AssertContains(frameTimeOverlayText, "return new FrameTimeOverlayPresentationController(new FrameTimeOverlayPresentationControllerContext");
        AssertContains(frameTimeOverlayText, "ExpectedLine = context.FrameTimeTargets.FrameTimeExpectedLine");
        AssertContains(frameTimeOverlayText, "_frameTimeOverlayPresentationController.Apply(snapshot);");
        AssertContains(mainWindowText, "InitializeStatsOverlayCompositionController();");
        AssertDoesNotContain(mainWindowText, "InitializeStatsOverlayController();");
        AssertDoesNotContain(mainWindowText, "InitializeStatsSectionChromeController();");
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
        AssertDoesNotContain(statsOverlayCompositionText, "line.Points.Clear();");
        AssertDoesNotContain(statsOverlayText, "new StatsOverlayControllerContext");
        AssertDoesNotContain(statsOverlayText, "new StatsDockControllerGraphContext");
        AssertDoesNotContain(statsOverlayText, "new StatsSnapshotProviderContext");
    }

    [Fact]
    public void StatsSectionChrome_LivesInFocusedPartial()
    {
        var statsOverlayText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs");
        var statsOverlayCompositionText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Stats/StatsSectionChromeController.cs");

        AssertContains(statsOverlayCompositionText, "private readonly StatsSectionChromeController _statsSectionChromeController;");
        AssertContains(statsOverlayCompositionText, "private StatsSectionChromeController CreateSectionChromeController(");
        AssertContains(statsOverlayText, "private void StatsSectionHeader_Tapped(object sender, TappedRoutedEventArgs e)");
        AssertContains(statsOverlayText, "private void SetStatsSectionVisible(string section, bool visible)");
        AssertContains(statsOverlayText, "=> _statsOverlayCompositionController.ToggleSectionFromHeader(sender);");
        AssertContains(statsOverlayText, "=> _statsOverlayCompositionController.SetSectionVisible(section, visible);");
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
        AssertContains(mainWindowText, "InitializeStatsOverlayCompositionController();");
        AssertContains(statsOverlayCompositionText, "RefreshDiagnosticsSection = _statsDockControllerGraph.RefreshDiagnosticsSection");
        AssertDoesNotContain(statsOverlayText, "StatsDockPanel.FindName(contentName)");
        AssertDoesNotContain(statsOverlayText, "rotate.Angle =");
        AssertDoesNotContain(statsOverlayText, "UpdateDiagnosticsSection(snapshot");
    }

    private static string ReadRepoFile(string relativePath)
        => RuntimeContractSource.ReadRepoFile(relativePath).Replace("\r\n", "\n");

    private static void AssertContains(string actual, string expected)
        => Assert.True(actual.Contains(expected, StringComparison.Ordinal), $"Expected to find: {expected}");

    private static void AssertDoesNotContain(string actual, string unexpected)
        => Assert.False(actual.Contains(unexpected, StringComparison.Ordinal), $"Did not expect to find: {unexpected}");

    private static void AssertOccursBefore(string actual, string before, string after)
    {
        var beforeIndex = actual.IndexOf(before, StringComparison.Ordinal);
        var afterIndex = actual.IndexOf(after, StringComparison.Ordinal);
        Assert.True(beforeIndex >= 0, $"Expected to find: {before}");
        Assert.True(afterIndex >= 0, $"Expected to find: {after}");
        Assert.True(beforeIndex < afterIndex, $"Expected '{before}' to appear before '{after}'.");
    }
}
