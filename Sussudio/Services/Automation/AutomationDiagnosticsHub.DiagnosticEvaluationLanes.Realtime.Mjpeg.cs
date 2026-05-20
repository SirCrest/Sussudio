using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private static string BuildDecodeLane(
        CaptureHealthSnapshot health,
        MjpegRecentCounters recentMjpeg)
    {
        return $"decode p95={health.MjpegDecodeP95Ms:0.##}ms callbackP95={health.MjpegCallbackP95Ms:0.##}ms dropped={health.MjpegTotalDropped} failures={health.MjpegDecodeFailures + health.MjpegEmitFailures} recentDropped={recentMjpeg.TotalDropped} recentFailures={recentMjpeg.Failures}";
    }
}
