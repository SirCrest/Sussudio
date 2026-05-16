using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class WindowCloseRecordingFinalizationController
{
    private const int StopBudgetMs = 120_000;

    private enum RecordingStopWaitResult
    {
        Completed,
        TimedOut,
    }

    public async Task<bool> StopBeforeCloseAsync(
        MainViewModel viewModel,
        FrameworkElement? shutdownContent,
        Func<bool> isAllowedAfterRecordingStop)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(isAllowedAfterRecordingStop);

        Logger.Log("WINDOW_CLOSE_RECORDING_STOP: recording active, awaiting graceful stop...");
        viewModel.StatusText = "Stopping recording - please wait...";

        if (shutdownContent != null)
        {
            shutdownContent.IsHitTestVisible = false;
            shutdownContent.Opacity = 0.5;
        }

        try
        {
            var stopResult = await WaitForRecordingStopAsync(viewModel);
            if (stopResult == RecordingStopWaitResult.Completed)
            {
                Logger.Log("WINDOW_CLOSE_RECORDING_STOP: recording stopped cleanly.");
                return true;
            }

            Logger.LogFatalBreadcrumb("RECORDING_FINALIZE_TIMEOUT "
                + $"budget_ms={StopBudgetMs}; close cancelled to protect recording.");
            viewModel.StatusText = "Still saving recording. Close cancelled.";
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            Logger.Log($"WINDOW_CLOSE_RECORDING_STOP: stop failed: {ex.Message}");
            viewModel.StatusText = $"Close cancelled: recording stop failed ({ex.Message})";
            return false;
        }
        finally
        {
            if (shutdownContent != null &&
                !isAllowedAfterRecordingStop())
            {
                shutdownContent.IsHitTestVisible = true;
                shutdownContent.Opacity = 1;
            }
        }
    }

    public async Task StopAfterClosedBestEffortAsync(
        MainViewModel viewModel,
        FrameworkElement? shutdownContent)
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        if (!viewModel.IsRecording)
        {
            return;
        }

        Logger.Log("WINDOW_CLOSE_RECORDING_STOP: recording active, awaiting graceful stop...");
        viewModel.StatusText = "Stopping recording — please wait…";

        if (shutdownContent != null)
        {
            shutdownContent.IsHitTestVisible = false;
            shutdownContent.Opacity = 0.5;
        }

        try
        {
            var stopResult = await WaitForRecordingStopAsync(viewModel);
            if (stopResult == RecordingStopWaitResult.Completed)
            {
                Logger.Log("WINDOW_CLOSE_RECORDING_STOP: recording stopped cleanly.");
            }
            else
            {
                Logger.LogFatalBreadcrumb("RECORDING_FINALIZE_TIMEOUT "
                    + $"budget_ms={StopBudgetMs}; window already closed; continuing shutdown cleanup.");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"WINDOW_CLOSE_RECORDING_STOP: stop failed: {ex.Message}");
            Logger.LogFatalBreadcrumb("RECORDING_FINALIZE_FAILED_AFTER_CLOSE "
                + $"window already closed; continuing shutdown cleanup. error='{ex.Message}'");
        }
    }

    private static async Task<RecordingStopWaitResult> WaitForRecordingStopAsync(MainViewModel viewModel)
    {
        var stopTask = viewModel.StopRecordingAndWaitAsync();
        var completed = await Task.WhenAny(stopTask, Task.Delay(StopBudgetMs));
        if (completed == stopTask)
        {
            await stopTask;
            return RecordingStopWaitResult.Completed;
        }

        return RecordingStopWaitResult.TimedOut;
    }
}
