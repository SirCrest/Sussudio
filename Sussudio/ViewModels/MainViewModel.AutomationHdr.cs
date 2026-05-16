using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation HDR enablement mutators.
/// </summary>
public partial class MainViewModel
{
    public Task SetHdrEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            if (IsRecording)
            {
                throw new InvalidOperationException(HdrToggleBlockedWhileRecordingMessage);
            }

            if (enabled && !IsHdrAvailable)
            {
                throw new InvalidOperationException("HDR is not available on the selected device.");
            }

            IsHdrEnabled = enabled;
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SetTrueHdrPreviewEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            if (IsRecording)
            {
                throw new InvalidOperationException("True HDR preview cannot be changed while recording.");
            }

            IsTrueHdrPreviewEnabled = enabled;
            return Task.CompletedTask;
        }, cancellationToken);
    }
}
