using System;

namespace ElgatoCapture.Services;

internal static class DeviceSymbolicLinkMatcher
{
    internal static bool Matches(string target, string candidate)
    {
        if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        return string.Equals(target, candidate, StringComparison.OrdinalIgnoreCase) ||
               candidate.Contains(target, StringComparison.OrdinalIgnoreCase) ||
               target.Contains(candidate, StringComparison.OrdinalIgnoreCase);
    }
}
