using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationCommandDispatcher_RequiresReadyDevices_ClassifiesCommands()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("RequiresReadyDevices",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RequiresReadyDevices not found.");

        var commandType = RequireType("Sussudio.Models.AutomationCommandKind");

        // UI/info commands should NOT require ready devices
        var getSnapshot = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "GetSnapshot") })!;
        AssertEqual(false, getSnapshot, "GetSnapshot does not require ready devices");

        var windowAction = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "WindowAction") })!;
        AssertEqual(false, windowAction, "WindowAction does not require ready devices");

        var authenticate = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "Authenticate") })!;
        AssertEqual(false, authenticate, "Authenticate does not require ready devices");

        var setFlashbackEnabled = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "SetFlashbackEnabled") })!;
        AssertEqual(false, setFlashbackEnabled, "SetFlashbackEnabled does not require ready devices");

        var getAutomationManifest = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "GetAutomationManifest") })!;
        AssertEqual(false, getAutomationManifest, "GetAutomationManifest does not require ready devices");

        var setFullScreenEnabled = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "SetFullScreenEnabled") })!;
        AssertEqual(false, setFullScreenEnabled, "SetFullScreenEnabled does not require ready devices");

        var openRecordingsFolder = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "OpenRecordingsFolder") })!;
        AssertEqual(false, openRecordingsFolder, "OpenRecordingsFolder does not require ready devices");

        // Capture configuration commands SHOULD require ready devices
        var setResolution = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "SetResolution") })!;
        AssertEqual(true, setResolution, "SetResolution requires ready devices");

        var setFrameRate = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "SetFrameRate") })!;
        AssertEqual(true, setFrameRate, "SetFrameRate requires ready devices");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_WindowClose_AwaitsCloseCompletion()
    {
        var sourceText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.WindowCommands.cs")
            .Replace("\r\n", "\n");
        var windowActionBlock = ExtractTextBetween(
            sourceText,
            "private async Task<AutomationCommandResponse> ExecuteWindowActionCommandAsync(",
            "return CreateAcknowledgedResponse(correlationId, $\"Window action requested: {action}.\");");
        var closeBlock = ExtractTextBetween(
            windowActionBlock,
            "if (action == AutomationWindowAction.Close)",
            "await ExecuteWindowActionAsync(action, cancellationToken, payload).ConfigureAwait(false);");

        AssertContains(closeBlock, "await ExecuteWindowActionAsync(action, cancellationToken).ConfigureAwait(false);");
        AssertContains(closeBlock, "Window close completed.");
        AssertDoesNotContain(closeBlock, "ContinueWith(");
        AssertDoesNotContain(closeBlock, "CancellationToken.None");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_PreviewRendererHealthy_RequiresFirstVisual()
    {
        var sourceText = ReadAutomationCommandDispatcherFamilyText();
        var conditionBlock = ExtractTextBetween(
            sourceText,
            "AutomationWaitCondition.PreviewRendererHealthy =>",
            "AutomationWaitCondition.AudioSignalPresent =>");

        AssertContains(conditionBlock, "snapshot.PreviewFirstVisualConfirmed");
        AssertContains(conditionBlock, "snapshot.PreviewGpuActive || snapshot.PreviewFramesDisplayed > 0");
        AssertDoesNotContain(conditionBlock, "snapshot.PreviewGpuActive || snapshot.PreviewRendererAttached");
        AssertDoesNotContain(sourceText, "WaitConditionRefreshCadenceMs");

        return Task.CompletedTask;
    }

    internal static Task UiAutomationCommands_AreNotBlockedOnDeviceReadiness()
    {
        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();

        AssertDoesNotContain(dispatcherText, "AutomationCommandKind.SetShowAllCaptureOptions => true,");
        AssertDoesNotContain(dispatcherText, "AutomationCommandKind.SetPreviewVolume => true,");
        AssertDoesNotContain(dispatcherText, "AutomationCommandKind.SetStatsVisible => true,");
        AssertDoesNotContain(dispatcherText, "AutomationCommandKind.GetCaptureOptions => true,");

        return Task.CompletedTask;
    }
}
