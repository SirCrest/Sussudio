using System.Threading;
using System.Threading.Tasks;
using Sussudio.Controllers;
using Sussudio.Models;

namespace Sussudio;

// XAML/automation adapter for window geometry and folder commands. Recording-
// aware close behavior stays with window lifecycle management.
public sealed partial class MainWindow
{
    private WindowAutomationController _windowAutomationController = null!;

    private void InitializeWindowAutomationController()
    {
        _windowAutomationController = new WindowAutomationController(
            new WindowAutomationControllerContext
            {
                DispatcherQueue = _dispatcherQueue,
                ViewModel = ViewModel,
                GetAppWindow = GetAppWindow,
                GetWindowHandle = () => _hwnd,
                InvokeOnUiThreadAsync = InvokeOnUiThreadAsync
            });
    }

    public Task MinimizeAsync(CancellationToken cancellationToken = default)
        => _windowAutomationController.MinimizeAsync(cancellationToken);

    public Task MaximizeAsync(CancellationToken cancellationToken = default)
        => _windowAutomationController.MaximizeAsync(cancellationToken);

    public Task RestoreAsync(CancellationToken cancellationToken = default)
        => _windowAutomationController.RestoreAsync(cancellationToken);

    public Task OpenRecordingsFolderAsync(CancellationToken cancellationToken = default)
        => _windowAutomationController.OpenRecordingsFolderAsync(cancellationToken);

    public Task MoveToAsync(int x, int y, CancellationToken cancellationToken = default)
        => _windowAutomationController.MoveToAsync(x, y, cancellationToken);

    public Task ResizeToAsync(int width, int height, CancellationToken cancellationToken = default)
        => _windowAutomationController.ResizeToAsync(width, height, cancellationToken);

    public Task SnapToRegionAsync(AutomationWindowAction region, CancellationToken cancellationToken = default)
        => _windowAutomationController.SnapToRegionAsync(region, cancellationToken);
}
