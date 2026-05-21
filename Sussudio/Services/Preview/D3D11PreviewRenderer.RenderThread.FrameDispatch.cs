using System;
using System.Threading;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private bool ProcessRenderThreadFrameOrIdle()
    {
        if (!TryDequeuePendingFrame(out var frame))
        {
            ResetFrameReady("render_loop_idle");
            if (!_pendingFrames.IsEmpty ||
                Volatile.Read(ref _compositionTransformDirty) != 0 ||
                Volatile.Read(ref _sharedDeviceResetPending) != 0)
            {
                SignalFrameReady("render_loop_race");
            }

            return true;
        }

        if (Volatile.Read(ref _stopRequested) != 0)
        {
            TrackFrameDropped(frame, "renderer-stopped");
            frame.Dispose();
            return false;
        }

        try
        {
            if (frame.SubmissionGeneration != Interlocked.Read(ref _submissionGeneration))
            {
                var reason = Volatile.Read(ref _submissionGenerationDropReason);
                TrackFrameDropped(frame, string.IsNullOrWhiteSpace(reason) ? "stale-generation" : $"{reason}:stale");
                return true;
            }

            WaitForFrameLatencySignal();
            var framesRenderedBefore = Interlocked.Read(ref _framesRendered);
            RenderFrame(frame);
            if (Interlocked.Read(ref _framesRendered) == framesRenderedBefore)
            {
                TrackFrameDropped(frame, "render-skipped");
            }

            // Keep the event set while more frames are queued so the
            // render thread drains the elastic buffer without waiting.
            if (!_pendingFrames.IsEmpty)
            {
                SignalFrameReady("render_loop_drain");
            }
        }
        catch (Exception ex)
        {
            if (IsDeviceLostException(ex))
            {
                HandleDeviceLost(ex);
            }
            else
            {
                Logger.Log($"D3D11 preview render failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
            }

            TrackFrameDropped(frame, "render-failed");
        }
        finally
        {
            frame.Dispose();
        }

        if (_pendingFrames.IsEmpty &&
            Volatile.Read(ref _compositionTransformDirty) == 0 &&
            Volatile.Read(ref _sharedDeviceResetPending) == 0)
        {
            ResetFrameReady("render_loop_empty_after_failure");
        }

        return true;
    }
}
