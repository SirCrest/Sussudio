using System;
using Microsoft.UI.Dispatching;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    /// <summary>
    /// Owns runtime bootstrap, periodic refresh, and shutdown coordination for
    /// the compatibility ViewModel facade.
    /// </summary>
    private sealed class MainViewModelRuntimeLifecycleController
    {
        private readonly MainViewModelRuntimeLifecycleControllerContext _context;
        private readonly MainViewModelRuntimeEventIngressController _eventIngressController;
        private DispatcherQueueTimer? _timer;

        public MainViewModelRuntimeLifecycleController(MainViewModelRuntimeLifecycleControllerContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _eventIngressController = _context.CreateEventIngressController();
        }

        public void Start()
            => _eventIngressController.Attach();

        public void InitializePresentation()
        {
            var latestSourceTelemetry = _context.GetLatestSourceTelemetrySnapshot();
            _context.SetLatestSourceTelemetrySnapshot(latestSourceTelemetry);
            _context.ApplySourceTelemetrySnapshot(latestSourceTelemetry, false);
            _context.UpdateHdrRuntimeStatusFromCapture();
            _context.UpdateLiveCaptureInfo();

            SetupTimer();
            _context.UpdateDiskSpace();
        }

        public void StopForDispose()
        {
            _timer?.Stop();
            _eventIngressController.Detach();
            _context.DisposeAudioDeviceWatcher();
        }

        private void SetupTimer()
        {
            _timer = _context.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) =>
            {
                var runtimeSnapshot = _context.GetRuntimeSnapshot();

                if (_context.IsRecording())
                {
                    _context.SetRecordingTime(_context.GetRecordingElapsed().ToString(@"hh\:mm\:ss"));
                    _context.UpdateRecordingStats();
                }

                if (!_context.IsRecording() && _context.IsFlashbackActive())
                {
                    _context.UpdateFlashbackBitrate();
                }

                if (_context.IsPreviewing() || _context.IsRecording())
                {
                    _context.UpdateLiveCaptureInfo(runtimeSnapshot);
                }
                else
                {
                    _context.ResetLiveCaptureInfo();
                }

                _context.UpdateDiskSpace();
                _context.RefreshSourceTelemetrySummaryAge();
                _context.UpdateHdrRuntimeStatusFromCapture(runtimeSnapshot);
            };
            _timer.Start();
        }

    }
}
