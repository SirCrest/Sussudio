using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Legacy Flashback snapshot compatibility fields consumed by runtime/health snapshots.
public partial class CaptureService
{
    private static string? ResolveFlashbackExportVerificationFormat(
        CaptureSettings? settings,
        UnifiedVideoCapture? unifiedVideoCapture)
        => settings?.Format.ToString();

    /// <summary>
    /// Flashback recording honors the requested codec and preset directly. This legacy
    /// snapshot field remains for compatibility and should stay null unless a future
    /// explicit, user-visible substitution is introduced.
    /// </summary>
    private static string? ResolveFlashbackCodecDowngradeReason(
        CaptureSettings? settings,
        UnifiedVideoCapture? unifiedVideoCapture)
        => null;
}
