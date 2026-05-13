using System.Reflection;
using System.Text.RegularExpressions;

static partial class Program
{
    private static Task MainWindowAutomationIds_CoverAgentCriticalSurface()
    {
        var xaml = ReadRepoFile("Sussudio/MainWindow.xaml").Replace("\r\n", "\n");
        var requiredIds = new[]
        {
            "PreviewBorder",
            "PreviewPlayerElement",
            "PreviewImage",
            "PreviewLoadingOverlay",
            "NoDevicePlaceholder",
            "DiskWarningInfoBar",
            "SettingsOverlayPanel",
            "DeviceComboBox",
            "ApplyDeviceButton",
            "RefreshButton",
            "DeviceAudioModeComboBox",
            "AnalogAudioGainSlider",
            "AudioInputComboBox",
            "MicrophoneComboBox",
            "VideoFormatComboBox",
            "ResolutionComboBox",
            "FrameRateComboBox",
            "FormatComboBox",
            "QualityComboBox",
            "PresetComboBox",
            "CustomBitrateNumberBox",
            "OutputPathTextBox",
            "BrowseButton",
            "ShowAllCaptureOptionsToggle",
            "FlashbackEnabledToggle",
            "FlashbackBufferDurationCombo",
            "FlashbackApplyButton",
            "FlashbackGpuDecodeToggle",
            "FlashbackTimelinePanel",
            "FlashbackScrubArea",
            "FlashbackInButton",
            "FlashbackOutButton",
            "FlashbackClearButton",
            "FlashbackPlayPauseButton",
            "FlashbackGoLiveButton",
            "FlashbackExportButton",
            "FlashbackSaveLast5mButton",
            "FlashbackExportProgressBar",
            "ControlBarBorder",
            "SettingsToggleButton",
            "OpenRecordingsButton",
            "ScreenshotButton",
            "RecordButton",
            "PreviewButton",
            "HdrToggle",
            "AudioRecordToggle",
            "TrueHdrPreviewToggle",
            "AudioPreviewToggle",
            "StatsToggle",
            "FrameTimeOverlayToggle",
            "FlashbackToggle",
            "FullScreenButton",
            "FullScreenControlsOverlay",
            "SplashOverlay",
            "StatsDockPanel",
            "Stats_SessionStateValue",
            "Stats_SummaryCaptureValue",
            "Stats_SummaryPreviewValue",
            "Stats_SummaryRendererFpsValue",
            "Stats_SummaryVisualFpsValue",
            "Stats_SummaryLatencyValue",
            "Stats_SourceFormatValue",
            "Stats_PreviewFpsValue",
            "Stats_PipelineLatencyValue",
            "FrameTimeOverlay",
            "FrameTime_SourceValue",
            "FrameTime_VisualValue",
            "FrameTime_PreviewValue",
            "FrameTime_LatencyValue",
            "FrameTime_StatusValue",
            "StatusTextBlock",
            "RecordingTimeTextBlock",
            "LiveResolutionTextBlock",
            "LiveFrameRateTextBlock",
            "LivePixelFormatTextBlock",
            "PreviewVolumeSlider",
            "MicVolumeSlider"
        };

        var matches = Regex.Matches(
                xaml,
                "AutomationProperties\\.AutomationId=\"(?<id>[^\"]+)\"",
                RegexOptions.CultureInvariant)
            .Select(match => match.Groups["id"].Value)
            .ToArray();

        foreach (var id in requiredIds)
        {
            AssertEqual(1, matches.Count(candidate => string.Equals(candidate, id, StringComparison.Ordinal)), id);
        }

        var duplicates = matches
            .GroupBy(id => id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key} x{group.Count()}")
            .ToArray();
        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException(
                $"MainWindow automation IDs must be unique. Duplicates: {string.Join(", ", duplicates)}");
        }

