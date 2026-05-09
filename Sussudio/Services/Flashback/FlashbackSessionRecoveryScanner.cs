using System;
using System.IO;

namespace Sussudio.Services.Flashback;

/// <summary>
/// Session-directory naming and probing helpers used during flashback initialization
/// and recovery-directory scanning.
/// </summary>
internal static class FlashbackSessionRecoveryScanner
{
    internal static string BuildSessionDirectory(string tempDirectory, string sessionId)
    {
        if (Path.IsPathRooted(sessionId) ||
            sessionId.IndexOf(Path.DirectorySeparatorChar) >= 0 ||
            sessionId.IndexOf(Path.AltDirectorySeparatorChar) >= 0 ||
            sessionId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Session id must be a simple file-name component.", nameof(sessionId));
        }

        var tempRoot = EnsureTrailingDirectorySeparator(Path.GetFullPath(tempDirectory));
        var sessionDirectory = Path.GetFullPath(Path.Combine(tempRoot, sessionId));
        if (!IsPathUnderDirectory(sessionDirectory, tempRoot))
        {
            throw new ArgumentException("Session id must resolve inside the flashback temp directory.", nameof(sessionId));
        }

        return sessionDirectory;
    }

    internal static string NormalizeSegmentExtension(string extension)
    {
        if (string.Equals(extension, ".ts", StringComparison.OrdinalIgnoreCase))
        {
            return ".ts";
        }

        if (string.Equals(extension, ".mp4", StringComparison.OrdinalIgnoreCase))
        {
            return ".mp4";
        }

        throw new ArgumentException("Flashback segment extension must be .ts or .mp4.", nameof(extension));
    }

    internal static string EnsureTrailingDirectorySeparator(string path)
        => Path.EndsInDirectorySeparator(path) ? path : path + Path.DirectorySeparatorChar;

    internal static bool IsPlausibleFlashbackSessionDirectoryName(string name)
    {
        if (name.Length == 32)
        {
            return IsLowerHexString(name);
        }

        var underscore = name.IndexOf('_');
        return underscore > 0 &&
               underscore < name.Length - 1 &&
               IsLowerHexString(name.AsSpan(0, underscore)) &&
               name.AsSpan(underscore + 1).Length == 32 &&
               IsLowerHexString(name.AsSpan(underscore + 1));
    }

    internal static bool IsLowerHexString(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            return false;
        }

        foreach (var ch in value)
        {
            if (!IsLowerHexDigit(ch))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsLowerHexDigit(char value)
        => value is >= '0' and <= '9' or >= 'a' and <= 'f';

    internal static bool IsPathUnderDirectory(string fullPath, string fullDirectoryRoot)
        => fullPath.StartsWith(fullDirectoryRoot, StringComparison.OrdinalIgnoreCase);

    internal static bool IsReparsePoint(FileSystemInfo info)
        => (info.Attributes & FileAttributes.ReparsePoint) != 0;
}
