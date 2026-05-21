namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private static string BuildSlowFrameDiagnosticReason(
        double presentIntervalMs,
        double totalFrameCpuMs,
        double presentCallMs,
        bool dxgiRefreshSlip,
        double thresholdMs)
    {
        var reason = string.Empty;
        AppendSlowFrameReason(ref reason, presentIntervalMs >= thresholdMs, "present_interval");
        AppendSlowFrameReason(ref reason, totalFrameCpuMs >= thresholdMs, "total_cpu");
        AppendSlowFrameReason(ref reason, presentCallMs >= thresholdMs, "present_call");
        AppendSlowFrameReason(ref reason, dxgiRefreshSlip, "dxgi_refresh_slip");
        return reason.Length > 0 ? reason : "unknown";
    }

    private static void AppendSlowFrameReason(ref string reason, bool condition, string token)
    {
        if (!condition)
        {
            return;
        }

        reason = reason.Length == 0 ? token : $"{reason}+{token}";
    }
}