        return Task.CompletedTask;
    }

    private static Task MainWindowFullScreenAutomation_AwaitsTransitionTask()
    {
        var fullScreenSource = ReadRepoFile("Sussudio/MainWindow.FullScreen.cs")
            .Replace("\r\n", "\n");
        var fullScreenControllerSource = ReadRepoFile("Sussudio/Controllers/FullScreenController.cs")
            .Replace("\r\n", "\n");
        var windowManagementSource = ReadRepoFile("Sussudio/MainWindow.WindowManagement.cs")
            .Replace("\r\n", "\n");
        var dispatchingSource = ReadRepoFile("Sussudio/MainWindow.Dispatching.cs")
            .Replace("\r\n", "\n");

        AssertContains(fullScreenSource, "public Task SetFullScreenEnabledAsync(bool enabled, CancellationToken cancellationToken = default)\n        => InvokeOnUiThreadAsync(\n            () => _fullScreenController.SetEnabledAsync(enabled),");
        AssertContains(fullScreenSource, "private Task EnterFullScreenAsync()\n        => _fullScreenController.EnterAsync();");
        AssertContains(fullScreenSource, "private Task ExitFullScreenAsync()\n        => _fullScreenController.ExitAsync();");
        AssertContains(fullScreenControllerSource, "internal sealed class FullScreenController");
        AssertContains(fullScreenControllerSource, "public async Task EnterAsync()");
        AssertContains(fullScreenControllerSource, "public async Task ExitAsync()");
        AssertContains(fullScreenControllerSource, "await AnimateFullScreenRectAsync(");
        AssertContains(fullScreenControllerSource, "private Task AnimateFullScreenRectAsync(");
        AssertContains(fullScreenControllerSource, "return completion.Task;");
        AssertDoesNotContain(fullScreenSource, "private async void EnterFullScreen");
        AssertDoesNotContain(fullScreenSource, "private async void ExitFullScreen");
        AssertDoesNotContain(fullScreenControllerSource, "async void");
        AssertContains(dispatchingSource, "private Task InvokeOnUiThreadAsync(Func<Task> action, CancellationToken cancellationToken = default)");
        AssertContains(dispatchingSource, "await action().ConfigureAwait(true);");
        AssertDoesNotContain(dispatchingSource, "registration.Dispose();\n                registration = default;\n\n                if (cancellationToken.IsCancellationRequested)");
        AssertDoesNotContain(windowManagementSource, "private Task InvokeOnUiThreadAsync(Func<Task> action, CancellationToken cancellationToken = default)");
        return Task.CompletedTask;
    }

    private static Task MainWindowWindowAutomationCommands_LiveInController()
    {
        var mainWindowSource = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var windowManagementSource = ReadRepoFile("Sussudio/MainWindow.WindowManagement.cs")
            .Replace("\r\n", "\n");
        var dispatchingSource = ReadRepoFile("Sussudio/MainWindow.Dispatching.cs")
            .Replace("\r\n", "\n");
        var adapterSource = ReadRepoFile("Sussudio/MainWindow.WindowAutomation.cs")
            .Replace("\r\n", "\n");
        var controllerSource = ReadRepoFile("Sussudio/Controllers/WindowAutomationController.cs")
            .Replace("\r\n", "\n");

        AssertContains(adapterSource, "private WindowAutomationController _windowAutomationController = null!;");
        AssertContains(adapterSource, "private void InitializeWindowAutomationController()");
        AssertContains(adapterSource, "GetAppWindow = GetAppWindow,");
        AssertContains(adapterSource, "GetWindowHandle = () => _hwnd,");
        AssertContains(adapterSource, "InvokeOnUiThreadAsync = InvokeOnUiThreadAsync");
        AssertContains(dispatchingSource, "private Task InvokeOnUiThreadAsync(Action action, CancellationToken cancellationToken = default)");
        AssertContains(adapterSource, "=> _windowAutomationController.MinimizeAsync(cancellationToken);");
        AssertContains(adapterSource, "=> _windowAutomationController.OpenRecordingsFolderAsync(cancellationToken);");
        AssertContains(adapterSource, "=> _windowAutomationController.SnapToRegionAsync(region, cancellationToken);");
        AssertContains(mainWindowSource, "InitializeWindowAutomationController();");
        AssertContains(controllerSource, "internal sealed class WindowAutomationController");
        AssertContains(controllerSource, "public Task MoveToAsync(int x, int y, CancellationToken cancellationToken = default)");
        AssertContains(controllerSource, "public Task ResizeToAsync(int width, int height, CancellationToken cancellationToken = default)");
        AssertContains(controllerSource, "public Task SnapToRegionAsync(AutomationWindowAction region, CancellationToken cancellationToken = default)");
        AssertContains(controllerSource, "DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary)");
        AssertContains(controllerSource, "Process.Start(\"explorer.exe\", path);");
        AssertContains(windowManagementSource, "public Task CloseAsync(CancellationToken cancellationToken = default)");
        AssertDoesNotContain(windowManagementSource, "public Task MinimizeAsync(");
        AssertDoesNotContain(windowManagementSource, "public Task OpenRecordingsFolderAsync(");
        AssertDoesNotContain(windowManagementSource, "public Task SnapToRegionAsync(");
        return Task.CompletedTask;
    }

    private static Task MainWindowUiDispatching_LivesInDispatchingPartial()
    {
        var dispatchingSource = ReadRepoFile("Sussudio/MainWindow.Dispatching.cs")
            .Replace("\r\n", "\n");
        var windowManagementSource = ReadRepoFile("Sussudio/MainWindow.WindowManagement.cs")
            .Replace("\r\n", "\n");
        var eventHandlersSource = ReadRepoFile("Sussudio/MainWindow.EventHandlers.cs")
            .Replace("\r\n", "\n");
        var flashbackSource = ReadRepoFile("Sussudio/MainWindow.Flashback.cs")
            .Replace("\r\n", "\n");

        AssertContains(dispatchingSource, "private Task InvokeOnUiThreadAsync(Action action, CancellationToken cancellationToken = default)");
        AssertContains(dispatchingSource, "private Task InvokeOnUiThreadAsync(Func<Task> action, CancellationToken cancellationToken = default)");
        AssertContains(dispatchingSource, "private async Task RunUiEventHandlerAsync(Func<Task> operation, string operationName)");
        AssertContains(dispatchingSource, "CompleteWindowCloseRequest(new OperationCanceledException(cancellationToken));");
        AssertContains(dispatchingSource, "ViewModel.StatusText = $\"{operationName} failed: {ex.Message}\";");
        AssertContains(eventHandlersSource, "RunUiEventHandlerAsync(async () =>");
        AssertContains(flashbackSource, "_ = RunUiEventHandlerAsync(() => ViewModel.ExportFlashbackAsync(), nameof(FlashbackExportButton_Click));");
        AssertDoesNotContain(windowManagementSource, "private Task InvokeOnUiThreadAsync(Action action, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(windowManagementSource, "private async Task RunUiEventHandlerAsync(Func<Task> operation, string operationName)");

        return Task.CompletedTask;
    }

    private static Task StatsSnapshotConstruction_LivesInFocusedBuilder()
    {
        var statsOverlayText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs").Replace("\r\n", "\n");
        var statsSnapshotBuilderText = ReadRepoFile("Sussudio/ViewModels/StatsSnapshotBuilder.cs").Replace("\r\n", "\n");
        var statsSnapshotText = ReadRepoFile("Sussudio/ViewModels/StatsSnapshot.cs").Replace("\r\n", "\n");
        var statsWindowText = ReadRepoFile("Sussudio/StatsWindow.xaml.cs").Replace("\r\n", "\n");

        AssertContains(statsSnapshotBuilderText, "internal static class StatsSnapshotBuilder");
        AssertContains(statsSnapshotBuilderText, "public static StatsSnapshot Build(");
        AssertContains(statsSnapshotBuilderText, "internal readonly record struct StatsSnapshotRenderMetrics(");
        AssertContains(statsSnapshotBuilderText, "internal readonly record struct StatsSnapshotViewState(");
        AssertContains(statsSnapshotBuilderText, "return new StatsSnapshot(");
        AssertContains(statsSnapshotText, "public sealed record StatsSnapshot(");
        AssertContains(statsOverlayText, "var renderer = new StatsSnapshotRenderMetrics(");
        AssertContains(statsOverlayText, "return StatsSnapshotBuilder.Build(health, renderer, viewState);");
        AssertDoesNotContain(statsOverlayText, "return new StatsSnapshot(");
        AssertContains(statsWindowText, "private readonly Func<StatsSnapshot> _dataProvider;");
        AssertDoesNotContain(statsWindowText, "public sealed record StatsSnapshot(");

        return Task.CompletedTask;
    }

    private static Task StatsSnapshotBuilder_MapsHealthAndRendererMetrics()
    {
        var health = CreateInstance("Sussudio.Models.CaptureHealthSnapshot");
        SetPropertyOrBackingField(health, "ExpectedFrameRate", 120d);
        SetPropertyOrBackingField(health, "NegotiatedWidth", 1920u);
        SetPropertyOrBackingField(health, "NegotiatedHeight", 1080u);
        SetPropertyOrBackingField(health, "NegotiatedFrameRate", 120d);
        SetPropertyOrBackingField(health, "ReaderSourceSubtype", "MJPG");
        SetPropertyOrBackingField(health, "CaptureCadenceSampleCount", 60);
        SetPropertyOrBackingField(health, "CaptureCadenceObservedFps", 119.8d);
        SetPropertyOrBackingField(health, "CaptureCadenceAverageIntervalMs", 8.33d);
        SetPropertyOrBackingField(health, "CaptureCadenceP95IntervalMs", 8.75d);
        SetPropertyOrBackingField(health, "CaptureCadenceJitterStdDevMs", 0.12d);
        SetPropertyOrBackingField(health, "CaptureCadenceEstimatedDropPercent", 0.5d);
        SetPropertyOrBackingField(health, "CaptureCadenceEstimatedDroppedFrames", 2L);
        SetPropertyOrBackingField(health, "VideoFramesArrived", 240L);
        SetPropertyOrBackingField(health, "VideoFramesDropped", 3L);
        SetPropertyOrBackingField(health, "VisualCadenceSampleCount", 30);
        SetPropertyOrBackingField(health, "VisualCadenceOutputObservedFps", 120d);
        SetPropertyOrBackingField(health, "VisualCadenceChangeObservedFps", 119d);
        SetPropertyOrBackingField(health, "VisualCadenceMotionConfidence", "HighMotion");
        SetPropertyOrBackingField(health, "VisualCenterCadenceMotionConfidence", "HighMotion");
        SetPropertyOrBackingField(health, "SourceTelemetryOrigin", ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "NativeXu"));
        SetPropertyOrBackingField(health, "SourceTelemetryConfidence", ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "High"));
        SetPropertyOrBackingField(health, "SourceWidth", 3840);
        SetPropertyOrBackingField(health, "SourceHeight", 2160);
        SetPropertyOrBackingField(health, "SourceFrameRateExact", 119.88d);
        SetPropertyOrBackingField(health, "SourceIsHdr", true);
        SetPropertyOrBackingField(health, "SourceVideoFormat", "YCbCr422");
        SetPropertyOrBackingField(health, "SourceColorimetry", "BT.2020");
        SetPropertyOrBackingField(health, "AvSyncCaptureDriftMs", -1.25d);
        SetPropertyOrBackingField(health, "EncoderCodecName", "hevc_nvenc");
        SetPropertyOrBackingField(health, "EncoderWidth", 1920);
        SetPropertyOrBackingField(health, "EncoderHeight", 1080);
        SetPropertyOrBackingField(health, "EncoderFrameRate", 120d);
        SetPropertyOrBackingField(health, "EncoderTargetBitRate", 50_000_000u);

        var detailType = RequireType("Sussudio.Models.SourceTelemetryDetailEntry");
        var details = Array.CreateInstance(detailType, 1);
        details.SetValue(
            Activator.CreateInstance(detailType, "Audio / Input", "ADC (Analog)", "On", null),
            0);
        SetPropertyOrBackingField(health, "SourceTelemetryDetails", details);

        var renderMetricsType = RequireType("Sussudio.StatsSnapshotRenderMetrics");
        var renderMetrics = Activator.CreateInstance(
                renderMetricsType,
                20,
                119.7d,
                8.4d,
                9.0d,
                10.0d,
                118.2d,
                1L,
                double.NaN,
                14.5d,
                250L,
                248L,
                2L,
                1920,
                1080,
                new[] { 8.2d, 8.4d },
                new[] { 12.0d, 14.5d })
            ?? throw new InvalidOperationException("Failed to create StatsSnapshotRenderMetrics.");

        var viewStateType = RequireType("Sussudio.StatsSnapshotViewState");
        var viewState = Activator.CreateInstance(viewStateType, true, false)
            ?? throw new InvalidOperationException("Failed to create StatsSnapshotViewState.");

        var builderType = RequireType("Sussudio.StatsSnapshotBuilder");
        var build = builderType.GetMethod("Build", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("StatsSnapshotBuilder.Build was not found.");
        var snapshot = build.Invoke(null, new[] { health, renderMetrics, viewState })
            ?? throw new InvalidOperationException("StatsSnapshotBuilder.Build returned null.");

        AssertEqual(60, GetIntProperty(snapshot, "SourceCadenceSamples"), "SourceCadenceSamples");
        AssertNearlyEqual(119.8d, GetDoubleProperty(snapshot, "SourceObservedFps"), 0.0001, "SourceObservedFps");
        AssertEqual(20, GetIntProperty(snapshot, "PreviewCadenceSamples"), "PreviewCadenceSamples");
        AssertNearlyEqual(118.2d, GetDoubleProperty(snapshot, "PreviewOnePercentLowFps"), 0.0001, "PreviewOnePercentLowFps");
        AssertNearlyEqual(0.0d, GetDoubleProperty(snapshot, "PreviewSlowPct"), 0.0001, "PreviewSlowPct sanitizes NaN");
        AssertNearlyEqual(99.5d, GetDoubleProperty(snapshot, "PerformanceScore"), 0.0001, "PerformanceScore");
        AssertEqual(true, GetBoolProperty(snapshot, "Previewing"), "Previewing");
        AssertEqual(false, GetBoolProperty(snapshot, "Recording"), "Recording");
        AssertEqual(1920, GetIntProperty(snapshot, "CaptureWidth"), "CaptureWidth");
        AssertEqual("NativeXu", GetStringProperty(snapshot, "TelemetryOrigin"), "TelemetryOrigin");
        AssertEqual("High", GetStringProperty(snapshot, "TelemetryConfidence"), "TelemetryConfidence");
        AssertEqual("Warning", GetStringProperty(snapshot, "DiagnosticHealthStatus"), "DiagnosticHealthStatus");
        AssertEqual("source_capture", GetStringProperty(snapshot, "DiagnosticLikelyStage"), "DiagnosticLikelyStage");
        AssertEqual(2, GetCountProperty(GetPropertyValue(snapshot, "SourceTelemetryDetails")), "SourceTelemetryDetails count");
        AssertEqual(2, GetCountProperty(GetPropertyValue(snapshot, "PreviewRecentPresentIntervalsMs")), "PreviewRecentPresentIntervalsMs count");

        return Task.CompletedTask;
    }
}
