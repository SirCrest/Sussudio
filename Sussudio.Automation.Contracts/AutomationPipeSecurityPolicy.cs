namespace Sussudio.Tools;

// Security fallback policy for the named-pipe server.
public static class AutomationPipeSecurityPolicy
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
