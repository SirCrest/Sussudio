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
        private readonly MainViewModel _viewModel;
        private readonly MainViewModelRuntimeEventIngressController _eventIngressController;
        private DispatcherQueueTimer? _timer;

        public MainViewModelRuntimeLifecycleController(MainViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _eventIngressController = new MainViewModelRuntimeEventIngressController(_viewModel);
        }

        public void Start()
            => _eventIngressController.Attach();

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
            _eventIngressController.Detach();
            _viewModel._audioDeviceWatcher.Dispose();
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

    }
}
