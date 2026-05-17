using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.Win32;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    /// <summary>
    /// Owns runtime event ingress, periodic refresh, and shutdown detachment for
    /// the compatibility ViewModel facade.
    /// </summary>
    private sealed class MainViewModelRuntimeLifecycleController
    {
        private readonly MainViewModel _viewModel;
        private DispatcherQueueTimer? _timer;

        public MainViewModelRuntimeLifecycleController(MainViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public void Start()
        {
            _viewModel._deviceService.FormatProbeCompleted += _viewModel.OnDeviceFormatProbeCompleted;

            _viewModel._captureService.StatusChanged += OnCaptureStatusChanged;
            _viewModel._captureService.ErrorOccurred += OnCaptureError;
            _viewModel._captureService.PreCleanupRequested += OnCapturePreCleanupRequested;
            _viewModel._captureService.FrameCaptured += OnFrameCaptured;
            _viewModel._captureService.AudioLevelUpdated += _viewModel.OnAudioLevelUpdated;
            _viewModel._captureService.MicrophoneAudioLevelUpdated += _viewModel.OnMicrophoneAudioLevelUpdated;
            _viewModel._captureService.SourceTelemetryUpdated += _viewModel.OnSourceTelemetryUpdated;

            // SystemEvents.PowerModeChanged is the managed desktop wake signal used
            // to recover capture after sleep or hibernate resume.
            SystemEvents.PowerModeChanged += OnSystemPowerModeChanged;

            _viewModel._audioDeviceWatcher.DevicesChanged += _viewModel.OnAudioDevicesChanged;
        }

        public void InitializePresentation()
        {
            _viewModel._latestSourceTelemetry = _viewModel._captureService.GetLatestSourceTelemetrySnapshot();
            _viewModel.ApplySourceTelemetrySnapshot(_viewModel._latestSourceTelemetry, allowAutoRetarget: false);
            _viewModel.UpdateHdrRuntimeStatusFromCapture();
            _viewModel.UpdateLiveCaptureInfo();

            SetupTimer();
            _viewModel.UpdateDiskSpace();
        }

        public void StopForDispose()
        {
            _timer?.Stop();
            DetachRuntimeWiring();
            _viewModel._audioDeviceWatcher.Dispose();
        }

        private void DetachRuntimeWiring()
        {
            _viewModel._deviceService.FormatProbeCompleted -= _viewModel.OnDeviceFormatProbeCompleted;

            SystemEvents.PowerModeChanged -= OnSystemPowerModeChanged;

            _viewModel._captureService.StatusChanged -= OnCaptureStatusChanged;
            _viewModel._captureService.ErrorOccurred -= OnCaptureError;
            _viewModel._captureService.PreCleanupRequested -= OnCapturePreCleanupRequested;
            _viewModel._captureService.FrameCaptured -= OnFrameCaptured;
            _viewModel._captureService.AudioLevelUpdated -= _viewModel.OnAudioLevelUpdated;
            _viewModel._captureService.MicrophoneAudioLevelUpdated -= _viewModel.OnMicrophoneAudioLevelUpdated;
            _viewModel._captureService.SourceTelemetryUpdated -= _viewModel.OnSourceTelemetryUpdated;

            _viewModel._audioDeviceWatcher.DevicesChanged -= _viewModel.OnAudioDevicesChanged;
        }

        private void SetupTimer()
        {
            _timer = _viewModel._dispatcherQueue.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) =>
            {
                var runtimeSnapshot = _viewModel._captureService.GetRuntimeSnapshot();

                if (_viewModel.IsRecording)
                {
                    _viewModel.RecordingTime = _viewModel._recordingStopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                    _viewModel.UpdateRecordingStats();
                }

                if (!_viewModel.IsRecording && _viewModel._captureService.IsFlashbackActive)
                {
                    _viewModel.UpdateFlashbackBitrate();
                }

                if (_viewModel.IsPreviewing || _viewModel.IsRecording)
                {
                    _viewModel.UpdateLiveCaptureInfo(runtimeSnapshot);
                }
                else
                {
                    _viewModel.ResetLiveCaptureInfo();
                }

                _viewModel.UpdateDiskSpace();
                _viewModel.RefreshSourceTelemetrySummaryAge();
                _viewModel.UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);
            };
            _timer.Start();
        }

        private void OnCaptureStatusChanged(object? sender, string status)
        {
            if (!_viewModel._dispatcherQueue.TryEnqueue(() =>
            {
                var runtimeSnapshot = _viewModel._captureService.GetRuntimeSnapshot();
                _viewModel.StatusText = status;
                _viewModel.UpdateLiveCaptureInfo(runtimeSnapshot);
                _viewModel.UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);
            }))
            {
                Logger.Log($"CAPTURE_STATUS_UI_ENQUEUE_FAILED status='{status}'");
            }
        }

        private void OnCaptureError(object? sender, Exception ex)
        {
            if (!_viewModel._dispatcherQueue.TryEnqueue(() =>
            {
                var runtimeSnapshot = _viewModel._captureService.GetRuntimeSnapshot();
                _viewModel.StatusText = $"Error: {ex.Message}";
                _viewModel.IsInitialized = _viewModel._captureService.IsInitialized;
                _viewModel.IsPreviewing = _viewModel._captureService.IsVideoPreviewActive;
                _viewModel.IsRecording = _viewModel._captureService.IsRecording;
                if (!_viewModel.IsPreviewing && !_viewModel.IsRecording)
                {
                    _viewModel.ResetAudioMeter();
                }

                _viewModel.UpdateLiveCaptureInfo(runtimeSnapshot);
                _viewModel.UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);

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
                        _viewModel.IsPreviewing &&
                        !_viewModel.IsRecording)
                    {
                        Logger.Log("AUDCLNT_E_DEVICE_INVALIDATED received \u2014 scheduling audio rebind.");
                        _viewModel.EnqueueUiOperation(
                            () => _viewModel.ReinitializeDeviceAsync("audio device invalidated"),
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
            var handlers = _viewModel.PreviewRendererStopRequested;
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
            _viewModel.EnqueueUiOperation(() =>
            {
                if (!_viewModel.IsPreviewing || !_viewModel.IsInitialized || _viewModel.IsRecording)
                {
                    Logger.Log(
                        $"SYSTEM_RESUMING_REINIT_SKIP previewing={_viewModel.IsPreviewing} " +
                        $"initialized={_viewModel.IsInitialized} recording={_viewModel.IsRecording}");
                    return Task.CompletedTask;
                }

                Logger.Log("SYSTEM_RESUMING_REINIT_SCHEDULED");
                return _viewModel.ReinitializeDeviceAsync("system resume");
            }, "system resume reinit");
        }
    }
}
