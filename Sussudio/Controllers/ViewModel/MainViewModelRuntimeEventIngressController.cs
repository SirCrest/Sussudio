using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32;
using Sussudio.Services.Capture;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    /// <summary>
    /// Owns runtime event subscriptions and external event ingress for the
    /// compatibility ViewModel facade.
    /// </summary>
    private sealed partial class MainViewModelRuntimeEventIngressController
    {
        private readonly MainViewModelRuntimeEventIngressControllerContext _context;

        public MainViewModelRuntimeEventIngressController(MainViewModelRuntimeEventIngressControllerContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        private void OnCaptureStatusChanged(object? sender, string status)
        {
            if (!_context.TryEnqueueOnUiThread(() =>
            {
                var runtimeSnapshot = _context.GetRuntimeSnapshot();
                _context.SetStatusText(status);
                _context.UpdateLiveCaptureInfo(runtimeSnapshot);
                _context.UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);
            }))
            {
                Logger.Log($"CAPTURE_STATUS_UI_ENQUEUE_FAILED status='{status}'");
            }
        }

        private void OnCaptureError(object? sender, Exception ex)
        {
            if (!_context.TryEnqueueOnUiThread(() =>
            {
                var runtimeSnapshot = _context.GetRuntimeSnapshot();
                _context.SetStatusText($"Error: {ex.Message}");
                _context.SetIsInitialized(_context.IsCaptureInitialized());
                _context.SetIsPreviewing(_context.IsVideoPreviewActive());
                _context.SetIsRecording(_context.IsCaptureRecording());
                if (!_context.IsPreviewing() && !_context.IsRecording())
                {
                    _context.ResetAudioMeter();
                }

                _context.UpdateLiveCaptureInfo(runtimeSnapshot);
                _context.UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);

                // AUDCLNT_E_DEVICE_INVALIDATED (0x88890004) arrives when the audio
                // engine is reset independently of a full system suspend, e.g. monitor
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
                        _context.IsPreviewing() &&
                        !_context.IsRecording())
                    {
                        Logger.Log("AUDCLNT_E_DEVICE_INVALIDATED received \u2014 scheduling audio rebind.");
                        _context.EnqueueUiOperation(
                            () => _context.ReinitializeDeviceAsync("audio device invalidated"),
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
            // as the reinit crash, where the renderer calls native D3D on a dying device.
            var handlers = _context.GetPreviewRendererStopHandlers();
            foreach (var handler in handlers)
            {
                try { handler().GetAwaiter().GetResult(); }
                catch (Exception ex) { Logger.Log($"PreCleanup renderer stop warning: {ex.Message}"); }
            }
        }

        private void OnFrameCaptured(object? sender, ulong frameCount)
        {
            // Could update frame count display if needed.
        }

        // PowerModeChanged fires on the system thread pool - must not touch UI properties
        // directly. We act only on PowerModes.Resume; Suspend/StatusChange are ignored
        // (Suspend arrives just before the OS freezes the process so there's nothing
        // useful to do, and StatusChange fires on AC/battery transitions which don't
        // affect capture). All UI-state reads happen inside the EnqueueUiOperation
        // lambda, which executes on the DispatcherQueue thread. ReinitializeDeviceAsync's
        // IsRecording guard (fix #1) keeps this safe to call regardless of state.
        private void OnSystemPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode != PowerModes.Resume)
            {
                return;
            }

            Logger.Log("SYSTEM_RESUMING_EVENT received \u2014 scheduling capture rebind if previewing.");
            _context.EnqueueUiOperation(() =>
            {
                if (!_context.IsPreviewing() || !_context.IsInitialized() || _context.IsRecording())
                {
                    Logger.Log(
                        $"SYSTEM_RESUMING_REINIT_SKIP previewing={_context.IsPreviewing()} " +
                        $"initialized={_context.IsInitialized()} recording={_context.IsRecording()}");
                    return Task.CompletedTask;
                }

                Logger.Log("SYSTEM_RESUMING_REINIT_SCHEDULED");
                return _context.ReinitializeDeviceAsync("system resume");
            }, "system resume reinit");
        }
    }

}
