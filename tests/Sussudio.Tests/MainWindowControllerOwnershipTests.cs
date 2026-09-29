using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

static partial class Program
{

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

internal static Task ResponsiveShellLayout_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var xamlText = ReadRepoFile("Sussudio/MainWindow.xaml").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Shell/ShellChromeController.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");

        AssertContains(adapterText, "private ControlBarLabelVisibilityController _controlBarLabelVisibilityController = null!;");
        AssertContains(adapterText, "private ResponsiveShellLayoutController _responsiveShellLayoutController = null!;");
        AssertContains(adapterText, "private void InitializeResponsiveShellLayoutController()");
        AssertContains(adapterText, "var controlBarLabels = new UIElement[]");
        AssertContains(adapterText, "CaptureSettingsGrid = CaptureSettingsGrid,");
        AssertContains(adapterText, "FlashbackToggleLabel,");
        AssertContains(adapterText, "_controlBarLabelVisibilityController = new ControlBarLabelVisibilityController(new ControlBarLabelVisibilityControllerContext");
        AssertContains(adapterText, "ControlBarBorder = ControlBarBorder,");
        AssertContains(adapterText, "ControlBarLabels = controlBarLabels,");
        AssertContains(adapterText, "private void SetupResponsiveShellLayoutBindings()");
        AssertContains(adapterText, "_controlBarLabelVisibilityController.Attach();");
        AssertContains(adapterText, "_responsiveShellLayoutController.Attach();");
        AssertContains(adapterText, "private void SetupResponsiveShellLayoutBindings()\n    {\n        _controlBarLabelVisibilityController.Attach();\n        _responsiveShellLayoutController.Attach();\n    }");
        AssertContains(xamlText, "x:Name=\"FlashbackToggleLabel\"");
        AssertContains(mainWindowText, "InitializeResponsiveShellLayoutController();");
        AssertContains(bindingsText, "SetupResponsiveShellLayoutBindings();");
        AssertContains(controllerText, "internal sealed class ResponsiveShellLayoutController");
        AssertContains(controllerText, "internal sealed class ControlBarLabelVisibilityController");
        AssertContains(controllerText, "public required UIElement[] ControlBarLabels { get; init; }");
        AssertContains(controllerText, "internal static class ResponsiveShellLayoutPolicy");
        AssertContains(controllerText, "public const double ControlBarLabelThreshold = 900.0;");
        AssertContains(controllerText, "public const double CaptureSettingsNarrowWidth = 700.0;");
        AssertContains(controllerText, "internal readonly record struct ResponsiveCaptureSettingsPlacement");
        AssertContains(controllerText, "private bool _toggleLabelsVisible;");
        AssertContains(controllerText, "private bool _captureSettingsNarrow;");
        AssertContains(controllerText, "public void Attach()");
        AssertContains(controllerText, "_context.ControlBarBorder.SizeChanged += (_, e) => ApplyControlBarWidth(e.NewSize.Width);");
        AssertContains(controllerText, "ResponsiveShellLayoutPolicy.ShouldShowControlBarLabels(controlBarWidth);");
        AssertContains(controllerText, "foreach (var label in _context.ControlBarLabels)");
        AssertContains(controllerText, "label.Visibility = visibility;");
        AssertContains(controllerText, "ResponsiveShellLayoutPolicy.GetCaptureSettingsLayoutKind(width);");
        AssertContains(controllerText, "private void ApplyCaptureSettingsLayout(ResponsiveCaptureSettingsPlacement placement)");
        AssertContains(controllerText, "private static void ApplyGridSlot(FrameworkElement element, ResponsiveGridSlot slot)");
        AssertContains(agentMapText, "complete control-bar label set");
        AssertContains(cleanupPlanText, "complete control-bar label set");
        AssertDoesNotContain(mainWindowText, "private bool _toggleLabelsVisible;");
        AssertDoesNotContain(mainWindowText, "private bool _captureSettingsNarrow;");
        AssertDoesNotContain(mainWindowText, "private const double ControlBarLabelThreshold = 900.0;");
        AssertDoesNotContain(controllerText, "private const double ControlBarLabelThreshold = 900.0;");
        AssertDoesNotContain(controllerText, "private const double CaptureSettingsNarrowWidth = 700.0;");
        AssertDoesNotContain(controllerText, "_context.HdrToggleLabel.Visibility = visibility;");
        AssertDoesNotContain(controllerText, "_context.FrameTimeOverlayToggleLabel.Visibility = visibility;");
        AssertDoesNotContain(adapterText, "FlashbackToggleLabel = FlashbackToggleLabel,");
        AssertDoesNotContain(controllerText, "private void ApplyNarrowCaptureSettingsLayout()");
        AssertDoesNotContain(controllerText, "private void ApplyWideCaptureSettingsLayout()");
        AssertDoesNotContain(bindingsText, "private void UpdateToggleLabelVisibility(");
        AssertDoesNotContain(bindingsText, "private void CaptureSettingsGrid_SizeChanged(");

