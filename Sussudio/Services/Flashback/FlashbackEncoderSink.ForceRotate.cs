using System;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    private bool _forceRotateRequested;
    private volatile ForceRotateRequest? _forceRotateRequest;
    private TimeSpan _forceRotateInPoint;
    private TimeSpan _forceRotateOutPoint;
    private bool _forceRotateDraining;

    public bool IsForceRotateActive =>
        Volatile.Read(ref _forceRotateRequested) ||
        Volatile.Read(ref _forceRotateDraining);
    public bool IsForceRotateRequested => Volatile.Read(ref _forceRotateRequested);
    public bool IsForceRotateDraining => Volatile.Read(ref _forceRotateDraining);

    public bool WaitForForceRotateIdle(TimeSpan timeout)
    {
        var timeoutMs = Math.Max(0, (long)timeout.TotalMilliseconds);
        var deadlineTick = Environment.TickCount64 + timeoutMs;
        while (IsForceRotateActive)
        {
            if (timeoutMs == 0 || Environment.TickCount64 >= deadlineTick)
            {
                return false;
            }

            SignalWork("force_rotate_idle");
            if (WaitForCancellation(TimeSpan.FromMilliseconds(10)))
            {
                return false;
            }
        }

        return true;
    }
}
