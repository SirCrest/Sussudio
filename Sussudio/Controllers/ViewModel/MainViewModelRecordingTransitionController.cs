using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    /// <summary>
    /// Owns UI-facing recording start/stop transition serialization and state repair.
    /// </summary>
    private sealed partial class MainViewModelRecordingTransitionController
    {
        private readonly MainViewModelRecordingTransitionControllerContext _context;
        private readonly MainViewModelPreviewLifecycleController _previewLifecycleController;
        private int _recordingToggleInProgress;
        // Holds the in-flight ToggleRecordingAsync task so the window-close path can
        // observe (and await) an already-running stop instead of short-circuiting on
        // the CAS gate. Cleared by the transition completion continuation.
        private volatile Task? _activeRecordingToggleTask;
        private int _activeRecordingTransitionTarget = -1;

        public MainViewModelRecordingTransitionController(
            MainViewModelRecordingTransitionControllerContext context,
            MainViewModelPreviewLifecycleController previewLifecycleController)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _previewLifecycleController = previewLifecycleController ?? throw new ArgumentNullException(nameof(previewLifecycleController));
        }

        public Task ToggleRecordingAsync()
            => SetRecordingDesiredStateAsync(!_context.IsRecording());

        public Task SetRecordingDesiredStateAsync(bool enabled, CancellationToken cancellationToken = default)
            => _context.InvokeOnUiThreadAsync(
                () => SetRecordingDesiredStateOnUiThreadAsync(enabled, cancellationToken),
                cancellationToken);

        /// <summary>
        /// Graceful-stop entry point for callers that must NOT short-circuit on the
        /// toggle CAS gate. If a toggle is in flight, await it; afterwards, if still
        /// recording, initiate a fresh stop.
        /// </summary>
        public Task StopRecordingAndWaitAsync(CancellationToken cancellationToken = default)
            => _context.InvokeOnUiThreadAsync(
                () => SetRecordingDesiredStateOnUiThreadAsync(enabled: false, cancellationToken),
                cancellationToken);

        private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)
        {
            if (enabled == _context.IsRecording())
            {
                return Task.CompletedTask;
            }

            if (Interlocked.CompareExchange(ref _recordingToggleInProgress, 1, 0) != 0)
            {
                Logger.Log("Recording transition rejected: operation already in progress.");
                throw new InvalidOperationException("Recording transition already in progress.");
            }

            var task = RecordingTransitionInnerAsync(enabled, cancellationToken);
            Volatile.Write(ref _activeRecordingTransitionTarget, enabled ? 1 : 0);
            _activeRecordingToggleTask = task;
            _ = task.ContinueWith(completed =>
            {
                if (ReferenceEquals(_activeRecordingToggleTask, completed))
                {
                    _activeRecordingToggleTask = null;
                    Volatile.Write(ref _activeRecordingTransitionTarget, -1);
                }
            }, TaskScheduler.Default);

            return task;
        }

        private async Task SetRecordingDesiredStateOnUiThreadAsync(bool enabled, CancellationToken cancellationToken)
        {
            var inFlight = _activeRecordingToggleTask;
            if (inFlight != null && !inFlight.IsCompleted)
            {
                var inFlightTarget = Volatile.Read(ref _activeRecordingTransitionTarget);
                Exception? transitionError = null;
                try
                {
                    await inFlight;
                }
                catch (OperationCanceledException ex)
                {
                    transitionError = ex;
                    Logger.Log($"Recording transition wait canceled: {ex.Message}");
                }
                catch (Exception ex)
                {
                    transitionError = ex;
                    Logger.Log($"Recording transition wait faulted: {ex.Message}");
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (transitionError is OperationCanceledException transitionCanceled && inFlightTarget == (enabled ? 1 : 0))
                {
                    throw transitionCanceled;
                }

                if (transitionError != null && inFlightTarget == (enabled ? 1 : 0))
                {
                    throw new InvalidOperationException("Recording transition failed.", transitionError);
                }

                if (_context.IsRecording() == enabled)
                {
                    return;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (_context.IsRecording() == enabled)
            {
                return;
            }

            await BeginRecordingTransitionAsync(enabled, cancellationToken);
            if (_context.IsRecording() != enabled)
            {
                throw new InvalidOperationException(
                    $"Recording transition did not reach requested state: requested={enabled}, actual={_context.IsRecording()}.");
            }
        }

    }

    private sealed class MainViewModelRecordingTransitionControllerContext
    {
        public required Func<bool> IsRecording { get; init; }
        public required Action<bool> SetIsRecording { get; init; }
        public required Func<bool> IsInitialized { get; init; }
        public required Func<bool> HasSelectedDevice { get; init; }
        public required Func<string> GetStatusText { get; init; }
        public required Action<string> SetStatusText { get; init; }
        public required Action<bool> SetIsRecordingTransitioning { get; init; }
        public required Func<Func<Task>, CancellationToken, Task> InvokeOnUiThreadAsync { get; init; }
        public required Func<CaptureSettings> BuildCaptureSettings { get; init; }
        public required Func<CaptureSettings, CancellationToken, Task> StartRecordingAsync { get; init; }
        public required Func<CancellationToken, Task> StopRecordingAsync { get; init; }
        public required Func<bool> GetSessionIsRecording { get; init; }
        public required Action RestartRecordingStopwatch { get; init; }
        public required Action StopRecordingStopwatch { get; init; }
        public required Action ClearRecordingBitrateSamples { get; init; }
        public required Action<string> SetRecordingSizeInfo { get; init; }
        public required Action<string> SetRecordingBitrateInfo { get; init; }
        public required Func<string> GetRecordingTime { get; init; }
    }
}
