static partial class Program
{
    private static Task StatsPanels_UseSourceTelemetry_ForHdmiInput()
    {
        var statsOverlayText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs").Replace("\r\n", "\n");
        var statsPresentationText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.cs").Replace("\r\n", "\n");
        var statsPresentationWindowText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.Window.cs").Replace("\r\n", "\n");
        var mainWindowXaml = ReadRepoFile("Sussudio/MainWindow.xaml").Replace("\r\n", "\n");
        var statsWindowText = ReadRepoFile("Sussudio/StatsWindow.xaml.cs").Replace("\r\n", "\n");
        var statsWindowXaml = ReadRepoFile("Sussudio/StatsWindow.xaml").Replace("\r\n", "\n");
        var nativeXuText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs").Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.RollingPoll.cs").Replace("\r\n", "\n");

        AssertContains(statsPresentationText, "var sourceHdr = FormatSourceHdr(snapshot.SourceIsHdr, snapshot.SourceColorimetry);");
        AssertContains(statsPresentationText, "var sourceFormat = snapshot.SourceVideoFormat ?? \"\\u2014\";");
        AssertDoesNotContain(statsPresentationText, "var sourceFormat =\n            snapshot.ReaderSourceSubtype ??");
        AssertContains(statsOverlayText, "StatsPresentationBuilder.BuildDockPresentation(snapshot)");
        AssertContains(statsWindowText, "StatsPresentationBuilder.BuildStatsWindowPresentation(snapshot)");
        AssertContains(statsPresentationWindowText, "SourceHdr: FormatSourceHdr(snapshot.SourceIsHdr, snapshot.SourceColorimetry),");
        AssertContains(statsPresentationWindowText, "SourceFormat: snapshot.SourceVideoFormat ?? \"\\u2014\",");
        AssertContains(mainWindowXaml, "Text=\"Video Format\"");
        AssertContains(mainWindowXaml, "Text=\"Telemetry Details\"");
        AssertContains(statsWindowXaml, "Text=\"Video Format\"");
        AssertContains(statsWindowXaml, "Text=\"Telemetry Details\"");
        AssertContains(nativeXuText, "VideoFormat = aviInfo.ColorSpace,");
        AssertContains(nativeXuText, "Colorimetry = aviInfo.Colorimetry,");
        AssertContains(nativeXuText, "Quantization = aviInfo.Quantization,");
        AssertContains(nativeXuText, "HdrTransferFunction = ResolveHdrTransferFunction(hdrInfo.Eotf),");

        return Task.CompletedTask;
    }

    private static Task StatsOverlayLifecycle_LivesInController()
    {
        var statsOverlayText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/StatsOverlayController.cs").Replace("\r\n", "\n");

        AssertContains(statsOverlayText, "private StatsOverlayController _statsOverlayController = null!;");
        AssertContains(statsOverlayText, "private void InitializeStatsOverlayController()");
        AssertContains(statsOverlayText, "=> _statsOverlayController.ApplyStatsVisibility(visible, immediate);");
        AssertContains(statsOverlayText, "=> _statsOverlayController.SetFrameTimeOverlayVisible(visible);");
        AssertContains(mainWindowText, "InitializeStatsOverlayController();");
        AssertDoesNotContain(mainWindowText, "private DispatcherQueueTimer? _statsPollTimer;");
        AssertDoesNotContain(mainWindowText, "private Storyboard? _statsDockStoryboard;");
        AssertContains(controllerText, "internal sealed class StatsOverlayController");
        AssertContains(controllerText, "private DispatcherQueueTimer? _statsPollTimer;");
        AssertContains(controllerText, "private Storyboard? _statsDockStoryboard;");
        AssertContains(controllerText, "public void ApplyStatsVisibility(bool visible, bool immediate = false)");
        AssertContains(controllerText, "public void SetFrameTimeOverlayVisible(bool visible)");
        AssertContains(controllerText, "private Storyboard CreateStatsDockStoryboard(bool showing)");
        AssertContains(controllerText, "STATS_POLL_TIMER_FAIL");
        AssertDoesNotContain(statsOverlayText, "private void StatsPollTimer_Tick(");
        AssertDoesNotContain(statsOverlayText, "private Storyboard CreateStatsDockStoryboard(");

        return Task.CompletedTask;
    }

    private static Task StatsDockPresentationApplication_LivesInController()
    {
        var statsOverlayText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/StatsDockPresentationController.cs").Replace("\r\n", "\n");

        AssertContains(statsOverlayText, "private StatsDockPresentationController _statsDockPresentationController = null!;");
        AssertContains(statsOverlayText, "_statsDockPresentationController = new StatsDockPresentationController(new StatsDockPresentationControllerContext");
        AssertContains(statsOverlayText, "_statsDockPresentationController.Apply(presentation);");
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
        AssertContains(statsOverlayText, "private void RefreshDiagnosticsSection()");
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
        var hardwareSectionsText = ReadRepoFile("Sussudio/MainWindow.StatsHardwareSections.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/StatsDiagnosticRowsController.cs").Replace("\r\n", "\n");

        AssertContains(statsOverlayText, "private StatsDiagnosticRowsController _statsDiagnosticRowsController = null!;");
        AssertContains(statsOverlayText, "_statsDiagnosticRowsController = new StatsDiagnosticRowsController");
        AssertContains(statsOverlayText, "ResourceOwner = StatsDockPanel,");
        AssertContains(statsOverlayText, "DiagnosticsContent = Diagnostics_Content");
        AssertContains(hardwareSectionsText, "private void UpdateDecodeSection()");
        AssertContains(hardwareSectionsText, "private void UpdateGpuSection()");
        AssertContains(hardwareSectionsText, "_statsDiagnosticRowsController.CollapseDecodeRows(Decode_Content);");
        AssertContains(hardwareSectionsText, "_statsDiagnosticRowsController.UpdateDecodeRows(Decode_Content, rows);");
        AssertContains(hardwareSectionsText, "_statsDiagnosticRowsController.UpdateGpuRows(GPU_Content, rows);");
        AssertContains(statsOverlayText, "_statsDiagnosticRowsController.UpdateDiagnostics(presentation);");
        AssertContains(controllerText, "internal sealed class StatsDiagnosticRowsController");
        AssertContains(controllerText, "private const int MaxExpectedDecodeRowCount = 14;");
        AssertContains(controllerText, "private const int FixedGpuRowCount = 10;");
        AssertContains(controllerText, "private readonly List<DiagnosticRowSlot> _decodeRowPool = new();");
        AssertContains(controllerText, "private TextBlock? _diagnosticsEmptyStateTextBlock;");
        AssertContains(controllerText, "public void UpdateDiagnostics(StatsDiagnosticRowsPresentation presentation)");
        AssertContains(controllerText, "private Border CreateDiagnosticRow(string label, string value, bool alt)");
        AssertDoesNotContain(mainWindowText, "_decodeRowPool");
        AssertDoesNotContain(mainWindowText, "_diagnosticsRowPool");
        AssertDoesNotContain(statsOverlayText, "private sealed record DiagnosticRowSlot(");
        AssertDoesNotContain(statsOverlayText, "private void EnsureDiagnosticRowPool(");
        AssertDoesNotContain(statsOverlayText, "private Border CreateDiagnosticRow(");
        AssertDoesNotContain(statsOverlayText, "private void UpdateDecodeSection()");
        AssertDoesNotContain(statsOverlayText, "private void UpdateGpuSection()");

        return Task.CompletedTask;
    }
}
