using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainViewModelCapture_RecordingFailuresPropagateToCallers()
    {
        var recordingTransitionControllerRootText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.cs")
            .Replace("\r\n", "\n");
        var recordingTransitionControllerOperationsText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelRecordingTransitionController.Operations.cs")
            .Replace("\r\n", "\n");
        var recordingTransitionControllerText = recordingTransitionControllerRootText
            + "\n" + recordingTransitionControllerOperationsText;

        AssertContains(recordingTransitionControllerText, "Logger.LogException(ex);");
        AssertContains(recordingTransitionControllerText, "_context.SetIsRecording(_context.GetSessionIsRecording());");
        AssertContains(recordingTransitionControllerText, "catch (OperationCanceledException ex)");
        AssertContains(recordingTransitionControllerText, "transitionError = ex;");
        AssertContains(recordingTransitionControllerText, "Logger.Log($\"Recording transition wait canceled: {ex.Message}\");");
        AssertContains(recordingTransitionControllerText, "if (transitionError is OperationCanceledException transitionCanceled && inFlightTarget == (enabled ? 1 : 0))");
        AssertContains(recordingTransitionControllerText, "throw transitionCanceled;");
        AssertContains(recordingTransitionControllerText, "catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)");
        AssertContains(recordingTransitionControllerText, "_context.SetStatusText(\"Recording start canceled\");");
        AssertContains(recordingTransitionControllerText, "_context.SetStatusText(\"Stop recording canceled\");");
        AssertContains(recordingTransitionControllerText, "_context.SetStatusText($\"Recording failed: {ex.Message}\");");
        AssertContains(recordingTransitionControllerText, "_context.SetStatusText($\"Stop recording failed: {ex.Message}\");");
        AssertContains(recordingTransitionControllerText, "throw;");

        return Task.CompletedTask;
    }
}
