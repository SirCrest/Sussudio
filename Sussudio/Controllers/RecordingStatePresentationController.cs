using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class RecordingStatePresentationControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required Border RecordingGlowBorder { get; init; }
    public required Storyboard RecordingGlowPulseStoryboard { get; init; }
    public required Storyboard RecPulseStoryboard { get; init; }
    public required Button RecordButton { get; init; }
    public required UIElement RecordButtonNormalContent { get; init; }
    public required ProgressRing RecordButtonStartingContent { get; init; }
    public required UIElement RecordButtonRecordingContent { get; init; }
    public required Control AudioRecordToggle { get; init; }
    public required Control CustomAudioToggle { get; init; }
    public required Control MicrophoneToggle { get; init; }
    public required Control AudioInputComboBox { get; init; }
    public required Control MicrophoneComboBox { get; init; }
    public required Control DeviceAudioModeToggle { get; init; }
    public required Control AnalogAudioGainSlider { get; init; }
    public required Action<double, double, Action?> AnimateRecordButtonWidth { get; init; }
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

    public void HandleRecordingChanged()
    {
        var viewModel = _context.ViewModel;
        var isRecording = viewModel.IsRecording;
        var state = BuildPresentationState();

        if (isRecording)
        {
            _context.RecordingGlowBorder.Opacity = 1.0;
            _context.RecordingGlowPulseStoryboard.Begin();
        }
        else
        {
            _context.RecordingGlowPulseStoryboard.Stop();
            _context.RecordingGlowBorder.Opacity = 0;
            _context.ResetAudioMeterVisuals();
        }

        _context.RecordButtonStartingContent.IsActive = false;
        _context.RecordButtonStartingContent.Visibility = Visibility.Collapsed;
        if (isRecording)
        {
            _context.RecordButtonNormalContent.Visibility = Visibility.Collapsed;
            _context.RecordButtonRecordingContent.Visibility = Visibility.Visible;
            _context.RecordButton.Padding = new Thickness(12, 0, 12, 0);
            _context.RecordButton.Width = double.NaN;
            _context.RecordButton.UpdateLayout();
            var targetWidth = _context.RecordButton.ActualWidth;
            _context.RecordButton.Width = 36;
            _context.AnimateRecordButtonWidth(36, targetWidth, null);
        }
        else
        {
            var currentWidth = _context.RecordButton.ActualWidth;
            _context.RecordButton.Width = currentWidth;
            _context.AnimateRecordButtonWidth(currentWidth, 36, () =>
            {
                _context.RecordButtonRecordingContent.Visibility = Visibility.Collapsed;
                _context.RecordButtonNormalContent.Visibility = Visibility.Visible;
                _context.RecordButton.Padding = new Thickness(0);
            });
        }

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
        if (isRecording)
        {
            _context.RecPulseStoryboard.Begin();
        }
        else
        {
            _context.RecPulseStoryboard.Stop();
        }

        _context.ApplyWindowTitle();
    }

    public void HandleRecordingTransitioningChanged()
    {
        var viewModel = _context.ViewModel;
        var state = BuildPresentationState();
        _context.RecordButton.IsEnabled = state.TransitionRecordButtonEnabled;
        if (viewModel.IsRecordingTransitioning)
        {
            if (viewModel.IsRecording)
            {
                _context.RecordButton.Width = _context.RecordButton.ActualWidth;
                _context.RecordButtonRecordingContent.Visibility = Visibility.Collapsed;
            }
            else
            {
                _context.RecordButtonNormalContent.Visibility = Visibility.Collapsed;
            }

            _context.RecordButtonStartingContent.IsActive = state.TransitionStartingContentActive;
            _context.RecordButtonStartingContent.Visibility = Visibility.Visible;
        }
        else
        {
            _context.RecordButtonStartingContent.IsActive = false;
            _context.RecordButtonStartingContent.Visibility = Visibility.Collapsed;
            _context.RecordButtonNormalContent.Visibility = ToVisibility(state.SettledNormalContentVisible);
            _context.RecordButtonRecordingContent.Visibility = ToVisibility(state.SettledRecordingContentVisible);
        }
    }

    public void HandleFfmpegMissingChanged()
    {
        var state = BuildPresentationState();
        _context.RecordButton.IsEnabled = state.FfmpegRecordButtonEnabled;
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

    private static Visibility ToVisibility(bool isVisible)
        => isVisible ? Visibility.Visible : Visibility.Collapsed;
}
