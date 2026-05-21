using System;
using System.IO;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    private static long GetFileSize(string path)
    {
        try
        {
            return File.Exists(path) ? new FileInfo(path).Length : 0;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_SINK_FILE_SIZE_WARN path='{path}' type={ex.GetType().Name} msg='{ex.Message}'");
            return 0;
        }
    }

    private static string CreateSessionId()
    {
        return $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds():x}_{Guid.NewGuid():N}";
    }
}
