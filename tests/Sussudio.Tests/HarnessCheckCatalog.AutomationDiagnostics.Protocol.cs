using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddAutomationDiagnosticsProtocolChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "Automation SetRecordingEnabled uses recording-sized client timeout",
            AutomationProtocol_SetRecordingUsesRecordingSizedTimeout);
    }
}
