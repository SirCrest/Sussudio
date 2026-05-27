using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

static partial class Program
{
    internal static Task MainWindowAutomationIds_CoverAgentCriticalSurface()
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

    internal static Task MainWindowFullScreenAutomation_AwaitsTransitionTask()
    {
        var fullScreenSource = ReadMainWindowShellChromeAdapterSource();
        var fullScreenControllerRootSource = ReadRepoFile("Sussudio/Controllers/FullScreen/FullScreenController.cs")
            .Replace("\r\n", "\n");
        var dispatchingSource = ReadRepoFile("Sussudio/MainWindow.ShellChrome.Composition.cs")
            .Replace("\r\n", "\n");
        var dispatchControllerSource = ReadRepoFile("Sussudio/Controllers/Window/WindowUiDispatchController.cs")
            .Replace("\r\n", "\n");

        AssertContains(fullScreenSource, "public Task SetFullScreenEnabledAsync(bool enabled, CancellationToken cancellationToken = default)\n        => InvokeOnUiThreadAsync(\n            () => _fullScreenController.SetEnabledAsync(enabled),");
        AssertContains(fullScreenSource, "private void OnContentKeyDown(object sender, KeyRoutedEventArgs e)\n        => _fullScreenController.OnKeyDown(e);");
        AssertContains(fullScreenSource, "private Task EnterFullScreenAsync()\n        => _fullScreenController.EnterAsync();");
        AssertContains(fullScreenSource, "private Task ExitFullScreenAsync()\n        => _fullScreenController.ExitAsync();");
        AssertContains(fullScreenControllerRootSource, "internal sealed class FullScreenController");
        AssertContains(fullScreenControllerRootSource, "public async Task EnterAsync()");
        AssertContains(fullScreenControllerRootSource, "public async Task ExitAsync()");
        AssertContains(fullScreenControllerRootSource, "await AnimateFullScreenRectAsync(");
        AssertContains(fullScreenControllerRootSource, "private Task AnimateFullScreenRectAsync(");
        AssertContains(fullScreenControllerRootSource, "return completion.Task;");
        AssertContains(fullScreenControllerRootSource, "private void PrepareChromeForOverlay()");
        AssertContains(fullScreenControllerRootSource, "public void OnKeyDown(KeyRoutedEventArgs e)");
        AssertContains(fullScreenControllerRootSource, "if (e.Key == Windows.System.VirtualKey.Escape && _isFullScreen)");
        AssertContains(fullScreenControllerRootSource, "Exit();");
        AssertContains(fullScreenControllerRootSource, "public void OnPointerActivity(PointerRoutedEventArgs e)");
        AssertContains(fullScreenControllerRootSource, "private void ShowControls()");
        AssertDoesNotContain(fullScreenControllerRootSource, "partial class FullScreenController");
        AssertDoesNotContain(fullScreenSource, "private async void EnterFullScreen");
        AssertDoesNotContain(fullScreenSource, "private async void ExitFullScreen");
        AssertDoesNotContain(fullScreenSource, "Windows.System.VirtualKey.Escape");
        AssertDoesNotContain(fullScreenSource, "HandleFlashbackFullScreenKeyDown");
        AssertDoesNotContain(fullScreenControllerRootSource, "async void");
        AssertContains(dispatchingSource, "private Task InvokeOnUiThreadAsync(Func<Task> action, CancellationToken cancellationToken = default)");
        AssertContains(dispatchingSource, "=> WindowUiDispatchController.InvokeAsync(action, cancellationToken);");
        AssertContains(dispatchControllerSource, "await action().ConfigureAwait(true);");
        AssertDoesNotContain(dispatchControllerSource, "registration.Dispose();\n                registration = default;\n\n                if (cancellationToken.IsCancellationRequested)");
        return Task.CompletedTask;
    }

