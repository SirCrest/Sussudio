using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Runtime;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    /// <summary>
    /// Owns bounded teardown policy for the compatibility ViewModel facade.
    /// </summary>
    private sealed class MainViewModelDisposalController
    {
        private const int DefaultDisposeTimeoutMs = 30000;

        private readonly MainViewModel _viewModel;

        public MainViewModelDisposalController(MainViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public void Dispose()
        {
            var disposeTimeoutMs = GetDisposeTimeoutMs();
            var disposeTask = Task.Run(DisposeCoreAsync);
            var completed = Task.WhenAny(disposeTask, Task.Delay(disposeTimeoutMs)).GetAwaiter().GetResult();
            if (completed != disposeTask)
            {
                Logger.Log($"ViewModel dispose timed out after {disposeTimeoutMs} ms.");
                return;
            }

            try
            {
                disposeTask.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.Log($"ViewModel dispose failed: {ex.Message}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            var disposeTimeoutMs = GetDisposeTimeoutMs();
            var disposeTask = DisposeCoreAsync();
            var completed = await Task.WhenAny(disposeTask, Task.Delay(disposeTimeoutMs)).ConfigureAwait(false);
            if (completed != disposeTask)
            {
                Logger.Log($"ViewModel async dispose timed out after {disposeTimeoutMs} ms.");
                return;
            }

            try
            {
                await disposeTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"ViewModel async dispose failed: {ex.Message}");
            }
        }

        private async Task DisposeCoreAsync()
        {
            if (Interlocked.Exchange(ref _viewModel._disposeState, 1) == 1)
            {
                return;
            }

            _viewModel.CancelActiveFlashbackExportForDispose();
            _viewModel._deviceAudioRequestController.CancelPendingAudioControlWork();
            _viewModel._runtimeLifecycleController.StopForDispose();

            var stepTimeoutMs = EnvironmentHelpers.GetIntFromEnv(
                "SUSSUDIO_VIEWMODEL_DISPOSE_STEP_TIMEOUT_MS",
                DefaultDisposeTimeoutMs,
                1000,
                300000);

            await RunDisposeStepAsync(
                _viewModel._sessionCoordinator.CleanupAsync(),
                stepTimeoutMs,
                "Coordinator cleanup",
                "ViewModel cleanup during dispose failed").ConfigureAwait(false);
            await RunDisposeStepAsync(
                _viewModel._sessionCoordinator.DisposeAsync().AsTask(),
                stepTimeoutMs,
                "Coordinator dispose",
                "Coordinator dispose failed").ConfigureAwait(false);

            try
            {
                await AwaitWithTimeoutAsync(
                    _viewModel._captureService.DisposeAsync().AsTask(),
                    stepTimeoutMs,
                    "Capture service dispose").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Capture service async dispose failed: {ex.Message}");
                _viewModel._captureService.Dispose();
            }
        }

        private static int GetDisposeTimeoutMs()
            => EnvironmentHelpers.GetIntFromEnv(
                "SUSSUDIO_VIEWMODEL_DISPOSE_TIMEOUT_MS",
                DefaultDisposeTimeoutMs,
                1000,
                300000);

        private static async Task RunDisposeStepAsync(
            Task task,
            int timeoutMs,
            string operationName,
            string failureLogPrefix)
        {
            try
            {
                await AwaitWithTimeoutAsync(task, timeoutMs, operationName).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"{failureLogPrefix}: {ex.Message}");
            }
        }
    }
}
