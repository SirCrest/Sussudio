using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task SettingsShelfLifecycle_LivesInController()
    {
        var fullScreenText = ReadMainWindowFullScreenAdapterSource();
        var mainWindowText = ReadMainWindowCompositionSource();
        var settingsShelfText = ReadMainWindowShellChromeAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Shell/SettingsShelfController.cs").Replace("\r\n", "\n");

        AssertContains(settingsShelfText, "private SettingsShelfController _settingsShelfController = null!;");
        AssertContains(settingsShelfText, "private void InitializeSettingsShelfController()");
        AssertContains(settingsShelfText, "=> _settingsShelfController.Toggle();");
        AssertContains(settingsShelfText, "=> _settingsShelfController.ApplyVisibility(visible);");
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.ShellChrome.Composition.cs")),
            "settings shelf adapter lives in the shell chrome composition partial");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.FullScreen.Composition.cs")),
            "fullscreen adapter folded into the shell chrome composition partial");
        AssertContains(mainWindowText, "InitializeSettingsShelfController();");
        AssertContains(fullScreenText, "ResetSettingsShelfAnimation = _settingsShelfController.ResetAnimationState,");
        AssertDoesNotContain(settingsShelfText, "ResetSettingsShelfAnimationForFullScreen");
        AssertContains(controllerText, "internal sealed class SettingsShelfController");
        AssertContains(controllerText, "private bool _isAnimating;");
        AssertContains(controllerText, "public bool IsAnimating => _isAnimating;");
        AssertContains(controllerText, "public void Toggle()");
        AssertContains(controllerText, "public void ApplyVisibility(bool visible)");
        AssertContains(controllerText, "public bool TryHandlePropertyChanged(string propertyName, bool isSettingsVisible)");
        AssertContains(controllerText, "case nameof(MainViewModel.IsSettingsVisible):");
        AssertContains(controllerText, "ApplyVisibility(isSettingsVisible);");
        AssertContains(controllerText, "_context.SettingsOverlayPanel.UpdateLayout();");
        AssertContains(controllerText, "EnableDependentAnimation = true");
        AssertContains(controllerText, "_context.SettingsOverlayPanel.Visibility = Visibility.Collapsed;");
        AssertDoesNotContain(mainWindowText, "private bool _isSettingsShelfAnimating;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.EventHandlers.cs")),
            "generic MainWindow event-handler partial removed");

        return Task.CompletedTask;
    }

    internal static Task MainWindowTitlePresentation_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var statusStripText = ReadRepoFile("Sussudio/MainWindow.StatusStripPresentation.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Shell/ShellChromeController.cs").Replace("\r\n", "\n");

        AssertContains(statusStripText, "private WindowTitleController _windowTitleController = null!;");
        AssertContains(statusStripText, "private void InitializeWindowTitleController()");
        AssertContains(statusStripText, "private void ApplyWindowTitle()");
        AssertContains(statusStripText, "=> _windowTitleController = new WindowTitleController();");
        AssertContains(statusStripText, "=> Title = _windowTitleController.BuildTitle(ViewModel.IsRecording, ViewModel.RecordingTime);");
        AssertContains(controllerText, "internal sealed class WindowTitleController");
        AssertContains(controllerText, "private const string DefaultTitle = \"Simple Sussudio\";");
        AssertContains(controllerText, "public string BuildTitle(bool isRecording, string recordingTime)");
        AssertContains(controllerText, "internal static string BuildWindowTitleBase()");
        AssertContains(controllerText, "Environment.ProcessPath");
        AssertContains(controllerText, "File.GetLastWriteTime(exePath)");
        AssertContains(controllerText, "internal static string FormatBuildTitle(DateTime buildTime)");
        AssertContains(controllerText, "CultureInfo.InvariantCulture");
        AssertContains(controllerText, "internal static string FormatTitle(string baseTitle, bool isRecording, string recordingTime)");
        AssertContains(controllerText, "=> isRecording ? $\"{baseTitle} - REC {recordingTime}\" : baseTitle;");
        AssertContains(mainWindowText, "InitializeWindowTitleController();");
        AssertContains(mainWindowText, "ApplyWindowTitle();");
        AssertContains(propertyChangedText, "TryHandleStatusStrip = TryHandleStatusStripPropertyChanged,");
        AssertContains(statusStripText, "ApplyWindowTitle);");
        AssertDoesNotContain(mainWindowText, "private static string BuildWindowTitleBase()");
        AssertDoesNotContain(mainWindowText, "private void ApplyWindowTitle()");
        AssertDoesNotContain(mainWindowText, "CultureInfo.InvariantCulture");
        AssertDoesNotContain(statusStripText, "Environment.ProcessPath");
        AssertDoesNotContain(statusStripText, "File.GetLastWriteTime(");
        AssertDoesNotContain(statusStripText, "CultureInfo.InvariantCulture");

        return Task.CompletedTask;
    }

    internal static Task LiveSignalInfoPresentation_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var liveSignalAdapterText = ReadRepoFile("Sussudio/MainWindow.StatusStripPresentation.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var shutdownCleanupControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowShutdownCleanupController.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Shell/LiveSignalInfoController.cs").Replace("\r\n", "\n");

        AssertContains(liveSignalAdapterText, "private LiveSignalInfoController _liveSignalInfoController = null!;");
        AssertContains(liveSignalAdapterText, "private void InitializeLiveSignalInfoController()");
        AssertContains(liveSignalAdapterText, "LiveResolutionTextBlock = LiveResolutionTextBlock,");
        AssertContains(liveSignalAdapterText, "LiveFrameRateTextBlock = LiveFrameRateTextBlock,");
        AssertContains(liveSignalAdapterText, "LivePixelFormatTextBlock = LivePixelFormatTextBlock,");
        AssertContains(liveSignalAdapterText, "=> _liveSignalInfoController.Update(");
        AssertContains(liveSignalAdapterText, "ViewModel.LiveResolution,");
        AssertContains(liveSignalAdapterText, "private void StopLiveSignalInfoTimers()");
        AssertContains(liveSignalAdapterText, "=> _liveSignalInfoController.StopTimers();");
        AssertContains(liveSignalAdapterText, "private bool TryHandleLiveSignalPropertyChanged(string propertyName)");
        AssertContains(liveSignalAdapterText, "=> _liveSignalInfoController.TryHandlePropertyChanged(");
        AssertDoesNotContain(liveSignalAdapterText, "case nameof(MainViewModel.LiveResolution):");
        AssertContains(mainWindowText, "InitializeLiveSignalInfoController();");
        AssertContains(bindingsText, "UpdateLiveSignalInfoVisibility();");
        AssertContains(shutdownCleanupControllerText, "_context.StopTimers();");
        AssertContains(controllerText, "internal sealed class LiveSignalInfoController");
        AssertContains(controllerText, "private DispatcherQueueTimer? _showDebounceTimer;");
        AssertContains(controllerText, "private DispatcherQueueTimer? _hideDebounceTimer;");
        AssertContains(controllerText, "public void Update(string liveResolution, string liveFrameRate, string livePixelFormat)");
        AssertContains(controllerText, "public bool TryHandlePropertyChanged(string propertyName, string liveResolution, string liveFrameRate, string livePixelFormat)");
        AssertContains(controllerText, "case nameof(MainViewModel.LiveResolution):");
        AssertContains(controllerText, "case nameof(MainViewModel.LiveFrameRate):");
        AssertContains(controllerText, "case nameof(MainViewModel.LivePixelFormat):");
        AssertContains(controllerText, "Update(liveResolution, liveFrameRate, livePixelFormat);");
        AssertContains(controllerText, "_context.LiveResolutionTextBlock.Text = liveResolution;");
        AssertContains(controllerText, "_context.LiveFrameRateTextBlock.Text = liveFrameRate;");
        AssertContains(controllerText, "_context.LivePixelFormatTextBlock.Text = livePixelFormat;");
        AssertContains(controllerText, "private bool HasCompleteLiveSignal()");
        AssertContains(controllerText, "private void AnimateIn()");
        AssertContains(controllerText, "private void AnimateOut()");
        AssertDoesNotContain(propertyChangedText, "LiveResolutionTextBlock.Text = ViewModel.LiveResolution;");
        AssertDoesNotContain(propertyChangedText, "LiveFrameRateTextBlock.Text = ViewModel.LiveFrameRate;");
        AssertDoesNotContain(propertyChangedText, "LivePixelFormatTextBlock.Text = ViewModel.LivePixelFormat;");
        AssertDoesNotContain(mainWindowText, "private bool _liveSignalInfoVisible;");
        AssertDoesNotContain(mainWindowText, "private DispatcherQueueTimer? _liveSignalDebounceTimer;");

        return Task.CompletedTask;
    }

    internal static Task StatusStripPresentation_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.StatusStripPresentation.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Shell/StatusStripPresentationController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private StatusStripPresentationController _statusStripPresentationController = null!;");
        AssertContains(adapterText, "private void InitializeStatusStripPresentationController()");
        AssertContains(adapterText, "DiskWarningInfoBar = DiskWarningInfoBar,");
        AssertContains(adapterText, "StatusTextBlock = StatusTextBlock,");
        AssertContains(adapterText, "RecordingTimeTextBlock = RecordingTimeTextBlock,");
        AssertContains(adapterText, "DiskSpaceTextBlock = DiskSpaceTextBlock,");
        AssertContains(adapterText, "RecordingSizeTextBlock = RecordingSizeTextBlock,");
        AssertContains(adapterText, "RecordingBitrateTextBlock = RecordingBitrateTextBlock,");
        AssertContains(adapterText, "private void ApplyInitialStatusStripPresentation()");
        AssertContains(adapterText, "private StatusStripPresentationSnapshot BuildStatusStripPresentationSnapshot()");
        AssertContains(adapterText, "private void UpdateStatusTextPresentation()");
        AssertContains(adapterText, "private void UpdateRecordingTimePresentation()");
        AssertContains(adapterText, "private void UpdateDiskSpacePresentation()");
        AssertContains(adapterText, "private void UpdateRecordingSizePresentation()");
        AssertContains(adapterText, "private void UpdateRecordingBitratePresentation()");
        AssertDoesNotContain(adapterText, "private void UpdateFlashbackBitratePresentation()");
        AssertContains(adapterText, "private void UpdateDiskWarningPresentation()");
        AssertContains(adapterText, "private bool TryHandleStatusStripPropertyChanged(string? propertyName)");
        AssertContains(adapterText, "_statusStripPresentationController.TryHandlePropertyChanged(");
        AssertContains(adapterText, "BuildStatusStripPresentationSnapshot(),");
        AssertContains(adapterText, "ApplyWindowTitle);");
        AssertContains(mainWindowText, "InitializeStatusStripPresentationController();");
        AssertContains(bindingsText, "ApplyInitialStatusStripPresentation();");
        AssertContains(propertyChangedText, "TryHandleStatusStrip = TryHandleStatusStripPropertyChanged,");
        AssertDoesNotContain(flashbackPropertyChangedText, "UpdateBitrate = UpdateFlashbackBitratePresentation,");
        AssertDoesNotContain(flashbackPropertyChangedControllerText, "_context.UpdateBitrate();");
        AssertDoesNotContain(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.FlashbackBitrateInfo):");
        AssertContains(controllerText, "internal readonly record struct StatusStripPresentationSnapshot");
        AssertContains(controllerText, "internal sealed class StatusStripPresentationController");
        AssertContains(controllerText, "public void ApplyInitial(StatusStripPresentationSnapshot snapshot)");
        AssertContains(controllerText, "public bool TryHandlePropertyChanged(");
        AssertContains(controllerText, "case nameof(MainViewModel.StatusText):");
        AssertContains(controllerText, "case nameof(MainViewModel.RecordingTime):");
        AssertContains(controllerText, "case nameof(MainViewModel.DiskSpaceInfo):");
        AssertContains(controllerText, "case nameof(MainViewModel.RecordingSizeInfo):");
        AssertContains(controllerText, "case nameof(MainViewModel.RecordingBitrateInfo):");
        AssertContains(controllerText, "case nameof(MainViewModel.FlashbackBitrateInfo):");
        AssertContains(controllerText, "UpdateFlashbackBitrate(snapshot.FlashbackBitrateInfo, snapshot.IsRecording, snapshot.IsFlashbackEnabled);");
        AssertContains(controllerText, "case nameof(MainViewModel.IsDiskWarningActive):");
        AssertContains(controllerText, "if (snapshot.IsRecording)");
        AssertContains(controllerText, "applyWindowTitle();");
        AssertContains(controllerText, "_context.StatusTextBlock.Text = statusText;");
        AssertContains(controllerText, "_context.RecordingTimeTextBlock.Text = recordingTime;");
        AssertContains(controllerText, "_context.DiskSpaceTextBlock.Text = diskSpaceInfo;");
        AssertContains(controllerText, "_context.RecordingSizeTextBlock.Text = recordingSizeInfo;");
        AssertContains(controllerText, "_context.RecordingBitrateTextBlock.Text = recordingBitrateInfo;");
        AssertContains(controllerText, "if (!isRecording && isFlashbackEnabled)");
        AssertContains(controllerText, "_context.RecordingBitrateTextBlock.Text = flashbackBitrateInfo;");
        AssertContains(controllerText, "_context.DiskWarningInfoBar.IsOpen = isDiskWarningActive;");
        AssertDoesNotContain(bindingsText, "DiskSpaceTextBlock.Text = ViewModel.DiskSpaceInfo;");
        AssertDoesNotContain(bindingsText, "RecordingSizeTextBlock.Text = ViewModel.RecordingSizeInfo;");
        AssertDoesNotContain(bindingsText, "RecordingBitrateTextBlock.Text = ViewModel.RecordingBitrateInfo;");
        AssertDoesNotContain(bindingsText, "LiveResolutionTextBlock.Text = ViewModel.LiveResolution;");
        AssertDoesNotContain(bindingsText, "LiveFrameRateTextBlock.Text = ViewModel.LiveFrameRate;");
        AssertDoesNotContain(bindingsText, "LivePixelFormatTextBlock.Text = ViewModel.LivePixelFormat;");
        AssertDoesNotContain(propertyChangedText, "StatusTextBlock.Text = ViewModel.StatusText;");
        AssertDoesNotContain(propertyChangedText, "RecordingTimeTextBlock.Text = ViewModel.RecordingTime;");
        AssertDoesNotContain(propertyChangedText, "DiskSpaceTextBlock.Text = ViewModel.DiskSpaceInfo;");
        AssertDoesNotContain(propertyChangedText, "RecordingSizeTextBlock.Text = ViewModel.RecordingSizeInfo;");
        AssertDoesNotContain(propertyChangedText, "RecordingBitrateTextBlock.Text = ViewModel.RecordingBitrateInfo;");
        AssertDoesNotContain(propertyChangedText, "DiskWarningInfoBar.IsOpen = ViewModel.IsDiskWarningActive;");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.StatusText):");
        AssertDoesNotContain(propertyChangedText, "UpdateStatusTextPresentation();");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.RecordingTime):");
        AssertDoesNotContain(propertyChangedText, "UpdateRecordingTimePresentation();");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.DiskSpaceInfo):");
        AssertDoesNotContain(propertyChangedText, "UpdateDiskSpacePresentation();");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.RecordingSizeInfo):");
        AssertDoesNotContain(propertyChangedText, "UpdateRecordingSizePresentation();");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.RecordingBitrateInfo):");
        AssertDoesNotContain(propertyChangedText, "UpdateRecordingBitratePresentation();");
        AssertDoesNotContain(propertyChangedText, "case nameof(MainViewModel.IsDiskWarningActive):");
        AssertDoesNotContain(propertyChangedText, "UpdateDiskWarningPresentation();");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.StatusText):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.RecordingTime):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.DiskSpaceInfo):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.RecordingSizeInfo):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.RecordingBitrateInfo):");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.IsDiskWarningActive):");
        AssertDoesNotContain(adapterText, "if (ViewModel.IsRecording)");
        AssertDoesNotContain(flashbackPropertyChangedText, "RecordingBitrateTextBlock.Text = ViewModel.FlashbackBitrateInfo;");

        return Task.CompletedTask;
    }
}
