using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Capture-service event callbacks that marshal runtime state updates onto the UI thread.
/// </summary>
public partial class MainViewModel
{
    private void OnCaptureStatusChanged(object? sender, string status)
    {
        if (!_dispatcherQueue.TryEnqueue(() =>
        {
            var runtimeSnapshot = _captureService.GetRuntimeSnapshot();
            StatusText = status;
            UpdateLiveCaptureInfo(runtimeSnapshot);
            UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);
        }))
        {
            Logger.Log($"CAPTURE_STATUS_UI_ENQUEUE_FAILED status='{status}'");
        }
    }

    private void OnCaptureError(object? sender, Exception ex)
    {
        if (!_dispatcherQueue.TryEnqueue(() =>
        {
            var runtimeSnapshot = _captureService.GetRuntimeSnapshot();
            StatusText = $"Error: {ex.Message}";
            IsInitialized = _captureService.IsInitialized;
            IsPreviewing = _captureService.IsVideoPreviewActive;
            IsRecording = _captureService.IsRecording;
            if (!IsPreviewing && !IsRecording)
            {
                ResetAudioMeter();
            }

            UpdateLiveCaptureInfo(runtimeSnapshot);
            UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);

            // AUDCLNT_E_DEVICE_INVALIDATED (0x88890004) arrives when the audio
            // engine is reset independently of a full system suspend — e.g. monitor
            // power-off, USB hot-unplug, or wake events that don't trigger
            // PowerManager.SystemResuming. Trigger a full rebind so the user does
            // not have to manually re-pick the device. The IsRecording guard inside
            // ReinitializeDeviceAsync (fix #1) prevents this path from running
            // mid-recording; EnqueueUiOperation serializes with any in-flight
            // PowerManager-triggered reinit from OnSystemResuming.
            unchecked
            {
                const int AudclntDeviceInvalidated = (int)0x88890004;
                if (ex is COMException comEx &&
                    comEx.HResult == AudclntDeviceInvalidated &&
                    IsPreviewing &&
                    !IsRecording)
                {
                    Logger.Log("AUDCLNT_E_DEVICE_INVALIDATED received — scheduling audio rebind.");
                    EnqueueUiOperation(
                        () => ReinitializeDeviceAsync("audio device invalidated"),
                        "audio device invalidated reinit");
                }
            }
        }))
        {
            Logger.Log($"CAPTURE_ERROR_UI_ENQUEUE_FAILED type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void OnCapturePreCleanupRequested()
    {
        // Fires on a background thread before CaptureService.CleanupAsync disposes
        // the shared D3D11 device. Stop the renderer first to prevent the same race
        // as the reinit crash (renderer calling native D3D on a dying device).
        var handlers = PreviewRendererStopRequested;
        if (handlers != null)
        {
            foreach (Func<Task> handler in handlers.GetInvocationList())
            {
                try { handler().GetAwaiter().GetResult(); }
                catch (Exception ex) { Logger.Log($"PreCleanup renderer stop warning: {ex.Message}"); }
            }
        }
    }

    private void OnFrameCaptured(object? sender, ulong frameCount)
    {
        // Could update frame count display if needed
    }
}
