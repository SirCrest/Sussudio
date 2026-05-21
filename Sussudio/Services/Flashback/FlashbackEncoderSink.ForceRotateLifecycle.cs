using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    private bool TryCancelForceRotate(ForceRotateRequest request)
    {
        lock (_sync)
        {
            if (ReferenceEquals(_forceRotateRequest, request))
            {
                _forceRotateRequested = false;
                _forceRotateRequest = null;
            }
        }

        return request.TryCancel();
    }

    private void CompletePendingForceRotateWithEmptyResult()
    {
        ForceRotateRequest? pendingRequest;
        lock (_sync)
        {
            _forceRotateRequested = false;
            pendingRequest = _forceRotateRequest;
            _forceRotateRequest = null;
        }

        lock (_videoQueueSync)
        {
            Volatile.Write(ref _forceRotateDraining, false);
        }

        pendingRequest?.CompleteEmpty();
    }

    private static bool ShouldAbortForceRotateDrain(
        ForceRotateRequest request,
        string phase,
        int inFlightRounds)
    {
        if (!request.IsCompleted)
        {
            return false;
        }

        Logger.Log($"FLASHBACK_SINK_FORCE_ROTATE_ABORT_DRAIN phase={phase} in_flight_rounds={inFlightRounds}");
        return true;
    }
}
