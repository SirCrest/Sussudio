using System.Threading;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed partial class MainViewModelControllerGraph
    {
        private static MainViewModelDisposalController CreateDisposalController(MainViewModel viewModel)
        {
            return new MainViewModelDisposalController(
                new MainViewModelDisposalControllerContext
                {
                    TryBeginDispose = () => Interlocked.Exchange(ref viewModel._disposeState, 1) == 0,
                    CancelActiveFlashbackExport = viewModel.CancelActiveFlashbackExportForDispose,
                    CancelPendingAudioControlWork = viewModel._deviceAudioRequestController.CancelPendingAudioControlWork,
                    StopRuntimeForDispose = viewModel._runtimeLifecycleController.StopForDispose,
                    CleanupSessionCoordinatorAsync = () => viewModel._sessionCoordinator.CleanupAsync(),
                    DisposeSessionCoordinatorAsync = () => viewModel._sessionCoordinator.DisposeAsync().AsTask(),
                    DisposeCaptureServiceAsync = () => viewModel._captureService.DisposeAsync().AsTask(),
                    DisposeCaptureService = viewModel._captureService.Dispose,
                });
        }
    }
}
