using System.Threading;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private int _firstFrameRaised;

    private void ResetFirstFrameNotification()
        => Interlocked.Exchange(ref _firstFrameRaised, 0);

    private void NotifyFirstFrameRendered(string message)
    {
        if (Interlocked.Exchange(ref _firstFrameRaised, 1) != 0)
        {
            return;
        }

        Logger.Log(message);
        if (!_dispatcherQueue.TryEnqueue(() => FirstFrameRendered?.Invoke()))
        {
            Logger.Log("D3D_FIRST_FRAME_UI_ENQUEUE_FAILED");
        }
    }
}