    internal static Task MainWindowWindowAutomationCommands_LiveInController()
    {
        var mainWindowSource = ReadMainWindowCompositionSource();
        var dispatchingSource = ReadRepoFile("Sussudio/MainWindow.ShellChrome.Composition.cs")
            .Replace("\r\n", "\n");
        var dispatchControllerSource = ReadRepoFile("Sussudio/Controllers/Window/WindowUiDispatchController.cs")
            .Replace("\r\n", "\n");
        var adapterSource = ReadRepoFile("Sussudio/MainWindow.ShellChrome.Composition.cs")
            .Replace("\r\n", "\n");
        var controllerSource = ReadRepoFile("Sussudio/Controllers/Window/WindowAutomationController.cs")
            .Replace("\r\n", "\n");

        AssertContains(adapterSource, "private WindowAutomationController _windowAutomationController = null!;");
        AssertContains(adapterSource, "private void InitializeWindowAutomationController()");
        AssertContains(adapterSource, "GetAppWindow = GetAppWindow,");
        AssertContains(adapterSource, "GetWindowHandle = () => _hwnd,");
        AssertContains(adapterSource, "InvokeOnUiThreadAsync = InvokeOnUiThreadAsync");
        AssertContains(dispatchingSource, "private Task InvokeOnUiThreadAsync(Action action, CancellationToken cancellationToken = default)");
        AssertContains(dispatchingSource, "=> WindowUiDispatchController.InvokeAsync(action, cancellationToken);");
        AssertContains(dispatchControllerSource, "public Task InvokeAsync(Action action, CancellationToken cancellationToken = default)");
        AssertContains(adapterSource, "=> _windowAutomationController.MinimizeAsync(cancellationToken);");
        AssertContains(adapterSource, "=> _windowAutomationController.OpenRecordingsFolderAsync(cancellationToken);");
        AssertContains(adapterSource, "=> _windowAutomationController.SnapToRegionAsync(region, cancellationToken);");
        AssertContains(mainWindowSource, "InitializeWindowAutomationController();");
        AssertContains(controllerSource, "internal sealed class WindowAutomationController");
        AssertContains(controllerSource, "internal sealed class WindowAutomationHostLifecycleController");
        AssertContains(controllerSource, "public Task MoveToAsync(int x, int y, CancellationToken cancellationToken = default)");
        AssertContains(controllerSource, "public Task ResizeToAsync(int width, int height, CancellationToken cancellationToken = default)");
        AssertContains(controllerSource, "public Task SnapToRegionAsync(AutomationWindowAction region, CancellationToken cancellationToken = default)");
        AssertContains(controllerSource, "DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary)");
        AssertContains(controllerSource, "var currentSize = region == AutomationWindowAction.Center");
        AssertContains(controllerSource, "WindowSnapRegionLayoutPolicy.ResolveTargetBounds(region, work, currentSize)");
        AssertContains(controllerSource, "appWindow.MoveAndResize(bounds);");
        AssertContains(controllerSource, "Process.Start(\"explorer.exe\", path);");
        AssertContains(controllerSource, "internal static class WindowSnapRegionLayoutPolicy");
        AssertContains(controllerSource, "public static RectInt32? ResolveTargetBounds(");
        AssertContains(controllerSource, "AutomationWindowAction.Center => new RectInt32(");
        AssertContains(adapterSource, "public Task CloseAsync(CancellationToken cancellationToken = default)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Window", "WindowSnapRegionLayoutPolicy.cs")),
            "snap-region rectangle math lives with window automation controller concerns");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Window", "WindowAutomationHostLifecycleController.cs")),
            "automation host lifecycle lives with the window automation controller owner");
        return Task.CompletedTask;
    }

    internal static Task MainWindowUiDispatching_LivesInShellChromeAdapter()
    {
        var dispatchingSource = ReadRepoFile("Sussudio/MainWindow.ShellChrome.Composition.cs")
            .Replace("\r\n", "\n");
        var dispatchControllerSource = ReadRepoFile("Sussudio/Controllers/Window/WindowUiDispatchController.cs")
            .Replace("\r\n", "\n");
        var previewActionsSource = ReadMainWindowPreviewTransitionsAdapterSource();
        var flashbackSource = ReadMainWindowFlashbackAdapterSource();
        var flashbackControllerSource = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs")
            .Replace("\r\n", "\n");

        AssertContains(dispatchingSource, "private WindowUiDispatchController? _windowUiDispatchController;");
        AssertContains(dispatchingSource, "private WindowUiDispatchController WindowUiDispatchController =>");
        AssertContains(dispatchingSource, "CompleteWindowCloseRequest = CompleteWindowCloseRequest");
        AssertContains(dispatchingSource, "private Task InvokeOnUiThreadAsync(Action action, CancellationToken cancellationToken = default)");
        AssertContains(dispatchingSource, "=> WindowUiDispatchController.InvokeAsync(action, cancellationToken);");
        AssertContains(dispatchingSource, "private Task InvokeOnUiThreadAsync(Func<Task> action, CancellationToken cancellationToken = default)");
        AssertContains(dispatchingSource, "private Task RunUiEventHandlerAsync(Func<Task> operation, string operationName)");
        AssertContains(dispatchingSource, "=> WindowUiDispatchController.RunUiEventHandlerAsync(operation, operationName);");
        AssertContains(dispatchControllerSource, "internal sealed class WindowUiDispatchController");
        AssertContains(dispatchControllerSource, "public Task InvokeAsync(Action action, CancellationToken cancellationToken = default)");
        AssertContains(dispatchControllerSource, "public Task InvokeAsync(Func<Task> action, CancellationToken cancellationToken = default)");
        AssertContains(dispatchControllerSource, "public async Task<TResult> InvokeWithRetryAsync<TResult>(");
        AssertContains(dispatchControllerSource, "public async Task RunUiEventHandlerAsync(Func<Task> operation, string operationName)");
        AssertContains(dispatchControllerSource, "if (_context.DispatcherQueue.HasThreadAccess)\n        {\n            action();\n            return Task.CompletedTask;\n        }");
        AssertContains(dispatchControllerSource, "if (_context.DispatcherQueue.HasThreadAccess)\n        {\n            return action();\n        }");
        AssertContains(dispatchControllerSource, "const int maxAttempts = 3;");
        AssertContains(dispatchControllerSource, "completion.TrySetResult(action());");
        AssertContains(dispatchControllerSource, "await Task.Delay(50, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatchControllerSource, "throw new InvalidOperationException(enqueueFailureMessage);");
        AssertContains(dispatchControllerSource, "_context.CompleteWindowCloseRequest(new OperationCanceledException(cancellationToken));");
        AssertContains(dispatchControllerSource, "await action().ConfigureAwait(true);");
        AssertContains(dispatchControllerSource, "completion.TrySetException(new InvalidOperationException(\"Failed to enqueue window action on the UI thread.\"));");
        AssertContains(dispatchControllerSource, "_context.ViewModel.StatusText = $\"{operationName} failed: {ex.Message}\";");
        AssertContains(previewActionsSource, "_ = RunUiEventHandlerAsync(() => TogglePreviewFromButtonAsync(), nameof(PreviewButton_Click));");
        AssertContains(flashbackSource, "=> _flashbackCommandController.Export(nameof(FlashbackExportButton_Click));");
        AssertContains(flashbackSource, "=> _flashbackCommandController.SaveLast5m(nameof(FlashbackSaveLast5mButton_Click));");
        AssertContains(flashbackControllerSource, "=> _ = _context.RunUiEventHandlerAsync(() => _context.ViewModel.ExportFlashbackAsync(), operationName);");
        AssertContains(flashbackControllerSource, "=> _ = _context.RunUiEventHandlerAsync(() => _context.ViewModel.SaveFlashbackLast5mAsync(), operationName);");
        AssertDoesNotContain(dispatchingSource, "CompleteWindowCloseRequest(new OperationCanceledException(cancellationToken));");
        AssertDoesNotContain(dispatchingSource, "ViewModel.StatusText = $\"{operationName} failed: {ex.Message}\";");

        return Task.CompletedTask;
    }
}

