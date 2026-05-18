using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private sealed partial class MainViewModelRecordingTransitionController
    {
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
