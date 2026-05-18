using System;
using Microsoft.UI.Xaml.Controls;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class RecordingStatePresentationControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required RecordingButtonChromeController RecordingButtonChrome { get; init; }
    public required Control AudioRecordToggle { get; init; }
    public required Control CustomAudioToggle { get; init; }
    public required Control MicrophoneToggle { get; init; }
    public required Control AudioInputComboBox { get; init; }
    public required Control MicrophoneComboBox { get; init; }
    public required Control DeviceAudioModeToggle { get; init; }
    public required Control AnalogAudioGainSlider { get; init; }
    public required Action ResetAudioMeterVisuals { get; init; }
    public required Action ApplyHdrToggleEnabledState { get; init; }
    public required Action RefreshHdrHintText { get; init; }
    public required Action UpdateDeviceApplyButtonState { get; init; }
    public required Action ApplyWindowTitle { get; init; }
}

internal sealed class RecordingStatePresentationController
{
    private readonly RecordingStatePresentationControllerContext _context;

    public RecordingStatePresentationController(RecordingStatePresentationControllerContext context)
    {
        _context = context;
    }

    public bool TryHandlePropertyChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.IsRecording):
                HandleRecordingChanged();
                return true;

            case nameof(MainViewModel.IsRecordingTransitioning):
                HandleRecordingTransitioningChanged();
                return true;

            case nameof(MainViewModel.IsFfmpegMissing):
                HandleFfmpegMissingChanged();
                return true;

            default:
                return false;
        }
    }

    public void HandleRecordingChanged()
    {
        var viewModel = _context.ViewModel;
        var isRecording = viewModel.IsRecording;
        var state = BuildPresentationState();

        _context.RecordingButtonChrome.ApplyRecordingGlow(isRecording);
        if (!isRecording)
        {
            _context.ResetAudioMeterVisuals();
        }
        _context.RecordingButtonChrome.ApplyRecordingButtonState(isRecording);

        _context.AudioRecordToggle.IsEnabled = state.AudioRecordToggleEnabled;
        _context.CustomAudioToggle.IsEnabled = state.CustomAudioToggleEnabled;
        _context.MicrophoneToggle.IsEnabled = state.MicrophoneToggleEnabled;
        _context.AudioInputComboBox.IsEnabled = state.AudioInputComboBoxEnabled;
        _context.MicrophoneComboBox.IsEnabled = state.MicrophoneComboBoxEnabled;
        _context.DeviceAudioModeToggle.IsEnabled = state.DeviceAudioModeToggleEnabled;
        _context.AnalogAudioGainSlider.IsEnabled = state.AnalogAudioGainSliderEnabled;
        _context.ApplyHdrToggleEnabledState();
        _context.RefreshHdrHintText();
        _context.UpdateDeviceApplyButtonState();
        _context.RecordingButtonChrome.ApplyRecordingPulse(isRecording);

        _context.ApplyWindowTitle();
    }

    public void HandleRecordingTransitioningChanged()
    {
        var viewModel = _context.ViewModel;
        var state = BuildPresentationState();
        _context.RecordingButtonChrome.ApplyTransitioningState(viewModel.IsRecording, state);
    }

    public void HandleFfmpegMissingChanged()
    {
        var state = BuildPresentationState();
        _context.RecordingButtonChrome.ApplyFfmpegMissingState(state);
    }

    private RecordingStatePresentationState BuildPresentationState()
    {
        var viewModel = _context.ViewModel;
        return RecordingStatePresentationPolicy.Build(new RecordingStatePresentationInput(
            IsRecording: viewModel.IsRecording,
            IsRecordingTransitioning: viewModel.IsRecordingTransitioning,
            IsFfmpegMissing: viewModel.IsFfmpegMissing,
            IsCustomAudioInputEnabled: viewModel.IsCustomAudioInputEnabled,
            IsMicrophoneEnabled: viewModel.IsMicrophoneEnabled,
            IsDeviceAudioControlSupported: viewModel.IsDeviceAudioControlSupported,
            SelectedDeviceAudioMode: viewModel.SelectedDeviceAudioMode));
    }
}