namespace Sussudio.Tests
{
public class MainWindowUiContractStatsSnapshotTests
{
    [Fact]
    public void StatsSnapshotConstruction_LivesInFocusedBuilder()
    {
        var statsOverlayText = Sussudio.Tests.MainWindowStatsOverlaySource.Read();
        var statsOverlayCompositionText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs");
        var statsSnapshotProviderText = statsOverlayCompositionText;
        var mainWindowText = MainWindowCompositionSource.Read();
        var statsSnapshotText = ReadRepoFile("Sussudio/ViewModels/StatsSnapshot.cs");
        var statsSnapshotBuilderText = statsSnapshotText;
        var statsWindowText = ReadRepoFile("Sussudio/StatsWindow.xaml.cs");

        AssertContains(statsSnapshotBuilderText, "internal static class StatsSnapshotBuilder");
        AssertContains(statsSnapshotBuilderText, "public static StatsSnapshot Build(");
        AssertContains(statsSnapshotBuilderText, "internal readonly record struct StatsSnapshotRenderMetrics(");
        AssertContains(statsSnapshotBuilderText, "internal readonly record struct StatsSnapshotViewState(");
        AssertContains(statsSnapshotBuilderText, "return new StatsSnapshot(");
        AssertContains(statsSnapshotText, "public sealed record StatsSnapshot(");
        AssertContains(mainWindowText, "InitializeStatsOverlayCompositionController();");
        AssertContains(statsOverlayText, "private StatsSnapshot GetStatsSnapshot()");
        AssertContains(statsOverlayCompositionText, "private readonly StatsSnapshotProvider _statsSnapshotProvider;");
        AssertContains(statsOverlayText, "GetCaptureHealthSnapshot = ViewModel.GetCaptureHealthSnapshot,");
        AssertContains(statsOverlayText, "GetRenderer = () => _previewRendererHostController.Renderer,");
        AssertContains(statsOverlayText, "GetPreviewMinPresentationIntervalMs = () => _previewRendererHostController.PreviewMinPresentationIntervalMs");
        AssertContains(statsOverlayText, "IsPreviewing = () => ViewModel.IsPreviewing,");
        AssertContains(statsOverlayText, "IsRecording = () => ViewModel.IsRecording");
        AssertContains(statsOverlayText, "=> _statsOverlayCompositionController.GetStatsSnapshot();");
        AssertContains(statsOverlayCompositionText, "private static StatsSnapshotProvider CreateSnapshotProvider(");
        AssertContains(statsOverlayCompositionText, "=> _statsSnapshotProvider.GetSnapshot();");
        AssertContains(statsSnapshotProviderText, "internal sealed class StatsSnapshotProvider");
        AssertDoesNotContain(statsSnapshotProviderText, "internal sealed partial class StatsSnapshotProvider");
        AssertContains(statsSnapshotProviderText, "private const int RecentSampleCount = 180;");
        AssertContains(statsSnapshotProviderText, "var health = _context.GetCaptureHealthSnapshot();");
        AssertContains(statsSnapshotProviderText, "BuildRenderMetrics(_context.GetRenderer(), _context.GetPreviewMinPresentationIntervalMs())");
        AssertContains(statsSnapshotProviderText, "new StatsSnapshotViewState(_context.IsPreviewing(), _context.IsRecording())");
        AssertContains(statsSnapshotProviderText, "return StatsSnapshotBuilder.Build(health, renderer, viewState);");
        AssertDoesNotContain(statsSnapshotProviderText, "MainViewModel ViewModel");
        AssertContains(statsSnapshotProviderText, "var presentCadence = renderer?.GetPresentCadenceMetrics(previewMinPresentationIntervalMs);");
        AssertContains(statsSnapshotProviderText, "PreviewRecentPresentIntervalsMs: renderer?.GetRecentPresentIntervalsMs(RecentSampleCount) ?? Array.Empty<double>()");
        Assert.False(
            File.Exists(Path.Combine(Environment.CurrentDirectory, "Sussudio", "Controllers", "Stats", "StatsSnapshotProvider.cs")),
            "stats snapshot provider lives with stats overlay composition");
        AssertDoesNotContain(statsOverlayText, "var renderer = new StatsSnapshotRenderMetrics(");
        AssertDoesNotContain(statsOverlayText, "return new StatsSnapshot(");
        AssertDoesNotContain(statsOverlayText, "return StatsSnapshotBuilder.Build(health, renderer, viewState);");
        AssertContains(statsWindowText, "private readonly Func<StatsSnapshot> _dataProvider;");
        AssertDoesNotContain(statsWindowText, "public sealed record StatsSnapshot(");
    }

