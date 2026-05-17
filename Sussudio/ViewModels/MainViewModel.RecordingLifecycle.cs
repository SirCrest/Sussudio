using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Recording lifecycle: toggle serialization, start/stop transitions, and emergency stop routing.
/// </summary>
public partial class MainViewModel
{
    public Task ToggleRecordingAsync()
        => _recordingTransitionController.ToggleRecordingAsync();

    public Task SetRecordingEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => SetRecordingDesiredStateAsync(enabled, cancellationToken);

    internal Task SetRecordingDesiredStateAsync(bool enabled, CancellationToken cancellationToken = default)
        => _recordingTransitionController.SetRecordingDesiredStateAsync(enabled, cancellationToken);

    /// <summary>
    /// Graceful-stop entry point for callers that must NOT short-circuit on the
    /// toggle CAS gate (e.g. the window-close handler). If a toggle is in flight,
    /// await it; afterwards, if still recording, initiate a fresh stop.
    /// </summary>
    public Task StopRecordingAndWaitAsync(CancellationToken cancellationToken = default)
        => _recordingTransitionController.StopRecordingAndWaitAsync(cancellationToken);

    internal Task StopRecordingForEmergencyAsync(CancellationToken cancellationToken = default)
        => _recordingTransitionController.StopRecordingForEmergencyAsync(cancellationToken);
}
