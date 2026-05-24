using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task PreviewScreenshotButtonWorkflow_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var adapterText = ReadRepoFile("Sussudio/MainWindow.ButtonActions.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Screenshot/Preview/PreviewScreenshotController.cs").Replace("\r\n", "\n");
        var policyText = ReadRepoFile("Sussudio/Controllers/Screenshot/Preview/PreviewScreenshotPlanPolicy.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");

        AssertContains(adapterText, "private PreviewScreenshotController _previewScreenshotController = null!;");
        AssertContains(adapterText, "private void InitializePreviewScreenshotController()");
        AssertContains(adapterText, "ViewModel = ViewModel,");
        AssertContains(adapterText, "ScreenshotButton = ScreenshotButton,");
        AssertContains(adapterText, "private Task CapturePreviewScreenshotAsync()");
        AssertContains(adapterText, "=> _previewScreenshotController.CaptureAsync();");
        AssertContains(adapterText, "private void ScreenshotButton_Click(object sender, RoutedEventArgs e)");
        AssertContains(adapterText, "_ = RunUiEventHandlerAsync(() => CapturePreviewScreenshotAsync(), nameof(ScreenshotButton_Click));");
        AssertContains(mainWindowText, "InitializePreviewScreenshotController();");
        AssertContains(controllerText, "internal sealed class PreviewScreenshotController");
        AssertContains(controllerText, "public async Task CaptureAsync()");
        AssertContains(controllerText, "PreviewScreenshotPlanPolicy.PreviewRequiredStatusText");
        AssertContains(controllerText, "PreviewScreenshotPlanPolicy.Create(");
        AssertContains(controllerText, "Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)");
        AssertContains(controllerText, "DateTime.Now");
        AssertContains(controllerText, "Directory.CreateDirectory(plan.OutputDirectory);");
        AssertContains(controllerText, "CapturePreviewFrameAsync(plan.FilePath)");
        AssertContains(controllerText, "PreviewScreenshotPlanPolicy.FormatSavedStatus(plan.FilePath)");
        AssertContains(controllerText, "PreviewScreenshotPlanPolicy.FormatSavedLog(plan.FilePath, result.CapturedWidth, result.CapturedHeight)");
        AssertContains(controllerText, "PreviewScreenshotPlanPolicy.FormatFailedStatus(result.Message)");
        AssertContains(controllerText, "PreviewScreenshotPlanPolicy.FormatFailedLog(result.Message)");
        AssertContains(policyText, "internal static class PreviewScreenshotPlanPolicy");
        AssertContains(policyText, "PreviewRequiredStatusText = \"Start preview before capturing a screenshot\"");
        AssertContains(policyText, "Path.Combine(picturesFolder, DefaultOutputFolderName)");
        AssertContains(policyText, "$\"Screenshot_{timestamp.ToString(TimestampFormat)}.png\"");
        AssertContains(policyText, "=> $\"Screenshot saved: {Path.GetFileName(filePath)}\";");
        AssertContains(policyText, "=> $\"Screenshot failed: {message}\";");
        AssertContains(policyText, "=> $\"SCREENSHOT_SAVED path={filePath} width={capturedWidth} height={capturedHeight}\";");
        AssertContains(policyText, "=> $\"SCREENSHOT_FAILED reason={message}\";");
        AssertContains(policyText, "internal readonly record struct PreviewScreenshotPlan(string OutputDirectory, string FilePath);");
        AssertContains(controllerText, "_context.ScreenshotButton.IsEnabled = false;");
        AssertContains(controllerText, "_context.ScreenshotButton.IsEnabled = true;");
        AssertContains(agentMapText, "PreviewScreenshotPlanPolicy.cs");
        AssertContains(cleanupPlanText, "PreviewScreenshotPlanPolicy.cs");
        AssertDoesNotContain(adapterText, "Directory.CreateDirectory(outputDir);");
        AssertDoesNotContain(adapterText, "CapturePreviewFrameAsync(");
        AssertDoesNotContain(controllerText, "Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), \"Sussudio\")");
        AssertDoesNotContain(controllerText, "Screenshot saved: {Path.GetFileName(filePath)}");
        AssertDoesNotContain(policyText, "Button");
        AssertDoesNotContain(policyText, "CapturePreviewFrameAsync");
        AssertDoesNotContain(policyText, "Directory.CreateDirectory");
        AssertDoesNotContain(policyText, "Logger.Log");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Screenshot.cs")),
            "preview screenshot button adapter lives with MainWindow button actions");

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
        var fallbackPlan = create.Invoke(null, new object?[] { "   ", "C:\\Users\\crest\\Pictures", timestamp })
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.Create returned null.");
        var configuredPlan = create.Invoke(null, new object?[] { "D:\\Captures", "C:\\Users\\crest\\Pictures", timestamp })
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
            "C:\\Users\\crest\\Pictures\\Sussudio\\Screenshot_2026-05-16_14-03-04.png",
            fallbackPath,
            "preview screenshot fallback path");
        AssertEqual(
            "D:\\Captures",
            GetStringProperty(configuredPlan, "OutputDirectory"),
            "preview screenshot configured output directory");
        AssertEqual(
            "D:\\Captures\\Screenshot_2026-05-16_14-03-04.png",
            configuredPath,
            "preview screenshot configured path");
        AssertEqual(
            "Screenshot saved: Screenshot_2026-05-16_14-03-04.png",
            savedStatus.Invoke(null, new object[] { configuredPath })?.ToString(),
            "preview screenshot saved status");
        AssertEqual(
            "SCREENSHOT_SAVED path=D:\\Captures\\Screenshot_2026-05-16_14-03-04.png width=1280 height=720",
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

    internal static Task MainWindowScreenshot_CompletesOnDispatcherFailureAndCancellation()
    {
        var windowText = ReadRepoFile("Sussudio/MainWindow.WindowShell.cs")
            .Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Screenshot/Window/WindowScreenshotController.cs")
            .Replace("\r\n", "\n");
        var method = ExtractTextBetween(
            controllerText,
            "public Task<WindowScreenshotResult> CaptureAsync",
            "    private WindowScreenshotResult CaptureCore");

        AssertContains(windowText, "public Task<WindowScreenshotResult> CaptureWindowScreenshotAsync");
        AssertContains(windowText, "=> _windowScreenshotController.CaptureAsync(outputPath, cancellationToken);");
        AssertContains(method, "if (cancellationToken.IsCancellationRequested)");
        AssertContains(method, "Message = \"Screenshot canceled.\"");
        AssertContains(method, "CancellationTokenRegistration cancellationRegistration = default;");
        AssertContains(method, "cancellationToken.Register(() =>");
        AssertContains(method, "_ = completion.Task.ContinueWith(");
        AssertContains(method, "cancellationRegistration.Dispose()");
        AssertContains(method, "if (!_dispatcherQueue.TryEnqueue(() =>");
        AssertContains(method, "Message = \"Failed to enqueue screenshot capture on the UI thread.\"");
        AssertContains(controllerText, "=> WindowScreenshotNativeCapture.Capture(_windowHandleProvider(), outputPath);");

        return Task.CompletedTask;
    }

    internal static Task WindowScreenshotNativeCapture_LivesInFocusedHelper()
    {
        var controllerText = ReadRepoFile("Sussudio/Controllers/Screenshot/Window/WindowScreenshotController.cs")
            .Replace("\r\n", "\n");
        var nativeCaptureText = ReadRepoFile("Sussudio/Controllers/Screenshot/Window/WindowScreenshotNativeCapture.cs")
            .Replace("\r\n", "\n");

        AssertContains(controllerText, "=> WindowScreenshotNativeCapture.Capture(_windowHandleProvider(), outputPath);");
        AssertDoesNotContain(controllerText, "[DllImport(");
        AssertDoesNotContain(controllerText, "PrintWindow(");
        AssertDoesNotContain(controllerText, "CreateCompatibleBitmap(");
        AssertDoesNotContain(controllerText, "GetDIBits(");
        AssertDoesNotContain(controllerText, "struct BITMAPINFOHEADER");
        AssertContains(nativeCaptureText, "internal static class WindowScreenshotNativeCapture");
        AssertContains(nativeCaptureText, "internal static WindowScreenshotResult Capture(IntPtr hwnd, string outputPath)");
        AssertContains(nativeCaptureText, "Message = \"Window handle not available.\"");
        AssertContains(nativeCaptureText, "Message = \"PrintWindow failed.\"");
        AssertContains(nativeCaptureText, "var hBitmap = CreateCompatibleBitmap(hdcWindow, width, height);");
        AssertContains(nativeCaptureText, "GetDIBits(hdcScreen, hBitmap, 0, (uint)height, pixelData, ref bmi, 0);");
        AssertContains(nativeCaptureText, "WindowScreenshotImageEncoder.WriteToStream(");
        AssertContains(nativeCaptureText, "Message = $\"Window screenshot saved: {width}x{height}\"");

        return Task.CompletedTask;
    }

    internal static Task WindowScreenshotImageEncoding_LivesInFocusedHelper()
    {
        var controllerText = ReadRepoFile("Sussudio/Controllers/Screenshot/Window/WindowScreenshotController.cs")
            .Replace("\r\n", "\n");
        var nativeCaptureText = ReadRepoFile("Sussudio/Controllers/Screenshot/Window/WindowScreenshotNativeCapture.cs")
            .Replace("\r\n", "\n");
        var encoderText = ReadRepoFile("Sussudio/Controllers/Screenshot/Window/WindowScreenshotImageEncoder.cs")
            .Replace("\r\n", "\n");

        AssertContains(controllerText, "WindowScreenshotNativeCapture.Capture(");
        AssertContains(nativeCaptureText, "WindowScreenshotImageEncoder.WriteToStream(");
        AssertDoesNotContain(controllerText, "private static void WritePngToStream");
        AssertDoesNotContain(controllerText, "private static void WriteBmpToStream");
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
}