    [Fact]
    public void StatsSnapshotBuilder_MapsHealthAndRendererMetrics()
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

        Assert.Equal(60, GetIntProperty(snapshot, "SourceCadenceSamples"));
        AssertNearlyEqual(119.8d, GetDoubleProperty(snapshot, "SourceObservedFps"), 0.0001);
        Assert.Equal(20, GetIntProperty(snapshot, "PreviewCadenceSamples"));
        AssertNearlyEqual(118.2d, GetDoubleProperty(snapshot, "PreviewOnePercentLowFps"), 0.0001);
        AssertNearlyEqual(0.0d, GetDoubleProperty(snapshot, "PreviewSlowPct"), 0.0001);
        AssertNearlyEqual(99.5d, GetDoubleProperty(snapshot, "PerformanceScore"), 0.0001);
        Assert.True(GetBoolProperty(snapshot, "Previewing"));
        Assert.False(GetBoolProperty(snapshot, "Recording"));
        Assert.Equal(1920, GetIntProperty(snapshot, "CaptureWidth"));
        Assert.Equal("NativeXu", GetStringProperty(snapshot, "TelemetryOrigin"));
        Assert.Equal("High", GetStringProperty(snapshot, "TelemetryConfidence"));
        Assert.Equal("Warning", GetStringProperty(snapshot, "DiagnosticHealthStatus"));
        Assert.Equal("source_capture", GetStringProperty(snapshot, "DiagnosticLikelyStage"));
        Assert.Equal(2, GetCountProperty(GetPropertyValue(snapshot, "SourceTelemetryDetails")));
        Assert.Equal(2, GetCountProperty(GetPropertyValue(snapshot, "PreviewRecentPresentIntervalsMs")));
    }

