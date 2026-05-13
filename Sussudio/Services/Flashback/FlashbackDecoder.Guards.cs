using System;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackDecoder
{
    private void ThrowIfNotInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("FlashbackDecoder has not been initialized. Call Initialize() first.");
        }
    }

    private void ThrowIfNotOpen()
    {
        ThrowIfDisposed();
        if (!_isOpen)
        {
            throw new InvalidOperationException("No file is open. Call OpenFile() first.");
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
