using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal sealed partial class DiagnosticSessionCommandChannel
{
    internal async Task TryWaitAsync(string condition, int timeoutMs)
        => await TryWaitWithTokenAsync(condition, timeoutMs, _defaultCancellationToken).ConfigureAwait(false);

    internal async Task TryWaitWithTokenAsync(string condition, int timeoutMs, CancellationToken waitCancellationToken)
    {
        var response = await SendWithTokenAsync(
                "WaitForCondition",
                new Dictionary<string, object?>
                {
                    ["condition"] = condition,
                    ["timeoutMs"] = timeoutMs,
                    ["pollMs"] = 250
                },
                timeoutMs + 2_000,
                false,
                waitCancellationToken)
            .ConfigureAwait(false);
        if (!IsSuccess(response))
        {
            _warnings.Add($"wait {condition}: {Get(response, "Message", "not met")}");
        }
    }
}