    private static Type RequireType(string typeName)
        => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

    private static object CreateInstance(string typeName)
        => Activator.CreateInstance(RequireType(typeName))
           ?? throw new InvalidOperationException($"Failed to create {typeName}.");

    private static object ParseEnum(string typeName, string value)
        => Enum.Parse(RequireType(typeName), value);

    private static void SetPropertyOrBackingField(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property?.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        var field = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Backing field for {propertyName} was not found.");
        field.SetValue(instance, value);
    }

    private static object? GetPropertyValue(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(instance);

    private static int GetIntProperty(object instance, string propertyName)
        => Convert.ToInt32(GetPropertyValue(instance, propertyName), CultureInfo.InvariantCulture);

    private static double GetDoubleProperty(object instance, string propertyName)
        => Convert.ToDouble(GetPropertyValue(instance, propertyName), CultureInfo.InvariantCulture);

    private static bool GetBoolProperty(object instance, string propertyName)
        => Convert.ToBoolean(GetPropertyValue(instance, propertyName), CultureInfo.InvariantCulture);

    private static string GetStringProperty(object instance, string propertyName)
        => GetPropertyValue(instance, propertyName) as string
           ?? throw new InvalidOperationException($"{propertyName} was not a string.");

    private static int GetCountProperty(object? value)
        => value is ICollection collection
            ? collection.Count
            : value is IEnumerable enumerable
                ? enumerable.Cast<object>().Count()
                : throw new InvalidOperationException("Expected collection value.");

    private static string ReadRepoFile(string relativePath)
        => RuntimeContractSource.ReadRepoFile(relativePath).Replace("\r\n", "\n");

    private static void AssertContains(string actual, string expectedSubstring)
        => Assert.Contains(expectedSubstring, actual, StringComparison.Ordinal);

    private static void AssertDoesNotContain(string actual, string unexpectedSubstring)
        => Assert.DoesNotContain(unexpectedSubstring, actual, StringComparison.Ordinal);

    private static void AssertNearlyEqual(double expected, double actual, double tolerance)
        => Assert.True(
            Math.Abs(expected - actual) <= tolerance,
            $"Expected {expected:0.####}, got {actual:0.####}; tolerance {tolerance:0.####}.");
}
}
