namespace Sussudio.Controllers;

internal static class PreviewStartupFailureTextFormatter
{
    public static string FormatTimeoutReason(int timeoutMs, string? missingSignals)
        => string.IsNullOrWhiteSpace(missingSignals)
            ? $"no-visual-confirmation-within-{timeoutMs}ms"
            : $"no-visual-confirmation-within-{timeoutMs}ms missing:{missingSignals}";

    public static string FormatTimeoutStatusText(string? missingSignals)
        => string.IsNullOrWhiteSpace(missingSignals)
            ? "Preview failed to attach to UI (session started but no visual confirmation)."
            : $"Preview failed to start (missing readiness signal: {missingSignals}).";

    public static string FormatFailureStopStatusText(string reason)
        => $"Preview startup failed: {reason}";
}
