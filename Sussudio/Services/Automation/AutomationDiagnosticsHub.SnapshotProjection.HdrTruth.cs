using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static HdrTruthProjection BuildHdrTruthProjection(HdrTruthVerdict verdict)
        => new()
        {
            Verdict = verdict
        };

    private readonly record struct HdrTruthProjection
    {
        public HdrTruthVerdict Verdict { get; init; }
    }
}
