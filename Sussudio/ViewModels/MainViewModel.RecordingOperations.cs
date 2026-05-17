using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Concrete recording start/stop operations invoked by the serialized lifecycle gate.
/// </summary>
public partial class MainViewModel
{
    private async Task StartRecordingAsync(CancellationToken cancellationToken = default)
    {
        if (SelectedDevice == null)
        {
            StatusText = "No device selected";
            throw new InvalidOperationException(StatusText);
        }

        if (!IsInitialized)
        {
            await InitializeDeviceAsync(cancellationToken);
            if (!IsInitialized)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(StatusText)
                        ? "Device failed to initialize."
                        : StatusText);
            }
        }

        try
        {
            var settings = BuildCaptureSettings();
            await _sessionCoordinator.StartRecordingAsync(settings, cancellationToken);

            IsRecording = true;
            _recordingStopwatch.Restart();
            _bitrateSamples.Clear();
            RecordingSizeInfo = "0 B";
            RecordingBitrateInfo = "--";
            StatusText = "Recording...";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            IsRecording = _sessionCoordinator.Snapshot.IsRecording;
            StatusText = "Recording start canceled";
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            IsRecording = _sessionCoordinator.Snapshot.IsRecording;
            StatusText = $"Recording failed: {ex.Message}";
            throw;
        }
    }

    private async Task StopRecordingAsync(CancellationToken cancellationToken = default)
    {
        // UX: Freeze the timer immediately when the user requests stop (finalization can take seconds).
        // Keep IsRecording true until the stop transition completes so the button remains in "Stop" state.
        _recordingStopwatch.Stop();

        try
        {
            await _sessionCoordinator.StopRecordingAsync(cancellationToken);
            IsRecording = false;
            StatusText = $"Recording saved ({RecordingTime})";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            IsRecording = _sessionCoordinator.Snapshot.IsRecording;
            StatusText = "Stop recording canceled";
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            IsRecording = _sessionCoordinator.Snapshot.IsRecording;
            StatusText = $"Stop recording failed: {ex.Message}";
            throw;
        }
    }
}
