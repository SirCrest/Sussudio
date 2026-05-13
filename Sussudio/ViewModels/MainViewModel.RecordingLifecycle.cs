using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Recording lifecycle: toggle serialization, start/stop transitions, and emergency stop routing.
/// </summary>
public partial class MainViewModel
{
    public Task ToggleRecordingAsync()
        => SetRecordingDesiredStateAsync(!IsRecording);

    private Task BeginRecordingTransitionAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        if (enabled == IsRecording)
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
            IsRecordingTransitioning = true;
            StatusText = enabled ? "Starting recording..." : "Stopping recording...";

            if (enabled)
            {
                await StartRecordingAsync(cancellationToken);
            }
            else
            {
                await StopRecordingAsync(cancellationToken);
            }

            if (IsRecording != enabled)
            {
                throw new InvalidOperationException(
                    $"Recording transition did not reach requested state: requested={enabled}, actual={IsRecording}.");
            }
        }
        finally
        {
            IsRecordingTransitioning = false;
            Interlocked.Exchange(ref _recordingToggleInProgress, 0);
        }
    }

    internal Task SetRecordingDesiredStateAsync(bool enabled, CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(() => SetRecordingDesiredStateOnUiThreadAsync(enabled, cancellationToken), cancellationToken);

    /// <summary>
    /// Graceful-stop entry point for callers that must NOT short-circuit on the
    /// toggle CAS gate (e.g. the window-close handler). If a toggle is in flight,
    /// await it; afterwards, if still recording, initiate a fresh stop.
    /// </summary>
    public Task StopRecordingAndWaitAsync(CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(() => StopRecordingAndWaitOnUiThreadAsync(cancellationToken), cancellationToken);

    internal Task StopRecordingForEmergencyAsync(CancellationToken cancellationToken = default)
        => _sessionCoordinator.StopRecordingForEmergencyAsync(cancellationToken);

    private Task StopRecordingAndWaitOnUiThreadAsync(CancellationToken cancellationToken)
        => SetRecordingDesiredStateOnUiThreadAsync(enabled: false, cancellationToken);

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

            if (IsRecording == enabled)
            {
                return;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (IsRecording == enabled)
        {
            return;
        }

        await BeginRecordingTransitionAsync(enabled, cancellationToken);
        if (IsRecording != enabled)
        {
            throw new InvalidOperationException(
                $"Recording transition did not reach requested state: requested={enabled}, actual={IsRecording}.");
        }
    }

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
