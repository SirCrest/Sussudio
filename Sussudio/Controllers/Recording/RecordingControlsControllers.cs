using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Sussudio.Models;
using Sussudio.ViewModels;
using Windows.Storage.Pickers;

namespace Sussudio.Controllers;

internal readonly record struct RecordingPreviewActivitySnapshot(
    bool GpuActive,
    bool CpuActive,
    bool PlaceholderVisible)
{
    public bool RendererActive => GpuActive || CpuActive;
}

internal sealed class RecordingButtonActionControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required Func<RecordingPreviewActivitySnapshot> GetPreviewActivitySnapshot { get; init; }
}

internal sealed class RecordingButtonActionController
{
    private readonly RecordingButtonActionControllerContext _context;

    public RecordingButtonActionController(RecordingButtonActionControllerContext context)
    {
        _context = context;
    }

    public async Task ToggleRecordingAsync()
    {
        await _context.ViewModel.ToggleRecordingAsync();

        if (!_context.ViewModel.IsRecording)
        {
            return;
        }

        var snapshot = _context.GetPreviewActivitySnapshot();
        Logger.Log(
            $"PreviewStateDuringRecording: rendererActive={snapshot.RendererActive}, " +
            $"gpuActive={snapshot.GpuActive}, cpuActive={snapshot.CpuActive}, " +
            $"placeholderVisible={snapshot.PlaceholderVisible}");

        if (!snapshot.RendererActive || snapshot.PlaceholderVisible)
        {
            Logger.Log("WARNING: preview renderer appears inactive while recording.");
        }
    }
}

internal sealed class RecordingButtonChromeControllerContext
{
    public required Border RecordingGlowBorder { get; init; }
    public required Storyboard RecordingGlowPulseStoryboard { get; init; }
    public required Storyboard RecPulseStoryboard { get; init; }
    public required Button RecordButton { get; init; }
    public required UIElement RecordButtonNormalContent { get; init; }
    public required ProgressRing RecordButtonStartingContent { get; init; }
    public required UIElement RecordButtonRecordingContent { get; init; }
}

internal sealed class RecordingButtonChromeController
{
    private const double CollapsedRecordButtonWidth = 36;
    private readonly RecordingButtonChromeControllerContext _context;

    public RecordingButtonChromeController(RecordingButtonChromeControllerContext context)
    {
        _context = context;
    }

    public void ApplyRecordingGlow(bool isRecording)
    {
        if (isRecording)
        {
            _context.RecordingGlowBorder.Opacity = 1.0;
            _context.RecordingGlowPulseStoryboard.Begin();
        }
        else
        {
            _context.RecordingGlowPulseStoryboard.Stop();
            _context.RecordingGlowBorder.Opacity = 0;
        }
    }

