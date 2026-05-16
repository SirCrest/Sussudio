static partial class Program
{
    private static Task StatsOverlayLifecycle_LivesInController()
    {
        var statsOverlayText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs").Replace("\r\n", "\n");
        var statsOverlayCompositionText = ReadRepoFile("Sussudio/MainWindow.StatsOverlayComposition.cs").Replace("\r\n", "\n");
        var frameTimeOverlayText = ReadRepoFile("Sussudio/MainWindow.FrameTimeOverlay.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/StatsOverlayController.cs").Replace("\r\n", "\n");
        var dockAnimationText = ReadRepoFile("Sussudio/Controllers/StatsOverlayController.DockAnimation.cs").Replace("\r\n", "\n");
        var frameTimeControllerText = ReadRepoFile("Sussudio/Controllers/FrameTimeOverlayPresentationController.cs").Replace("\r\n", "\n");

        AssertContains(statsOverlayCompositionText, "private StatsOverlayController _statsOverlayController = null!;");
        AssertContains(statsOverlayCompositionText, "private void InitializeStatsOverlayController()");
        AssertContains(statsOverlayCompositionText, "InitializeFrameTimeOverlayPresentationController();");
        AssertContains(statsOverlayCompositionText, "StatsToggle = StatsToggle,");
        AssertContains(statsOverlayCompositionText, "IsWindowClosing = () => _isWindowClosing,");
        AssertContains(statsOverlayCompositionText, "SetStatsVisible = visible => ViewModel.IsStatsVisible = visible,");
        AssertContains(statsOverlayCompositionText, "UpdateStatsDock = _statsDockRefreshController.RefreshDock,");
        AssertContains(statsOverlayText, "=> _statsOverlayController.HandleStatsToggleChecked();");
        AssertContains(statsOverlayText, "=> _statsOverlayController.HandleStatsToggleUnchecked();");
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
        AssertContains(controllerText, "STATS_POLL_TIMER_FAIL");
        AssertContains(dockAnimationText, "internal sealed partial class StatsOverlayController");
        AssertContains(dockAnimationText, "private Storyboard? _statsDockStoryboard;");
        AssertContains(dockAnimationText, "public void ShowDockPanel()");
        AssertContains(dockAnimationText, "public void HideDockPanel(bool immediate = false)");
        AssertContains(dockAnimationText, "private Storyboard CreateStatsDockStoryboard(bool showing)");
        AssertContains(dockAnimationText, "EnableDependentAnimation = true");
        AssertContains(frameTimeControllerText, "internal sealed class FrameTimeOverlayPresentationController");
        AssertContains(frameTimeControllerText, "public void Apply(StatsSnapshot snapshot)");
        AssertDoesNotContain(statsOverlayText, "private FrameTimeOverlayPresentationController _frameTimeOverlayPresentationController");
        AssertDoesNotContain(statsOverlayText, "new FrameTimeOverlayPresentationController(new FrameTimeOverlayPresentationControllerContext");
        AssertDoesNotContain(statsOverlayText, "private void StatsPollTimer_Tick(");
        AssertDoesNotContain(statsOverlayText, "private Storyboard CreateStatsDockStoryboard(");
        AssertDoesNotContain(statsOverlayText, "ViewModel.IsStatsVisible = true;");
        AssertDoesNotContain(statsOverlayText, "ViewModel.IsStatsVisible = false;");
        AssertDoesNotContain(statsOverlayText, "if (_isWindowClosing)");
        AssertDoesNotContain(controllerText, "private Storyboard? _statsDockStoryboard;");
        AssertDoesNotContain(controllerText, "private Storyboard CreateStatsDockStoryboard(");
        AssertDoesNotContain(statsOverlayText, "line.Points.Clear();");
        AssertDoesNotContain(frameTimeOverlayText, "line.Points.Clear();");

        return Task.CompletedTask;
    }

    private static Task StatsDockPresentationApplication_LivesInController()
    {
        var statsOverlayText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs").Replace("\r\n", "\n");
        var statsOverlayCompositionText = ReadRepoFile("Sussudio/MainWindow.StatsOverlayComposition.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/StatsDockPresentationController.cs").Replace("\r\n", "\n");
        var refreshControllerText = ReadRepoFile("Sussudio/Controllers/StatsDockRefreshController.cs").Replace("\r\n", "\n");

        AssertContains(statsOverlayCompositionText, "private StatsDockRefreshController _statsDockRefreshController = null!;");
        AssertContains(statsOverlayCompositionText, "var statsDockPresentationController = new StatsDockPresentationController(new StatsDockPresentationControllerContext");
        AssertContains(statsOverlayCompositionText, "_statsDockRefreshController = new StatsDockRefreshController(new StatsDockRefreshControllerContext");
        AssertOccursBefore(statsOverlayCompositionText, "InitializeFrameTimeOverlayPresentationController();", "var statsDockPresentationController = new StatsDockPresentationController");
        AssertOccursBefore(statsOverlayCompositionText, "var statsDockPresentationController = new StatsDockPresentationController", "var statsDiagnosticRowsController = new StatsDiagnosticRowsController");
        AssertOccursBefore(statsOverlayCompositionText, "var statsDiagnosticRowsController = new StatsDiagnosticRowsController", "var statsHardwareRowsController = new StatsHardwareRowsController");
        AssertOccursBefore(statsOverlayCompositionText, "var statsHardwareRowsController = new StatsHardwareRowsController", "_statsDockRefreshController = new StatsDockRefreshController");
        AssertOccursBefore(statsOverlayCompositionText, "_statsDockRefreshController = new StatsDockRefreshController", "_statsOverlayController = new StatsOverlayController");
        AssertContains(refreshControllerText, "internal sealed class StatsDockRefreshControllerContext");
        AssertContains(refreshControllerText, "internal sealed class StatsDockRefreshController");
        AssertContains(refreshControllerText, "public required Func<bool> IsStatsDockVisible { get; init; }");
        AssertContains(refreshControllerText, "public required Func<bool> IsDiagnosticsSectionVisible { get; init; }");
        AssertContains(refreshControllerText, "public void RefreshDock()");
        AssertContains(refreshControllerText, "public void RefreshDiagnosticsSection()");
        AssertContains(refreshControllerText, "_context.IsWindowClosing() || !_context.IsStatsDockVisible()");
        AssertContains(refreshControllerText, "StatsPresentationBuilder.BuildDockPresentation(snapshot)");
        AssertContains(refreshControllerText, "_context.DockPresentationController.Apply(presentation);");
        AssertContains(refreshControllerText, "_context.HardwareRowsController.UpdateDecodeSection();");
        AssertContains(refreshControllerText, "_context.HardwareRowsController.UpdateGpuSection();");
        AssertContains(refreshControllerText, "StatsPresentationBuilder.BuildDiagnosticRows(telemetryDetails, diagnosticSummary)");
        AssertContains(refreshControllerText, "if (!_context.IsDiagnosticsSectionVisible())");
        AssertContains(controllerText, "internal sealed class StatsDockPresentationControllerContext");
        AssertContains(controllerText, "internal sealed class StatsDockPresentationController");
        AssertContains(controllerText, "public void Apply(StatsDockPresentation presentation)");
        AssertContains(controllerText, "SetTextIfChanged(_context.SessionStateValue, presentation.SessionState);");
        AssertContains(controllerText, "SetMetricBrush(_context.SummaryRendererFpsValue, presentation.SummaryRendererFpsStatus);");
        AssertContains(controllerText, "SetVisibilityIfChanged(_context.AvSyncEncoderRow, presentation.EncoderDriftVisible ? Visibility.Visible : Visibility.Collapsed);");
        AssertContains(controllerText, "SetVisibilityIfChanged(_context.EncoderSection, presentation.EncoderActive ? Visibility.Visible : Visibility.Collapsed);");
        AssertContains(controllerText, "MetricGoodBrush = new(Windows.UI.Color.FromArgb(0xFF, 0x70, 0xF0, 0x8B))");
        AssertDoesNotContain(statsOverlayText, "SetMetricBrush(");
        AssertDoesNotContain(statsOverlayText, "SetTextIfChanged(Stats_");
        AssertDoesNotContain(statsOverlayText, "private static readonly SolidColorBrush MetricNeutralBrush");
        AssertDoesNotContain(statsOverlayText, "StatsPresentationBuilder.BuildDockPresentation(snapshot)");
        AssertDoesNotContain(statsOverlayText, "StatsPresentationBuilder.BuildDiagnosticRows(telemetryDetails, diagnosticSummary)");
        AssertDoesNotContain(statsOverlayText, "private void UpdateStatsDock()");
        AssertDoesNotContain(statsOverlayText, "private void RefreshDiagnosticsSection()");
        AssertDoesNotContain(statsOverlayText, "private void UpdateDiagnosticsSection(");

        return Task.CompletedTask;
    }

    private static Task StatsSectionChrome_LivesInFocusedPartial()
    {
        var statsOverlayText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs").Replace("\r\n", "\n");
        var statsSectionsText = ReadRepoFile("Sussudio/MainWindow.StatsSections.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/StatsSectionChromeController.cs").Replace("\r\n", "\n");

        AssertContains(statsSectionsText, "private StatsSectionChromeController _statsSectionChromeController = null!;");
        AssertContains(statsSectionsText, "private void InitializeStatsSectionChromeController()");
        AssertContains(statsSectionsText, "private void StatsSectionHeader_Tapped(object sender, TappedRoutedEventArgs e)");
        AssertContains(statsSectionsText, "private void SetStatsSectionVisible(string section, bool visible)");
        AssertContains(statsSectionsText, "=> _statsSectionChromeController.ToggleFromHeader(sender);");
        AssertContains(statsSectionsText, "=> _statsSectionChromeController.SetVisible(section, visible);");
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
        AssertContains(statsSectionsText, "RefreshDiagnosticsSection = _statsDockRefreshController.RefreshDiagnosticsSection");
        AssertDoesNotContain(statsSectionsText, "StatsDockPanel.FindName(contentName)");
        AssertDoesNotContain(statsSectionsText, "rotate.Angle =");
        AssertDoesNotContain(statsSectionsText, "UpdateDiagnosticsSection(snapshot");
        AssertDoesNotContain(statsOverlayText, "private void StatsSectionHeader_Tapped(");
        AssertDoesNotContain(statsOverlayText, "private void SetStatsSectionVisible(string section, bool visible)");

        return Task.CompletedTask;
    }

    private static Task StatsDiagnosticRowPooling_LivesInController()
    {
        var statsOverlayText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs").Replace("\r\n", "\n");
        var statsOverlayCompositionText = ReadRepoFile("Sussudio/MainWindow.StatsOverlayComposition.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/StatsDiagnosticRowsController.cs").Replace("\r\n", "\n");
        var hardwareRowsControllerText = ReadRepoFile("Sussudio/Controllers/StatsHardwareRowsController.cs").Replace("\r\n", "\n");
        var hardwareRowsBuilderText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.HardwareRows.cs").Replace("\r\n", "\n");
        var statsPresentationModelsText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationModels.cs").Replace("\r\n", "\n");
        var refreshControllerText = ReadRepoFile("Sussudio/Controllers/StatsDockRefreshController.cs").Replace("\r\n", "\n");

        AssertContains(statsOverlayCompositionText, "var statsDiagnosticRowsController = new StatsDiagnosticRowsController");
        AssertContains(statsOverlayCompositionText, "var statsHardwareRowsController = new StatsHardwareRowsController(new StatsHardwareRowsControllerContext");
        AssertContains(statsOverlayCompositionText, "ResourceOwner = StatsDockPanel,");
        AssertContains(statsOverlayCompositionText, "DiagnosticsContent = Diagnostics_Content");
        AssertContains(statsOverlayCompositionText, "GetMjpegPipelineTimingDetails = ViewModel.GetMjpegPipelineTimingDetails,");
        AssertContains(statsOverlayCompositionText, "GetPendingPreviewFrameCount = () => _previewRendererHostController.PendingFrameCount,");
        AssertContains(statsOverlayCompositionText, "GetNvmlSnapshot = () => _nvmlMonitor?.GetLatestSnapshot()");
        AssertContains(refreshControllerText, "_context.DiagnosticRowsController.UpdateDiagnostics(presentation);");
        AssertContains(refreshControllerText, "_context.HardwareRowsController.UpdateDecodeSection();");
        AssertContains(refreshControllerText, "_context.HardwareRowsController.UpdateGpuSection();");
        AssertContains(hardwareRowsControllerText, "internal sealed class StatsHardwareRowsControllerContext");
        AssertContains(hardwareRowsControllerText, "internal sealed class StatsHardwareRowsController");
        AssertContains(hardwareRowsControllerText, "public void UpdateDecodeSection()");
        AssertContains(hardwareRowsControllerText, "public void UpdateGpuSection()");
        AssertContains(hardwareRowsControllerText, "StatsPresentationBuilder.BuildHardwareDecodeRows(");
        AssertContains(hardwareRowsControllerText, "CreateDecodeRowsInput(mjpeg, _context.GetPendingPreviewFrameCount())");
        AssertContains(hardwareRowsControllerText, "StatsPresentationBuilder.BuildHardwareGpuRows(CreateGpuRowsInput(_context.GetNvmlSnapshot()))");
        AssertContains(hardwareRowsControllerText, "private static StatsHardwareDecodeRowsInput CreateDecodeRowsInput(");
        AssertContains(hardwareRowsControllerText, "private static StatsHardwareGpuRowsInput? CreateGpuRowsInput(NvmlSnapshot? nvml)");
        AssertContains(hardwareRowsBuilderText, "public static IReadOnlyList<StatsHardwareRowPresentation> BuildHardwareDecodeRows(");
        AssertContains(hardwareRowsBuilderText, "StatsHardwareDecodeRowsInput mjpeg)");
        AssertContains(hardwareRowsBuilderText, "public static IReadOnlyList<StatsHardwareRowPresentation> BuildHardwareGpuRows(StatsHardwareGpuRowsInput? nvml)");
        AssertDoesNotContain(hardwareRowsBuilderText, "using Sussudio.Services.Gpu;");
        AssertContains(statsPresentationModelsText, "internal readonly record struct StatsHardwareRowPresentation(string Label, string Value);");
        AssertContains(statsPresentationModelsText, "internal readonly record struct StatsHardwareDecodeRowsInput(");
        AssertContains(statsPresentationModelsText, "internal readonly record struct StatsHardwareGpuRowsInput(");
        AssertContains(hardwareRowsControllerText, "_context.GetMjpegPipelineTimingDetails()");
        AssertContains(hardwareRowsControllerText, "_context.GetPendingPreviewFrameCount()");
        AssertContains(hardwareRowsControllerText, "_context.GetNvmlSnapshot()");
        AssertContains(hardwareRowsControllerText, "_context.DiagnosticRowsController.CollapseDecodeRows(_context.DecodeContent);");
        AssertContains(hardwareRowsControllerText, "_context.DiagnosticRowsController.UpdateDecodeRows(_context.DecodeContent, rows);");
        AssertContains(hardwareRowsControllerText, "_context.DiagnosticRowsController.UpdateGpuRows(_context.GpuContent, rows);");
        AssertContains(controllerText, "internal sealed class StatsDiagnosticRowsController");
        AssertContains(controllerText, "private const int MaxExpectedDecodeRowCount = 14;");
        AssertContains(controllerText, "private const int FixedGpuRowCount = 10;");
        AssertContains(controllerText, "private readonly List<DiagnosticRowSlot> _decodeRowPool = new();");
        AssertContains(controllerText, "private TextBlock? _diagnosticsEmptyStateTextBlock;");
        AssertContains(controllerText, "public void UpdateDecodeRows(StackPanel container, IReadOnlyList<StatsHardwareRowPresentation> rows)");
        AssertContains(controllerText, "public void UpdateGpuRows(StackPanel container, IReadOnlyList<StatsHardwareRowPresentation> rows)");
        AssertContains(controllerText, "public void UpdateDiagnostics(StatsDiagnosticRowsPresentation presentation)");
        AssertContains(controllerText, "private Border CreateDiagnosticRow(string label, string value, bool alt)");
        AssertDoesNotContain(hardwareRowsControllerText, "new List<StatsHardwareRowPresentation>");
        AssertDoesNotContain(hardwareRowsControllerText, "public static IReadOnlyList<StatsHardwareRowPresentation> BuildHardwareGpuRows(");
        AssertDoesNotContain(mainWindowText, "_decodeRowPool");
        AssertDoesNotContain(mainWindowText, "_diagnosticsRowPool");
        AssertDoesNotContain(statsOverlayText, "private sealed record DiagnosticRowSlot(");
        AssertDoesNotContain(statsOverlayText, "private void EnsureDiagnosticRowPool(");
        AssertDoesNotContain(statsOverlayText, "private Border CreateDiagnosticRow(");
        AssertDoesNotContain(statsOverlayText, "private void UpdateDecodeSection()");
        AssertDoesNotContain(statsOverlayText, "private void UpdateGpuSection()");
        AssertDoesNotContain(statsOverlayText, "_statsDiagnosticRowsController.UpdateDiagnostics(presentation);");
        AssertDoesNotContain(statsOverlayText, "new List<StatsHardwareRowPresentation>");

        return Task.CompletedTask;
    }
}