        return Task.CompletedTask;
    }

    internal static Task ResponsiveShellLayoutPolicy_PreservesBreakpointsAndPlacements()
    {
        var policyType = RequireType("Sussudio.Controllers.ResponsiveShellLayoutPolicy");
        var shouldShowLabels = policyType.GetMethod(
            "ShouldShowControlBarLabels",
            BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("ResponsiveShellLayoutPolicy.ShouldShowControlBarLabels not found.");
        var getLayoutKind = policyType.GetMethod(
            "GetCaptureSettingsLayoutKind",
            BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("ResponsiveShellLayoutPolicy.GetCaptureSettingsLayoutKind not found.");
        var getPlacement = policyType.GetMethod(
            "GetCaptureSettingsPlacement",
            BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("ResponsiveShellLayoutPolicy.GetCaptureSettingsPlacement not found.");

        AssertEqual(false, (bool)shouldShowLabels.Invoke(null, new object[] { 899.99 })!, "control bar labels below 900");
        AssertEqual(true, (bool)shouldShowLabels.Invoke(null, new object[] { 900.0 })!, "control bar labels at 900");

        var narrowKind = getLayoutKind.Invoke(null, new object[] { 699.99 })
            ?? throw new InvalidOperationException("Narrow responsive shell layout kind was null.");
        var wideKind = getLayoutKind.Invoke(null, new object[] { 700.0 })
            ?? throw new InvalidOperationException("Wide responsive shell layout kind was null.");
        AssertEqual("Narrow", narrowKind.ToString()!, "capture settings below 700");
        AssertEqual("Wide", wideKind.ToString()!, "capture settings at 700");

        var narrowPlacement = getPlacement.Invoke(null, new[] { narrowKind })
            ?? throw new InvalidOperationException("Narrow responsive shell placement was null.");
        AssertEqual(true, GetBoolProperty(narrowPlacement, "CollapseCaptureOptionColumns"), "narrow columns collapse");
        AssertGridSlot(narrowPlacement, "VideoFormat", 1, 1);
        AssertGridSlot(narrowPlacement, "Preset", 1, 2);
        AssertGridSlot(narrowPlacement, "Split", 1, 3);
        AssertGridSlot(narrowPlacement, "CustomBitrate", 1, 2);

        var widePlacement = getPlacement.Invoke(null, new[] { wideKind })
            ?? throw new InvalidOperationException("Wide responsive shell placement was null.");
        AssertEqual(false, GetBoolProperty(widePlacement, "CollapseCaptureOptionColumns"), "wide columns stay flexible");
        AssertGridSlot(widePlacement, "VideoFormat", 0, 0);
        AssertGridSlot(widePlacement, "Preset", 0, 5);
        AssertGridSlot(widePlacement, "Split", 0, 6);
        AssertGridSlot(widePlacement, "CustomBitrate", 0, 5);

        return Task.CompletedTask;
    }

    private static void AssertGridSlot(object placement, string propertyName, int expectedRow, int expectedColumn)
    {
        var slot = GetPropertyValue(placement, propertyName)
            ?? throw new InvalidOperationException($"Responsive grid slot '{propertyName}' was null.");
        AssertEqual(expectedRow, GetIntProperty(slot, "Row"), $"{propertyName} row");
        AssertEqual(expectedColumn, GetIntProperty(slot, "Column"), $"{propertyName} column");
    }

    internal static Task OutputPathDisplayTextFormatter_PreservesTruncationPolicy()
    {
        var formatterType = RequireType("Sussudio.Controllers.OutputPathDisplayTextFormatter");
        var format = formatterType.GetMethod("Format", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("OutputPathDisplayTextFormatter.Format was not found.");

        string Format(string path, double availableWidth)
        {
            return format.Invoke(null, new object[] { path, availableWidth })?.ToString()
                ?? throw new InvalidOperationException("OutputPathDisplayTextFormatter.Format returned null.");
        }

        AssertEqual(
            "C:\\captures\\clip.mp4",
            Format("C:\\captures\\clip.mp4", 240),
            "Full output path fits when width has enough characters");
        AssertEqual(
            "C:\\captures\\clip.mp4",
            Format("C:\\captures\\clip.mp4", 0),
            "Zero output path width preserves full path");
        AssertEqual(
            "C:\\captures\\clip.mp4",
            Format("C:\\captures\\clip.mp4", -10),
            "Negative output path width preserves full path");
        AssertEqual(
            "clip-with-a-very-long-name.mp4",
            Format("clip-with-a-very-long-name.mp4", 40),
            "Simple path without folder segments stays unchanged");
        AssertEqual(
            "C:\\...\\session\\captures\\clip.mp4",
            Format("C:\\users\\crest\\videos\\session\\captures\\clip.mp4", 250),
            "Deep output path keeps root and fitting tail segments");
        AssertEqual(
            "C:\\...\\clip.mp4",
            Format("C:\\users\\crest\\videos\\session\\captures\\clip.mp4", 80),
            "Deep output path falls back to root and filename");

        return Task.CompletedTask;
    }


    internal static Task OutputDriveSpacePresentationBuilder_InvalidPathReturnsEmpty()
    {
        var builderType = RequireType("Sussudio.ViewModels.OutputDriveSpacePresentationBuilder");
        var buildMethod = builderType.GetMethod(
            "Build",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("OutputDriveSpacePresentationBuilder.Build was not found.");

        AssertEqual(
            "",
            buildMethod.Invoke(null, new object?[] { "\0" }),
            "Output drive space invalid path fallback");

        return Task.CompletedTask;
    }


    internal static Task PreviewScreenshotPlanPolicy_PreservesPathAndTextContracts()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewScreenshotPlanPolicy");
        var create = policyType.GetMethod("Create", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.Create was not found.");
        var savedStatus = policyType.GetMethod("FormatSavedStatus", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.FormatSavedStatus was not found.");
        var failedStatus = policyType.GetMethod("FormatFailedStatus", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.FormatFailedStatus was not found.");
        var savedLog = policyType.GetMethod("FormatSavedLog", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.FormatSavedLog was not found.");
        var failedLog = policyType.GetMethod("FormatFailedLog", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.FormatFailedLog was not found.");
        var previewRequired = policyType.GetField("PreviewRequiredStatusText", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.PreviewRequiredStatusText was not found.");

        var timestamp = new System.DateTime(2026, 5, 16, 14, 3, 4);
        var captureId = new Guid("01234567-89ab-cdef-0123-456789abcdef");
        var fallbackPlan = create.Invoke(null, new object?[] { "   ", "C:\\Users\\crest\\Pictures", timestamp, captureId })
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.Create returned null.");
        var configuredPlan = create.Invoke(null, new object?[] { "D:\\Captures", "C:\\Users\\crest\\Pictures", timestamp, captureId })
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.Create returned null.");
        var fallbackPath = GetStringProperty(fallbackPlan, "FilePath");
        var configuredPath = GetStringProperty(configuredPlan, "FilePath");

        AssertEqual(
            "Start preview before capturing a screenshot",
            previewRequired.GetValue(null)?.ToString(),
            "preview screenshot not-previewing status");
        AssertEqual(
            "C:\\Users\\crest\\Pictures\\Sussudio",
            GetStringProperty(fallbackPlan, "OutputDirectory"),
            "preview screenshot fallback output directory");
        AssertEqual(
            "C:\\Users\\crest\\Pictures\\Sussudio\\Screenshot_2026-05-16_14-03-04_000_0123456789abcdef0123456789abcdef.png",
            fallbackPath,
            "preview screenshot fallback path");
        AssertEqual(
            "D:\\Captures",
            GetStringProperty(configuredPlan, "OutputDirectory"),
            "preview screenshot configured output directory");
        AssertEqual(
            "D:\\Captures\\Screenshot_2026-05-16_14-03-04_000_0123456789abcdef0123456789abcdef.png",
            configuredPath,
            "preview screenshot configured path");
        AssertEqual(
            "Screenshot saved: Screenshot_2026-05-16_14-03-04_000_0123456789abcdef0123456789abcdef.png",
            savedStatus.Invoke(null, new object[] { configuredPath })?.ToString(),
            "preview screenshot saved status");
        AssertEqual(
            "SCREENSHOT_SAVED path=D:\\Captures\\Screenshot_2026-05-16_14-03-04_000_0123456789abcdef0123456789abcdef.png width=1280 height=720",
            savedLog.Invoke(null, new object[] { configuredPath, 1280, 720 })?.ToString(),
            "preview screenshot saved log");
        AssertEqual(
            "Screenshot failed: renderer unavailable",
            failedStatus.Invoke(null, new object[] { "renderer unavailable" })?.ToString(),
            "preview screenshot failed status");
        AssertEqual(
            "SCREENSHOT_FAILED reason=renderer unavailable",
            failedLog.Invoke(null, new object[] { "renderer unavailable" })?.ToString(),
            "preview screenshot failed log");

        return Task.CompletedTask;
    }


    internal static Task WindowScreenshotImageEncoding_LivesInFocusedHelper()
    {
        var controllerText = ReadRepoFile("Sussudio/Controllers/Screenshot/ScreenshotControllers.cs")
            .Replace("\r\n", "\n");
        var encoderText = controllerText;

        AssertContains(controllerText, "private static void SaveHBitmapAsImage(");
        AssertContains(controllerText, "new FileStream(outputPath, FileMode.CreateNew");
        AssertContains(controllerText, "WindowScreenshotImageEncoder.WriteToStream(");
        AssertContains(controllerText, "internal static void WritePngToStream");
        AssertContains(controllerText, "internal static void WriteBmpToStream");
        AssertContains(encoderText, "internal static class WindowScreenshotImageEncoder");
        AssertContains(encoderText, "internal static void WritePngToStream");
        AssertContains(encoderText, "internal static void WriteBmpToStream");
        AssertContains(encoderText, "internal static uint[] InitCrc32Table()");

        var encoderType = RequireType("Sussudio.Controllers.WindowScreenshotImageEncoder");
        var writePng = encoderType.GetMethod("WritePngToStream", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("WindowScreenshotImageEncoder.WritePngToStream missing.");
        var writeBmp = encoderType.GetMethod("WriteBmpToStream", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("WindowScreenshotImageEncoder.WriteBmpToStream missing.");
        var bgra = new byte[] { 0, 0, 255, 255 };

        using var pngStream = new MemoryStream();
        writePng.Invoke(null, new object[] { pngStream, 1, 1, bgra });
        var pngBytes = pngStream.ToArray();
        AssertSequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, pngBytes.Take(8).ToArray(), "PNG signature");
        AssertEqual((byte)73, pngBytes[12], "PNG IHDR I");
        AssertEqual((byte)72, pngBytes[13], "PNG IHDR H");
        AssertEqual((byte)68, pngBytes[14], "PNG IHDR D");
        AssertEqual((byte)82, pngBytes[15], "PNG IHDR R");

        using var bmpStream = new MemoryStream();
        writeBmp.Invoke(null, new object[] { bmpStream, 1, 1, bgra });
        var bmpBytes = bmpStream.ToArray();
        AssertEqual((byte)0x42, bmpBytes[0], "BMP signature B");
        AssertEqual((byte)0x4D, bmpBytes[1], "BMP signature M");
        AssertEqual(58, bmpBytes.Length, "BMP byte length");
        AssertEqual(1, BitConverter.ToInt32(bmpBytes, 18), "BMP width");
        AssertEqual(-1, BitConverter.ToInt32(bmpBytes, 22), "BMP top-down height");

        return Task.CompletedTask;
    }


    internal static Task WindowTitleController_FormatsBuildStampAndRecordingSuffix()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("de-DE");

            var controllerType = RequireType("Sussudio.Controllers.WindowTitleController");
            var formatBuildTitle = controllerType.GetMethod("FormatBuildTitle", BindingFlags.Static | BindingFlags.NonPublic)
                                   ?? throw new InvalidOperationException("WindowTitleController.FormatBuildTitle not found.");
            var formatTitle = controllerType.GetMethod("FormatTitle", BindingFlags.Static | BindingFlags.NonPublic)
                              ?? throw new InvalidOperationException("WindowTitleController.FormatTitle not found.");

            var buildTime = new DateTime(2026, 5, 14, 22, 30, 45, DateTimeKind.Local);
            var buildTitle = formatBuildTitle.Invoke(null, new object?[] { buildTime });
            AssertEqual("Simple Sussudio (build 2026-05-14 22:30:45)", buildTitle, "invariant build title");
            AssertEqual("Simple Sussudio", formatBuildTitle.Invoke(null, new object?[] { DateTime.MinValue }), "missing build-time title");

            AssertEqual("Simple Sussudio", formatTitle.Invoke(null, new object?[] { "Simple Sussudio", false, "00:01:02" }), "idle title");
            AssertEqual(
                "Simple Sussudio - REC 00:01:02",
                formatTitle.Invoke(null, new object?[] { "Simple Sussudio", true, "00:01:02" }),
                "recording title");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }

        return Task.CompletedTask;
    }


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

    internal static async Task WindowUiDispatchCancellation_DoesNotCompleteWindowCloseRequest()
    {
        var dispatchControllerSource = ReadRepoFile("Sussudio/Controllers/Dispatch/UiDispatchControllers.cs")
            .Replace("\r\n", "\n");
        var windowDispatchControllerSource = ExtractTypeBlock(dispatchControllerSource, "WindowUiDispatchController");
        var lifecycleController = CreateInstance("Sussudio.Controllers.WindowCloseLifecycleController");
        var lifecycleType = lifecycleController.GetType();
        var getCompletionTask = lifecycleType.GetMethod(
            "GetCompletionTask",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("WindowCloseLifecycleController.GetCompletionTask was not found.");
        var completeRequest = lifecycleType.GetMethod(
            "CompleteRequest",
            BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("WindowCloseLifecycleController.CompleteRequest was not found.");
        var closeTask = (Task)(getCompletionTask.Invoke(lifecycleController, new object[] { CancellationToken.None })
            ?? throw new InvalidOperationException("Window close completion task was not created."));

        completeRequest.Invoke(
            lifecycleController,
            new object?[] { new OperationCanceledException("Unrelated UI dispatch canceled.") });

        await Assert.ThrowsAsync<OperationCanceledException>(() => closeTask);
        Assert.True(closeTask.IsFaulted, "Completing a close request with a dispatch cancellation faults the close task.");
        AssertDoesNotContain(windowDispatchControllerSource, "CompleteWindowCloseRequest");
    }

    private static string ExtractTypeBlock(string source, string typeName)
    {
        var declaration = $"internal sealed class {typeName}\n";
        var start = source.IndexOf(declaration, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"{typeName} declaration not found.");
        }

        var openBrace = source.IndexOf('{', start);
        if (openBrace < 0)
        {
            throw new InvalidOperationException($"{typeName} body start not found.");
        }

        var depth = 0;
        for (var i = openBrace; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source.Substring(start, i - start + 1);
                }
            }
        }

        throw new InvalidOperationException($"{typeName} body end not found.");
    }
}

namespace Sussudio.Tests
{
    public sealed class WindowSnapRegionLayoutPolicyTests
    {
        private const string PolicyTypeName = "Sussudio.Controllers.WindowSnapRegionLayoutPolicy";
        private const string ActionTypeName = "Sussudio.Models.AutomationWindowAction";

        [Theory]
        [InlineData("SnapLeft", 10, 20, 50, 55)]
        [InlineData("SnapRight", 60, 20, 51, 55)]
        [InlineData("SnapTopLeft", 10, 20, 50, 27)]
        [InlineData("SnapTopRight", 60, 20, 51, 27)]
        [InlineData("SnapBottomLeft", 10, 47, 50, 28)]
        [InlineData("SnapBottomRight", 60, 47, 51, 28)]
        [InlineData("Center", 44, 40, 33, 15)]
        public void ResolveTargetBounds_PreservesExistingSnapGeometry(string actionName, int x, int y, int width, int height)
        {
            var policyType = SussudioAssembly.Load().GetType(PolicyTypeName, throwOnError: true)!;
            var actionType = SussudioAssembly.Load().GetType(ActionTypeName, throwOnError: true)!;
            var method = policyType.GetMethod("ResolveTargetBounds", BindingFlags.Public | BindingFlags.Static)!;
            var parameterTypes = method.GetParameters();
            var workArea = CreateStruct(parameterTypes[1].ParameterType, 10, 20, 101, 55);
            var currentSize = CreateStruct(parameterTypes[2].ParameterType, 33, 15);
            var action = Enum.Parse(actionType, actionName);

            var result = method.Invoke(null, new[] { action, workArea, currentSize });

            Assert.NotNull(result);
            AssertRect(result!, x, y, width, height);
        }

        [Theory]
        [InlineData("Restore")]
        [InlineData(null)]
        public void ResolveTargetBounds_ReturnsNullForNonSnapActions(string? actionName)
        {
            var policyType = SussudioAssembly.Load().GetType(PolicyTypeName, throwOnError: true)!;
            var actionType = SussudioAssembly.Load().GetType(ActionTypeName, throwOnError: true)!;
            var method = policyType.GetMethod("ResolveTargetBounds", BindingFlags.Public | BindingFlags.Static)!;
            var parameterTypes = method.GetParameters();
            var workArea = CreateStruct(parameterTypes[1].ParameterType, 10, 20, 101, 55);
            var currentSize = CreateStruct(parameterTypes[2].ParameterType, 33, 15);
            var action = actionName is null
                ? Enum.ToObject(actionType, 999)
                : Enum.Parse(actionType, actionName);

            var result = method.Invoke(null, new[] { action, workArea, currentSize });

            Assert.Null(result);
        }

        private static object CreateStruct(Type type, params int[] args)
            => Activator.CreateInstance(type, args.Cast<object>().ToArray())!;

        private static void AssertRect(object rect, int x, int y, int width, int height)
        {
            Assert.Equal(x, ReadIntProperty(rect, "X"));
            Assert.Equal(y, ReadIntProperty(rect, "Y"));
            Assert.Equal(width, ReadIntProperty(rect, "Width"));
            Assert.Equal(height, ReadIntProperty(rect, "Height"));
        }

        private static int ReadIntProperty(object instance, string propertyName)
        {
            var type = instance.GetType();
            var property = type.GetProperty(propertyName, ReflectionFlags.Instance);
            if (property != null)
            {
                return (int)property.GetValue(instance)!;
            }

            return (int)type.GetField(propertyName, ReflectionFlags.Instance)!.GetValue(instance)!;
        }
    }

    public class MainWindowUiContractStatsSnapshotTests
    {

    [Fact]
    public void StatsSnapshotBuilder_MapsSourceCadenceMetrics()
    {
        var snapshot = BuildStatsSnapshot();

        Assert.Equal(60, GetIntProperty(snapshot, "SourceCadenceSamples"));
        AssertNearlyEqual(119.8d, GetDoubleProperty(snapshot, "SourceObservedFps"), 0.0001);
    }

    [Fact]
    public void StatsSnapshotBuilder_MapsPreviewCadenceMetrics()
    {
        var snapshot = BuildStatsSnapshot();

        Assert.Equal(20, GetIntProperty(snapshot, "PreviewCadenceSamples"));
        AssertNearlyEqual(118.2d, GetDoubleProperty(snapshot, "PreviewOnePercentLowFps"), 0.0001);
    }

    [Fact]
    public void StatsSnapshotBuilder_SanitizesPreviewSlowPercentBeforeScoring()
    {
        var snapshot = BuildStatsSnapshot();

        AssertNearlyEqual(0.0d, GetDoubleProperty(snapshot, "PreviewSlowPct"), 0.0001);
        AssertNearlyEqual(99.5d, GetDoubleProperty(snapshot, "PerformanceScore"), 0.0001);
    }

    [Fact]
    public void StatsSnapshotBuilder_CarriesPresentationState()
    {
        var snapshot = BuildStatsSnapshot();

        Assert.True(GetBoolProperty(snapshot, "Previewing"));
        Assert.False(GetBoolProperty(snapshot, "Recording"));
    }

    [Fact]
    public void StatsSnapshotBuilder_ConvertsNegotiatedCaptureWidth()
    {
        var snapshot = BuildStatsSnapshot();

        Assert.Equal(1920, GetIntProperty(snapshot, "CaptureWidth"));
    }

    [Fact]
    public void StatsSnapshotBuilder_PreservesTelemetryProvenance()
    {
        var snapshot = BuildStatsSnapshot();

        Assert.Equal("NativeXu", GetStringProperty(snapshot, "TelemetryOrigin"));
        Assert.Equal("High", GetStringProperty(snapshot, "TelemetryConfidence"));
    }

    [Fact]
    public void StatsSnapshotBuilder_AppendsCaptureFormatToTelemetryDetails()
    {
        var snapshot = BuildStatsSnapshot();

        Assert.Equal(2, GetCountProperty(GetPropertyValue(snapshot, "SourceTelemetryDetails")));
    }

    [Fact]
    public void StatsSnapshotBuilder_ClassifiesSourceCadenceDrops()
    {
        var snapshot = BuildStatsSnapshot();

        Assert.Equal("Warning", GetStringProperty(snapshot, "DiagnosticHealthStatus"));
        Assert.Equal("source_capture", GetStringProperty(snapshot, "DiagnosticLikelyStage"));
    }

    [Fact]
    public void StatsSnapshotBuilder_PreservesPreviewIntervalSamples()
    {
        var snapshot = BuildStatsSnapshot();

        Assert.Equal(2, GetCountProperty(GetPropertyValue(snapshot, "PreviewRecentPresentIntervalsMs")));
    }

    private static object BuildStatsSnapshot()
    {
        var health = CreateInstance("Sussudio.Models.CaptureHealthSnapshot");
        SetPropertyOrBackingField(health, "NegotiatedWidth", 1920u);
        SetPropertyOrBackingField(health, "ReaderSourceSubtype", "MJPG");
        SetPropertyOrBackingField(health, "CaptureCadenceSampleCount", 60);
        SetPropertyOrBackingField(health, "CaptureCadenceObservedFps", 119.8d);
        SetPropertyOrBackingField(health, "CaptureCadenceEstimatedDropPercent", 0.5d);
        SetPropertyOrBackingField(health, "SourceTelemetryOrigin", ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "NativeXu"));
        SetPropertyOrBackingField(health, "SourceTelemetryConfidence", ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "High"));

        var detailType = RequireType("Sussudio.Models.SourceTelemetryDetailEntry");
        var details = Array.CreateInstance(detailType, 1);
        details.SetValue(
            Activator.CreateInstance(detailType, "Audio / Input", "ADC (Analog)", "On", null),
            0);
        SetPropertyOrBackingField(health, "SourceTelemetryDetails", details);

        var renderMetricsType = RequireType("Sussudio.ViewModels.StatsSnapshotRenderMetrics");
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

        var viewStateType = RequireType("Sussudio.ViewModels.StatsSnapshotViewState");
        var viewState = Activator.CreateInstance(viewStateType, true, false)
            ?? throw new InvalidOperationException("Failed to create StatsSnapshotViewState.");

        var builderType = RequireType("Sussudio.ViewModels.StatsSnapshotBuilder");
        var build = builderType.GetMethod("Build", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("StatsSnapshotBuilder.Build was not found.");
        return build.Invoke(null, new[] { health, renderMetrics, viewState })
            ?? throw new InvalidOperationException("StatsSnapshotBuilder.Build returned null.");
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

    private static void AssertNearlyEqual(double expected, double actual, double tolerance)
        => Assert.True(
            Math.Abs(expected - actual) <= tolerance,
            $"Expected {expected:0.####}, got {actual:0.####}; tolerance {tolerance:0.####}.");
    }
}
