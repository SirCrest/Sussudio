namespace ElgatoCapture.Services.Automation;

internal static class AutomationPipeSecurityPolicy
{
    public static bool ShouldDisableDefaultSecurityFallback(
        bool isWindows,
        bool hasExplicitSecurityDescriptor,
        bool explicitSecurityFailed,
        bool authTokenRequired)
        => isWindows &&
           (!hasExplicitSecurityDescriptor || explicitSecurityFailed) &&
           !authTokenRequired;
}
