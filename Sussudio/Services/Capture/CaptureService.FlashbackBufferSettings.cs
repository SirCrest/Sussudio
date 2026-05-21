using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.Services.Capture;

// Flashback buffer mutations: live buffer duration and GPU decode propagation.
public partial class CaptureService
{
    /// <summary>
    /// Updates flashback-specific fields in the active capture settings without
    /// requiring a full session restart. Call before <see cref="RestartFlashbackAsync"/>
    /// so the rebuild uses the latest values.
    /// </summary>
    // REVIEWED 2026-04-07: called from UI thread only; values are independent scalars
    // so a stale read from a background thread produces a slightly-off config, not a crash.
    // RestartFlashbackAsync (which consumes these) acquires _sessionTransitionLock.
    public Task UpdateFlashbackSettingsAsync(
        int bufferMinutes,
        bool gpuDecode,
        CancellationToken cancellationToken = default)
        => RunTransitionAsync(CurrentSessionState, transitionToken =>
        {
            if (_currentSettings != null)
            {
                _currentSettings.FlashbackBufferMinutes = bufferMinutes;
                _currentSettings.FlashbackGpuDecode = gpuDecode;
            }

            if (_flashbackBackend.PlaybackController != null)
            {
                _flashbackBackend.PlaybackController.GpuDecodeEnabled = gpuDecode;
            }

            if (_isRecording && IsFlashbackRecordingBackendActive())
            {
                _pendingFlashbackSettingsChange = true;
            }

            return Task.CompletedTask;
        }, cancellationToken);
}
