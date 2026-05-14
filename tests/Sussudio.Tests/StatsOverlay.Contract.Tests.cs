static partial class Program
{
    private static Task StatsPanels_UseSourceTelemetry_ForHdmiInput()
    {
        var statsOverlayText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs").Replace("\r\n", "\n");
        var statsPresentationText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.cs").Replace("\r\n", "\n");
        var mainWindowXaml = ReadRepoFile("Sussudio/MainWindow.xaml").Replace("\r\n", "\n");
        var statsWindowText = ReadRepoFile("Sussudio/StatsWindow.xaml.cs").Replace("\r\n", "\n");
        var statsWindowXaml = ReadRepoFile("Sussudio/StatsWindow.xaml").Replace("\r\n", "\n");
        var nativeXuText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs").Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.RollingPoll.cs").Replace("\r\n", "\n");

        AssertContains(statsPresentationText, "var sourceHdr = FormatSourceHdr(snapshot.SourceIsHdr, snapshot.SourceColorimetry);");
        AssertContains(statsPresentationText, "var sourceFormat = snapshot.SourceVideoFormat ?? \"\\u2014\";");
        AssertDoesNotContain(statsPresentationText, "var sourceFormat =\n            snapshot.ReaderSourceSubtype ??");
        AssertContains(statsOverlayText, "StatsPresentationBuilder.BuildDockPresentation(snapshot)");
        AssertContains(statsWindowText, "SourceHdrValue.Text = FormatSourceHdr(snapshot.SourceIsHdr, snapshot.SourceColorimetry);");
        AssertContains(statsWindowText, "SourceFormatValue.Text = snapshot.SourceVideoFormat ?? \"\\u2014\";");
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
