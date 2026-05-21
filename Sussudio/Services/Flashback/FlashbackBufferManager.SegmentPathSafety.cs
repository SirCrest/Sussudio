using System;
using System.IO;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackBufferManager
{
    private bool IsPathInSessionDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(_sessionDirectory))
        {
            return false;
        }

        try
        {
            var sessionRoot = FlashbackSessionRecoveryScanner.EnsureTrailingDirectorySeparator(Path.GetFullPath(_sessionDirectory));
            var fullPath = Path.GetFullPath(path);
            return FlashbackSessionRecoveryScanner.IsPathUnderDirectory(fullPath, sessionRoot);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_BUFFER_SEGMENT_PATH_WARN path='{path}' type={ex.GetType().Name} msg='{ex.Message}'");
            return false;
        }
    }
}
