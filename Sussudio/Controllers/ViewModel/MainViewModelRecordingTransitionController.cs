using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    /// <summary>
    /// Owns UI-facing recording start/stop transition serialization and state repair.
    /// </summary>
    private sealed class MainViewModelRecordingTransitionController
    {
        private readonly MainViewModel _viewModel;
        private int _recordingToggleInProgress;
        // Holds the in-flight ToggleRecordingAsync task so the window-close path can
        // observe (and await) an already-running stop instead of short-circuiting on
        // the CAS gate. Cleared by the transition completion continuation.
        private volatile Task? _activeRecordingToggleTask;
        private int _activeRecordingTransitionTarget = -1;

        public MainViewModelRecordingTransitionController(MainViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public Task ToggleRecordingAsync()
            => SetRecordingDesiredStateAsync(!_viewModel.IsRecording);

        public Task SetRecordingDesiredStateAsync(bool enabled, CancellationToken cancellationToken = default)
            => _viewModel.InvokeOnUiThreadAsync(
                () => SetRecordingDesiredStateOnUiThreadAsync(enabled, cancellationToken),
                cancellationToken);

        /// <summary>
        /// Graceful-stop entry point for callers that must NOT short-circuit on the
        /// toggle CAS gate. If a toggle is in flight, await it; afterwards, if still
        /// recording, initiate a fresh stop.
        /// </summary>
        public Task StopRecordingAndWaitAsync(CancellationToken cancellationToken = default)
            => _viewModel.InvokeOnUiThreadAsync(
                () => SetRecordingDesiredStateOnUiThreadAsync(enabled: false, cancellationToken),
                cancellationToken);

        public Task StopRecordingForEmergencyAsync(CancellationToken cancellationToken = default)
            => _viewModel._sessionCoordinator.StopRecordingForEmergencyAsync(cancellationToken);

        private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)
        {
            if (enabled == _viewModel.IsRecording)
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

        private async Task RecordingTransitionInnerAsync(bool enabled, CancellationToken cancellationToken)
        {
            try
            {
                _viewModel.IsRecordingTransitioning = true;
                _viewModel.StatusText = enabled ? "Starting recording..." : "Stopping recording...";

                if (enabled)
                {
                    await StartRecordingAsync(cancellationToken);
                }
                else
                {
                    await StopRecordingAsync(cancellationToken);
                }

                if (_viewModel.IsRecording != enabled)
                {
                    throw new InvalidOperationException(
                        $"Recording transition did not reach requested state: requested={enabled}, actual={_viewModel.IsRecording}.");
                }
            }
            finally
            {
                _viewModel.IsRecordingTransitioning = false;
                Interlocked.Exchange(ref _recordingToggleInProgress, 0);
            }
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

                if (_viewModel.IsRecording == enabled)
                {
                    return;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (_viewModel.IsRecording == enabled)
            {
                return;
            }

            await BeginRecordingTransitionAsync(enabled, cancellationToken);
            if (_viewModel.IsRecording != enabled)
            {
                throw new InvalidOperationException(
                    $"Recording transition did not reach requested state: requested={enabled}, actual={_viewModel.IsRecording}.");
            }
        }

        private async Task StartRecordingAsync(CancellationToken cancellationToken = default)
        {
            if (_viewModel.SelectedDevice == null)
            {
                _viewModel.StatusText = "No device selected";
                throw new InvalidOperationException(_viewModel.StatusText);
            }

            if (!_viewModel.IsInitialized)
            {
                await _viewModel.InitializeDeviceAsync(cancellationToken);
                if (!_viewModel.IsInitialized)
                {
                    throw new InvalidOperationException(
                        string.IsNullOrWhiteSpace(_viewModel.StatusText)
                            ? "Device failed to initialize."
                            : _viewModel.StatusText);
                }
            }

            try
            {
                var settings = _viewModel.BuildCaptureSettings();
                await _viewModel._sessionCoordinator.StartRecordingAsync(settings, cancellationToken);

                _viewModel.IsRecording = true;
                _viewModel._recordingStopwatch.Restart();
                _viewModel._bitrateSamples.Clear();
                _viewModel.RecordingSizeInfo = "0 B";
                _viewModel.RecordingBitrateInfo = "--";
                _viewModel.StatusText = "Recording...";
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _viewModel.IsRecording = _viewModel._sessionCoordinator.Snapshot.IsRecording;
                _viewModel.StatusText = "Recording start canceled";
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                _viewModel.IsRecording = _viewModel._sessionCoordinator.Snapshot.IsRecording;
                _viewModel.StatusText = $"Recording failed: {ex.Message}";
                throw;
            }
        }

        private async Task StopRecordingAsync(CancellationToken cancellationToken = default)
        {
            // UX: Freeze the timer immediately when the user requests stop (finalization can take seconds).
            // Keep IsRecording true until the stop transition completes so the button remains in "Stop" state.
            _viewModel._recordingStopwatch.Stop();

            try
            {
                await _viewModel._sessionCoordinator.StopRecordingAsync(cancellationToken);
                _viewModel.IsRecording = false;
                _viewModel.StatusText = $"Recording saved ({_viewModel.RecordingTime})";
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _viewModel.IsRecording = _viewModel._sessionCoordinator.Snapshot.IsRecording;
                _viewModel.StatusText = "Stop recording canceled";
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                _viewModel.IsRecording = _viewModel._sessionCoordinator.Snapshot.IsRecording;
                _viewModel.StatusText = $"Stop recording failed: {ex.Message}";
                throw;
            }
        }
    }
}
