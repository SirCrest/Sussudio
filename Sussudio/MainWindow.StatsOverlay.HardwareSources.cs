using Sussudio.Controllers;

namespace Sussudio;

public sealed partial class MainWindow
{
    private StatsOverlayHardwareSourceContext CreateStatsOverlayHardwareSourceContext()
        => new()
        {
            GetMjpegPipelineTimingDetails = ViewModel.GetMjpegPipelineTimingDetails,
            GetPendingPreviewFrameCount = () => _previewRendererHostController.PendingFrameCount,
            GetNvmlSnapshot = () => _nvmlMonitor?.GetLatestSnapshot(),
        };
}
