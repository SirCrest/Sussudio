using Xunit;

namespace Sussudio.Tests;

public partial class StatsPresentationTests
{
    [Fact]
    public void StatsPanels_UseSourceTelemetry_ForHdmiInput()
    {
        var statsOverlayText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs").Replace("\r\n", "\n");
        var statsDockRefreshControllerText = ReadRepoFile("Sussudio/Controllers/Stats/StatsDockRefreshController.cs").Replace("\r\n", "\n");
        var statsPresentationText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.cs").Replace("\r\n", "\n");
        var statsPresentationDockText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.Dock.cs").Replace("\r\n", "\n");
        var statsPresentationWindowText = ReadRepoFile("Sussudio/ViewModels/StatsPresentationBuilder.Window.cs").Replace("\r\n", "\n");
        var mainWindowXaml = ReadRepoFile("Sussudio/MainWindow.xaml").Replace("\r\n", "\n");
        var statsWindowText = ReadRepoFile("Sussudio/StatsWindow.xaml.cs").Replace("\r\n", "\n");
        var statsWindowXaml = ReadRepoFile("Sussudio/StatsWindow.xaml").Replace("\r\n", "\n");
        var nativeXuText = ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.cs").Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.RollingPoll.cs").Replace("\r\n", "\n")
            + "\n" + ReadRepoFile("Sussudio/Services/Telemetry/NativeXuAtCommandProvider.SnapshotAssembly.cs").Replace("\r\n", "\n");

        AssertContains(statsPresentationDockText, "var sourceHdr = FormatSourceHdr(snapshot.SourceIsHdr, snapshot.SourceColorimetry);");
        AssertContains(statsPresentationDockText, "var sourceFormat = snapshot.SourceVideoFormat ?? \"\\u2014\";");
        AssertDoesNotContain(statsPresentationText, "var sourceFormat =\n            snapshot.ReaderSourceSubtype ??");
        AssertContains(statsDockRefreshControllerText, "StatsPresentationBuilder.BuildDockPresentation(snapshot)");
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
    }
}
