using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Sussudio.Controllers;
using Sussudio.Models;

namespace Sussudio;

// UI-thread automation/runtime snapshot sampling for diagnostics and MCP/CLI callers.
public sealed partial class MainWindow
{
    private PreviewRuntimeSnapshotSamplingController _previewRuntimeSnapshotSamplingController = null!;

    private void InitializePreviewRuntimeSnapshotSamplingController()
    {
        _previewRuntimeSnapshotSamplingController = new PreviewRuntimeSnapshotSamplingController(new PreviewRuntimeSnapshotSamplingControllerContext
        {
            UiDispatchController = WindowUiDispatchController,
            ViewModel = ViewModel,
            RendererHostController = _previewRendererHostController,
            StartupSessionController = _previewStartupSessionController,
            StartupSignalCoordinator = _previewStartupSignalCoordinator,
            IsGpuElementVisible = () => PreviewSwapChainPanel.Visibility == Visibility.Visible,
            IsCpuElementVisible = () => PreviewImage.Visibility == Visibility.Visible,
            IsPlaceholderVisible = () => NoDevicePlaceholder.Visibility == Visibility.Visible,
            GetStartupVisualTimeoutMs = () => PreviewStartupVisualTimeoutMs
        });
    }

    private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync(CancellationToken cancellationToken = default)
        => await _previewRuntimeSnapshotSamplingController.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
}
