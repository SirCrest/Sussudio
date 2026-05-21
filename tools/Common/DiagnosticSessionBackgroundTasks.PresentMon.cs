using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sussudio.Tools;

internal sealed partial class DiagnosticSessionBackgroundTasks
{
    private Task<PresentMonProbeResult>? _presentMonTask;

    internal void SetPresentMon(Task<PresentMonProbeResult> task)
    {
        _presentMonTask = task;
    }

    internal async Task<PresentMonProbeResult?> CompletePresentMonAsync(
        PresentMonProbeResult? presentMon,
        List<string> warnings)
    {
        return await AwaitPresentMonAsync(presentMon, warnings).ConfigureAwait(false);
    }

    private async Task<PresentMonProbeResult?> ObservePresentMonAfterFaultAsync(
        List<string> warnings,
        Action<Exception, string> recordTerminalException,
        PresentMonProbeResult? presentMon)
    {
        if (_presentMonTask is null || _presentMonTask.IsCompletedSuccessfully)
        {
            if (_presentMonTask is { IsCompletedSuccessfully: true } && presentMon is null)
            {
                presentMon = await _presentMonTask.ConfigureAwait(false);
            }

            return presentMon;
        }

        try
        {
            var completedTask = _presentMonTask.IsCompleted
                ? _presentMonTask
                : await Task.WhenAny(_presentMonTask, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
            if (!ReferenceEquals(completedTask, _presentMonTask))
            {
                warnings.Add("presentmon-task: task still running after diagnostic interruption");
                return presentMon;
            }

            presentMon = await _presentMonTask.ConfigureAwait(false);
            if (!presentMon.Success)
            {
                warnings.Add($"PresentMon failed: {presentMon.Message}");
            }

            return presentMon;
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, "presentmon-task");
            return presentMon;
        }
    }

    private async Task<PresentMonProbeResult?> AwaitPresentMonAsync(
        PresentMonProbeResult? current,
        List<string> warnings)
    {
        if (_presentMonTask is null)
        {
            return current;
        }

        var result = await _presentMonTask.ConfigureAwait(false);
        if (!result.Success)
        {
            warnings.Add($"PresentMon failed: {result.Message}");
        }

        return result;
    }
}
