using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sussudio.Tools;

internal sealed partial class DiagnosticSessionLiveStateWriter
{
    private DateTimeOffset _lastSamplingLiveStateUtc = DateTimeOffset.MinValue;

    internal async Task WriteSamplingLiveStateBestEffortAsync(
        IReadOnlyList<DiagnosticSessionSample> samples,
        JsonElement initialSnapshot,
        int commandFailureCount)
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _lastSamplingLiveStateUtc < TimeSpan.FromSeconds(5))
        {
            return;
        }

        _lastSamplingLiveStateUtc = now;
        await WriteLiveStateBestEffortAsync(samples, initialSnapshot, commandFailureCount).ConfigureAwait(false);
    }
}
