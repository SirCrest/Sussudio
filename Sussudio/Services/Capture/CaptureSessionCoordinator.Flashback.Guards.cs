using System;
using System.Threading;
using Sussudio.Models;
using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

public sealed partial class CaptureSessionCoordinator
{
    private bool TryGetActiveFlashback(
        string command,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out FlashbackPlaybackController? controller)
    {
        ThrowIfDisposed();
        controller = _captureService.FlashbackPlaybackController;
        if (controller is { IsDisposed: false, IsInitialized: true, State: not FlashbackPlaybackState.Disabled })
        {
            return true;
        }

        var reason = controller == null
            ? "missing_controller"
            : controller.IsDisposed
                ? "disposed"
                : !controller.IsInitialized
                ? "not_initialized"
                : $"state_{controller.State}";
        _lastFlashbackCommandRejection = $"{reason}:{command}";
        Interlocked.Exchange(ref _lastFlashbackCommandRejectionUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Logger.Log($"FLASHBACK_COORD_COMMAND_REJECTED command={command} reason={reason}");
        return false;
    }
}
