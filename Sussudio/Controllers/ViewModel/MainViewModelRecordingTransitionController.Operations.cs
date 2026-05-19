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
                _context.SetIsRecordingTransitioning(true);
                _context.SetStatusText(enabled ? "Starting recording..." : "Stopping recording...");

                if (enabled)
                {
                    await StartRecordingAsync(cancellationToken);
                }
                else
                {
                    await StopRecordingAsync(cancellationToken);
                }

                if (_context.IsRecording() != enabled)
                {
                    throw new InvalidOperationException(
                        $"Recording transition did not reach requested state: requested={enabled}, actual={_context.IsRecording()}.");
                }
            }
            finally
            {
                _context.SetIsRecordingTransitioning(false);
                Interlocked.Exchange(ref _recordingToggleInProgress, 0);
            }
        }

        private async Task StartRecordingAsync(CancellationToken cancellationToken = default)
        {
            if (!_context.HasSelectedDevice())
            {
                _context.SetStatusText("No device selected");
                throw new InvalidOperationException(_context.GetStatusText());
            }

            if (!_context.IsInitialized())
            {
                await _previewLifecycleController.InitializeDeviceAsync(cancellationToken);
                if (!_context.IsInitialized())
                {
                    throw new InvalidOperationException(
                        string.IsNullOrWhiteSpace(_context.GetStatusText())
                            ? "Device failed to initialize."
                            : _context.GetStatusText());
                }
            }

            try
            {
                var settings = _context.BuildCaptureSettings();
                await _context.StartRecordingAsync(settings, cancellationToken);

                _context.SetIsRecording(true);
                _context.RestartRecordingStopwatch();
                _context.ClearRecordingBitrateSamples();
                _context.SetRecordingSizeInfo("0 B");
                _context.SetRecordingBitrateInfo("--");
                _context.SetStatusText("Recording...");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _context.SetIsRecording(_context.GetSessionIsRecording());
                _context.SetStatusText("Recording start canceled");
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                _context.SetIsRecording(_context.GetSessionIsRecording());
                _context.SetStatusText($"Recording failed: {ex.Message}");
                throw;
            }
        }

        private async Task StopRecordingAsync(CancellationToken cancellationToken = default)
        {
            // UX: Freeze the timer immediately when the user requests stop (finalization can take seconds).
            // Keep IsRecording true until the stop transition completes so the button remains in "Stop" state.
            _context.StopRecordingStopwatch();

            try
            {
                await _context.StopRecordingAsync(cancellationToken);
                _context.SetIsRecording(false);
                _context.SetStatusText($"Recording saved ({_context.GetRecordingTime()})");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _context.SetIsRecording(_context.GetSessionIsRecording());
                _context.SetStatusText("Stop recording canceled");
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                _context.SetIsRecording(_context.GetSessionIsRecording());
                _context.SetStatusText($"Stop recording failed: {ex.Message}");
                throw;
            }
        }
    }
}