    public void ApplyRecordingButtonState(bool isRecording)
    {
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
            _context.RecordButton.Width = CollapsedRecordButtonWidth;
            AnimateWidth(CollapsedRecordButtonWidth, targetWidth, null);
        }
        else
        {
            var currentWidth = _context.RecordButton.ActualWidth;
            _context.RecordButton.Width = currentWidth;
            AnimateWidth(currentWidth, CollapsedRecordButtonWidth, () =>
            {
                _context.RecordButtonRecordingContent.Visibility = Visibility.Collapsed;
                _context.RecordButtonNormalContent.Visibility = Visibility.Visible;
                _context.RecordButton.Padding = new Thickness(0);
            });
        }
    }

    public void ApplyRecordingPulse(bool isRecording)
    {
        if (isRecording)
        {
            _context.RecPulseStoryboard.Begin();
        }
        else
        {
            _context.RecPulseStoryboard.Stop();
        }
    }

    public void ApplyTransitioningState(bool isRecording, RecordingStatePresentationState state)
    {
        _context.RecordButton.IsEnabled = state.TransitionRecordButtonEnabled;
        if (state.TransitionStartingContentActive)
        {
            if (isRecording)
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

    public void ApplyFfmpegMissingState(RecordingStatePresentationState state)
    {
        _context.RecordButton.IsEnabled = state.FfmpegRecordButtonEnabled;
    }

    private void AnimateWidth(double from, double to, Action? onCompleted = null)
    {
        var anim = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(anim, _context.RecordButton);
        Storyboard.SetTargetProperty(anim, "Width");

        var storyboard = new Storyboard();
        storyboard.Children.Add(anim);
        storyboard.Completed += (_, _) =>
        {
            _context.RecordButton.Width = to == CollapsedRecordButtonWidth ? CollapsedRecordButtonWidth : double.NaN;
            onCompleted?.Invoke();
        };
        storyboard.Begin();
    }

    private static Visibility ToVisibility(bool isVisible)
        => isVisible ? Visibility.Visible : Visibility.Collapsed;
}

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

internal static class RecordingStatePresentationPolicy
{
    internal static RecordingStatePresentationState Build(RecordingStatePresentationInput input)
    {
        var isIdle = !input.IsRecording;
        var isAnalogAudioMode = string.Equals(
            input.SelectedDeviceAudioMode,
            DeviceAudioMode.Analog,
            StringComparison.OrdinalIgnoreCase);

        return new RecordingStatePresentationState(
            AudioRecordToggleEnabled: isIdle,
            CustomAudioToggleEnabled: isIdle,
            MicrophoneToggleEnabled: isIdle,
            AudioInputComboBoxEnabled: input.IsCustomAudioInputEnabled && isIdle,
            MicrophoneComboBoxEnabled: input.IsMicrophoneEnabled && isIdle,
            DeviceAudioModeToggleEnabled: input.IsDeviceAudioControlSupported && isIdle,
            AnalogAudioGainSliderEnabled: input.IsDeviceAudioControlSupported && isAnalogAudioMode && isIdle,
            TransitionRecordButtonEnabled: !input.IsRecordingTransitioning,
            FfmpegRecordButtonEnabled: !input.IsFfmpegMissing && !input.IsRecordingTransitioning,
            TransitionStartingContentActive: input.IsRecordingTransitioning,
            SettledNormalContentVisible: !input.IsRecording,
            SettledRecordingContentVisible: input.IsRecording);
    }
}

internal readonly record struct RecordingStatePresentationInput(
    bool IsRecording,
    bool IsRecordingTransitioning,
    bool IsFfmpegMissing,
    bool IsCustomAudioInputEnabled,
    bool IsMicrophoneEnabled,
    bool IsDeviceAudioControlSupported,
    string? SelectedDeviceAudioMode);

internal readonly record struct RecordingStatePresentationState(
    bool AudioRecordToggleEnabled,
    bool CustomAudioToggleEnabled,
    bool MicrophoneToggleEnabled,
    bool AudioInputComboBoxEnabled,
    bool MicrophoneComboBoxEnabled,
    bool DeviceAudioModeToggleEnabled,
    bool AnalogAudioGainSliderEnabled,
    bool TransitionRecordButtonEnabled,
    bool FfmpegRecordButtonEnabled,
    bool TransitionStartingContentActive,
    bool SettledNormalContentVisible,
    bool SettledRecordingContentVisible);

internal sealed class OutputPathControllerContext
{
    public required TextBox OutputPathTextBox { get; init; }
    public required Func<IntPtr> GetWindowHandle { get; init; }
    public required Func<string?> GetOutputPath { get; init; }
    public required Action<string> SetOutputPath { get; init; }
    public required Action<string> SetStatusText { get; init; }
    public required Func<Task> OpenRecordingsFolderAsync { get; init; }
}

internal sealed class OutputPathController
{
    private readonly OutputPathControllerContext _context;

    public OutputPathController(OutputPathControllerContext context)
    {
        _context = context;
    }

    public void AttachDisplay()
        => _context.OutputPathTextBox.SizeChanged += (_, _) => UpdateDisplay();

    public void UpdateDisplay()
    {
        var path = _context.GetOutputPath();
        if (string.IsNullOrEmpty(path))
        {
            _context.OutputPathTextBox.Text = string.Empty;
            return;
        }

        ToolTipService.SetToolTip(_context.OutputPathTextBox, path);

        var availableWidth = _context.OutputPathTextBox.ActualWidth;
        _context.OutputPathTextBox.Text = OutputPathDisplayTextFormatter.Format(path, availableWidth);
    }

    public bool TryHandlePropertyChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.OutputPath):
                UpdateDisplay();
                return true;

            default:
                return false;
        }
    }

    public async Task BrowseAsync()
    {
        try
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            picker.FileTypeFilter.Add("*");

            // Initialize the picker with the window handle for WinUI 3.
            WinRT.Interop.InitializeWithWindow.Initialize(picker, _context.GetWindowHandle());

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                _context.SetOutputPath(folder.Path);
            }
        }
        catch (Exception ex)
        {
            _context.SetStatusText($"Error selecting folder: {ex.Message}");
        }
    }

    public Task OpenRecordingsFolderIfAvailableAsync()
    {
        var path = _context.GetOutputPath();
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return Task.CompletedTask;
        }

        return _context.OpenRecordingsFolderAsync();
    }
}

internal static class OutputPathDisplayTextFormatter
{
    public static string Format(string path, double availableWidth)
    {
        if (availableWidth <= 0)
        {
            return path;
        }

        // FontSize 12 is about 7px per char, minus internal padding.
        var maxChars = (int)((availableWidth - 20) / 7);
        if (path.Length <= maxChars)
        {
            return path;
        }

        var parts = path.Split('\\', '/');
        if (parts.Length <= 2)
        {
            return path;
        }

        // Progressively truncate: keep root, show as many trailing segments as fit.
        var root = parts[0];
        for (int tailCount = parts.Length - 1; tailCount >= 1; tailCount--)
        {
            var tail = string.Join("\\", parts[^tailCount..]);
            var candidate = $"{root}\\...\\{tail}";
            if (candidate.Length <= maxChars)
            {
                return candidate;
            }
        }

        return $"{root}\\...\\{parts[^1]}";
    }
}
