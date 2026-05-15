using System;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackExporter
{
    private CancellationTokenSource CreateExportCancellationSource(CancellationToken ct)
    {
        lock (_lifetimeSync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var disposeCts = _disposeCts ?? throw new ObjectDisposedException(nameof(FlashbackExporter));
            return CancellationTokenSource.CreateLinkedTokenSource(ct, disposeCts.Token);
        }
    }

    private static void DisposeLinkedCtsBestEffort(CancellationTokenSource? cts, string operation)
    {
        if (cts == null) return;

        try
        {
            cts.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_LINKED_CTS_DISPOSE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void ClearDisposeCtsReference(CancellationTokenSource? disposeCts)
    {
        lock (_lifetimeSync)
        {
            if (ReferenceEquals(_disposeCts, disposeCts))
            {
                _disposeCts = null;
            }
        }
    }

    private void EnsureNotDisposed()
    {
        lock (_lifetimeSync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }
}
