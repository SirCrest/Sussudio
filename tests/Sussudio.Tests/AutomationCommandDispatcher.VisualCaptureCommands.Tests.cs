using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationCommandDispatcher_VisualCaptureCommands_LiveInFocusedPartial()
    {
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var visualCaptureCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.VisualCaptureCommands.cs")
            .Replace("\r\n", "\n");

        AssertContains(customCommandsText, "case AutomationCommandKind.ProbeVideoSource:");
        AssertContains(customCommandsText, "ExecuteProbeVideoSourceCommandAsync(correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.ProbePreviewColor:");
        AssertContains(customCommandsText, "ExecuteProbePreviewColorCommandAsync(correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.CapturePreviewFrame:");
        AssertContains(customCommandsText, "ExecuteCapturePreviewFrameCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "case AutomationCommandKind.CaptureWindowScreenshot:");
        AssertContains(customCommandsText, "ExecuteCaptureWindowScreenshotCommandAsync(payload, correlationId, cancellationToken)");

        AssertDoesNotContain(customCommandsText, "_viewModel.ProbeVideoSourceAsync");
        AssertDoesNotContain(customCommandsText, "_viewModel.ProbePreviewColorAsync");
        AssertDoesNotContain(customCommandsText, "Path.Combine(Path.GetTempPath()");
        AssertDoesNotContain(customCommandsText, "_windowControl.CaptureWindowScreenshotAsync");

        AssertContains(visualCaptureCommandsText, "private async Task<AutomationCommandResponse> ExecuteProbeVideoSourceCommandAsync(");
        AssertContains(visualCaptureCommandsText, "_viewModel.ProbeVideoSourceAsync(cancellationToken)");
        AssertContains(visualCaptureCommandsText, "private async Task<AutomationCommandResponse> ExecuteProbePreviewColorCommandAsync(");
        AssertContains(visualCaptureCommandsText, "_viewModel.ProbePreviewColorAsync(cancellationToken)");
        AssertContains(visualCaptureCommandsText, "AutomationCommandKind.CapturePreviewFrame");
        AssertContains(visualCaptureCommandsText, "preview_capture_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.bmp");
        AssertContains(visualCaptureCommandsText, "AutomationCommandKind.CaptureWindowScreenshot");
        AssertContains(visualCaptureCommandsText, "window_screenshot_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.png");
        AssertContains(visualCaptureCommandsText, "CreateCaptureResponse(correlationId, result.Message, result, result.Succeeded)");
        AssertContains(visualCaptureCommandsText, "errorCode: succeeded ? null : \"capture-failed\"");

        return Task.CompletedTask;
    }
}
