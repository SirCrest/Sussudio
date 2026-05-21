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
