using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

// Flashback restart orchestration: committed rebuild after preview backend teardown.
public partial class CaptureService
{
    private async Task RestartFlashbackCoreAsync(CancellationToken cancellationToken)
    {
        await DisposeFlashbackPreviewBackendAsync(cancellationToken, purgeSegments: true).ConfigureAwait(false);

        var committedRestartToken = CancellationToken.None;
        var unifiedVideoCapture = _unifiedVideoCapture;
        var settings = _currentSettings;
        if (!_flashbackEnabled || unifiedVideoCapture == null || settings == null)
        {
            Logger.Log($"FLASHBACK_RESTART_TEARDOWN_ONLY enabled={_flashbackEnabled} capture={unifiedVideoCapture != null} settings={settings != null}");
            return;
        }

        await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, committedRestartToken).ConfigureAwait(false);
        Logger.Log("FLASHBACK_RESTART_OK");
        cancellationToken.ThrowIfCancellationRequested();
    }
}
