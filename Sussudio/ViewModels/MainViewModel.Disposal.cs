using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// View-model teardown adapter surface. Bounded disposal policy lives in
/// MainViewModelDisposalController.
/// </summary>
public partial class MainViewModel
{
    private void CancelActiveFlashbackExportForDispose()
    {
        Interlocked.Increment(ref _flashbackExportOperationId);
        var exportCts = Interlocked.Exchange(ref _exportCts, null);
        CancelFlashbackExportCts(exportCts);
        if (exportCts != null)
        {
            DisposeFlashbackExportCtsBestEffort(exportCts, "viewmodel_dispose");
        }
    }

    // REVIEWED 2026-04-07: IDisposable fallback only. MainWindow.Closed calls
    // await ViewModel.DisposeAsync(); this sync path is for GC finalizer safety.
    public void Dispose()
        => _disposalController.Dispose();

    public async ValueTask DisposeAsync()
        => await _disposalController.DisposeAsync().ConfigureAwait(false);
}
