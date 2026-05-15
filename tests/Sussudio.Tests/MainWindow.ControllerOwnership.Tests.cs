using System.Threading.Tasks;

static partial class Program
{
    private static Task MainWindowPropertyChangedRouting_LivesInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var previewText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedPreview.cs").Replace("\r\n", "\n");
        var recordingText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedRecording.cs").Replace("\r\n", "\n");
        var outputText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedOutput.cs").Replace("\r\n", "\n");
        var captureOptionText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedCaptureOptions.cs").Replace("\r\n", "\n");
        var audioText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedAudio.cs").Replace("\r\n", "\n");
        var shellText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedShell.cs").Replace("\r\n", "\n");
        var liveSignalText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedLiveSignal.cs").Replace("\r\n", "\n");
        var flashbackText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedFlashback.cs").Replace("\r\n", "\n");

        AssertContains(rootText, "var propertyName = e.PropertyName ?? string.Empty;");
        AssertContains(rootText, "TryHandleCaptureSelectionPropertyChanged(propertyName)");
        AssertContains(rootText, "TryHandleStatusStripPropertyChanged(propertyName)");
        AssertContains(rootText, "await TryHandlePreviewPropertyChangedAsync(propertyName)");
        AssertContains(rootText, "TryHandleRecordingPropertyChanged(propertyName)");
        AssertContains(rootText, "TryHandleOutputPropertyChanged(propertyName)");
        AssertContains(rootText, "TryHandleCaptureOptionPropertyChanged(propertyName)");
        AssertContains(rootText, "TryHandleAudioPropertyChanged(propertyName)");
        AssertContains(rootText, "TryHandleShellPropertyChanged(propertyName)");
        AssertContains(rootText, "TryHandleLiveSignalPropertyChanged(propertyName)");
        AssertContains(rootText, "TryHandleFlashbackPropertyChanged(propertyName)");

        AssertOccursBefore(rootText, "TryHandleCaptureSelectionPropertyChanged(propertyName)", "TryHandleStatusStripPropertyChanged(propertyName)");
        AssertOccursBefore(rootText, "TryHandleStatusStripPropertyChanged(propertyName)", "await TryHandlePreviewPropertyChangedAsync(propertyName)");
        AssertOccursBefore(rootText, "await TryHandlePreviewPropertyChangedAsync(propertyName)", "TryHandleRecordingPropertyChanged(propertyName)");
        AssertOccursBefore(rootText, "TryHandleRecordingPropertyChanged(propertyName)", "TryHandleOutputPropertyChanged(propertyName)");
        AssertOccursBefore(rootText, "TryHandleOutputPropertyChanged(propertyName)", "TryHandleCaptureOptionPropertyChanged(propertyName)");
        AssertOccursBefore(rootText, "TryHandleCaptureOptionPropertyChanged(propertyName)", "TryHandleAudioPropertyChanged(propertyName)");
        AssertOccursBefore(rootText, "TryHandleAudioPropertyChanged(propertyName)", "TryHandleShellPropertyChanged(propertyName)");
        AssertOccursBefore(rootText, "TryHandleShellPropertyChanged(propertyName)", "TryHandleLiveSignalPropertyChanged(propertyName)");
        AssertOccursBefore(rootText, "TryHandleLiveSignalPropertyChanged(propertyName)", "TryHandleFlashbackPropertyChanged(propertyName)");

        AssertDoesNotContain(rootText, "case nameof(MainViewModel.");
        AssertDoesNotContain(rootText, "HandlePreviewingChangedAsync();");
        AssertDoesNotContain(rootText, "HandleRecordingChanged();");
        AssertDoesNotContain(rootText, "HandleFlashbackTimelineVisibleChanged();");
        AssertDoesNotContain(rootText, "HandleAudioPreviewActiveChanged();");

        AssertContains(previewText, "private async Task<bool> TryHandlePreviewPropertyChangedAsync(string propertyName)");
        AssertContains(previewText, "case nameof(MainViewModel.IsPreviewing):");
        AssertContains(previewText, "await HandlePreviewingChangedAsync();");
        AssertContains(recordingText, "private bool TryHandleRecordingPropertyChanged(string propertyName)");
        AssertContains(recordingText, "case nameof(MainViewModel.IsRecording):");
        AssertContains(outputText, "private bool TryHandleOutputPropertyChanged(string propertyName)");
        AssertContains(outputText, "case nameof(MainViewModel.OutputPath):");
        AssertContains(captureOptionText, "private bool TryHandleCaptureOptionPropertyChanged(string propertyName)");
        AssertContains(captureOptionText, "case nameof(MainViewModel.IsHdrEnabled):");
        AssertContains(audioText, "private bool TryHandleAudioPropertyChanged(string propertyName)");
        AssertContains(audioText, "case nameof(MainViewModel.IsAudioPreviewActive):");
        AssertContains(shellText, "private bool TryHandleShellPropertyChanged(string propertyName)");
        AssertContains(shellText, "case nameof(MainViewModel.IsStatsVisible):");
        AssertContains(liveSignalText, "private bool TryHandleLiveSignalPropertyChanged(string propertyName)");
        AssertContains(liveSignalText, "case nameof(MainViewModel.LiveResolution):");
        AssertContains(flashbackText, "private bool TryHandleFlashbackPropertyChanged(string propertyName)");
        AssertContains(flashbackText, "case nameof(MainViewModel.IsFlashbackTimelineVisible):");

        return Task.CompletedTask;
    }
}
