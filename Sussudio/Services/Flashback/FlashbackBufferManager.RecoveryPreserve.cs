using System;
using System.IO;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackBufferManager
{
    public bool IsSessionPreservedForRecovery
    {
        get
        {
            lock (_indexLock)
            {
                return IsSessionPreservedForRecoveryUnsafe();
            }
        }
    }

    public void MarkSessionPreservedForRecovery()
    {
        lock (_indexLock)
        {
            if (_disposed)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(_sessionDirectory))
            {
                return;
            }

            _preserveSessionForRecovery = true;

            try
            {
                Directory.CreateDirectory(_sessionDirectory);
                var markerPath = Path.Combine(_sessionDirectory, RecoveryPreserveMarkerFileName);
                File.WriteAllText(markerPath, DateTimeOffset.UtcNow.ToString("O"));
                Logger.Log($"FLASHBACK_RECOVERY_PRESERVE_MARKER dir='{_sessionDirectory}'");
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_RECOVERY_PRESERVE_MARKER_WARN dir='{_sessionDirectory}' type={ex.GetType().Name} msg={ex.Message}");
            }
        }
    }

    private bool IsSessionPreservedForRecoveryUnsafe()
    {
        if (_preserveSessionForRecovery)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(_sessionDirectory))
        {
            return false;
        }

        try
        {
            return File.Exists(Path.Combine(_sessionDirectory, RecoveryPreserveMarkerFileName));
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_RECOVERY_PRESERVE_MARKER_CHECK_WARN dir='{_sessionDirectory}' type={ex.GetType().Name} msg={ex.Message}");
            return true;
        }
    }
}
