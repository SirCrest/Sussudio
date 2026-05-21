using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task RecordingButtonChrome_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var recordingPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedRecording.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Recording/Button/RecordingButtonChromeController.cs").Replace("\r\n", "\n");
        var recordingPresentationText = ReadRepoFile("Sussudio/Controllers/Recording/RecordingStatePresentationController.cs").Replace("\r\n", "\n");

        AssertContains(recordingPropertyChangedText, "private RecordingButtonChromeController _recordingButtonChromeController = null!;");
        AssertContains(recordingPropertyChangedText, "private void InitializeRecordingButtonChromeController()");
        AssertContains(recordingPropertyChangedText, "RecordingGlowBorder = RecordingGlowBorder,");
        AssertContains(recordingPropertyChangedText, "RecordingGlowPulseStoryboard = RecordingGlowPulseStoryboard,");
        AssertContains(recordingPropertyChangedText, "RecPulseStoryboard = RecPulseStoryboard,");
        AssertContains(recordingPropertyChangedText, "RecordButton = RecordButton,");
        AssertContains(recordingPropertyChangedText, "RecordButtonNormalContent = RecordButtonNormalContent,");
        AssertContains(recordingPropertyChangedText, "RecordButtonStartingContent = RecordButtonStartingContent,");
        AssertContains(recordingPropertyChangedText, "RecordButtonRecordingContent = RecordButtonRecordingContent,");
        AssertContains(mainWindowText, "InitializeRecordingButtonChromeController();");
        AssertContains(propertyChangedText, "TryHandleRecording = TryHandleRecordingPropertyChanged,");
        AssertContains(recordingPropertyChangedText, "=> _recordingStatePresentationController.TryHandlePropertyChanged(propertyName);");
        AssertContains(recordingPropertyChangedText, "RecordingButtonChrome = _recordingButtonChromeController,");
        AssertContains(recordingPresentationText, "case nameof(MainViewModel.IsRecording):");
        AssertContains(recordingPresentationText, "HandleRecordingChanged();");
        AssertContains(recordingPresentationText, "public required RecordingButtonChromeController RecordingButtonChrome { get; init; }");
        AssertContains(recordingPresentationText, "_context.RecordingButtonChrome.ApplyRecordingGlow(isRecording);");
        AssertContains(recordingPresentationText, "_context.RecordingButtonChrome.ApplyRecordingButtonState(isRecording);");
        AssertContains(recordingPresentationText, "_context.RecordingButtonChrome.ApplyRecordingPulse(isRecording);");
        AssertContains(recordingPresentationText, "_context.RecordingButtonChrome.ApplyTransitioningState(viewModel.IsRecording, state);");
        AssertContains(recordingPresentationText, "_context.RecordingButtonChrome.ApplyFfmpegMissingState(state);");
        AssertContains(controllerText, "internal sealed class RecordingButtonChromeController");
        AssertContains(controllerText, "private const double CollapsedRecordButtonWidth = 36;");
        AssertContains(controllerText, "public void ApplyRecordingGlow(bool isRecording)");
        AssertContains(controllerText, "_context.RecordingGlowBorder.Opacity = 1.0;");
        AssertContains(controllerText, "_context.RecordingGlowPulseStoryboard.Begin();");
        AssertContains(controllerText, "_context.RecordingGlowPulseStoryboard.Stop();");
        AssertContains(controllerText, "_context.RecordingGlowBorder.Opacity = 0;");
        AssertContains(controllerText, "public void ApplyRecordingButtonState(bool isRecording)");
        AssertContains(controllerText, "_context.RecordButtonStartingContent.IsActive = false;");
        AssertContains(controllerText, "_context.RecordButtonStartingContent.Visibility = Visibility.Collapsed;");
        AssertContains(controllerText, "_context.RecordButtonNormalContent.Visibility = Visibility.Collapsed;");
        AssertContains(controllerText, "_context.RecordButtonRecordingContent.Visibility = Visibility.Visible;");
        AssertContains(controllerText, "_context.RecordButton.Padding = new Thickness(12, 0, 12, 0);");
        AssertContains(controllerText, "_context.RecordButton.Width = double.NaN;");
        AssertContains(controllerText, "_context.RecordButton.UpdateLayout();");
        AssertContains(controllerText, "var targetWidth = _context.RecordButton.ActualWidth;");
        AssertContains(controllerText, "_context.RecordButton.Width = CollapsedRecordButtonWidth;");
        AssertContains(controllerText, "AnimateWidth(CollapsedRecordButtonWidth, targetWidth, null);");
        AssertContains(controllerText, "var currentWidth = _context.RecordButton.ActualWidth;");
        AssertContains(controllerText, "AnimateWidth(currentWidth, CollapsedRecordButtonWidth, () =>");
        AssertContains(controllerText, "_context.RecordButtonRecordingContent.Visibility = Visibility.Collapsed;");
        AssertContains(controllerText, "_context.RecordButtonNormalContent.Visibility = Visibility.Visible;");
        AssertContains(controllerText, "_context.RecordButton.Padding = new Thickness(0);");
        AssertContains(controllerText, "public void ApplyRecordingPulse(bool isRecording)");
        AssertContains(controllerText, "_context.RecPulseStoryboard.Begin();");
        AssertContains(controllerText, "_context.RecPulseStoryboard.Stop();");
        AssertContains(controllerText, "public void ApplyTransitioningState(bool isRecording, RecordingStatePresentationState state)");
        AssertContains(controllerText, "_context.RecordButton.IsEnabled = state.TransitionRecordButtonEnabled;");
        AssertContains(controllerText, "_context.RecordButton.Width = _context.RecordButton.ActualWidth;");
        AssertContains(controllerText, "_context.RecordButtonStartingContent.IsActive = state.TransitionStartingContentActive;");
        AssertContains(controllerText, "_context.RecordButtonStartingContent.Visibility = Visibility.Visible;");
        AssertContains(controllerText, "public void ApplyFfmpegMissingState(RecordingStatePresentationState state)");
        AssertContains(controllerText, "_context.RecordButton.IsEnabled = state.FfmpegRecordButtonEnabled;");
        AssertContains(controllerText, "private void AnimateWidth(double from, double to, Action? onCompleted = null)");
        AssertContains(controllerText, "Duration = new Duration(TimeSpan.FromMilliseconds(200)),");
        AssertContains(controllerText, "EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },");
        AssertContains(controllerText, "EnableDependentAnimation = true");
        AssertContains(controllerText, "Storyboard.SetTarget(anim, _context.RecordButton);");
        AssertContains(controllerText, "Storyboard.SetTargetProperty(anim, \"Width\");");
        AssertContains(controllerText, "_context.RecordButton.Width = to == CollapsedRecordButtonWidth ? CollapsedRecordButtonWidth : double.NaN;");
        AssertOccursBefore(controllerText, "_context.RecordButton.UpdateLayout();", "var targetWidth = _context.RecordButton.ActualWidth;");
        AssertDoesNotContain(recordingPresentationText, "public required Action<double, double, Action?> AnimateRecordButtonWidth { get; init; }");
        AssertDoesNotContain(recordingPresentationText, "_context.AnimateRecordButtonWidth(");
        AssertDoesNotContain(recordingPresentationText, "_context.RecordButton.");
        AssertDoesNotContain(recordingPresentationText, "_context.RecordButtonStartingContent.");
        AssertDoesNotContain(recordingPresentationText, "_context.RecordingGlowPulseStoryboard.");
        AssertDoesNotContain(recordingPresentationText, "_context.RecPulseStoryboard.");
        AssertDoesNotContain(recordingPropertyChangedText, "Storyboard.SetTarget(anim, RecordButton);");

        return Task.CompletedTask;
    }

    internal static Task RecordingStatePresentation_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedRecording.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Recording/RecordingStatePresentationController.cs").Replace("\r\n", "\n");
        var policyText = ReadRepoFile("Sussudio/Controllers/Recording/RecordingStatePresentationPolicy.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private RecordingStatePresentationController _recordingStatePresentationController = null!;");
        AssertContains(adapterText, "private void InitializeRecordingStatePresentationController()");
        AssertContains(adapterText, "RecordingButtonChrome = _recordingButtonChromeController,");
        AssertContains(adapterText, "AudioRecordToggle = AudioRecordToggle,");
        AssertContains(adapterText, "AnalogAudioGainSlider = AnalogAudioGainSlider,");
        AssertContains(adapterText, "ApplyWindowTitle = ApplyWindowTitle,");
        AssertContains(adapterText, "=> _recordingStatePresentationController.TryHandlePropertyChanged(propertyName);");
        AssertContains(adapterText, "private void ApplyInitialRecordingStatePresentation()");
        AssertContains(adapterText, "=> _recordingStatePresentationController.HandleFfmpegMissingChanged();");
        AssertContains(bindingsText, "ApplyInitialRecordingStatePresentation();");
        AssertContains(mainWindowText, "InitializeRecordingStatePresentationController();");
        AssertContains(controllerText, "internal sealed class RecordingStatePresentationController");
        AssertContains(controllerText, "public bool TryHandlePropertyChanged(string propertyName)");
        AssertContains(controllerText, "case nameof(MainViewModel.IsRecording):");
        AssertContains(controllerText, "case nameof(MainViewModel.IsRecordingTransitioning):");
        AssertContains(controllerText, "case nameof(MainViewModel.IsFfmpegMissing):");
        AssertContains(controllerText, "public void HandleRecordingChanged()");
        AssertContains(controllerText, "_context.RecordingButtonChrome.ApplyRecordingGlow(isRecording);");
        AssertContains(controllerText, "_context.ResetAudioMeterVisuals();");
        AssertContains(controllerText, "_context.RecordingButtonChrome.ApplyRecordingButtonState(isRecording);");
        AssertContains(controllerText, "RecordingStatePresentationPolicy.Build(new RecordingStatePresentationInput(");
        AssertContains(controllerText, "_context.AudioInputComboBox.IsEnabled = state.AudioInputComboBoxEnabled;");
        AssertContains(controllerText, "_context.AnalogAudioGainSlider.IsEnabled = state.AnalogAudioGainSliderEnabled;");
        AssertContains(controllerText, "_context.RecordingButtonChrome.ApplyRecordingPulse(isRecording);");
        AssertContains(controllerText, "_context.ApplyWindowTitle();");
        AssertContains(controllerText, "public void HandleRecordingTransitioningChanged()");
        AssertContains(controllerText, "_context.RecordingButtonChrome.ApplyTransitioningState(viewModel.IsRecording, state);");
        AssertContains(controllerText, "public void HandleFfmpegMissingChanged()");
        AssertContains(controllerText, "_context.RecordingButtonChrome.ApplyFfmpegMissingState(state);");
        AssertContains(policyText, "internal static class RecordingStatePresentationPolicy");
        AssertContains(policyText, "internal static RecordingStatePresentationState Build(RecordingStatePresentationInput input)");
        AssertContains(policyText, "internal readonly record struct RecordingStatePresentationInput(");
        AssertContains(policyText, "internal readonly record struct RecordingStatePresentationState(");
        AssertContains(policyText, "DeviceAudioMode.Analog");
        AssertContains(policyText, "StringComparison.OrdinalIgnoreCase");
        AssertDoesNotContain(policyText, "Microsoft.UI.Xaml");
        AssertDoesNotContain(policyText, "Storyboard");
        AssertDoesNotContain(adapterText, "RecordingGlowPulseStoryboard.Begin();");
        AssertDoesNotContain(adapterText, "RecordButtonStartingContent.IsActive = false;");
        AssertDoesNotContain(adapterText, "AnimateRecordButtonWidth = AnimateRecordButtonWidth,");
        AssertDoesNotContain(adapterText, "AudioInputComboBox.IsEnabled = ViewModel.IsCustomAudioInputEnabled");
        AssertDoesNotContain(adapterText, "RecordButton.IsEnabled = !ViewModel.IsFfmpegMissing");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.");
        AssertDoesNotContain(adapterText, "=> _recordingStatePresentationController.HandleRecordingChanged();");
        AssertDoesNotContain(bindingsText, "RecordButton.IsEnabled = !ViewModel.IsFfmpegMissing");
        AssertDoesNotContain(controllerText, "_context.RecordButton.");
        AssertDoesNotContain(controllerText, "_context.RecordButtonStartingContent.");
        AssertDoesNotContain(controllerText, "_context.RecordingGlowPulseStoryboard.");
        AssertDoesNotContain(controllerText, "string.Equals(viewModel.SelectedDeviceAudioMode, DeviceAudioMode.Analog");
        AssertDoesNotContain(controllerText, "_context.ViewModel.IsFfmpegMissing &&");

        return Task.CompletedTask;
    }

    internal static Task RecordingStatePresentationPolicy_PreservesLockoutRules()
    {
        var policyType = RequireType("Sussudio.Controllers.RecordingStatePresentationPolicy");
        var inputType = RequireType("Sussudio.Controllers.RecordingStatePresentationInput");
        var build = policyType.GetMethod("Build", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RecordingStatePresentationPolicy.Build was not found.");
        var constructor = inputType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Single(ctor => ctor.GetParameters().Length == 7);

        object Build(
            bool isRecording = false,
            bool isRecordingTransitioning = false,
            bool isFfmpegMissing = false,
            bool isCustomAudioInputEnabled = true,
            bool isMicrophoneEnabled = true,
            bool isDeviceAudioControlSupported = true,
            string? selectedDeviceAudioMode = "Analog")
        {
            var input = constructor.Invoke(new object?[]
            {
                isRecording,
                isRecordingTransitioning,
                isFfmpegMissing,
                isCustomAudioInputEnabled,
                isMicrophoneEnabled,
                isDeviceAudioControlSupported,
                selectedDeviceAudioMode
            });

            return build.Invoke(null, new[] { input })
                ?? throw new InvalidOperationException("RecordingStatePresentationPolicy.Build returned null.");
        }

        var idleAnalog = Build();
        AssertEqual(true, GetBoolProperty(idleAnalog, "AudioRecordToggleEnabled"), "idle enables audio record toggle");
        AssertEqual(true, GetBoolProperty(idleAnalog, "CustomAudioToggleEnabled"), "idle enables custom audio toggle");
        AssertEqual(true, GetBoolProperty(idleAnalog, "MicrophoneToggleEnabled"), "idle enables microphone toggle");
        AssertEqual(true, GetBoolProperty(idleAnalog, "AudioInputComboBoxEnabled"), "custom audio enables input combo while idle");
        AssertEqual(true, GetBoolProperty(idleAnalog, "MicrophoneComboBoxEnabled"), "microphone enables combo while idle");
        AssertEqual(true, GetBoolProperty(idleAnalog, "DeviceAudioModeToggleEnabled"), "device audio controls enable mode toggle while idle");
        AssertEqual(true, GetBoolProperty(idleAnalog, "AnalogAudioGainSliderEnabled"), "analog device audio enables gain while idle");
        AssertEqual(true, GetBoolProperty(idleAnalog, "TransitionRecordButtonEnabled"), "idle transition state enables record button");
        AssertEqual(true, GetBoolProperty(idleAnalog, "FfmpegRecordButtonEnabled"), "available FFmpeg enables record button");
        AssertEqual(false, GetBoolProperty(idleAnalog, "TransitionStartingContentActive"), "idle transition hides starting content");
        AssertEqual(true, GetBoolProperty(idleAnalog, "SettledNormalContentVisible"), "idle settled content shows normal record button");
        AssertEqual(false, GetBoolProperty(idleAnalog, "SettledRecordingContentVisible"), "idle settled content hides recording button");

        var recording = Build(isRecording: true);
        AssertEqual(false, GetBoolProperty(recording, "AudioRecordToggleEnabled"), "recording locks audio record toggle");
        AssertEqual(false, GetBoolProperty(recording, "CustomAudioToggleEnabled"), "recording locks custom audio toggle");
        AssertEqual(false, GetBoolProperty(recording, "MicrophoneToggleEnabled"), "recording locks microphone toggle");
        AssertEqual(false, GetBoolProperty(recording, "AudioInputComboBoxEnabled"), "recording locks audio input combo");
        AssertEqual(false, GetBoolProperty(recording, "MicrophoneComboBoxEnabled"), "recording locks microphone combo");
        AssertEqual(false, GetBoolProperty(recording, "DeviceAudioModeToggleEnabled"), "recording locks device audio mode");
        AssertEqual(false, GetBoolProperty(recording, "AnalogAudioGainSliderEnabled"), "recording locks analog gain");
        AssertEqual(false, GetBoolProperty(recording, "SettledNormalContentVisible"), "recording hides normal content");
        AssertEqual(true, GetBoolProperty(recording, "SettledRecordingContentVisible"), "recording shows recording content");

        var unsupportedAnalog = Build(isDeviceAudioControlSupported: false);
        AssertEqual(false, GetBoolProperty(unsupportedAnalog, "DeviceAudioModeToggleEnabled"), "unsupported device audio disables mode");
        AssertEqual(false, GetBoolProperty(unsupportedAnalog, "AnalogAudioGainSliderEnabled"), "unsupported device audio disables gain");

        var hdmiMode = Build(selectedDeviceAudioMode: "HDMI");
        AssertEqual(false, GetBoolProperty(hdmiMode, "AnalogAudioGainSliderEnabled"), "non-analog device audio disables gain");

        var transition = Build(isRecordingTransitioning: true);
        AssertEqual(false, GetBoolProperty(transition, "TransitionRecordButtonEnabled"), "transition disables record button through transition handler");
        AssertEqual(false, GetBoolProperty(transition, "FfmpegRecordButtonEnabled"), "transition disables record button through FFmpeg handler");
        AssertEqual(true, GetBoolProperty(transition, "TransitionStartingContentActive"), "transition activates starting content");

        var ffmpegMissing = Build(isFfmpegMissing: true);
        AssertEqual(true, GetBoolProperty(ffmpegMissing, "TransitionRecordButtonEnabled"), "FFmpeg missing does not affect transition handler enablement");
        AssertEqual(false, GetBoolProperty(ffmpegMissing, "FfmpegRecordButtonEnabled"), "FFmpeg missing disables record button through FFmpeg handler");

        var inactiveInputs = Build(isCustomAudioInputEnabled: false, isMicrophoneEnabled: false);
        AssertEqual(false, GetBoolProperty(inactiveInputs, "AudioInputComboBoxEnabled"), "custom audio disabled locks input combo");
        AssertEqual(false, GetBoolProperty(inactiveInputs, "MicrophoneComboBoxEnabled"), "microphone disabled locks microphone combo");

        return Task.CompletedTask;
    }
}
